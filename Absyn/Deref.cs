using System.Diagnostics.CodeAnalysis;
using Microsoft.FSharp.Core;
using Unit = Lib.Unit;

namespace Absyn;

public static class Deref
{
    extension<T>(FSharpOption<T> e)
    {
        public bool IsSome()
        {
            return OptionModule.IsSome(e);
        }
        
        public bool IsSome([MaybeNullWhen(false)] out T value)
        {
            if (e.IsSome())
            {
                value = e.Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool IsNone()
        {
            return OptionModule.IsNone(e);
        }

        public Unit If(Func<T, Unit> action)
        {
            return e.IsSome() ? action(e.Value) : Unit.Value;
        }
        
        public static FSharpOption<T> Of(T? opt)
        {
            return opt == null ? FSharpOption<T>.None : new FSharpOption<T>(opt);
        }
    }
}