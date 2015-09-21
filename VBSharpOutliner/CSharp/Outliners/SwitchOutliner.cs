using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VBSharpOutliner.CSharp.Outliners
{
    class SwitchOutliner : ISharpOutliner
    {
        public List<TagSpan<IOutliningRegionTag>> GetOutlineSpan(SyntaxNode node, ITextSnapshot textSnapshot,
            IdeServices ideServices)
        {
            var ret = new List<TagSpan<IOutliningRegionTag>>();
            var switcher = node as SwitchStatementSyntax;
            if (switcher == null)
            {
                return ret;
            }
            var hint = CollapsedHintCreator.GetHint(
                new SnapshotSpan(textSnapshot, node.FullSpan.Start, node.FullSpan.Length),
                ideServices);
            var span = new TagSpan<IOutliningRegionTag>(
                new SnapshotSpan(textSnapshot,
                    switcher.OpenBraceToken.FullSpan.Start - 1, 
                    switcher.CloseBraceToken.FullSpan.End - switcher.OpenBraceToken.FullSpan.Start - 1),
                new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                    collapsedForm: "...",
                    collapsedHintForm: hint));
            ret.Add(span);
            return ret;
        }
    }
}
