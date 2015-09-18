using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VBSharpOutliner.VisualBasic
{
    class VbOutlineParser : IOutlineParser
    {
        private static readonly List<SyntaxKind> VbBlocks = new List<SyntaxKind>()
        {
            SyntaxKind.MultiLineFunctionLambdaExpression,
            SyntaxKind.MultiLineIfBlock,
            SyntaxKind.ElseBlock,
            SyntaxKind.ElseIfBlock,

            SyntaxKind.SelectBlock,
            SyntaxKind.CaseElseBlock,
            SyntaxKind.CaseBlock,

            SyntaxKind.CatchBlock,
            SyntaxKind.TryBlock,
            SyntaxKind.FinallyBlock,

            SyntaxKind.DoLoopUntilBlock,
            SyntaxKind.DoUntilLoopBlock,
            SyntaxKind.DoLoopWhileBlock,
            SyntaxKind.DoWhileLoopBlock,
            SyntaxKind.SimpleDoLoopBlock,
            SyntaxKind.WhileBlock,
            SyntaxKind.ForBlock,
            SyntaxKind.ForEachBlock,

            SyntaxKind.OperatorBlock,
            SyntaxKind.PropertyBlock,
            SyntaxKind.RaiseEventAccessorBlock,
            SyntaxKind.RemoveHandlerAccessorBlock,
            SyntaxKind.AddHandlerAccessorBlock,

            SyntaxKind.SyncLockBlock,
            SyntaxKind.UsingBlock,
            SyntaxKind.WithBlock,
            SyntaxKind.ObjectCollectionInitializer,
            SyntaxKind.ObjectCreationExpression,
        };

        private static bool IsBlock(SyntaxNode node)
        {
            return VbBlocks.Any(node.IsKind);
        }

        public List<TagSpan<IOutliningRegionTag>> GetOutlineSpans(SyntaxNode node, ITextSnapshot textSnapshot)
        {
            var ret = new List<TagSpan<IOutliningRegionTag>>();
            var isBlock = IsBlock(node);
            if (!isBlock)
            {
                return ret;
            }
            // always has an extra empty line?
            var text = node.GetText();
            var isOnlineBlock = text.Lines.Count <= 2;
            if (isOnlineBlock)
            {
                return ret;
            }

            if (node.IsKind(SyntaxKind.MultiLineIfBlock))
            {
                var additionalSpan = AddAdditionalOutlinerForIfStatement(node, textSnapshot);
                if (additionalSpan != null)
                {
                    ret.Add(additionalSpan);
                }
            }
            var span = new TagSpan<IOutliningRegionTag>(
                new SnapshotSpan(textSnapshot, GetSpanStartPosition(node, text), GetSpanLength(node, text)),
                new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                    collapsedForm: "...", collapsedHintForm: text));
            ret.Add(span);
            return ret;
        }

        private static TagSpan<IOutliningRegionTag> AddAdditionalOutlinerForIfStatement(SyntaxNode node, 
            ITextSnapshot textSnapshot)
        {
            var multiLineIf = node as MultiLineIfBlockSyntax;
            Debug.Assert(multiLineIf != null, "multiLineIf != null");
            var hasElse = multiLineIf.ElseIfBlocks.Any() || multiLineIf.ElseBlock != null;
            if (!multiLineIf.Statements.Any())
            {
                return null;
            }
            var start = multiLineIf.Statements.Span.Start;
            var len = multiLineIf.Statements.Span.Length;

            var span = new TagSpan<IOutliningRegionTag>(
                new SnapshotSpan(textSnapshot, start, len),
                new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                    collapsedForm: "...", collapsedHintForm: multiLineIf.Statements.ToFullString()));
            return span;
        }
        private static int GetBlockStartPos(SyntaxNode node, SourceText text)
        {
            var chars = node.FullSpan.Start;
            foreach (var line in text.Lines)
            {
                chars += line.SpanIncludingLineBreak.Length;
                if (chars >= node.Span.Start)
                {
                    break;
                }
            }
            return chars - 1;
        }

        private static int GetSpanStartPosition(SyntaxNode node, SourceText text)
        {
            return GetBlockStartPos(node, text);
        }

        private static int GetSpanLength(SyntaxNode node, SourceText text)
        {
            var chars = GetBlockStartPos(node, text);
            return node.Span.End - chars; //node.FullSpan.Length - text.Lines.First().SpanIncludingLineBreak.Length;
        }
    }
}
