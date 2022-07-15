namespace Nova.Script
{
    public enum TokenType
    {
        BlockStart,
        BlockEnd,
        LeftBrace,
        RightBrace,
        AttrStart,
        AttrEnd,
        At,
        Equal,
        Comma,
        NewLine,
        Quote,
        Character,
        WhiteSpace,
        CommentStart,
        EndOfFile
    };

    public class Token
    {
        public int index;
        public int length;
        public int line;
        public int column;
        public TokenType type;

        public Token Clone()
        {
            return new Token()
            {
                index = index,
                length = length,
                line = line,
                column = column,
                type = type
            };
        }
    }
}
