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
        public string text;
        public int line;
        public int column;
        public TokenType type;
    }
}