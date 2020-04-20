using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.MarkdigExtentions
{
    /// <summary>
    /// Automatically converts text like "std.json" into links towards phobos' documentation. 
    /// </summary>
    /// <remarks>
    /// std.json -> [std.json](https://dlang.org/phobos/std_json.html)
    /// 
    /// std.json:json -> [json](https://dlang.org/phobos/std_json.html)
    /// 
    /// std.json#JSONValue -> [JSONValue](https://dlang.org/phobos/std_json.html#JSONValue)
    /// 
    /// std.json#JSONValue:json -> [json](https://dlang.org/phobos/std_json.html#JSONValue)
    /// </remarks>
    public class AutoStdLink : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if(!pipeline.InlineParsers.Contains<AutoStdLinkParser>())
                pipeline.InlineParsers.Insert(pipeline.InlineParsers.Count, new AutoStdLinkParser());
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }
    }

    public static class PipelineBuilderExtensions
    {
        public static MarkdownPipelineBuilder UseAutoStdLink(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<AutoStdLink>();
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseBlogMetadata(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<BlogMetadata>();
            return pipeline;
        }
    }

    public class AutoStdLinkParser : InlineParser
    {
        public AutoStdLinkParser()
        {
            base.OpeningCharacters = new char[] 
            {
                's' // Start of 'std'
            };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var previousChar = slice.PeekCharExtra(-1);
            if (!previousChar.IsWhiteSpaceOrZero())
                return false;

            if(!slice.MatchLowercase("std."))
                return false;

            var startPosition = slice.Start;
            var endOffset     = 0;
            while(slice.Start < slice.End && !Char.IsWhiteSpace(slice.CurrentChar) && slice.CurrentChar != ';')
                slice.NextChar();

            if(slice.CurrentChar == ';')
                endOffset += 1;

            var text     = slice.Text.Substring(startPosition, (slice.Start - startPosition) + 1).TrimEnd(' ', '\n', ';', '\t', '\r');
            var sections = text.Split('#', ':');
            if(sections.Length == 0 || sections.Length > 3)
                return false;

            string module      = null;
            string member      = null;
            string displayText = null;
            switch(sections.Length)
            {
                case 1:
                    module      = sections[0];
                    displayText = module;
                    break;

                case 2:
                    module = sections[0];

                    if(text.Contains("#"))
                        member = sections[1];
                    else // :
                        displayText = sections[1];
                    break;

                case 3:
                    module      = sections[0];
                    member      = sections[1];
                    displayText = sections[2];
                    break;

                default: break;
            }

            var inlineLink = new LinkInline 
            {
                Span =
                {
                    Start = processor.GetSourcePosition(startPosition, out int line, out int column),
                },
                Line       = line,
                Column     = column,
                IsClosed   = true,
                IsAutoLink = true,
                Url        = this.CreateUrlToDlangDocumentation("phobos", module, member)
            };

            inlineLink.Span.End = startPosition + text.Length + endOffset;
            inlineLink.UrlSpan  = inlineLink.Span;
            inlineLink.AppendChild(new LiteralInline
            {
                Span     = inlineLink.Span,
                Line     = inlineLink.Line,
                Column   = inlineLink.Column,
                Content  = new StringSlice(displayText ?? member ?? module),
                IsClosed = true
            });

            slice.Start = inlineLink.Span.End;

            processor.Inline = inlineLink;
            return true;
        }

        private string CreateUrlToDlangDocumentation(string library, string module, string member)
        {
            return $"https://dlang.org/{library}/{module.Replace('.', '_')}.html#.{member ?? ""}";
        }
    }
}
