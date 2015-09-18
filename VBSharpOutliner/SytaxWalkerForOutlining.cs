using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using VBSharpOutliner.CSharp;
using VBSharpOutliner.VisualBasic;

namespace VBSharpOutliner
{
    internal class SytaxWalkerForOutlining : SyntaxWalker
    {
        private readonly CSharpOutlineParser _csharpOutlineParser = new CSharpOutlineParser();
        private readonly VbOutlineParser _vbOutlineParser = new VbOutlineParser();

        public SytaxWalkerForOutlining(ITextSnapshot snapshot)
        {
            _textSnapshot = snapshot;
        }

        public List<TagSpan<IOutliningRegionTag>> OutlineSpans { get; set; } = new List<TagSpan<IOutliningRegionTag>>();

        private readonly ITextSnapshot _textSnapshot;

        public override void Visit(SyntaxNode node)
        {
            base.Visit(node);
            try
            {
                var parser = GetOutlineParser(node);
                var spans = parser.GetOutlineSpans(node, _textSnapshot);
                OutlineSpans.AddRange(spans);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, node.GetText().ToString());
            }
        }

        private static bool IsVisualBasic(SyntaxNode node)
        {
            return node.Language == "Visual Basic";
        }

        private IOutlineParser GetOutlineParser(SyntaxNode node)
        {
            if (IsVisualBasic(node))
            {
                return _vbOutlineParser;
            }
            else
            {
                return _csharpOutlineParser;
            }
        }
    }
}