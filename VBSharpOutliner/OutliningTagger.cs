using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Text;

namespace VBSharpOutliner
{
    internal class OutliningTagger : ITagger<IOutliningRegionTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private ITextSnapshot _currSnapshot;
        private readonly DispatcherTimer _updateTimer;
        private List<TagSpan<IOutliningRegionTag>> _outlineSpans = new List<TagSpan<IOutliningRegionTag>>();

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public OutliningTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += BufferChanged;

            _updateTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromMilliseconds(2500)
            };
            _updateTimer.Tick += (sender, args) =>
            {
                _updateTimer.Stop();
                Task.Run(() => Outline());
            };

            Task.Run(() => Outline());
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;
            if (_outlineSpans == null || !_outlineSpans.Any())
            {
                yield break;
            }

            var entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End)
                .TranslateTo(_currSnapshot, SpanTrackingMode.EdgeExclusive);

            foreach (var outline in _outlineSpans)
            {
                var outlineSpanIntersectsTheRequestedRange = outline.Span.Start <= entire.End
                    && outline.Span.End >= entire.Start;
                if (outlineSpanIntersectsTheRequestedRange)
                    yield return outline;
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
            _updateTimer.Stop();
            _updateTimer.Start(); 
        }

        private void Outline()
        {
            try
            {
                var oldSpans = new List<Span>(_outlineSpans
                    .Select(r => r.Span.TranslateTo(_buffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive).Span));

                _outlineSpans = GetOutlineSpans();

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
                _currSnapshot = _buffer.CurrentSnapshot;

                if (changeStart <= changeEnd)
                {
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(_buffer.CurrentSnapshot, Span.FromBounds(changeStart, changeEnd))));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private List<TagSpan<IOutliningRegionTag>> GetOutlineSpans()
        {
            var docs = _buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges();
            var doc = docs.First();
            var tree = doc.GetSyntaxTreeAsync().Result;
            var walker = new SytaxWalkerForOutlining(_buffer.CurrentSnapshot);
            walker.Visit(tree.GetRoot());
            return walker.OutlineSpans;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _updateTimer.Stop();
            _buffer.Changed -= BufferChanged;
        }

        #endregion
    }
}
