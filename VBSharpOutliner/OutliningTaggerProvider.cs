using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace VBSharpOutliner
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    internal sealed class OutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //no outlining for projection buffers
            if (buffer is IProjectionBuffer) return null;

            return buffer.Properties.GetOrCreateSingletonProperty(() => new OutliningTagger(buffer) as ITagger<T>);
        }
    }
}
