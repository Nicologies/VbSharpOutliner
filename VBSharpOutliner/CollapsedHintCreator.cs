using System.Windows;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.VisualStudio.Text;

namespace VBSharpOutliner
{
    static class CollapsedHintCreator
    {
        public static FrameworkElement GetHint(SnapshotSpan span, IdeServices ideServices)
        {
            var x = new ElisionBufferDeferredContent(span,
                ideServices.ProjectionBufferFactoryService, 
                ideServices.EditorOptionsFactoryService,
                ideServices.TextEditorFactoryService);
            return ideServices.UiDispatcher.Invoke(() => x.Create());
        }
    }
}
