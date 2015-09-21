using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VBSharpOutliner
{
    internal interface IOutlineParser
    {
        List<TagSpan<IOutliningRegionTag>> GetOutlineSpans(SyntaxNode node, ITextSnapshot textSnapshot,
            IdeServices ideServices);
    }
}