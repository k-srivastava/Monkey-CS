namespace Monkey;

public static class Builtins
{
    public static readonly Dictionary<string, Builtin> BuiltinsMap = new()
    {
        { "puts", new Builtin(PutsFunction) },
        { "len", new Builtin(LenFunction) },
        { "first", new Builtin(FirstFunction) },
        { "last", new Builtin(LastFunction) },
        { "rest", new Builtin(RestFunction) },
        { "push", new Builtin(PushFunction) },
        { "map", new Builtin(MapFunction) },
        { "reduce", new Builtin(ReduceFunction) },
        { "sum", new Builtin(SumFunction) },
        { "stringOf", new Builtin(StringOfFunction) },
        { "arrayOf", new Builtin(ArrayOfFunction) }
    };

    private static Object PutsFunction(params Object[] arguments)
    {
        foreach (Object @object in arguments) Console.WriteLine(@object.Inspect());
        return Evaluator.Null;
    }

    private static Object LenFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Builtin function 'len' accepts only 1 argument, got {arguments.Length}.");

        return arguments[0] switch
        {
            String @string => new Integer(@string.Value.Length),
            Array array => new Integer(array.Elements.Length),
            Hash hash => new Integer(hash.Pairs.Count),
            _ => new Error(
                $"Builtin function 'len' is only supported for Strings, Arrays and Hashes, not {arguments[0].Type}."
            )
        };
    }

    private static Object FirstFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Builtin function 'first' accepts only 1 argument, got {arguments.Length}.");

        switch (arguments[0].Type)
        {
            case ObjectType.String:
            {
                var @string = (String)arguments[0];
                return @string.Value.Length > 0 ? new String(@string.Value[0].ToString()) : Evaluator.Null;
            }

            case ObjectType.Array:
            {
                var array = (Array)arguments[0];
                return array.Elements.Length > 0 ? array.Elements[0] : Evaluator.Null;
            }

            case ObjectType.Error:
            case ObjectType.ReturnValue:
            case ObjectType.Integer:
            case ObjectType.Boolean:
            case ObjectType.Hash:
            case ObjectType.Builtin:
            case ObjectType.Function:
            case ObjectType.Null:
            default:
            {
                return new Error(
                    $"Builtin function 'first' is only supported for Strings and Arrays, not {arguments[0].Type}."
                );
            }
        }
    }

    private static Object LastFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Builtin function 'last' accepts only 1 argument, got {arguments.Length}.");

        switch (arguments[0].Type)
        {
            case ObjectType.String:
            {
                var @string = (String)arguments[0];
                return @string.Value.Length > 0 ? new String(@string.Value[^1].ToString()) : Evaluator.Null;
            }

            case ObjectType.Array:
            {
                var array = (Array)arguments[0];
                return array.Elements.Length > 0 ? array.Elements[^1] : Evaluator.Null;
            }

            case ObjectType.Error:
            case ObjectType.ReturnValue:
            case ObjectType.Integer:
            case ObjectType.Boolean:
            case ObjectType.Hash:
            case ObjectType.Builtin:
            case ObjectType.Function:
            case ObjectType.Null:
            default:
            {
                return new Error(
                    $"Builtin function 'last' is only supported for Strings and Arrays, not {arguments[0].Type}."
                );
            }
        }
    }

    private static Object RestFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Builtin function 'rest' accepts only 1 argument, got {arguments.Length}.");

        switch (arguments[0].Type)
        {
            case ObjectType.String:
            {
                var @string = (String)arguments[0];
                int length = @string.Value.Length;

                if (length > 0)
                {
                    var newString = string.Empty;
                    for (var i = 1; i < length; i++) newString += @string.Value[i];
                    return new String(newString);
                }

                return Evaluator.Null;
            }

            case ObjectType.Array:
            {
                var array = (Array)arguments[0];
                int length = array.Elements.Length;

                if (length > 0)
                {
                    var newElements = new Object[length - 1];
                    for (var i = 1; i < length; i++) newElements[i - 1] = array.Elements[i];
                    return new Array(newElements);
                }

                return Evaluator.Null;
            }

            case ObjectType.Error:
            case ObjectType.ReturnValue:
            case ObjectType.Integer:
            case ObjectType.Boolean:
            case ObjectType.Hash:
            case ObjectType.Builtin:
            case ObjectType.Function:
            case ObjectType.Null:
            default:
            {
                return new Error(
                    $"Builtin function 'rest' is only supported for Strings and Arrays, not {arguments[0].Type}."
                );
            }
        }
    }

    private static Object PushFunction(params Object[] arguments)
    {
        Object PushArrayFunction(Array array, Object value)
        {
            var newElements = new Object[array.Elements.Length + 1];
            array.Elements.CopyTo(newElements, 0);
            newElements[^1] = value;

            return new Array(newElements);
        }

        Object PushHashFunction(Hash hash, Object key, Object value)
        {
            var newPairs = new Dictionary<HashKey, HashPair>();

            if (key is not IHashable hashableKey)
            {
                return new Error(
                    $"Builtin function 'push' for Hashes accepts only IHashable key types like Boolean, Integer and String, not {key.Type}."
                );
            }

            foreach ((HashKey hashKey, HashPair hashPair) in hash.Pairs) newPairs.Add(hashKey, hashPair);
            newPairs.Add(hashableKey.HashKey(), new HashPair(key, value));

            return new Hash(newPairs);
        }

        if (arguments.Length is not (2 or 3))
            return new Error($"Builtin function 'push' accepts only 2 or 3 arguments, got {arguments.Length}.");

        return arguments[0].Type switch
        {
            ObjectType.Array when arguments.Length == 2 => PushArrayFunction((Array)arguments[0], arguments[1]),
            ObjectType.Array => new Error(
                $"Builtin function 'push' for Arrays accepts only 2 arguments, got {arguments.Length}."
            ),

            ObjectType.Hash when arguments.Length == 3 => PushHashFunction(
                (Hash)arguments[0], arguments[1], arguments[2]
            ),
            ObjectType.Hash => new Error(
                $"Builtin function 'push' for Arrays accepts only 3 arguments, got {arguments.Length}."
            ),

            _ => new Error($"Builtin function 'rest' is only supported for Arrays and Hashes, not {arguments[0].Type}.")
        };
    }

    private static Object MapFunction(params Object[] arguments)
    {
        if (arguments.Length != 2)
            return new Error($"Builtin function 'map' accepts only 2 arguments, got {arguments.Length}.");

        if (arguments[0].Type != ObjectType.Array)
            return new Error($"Builtin function 'map' can only map onto Arrays, not {arguments[0].Type}.");

        if (arguments[1].Type != ObjectType.Function)
            return new Error($"Builtin function 'map' can only map from Functions, not {arguments[1].Type}.");

        var array = (Array)arguments[0];
        var function = (Function)arguments[1];

        if (function.Parameters.Length != 1)
        {
            return new Error(
                $"Function passed to builtin function 'map' should have only 1 parameter, not {function.Parameters.Length}."
            );
        }

        var newElements = new Object[array.Elements.Length];
        for (var i = 0; i < array.Elements.Length; i++)
        {
            Object value = Evaluator.ApplyFunction(function, new[] { array.Elements[i] });
            newElements[i] = value;
        }

        return new Array(newElements);
    }

    private static Object ReduceFunction(params Object[] arguments)
    {
        if (arguments.Length != 3)
            return new Error($"Builtin function 'reduce' accepts only 3 arguments, got {arguments.Length}.");

        if (arguments[0].Type != ObjectType.Array)
            return new Error($"Builtin function 'reduce' can only reduce Arrays, not {arguments[0].Type}.");

        if (arguments[1].Type != ObjectType.Integer)
            return new Error($"Builtin function 'reduce' can only reduce into Integers, not {arguments[1].Type}.");

        if (arguments[2].Type != ObjectType.Function)
            return new Error($"Builtin function 'reduce' can only reduce from Functions, not {arguments[2].Type}.");

        var array = (Array)arguments[0];
        var start = (Integer)arguments[1];
        var function = (Function)arguments[2];

        if (function.Parameters.Length != 2)
        {
            return new Error(
                $"Function passed into builtin function 'reduce' should have only 2 parameters, not {function.Parameters.Length}."
            );
        }

        Integer output = start;
        for (var i = 0; i < array.Elements.Length; i++)
        {
            Object @object = array.Elements[i];
            if (@object.Type != ObjectType.Integer)
            {
                return new Error(
                    $"Element at index [{i}] in Array passed into builtin function 'reduce' should be an Integer, not {@object.Type}."
                );
            }

            output = (Integer)Evaluator.ApplyFunction(function, new[] { output, @object });
        }

        return output;
    }

    private static Object SumFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Builtin function 'sum' accepts only 1 argument, got {arguments.Length}.");

        if (arguments[0].Type != ObjectType.Array)
            return new Error($"Builtin function 'sum' can only sum Arrays, not {arguments[0].Type}.");

        var array = (Array)arguments[0];
        long sum = 0;

        for (var i = 0; i < array.Elements.Length; i++)
        {
            Object @object = array.Elements[i];
            if (@object.Type != ObjectType.Integer)
                return new Error(
                    $"Element at index [{i}] in Array passed into builtin function 'sum' should be an Integer, not {@object.Type}."
                );

            sum += ((Integer)@object).Value;
        }

        return new Integer(sum);
    }

    private static Object StringOfFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Builtin function 'stringOf' accepts only 1 argument, got {arguments.Length}");

        return new String(arguments[0].Inspect());
    }

    private static Object ArrayOfFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Builtin function 'arrayOf' accepts only 1 argument, got {arguments.Length}");

        switch (arguments[0].Type)
        {
            case ObjectType.Integer:
            {
                var number = ((Integer)arguments[0]).Value.ToString();
                var digits = new Object[number.Length];

                for (var i = 0; i < number.Length; i++)
                {
                    digits[i] = new String(number[i]);
                }

                return new Array(digits);
            }

            case ObjectType.Boolean:
                return new Array(new Object[] { (Boolean)arguments[0] });

            case ObjectType.String:
            {
                string data = ((String)arguments[0]).Value;
                var chars = new Object[data.Length];

                for (var i = 0; i < data.Length; i++)
                {
                    chars[i] = new String(data[i]);
                }

                return new Array(chars);
            }

            case ObjectType.Error:
            case ObjectType.ReturnValue:
            case ObjectType.Array:
            case ObjectType.Hash:
            case ObjectType.Builtin:
            case ObjectType.Function:
            case ObjectType.Null:
            default:
            {
                return new Error(
                    $"Builtin function 'arrayOf' can only convert Integers, Boolean and Strings, not {arguments[0].Type}."
                );
            }
        }
    }
}
