using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.BcBlog
{
    public class Manifest
    {
        public IEnumerable<BlogSeries> Series { get; set; }
    }

    public class BlogSeries
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset DateReleased { get; set; }
        public DateTimeOffset DateUpdated { get; set; }
        public IEnumerable<string> Posts { get; set; }
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
                  new Dictionary<char, ManifestParser.TokenType>
                  {
                      { '[', ManifestParser.TokenType.LSquareBracket },
                      { ']', ManifestParser.TokenType.RSquareBracket },
                      { '{', ManifestParser.TokenType.LBracket },
                      { '}', ManifestParser.TokenType.RBracket }
                  },
                  new Dictionary<string, ManifestParser.TokenType>
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
