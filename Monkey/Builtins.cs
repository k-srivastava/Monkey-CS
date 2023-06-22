namespace Monkey;

public static class Builtins
{
    private static Object PutsFunction(params Object[] arguments)
    {
        foreach (Object @object in arguments) Console.WriteLine(@object.Inspect());
        return Evaluator.Null;
    }

    private static Object LenFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Wrong number of arguments, expected 1 argument, got {arguments.Length}.");

        return arguments[0] switch
        {
            String @string => new Integer(@string.Value.Length),
            Array array => new Integer(array.Elements.Length),
            _ => new Error($"Builtin function 'len' is not supported for {arguments[0].Type}.")
        };
    }

    private static Object FirstFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Wrong number of arguments, expected 1 argument, got {arguments.Length}.");

        if (arguments[0].Type != ObjectType.Array)
            return new Error($"Builtin function 'first' is not supported for {arguments[0].Type}.");

        var array = (arguments[0] as Array)!;
        return array.Elements.Length > 0 ? array.Elements[0] : Evaluator.Null;
    }

    private static Object LastFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Wrong number of arguments, expected 1 argument, got {arguments.Length}.");

        if (arguments[0].Type != ObjectType.Array)
            return new Error($"Builtin function 'last' is not supported for {arguments[0].Type}.");

        var array = (arguments[0] as Array)!;
        return array.Elements.Length > 0 ? array.Elements[^1] : Evaluator.Null;
    }

    private static Object RestFunction(params Object[] arguments)
    {
        if (arguments.Length != 1)
            return new Error($"Wrong number of arguments, expected 1 argument, got {arguments.Length}.");

        if (arguments[0].Type != ObjectType.Array)
            return new Error($"Builtin function 'last' is not supported for {arguments[0].Type}.");

        var array = (arguments[0] as Array)!;
        int length = array.Elements.Length;

        if (length > 0)
        {
            var newElements = new Object[length - 1];
            for (var i = 1; i < length; i++) newElements[i - 1] = array.Elements[i];
            return new Array(newElements);
        }

        return Evaluator.Null;
    }

    private static Object PushFunction(params Object[] arguments)
    {
        if (arguments.Length != 2)
            return new Error($"Wrong number of arguments, expected 2 arguments, got {arguments.Length}.");

        if (arguments[0].Type != ObjectType.Array)
            return new Error($"Builtin function 'push' is not supported for {arguments[0].Type}.");

        var array = (arguments[0] as Array)!;
        var newElements = new Object[array.Elements.Length + 1];

        array.Elements.CopyTo(newElements, 0);
        newElements[^1] = arguments[1];

        return new Array(newElements);
    }

    public static readonly Dictionary<string, Builtin> BuiltinsMap = new()
    {
        { "puts", new Builtin(PutsFunction) },
        { "len", new Builtin(LenFunction) },
        { "first", new Builtin(FirstFunction) },
        { "last", new Builtin(LastFunction) },
        { "rest", new Builtin(RestFunction) },
        { "push", new Builtin(PushFunction) }
    };
}
