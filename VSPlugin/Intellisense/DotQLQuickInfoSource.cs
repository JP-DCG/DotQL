﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace DotQLLanguage
{
    
    [Export(typeof(IQuickInfoSourceProvider))]
    [ContentType("ook!")]
    [Name("ookQuickInfo")]
    class OokQuickInfoSourceProvider : IQuickInfoSourceProvider
    {

        [Import]
        IBufferTagAggregatorFactoryService aggService = null;

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new OokQuickInfoSource(textBuffer, aggService.CreateTagAggregator<DotQLTokenTag>(textBuffer));
        }
    }

    class OokQuickInfoSource : IQuickInfoSource
    {
        private ITagAggregator<DotQLTokenTag> _aggregator;
        private ITextBuffer _buffer;
        private bool _disposed = false;


        public OokQuickInfoSource(ITextBuffer buffer, ITagAggregator<DotQLTokenTag> aggregator)
        {
            _aggregator = aggregator;
            _buffer = buffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (_disposed)
                throw new ObjectDisposedException("TestQuickInfoSource");

            var triggerPoint = (SnapshotPoint) session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (triggerPoint == null)
                return;

            foreach (IMappingTagSpan<DotQLTokenTag> curTag in _aggregator.GetTags(new SnapshotSpan(triggerPoint, triggerPoint)))
            {
                if (curTag.Tag.type == DotQLTokenTypes.OokExclaimation)
                {
                    var tagSpan = curTag.Span.GetSpans(_buffer).First();
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
                    quickInfoContent.Add("Exclaimed Ook!");
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}

