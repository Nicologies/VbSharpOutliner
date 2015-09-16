using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace VBSharpOutliner
{
    class SytaxWalkerForOutlining : SyntaxWalker
    {
        public SytaxWalkerForOutlining(ITextSnapshot snapshot)
        {
            _textSnapshot = snapshot;
        }

        public List<TagSpan<IOutliningRegionTag>> OutlineSpans { get; set; } = new List<TagSpan<IOutliningRegionTag>>(); 

        private readonly ITextSnapshot _textSnapshot;

        public override void Visit(SyntaxNode node)
        {
            base.Visit(node);
            var isBlock = IsBlock(node);
            if (!isBlock)
            {
                return;
            }
            var text = IsVisualBasic(node)? node.GetText() : node.Parent.GetText();
            // always has an extra empty line?
            var isOnlineBlock = text.Lines.Count <= 2;
            if (isOnlineBlock)
            {
                return;
            }
            try
            {
                if (node.IsKind(VBSyntaxKind.MultiLineIfBlock))
                {
                    AddAdditionalOutlinerForIfStatement(node);
                }
                var span = new TagSpan<IOutliningRegionTag>(
                    new SnapshotSpan(_textSnapshot,
                        GetSpanStartPosition(node, text),
                        GetSpanLength(node, text)),
                    new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                        collapsedForm: "...", collapsedHintForm: text));
                OutlineSpans.Add(span);
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex, text.ToString());
            }
        }

        private void AddAdditionalOutlinerForIfStatement(SyntaxNode node)
        {
            var multiLineIf = node as MultiLineIfBlockSyntax;
            Debug.Assert(multiLineIf != null, "multiLineIf != null");
            var hasElse = multiLineIf.ElseIfBlocks.Any() || multiLineIf.ElseBlock != null;
            if (!multiLineIf.Statements.Any())
            {
                return;
            }
            var start = multiLineIf.Statements.Span.Start;
            var len = multiLineIf.Statements.Span.Length;

            var span = new TagSpan<IOutliningRegionTag>(
                    new SnapshotSpan(_textSnapshot, start, len),
                    new OutliningRegionTag(isDefaultCollapsed: false, isImplementation: true,
                        collapsedForm: "...", collapsedHintForm: multiLineIf.Statements.ToFullString()));
            OutlineSpans.Add(span);
        }

        private static int GetSpanLength(SyntaxNode node, SourceText text)
        {
            if (IsVisualBasic(node))
            {
                var chars = GetBlockStartPos(node,text);
                return node.Span.End - chars ;//node.FullSpan.Length - text.Lines.First().SpanIncludingLineBreak.Length;
            }
            else
            {
                return node.SpanStart - node.FullSpan.Start + node.Span.Length + 1;
            }
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
            if (IsVisualBasic(node))
            {
                return GetBlockStartPos(node, text);
            }
            else
            {
                return node.FullSpan.Start - 1;
            }
        }

        private static readonly List<VBSyntaxKind> VbBlocks = new List<VBSyntaxKind>()
        {
            VBSyntaxKind.MultiLineFunctionLambdaExpression,
            VBSyntaxKind.MultiLineIfBlock,
            VBSyntaxKind.ElseBlock,
            VBSyntaxKind.ElseIfBlock,

            VBSyntaxKind.SelectBlock,
            VBSyntaxKind.CaseElseBlock,
            VBSyntaxKind.CaseBlock,

            VBSyntaxKind.CatchBlock,
            VBSyntaxKind.TryBlock,
            VBSyntaxKind.FinallyBlock,
            
            VBSyntaxKind.DoLoopUntilBlock,
            VBSyntaxKind.DoUntilLoopBlock,
            VBSyntaxKind.DoLoopWhileBlock,
            VBSyntaxKind.DoWhileLoopBlock,
            VBSyntaxKind.SimpleDoLoopBlock,
            VBSyntaxKind.WhileBlock,
            VBSyntaxKind.ForBlock,
            VBSyntaxKind.ForEachBlock,

            VBSyntaxKind.OperatorBlock,
            VBSyntaxKind.PropertyBlock,
            VBSyntaxKind.RaiseEventAccessorBlock,
            VBSyntaxKind.RemoveHandlerAccessorBlock,
            VBSyntaxKind.AddHandlerAccessorBlock,

            VBSyntaxKind.SyncLockBlock,
            VBSyntaxKind.UsingBlock,
            VBSyntaxKind.WithBlock,
            VBSyntaxKind.ObjectCollectionInitializer,
            VBSyntaxKind.ObjectCreationExpression,
        };

        private static bool IsBlock(SyntaxNode node)
        {
            if (!IsVisualBasic(node))
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
            else
            {
                return VbBlocks.Any(node.IsKind);
            }
        }

        private static bool IsVisualBasic(SyntaxNode node)
        {
            return node.Language == "Visual Basic";
        }
    }
}