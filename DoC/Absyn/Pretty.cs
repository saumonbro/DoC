using Absyn;
using Microsoft.FSharp.Core;

namespace DoC.Absyn;

public static class Pretty
{
    private const string IndentUnit = "    ";

    private static string Print(Node node)
    {
        return P(node, 0);
    }

    public static void PrettyPrint(Node node)
    {
        Console.WriteLine(Print(node));
    }

    public static string PrintProgram(Ast program)
    {
        return string.Join("\n\n", program.Decs.Select(d => P(d, 0)));
    }

    private static string Indent(int level)
    {
        return string.Concat(Enumerable.Repeat(IndentUnit, level));
    }

    private static string Ps<T>(IList<T> ns, string sep, int indent = 0) where T : Node
    {
        return string.Join(sep, ns.Select(n => P(n, indent)));
    }

    private static string OperToString(OpExp.Oper op)
    {
        return op switch
        {
            OpExp.Oper.Plus => "+",
            OpExp.Oper.Minus => "-",
            OpExp.Oper.Mul => "*",
            OpExp.Oper.Div => "/",
            OpExp.Oper.Mod => "%",
            OpExp.Oper.Eq => "=",
            OpExp.Oper.Neq => "!=",
            OpExp.Oper.Gt => ">",
            OpExp.Oper.Ge => ">=",
            OpExp.Oper.Lt => "<",
            OpExp.Oper.Le => "<=",
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };
    }

    private static string P(Node n, int indent)
    {
        return n switch
        {
            // Expressions
            IntExp intExp => intExp.Value,
            StringExp stringExp => $"\"{stringExp.Value}\"",
            SimpleVar simpleVar => simpleVar.Name,
            FieldVar fieldVar => $"{P(fieldVar.Left, indent)}.{fieldVar.Field}",
            SubscriptVar subscriptVar => $"{P(subscriptVar.Left, indent)}[{P(subscriptVar.Idx, indent)}]",
            AssignExp assignExp => $"{P(assignExp.Var, indent)} := {P(assignExp.Value, indent)}",
            CallExp callExp => $"{P(callExp.Callee, indent)}({Ps(callExp.Args, ", ", indent)})",
            OpExp opExp => $"({P(opExp.Lhs, indent)} {OperToString(opExp.Op)} {P(opExp.Rhs, indent)})",
            IfExp ifExp => PrintIfExp(ifExp, indent),
            SeqExp seqExp => "(" + Ps(seqExp.Exps, "; ", indent) + ")",
            RecordExp recordExp => $"{{ {Ps(recordExp.Fields, ", ", indent)} }}",
            ArrayExp arrayExp => $"[{Ps(arrayExp.Inits, ", ", indent)}]",
            ArrayRepeatExp arrayRepeatExp => $"[{P(arrayRepeatExp.Size, indent)}; {P(arrayRepeatExp.Init, indent)}]",
            FieldInit fieldInit => $"{fieldInit.Name} = {P(fieldInit.Init, indent)}",

            // Types
            NameTy nameTy => nameTy.Name,
            ArrayTy arrayTy => $"{P(arrayTy.ElementType, indent)}[]",
            RestrictTy restrictTy => $"{P(restrictTy.Type, indent)} with {restrictTy.Predicate}",
            RecordTy recordTy => PrintRecordTy(recordTy, indent),

            // Declarations
            VarDec varDec => PrintVarDec(varDec, indent),
            FormalDec formalDec => $"{formalDec.Name} : {P(formalDec.Ty.Value, indent)}",
            TypeDec typeDec => $"type {typeDec.Name} := {P(typeDec.Ty, indent)}",
            FunDec funDec => PrintFunDec(funDec, indent),

            // Statements
            ReturnStm returnStm => $"return {P(returnStm.Exp, indent)}",
            IfStm ifStm => PrintIfStm(ifStm, indent),
            Block stmList => PrintStmList(stmList, indent),
            Sxp sxp => P(sxp.Exp, indent),
            FieldDec fieldDec => $"{fieldDec.Name} : {P(fieldDec.Ty.Value, indent)}",

            _ => throw new ArgumentOutOfRangeException(nameof(n), n.GetType().Name)
        };
    }

    private static string PrintIfExp(IfExp ifExp, int indent)
    {
        return $"({P(ifExp.Cond, indent)}) ? {P(ifExp.Then, indent)}"
               + $" : {P(ifExp.Else, indent)}";
    }

    private static string PrintRecordTy(RecordTy recordTy, int indent)
    {
        return recordTy.Fields.Count == 0
            ? "{ }"
            : "{ " + Ps(recordTy.Fields, ", ", indent)
                   + $" {Indent(indent)}}}";
    }

    private static string PrintVarDec(VarDec varDec, int indent)
    {
        return $"let {varDec.Name}"
               + (varDec.Ty.IsSome(out var ty) ? $" : {P(ty, indent)}" : "")
               + $" := {P(varDec.Init, indent)}";
    }

    private static string PrintFunDec(FunDec funDec, int indent)
    {
        var pref = funDec.Pure ? "fun" : "proc";

        string res = $"{pref} {funDec.Name}({Ps(funDec.Formals, ", ", indent)})"
                     + (funDec.Ty.IsSome(out var retTy) ? $" : {P(retTy, indent)}" : "");

        if (funDec.Stmts.IsSome(out var block))
        {
            return block.Stmts is [ReturnStm ret]
                ? res + $" => {P(ret.Exp, indent)}"
                : res + $"\n{PrintStmList(block, indent)}";
        }

        return res + ";";
    }

    private static string PrintIfStm(IfStm ifStm, int indent)
    {
        var thenPart = ifStm.Then is Block thenList
            ? $"\n{PrintStmList(thenList, indent)}"
            : $"\n{Indent(indent + 1)}{P(ifStm.Then, indent + 1)};";

        if (ifStm.Else.IsSome(out var elseStm))
        {
            var elsePart = elseStm switch
            {
                Block elseList => $"\n{Indent(indent)}else\n{PrintStmList(elseList, indent)}",
                IfStm elseIf => $"\n{Indent(indent)}else {PrintIfStm(elseIf, indent)}",
                _ => $"\n{Indent(indent)}else\n{Indent(indent + 1)}{P(elseStm, indent + 1)};"
            };

            return $"if ({P(ifStm.Cond, indent)}){thenPart}{elsePart}";
        }

        return $"if ({P(ifStm.Cond, indent)}){thenPart}";
    }

    private static string PrintStmList(Block block, int indent)
    {
        var ind = Indent(indent + 1);
        var stmts = string.Join("\n", block.Stmts.Select(stm => stm switch
        {
            IfStm ifStm => $"{ind}{PrintIfStm(ifStm, indent + 1)};",
            FunDec funDec => $"{ind}{PrintFunDec(funDec, indent + 1)}",
            TypeDec => $"{ind}{P(stm, indent + 1)}",
            _ => $"{ind}{P(stm, indent + 1)};"
        }));

        return $"{Indent(indent)}{{\n{stmts}\n{Indent(indent)}}}";
    }
}