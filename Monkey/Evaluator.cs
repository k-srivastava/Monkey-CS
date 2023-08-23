namespace Monkey;

public static class Evaluator
{
    private static readonly Boolean True = new(true);
    private static readonly Boolean False = new(false);
    public static readonly Null Null = new();

    public static Object Evaluate(Node? node, Environment environment)
    {
        switch (node)
        {
            case Program program:
                return EvaluateProgram(program.Statements, environment);

            case LetStatement statement:
                Object letValue = Evaluate(statement.Value, environment);
                if (IsError(letValue)) return letValue;
                environment.Add(statement.Name.Value, letValue);

                break;

            case ReturnStatement statement:
                Object returnValue = Evaluate(statement.Value, environment);
                return IsError(returnValue) ? returnValue : new Return(returnValue);

            case ExpressionStatement statement:
                // ReSharper disable once TailRecursiveCall
                return Evaluate(statement.Expression, environment);

            case BlockStatement statement:
                return EvaluateBlockStatement(statement.Statements, environment);

            case Identifier identifier:
                return EvaluateIdentifier(identifier, environment);

            case IntegerLiteral literal:
                return new Integer(literal.Value);

            case BooleanLiteral literal:
                return GetBooleanInstance(literal.Value);

            case StringLiteral literal:
                return new String(literal.Value);

            case ArrayLiteral literal:
                Object[] elements = EvaluateExpressions(literal.Elements, environment);
                if (elements.Length == 1 && IsError(elements[0])) return elements[0];
                return new Array(elements);

            case HashLiteral literal:
                return EvaluateHashLiteral(literal, environment);

            case FunctionLiteral literal:
                return new Function(literal.Parameters, literal.Body, environment);

            case PrefixExpression expression:
                Object prefixRight = Evaluate(expression.Right, environment);
                return IsError(prefixRight) ? prefixRight : EvaluatePrefixExpression(expression.Operator, prefixRight);

            case InfixExpression expression:
                Object infixLeft = Evaluate(expression.Left, environment);
                if (IsError(infixLeft)) return infixLeft;

                Object infixRight = Evaluate(expression.Right, environment);
                return IsError(infixRight)
                    ? infixRight
                    : EvaluateInfixExpression(expression.Operator, infixLeft, infixRight);

            case IfExpression expression:
                return EvaluateIfExpression(expression, environment);

            case IndexExpression expression:
                Object indexLeft = Evaluate(expression.Left, environment);
                if (IsError(indexLeft)) return indexLeft;

                Object index = Evaluate(expression.Index, environment);
                return IsError(index) ? index : EvaluateIndexExpression(indexLeft, index);

            case CallExpression expression:
                Object function = Evaluate(expression.Function, environment);
                if (IsError(function)) return function;

                Object[] arguments = EvaluateExpressions(expression.Arguments, environment);
                if (arguments.Length == 1 && IsError(arguments[0])) return arguments[0];

                return ApplyFunction(function, arguments);
        }

        return Null;
    }

    private static Object EvaluateProgram(IEnumerable<Statement> statements, Environment environment)
    {
        Object? result = null;

        foreach (Statement statement in statements)
        {
            result = Evaluate(statement, environment);

            switch (result)
            {
                case Return @return:
                    return @return.Value;

                case Error error:
                    return error;
            }
        }

        return result ?? Null;
    }

    private static Object UnwrapReturnValue(Object @object)
    {
        if (@object is Return @return) return @return.Value;
        return @object;
    }

    private static Object EvaluateBlockStatement(IEnumerable<Statement> statements, Environment environment)
    {
        Object? result = null;

        foreach (Statement statement in statements)
        {
            result = Evaluate(statement, environment);
            if (result is { Type: ObjectType.ReturnValue or ObjectType.Error }) return result;
        }

        return result ?? Null;
    }

    private static Object EvaluateIdentifier(Identifier node, Environment environment)
    {
        if (environment.TryGetValue(node.Value, out Object? value)) return value;
        if (Builtins.BuiltinsMap.TryGetValue(node.Value, out Builtin? builtin)) return builtin;
        return new Error($"Identifier Not Found: {node.Value}");
    }

    private static Object[] EvaluateExpressions(IEnumerable<Expression> expressions, Environment environment)
    {
        var result = new List<Object>();

        foreach (Expression expression in expressions)
        {
            Object evaluated = Evaluate(expression, environment);
            if (IsError(evaluated)) return new[] { evaluated };
            result.Add(evaluated);
        }

        return result.ToArray();
    }

    private static Object EvaluateHashLiteral(HashLiteral node, Environment environment)
    {
        var pairs = new Dictionary<HashKey, HashPair>();

        foreach ((Expression nodeKey, Expression nodeValue) in node.Pairs)
        {
            Object key = Evaluate(nodeKey, environment);
            if (IsError(key)) return key;

            if (key is not IHashable hashKey) return new Error($"Invalid Hash Key: {key.Type}.");

            Object value = Evaluate(nodeValue, environment);
            if (IsError(value)) return value;

            HashKey hashed = hashKey.HashKey();
            pairs.Add(hashed, new HashPair(key, value));
        }

        return new Hash(pairs);
    }

    public static Object ApplyFunction(Object function, Object[] arguments)
    {
        switch (function)
        {
            case Function functionObject:
            {
                Environment extendedEnvironment = ExtendFunctionEnvironment(functionObject, arguments);
                Object evaluated = Evaluate(functionObject.Body, extendedEnvironment);
                return UnwrapReturnValue(evaluated);
            }

            case Builtin builtin:
                return builtin.Function(arguments);

            default:
                return new Error($"Not a Function: {function.Type}");
        }
    }

    private static Environment ExtendFunctionEnvironment(Function function, IReadOnlyList<Object> arguments)
    {
        var environment = new Environment(function.Environment);
        for (var i = 0; i < function.Parameters.Length; i++)
            environment.Add(function.Parameters[i].Value, arguments[i]);
        return environment;
    }

    private static Object EvaluatePrefixExpression(string @operator, Object? right)
    {
        if (right == null) return Null;

        return @operator switch
        {
            "!" => EvaluateNotExpression(right),
            "-" => EvaluateNegationExpression(right),
            _ => new Error($"Unknown Operator: {@operator}{right.Type}")
        };
    }

    private static Object EvaluateInfixExpression(string @operator, Object left, Object right)
    {
        if (left.Type != right.Type) return new Error($"Type Mismatch: {left.Type} {@operator} {right.Type}");

        if (left.Type == ObjectType.Integer && right.Type == ObjectType.Integer)
            return EvaluateArithmeticExpression(@operator, left, right);

        if (left.Type == ObjectType.String && right.Type == ObjectType.String)
            return EvaluateStringExpression(@operator, left, right);

        return @operator switch
        {
            "==" => GetBooleanInstance(left == right),
            "!=" => GetBooleanInstance(left != right),
            _ => new Error($"Unknown Operator: {left.Type} {@operator} {right.Type}")
        };
    }

    private static Object EvaluateIfExpression(IfExpression expression, Environment environment)
    {
        Object condition = Evaluate(expression.Condition, environment);
        if (IsError(condition)) return condition;

        if (IsTruthy(condition)) return Evaluate(expression.Consequence, environment);
        return expression.Alternative != null ? Evaluate(expression.Alternative, environment) : Null;
    }

    private static Object EvaluateIndexExpression(Object left, Object index)
    {
        if (left.Type == ObjectType.Array && index.Type == ObjectType.Integer)
            return EvaluateArrayIndexExpression(left, index);

        if (left.Type == ObjectType.Hash) return EvaluateHashIndexExpression(left, index);

        return new Error($"Index operator is not supported for {left.Type}.");
    }

    private static Object EvaluateNotExpression(Object right)
    {
        if (right == True) return False;
        if (right == False) return True;
        return right == Null ? True : False;
    }

    private static Object EvaluateNegationExpression(Object right)
    {
        if (right.Type != ObjectType.Integer) return new Error($"Unknown Operator: -{right.Type}");

        long value = (right as Integer)!.Value;
        return new Integer(-value);
    }

    private static Object EvaluateArithmeticExpression(string @operator, Object left, Object right)
    {
        long leftValue = (left as Integer)!.Value;
        long rightValue = (right as Integer)!.Value;

        return @operator switch
        {
            "+" => new Integer(leftValue + rightValue),
            "-" => new Integer(leftValue - rightValue),
            "*" => new Integer(leftValue * rightValue),
            "/" => new Integer(leftValue / rightValue),
            "<" => GetBooleanInstance(leftValue < rightValue),
            ">" => GetBooleanInstance(leftValue > rightValue),
            "==" => GetBooleanInstance(leftValue == rightValue),
            "!=" => GetBooleanInstance(leftValue != rightValue),
            _ => new Error($"Unknown Operator: {left.Type} {@operator} {right.Type}")
        };
    }

    private static Object EvaluateStringExpression(string @operator, Object left, Object right)
    {
        if (@operator != "+") return new Error($"Unknown Operator: {left.Type} {@operator} {right.Type}");
        return new String((left as String)!.Value + (right as String)!.Value);
    }

    private static Object EvaluateArrayIndexExpression(Object array, Object index)
    {
        var arrayObject = (array as Array)!;
        long indexValue = (index as Integer)!.Value;
        int max = arrayObject.Elements.Length - 1;

        if (indexValue < 0 || indexValue > max) return Null;
        return arrayObject.Elements[indexValue];
    }

    private static Object EvaluateHashIndexExpression(Object hash, Object index)
    {
        var hashObject = (hash as Hash)!;

        if (index is not IHashable key) return new Error($"Invalid Hash Key: {index.Type}.");
        return hashObject.Pairs.TryGetValue(key.HashKey(), out HashPair? pair) ? pair.Value : Null;
    }

    private static Boolean GetBooleanInstance(bool input)
    {
        return input ? True : False;
    }

    private static bool IsTruthy(Object @object)
    {
        if (@object == Null) return false;
        if (@object == True) return true;
        return @object != False;
    }

    private static bool IsError(Object? @object)
    {
        if (@object != null) return @object.Type == ObjectType.Error;
        return false;
    }
}
