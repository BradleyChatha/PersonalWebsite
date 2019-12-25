using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.BcBlog
{
    public struct BcToken<TokenTypeT>
    {
        public TokenTypeT Type;
        public string Value;

        public override string ToString()
        {
            return $"Type: {this.Type} | Value: {this.Value}";
        }
    }

    internal abstract class BcParser<TokenTypeT> : IEnumerator<BcToken<TokenTypeT>>
        where TokenTypeT : struct, IConvertible // enum
    {
        #region Nested types
        [Flags]
        protected enum Until
        {
            Whitespace = 1 << 0,
            Operator = 1 << 1
        }
        #endregion

        #region Constants
        protected const char   DATE_PREFIX = '$';
        internal  const string DATE_FORMAT = "dd-MM-yyyy-z";
        #endregion

        #region Main variables
        protected Dictionary<char, TokenTypeT>   Operators { get; private set; }
        protected Dictionary<string, TokenTypeT> Markers   { get; private set; }

        private string _input;
        private int    _inputIndex;

        protected BcToken<TokenTypeT> CurrentToken;

        protected char CurrentChar => this._input[this._inputIndex];
        #endregion

        public BcParser(
            string                          input, 
            Dictionary<char, TokenTypeT>    operators, 
            Dictionary<string, TokenTypeT>  markers
        )
        {
            this._input    = input;
            this.Operators = operators;
            this.Markers   = markers;
        }

        #region IEnumerator implementation
        public BcToken<TokenTypeT> Current
        {
            get => this.CurrentToken;
            set => this.CurrentToken = value;
        }

        object IEnumerator.Current => this.Current;

        public abstract bool MoveNext();

        public void Reset()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Parsing helpers
        protected bool IsEoF => this._inputIndex >= this._input.Length;

        protected void SkipWhitespace()
        {
            while (!this.IsEoF && Char.IsWhiteSpace(this.CurrentChar))
                this._inputIndex++;
        }

        protected void NextChar()
        {
            ++this._inputIndex;
        }

        protected ReadOnlySpan<char> ReadUntil(Until until)
        {
            var slice = this._input.AsSpan(this._inputIndex);
            var count = 0;
            while (count < slice.Length)
            {
                var sliceChar = slice[count];
                if ((until & Until.Whitespace) == Until.Whitespace && Char.IsWhiteSpace(sliceChar)
                || (until & Until.Operator) == Until.Operator && this.Operators.ContainsKey(sliceChar))
                    break;

                count++;
                this._inputIndex++;
            }

            return slice.Slice(0, count);
        }

        protected bool NextString()
        {
            this.NextChar(); // Skip first speech mark.

            var start = this._inputIndex;
            var count = 0;
            while (this.CurrentChar != '"')
            {
                count++;
                this.NextChar();

                if (this.IsEoF)
                    throw new Exception("Unterminated string");
            }
            this.NextChar(); // Skip closing speech mark.

            this.CurrentToken = new BcToken<TokenTypeT>
            {
                Type  = Enum.Parse<TokenTypeT>("String"), // yuck. But again, only I'm using this, so only I suffer.
                Value = this._input.Substring(start, count)
            };

            return true;
        }

        protected bool NextDate()
        {
            // Dates are in the form of DD-MM-YY-+0/+1/etc I really don't care about being accurate to the minute/second for this stuff.
            this.NextChar(); // Skip the dollar.

            var  date      = this.ReadUntil(Until.Whitespace | Until.Operator);
            bool validDate = DateTimeOffset.TryParseExact(date, DATE_FORMAT, null, DateTimeStyles.AdjustToUniversal, out _);
            if (!validDate)
                throw new Exception($"The date '{new string(date)}' is not valid.");

            this.CurrentToken = new BcToken<TokenTypeT>
            {
                Type  = Enum.Parse<TokenTypeT>("Date"), // yuck again.
                Value = new string(date)
            };

            return true;
        }
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
                    this.Current = new BcToken<TokenTypeT> { Value = "Disposed" }; // Type should default to the first one, which is ERROR.
                    this._input = null;
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
