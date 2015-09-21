using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace VBSharpOutliner
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    internal partial class OutliningTaggerProvider : ITaggerProvider
    {
        private readonly IdeServices _ideServices;

        [ImportingConstructor]
        public OutliningTaggerProvider(
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService)
        {
            _ideServices = new IdeServices(textEditorFactoryService, 
                editorOptionsFactoryService, projectionBufferFactoryService, Dispatcher.CurrentDispatcher);
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //no outlining for projection buffers
            if (buffer is IProjectionBuffer) return null;

            return buffer.Properties.GetOrCreateSingletonProperty(
                () => new OutliningTagger(buffer, _ideServices) as ITagger<T>);
        }
    }
}
