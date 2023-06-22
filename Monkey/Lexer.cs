namespace Monkey;

public class Lexer
{
    private readonly string _input;
    private char _character;
    private int _position;
    private int _readPosition;

    public Lexer(string input)
    {
        _input = input;
        ReadCharacter();
    }

    public Token NextToken()
    {
        Token token;

        SkipWhitespace();

        switch (_character)
        {
            case '=':
            {
                if (PeekCharacter() == '=')
                {
                    ReadCharacter();
                    token = new Token(TokenType.EqualEquals, "==");
                }
                else
                {
                    token = new Token(TokenType.Assign, "=");
                }

                break;
            }

            case '+':
                token = new Token(TokenType.Plus, "+");
                break;

            case '-':
                token = new Token(TokenType.Minus, "-");
                break;

            case '*':
                token = new Token(TokenType.Asterisk, "*");
                break;

            case '/':
                token = new Token(TokenType.Slash, "/");
                break;

            case '<':
                token = new Token(TokenType.LesserThan, "<");
                break;

            case '>':
                token = new Token(TokenType.GreaterThan, ">");
                break;

            case '!':
            {
                if (PeekCharacter() == '=')
                {
                    ReadCharacter();
                    token = new Token(TokenType.NotEquals, "!=");
                }
                else
                {
                    token = new Token(TokenType.Bang, "!");
                }

                break;
            }

            case '"':
                token = new Token(TokenType.String, ReadString());
                break;

            case ',':
                token = new Token(TokenType.Comma, ",");
                break;

            case ':':
                token = new Token(TokenType.Colon, ":");
                break;

            case ';':
                token = new Token(TokenType.Semicolon, ";");
                break;

            case '(':
                token = new Token(TokenType.LeftParenthesis, "(");
                break;

            case ')':
                token = new Token(TokenType.RightParenthesis, ")");
                break;

            case '{':
                token = new Token(TokenType.LeftBrace, "{");
                break;

            case '}':
                token = new Token(TokenType.RightBrace, "}");
                break;

            case '[':
                token = new Token(TokenType.LeftBracket, "[");
                break;

            case ']':
                token = new Token(TokenType.RightBracket, "]");
                break;

            case '\0':
                token = new Token(TokenType.Eof, "");
                break;

            default:
            {
                if (char.IsLetter(_character))
                {
                    string literal = ReadIdentifier();
                    TokenType type = Token.LookupIdentifier(literal);
                    return new Token(type, literal);
                }

                if (char.IsDigit(_character)) return new Token(TokenType.Integer, ReadNumber());

                token = new Token(TokenType.Illegal, _character.ToString());

                break;
            }
        }

        ReadCharacter();
        return token;
    }

    private void SkipWhitespace()
    {
        while (_character is ' ' or '\t' or '\n' or '\r') ReadCharacter();
    }

    private void ReadCharacter()
    {
        _character = _readPosition >= _input.Length ? '\0' : _input[_readPosition];

        _position = _readPosition;
        _readPosition++;
    }

    private string ReadString()
    {
        int position = _position + 1;

        while (true)
        {
            ReadCharacter();
            if (_character is '"' or '\0') break;
        }

        return _input.Substring(position, _position - position);
    }

    private string ReadNumber()
    {
        int position = _position;

        while (char.IsDigit(_character)) ReadCharacter();
        return _input.Substring(position, _position - position);
    }

    private string ReadIdentifier()
    {
        int position = _position;

        while (char.IsLetter(_character)) ReadCharacter();
        return _input.Substring(position, _position - position);
    }

    private char PeekCharacter()
    {
        return _readPosition >= _input.Length ? '\0' : _input[_readPosition];
    }
}
