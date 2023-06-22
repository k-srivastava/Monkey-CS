namespace Monkey;

public abstract class Node
{
    public abstract Token Token { get; }
    public abstract override string ToString();
}

public abstract class Statement : Node { }

public abstract class Expression : Node { }

public sealed class LetStatement : Statement
{
    public readonly Identifier Name;
    public readonly Expression? Value;

    public LetStatement(Token token, Identifier name, Expression? value)
    {
        Token = token;
        Name = name;
        Value = value;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        var output = $"{Token.Literal} {Name}";
        if (Value != null) output += $" = {Value}";
        output += ";";

        return output;
    }
}

public sealed class ReturnStatement : Statement
{
    public readonly Expression? Value;

    public ReturnStatement(Token token, Expression? value)
    {
        Token = token;
        Value = value;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        string output = Token.Literal;
        if (Value != null) output += $" {Value}";
        output += ";";

        return output;
    }
}

public sealed class ExpressionStatement : Statement
{
    public readonly Expression? Expression;

    public ExpressionStatement(Token token, Expression? value)
    {
        Token = token;
        Expression = value;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return Expression != null ? Expression.ToString() : string.Empty;
    }
}

public sealed class BlockStatement : Statement
{
    public readonly Statement[] Statements;

    public BlockStatement(Token token, Statement[] statements)
    {
        Token = token;
        Statements = statements;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return Statements.Aggregate(string.Empty, (current, statement) => current + statement);
    }
}

public sealed class Identifier : Expression
{
    public readonly string Value;

    public Identifier(Token token, string value)
    {
        Token = token;
        Value = value;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return Value;
    }
}

public sealed class IntegerLiteral : Expression
{
    public readonly long Value;

    public IntegerLiteral(Token token, long value)
    {
        Token = token;
        Value = value;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return Token.Literal;
    }
}

public sealed class BooleanLiteral : Expression
{
    public readonly bool Value;

    public BooleanLiteral(Token token, bool value)
    {
        Token = token;
        Value = value;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return Token.Literal;
    }
}

public sealed class StringLiteral : Expression
{
    public readonly string Value;

    public StringLiteral(Token token, string value)
    {
        Token = token;
        Value = value;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return Token.Literal;
    }
}

public sealed class ArrayLiteral : Expression
{
    public readonly Expression[] Elements;

    public ArrayLiteral(Token token, Expression[] elements)
    {
        Token = token;
        Elements = elements;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        List<string> elements = Elements.Select(element => element.ToString()).ToList();
        return $"[{string.Join(", ", elements)}]";
    }
}

public sealed class HashLiteral : Expression
{
    public readonly Dictionary<Expression, Expression> Pairs;

    public HashLiteral(Token token, Dictionary<Expression, Expression> pairs)
    {
        Token = token;
        Pairs = pairs;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        var pairs = new List<string>();
        foreach ((Expression key, Expression value) in Pairs) pairs.Add($"{key}: {value}");
        return $"{{{string.Join(", ", pairs)}}}";
    }
}

public sealed class FunctionLiteral : Expression
{
    public readonly BlockStatement Body;
    public readonly Identifier[] Parameters;

    public FunctionLiteral(Token token, Identifier[] parameters, BlockStatement body)
    {
        Token = token;
        Parameters = parameters;
        Body = body;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        List<string> parameters = Parameters.Select(parameter => parameter.ToString()).ToList();
        return $"{Token.Literal} ({string.Join(", ", parameters)}) {Body}";
    }
}

public sealed class PrefixExpression : Expression
{
    public readonly string Operator;
    public readonly Expression Right;

    public PrefixExpression(Token token, string @operator, Expression right)
    {
        Token = token;
        Operator = @operator;
        Right = right;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return $"({Operator}{Right})";
    }
}

public sealed class InfixExpression : Expression
{
    public readonly Expression Left;
    public readonly string Operator;
    public readonly Expression Right;

    public InfixExpression(Token token, Expression left, string @operator, Expression right)
    {
        Token = token;
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return $"({Left} {Operator} {Right})";
    }
}

public sealed class IfExpression : Expression
{
    public readonly BlockStatement? Alternative;
    public readonly Expression Condition;
    public readonly BlockStatement Consequence;

    public IfExpression(Token token, Expression condition, BlockStatement consequence, BlockStatement? alternative)
    {
        Token = token;
        Condition = condition;
        Consequence = consequence;
        Alternative = alternative;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        var output = string.Empty;

        output += $"if {Condition} {Consequence}";
        if (Alternative != null) output += $"else {Alternative}";

        return output;
    }
}

public sealed class IndexExpression : Expression
{
    public readonly Expression Index;
    public readonly Expression Left;

    public IndexExpression(Token token, Expression left, Expression index)
    {
        Token = token;
        Left = left;
        Index = index;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return $"({Left}[{Index}])";
    }
}

public sealed class CallExpression : Expression
{
    public readonly Expression[] Arguments;
    public readonly Expression Function;

    public CallExpression(Token token, Expression function, Expression[] arguments)
    {
        Token = token;
        Function = function;
        Arguments = arguments;
    }

    public override Token Token { get; }

    public override string ToString()
    {
        List<string> arguments = Arguments.Select(argument => argument.ToString()).ToList();
        return $"{Function} ({string.Join(", ", arguments)})";
    }
}

public class Program : Node
{
    public readonly Statement[] Statements;

    public Program(Statement[] statements)
    {
        Statements = statements;
        Token = new Token(TokenType.Illegal, Statements.Length > 0 ? Statements[0].Token.Literal : string.Empty);
    }

    public override Token Token { get; }

    public override string ToString()
    {
        return Statements.Aggregate("", (current, statement) => current + statement);
    }
}
