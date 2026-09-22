using Absyn;
using Lib;

namespace DoC.Sema.Type;

public class TypedAst : BoundAst
{
    public Dictionary<Node, DocType> Types;

    public TypedAst(BoundAst bound, Dictionary<Node, DocType> types) : base(bound)
    {
        Types = types;
    }

    public TypedAst(TypedAst other) : base(other)
    {
        Types = other.Types;
    }

    public DocType Type(Node v) => Types[v];
    public void Type(Node v, DocType def) => Types.Add(v, def);
}

public class TypeChecker : AstVisitor<BoundAst, TypedAst>
{
    protected TypedAst Result;

    public TypeChecker(BoundAst boundAst) : base(boundAst)
    {
        Result = new TypedAst(boundAst, []);
    }

    public (TypedAst, DocError) TypeCheck()
    {
        Result.Decs.ForEach(e => Visit(e));
        return (Result, DocError);
    }

    public DocType Check(Node node, DocType expect)
    {
        //return Infer(node).CompatibleWith(expect);
        return null;
    }

    protected ErrorTy Error(string s, Node? node)
    {
        if (node != null)
            DocError.Error(Result.Locate(node), s);
        else
            DocError.Error(s);

        return ErrorTy.Instance;
    }

    protected bool CheckTrait(DocType type, Trait trait, Node node)
    {
        if (!type.HasTrait(trait))
        {
            Error($"Expression does not have trait {trait}", node);
            return false;
        }

        return true;
    }

    protected List<(DocType, DocType)> StructurallyRelates(List<DocType> a, List<DocType> b)
    {
        List<(DocType, DocType)> comb = [];
        foreach (var aty in a)
        {
            comb.AddRange(b.Select(bty => (aty, bty)));
        }

        return comb.Where((x) => StructurallyRelates(x.Item1, x.Item2)).ToList();
    }


    protected bool StructurallyRelates(DocType a, DocType b)
    {
        return (a, b) switch
        {
            (Struct s1, Struct s2) => Do(() =>
            {
                if (s1.Fields.Count != s2.Fields.Count)
                    return false;

                foreach (var (f1, f2) in s1.Fields.Zip(s2.Fields, (f1, f2) => (f1, f2)))
                {
                    // should i check the names ?
                    if (!StructurallyRelates(f1.Item2, f2.Item2))
                    {
                        Error($"Fields {f1} and {f2} cannot be related", null);
                        return false;
                    }
                }

                return true;
            }),
            (Int i2, Int i1) => Do((() => { return i2.SizeOf == i1.SizeOf; })),
            (Uint i2, Uint i1) => Do((() => { return i2.SizeOf == i1.SizeOf; })),
            (String s1, String s2) => Do((() => { return true; })),
            (_, ErrorTy) or (ErrorTy, _) => true,
            (_, Any) or (Any, _) => true,
            _ => false// throw new NotImplementedException($"Not implemented for {a}, {b}")
        };
    }

    protected DocType BestCommonType(params DocType[] types)
    {
        DocType unifix = new Any();
        foreach (var type in types)
        {
            
        }

        return unifix;
    }

    public List<IdDec> DecOf(SimpleVar v) => Result.Def(v);
    public DocType TypeOf(Node v) => Result.Type(v);
    public void TypeOf(Node v, DocType def) => Result.Type(v, def);

    class ReturnVisitor : DefaultVisitor
    {
        public List<ReturnStm> Returns { get; protected set; } = [];

        public override Unit Visit(Node node)
        {
            // if closures are implemented, have a special case to no recurse on lambda exp to ignore the code from here
            if (node is ReturnStm returnStm)
            {
                Returns.Add(returnStm);
            }

            return base.Visit(node);
        }
    }

    public DocType ReturnType(FunDec fun)
    {
        DocType expect = fun.Ty.IsSome(out var outTy) ? TypeOf(outTy) : Void.Instance;

        if (fun.Stmts.IsSome(out var block))
        {
            ReturnVisitor returnVisitor = new ReturnVisitor();
            returnVisitor.Visit(block);
            var returns = returnVisitor.Returns;

            DocType res = expect;
            foreach (var ret in returns)
            {
                if (!StructurallyRelates(TypeOf(ret.Exp), expect))
                {
                    res = Error($"Invalid return type (expected {expect}, but got {TypeOf(ret.Exp)})", ret);
                }
            }

            return res;

        }
        return expect;
    }

    protected Dictionary<Node, DocType> Types = [];


    public DocType Infer(Node node)
    {
        if (Types.TryGetValue(node, out var type))
        {
            return type;
        }

        var theDocType = node switch
        {
            AssignExp assignExp => Do((() =>
            {
                var varTy = Infer(assignExp.Var);
                var expTy = Infer(assignExp.Value);


                var relates = StructurallyRelates(varTy, expTy);

                if (!relates)
                {
                    return ErrorTy.Instance;
                }

                return varTy;
            })),
            CallExp callExp => Do<DocType>(() =>
            {
                var funType = Infer(callExp.Callee);

                // is callable ?
                if (funType is not Function fun)
                {
                    return Error("Expression is not callable", callExp);
                }

                // are arguments compatible ?
                if (fun.Args.Count != callExp.Args.Count)
                {
                    return Error($"Expected {fun.Args.Count} but got {callExp.Args.Count}", callExp);
                }

                foreach (var arg in callExp.Args)
                {
                    var argTy = Infer(arg);
                    if (!StructurallyRelates(argTy, Infer(arg)))
                    {
                        return ErrorTy.Instance;
                    }
                }

                return fun.Return;
            }),
            FieldVar fieldVar => Do(() =>
            {
                var expTy = Infer(fieldVar.Left);
                if (expTy is Struct record)
                {
                    var fieldType = record.FieldType(fieldVar.Field);
                    if (fieldType == null)
                    {
                        return Error($"Invalid field {fieldVar.Field}", fieldVar);
                    }

                    return fieldType;
                }
                else
                {
                    return Error("Cannot access field on non-struct type", fieldVar);
                }
            }),
            IfExp ifExp => throw new NotImplementedException(),
            IntExp intExp => Int.I32,
            OpExp opExp => Do((() =>
            {
                // Is there an oper for x oper y (when there will be operator overloading. For now its only ints)
                var lhs = Infer(opExp.Lhs);
                var rhs = Infer(opExp.Rhs);

                if (!StructurallyRelates(lhs, Int.I32) || !StructurallyRelates(rhs, Int.I32))
                {
                    return Error($"Cannot apply {opExp.Op} to operands of type {lhs} and {rhs}", opExp);
                }

                return lhs;
            })),
            SeqExp seqExp => seqExp.Exps.Count == 0 ? Void.Instance : seqExp.Exps.Select(Infer).Last(),
            StringExp stringExp => new String(),
            SimpleVar simpleVar => TypeOf(DecOf(simpleVar).First()),
            SubscriptVar subscriptVar => throw new NotImplementedException(),
            VarDec varDec => Do(() =>
            {
                var defedTy = Infer(varDec.Init);
                if (varDec.Ty.IsNone())
                    return defedTy;

                var declaredTy = Infer(varDec.Ty.Value);
                if (!StructurallyRelates(defedTy, declaredTy))
                {
                    return Error($"Cannot initialize variable of type {declaredTy} from {defedTy}", varDec);
                }

                return declaredTy;
            }),
            FormalDec formalDec => Do((() => { return Infer(formalDec.Ty.Value); })),
            FunDec funDec => Do<DocType>((() =>
            {
                if (Result.Types.TryGetValue(funDec, out var theTy))
                {
                    return theTy;
                }

                DocType resTy = Void.Instance;
                if (funDec.Ty.IsSome())
                {
                    resTy = Infer(funDec.Ty.Value);
                }

                List<DocType> formalTys = [];

                formalTys.AddRange(funDec.Formals.Select(Infer));

                Function funTy = new Function(formalTys, resTy);
                Result.Types[funDec] = funTy;

                if (funDec.Stmts.IsNone())
                    return funTy;

                var _ = Infer(funDec.Stmts.Value);

                return ReturnType(funDec);
            })),
            TypeDec typeDec => Do((() =>
            {
                var theType = Infer(typeDec.Ty);
                return new Alias(typeDec.Name, theType);
            })),
            RecordExp recordExp => Do((() =>
            {
                List<(string, DocType)> fields = [];
                foreach (var field in recordExp.Fields)
                {
                    var tyType = Infer(field.Init);
                    fields.Add((field.Name, tyType));
                }

                return new Struct(fields);
            })),
            ArrayTy arrayTy => new Array(Infer(arrayTy.ElementType)),
            ArrayRepeatExp arrayRepeatExp => Do((() =>
            {
                
            })),
            ArrayExp arrayExp => Do((() =>
            {
                foreach (var inits in arrayExp.Inits)
                {
                    Infer(inits);
                }

                return new Array();
            })),
            IfStm ifStm => Do((() =>
            {
                var condTy = Infer(ifStm.Cond);
                var thenTy = Infer(ifStm.Then);
                var elseTy = ifStm.Else.IsSome(out var elseStm) ? Infer(elseStm) : Void.Instance;

                // FIXME: better checks for booleanization
                if (!StructurallyRelates(condTy, new Bool()) && !StructurallyRelates(condTy, new Int(32)))
                {
                    return Error($"Cannot use exp of type {condTy} as a boolean", ifStm.Cond);
                }

                if (!StructurallyRelates(thenTy, elseTy))
                {
                    return Error("Dissimilar types of if and else clauses", ifStm);
                }

                return thenTy;
            })),
            NameTy nameTy => nameTy.Name switch
            {
                "i32" => new Int(32),
                "string" => new String(), // FIXME lol
                _ => Result.Types[Result.Def(nameTy)],
            },
            RecordTy recordTy => Do((() =>
            {
                List<(string, DocType)> fields = [];
                foreach (var field in recordTy.Fields)
                {
                    var tyType = Infer(field.Ty.Value);
                    fields.Add((field.Name, tyType));
                }

                return new Struct(fields);
            })),
            RestrictTy restrictTy => throw new NotImplementedException(),
            Stm => Do(() =>
            {
                // anything that doesnt have a type in itself but needs to be visited: statements
                base.Visit(node);
                return new Any();
            }),
            _ => throw new NotImplementedException($"Not implemented {node}")
        };

        //Console.WriteLine($"Typing {node}");
        Result.Types[node] = theDocType;
        return theDocType;
    }

    public override (TypedAst, DocError) Work()
    {
        return TypeCheck();
    }

    public override Unit Visit(Node node)
    {
        Infer(node);
        return Unit.Value;
    }
}