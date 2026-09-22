using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Absyn;
using Lib;
using Microsoft.FSharp.Core;
using Unit = Lib.Unit;

namespace DoC.Sema.Bind;

public class Binder : AstVisitor<LocedAst, BoundAst>
{
    protected BoundAst Result;
    
    protected ScopedMap<FunDec> FunDecs = new();
    protected ScopedMap<TypeDec> TypeDecs = new();
    protected ScopedMap<FormalDec> VarDecs = new();

    public (BoundAst, DocError) Bind()
    {
        return Work();
    }
    
    public override (BoundAst, DocError) Work()
    {
        EnterScope();
        Result.Decs.ForEach(e => Visit(e));
        EnterScope();
        return (Result, DocError);
    }

    public Binder(LocedAst ast) : base(ast)
    {
        Result = new BoundAst(ast, [], []);
    }

    protected void Undeclared(SimpleVar var)
    {
        DocError.Error(Result.Locate(var), $"Undeclared variable: \"{var.Name}\"");
    }

    protected Unit SimpleVar(SimpleVar var)
    {
        var varDec = VarDecs.Lookup(var.Name, out var varDepth);
        var funDec = FunDecs.Lookup(var.Name, out var funDepth);

        // none
        if ((funDec.IsNone() && varDec.IsNone()))
        {
            return Unit.Do(() => Undeclared(var));
        }

        if (funDec.IsSome(out var fDec) && varDec.IsSome(out var vDec) && funDepth == varDepth)
        {
            Result.Def(var, fDec);
            Result.Def(var, vDec);
        }
        else
        {
            IdDec theDec = varDepth > funDepth ? varDec.Value : funDec.Value;
            Result.Def(var, theDec);
        }

        return Unit.Value;
    }
    
     

    protected Unit NameTy(NameTy nameTy)
    {
        // fixme :D
        if (nameTy.Name is "i32" or "string")
        {
            return Unit.Value;
        }
        var typeDec = TypeDecs.Lookup(nameTy.Name, out _);

        // error
        if (typeDec.IsNone())
            return Unit.Value;

        Result.Def(nameTy, typeDec.Value);
        return Unit.Value;
    }

    protected void EnterScope()
    {
        TypeDecs.Enter();
        FunDecs.Enter();
        VarDecs.Enter();
    }

    protected void ExitScope()
    {
        TypeDecs.Exit();
        FunDecs.Exit();
        VarDecs.Exit();
    }
    
    public override Unit Visit(Node node) => Unit.Do(() =>
    {
       // Console.WriteLine($"Binding {node}");
    }) | node switch
    {
        // todo redeclaration
        SimpleVar simpleVar => SimpleVar(simpleVar),
        FormalDec formalDec => Unit.Do(() => VarDecs.Add(formalDec.Name, formalDec)) | base.Visit(formalDec),
        FunDec funDec => Unit.Do(() => FunDecs.Add(funDec.Name, funDec)) | Unit.Do(EnterScope) |
                         Visit(funDec.Formals) | Visit(funDec.Stmts) | Unit.Do(ExitScope),
        TypeDec typeDec => Unit.Do(() => TypeDecs.Add(typeDec.Name, typeDec)),
        IfStm ifStm => Visit(ifStm.Cond) | Unit.Do(EnterScope) | Visit(ifStm.Then) | Unit.Do(ExitScope) |
                       Unit.If(ifStm.Else.IsSome(),
                           () => Unit.Do(EnterScope) | Visit(ifStm.Else) | Unit.Do(ExitScope)),
        NameTy nameTy => NameTy(nameTy),
        Block stmList => Unit.Do(EnterScope) | Visit(stmList.Stmts) | Unit.Do(ExitScope),
        _ => base.Visit(node)
    };


    protected class ScopedMap<T> where T : Dec
    {
        protected readonly Stack<Dictionary<string, T>> Stack = [];

        public void Enter()
        {
            Stack.Push([]);
        }

        public void Exit()
        {
            Debug.Assert(Stack.Count > 0);
            Stack.Pop();
        }

        public void Add(string v, T dec)
        {
            Debug.Assert(Stack.Count > 0);
            Stack.Peek()[v] = dec;
        }

        public FSharpOption<T> Lookup(string name, out int depth) => TryLookup(name, out var dec, out depth) ? dec : FSharpOption<T>.None;
        
        public bool TryLookup(string name, [MaybeNullWhen(false)] out T dec, out int depth)
        {
            depth = 0;
            dec = null;
            foreach (var map in Stack)
            {
                if (map.TryGetValue(name, out dec))
                    return true;
                depth++;
            }

            depth = -1;
            return false;
        }
    }
}