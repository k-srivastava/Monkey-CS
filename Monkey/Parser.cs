namespace Monkey;

internal delegate Expression? PrefixParseFunction();

internal delegate Expression InfixParseFunction(Expression expression);

internal enum Precedence
{
    Lowest = 1,
    Equals = 2,
    Comparison = 3,
    Sum = 4,
    Product = 5,
    Prefix = 6,
    Call = 7,
    Index = 8
}

public class Parser
{
    private static readonly Dictionary<TokenType, Precedence> Precedences = new()
    {
        { TokenType.EqualEquals, Precedence.Equals },
        { TokenType.NotEquals, Precedence.Equals },
        { TokenType.LesserThan, Precedence.Comparison },
        { TokenType.GreaterThan, Precedence.Comparison },
        { TokenType.Plus, Precedence.Sum },
        { TokenType.Minus, Precedence.Sum },
        { TokenType.Asterisk, Precedence.Product },
        { TokenType.Slash, Precedence.Product },
        { TokenType.LeftParenthesis, Precedence.Call },
        { TokenType.LeftBracket, Precedence.Index }
    };

    private readonly Dictionary<TokenType, InfixParseFunction> _infixParseFunctions;

    private readonly Lexer _lexer;
    private readonly Dictionary<TokenType, PrefixParseFunction> _prefixParseFunctions;

    private Token _currentToken;
    private Token _peekToken;

    public Parser(Lexer lexer)
    {
        _lexer = lexer;
        Errors = new List<string>();

        _prefixParseFunctions = new Dictionary<TokenType, PrefixParseFunction>
        {
            { TokenType.Identifier, ParseIdentifier },
            { TokenType.Integer, ParseIntegerLiteral },
            { TokenType.Bang, ParsePrefixExpression },
            { TokenType.Minus, ParsePrefixExpression },
            { TokenType.True, ParseBooleanLiteral },
            { TokenType.False, ParseBooleanLiteral },
            { TokenType.String, ParseStringLiteral },
            { TokenType.LeftParenthesis, ParseGroupedExpression },
            { TokenType.LeftBracket, ParseArrayLiteral },
            { TokenType.LeftBrace, ParseHashLiteral },
            { TokenType.If, ParseIfExpression },
            { TokenType.Function, ParseFunctionLiteral }
        };

        _infixParseFunctions = new Dictionary<TokenType, InfixParseFunction>
        {
            { TokenType.Plus, ParseInfixExpression },
            { TokenType.Minus, ParseInfixExpression },
            { TokenType.Slash, ParseInfixExpression },
            { TokenType.Asterisk, ParseInfixExpression },
            { TokenType.EqualEquals, ParseInfixExpression },
            { TokenType.NotEquals, ParseInfixExpression },
            { TokenType.LesserThan, ParseInfixExpression },
            { TokenType.GreaterThan, ParseInfixExpression },
            { TokenType.LeftParenthesis, ParseCallExpression },
            { TokenType.LeftBracket, ParseIndexExpression }
        };

        // NextToken();
        // NextToken();
        _currentToken = _peekToken!;
        _peekToken = _lexer.NextToken();

        _currentToken = _peekToken;
        _peekToken = _lexer.NextToken();
    }

    public List<string> Errors { get; }

    public Program ParseProgram()
    {
        var statements = new List<Statement>();

        while (_currentToken.Type != TokenType.Eof)
        {
            Statement? statement = ParseStatement();
            if (statement != null) statements.Add(statement);

            NextToken();
        }

        return new Program(statements.ToArray());
    }

    private Statement? ParseStatement()
    {
        return _currentToken.Type switch
        {
            TokenType.Let => ParseLetStatement(),
            TokenType.Return => ParseReturnStatement(),
            _ => ParseExpressionStatement()
        };
    }

    private LetStatement? ParseLetStatement()
    {
        Token token = _currentToken;

        if (!ExpectPeek(TokenType.Identifier)) return null;
        var name = new Identifier(_currentToken, _currentToken.Literal);

        if (!ExpectPeek(TokenType.Assign)) return null;
        NextToken();

        Expression? value = ParseExpression(Precedence.Lowest);

        while (_currentToken.Type != TokenType.Semicolon) NextToken();
        return new LetStatement(token, name, value);
    }

    private ReturnStatement ParseReturnStatement()
    {
        Token token = _currentToken;
        NextToken();
        Expression? value = ParseExpression(Precedence.Lowest);

        while (_currentToken.Type != TokenType.Semicolon) NextToken();
        return new ReturnStatement(token, value);
    }

    private ExpressionStatement ParseExpressionStatement()
    {
        var statement = new ExpressionStatement(_currentToken, ParseExpression(Precedence.Lowest));

        if (_peekToken.Type == TokenType.Semicolon) NextToken();
        return statement;
    }

    private BlockStatement ParseBlockStatement()
    {
        Token token = _currentToken;
        var statements = new List<Statement>();

        NextToken();

        while (_currentToken.Type != TokenType.RightBrace && _currentToken.Type != TokenType.Eof)
        {
            Statement? statement = ParseStatement();
            if (statement != null) statements.Add(statement);
            NextToken();
        }

        return new BlockStatement(token, statements.ToArray());
    }

    private Expression? ParseExpression(Precedence precedence)
    {
        if (!_prefixParseFunctions.TryGetValue(_currentToken.Type, out PrefixParseFunction? prefixFunction))
        {
            NoPrefixParseFunctionError(_currentToken.Type);
            return null;
        }

        Expression? left = prefixFunction();
        while (_peekToken.Type != TokenType.Semicolon && precedence < PeekPrecedence())
        {
            if (!_infixParseFunctions.TryGetValue(_peekToken.Type, out InfixParseFunction? infixFunction)) return left;
            NextToken();
            left = infixFunction(left!);
        }

        return left;
    }

    private Expression ParseIdentifier()
    {
        return new Identifier(_currentToken, _currentToken.Literal);
    }

    private Expression? ParseIntegerLiteral()
    {
        Token token = _currentToken;
        if (!int.TryParse(_currentToken.Literal, out int value))
        {
            Errors.Add($"Could not parse '{_currentToken.Literal} as an integer.'");
            return null;
        }

        return new IntegerLiteral(token, value);
    }

    private Expression ParseBooleanLiteral()
    {
        return new BooleanLiteral(_currentToken, _currentToken.Type == TokenType.True);
    }

    private Expression ParseStringLiteral()
    {
        return new StringLiteral(_currentToken, _currentToken.Literal);
    }

    private Expression ParseArrayLiteral()
    {
        return new ArrayLiteral(_currentToken, ParseExpressionList(TokenType.RightBracket));
    }

    private Expression? ParseHashLiteral()
    {
        var hash = new HashLiteral(_currentToken, new Dictionary<Expression, Expression>());

        while (_peekToken.Type != TokenType.RightBrace)
        {
            NextToken();
            Expression key = ParseExpression(Precedence.Lowest)!;

            if (!ExpectPeek(TokenType.Colon)) return null;

            NextToken();
            Expression value = ParseExpression(Precedence.Lowest)!;

            hash.Pairs.Add(key, value);
            if (_peekToken.Type != TokenType.RightBrace && !ExpectPeek(TokenType.Comma)) return null;
        }

        return !ExpectPeek(TokenType.RightBrace) ? null : hash;
    }

    private Expression? ParseFunctionLiteral()
    {
        Identifier[]? ParseFunctionParameters()
        {
            var identifiers = new List<Identifier>();

            if (_peekToken.Type == TokenType.RightParenthesis)
            {
                NextToken();
                return System.Array.Empty<Identifier>();
            }

            NextToken();
            identifiers.Add(new Identifier(_currentToken, _currentToken.Literal));

            while (_peekToken.Type == TokenType.Comma)
            {
                NextToken();
                NextToken();

                identifiers.Add(new Identifier(_currentToken, _currentToken.Literal));
            }

            return !ExpectPeek(TokenType.RightParenthesis) ? null : identifiers.ToArray();
        }

        Token token = _currentToken;

        if (!ExpectPeek(TokenType.LeftParenthesis)) return null;
        Identifier[]? parameters = ParseFunctionParameters();

        if (!ExpectPeek(TokenType.LeftBrace)) return null;
        BlockStatement body = ParseBlockStatement();

        return parameters == null
            ? new FunctionLiteral(token, System.Array.Empty<Identifier>(), body)
            : new FunctionLiteral(token, parameters, body);
    }

    private Expression ParsePrefixExpression()
    {
        Token token = _currentToken;
        string @operator = _currentToken.Literal;

        NextToken();
        Expression? right = ParseExpression(Precedence.Prefix);

        return new PrefixExpression(token, @operator, right!);
    }

    private Expression ParseInfixExpression(Expression left)
    {
        Token token = _currentToken;
        string @operator = _currentToken.Literal;

        Precedence precedence = CurrentPrecedence();
        NextToken();

        return new InfixExpression(token, left, @operator, ParseExpression(precedence)!);
    }

    private Expression? ParseIfExpression()
    {
        Token token = _currentToken;

        if (!ExpectPeek(TokenType.LeftParenthesis)) return null;

        NextToken();
        Expression condition = ParseExpression(Precedence.Lowest)!;
        BlockStatement? alternative = null;

        if (!ExpectPeek(TokenType.RightParenthesis)) return null;
        if (!ExpectPeek(TokenType.LeftBrace)) return null;
        BlockStatement consequence = ParseBlockStatement();

        if (_peekToken.Type == TokenType.Else)
        {
            NextToken();

            if (!ExpectPeek(TokenType.LeftBrace)) return null;
            alternative = ParseBlockStatement();
        }

        return new IfExpression(token, condition, consequence, alternative);
    }

    private Expression? ParseIndexExpression(Expression left)
    {
        Token token = _currentToken;
        NextToken();

        var expression = new IndexExpression(token, left, ParseExpression(Precedence.Lowest)!);
        return ExpectPeek(TokenType.RightBracket) ? expression : null;
    }

    private Expression ParseCallExpression(Expression function)
    {
        return new CallExpression(_currentToken, function, ParseExpressionList(TokenType.RightParenthesis));
    }

    private Expression? ParseGroupedExpression()
    {
        NextToken();

        Expression? expression = ParseExpression(Precedence.Lowest);
        return !ExpectPeek(TokenType.RightParenthesis) ? null : expression;
    }

    private Expression[]? ParseExpressionList(TokenType end)
    {
        var expressionList = new List<Expression>();

        if (_peekToken.Type == end)
        {
            NextToken();
            return expressionList.ToArray();
        }

        NextToken();
        expressionList.Add(ParseExpression(Precedence.Lowest)!);

        while (_peekToken.Type == TokenType.Comma)
        {
            NextToken();
            NextToken();

            expressionList.Add(ParseExpression(Precedence.Lowest)!);
        }

        return ExpectPeek(end) ? expressionList.ToArray() : null;
    }

    private void NextToken()
    {
        _currentToken = _peekToken;
        _peekToken = _lexer.NextToken();
    }

    private bool ExpectPeek(TokenType tokenType)
    {
        if (_peekToken.Type == tokenType)
        {
            NextToken();
            return true;
        }

        PeekError(tokenType);
        return false;
    }

    private void PeekError(TokenType tokenType)
    {
        var message = $"Expected next token to be '{tokenType}', instead got '{_peekToken.Type}'.";
        Errors.Add(message);
    }

    private Precedence PeekPrecedence()
    {
        return Precedences.TryGetValue(_peekToken.Type, out Precedence precedence) ? precedence : Precedence.Lowest;
    }

    private Precedence CurrentPrecedence()
    {
        return Precedences.TryGetValue(_currentToken.Type, out Precedence precedence) ? precedence : Precedence.Lowest;
    }

    private void NoPrefixParseFunctionError(TokenType tokenType)
    {
        Errors.Add($"No prefix parse function found for '{tokenType}'.");
    }
}
