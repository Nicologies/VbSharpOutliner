using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VBSharpOutliner.CSharp.Outliners
{
    class BlockOutliner : ISharpOutliner
    {
        public List<TagSpan<IOutliningRegionTag>> GetOutlineSpan(SyntaxNode node, ITextSnapshot textSnapshot,
            IdeServices ideServices)
        {
            var ret = new List<TagSpan<IOutliningRegionTag>>();
            var text = node.Parent.GetText();
            var isOnlineBlock = text.Lines.Count <= 2;
            if (isOnlineBlock)
            {
                return ret;
            }
            var hint = CollapsedHintCreator.GetHint(
                new SnapshotSpan(textSnapshot, node.Parent.FullSpan.Start, node.Parent.FullSpan.Length),
                ideServices);
            var span = new TagSpan<IOutliningRegionTag>(
                new SnapshotSpan(textSnapshot,
                    node.FullSpan.Start - 1,
                    GetSpanLength(node)),
                new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                    collapsedForm: "...", collapsedHintForm: hint));
            ret.Add(span);
            return ret;
        }

        private static int GetSpanLength(SyntaxNode node)
        {
            return node.SpanStart - node.FullSpan.Start + node.Span.Length + 1;
        }
    }
}