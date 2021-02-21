using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.MarkdigExtentions
{
    public class TableOfContents : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var headingParser = pipeline.BlockParsers.Find<HeadingBlockParser>();
            if(headingParser != null)
            {
                headingParser.Closed -= OnHeadingBlockParsed;
                headingParser.Closed += OnHeadingBlockParsed;
            }
            pipeline.BlockParsers.AddIfNotAlready<TableOfContentsBlockParser>();
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            renderer.ObjectRenderers.InsertBefore<ListRenderer>(new TableOfContentsHtmlRenderer());
        }

        private void OnHeadingBlockParsed(BlockProcessor processor, Block block)
        {
            if(!(block is HeadingBlock headingBlock) || block is BlogMetadataBlock)
                return;

            if(headingBlock.Level < 2)
                return; // Ignore h1 since there's no point including it.
            
            var document = processor.Document;
            var toc = document.Where(b => b is TableOfContentsBlock).FirstOrDefault() as TableOfContentsBlock;
            if(toc == null)
                return;

            ContainerBlock parent = toc;
            for(int i = 0; i < headingBlock.Level - 2; i++) // 2 is the minimum level we support, hence -2
            {
                if(!(parent.LastChild is ContainerBlock childContainer))
                {
                    childContainer = new ListItemBlock(block.Parser);
                    parent.Add(childContainer);
                }
                parent = (ContainerBlock)parent.LastChild;
            }

            var headingCopy = new HeadingBlock(block.Parser)
            {
                Column = headingBlock.Column,
                HeaderChar = headingBlock.HeaderChar,
                Inline = headingBlock.Inline,
                IsBreakable = headingBlock.IsBreakable,
                IsOpen = headingBlock.IsOpen,
                Level = headingBlock.Level,
                Line = headingBlock.Line,
                ProcessInlines = headingBlock.ProcessInlines,
                RemoveAfterProcessInlines = headingBlock.RemoveAfterProcessInlines,
                Span = headingBlock.Span
            };
            headingCopy.Lines = new StringLineGroup(headingBlock.Lines.Lines.Length);
            headingCopy.SetAttributes(headingBlock.GetAttributes());
            foreach(var line in headingBlock.Lines.Lines)
            {
                if(line.Slice.Text == null)
                    continue;

                var textCopy = new StringSlice(line.Slice.Text, line.Slice.Start, line.Slice.End);
                var reffableLine = new StringLine(ref textCopy);
                headingCopy.Lines.Add(ref reffableLine);
            }
            parent.Add(headingCopy);
        }
    }

    public class TableOfContentsBlockParser : BlockParser
    {
        const string MARKER = "--toc";

        public TableOfContentsBlockParser()
        {
            this.OpeningCharacters = new[]{ MARKER[0] };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if(processor.IsCodeIndent)
                return BlockState.None;

            var line = processor.Line;

            if(!line.Match(MARKER))
                return BlockState.None;

            var block = new TableOfContentsBlock(this);
            block.Span.Start = line.Start;
            block.Span.End = line.End;
            processor.NewBlocks.Push(block);
            
            for(int i = 0; i < MARKER.Length; i++)
                processor.NextChar();

            return BlockState.Break;
        }
    }

    public class TableOfContentsBlock : ListBlock
    {
        public TableOfContentsBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class TableOfContentsHtmlRenderer : HtmlObjectRenderer<TableOfContentsBlock>
    {
        protected override void Write(HtmlRenderer renderer, TableOfContentsBlock obj)
        {
            this.Write(renderer, obj, 0);
        }

        void Write(HtmlRenderer renderer, Block block, int level)
        {
            renderer.EnsureLine();
            if(block is ContainerBlock container)
            {
                var attributes = new HtmlAttributes
                {
                    Classes = new List<string>() { "level", "_" + Convert.ToString(level) }
                };
                if(block is TableOfContentsBlock)
                    attributes.Classes.AddRange(new[] { "table", "of", "contents" });

                renderer.Write("<ol ");
                renderer.WriteAttributes(attributes);
                renderer.Write(">");

                var children = container.ToList();
                for(int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    var next  = (i == children.Count - 1) ? null : children[i+1];

                    renderer.EnsureLine();
                    renderer.Write("<li>");
                    this.Write(renderer, child, level + 1);
                    renderer.EnsureLine();

                    if(next is ContainerBlock)
                    {
                        i++;
                        this.Write(renderer, next, level + 1);
                    }
                    renderer.Write("</li>");
                }
                renderer.EnsureLine();
                renderer.Write("</ol>");
            }
            else if(block is HeadingBlock heading)
            {
                var id = heading.GetAttributes().Id;
                renderer.Write("<a ");
                renderer.WriteAttributes(new HtmlAttributes
                { 
                    Properties = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("href", "#" + id) }
                });
                renderer.Write(">");
                
                renderer.EnsureLine();
                renderer.WriteLeafInline(heading);
                renderer.EnsureLine();
                renderer.Write("</a>");
            }
            else
                throw new Exception($"Unknown table of contents block: {block.GetType()}");
        }
    }
}
