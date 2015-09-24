using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Text;

namespace VBSharpOutliner
{
    internal class OutliningTagger : ITagger<IOutliningRegionTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly IdeServices _ideServices;
        private ITextSnapshot _currSnapshot;
        private readonly DispatcherTimer _updateTimer;
        private List<TagSpan<IOutliningRegionTag>> _outlineSpans = new List<TagSpan<IOutliningRegionTag>>();
        private readonly object _outliningLock = new object();
        private bool _isProcessing = false;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Thread _workerThread;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        private bool _isReportingTags = false;

        public OutliningTagger(ITextBuffer buffer,
            IdeServices ideServices)
        {
            _buffer = buffer;
            _ideServices = ideServices;
            _buffer.Changed += BufferChanged;

            _updateTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromMilliseconds(2500)
            };
            _updateTimer.Tick += (sender, args) =>
            {
                _updateTimer.Stop();
                RunOutlineAsync();
            };
            _updateTimer.Start();
        }

        private void RunOutlineAsync()
        {
            if(_workerThread != null)
            {
                _cancellationTokenSource.Cancel();
                _workerThread.Join();
                _cancellationTokenSource = new CancellationTokenSource();
            }
            _workerThread = new Thread(Outline) { Priority = ThreadPriority.BelowNormal };
            _workerThread.Start();
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            List<TagSpan<IOutliningRegionTag>> outlineSpanSnapshot;
            ITextSnapshot textSnapshot;
            lock (_outliningLock)
            {
                if(_isProcessing)
                {
                    yield break;
                }
                textSnapshot = _currSnapshot;
                outlineSpanSnapshot = _outlineSpans;
            }

            if (outlineSpanSnapshot == null || !outlineSpanSnapshot.Any())
            {
                yield break;
            }

            var changeset = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End)
                    .TranslateTo(textSnapshot, SpanTrackingMode.EdgeExclusive);

            var changesetPos = outlineSpanSnapshot.BinarySearch(x => x.Span.Start.CompareTo(changeset.Start));
            if (changesetPos < 0)
            {
                changesetPos = ~changesetPos;
            }
            changesetPos -= 1; //includes the previous span in case it ends in the changeset.
            if (changesetPos < 0)
            {
                changesetPos = 0;
            }

            for (var i = changesetPos; i < outlineSpanSnapshot.Count; ++i)
            {
                var outline = outlineSpanSnapshot[i];
                if (outline.Span.Start <= changeset.End
                    && outline.Span.End >= changeset.Start)
                {
                    yield return outline;
                }
                else if (i == changesetPos) 
                {
                    // the first one may end before the changeset's start
                }
                else
                {
                    yield break;
                }
            }
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, 
            // then ignore it for now (we'll eventually get another change event). 
            if (e.After != _buffer.CurrentSnapshot)
            {
                return;
            }
            if (_buffer.EditInProgress)
            {
                return;
            }
            _updateTimer.Stop();
            _updateTimer.Start(); 
        }

        private void Outline()
        {
            try
            {
                var newSnapshot = _buffer.CurrentSnapshot;
                var oldSpans = new List<Span>(_outlineSpans
                    .Select(r => r.Span.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span));
                var newOutlineSpans = GetOutlineSpans(newSnapshot).OrderBy(r => r.Span.Start).ToList();
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                lock (_outliningLock)
                {
                    _isProcessing = true;
                    _outlineSpans = newOutlineSpans;
                    _currSnapshot = newSnapshot;
                }

                var newSpans = new List<Span>(_outlineSpans.Select(r => r.Span.Span));

                var oldSpanCollection = new NormalizedSpanCollection(oldSpans);
                var newSpanCollection = new NormalizedSpanCollection(newSpans);

                //the changed regions are regions that appear in one set or the other, but not both.
                var removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

                var changeStart = int.MaxValue;
                var changeEnd = -1;

                if (removed.Count > 0)
                {
                    changeStart = removed[0].Start;
                    changeEnd = removed[removed.Count - 1].End;
                }

                if (newSpans.Count > 0)
                {
                    changeStart = Math.Min(changeStart, newSpans[0].Start);
                    changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
                }

                if (changeStart <= changeEnd)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    _ideServices.UiDispatcher.InvokeAsync(() =>
                    {
                        _isReportingTags = true;
                        try
                        {
                            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                                new SnapshotSpan(newSnapshot, Span.FromBounds(changeStart, changeEnd))));
                        }
                        finally
                        {
                            _isReportingTags = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
            finally
            {
                lock (_outliningLock)
                {
                    _isProcessing = false;
                }
            }
        }

        private List<TagSpan<IOutliningRegionTag>> GetOutlineSpans(ITextSnapshot textSnapshot)
        {
            var docs = textSnapshot.GetRelatedDocumentsWithChanges();
            var doc = docs.First();
            var tree = doc.GetSyntaxTreeAsync().Result;
            var walker = new SytaxWalkerForOutlining(textSnapshot, _ideServices, 
                _cancellationTokenSource.Token);
            walker.Visit(tree.GetRoot());
            return walker.OutlineSpans;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_isReportingTags)
            {
                // Seems to be a bug of visual studio, which disposes all Taggers when reporting tags
                // See Microsoft.VisualStudio.Editor.Implementation.SimpleTextViewWindow.OnOutliningTagsChanged
                return;
            }
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
            if (_workerThread != null)
            {
                _workerThread.Join();
                _workerThread = null;
            }
            _updateTimer.Stop();
            _buffer.Changed -= BufferChanged;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
