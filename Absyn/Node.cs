using Microsoft.FSharp.Core;

namespace Absyn;

using OptTy = FSharpOption<Ty>;

[ReferenceEquality]
public abstract record Node;

public abstract record Exp : Node;

public record SeqExp(List<Exp> Exps) : Exp;

public record IntExp(string Value) : Exp;

public record StringExp(string Value) : Exp;

public record AssignExp(Var Var, Exp Value) : Exp;

public record IfExp(Exp Cond, Exp Then, Exp Else) : Exp;

public record CallExp(Exp Callee, List<Exp> Args) : Exp;

public record FieldInit(string Name, Exp Init) : Node;

public record RecordExp(List<FieldInit> Fields) : Exp;

public record ArrayExp(List<Exp> Inits) : Exp;
public record ArrayRepeatExp(Exp Init, Exp Size) : Exp;

public record Var : Exp;

public record SimpleVar(string Name) : Var;

public record FieldVar(Exp Left, string Field) : Var;

public record SubscriptVar(Exp Left, Exp Idx) : Var;

public record OpExp(Exp Lhs, OpExp.Oper Op, Exp Rhs) : Exp
{
    public enum Oper
    {
        Plus,
        Minus,
        Mul,
        Div,
        Mod,
        Eq,
        Neq,
        Gt,
        Ge,
        Lt,
        Le
    }
}

public abstract record Stm : Node;

public record ReturnStm(Exp Exp) : Stm;

public record IfStm(Exp Cond, Stm Then, FSharpOption<Stm> Else) : Stm;

public record Block(List<Stm> Stmts) : Stm;

public record Sxp(Exp Exp) : Stm;

public abstract record Dec : Stm;

public abstract record IdDec(string Name) : Dec;

public record FunDec(string Name, List<FormalDec> Formals, OptTy Ty, FSharpOption<Block> Stmts, bool Pure = false)
    : IdDec(Name);

public record FormalDec(string Name, OptTy Ty) : IdDec(Name);

public record FieldDec(string Name, OptTy Ty) : Node;


public record VarDec(string Name, OptTy Ty, Exp Init) : FormalDec(Name, Ty);

public record TypeDec(string Name, Ty Ty) : Dec;

public abstract record Ty : Node;

/// fn NotZero(i : i32) => i != 0;
/// type a = { a : i32[]; b : i32 with NotZero; }
public record NameTy(string Name) : Ty;

/// <summary>
/// </summary>
/// <inheritdoc />
/// <param name="Type">The type to apply Predicate to</param>
/// <param name="Predicate">
///     The restriction applied to Type. Predicate takes an argument of type `Type` and returns a
///     `bool`
/// </param>
public record RestrictTy(Ty Type, string Predicate) : Ty;

/// <summary>
/// </summary>
/// <param name="Fields"></param>
public record RecordTy(IList<FieldDec> Fields) : Ty;

public record ArrayTy(Ty ElementType) : Ty;