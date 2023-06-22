using System.Diagnostics.CodeAnalysis;

namespace Monkey;

public class Environment
{
    private readonly Environment? _outer;
    private readonly Dictionary<string, Object> _store;

    public Environment()
    {
        _store = new Dictionary<string, Object>();
        _outer = null;
    }

    public Environment(Environment outer)
    {
        _store = new Dictionary<string, Object>();
        _outer = outer;
    }

    public bool TryGetValue(string name, [MaybeNullWhen(false)] out Object value)
    {
        if (_store.TryGetValue(name, out value)) return true;
        return _outer != null && _outer.TryGetValue(name, out value);
    }

    public void Add(string name, Object value)
    {
        _store.Add(name, value);
    }
}
