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
            var startToken = tokenizer.Take();
            var sb = new StringBuilder();
            var matchFound = false;
            while (tokenizer.Peek().type != TokenType.EndOfFile)
            {
                var token = tokenizer.Peek();
                if (token.type == TokenType.CommentStart)
                {
                    sb.Append(tokenizer.TakeComment());
                    continue;
                }

                if (token.type == TokenType.Quote)
                {
                    sb.Append(tokenizer.TakeQuoted());
                    continue;
                }

                tokenizer.Take();

                if (token.type == TokenType.BlockEnd)
                {
                    matchFound = true;
                    break;
                }

                sb.Append(token.text);
            }

            if (!matchFound)
            {
                throw new ParseException(startToken, "Unpaired block start <|");
            }

            tokenizer.SkipWhiteSpace();

            ParseException.ExpectToken(tokenizer.Peek(), new[]
            {
                TokenType.NewLine, TokenType.EndOfFile
            }, "new line or end of file after |>");
            tokenizer.Take();

            return new ParsedBlock
            {
                type = type,
                attributes = attributes,
                content = sb.ToString()
            };
        }

        private static ParsedBlock ParseEagerExecutionBlock(Tokenizer tokenizer)
        {
            var at = tokenizer.Take();
            ParseException.ExpectToken(at, TokenType.At, "@");
            var token = tokenizer.Peek();
            if (token.type == TokenType.AttrStart)
            {
                return ParseCodeBlockWithAttributes(tokenizer, BlockType.EagerExecution);
            }

            if (token.type == TokenType.BlockStart)
            {
                return ParseCodeBlock(tokenizer, BlockType.EagerExecution, new AttributeDict());
            }

            throw new ParseException(token, $"Except [ or <| after @, found {token.text}");
        }

        private static string ExpectIdentifierOrString(Tokenizer tokenizer)
        {
            if (tokenizer.Peek().type == TokenType.Character)
            {
                return tokenizer.TakeIdentifier();
            }

            if (tokenizer.Peek().type == TokenType.Quote)
            {
                return tokenizer.TakeQuoted(false);
            }

            throw new ParseException(tokenizer.Peek(), $"Expect identifier or string, found {tokenizer.Peek().text}");
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
            tokenizer.Take();
            var attrs = new AttributeDict();

            while (tokenizer.Peek().type != TokenType.EndOfFile)
            {
                tokenizer.SkipWhiteSpace();
                var token = tokenizer.Peek();
                if (token.type == TokenType.AttrEnd)
                {
                    tokenizer.Take();
                    break;
                }

                var key = ExpectIdentifierOrString(tokenizer);
                string value = null;

                tokenizer.SkipWhiteSpace();
                token = tokenizer.Peek();
                if (token.type == TokenType.Equal)
                {
                    tokenizer.Take();
                    tokenizer.SkipWhiteSpace();
                    value = ExpectIdentifierOrString(tokenizer);
                }

                tokenizer.SkipWhiteSpace();
                token = tokenizer.Peek();
                if (token.type == TokenType.Comma || token.type == TokenType.AttrEnd)
                {
                    tokenizer.Take();
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

        private static ParsedBlock ParseTextBlock(Tokenizer tokenizer, StringBuilder sb)
        {
            while (tokenizer.Peek().type != TokenType.EndOfFile && tokenizer.Peek().type != TokenType.NewLine)
            {
                sb.Append(tokenizer.Take().text);
            }

            // eat up the last newline
            tokenizer.Take();

            return new ParsedBlock
            {
                type = BlockType.Text,
                content = sb.ToString(),
                attributes = new AttributeDict()
            };
        }

        private static ParsedBlock ParseBlock(Tokenizer tokenizer)
        {
            var sb = new StringBuilder();
            var token = tokenizer.Peek();
            while (token.type == TokenType.WhiteSpace)
            {
                sb.Append(token.text);
                tokenizer.Take();
                token = tokenizer.Peek();
            }

            if (token.type == TokenType.NewLine || token.type == TokenType.EndOfFile)
            {
                tokenizer.Take();
                return new ParsedBlock()
                {
                    type = BlockType.Separator,
                    content = sb.ToString(),
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

            return ParseTextBlock(tokenizer, sb);
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