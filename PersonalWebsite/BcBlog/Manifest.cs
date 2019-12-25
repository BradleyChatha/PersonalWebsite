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

    public class ManifestParser : IEnumerable<ManifestParser.Token>
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

        public struct Token
        {
            public TokenType Type;
            public string Value;

            public override string ToString()
            {
                return $"Type: {this.Type} | Value: {this.Value}";
            }
        }

        private readonly string _input;

        public ManifestParser(string input)
        {
            this._input = input;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            return new ManifestParserImpl(this._input);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    class ManifestParserImpl : IEnumerator<ManifestParser.Token>
    {
        #region Nested types

        [Flags]
        private enum Until
        {
            Whitespace = 1 << 0,
            Operator   = 1 << 1
        }
        #endregion

        #region Main variables
        private Dictionary<char, ManifestParser.TokenType> _operators = new Dictionary<char, ManifestParser.TokenType> 
        {
            { '[', ManifestParser.TokenType.LSquareBracket },
            { ']', ManifestParser.TokenType.RSquareBracket },
            { '{', ManifestParser.TokenType.LBracket },
            { '}', ManifestParser.TokenType.RBracket }
        };

        private Dictionary<string, ManifestParser.TokenType> _markers = new Dictionary<string, ManifestParser.TokenType>
        {
            { "@series", ManifestParser.TokenType.SeriesMarker }
        };

        private string                _input;
        private int                   _inputIndex;
        private ManifestParser.Token  _current;
        private char                  _currentChar => this._input[this._inputIndex];
        #endregion

        public ManifestParserImpl(string input)
        {
            this._input = input;
        }

        #region IEnumerator implementation
        public ManifestParser.Token Current
        {
            get => this._current;
            set => this._current = value;
        }

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            if(this._current.Type == ManifestParser.TokenType.EoF)
                return false;

            this.SkipWhitespace();
            if (this.IsEoF)
            {
                this._current = new ManifestParser.Token { Type = ManifestParser.TokenType.EoF };
                return true;
            }

            // I *could* just split these into functions, but is it really worth it when I'm the only one touching this code?
            // And only barely?
            if(this._currentChar == '@')
            {
                var slice = this.ReadUntil(Until.Whitespace | Until.Operator);

                // As much as I love that C# now has slices, the fact I can't even use them in LINQ is annoying as hell
                // This could've all been one line :(
                this._current.Type = ManifestParser.TokenType.ERROR;
                foreach(var key in this._markers.Keys)
                {
                    if(slice.SequenceEqual(key))
                    {
                        this._current = new ManifestParser.Token { Type = this._markers[key] };
                        return true;
                    }
                }

                if(this._current.Type == ManifestParser.TokenType.ERROR)
                    throw new Exception($"Unrecognised marker '{new string(slice)}'");
            }
            else if(this._operators.ContainsKey(this._currentChar))
            {
                this._current = new ManifestParser.Token { Type = this._operators[this._currentChar] };
                this.NextChar();
                return true;
            }
            else if(this._currentChar == '"')
            {
                this.NextChar(); // Skip first speech mark.

                var start = this._inputIndex;
                var count = 0;
                while(this._currentChar != '"')
                {
                    count++;
                    this.NextChar();

                    if (this.IsEoF)
                        throw new Exception("Unterminated string");
                }
                this.NextChar(); // Skip closing speech mark.

                this._current = new ManifestParser.Token { Type = ManifestParser.TokenType.String, Value = this._input.Substring(start, count) };
                return true;
            }
            else if(this._currentChar == '$') // Dates start with a dollar to make the parser's job easier
            {
                // Dates are in the form of DD-MM-YY-+0/+1/etc I really don't care about being accurate to the minute/second for this stuff.
                this.NextChar(); // Skip the dollar.
                
                var date       = this.ReadUntil(Until.Whitespace | Until.Operator);
                bool validDate = DateTimeOffset.TryParseExact(date, "dd-MM-yyyy-z", null, DateTimeStyles.AdjustToUniversal, out _);
                if(!validDate)
                    throw new Exception($"The date '{new string(date)}' is not valid.");

                this._current = new ManifestParser.Token { Type = ManifestParser.TokenType.Date, Value = new string(date) };
                return true;
            }
            else
            {
                var slice     = this.ReadUntil(Until.Whitespace | Until.Operator);
                this._current = new ManifestParser.Token { Type = ManifestParser.TokenType.Identifier, Value = new string(slice) };
                return true;
            }
            
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Parsing helpers
        void SkipWhitespace()
        {
            while(!this.IsEoF && Char.IsWhiteSpace(this._currentChar))
                this._inputIndex++;
        }

        void NextChar()
        {
            ++this._inputIndex;
        }

        ReadOnlySpan<char> ReadUntil(Until until)
        {
            var slice = this._input.AsSpan(this._inputIndex);
            var count = 0;
            while(count < slice.Length)
            {
                var sliceChar = slice[count];
                if((until & Until.Whitespace) == Until.Whitespace && Char.IsWhiteSpace(sliceChar)
                || (until & Until.Operator)   == Until.Operator   && this._operators.ContainsKey(sliceChar))
                    break;

                count++;
                this._inputIndex++;
            }

            return slice.Slice(0, count);
        }

        bool IsEoF => this._inputIndex >= this._input.Length;
        #endregion

        // Based off of VS's auto generated code.
        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.Current     = new ManifestParser.Token { Value = "Disposed", Type = ManifestParser.TokenType.ERROR };
                    this._input      = null;
                    this._inputIndex = int.MaxValue;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
