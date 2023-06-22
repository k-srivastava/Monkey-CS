using System.Security.Cryptography;
using System.Text;

namespace Monkey;

public enum ObjectType
{
    Error,
    ReturnValue,
    Integer,
    Boolean,
    String,
    Array,
    Hash,
    Builtin,
    Function,
    Null
}

public abstract class Object
{
    public abstract ObjectType Type { get; }
    public abstract string Inspect();
}

public interface IHashable
{
    public HashKey HashKey();
}

public delegate Object BuiltinFunction(params Object[] arguments);

public class Error : Object
{
    private readonly string _message;

    public Error(string message)
    {
        _message = message;
    }

    public override ObjectType Type => ObjectType.Error;

    public override string Inspect()
    {
        return $"Error: {_message}";
    }
}

public class Return : Object
{
    public readonly Object? Value;

    public Return(Object? value)
    {
        Value = value;
    }

    public override ObjectType Type => ObjectType.ReturnValue;

    public override string Inspect()
    {
        return Value?.Inspect() ?? string.Empty;
    }
}

public class Integer : Object, IHashable
{
    public readonly long Value;

    public Integer(long value)
    {
        Value = value;
    }

    public override ObjectType Type => ObjectType.Integer;

    #region IHashable Members

    public HashKey HashKey()
    {
        return new HashKey(Type, (ulong)Value);
    }

    #endregion

    public override string Inspect()
    {
        return $"{Value}";
    }
}

public class Boolean : Object, IHashable
{
    private readonly bool _value;

    public Boolean(bool value)
    {
        _value = value;
    }

    public override ObjectType Type => ObjectType.Boolean;

    #region IHashable Members

    public HashKey HashKey()
    {
        return new HashKey(Type, (ulong)(_value ? 1 : 0));
    }

    #endregion

    public override string Inspect()
    {
        return $"{_value}";
    }
}

public class String : Object, IHashable
{
    public readonly string Value;

    public String(string value)
    {
        Value = value;
    }

    public override ObjectType Type => ObjectType.String;

    #region IHashable Members

    public HashKey HashKey()
    {
        using var algorithm = HashAlgorithm.Create("SHA512")!;

        byte[] data = Encoding.UTF8.GetBytes(Value);
        byte[] hash = algorithm.ComputeHash(data);
        var hashValue = BitConverter.ToUInt64(hash, 0);

        return new HashKey(Type, hashValue);
    }

    #endregion

    public override string Inspect()
    {
        return Value;
    }
}

public class Array : Object
{
    public readonly Object[] Elements;

    public Array(Object[] elements)
    {
        Elements = elements;
    }

    public override ObjectType Type => ObjectType.Array;

    public override string Inspect()
    {
        List<string> elements = Elements.Select(element => element.Inspect()).ToList();
        return $"[{string.Join(", ", elements)}]";
    }
}

public class HashKey
{
    private readonly ObjectType _type;
    private readonly ulong _value;

    public HashKey(ObjectType type, ulong value)
    {
        _type = type;
        _value = value;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;

        var other = (obj as HashKey)!;
        return _type == other._type && _value == other._value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)_type, _value);
    }
}

public class HashPair
{
    public readonly Object Key;
    public readonly Object Value;

    public HashPair(Object key, Object value)
    {
        Key = key;
        Value = value;
    }
}

public class Hash : Object
{
    public readonly Dictionary<HashKey, HashPair> Pairs;

    public Hash(Dictionary<HashKey, HashPair> pairs)
    {
        Pairs = pairs;
    }

    public override ObjectType Type => ObjectType.Hash;

    public override string Inspect()
    {
        var pairs = new List<string>();
        foreach ((HashKey _, HashPair pair) in Pairs) pairs.Add($"{pair.Key.Inspect()}: {pair.Value.Inspect()}");
        return $"{{{string.Join(", ", pairs)}}}";
    }
}

public class Builtin : Object
{
    public readonly BuiltinFunction Function;

    public Builtin(BuiltinFunction function)
    {
        Function = function;
    }

    public override ObjectType Type => ObjectType.Builtin;

    public override string Inspect()
    {
        return "Builtin Function";
    }
}

public class Function : Object
{
    public readonly BlockStatement Body;
    public readonly Environment Environment;
    public readonly Identifier[] Parameters;

    public Function(Identifier[] parameters, BlockStatement body, Environment environment)
    {
        Parameters = parameters;
        Body = body;
        Environment = environment;
    }

    public override ObjectType Type => ObjectType.Function;

    public override string Inspect()
    {
        List<string> parameters = Parameters.Select(parameter => parameter.ToString()).ToList();

        string output = $"fn({string.Join(", ", parameters)})" + "{\n";
        output += Body.ToString();
        output += "\n}";

        return output;
    }
}

public class Null : Object
{
    public override ObjectType Type => ObjectType.Null;

    public override string Inspect()
    {
        return "null";
    }
}
