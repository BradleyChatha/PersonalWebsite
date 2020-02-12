using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalWebsite.BcBlog
{
    public class Manifest
    {
        #region Nested types
        enum PopToken
        {
            No,
            Yes
        }
        #endregion

        public ICollection<BlogSeries> Series { get; set; }

        public Manifest(ManifestParser parser)
        {
            this.Series = new List<BlogSeries>();
            this.FromTokens(parser.GetEnumerator());
        }

        #region From Tokens
        void FromTokens(IEnumerator<BcToken<ManifestParser.TokenType>> tokens)
        {
            // Get the first token if it hasn't done so already.
            if(tokens.Current.Type == ManifestParser.TokenType.ERROR)
                tokens.MoveNext();

            switch(tokens.Current.Type)
            {
                case ManifestParser.TokenType.SeriesMarker:
                    this.HandleSeries(tokens);
                    break;

                case ManifestParser.TokenType.EoF:
                    return;

                default:
                    throw new Exception($"Unexpected token [{tokens.Current}] while parsing top-level.");
            }

            this.FromTokens(tokens); // Cleaner than a while(true) wrapped around. Maybe the compiler will tail-call anyway :3
        }

        void HandleSeries(IEnumerator<BcToken<ManifestParser.TokenType>> tokens)
        {
            this.AssertNextToken(tokens, ManifestParser.TokenType.SeriesMarker, PopToken.Yes);
            this.AssertNextToken(tokens, ManifestParser.TokenType.LBracket,     PopToken.Yes);

            var series = new BlogSeries();
            while(true)
            {
                if(tokens.Current.Type == ManifestParser.TokenType.RBracket)
                {
                    tokens.MoveNext();
                    break;
                }

                if(tokens.Current.Type == ManifestParser.TokenType.LSquareBracket)
                {
                    tokens.MoveNext();

                    var name = tokens.Current.Value;
                    this.AssertNextToken(tokens, ManifestParser.TokenType.Identifier, PopToken.Yes);

                    switch(name)
                    {
                        case "name":
                            var value = tokens.Current.Value;
                            this.AssertNextToken(tokens, ManifestParser.TokenType.String, PopToken.Yes);

                            series.Name = value;
                            break;

                        case "description":
                            var builder = new StringBuilder(128);

                            while(true)
                            {
                                if(tokens.Current.Type == ManifestParser.TokenType.RSquareBracket)
                                    break;

                                if(builder.Length > 0)
                                    builder.Append(" ");

                                builder.Append(tokens.Current.Value);
                                this.AssertNextToken(tokens, ManifestParser.TokenType.String, PopToken.Yes);
                            }

                            series.Description = builder.ToString();
                            break;

                        case "date-released":
                            var date = tokens.Current.Value;
                            this.AssertNextToken(tokens, ManifestParser.TokenType.Date, PopToken.Yes);
                            break;

                        case "date-updated":
                            date = tokens.Current.Value;
                            this.AssertNextToken(tokens, ManifestParser.TokenType.Date, PopToken.Yes);
                            break;

                        case "reference":
                            var reference = tokens.Current.Value;
                            this.AssertNextToken(tokens, ManifestParser.TokenType.String, PopToken.Yes);

                            series.Reference = reference;
                            break;

                        case "post":
                            var post = tokens.Current.Value;
                            this.AssertNextToken(tokens, ManifestParser.TokenType.String, PopToken.Yes);

                            series.PostFilePaths.Add(post);
                            break;

                        default:
                            throw new Exception($"Unknown entry called '{name}' while parsing a series entry.");
                    }

                    this.AssertNextToken(tokens, ManifestParser.TokenType.RSquareBracket, PopToken.Yes);
                }
                else
                    throw new Exception($"Unexpected token [{tokens.Current}] when parsing a series entry.");
            }

            this.Series.Add(series);
        }

        void AssertNextToken(
            IEnumerator<BcToken<ManifestParser.TokenType>> tokens, 
            ManifestParser.TokenType                       expectedType, 
            PopToken                                       pop
        )
        {
            if(tokens.Current.Type != expectedType)
                throw new Exception($"Expected token of type '{expectedType}' not [{tokens.Current}]");

            if(pop == PopToken.Yes)
                tokens.MoveNext();
        }
        #endregion
    }

    public class BlogSeries
    {
        public string Reference { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<string> PostFilePaths { get; set; }

        public BlogSeries()
        {
            this.PostFilePaths = new List<string>();
        }
    }

    public class ManifestParser : IEnumerable<BcToken<ManifestParser.TokenType>>
    {
        public enum TokenType
        {
            ERROR,

            SeriesMarker,

            LBracket,
            RBracket,
            LSquareBracket,
            RSquareBracket,

            Date,
            String,
            Identifier,

            EoF
        }

        private readonly string _input;

        public ManifestParser(string input)
        {
            this._input = input;
        }

        public IEnumerator<BcToken<TokenType>> GetEnumerator()
        {
            return new ManifestParserImpl(this._input);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class ManifestParserImpl : BcParser<ManifestParser.TokenType>
    {
        public ManifestParserImpl(string input) 
            : base(
                  input, 
                  operators: new Dictionary<char, ManifestParser.TokenType>
                  {
                      { '[', ManifestParser.TokenType.LSquareBracket },
                      { ']', ManifestParser.TokenType.RSquareBracket },
                      { '{', ManifestParser.TokenType.LBracket       },
                      { '}', ManifestParser.TokenType.RBracket       }
                  },
                  markers: new Dictionary<string, ManifestParser.TokenType>
                  {
                      { "@series", ManifestParser.TokenType.SeriesMarker }
                  }
            )
        {
        }

        public override bool MoveNext()
        {
            if (base.CurrentToken.Type == ManifestParser.TokenType.EoF)
                return false;

            base.SkipWhitespace();
            if (base.IsEoF)
            {
                base.CurrentToken = new BcToken<ManifestParser.TokenType> { Type = ManifestParser.TokenType.EoF };
                return true;
            }

            if (base.CurrentChar == '@')
            {
                var slice = base.ReadUntil(Until.Whitespace | Until.Operator);

                // As much as I love that C# now has slices, the fact I can't even use them in LINQ is annoying as hell
                // This could've all been one line :(
                base.CurrentToken.Type = ManifestParser.TokenType.ERROR;
                foreach (var key in base.Markers.Keys)
                {
                    if (slice.SequenceEqual(key))
                    {
                        base.CurrentToken = new BcToken<ManifestParser.TokenType> { Type = base.Markers[key] };
                        return true;
                    }
                }

                if (base.CurrentToken.Type == ManifestParser.TokenType.ERROR)
                    throw new Exception($"Unrecognised marker '{new string(slice)}'");
            }
            else if (base.Operators.ContainsKey(base.CurrentChar))
            {
                base.CurrentToken = new BcToken<ManifestParser.TokenType> { Type = base.Operators[base.CurrentChar] };
                base.NextChar();
                return true;
            }
            else if (base.CurrentChar == '"')
                return base.NextString();
            else if (base.CurrentChar == /*base.*/DATE_PREFIX)
                return base.NextDate();
            else
            {
                var slice = base.ReadUntil(Until.Whitespace | Until.Operator);
                base.CurrentToken = new BcToken<ManifestParser.TokenType> 
                { 
                    Type  = ManifestParser.TokenType.Identifier, 
                    Value = new string(slice) 
                };
                return true;
            }

            throw new NotImplementedException();
        }
    }
}
