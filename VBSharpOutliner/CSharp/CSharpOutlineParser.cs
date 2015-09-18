using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VBSharpOutliner.CSharp
{
    class CSharpOutlineParser : IOutlineParser
    {
        private readonly BlockOutliner _blockOutliner = new BlockOutliner();
        public List<TagSpan<IOutliningRegionTag>> GetOutlineSpans(SyntaxNode node, ITextSnapshot textSnapshot)
        {
            var ret = new List<TagSpan<IOutliningRegionTag>>();
            var isBlock = IsBlock(node);
            if (!isBlock)
            {
                return ret;
            }
            var span = _blockOutliner.GetOutlineSpan(node, textSnapshot);
            if (span != null)
            {
                ret.Add(span);
            }
            return ret;
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
    }
}
