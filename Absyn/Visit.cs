using Lib;
using Microsoft.FSharp.Core;
using Unit = Lib.Unit;

namespace Absyn;

public abstract class ReadonlyAstVisitor<TAst>(TAst theAst) : AstVisitor<TAst, TAst>(theAst) where TAst : Ast;


public class DefaultVisitor
{
    protected Unit Visit(params Node[] nodes) => Visit<Node>(nodes);
    protected Unit Visit<T>(IEnumerable<T> nodes) where T : Node => Unit.Foreach(nodes, Visit);
    protected Unit Visit<T>(params FSharpOption<T>[] nodes) where T : Node => Visit(nodes as IEnumerable<FSharpOption<T>>);
    protected Unit Visit<T>(IEnumerable<FSharpOption<T>> nodes) where T : Node => Unit.Foreach(nodes, opt => opt.If(Visit));

    public virtual Unit Visit(Node node)
    {
        return node switch
        {
            AssignExp assignExp => Visit(assignExp.Var, assignExp.Value),
            CallExp callExp => Visit(callExp.Callee) | Visit(callExp.Args),
            FieldVar fieldVar => Visit(fieldVar.Left),
            IfExp ifExp => Visit(ifExp.Cond, ifExp.Then, ifExp.Else),
            OpExp opExp => Visit(opExp.Lhs, opExp.Rhs),
            SeqExp seqExp => Visit(seqExp.Exps),
            SubscriptVar subscriptVar => Visit(subscriptVar.Left, subscriptVar.Idx),
            VarDec varDec => Visit(varDec.Ty) | Visit(varDec.Init),
            FormalDec formalDec => Visit(formalDec.Ty),
            FunDec funDec => Visit(funDec.Formals) | Visit(funDec.Ty) | Visit(funDec.Stmts),
            TypeDec typeDec => Visit(typeDec.Ty),
            ArrayTy arrayTy => Visit(arrayTy.ElementType),
            IfStm ifStm => Visit(ifStm.Cond, ifStm.Then) | Visit(ifStm.Else),
            RecordTy recordTy => Visit(recordTy.Fields),
            RestrictTy restrictTy => Visit(restrictTy.Type),
            ReturnStm returnStm => Visit(returnStm.Exp),
            Block stmList => Visit(stmList.Stmts),
            Sxp sxp => Visit(sxp.Exp),
            RecordExp recordExp => Visit(recordExp.Fields),
            ArrayExp arrayExp => Visit(arrayExp.Inits),
            ArrayRepeatExp arrayRepeatExp => Visit(arrayRepeatExp.Init, arrayRepeatExp.Size),
            FieldInit fieldInit => Visit(fieldInit.Init),
            SimpleVar or StringExp or NameTy or IntExp => Unit.Value,
            FieldDec fieldInit => Visit(fieldInit.Ty),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }
}


public abstract class AstVisitor<TIn, TOut>(TIn theAst) : DefaultVisitor where TIn : Ast
{
    protected TIn TheAst => theAst;

    protected DocError DocError = new ();
        
    public static T Do<T>(Func<T> action)
    {
        return action();
    }

    public abstract (TOut, DocError) Work();
}