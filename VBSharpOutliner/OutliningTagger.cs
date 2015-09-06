using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Text;

namespace VBSharpOutliner
{
    internal class OutliningTagger : ITagger<IOutliningRegionTag>, IDisposable
    {
        //Add some fields to track the text buffer and snapshot and to accumulate the sets of lines that should be tagged as outlining regions. 
        //This code includes a list of Region objects (to be defined later) that represent the outlining regions.		
        private readonly ITextBuffer _buffer;
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
                this.Outline();
            };

            Outline(); // Force an initial full parse
        }

        //Implement the GetTags method, which instantiates the tag spans. 
        //This example assumes that the spans in the NormalizedSpanCollection passed in to the method are contiguous, although this may not always be the case. 
        //This method instantiates a new tag span for each of the outlining regions.
        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;
            if (_outlineSpans == null || !_outlineSpans.Any())
            {
                yield break;
            }
            foreach (var outline in _outlineSpans)
            {
                yield return outline;
            }
        }

        //Add a BufferChanged event handler that responds to Changed events by parsing the text buffer.
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

        //Add a method that parses the buffer. The example given here is for illustration only. 
        //It synchronously parses the buffer into nested outlining regions.
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
