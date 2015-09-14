using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
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

    internal class OutliningSpan
    {
        /// <summary>
        /// The span of text to collapse.
        /// </summary>
        public TextSpan TextSpan { get; }

        /// <summary>
        /// The span of text to display in the hint on mouse hover.
        /// </summary>
        public TextSpan HintSpan { get; }

        /// <summary>
        /// The text to display inside the collapsed region.
        /// </summary>
        public string BannerText { get; }

        /// <summary>
        /// Whether or not this region should be automatically collapsed when the 'Collapse to Definitions' command is invoked.
        /// </summary>
        public bool AutoCollapse { get; }

        /// <summary>
        /// Whether this region should be collapsed by default when a file is opened the first time.
        /// </summary>
        public bool IsDefaultCollapsed { get; }

        public OutliningSpan(TextSpan textSpan, TextSpan hintSpan, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            TextSpan = textSpan;
            BannerText = bannerText;
            HintSpan = hintSpan;
            AutoCollapse = autoCollapse;
            IsDefaultCollapsed = isDefaultCollapsed;
        }

        public OutliningSpan(TextSpan textSpan, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
            : this(textSpan, textSpan, bannerText, autoCollapse, isDefaultCollapsed)
        {
        }

        public override string ToString()
        {
            return this.TextSpan != this.HintSpan
                ? $"{{Span={TextSpan}, HintSpan={HintSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}"
                : $"{{Span={TextSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}";
        }
    }

    internal interface IOutliningService : ILanguageService
    {
        Task<IList<OutliningSpan>> GetOutliningSpansAsync(Document document, CancellationToken cancellationToken);
    }

    class OutliningService : IOutliningService
    {
        public OutliningService()
        {
            
        }
        public Task<IList<OutliningSpan>> GetOutliningSpansAsync(Document document, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class LanguageServices : HostLanguageServices
    {
        public LanguageServices()
        {
            
        }
        public override TLanguageService GetService<TLanguageService>()
        {
            throw new NotImplementedException();
        }

        public override HostWorkspaceServices WorkspaceServices { get; }

        public override string Language => LanguageNames.CSharp;
    }
}
