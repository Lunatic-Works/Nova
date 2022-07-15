using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nova.Script
{
    using AttributeDict = Dictionary<string, string>;

    public enum BlockType
    {
        LazyExecution,
        EagerExecution,
        Text,
        Separator
    }

    public class ParsedBlock
    {
        public BlockType type;
        public string content;
        public AttributeDict attributes;
    }

    public class ParsedScript
    {
        public IReadOnlyList<ParsedBlock> blocks;
    }

    public static class Parser
    {
        private static ParsedBlock ParseCodeBlock(Tokenizer tokenizer, BlockType type, AttributeDict attributes)
        {
            ParseException.ExpectToken(tokenizer.Peek(), TokenType.BlockStart, "<|");
            var startToken = tokenizer.Peek().Clone();
            tokenizer.ParseNext();
            var matchFound = false;
            int startIndex = tokenizer.Peek().index;
            int endIndex = startIndex;
            while (tokenizer.Peek().type != TokenType.EndOfFile)
            {
                var token = tokenizer.Peek();
                var tokenType = token.type;
                var tokenIndex = token.index;
                var tokenLength = token.length;
                if (tokenType == TokenType.CommentStart)
                {
                    tokenizer.AdvanceComment();
                    continue;
                }

                if (tokenType == TokenType.Quote)
                {
                    tokenizer.AdvanceQuoted();
                    continue;
                }

                tokenizer.ParseNext();

                if (tokenType == TokenType.BlockEnd)
                {
                    matchFound = true;
                    break;
                }

                endIndex = tokenIndex + tokenLength;
            }

            string content = tokenizer.SubString(startIndex, endIndex - startIndex);

            if (!matchFound)
            {
                throw new ParseException(startToken, "Unpaired block start <|");
            }

            tokenizer.SkipWhiteSpace();

            ParseException.ExpectToken(tokenizer.Peek(), TokenType.NewLine, TokenType.EndOfFile,
                "new line or end of file after |>");
            tokenizer.ParseNext();

            return new ParsedBlock
            {
                type = type,
                attributes = attributes,
                content = content
            };
        }

        private static ParsedBlock ParseEagerExecutionBlock(Tokenizer tokenizer)
        {
            var at = tokenizer.Peek();
            ParseException.ExpectToken(at, TokenType.At, "@");
            tokenizer.ParseNext();
            var token = tokenizer.Peek();
            if (token.type == TokenType.AttrStart)
            {
                return ParseCodeBlockWithAttributes(tokenizer, BlockType.EagerExecution);
            }

            if (token.type == TokenType.BlockStart)
            {
                return ParseCodeBlock(tokenizer, BlockType.EagerExecution, new AttributeDict());
            }

            throw new ParseException(token, $"Except [ or <| after @, found {token.type}");
        }

        private static string ExpectIdentifierOrString(Tokenizer tokenizer)
        {
            if (tokenizer.Peek().type == TokenType.Character)
            {
                int startIndex = tokenizer.Peek().index;
                tokenizer.AdvanceIdentifier();
                int endIndex = tokenizer.Peek().index;
                return tokenizer.SubString(startIndex, endIndex - startIndex);
            }

            if (tokenizer.Peek().type == TokenType.Quote)
            {
                int startIndex = tokenizer.Peek().index;
                tokenizer.AdvanceQuoted(false);
                int endIndex = tokenizer.Peek().index;
                return tokenizer.SubString(startIndex, endIndex - startIndex);
            }

            throw new ParseException(tokenizer.Peek(), $"Expect identifier or string, found {tokenizer.Peek().type}");
        }

        private static char EscapeChar(char c)
        {
            if (c == 'a') return '\a';
            if (c == 'b') return '\b';
            if (c == 'f') return '\f';
            if (c == 'n') return '\n';
            if (c == 'r') return '\r';
            if (c == 't') return '\t';
            if (c == 'v') return '\v';
            return c;
        }

        private static string EscapeString(string str)
        {
            var sb = new StringBuilder();
            var escaped = false;
            foreach (var c in str)
            {
                var nextEscaped = !escaped && c == '\\';
                if (escaped)
                {
                    sb.Append(EscapeChar(c));
                }
                else if (c != '\\')
                {
                    sb.Append(c);
                }

                escaped = nextEscaped;
            }

            return sb.ToString();
        }

        private static string UnQuote(string str)
        {
            if (str.Length == 0)
            {
                return str;
            }

            var first = str.First();
            if (str.Length >= 2 && (first == '\'' || first == '\"' && first == str.Last()))
            {
                return EscapeString(str.Substring(1, str.Length - 2));
            }

            return str;
        }

        private static ParsedBlock ParseCodeBlockWithAttributes(Tokenizer tokenizer, BlockType type)
        {
            ParseException.ExpectToken(tokenizer.Peek(), TokenType.AttrStart, "[");
            tokenizer.ParseNext();
            var attrs = new AttributeDict();

            while (tokenizer.Peek().type != TokenType.EndOfFile)
            {
                tokenizer.SkipWhiteSpace();
                var token = tokenizer.Peek();
                if (token.type == TokenType.AttrEnd)
                {
                    tokenizer.ParseNext();
                    break;
                }

                var key = ExpectIdentifierOrString(tokenizer);
                string value = null;

                tokenizer.SkipWhiteSpace();
                token = tokenizer.Peek();
                if (token.type == TokenType.Equal)
                {
                    tokenizer.ParseNext();
                    tokenizer.SkipWhiteSpace();
                    value = ExpectIdentifierOrString(tokenizer);
                }

                tokenizer.SkipWhiteSpace();
                token = tokenizer.Peek().Clone();
                if (token.type == TokenType.Comma || token.type == TokenType.AttrEnd)
                {
                    tokenizer.ParseNext();
                }
                else
                {
                    throw new ParseException(token, "Expect , or ]");
                }

                attrs.Add(UnQuote(key.Trim()), value == null ? null : UnQuote(value.Trim()));

                if (token.type == TokenType.AttrEnd)
                {
                    break;
                }
            }

            return ParseCodeBlock(tokenizer, type, attrs);
        }

        private static ParsedBlock ParseTextBlock(Tokenizer tokenizer, int startIndex)
        {
            while (tokenizer.Peek().type != TokenType.EndOfFile && tokenizer.Peek().type != TokenType.NewLine)
            {
                tokenizer.ParseNext();
            }

            int endIndex = tokenizer.Peek().index;
            string content = tokenizer.SubString(startIndex, endIndex - startIndex);

            // eat up the last newline
            tokenizer.ParseNext();

            return new ParsedBlock
            {
                type = BlockType.Text,
                content = content,
                attributes = new AttributeDict()
            };
        }

        private static ParsedBlock ParseBlock(Tokenizer tokenizer)
        {
            var token = tokenizer.Peek();
            int startIndex = token.index;
            while (token.type == TokenType.WhiteSpace)
            {
                tokenizer.ParseNext();
                token = tokenizer.Peek();
            }

            int endIndex = token.index;

            if (token.type == TokenType.NewLine || token.type == TokenType.EndOfFile)
            {
                string content = tokenizer.SubString(startIndex, endIndex - startIndex);
                tokenizer.ParseNext();
                return new ParsedBlock()
                {
                    type = BlockType.Separator,
                    content = content,
                    attributes = new AttributeDict()
                };
            }

            if (token.type == TokenType.At)
            {
                return ParseEagerExecutionBlock(tokenizer);
            }

            if (token.type == TokenType.AttrStart)
            {
                return ParseCodeBlockWithAttributes(tokenizer, BlockType.LazyExecution);
            }

            if (token.type == TokenType.BlockStart)
            {
                return ParseCodeBlock(tokenizer, BlockType.LazyExecution, new AttributeDict());
            }

            return ParseTextBlock(tokenizer, startIndex);
        }

        private static IReadOnlyList<ParsedBlock> MergeConsecutiveSeparators(IReadOnlyList<ParsedBlock> oldBlocks)
        {
            var blocks = new List<ParsedBlock>();

            blocks.Add(new ParsedBlock
            {
                type = BlockType.Separator
            });

            foreach (var block in oldBlocks)
            {
                if (block.type != BlockType.Separator || blocks.Last().type != BlockType.Separator)
                {
                    blocks.Add(block);
                }
            }

            blocks.RemoveAt(0);
            if (blocks.Count > 0 && blocks.Last().type == BlockType.Separator)
            {
                blocks.RemoveAt(blocks.Count - 1);
            }

            return blocks;
        }

        public static ParsedScript Parse(string text)
        {
            var tokenizer = new Tokenizer(text);
            var blocks = new List<ParsedBlock>();

            while (tokenizer.Peek().type != TokenType.EndOfFile)
            {
                blocks.Add(ParseBlock(tokenizer));
            }

            return new ParsedScript
            {
                blocks = MergeConsecutiveSeparators(blocks)
            };
        }
    }
}
