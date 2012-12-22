
namespace DotQLLanguage
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ITaggerProvider))]
    [ContentType("ook!")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class OokClassifierProvider : ITaggerProvider
    {

        [Export]
        [Name("ook!")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition OokContentType = null;

        [Export]
        [FileExtension(".dql")]
        [ContentType("ook!")]
        internal static FileExtensionToContentTypeDefinition OokFileType = null;

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {

            ITagAggregator<DotQLTokenTag> ookTagAggregator = 
                                            aggregatorFactory.CreateTagAggregator<DotQLTokenTag>(buffer);

            return new OokClassifier(buffer, ookTagAggregator, ClassificationTypeRegistry) as ITagger<T>;
        }
    }

    internal sealed class OokClassifier : ITagger<ClassificationTag>
    {
        ITextBuffer _buffer;
        ITagAggregator<DotQLTokenTag> _aggregator;
        IDictionary<DotQLTokenTypes, IClassificationType> _ookTypes;

        internal OokClassifier(ITextBuffer buffer, 
                               ITagAggregator<DotQLTokenTag> ookTagAggregator, 
                               IClassificationTypeRegistryService typeService)
        {
            _buffer = buffer;
            _aggregator = ookTagAggregator;
            _ookTypes = new Dictionary<DotQLTokenTypes, IClassificationType>();
            _ookTypes[DotQLTokenTypes.OokExclaimation] = typeService.GetClassificationType("ook!");
            _ookTypes[DotQLTokenTypes.OokPeriod] = typeService.GetClassificationType("ook.");
            _ookTypes[DotQLTokenTypes.OokQuestion] = typeService.GetClassificationType("ook?");
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {

            foreach (var tagSpan in this._aggregator.GetTags(spans))
            {
                var tagSpans = tagSpan.Span.GetSpans(spans[0].Snapshot);
                yield return 
                    new TagSpan<ClassificationTag>(tagSpans[0], 
                                                   new ClassificationTag(_ookTypes[tagSpan.Tag.type]));
            }
        }
    }
}
