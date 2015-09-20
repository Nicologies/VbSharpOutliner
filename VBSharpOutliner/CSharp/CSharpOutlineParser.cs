using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using VBSharpOutliner.CSharp.Outliners;

namespace VBSharpOutliner.CSharp
{
    internal class CSharpOutlineParser : IOutlineParser
    {
        private readonly BlockOutliner _blockOutliner = new BlockOutliner();
        private readonly SwitchOutliner _switchOutliner = new SwitchOutliner();
        private readonly SwitchSelectionOutliner _switchSelectionOutliner = new SwitchSelectionOutliner();

        public List<TagSpan<IOutliningRegionTag>> GetOutlineSpans(SyntaxNode node, ITextSnapshot textSnapshot)
        {
            var outliner = GetOutliner(node);
            return outliner == null ? new List<TagSpan<IOutliningRegionTag>>() : outliner.GetOutlineSpan(node, textSnapshot);
        }

        private static bool IsBlock(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.Block))
            {
                return true;
            }
            if (node.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                return true;
            }
            if (node.IsKind(SyntaxKind.CollectionInitializerExpression))
            {
                return true;
            }
            return false;
        }

        private ISharpOutliner GetOutliner(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.SwitchStatement))
            {
                return _switchOutliner;
            }

            if (node.IsKind(SyntaxKind.SwitchSection))
            {
                return _switchSelectionOutliner;
            }

            var isBlock = IsBlock(node);
            if (!isBlock)
            {
                return null;
            }
            return _blockOutliner;
        }
    }
}
