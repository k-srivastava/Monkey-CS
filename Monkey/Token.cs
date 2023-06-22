namespace Monkey;

public enum TokenType
{
    Illegal,
    Eof,

    Identifier,
    Integer,

    Assign,
    Plus,
    Minus,
    Asterisk,
    Slash,

    LesserThan,
    GreaterThan,

    EqualEquals,
    NotEquals,

    Bang,

    Comma,
    Colon,
    Semicolon,

    LeftParenthesis,
    RightParenthesis,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,

    Function,
    String,

    Let,
    True,
    False,
    If,
    Else,
    Return
}

public record Token(TokenType Type, string Literal)
{
    public readonly string Literal = Literal;
    public readonly TokenType Type = Type;

    public static TokenType LookupIdentifier(string identifier)
    {
        var keywords = new Dictionary<string, TokenType>
        {
            { "fn", TokenType.Function },
            { "let", TokenType.Let },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "return", TokenType.Return }
        };

        return keywords.TryGetValue(identifier, out TokenType tokenType) ? tokenType : TokenType.Identifier;
    }
}
