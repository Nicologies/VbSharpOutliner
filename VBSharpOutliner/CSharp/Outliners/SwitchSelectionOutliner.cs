using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VBSharpOutliner.CSharp.Outliners 
{
    class SwitchSelectionOutliner : ISharpOutliner
    {
        public List<TagSpan<IOutliningRegionTag>> GetOutlineSpan(SyntaxNode node, ITextSnapshot textSnapshot)
        {
            var ret = new List<TagSpan<IOutliningRegionTag>>();
            var sectionSyntax = node as SwitchSectionSyntax;
            if (sectionSyntax == null)
            {
                return ret;
            }
            var span = new TagSpan<IOutliningRegionTag>(
                new SnapshotSpan(textSnapshot,
                    sectionSyntax.Statements.FullSpan.Start - 1,
                    sectionSyntax.Statements.FullSpan.Length - 1),
                new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                    collapsedForm: "...", collapsedHintForm: node.GetText()));
            ret.Add(span);
            return ret;
        }
    }
}
