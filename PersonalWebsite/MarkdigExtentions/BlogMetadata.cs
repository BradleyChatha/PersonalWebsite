using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.MarkdigExtentions
{
    public class BlogMetadata : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.InlineParsers.AddIfNotAlready<BlogMetadataParser>();
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }
    }

    public class BlogMetadataParser : InlineParser
    {
        public BlogMetadataParser()
        {
            OpeningCharacters = new[] { '@' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var startPosition = slice.Start;
            if (slice.CurrentChar == '@')
                slice.NextChar();

            slice.TrimStart();

            var valueStart = slice.Start;
            while (!slice.IsEmpty && !char.IsWhiteSpace(slice.CurrentChar))
                slice.NextChar();

            var key = slice.Text.Substring(valueStart, slice.Start - valueStart);

            valueStart = slice.Start;
            while (!slice.IsEmpty && slice.CurrentChar != '@')
                slice.NextChar();

            if(slice.CurrentChar == '@')
                slice.NextChar(); // Skip the @

            var value = slice.Text.Substring(valueStart, (slice.Start - valueStart) - 1);

            BlogMetadataBlock block;
            if(!(processor.BlockNew is BlogMetadataBlock))
            {
                block = new BlogMetadataBlock(processor.Block.Parser) // shh
                {
                    Span = new SourceSpan
                    {
                        Start = processor.GetSourcePosition(startPosition, out int line, out int column),
                        End = slice.Start
                    },
                    Line = line,
                    Column = column
                };

                processor.BlockNew = block;
            }
            else
                block = processor.BlockNew as BlogMetadataBlock;

            block.Entries.Add(new BlogMetadataEntry
            {
                Key = key,
                Value = value
            });

            return true;
        }
    }

    public class BlogMetadataBlock : LeafBlock
    {
        public IList<BlogMetadataEntry> Entries;

        public BlogMetadataBlock(BlockParser parser) : base(parser)
        {
            this.Entries = new List<BlogMetadataEntry>();
        }
    }

    public struct BlogMetadataEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
