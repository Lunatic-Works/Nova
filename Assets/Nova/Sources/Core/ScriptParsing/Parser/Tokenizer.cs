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
        private readonly Token next;

        public Tokenizer(string text)
        {
            this.text = text;
            line = 1;
            column = 1;
            index = 0;

            next = new Token();
            ParseNext();
        }

        public string SubString(int start, int length)
        {
            return text.Substring(start, length);
        }

        private void AdvanceString(int length)
        {
            for (var i = 0; i < length; i++)
            {
                // Advance counters
                index += 1;
                column += 1;
                if (index >= text.Length)
                    break;
                if (text[index] == '\n')
                {
                    column = 1;
                    line += 1;
                }
            }
        }

        private void AdvanceStringTill(char c)
        {
            while (text[index] != c)
            {
                // Advance counters
                index += 1;
                column += 1;
                if (index >= text.Length)
                    break;
                if (text[index] == '\n')
                {
                    column = 1;
                    line += 1;
                }
            }
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
                ParseNext();
            }
        }

        public void AdvanceIdentifier()
        {
            while (Peek().type == TokenType.Character)
            {
                ParseNext();
            }
        }

        private void AdvanceQuotedSingleLine()
        {
            var quoteChar = text[Peek().index];
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
                throw new ParserException(Peek(), "Unpaired quote");
            }

            AdvanceString(i + 1);
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
                throw new ParserException(Peek(), "Unpaired multiline string");
            }

            return len + 1;
        }

        private void AdvanceQuotedMultiline()
        {
            AdvanceString(TakeQuotedMultiline(0));
        }

        public void AdvanceQuoted(bool allowMultiline = true)
        {
            Assert.IsTrue(Peek().type == TokenType.Quote);
            var quoteChar = text[Peek().index];

            if (Peek().length == 1 && quoteChar == '\'' || quoteChar == '\"')
            {
                AdvanceQuotedSingleLine();
            }
            else if (allowMultiline && Peek().length == 2 && quoteChar == '[' && text[Peek().index + 1] == '[')
            {
                AdvanceQuotedMultiline();
            }
            else
            {
                throw new ParserException(Peek(), "Should not happen");
            }

            ParseNext();
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

        public void AdvanceComment()
        {
            Assert.IsTrue(Peek().type == TokenType.CommentStart);
            var blockCommentPattern = IsBlockComment();
            if (blockCommentPattern < 0)
            {
                AdvanceStringTill('\n');
                ParseNext();
                return;
            }

            var endPattern = BlockCommentEndPattern(blockCommentPattern);
            var endPatternIndex = text.IndexOf(endPattern, index, StringComparison.Ordinal);
            if (endPatternIndex == -1)
            {
                throw new ParserException(Peek(), "Unpaired block comment");
            }

            AdvanceString(endPatternIndex - index + endPattern.Length);
            ParseNext();
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

            // char.IsWhiteSpace('\n')
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
            char c2 = PeekChar(offset + 1);
            if (c == '[' && c2 == '[')
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

            if (c == '<' && c2 == '|')
            {
                type = TokenType.BlockStart;
                length = 2;
                return;
            }

            if (c == '|' && c2 == '>')
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

            if (c == '-' && c2 == '-')
            {
                type = TokenType.CommentStart;
                length = 2;
                return;
            }

            length = 1;
            type = TokenType.Character;
        }

        private void ParseNextImpl()
        {
            var tokenStartIndex = index;
            var tokenStartLine = line;
            var tokenStartColumn = column;

            PeekTokenType(out var tokenType, out var length);
            // in place
            next.index = tokenStartIndex;
            next.length = length;
            AdvanceString(length);
            next.column = tokenStartColumn;
            next.line = tokenStartLine;
            next.type = tokenType;
        }

        public void ParseNext()
        {
            ParseNextImpl();
        }

        /// <returns>null if no more tokens</returns>
        public Token Peek()
        {
            // Assert.IsNotNull(next);
            return next;
        }
    }
}
