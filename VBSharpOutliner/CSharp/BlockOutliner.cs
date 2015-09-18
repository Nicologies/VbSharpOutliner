using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VBSharpOutliner.CSharp
{
    class BlockOutliner
    {
        public TagSpan<IOutliningRegionTag> GetOutlineSpan(SyntaxNode node, ITextSnapshot textSnapshot)
        {
            var text = node.Parent.GetText();
            var isOnlineBlock = text.Lines.Count <= 2;
            if (isOnlineBlock)
            {
                return null;
            }
            return new TagSpan<IOutliningRegionTag>(
                new SnapshotSpan(textSnapshot,
                    node.FullSpan.Start - 1,
                    GetSpanLength(node)),
                new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                    collapsedForm: "...", collapsedHintForm: text));
        }

        private static int GetSpanLength(SyntaxNode node)
        {
            return node.SpanStart - node.FullSpan.Start + node.Span.Length + 1;
        }
    }
}