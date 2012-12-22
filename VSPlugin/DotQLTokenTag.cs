
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
    [TagType(typeof(DotQLTokenTag))]
    internal sealed class DotQLTokenTagProvider : ITaggerProvider
    {

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new DotQLTokenTagger(buffer) as ITagger<T>;
        }
    }

    public class DotQLTokenTag : ITag 
    {
        public DotQLTokenTypes type { get; private set; }

        public DotQLTokenTag(DotQLTokenTypes type)
        {
            this.type = type;
        }
    }

    internal sealed class DotQLTokenTagger : ITagger<DotQLTokenTag>
    {

        ITextBuffer _buffer;
        IDictionary<string, DotQLTokenTypes> _ookTypes;

        internal DotQLTokenTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _ookTypes = new Dictionary<string, DotQLTokenTypes>();
            _ookTypes["ook!"] = DotQLTokenTypes.OokExclaimation;
            _ookTypes["ook."] = DotQLTokenTypes.OokPeriod;
            _ookTypes["ook?"] = DotQLTokenTypes.OokQuestion;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<ITagSpan<DotQLTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {

            foreach (SnapshotSpan curSpan in spans)
            {
                ITextSnapshotLine containingLine = curSpan.Start.GetContainingLine();
                int curLoc = containingLine.Start.Position;
                string[] tokens = containingLine.GetText().ToLower().Split(' ');

                foreach (string ookToken in tokens)
                {
                    if (_ookTypes.ContainsKey(ookToken))
                    {
                        var tokenSpan = new SnapshotSpan(curSpan.Snapshot, new Span(curLoc, ookToken.Length));
                        if( tokenSpan.IntersectsWith(curSpan) ) 
                            yield return new TagSpan<DotQLTokenTag>(tokenSpan, 
                                                                  new DotQLTokenTag(_ookTypes[ookToken]));
                    }

                    //add an extra char location because of the space
                    curLoc += ookToken.Length + 1;
                }
            }
            
        }
    }
}
