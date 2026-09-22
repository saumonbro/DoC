namespace Lib;

public struct Unit
{
    public static Unit Value => default;

    public static Unit operator |(Unit _1, Unit _2)
    {
        return Value;
    }

    public static Unit Foreach<T>(IEnumerable<T> nodes, Func<T, Unit> action)
    {
        foreach (var node in nodes)
            action(node);
        return Value;
    }
    
    public static Unit Foreach<T, T2, T3>(IEnumerable<T> nodes, Func<T, Func<T, T2>, T3> action, Func<T, T2> callback)
    {
        foreach (var node in nodes)
        {
            action(node, callback);
        }

        return Value;
    }

    public static Unit Do(Action a)
    {
        a();
        return Value;
    }
    
    public static Unit Do<TIn, TOut>(Func<TIn, TOut> func, params TIn[] ins)
    {
        foreach (var a in ins)
        {
            func(a);
        }
        return Value;
    }

    public static Unit If(bool condition, Func<Unit> action)
    {
        return condition ? action() : Value;
    }
}

public static class UnitExtensions
{
    public static Unit Do(this Action a)
    {
        a();
        return Unit.Value;
    }
}