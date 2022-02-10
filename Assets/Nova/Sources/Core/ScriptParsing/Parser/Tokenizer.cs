using System;
using System.Text;
using UnityEngine.Assertions;

namespace Nova.Script
{
    public class Tokenizer
    {
        private readonly string text;

        private int column;
        private int line;
        private int index;
        private Token next;

        public Tokenizer(string text)
        {
            this.text = text;
            line = 1;
            column = 1;
            index = 0;
            ParseNext();
        }

        private string TakeString(int length)
        {
            var str = text.Substring(index, length);
            for (var i = 0; i < length; i++)
            {
                TakeChar();
            }

            return str;
        }

        private char TakeChar()
        {
            var c = PeekChar();
            if (c == '\0')
            {
                return c;
            }

            // Advance counters
            index += 1;
            column += 1;
            if (c == '\n')
            {
                column = 1;
                line += 1;
            }

            return c;
        }

        private char PeekChar(int offset = 0)
        {
            var idx = index + offset;
            if (idx >= text.Length)
            {
                return '\0';
            }

            return text[idx];
        }

        public void SkipWhiteSpace()
        {
            while (Peek().type == TokenType.WhiteSpace)
            {
                Take();
            }
        }

        public string TakeIdentifier()
        {
            var sb = new StringBuilder();
            while (Peek().type == TokenType.Character)
            {
                sb.Append(Take().text);
            }

            return sb.ToString();
        }

        private string TakeQuotedSingleLine()
        {
            var quoteChar = Peek().text[0];
            var escaped = false;
            var i = 0;
            for (; PeekChar(i) != '\0' && PeekChar(i) != '\n'; i++)
            {
                var c = PeekChar(i);
                if (!escaped && c == quoteChar)
                {
                    break;
                }

                escaped = c == '\\';
            }

            if (PeekChar(i) == '\0')
            {
                throw new ParseException(Peek(), "Unpaired quote");
            }

            return TakeString(i + 1);
        }

        private int TakeQuotedMultiline(int offset)
        {
            // Lua multiline string does not interpret escape sequence
            var len = 0;
            var lastIsRightSquareBracket = false;
            var lastIsLeftSquareBracket = false;
            for (; PeekChar(offset + len) != '\0'; len++)
            {
                var c = PeekChar(offset + len);
                if (lastIsLeftSquareBracket && c == '[')
                {
                    var nested = TakeQuotedMultiline(offset + len + 1);
                    len += nested;
                    c = PeekChar(offset + len);
                }
                else if (lastIsRightSquareBracket && c == ']')
                {
                    break;
                }

                lastIsLeftSquareBracket = c == '[';
                lastIsRightSquareBracket = c == ']';
            }

            if (!lastIsRightSquareBracket && PeekChar(offset + len) == ']')
            {
                throw new ParseException(Peek(), "Unpaired multiline string");
            }

            return len + 1;
        }

        private string TakeQuotedMultiline()
        {
            return TakeString(TakeQuotedMultiline(0));
        }

        public string TakeQuoted(bool allowMultiline = true)
        {
            Assert.IsTrue(Peek().type == TokenType.Quote);
            var quoteSeq = Peek().text;

            var str = "";
            if (quoteSeq == "'" || quoteSeq == "\"")
            {
                str = TakeQuotedSingleLine();
            }
            else if (allowMultiline && quoteSeq == "[[")
            {
                str = TakeQuotedMultiline();
            }
            else
            {
                throw new ParseException(Peek(), "Should not happen");
            }

            ParseNext();

            return quoteSeq + str;
        }

        private int IsBlockComment()
        {
            if (PeekChar() != '[')
            {
                return -1;
            }

            var i = 1;
            for (; PeekChar(i) == '='; i++) { }

            if (PeekChar(i) == '[')
            {
                return i - 1;
            }

            return -1;
        }

        private static string BlockCommentEndPattern(int num)
        {
            var sb = new StringBuilder();
            sb.Append(']');
            for (var i = 0; i < num; i++)
            {
                sb.Append('=');
            }

            sb.Append(']');
            return sb.ToString();
        }

        private string TakeStringAndParseNext(int len)
        {
            var str = TakeString(len);
            ParseNext();
            return str;
        }

        public string TakeComment()
        {
            Assert.IsTrue(Peek().type == TokenType.CommentStart);
            var commentStart = Peek().text;
            var blockCommentPattern = IsBlockComment();
            if (blockCommentPattern < 0)
            {
                var len = 0;
                for (; PeekChar(len) != '\n' && PeekChar(len) != '\0'; len++) { }

                return commentStart + TakeStringAndParseNext(len);
            }

            var endPattern = BlockCommentEndPattern(blockCommentPattern);
            var endPatternIndex = text.IndexOf(endPattern, index, StringComparison.Ordinal);
            if (endPatternIndex == -1)
            {
                throw new ParseException(Peek(), "Unpaired block comment");
            }

            return commentStart + TakeStringAndParseNext(endPatternIndex - index + endPattern.Length);
        }

        private void PeekTokenType(out TokenType type, out int length, int offset = 0)
        {
            var c = PeekChar(offset);

            if (c == '\0')
            {
                type = TokenType.EndOfFile;
                length = 0;
                return;
            }

            // char.IsWhiteSpace('\n') == true
            if (c == '\n')
            {
                type = TokenType.NewLine;
                length = 1;
                return;
            }

            if (char.IsWhiteSpace(c))
            {
                type = TokenType.WhiteSpace;
                length = 1;
                return;
            }

            if (c == '@')
            {
                type = TokenType.At;
                length = 1;
                return;
            }

            if (c == ',')
            {
                type = TokenType.Comma;
                length = 1;
                return;
            }

            if (c == '=')
            {
                type = TokenType.Equal;
                length = 1;
                return;
            }

            if (c == '\'' || c == '"')
            {
                type = TokenType.Quote;
                length = 1;
                return;
            }

            // Lua multiline text
            if (c == '[' && PeekChar(offset + 1) == '[')
            {
                type = TokenType.Quote;
                length = 2;
                return;
            }

            if (c == '[')
            {
                type = TokenType.AttrStart;
                length = 1;
                return;
            }

            if (c == ']')
            {
                type = TokenType.AttrEnd;
                length = 1;
                return;
            }

            if (c == '<' && PeekChar(offset + 1) == '|')
            {
                type = TokenType.BlockStart;
                length = 2;
                return;
            }

            if (c == '|' && PeekChar(offset + 1) == '>')
            {
                type = TokenType.BlockEnd;
                length = 2;
                return;
            }

            if (c == '{')
            {
                type = TokenType.LeftBrace;
                length = 1;
                return;
            }

            if (c == '}')
            {
                type = TokenType.RightBrace;
                length = 1;
                return;
            }

            if (c == '-' && PeekChar(offset + 1) == '-')
            {
                type = TokenType.CommentStart;
                length = 2;
                return;
            }

            length = 1;
            type = TokenType.Character;
        }

        private Token ParseNextImpl()
        {
            var tokenStartLine = line;
            var tokenStartColumn = column;

            Token Token(TokenType type, string s)
            {
                return new Token
                {
                    text = s,
                    column = tokenStartColumn,
                    line = tokenStartLine,
                    type = type
                };
            }

            PeekTokenType(out var tokenType, out var length);
            return Token(tokenType, TakeString(length));
        }

        private void ParseNext()
        {
            next = ParseNextImpl();
        }

        /// <returns>null if no more tokens</returns>
        public Token Take()
        {
            Assert.IsNotNull(next);
            var token = next;
            ParseNext();
            return token;
        }

        /// <returns>null if no more tokens</returns>
        public Token Peek()
        {
            Assert.IsNotNull(next);
            return next;
        }
    }
}
