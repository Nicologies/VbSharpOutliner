using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace VBSharpOutliner
{
    /// <summary>
    /// Services provided by the IDE
    /// </summary>
    internal class IdeServices
    {
        public readonly ITextEditorFactoryService TextEditorFactoryService;
        public readonly IEditorOptionsFactoryService EditorOptionsFactoryService;
        public readonly IProjectionBufferFactoryService ProjectionBufferFactoryService;
        public Dispatcher UiDispatcher;

        public IdeServices(ITextEditorFactoryService textEditorFactoryService, 
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService, 
            Dispatcher uiDispatcher)
        {
            TextEditorFactoryService = textEditorFactoryService;
            EditorOptionsFactoryService = editorOptionsFactoryService;
            ProjectionBufferFactoryService = projectionBufferFactoryService;
            UiDispatcher = uiDispatcher;
        }
    }
}