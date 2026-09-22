using System.Diagnostics;
using Absyn;
using DoC.Syntax.Scan;
using Lib;
using Microsoft.FSharp.Core;

namespace DoC.Syntax.Parse;

/*
translation-unit:
    | decs

decs:
    | %empty
    | dec decs

dec:
    | type-dec
    | fun-dec
    | var-dec

type-dec:
    | 'type' ID ':=' ty

ty:
    | NameTy
    | RestrictTy
    | RecordTy
    | ArrayTy

name-ty:
    | ID

restrict-ty:
    | ty 'with' ID

record-ty:
    | '{' formal-decs '}'

formal-dec:
    | ID ':' ty

formal-decs.1:
    | formal-decs ',' formal-dec
    | formal-dec

formal-decs:
    | %empty
    | formal.decs.1

fun-dec:
    | 'fn' ID '(' formal-decs ')' ':' ty '=>' Exp
    | 'fn' ID '(' formal-decs ')' => Exp
    | 'fn' ID '(' formal-decs ')' ':' ty StmList
    | 'fn' ID '(' formal-decs ')' StmList

stm-list.1:
    | stm-list.1 stm ';'
    | stm ';'

stm-list:
    | '{' '}'
    | '{' stm-list.1 '}'

stm:
    | dec
    | if exp stm else stm
    | stm-list

exp:
    | INT
    | STRING
    | exp op exp
    | exp '?' exp ':' exp

exp:

*/

public class Parser(string filename, string input)
{
    protected readonly Lexer Lexer = new(filename, input);
    protected DocError _error = new DocError();

    protected Token CurrentToken => Lexer.Peek();

    protected LocedAst Result = new([], []);

    protected T Locate<T>(T n, Location loc) where T : Node
    {
        Result.Locate(n, loc);
        return n;
    }

    protected Token LookAhead(int n)
    {
        return Lexer.LookAhead(n);
    }

    protected Token Eat(TokenType type)
    {
        if (CurrentToken.Type != type)
        {
            AddError($"Expected {type} at {CurrentToken}.", CurrentToken.Location);

        }
            //throw new Exception($"Expected {type} at {CurrentToken}.");
        return Lexer.Pop();
    }

    protected Token EatOneOf(params TokenType[] types)
    {
        if (types.All(t => CurrentToken.Type != t)) throw new Exception($"Expected {types[0]} at {CurrentToken}.");
        return Lexer.Pop();
    }

    public (LocedAst, DocError) Program()
    {
        while (CurrentToken.Type != TokenType.None) Result.Decs.Add(Dec());

        return (Result, _error);
    }

    protected void AddError(string msg, Location? loc)
    {
        if (loc != null)
            _error.Error(loc.Value, msg);
        else
            _error.Error(msg);
    }

    protected Exception Error(string msg = "")
    {
        return new Exception($"Invalid token {CurrentToken}: {msg}");
    }

    /// <summary>
    ///     let, fun, type
    /// </summary>
    /// <returns></returns>
    public Dec Dec()
    {
        return CurrentToken.Type switch
        {
            TokenType.Let => VarDec(),
            TokenType.Fun => FunDec(),
            TokenType.Proc => FunDec(),
            TokenType.Type => TypeDec(),
            _ => throw Error()
        };
    }

    public Ty Facty()
    {
        switch (CurrentToken.Type)
        {
            // Record
            case TokenType.LBrace:
            {
                var loc = Eat(TokenType.LBrace).Location;
                var ty = Locate(new RecordTy(Formals().Select(e => new FieldDec(e.Name, e.Ty)).ToList()), loc);
                Eat(TokenType.RBrace);
                return ty;
            }
            case TokenType.Identifier:
            {
                var token = Eat(TokenType.Identifier);
                return Locate(new NameTy(token.Value), token.Location);
            }
            default:
                throw Error();
        }
    }

    public Ty Array()
    {
        var left = Facty();
        var loc = Result.Locate(left);
        while (CurrentToken.Type == TokenType.LBracket)
        {
            Eat(TokenType.LBracket);
            Eat(TokenType.RBracket);
            left = Locate(new ArrayTy(left), loc);
        }

        return left;
    }

    public Ty Restrict()
    {
        var left = Array();
        while (CurrentToken.Type == TokenType.With)
        {
            var loc = Eat(TokenType.With).Location;
            var id = Eat(TokenType.Identifier);
            left = Locate(new RestrictTy(left, id.Value), loc);
        }

        return left;
    }

    /// <summary>
    ///     i32 with NotZero[] with NotEmpty
    ///     ((i32 with NotZero)[]) with NotEmpty
    /// </summary>
    /// <returns></returns>
    public Ty Ty()
    {
        // recursive !
        return Restrict();
    }

    public FormalDec Formal()
    {
        var id = Eat(TokenType.Identifier);
        Eat(TokenType.Colon);
        var type = Ty();
        return Locate(new FormalDec(id.Value, type), id.Location);
    }

    /// <summary>
    ///     formal-dec:
    ///     | ID ':' ty
    ///     formal-decs.1:
    ///     | formal-decs ',' formal-dec
    ///     | formal-dec
    ///     formal-decs:
    ///     | %empty
    ///     | formal.decs.1
    /// </summary>
    /// <returns></returns>
    public List<FormalDec> Formals()
    {
        var res = new List<FormalDec>();

        while (CurrentToken.Type == TokenType.Identifier)
        {
            res.Add(Formal());
            if (CurrentToken.Type == TokenType.Comma)
                Eat(TokenType.Comma);
            else
                break;
        }

        return res;
    }

    public IfStm If()
    {
        var loc = Eat(TokenType.If).Location;
        var condition = Exp();

        var inThisCase = Stm();
        Stm? otherwise = null;
        if (CurrentToken.Type == TokenType.Else)
        {
            Eat(TokenType.Else);
            otherwise = Stm();
        }

        return Locate(new IfStm(condition, inThisCase, FSharpOption<Stm>.Of(otherwise)), loc);
    }

    public ReturnStm Return()
    {
        var loc = Eat(TokenType.Return).Location;
        return Locate(new ReturnStm(Exp()), loc);
    }

    /*
     * stm:
       | dec
       | if exp stm else stm
       | stm-list
     */
    public Stm Stm()
    {
        switch (CurrentToken.Type)
        {
            case TokenType.Let:
            case TokenType.Fun:
            case TokenType.Proc:
            case TokenType.Type:
                return Dec();
            case TokenType.If:
                return If();
            case TokenType.Return:
                return Return();
            // may be a record exp ! { ID : value, ... }
            case TokenType.LBrace:
                if (LookAhead(1).Type == TokenType.Identifier && LookAhead(2).Type == TokenType.Colon)
                {
                    var exp1 = Exp();
                    return Locate(new Sxp(exp1), Result.Locate(exp1));
                }
                else
                {
                    return Stmts();
                }
            default:
                var exp = Exp();
                return Locate(new Sxp(exp), Result.Locate(exp));
        }
    }

    public Block Stmts()
    {
        var loc = Eat(TokenType.LBrace).Location;
        List<Stm> res = [];
        while (CurrentToken.Type != TokenType.RBrace)
        {
            res.Add(Stm());
            Eat(TokenType.Semicolon);
        }

        Eat(TokenType.RBrace);
        return Locate(new Block(res), loc);
    }

    public TypeDec TypeDec()
    {
        var loc = Eat(TokenType.Type).Location;
        var id = Eat(TokenType.Identifier).Value;

        Eat(TokenType.Assign);
        return Locate(new TypeDec(id, Ty()), loc);
    }

    /// <summary>
    ///     fun-dec:
    ///     | 'fn' ID '(' formal-decs ')' ':' ty '=>' Exp
    ///     | 'fn' ID '(' formal-decs ')' => Exp
    ///     | 'fn' ID '(' formal-decs ')' ':' ty StmList
    ///     | 'fn' ID '(' formal-decs ')' StmList
    /// </summary>
    /// <returns></returns>
    public FunDec FunDec()
    {
        var pure = CurrentToken.Type != TokenType.Proc;

        var token = EatOneOf(TokenType.Fun, TokenType.Proc);
        var id = Eat(TokenType.Identifier).Value;
        Eat(TokenType.LParen);
        var formals = Formals();
        Eat(TokenType.RParen);

        Ty? ty = null;

        if (CurrentToken.Type == TokenType.Colon)
        {
            Eat(TokenType.Colon);
            ty = Ty();
        }

        // declaration
        if (CurrentToken.Type == TokenType.Semicolon)
        {
            Eat(TokenType.Semicolon);
            return Locate(new FunDec(id, formals, FSharpOption<Ty>.Of(ty), FSharpOption<Block>.None, pure),
                token.Location);
        }


        // => Exp
        Block stmts;

        if (CurrentToken.Type == TokenType.Arrow)
        {
            var loc = Eat(TokenType.Arrow).Location;
            var exp = Exp();
            stmts = Locate(new Block([Locate(new ReturnStm(exp), loc)]), loc);
        }
        else
        {
            stmts = Stmts();
        }

        return Locate(new FunDec(id, formals, FSharpOption<Ty>.Of(ty), stmts, pure), token.Location);
    }

    public VarDec VarDec()
    {
        var loc = Eat(TokenType.Let).Location;
        var id = Eat(TokenType.Identifier);
        Ty? ty = null;
        if (CurrentToken.Type == TokenType.Colon) // explicit type
        {
            Eat(TokenType.Colon);
            ty = Ty();
        }

        Eat(TokenType.Assign);
        var init = Exp();
        return Locate(new VarDec(id.Value, FSharpOption<Ty>.Of(ty), init), loc);
    }


    public List<Exp> Exps()
    {
        List<Exp> exps = [Exp()];
        while (CurrentToken.Type == TokenType.Semicolon)
        {
            Eat(TokenType.Semicolon);
            exps.Add(Exp());
        }

        return exps;
    }

    // either [exp, exp...]
    // or [exp;exp] 
    public Exp ArrayExp()
    {
        var loc = Eat(TokenType.LBracket).Location;
        if (CurrentToken.Type == TokenType.RBracket)
        {
            Eat(TokenType.RBracket);
            return new ArrayExp([]);
        }

        List<Exp> exps = [Exp()];
        if (CurrentToken.Type == TokenType.Semicolon)
        {
            Eat(TokenType.Semicolon);
            var init = Exp();
            Eat(TokenType.RBracket);
            return Locate(new ArrayRepeatExp(init, exps[0]), loc);
        }

        while (CurrentToken.Type == TokenType.Comma)
        {
            Eat(TokenType.Comma);
            exps.Add(Exp());
        }

        Eat(TokenType.RBracket);

        return Locate(new ArrayExp(exps), loc);
    }

    public RecordExp RecordExp()
    {
        var loc = Eat(TokenType.LBrace).Location;
        List<FieldInit> inits = [];
        while (CurrentToken.Type != TokenType.RBrace)
        {
            Token id = Eat(TokenType.Identifier);
            Eat(TokenType.Colon);
            Exp init = Exp();
            FieldInit field = Locate(new FieldInit(id.Value, init), id.Location);
            inits.Add(field);
            if (CurrentToken.Type == TokenType.Comma)
                Eat(TokenType.Comma);
        }
        Eat(TokenType.RBrace);
        return Locate(new RecordExp(inits), loc);
    }

    public Exp Factor()
    {
        switch (CurrentToken.Type)
        {
            case TokenType.Number:
            {
                var tok = Eat(TokenType.Number);
                return Locate(new IntExp(tok.Value), tok.Location);
            }
            case TokenType.Identifier:
            {
                var tok = Eat(TokenType.Identifier);
                return Locate(new SimpleVar(tok.Value), tok.Location);
            }
            case TokenType.String:
            {
                var tok = Eat(TokenType.String);
                return Locate(new StringExp(tok.Value), tok.Location);
            }
            case TokenType.LParen:
            {
                var loc = Eat(TokenType.LParen).Location;
                Exp res = Locate(new SeqExp(CurrentToken.Type == TokenType.RParen ? [] : Exps()), loc);
                Eat(TokenType.RParen);
                return res;
            }
            case TokenType.LBrace:
            {
                return RecordExp();
            }
            case TokenType.LBracket:
            {
                return ArrayExp();
            }
            default:
                throw Error("Expect one of : TODO");
        }
    }

    public Exp Call()
    {
        var callee = Factor();

        while (new List<TokenType> { TokenType.Dot, TokenType.LBracket, TokenType.LParen }.Contains(CurrentToken.Type))
            if (CurrentToken.Type == TokenType.Dot)
            {
                Eat(TokenType.Dot);
                var field = Eat(TokenType.Identifier).Value;
                callee = Locate(new FieldVar(callee, field), Result.Locate(callee));
            }
            else if (CurrentToken.Type == TokenType.LBracket)
            {
                Eat(TokenType.LBracket);
                var exp = Exp();
                Eat(TokenType.RBracket);
                callee = Locate(new SubscriptVar(callee, exp), Result.Locate(callee));
            }
            else if (CurrentToken.Type == TokenType.LParen)
            {
                Eat(TokenType.LParen);
                List<Exp> exps = [];
                while (CurrentToken.Type != TokenType.RParen)
                {
                    exps.Add(Exp());
                    if (CurrentToken.Type != TokenType.RParen)
                        Eat(TokenType.Comma);
                }

                Eat(TokenType.RParen);
                callee = Locate(new CallExp(callee, exps), Result.Locate(callee));
            }


        return callee;
    }

    public static OpExp.Oper OperFrom(TokenType ty)
    {
        return ty switch
        {
            TokenType.Plus => OpExp.Oper.Plus,
            TokenType.Minus => OpExp.Oper.Minus,
            TokenType.Mod => OpExp.Oper.Mod,
            TokenType.Div => OpExp.Oper.Div,
            TokenType.Mul => OpExp.Oper.Mul,
            TokenType.Eq => OpExp.Oper.Eq,
            TokenType.Neq => OpExp.Oper.Neq,
            TokenType.Gt => OpExp.Oper.Gt,
            TokenType.Lt => OpExp.Oper.Lt,
            TokenType.Ge => OpExp.Oper.Ge,
            TokenType.Le => OpExp.Oper.Le,
            _ => throw new ArgumentOutOfRangeException(nameof(ty), ty, null)
        };
    }

    public Exp Mul()
    {
        var lhs = Call();
        while (new List<TokenType> { TokenType.Mul, TokenType.Mod, TokenType.Div }.Contains(CurrentToken.Type))
        {
            var tok = Eat(CurrentToken.Type);
            lhs = Locate(new OpExp(lhs, OperFrom(tok.Type), Mul()), Result.Locate(lhs));
        }

        return lhs;
    }

    public Exp Oper()
    {
        var lhs = Mul();
        while (new List<TokenType> { TokenType.Plus, TokenType.Minus }.Contains(CurrentToken.Type))
        {
            var tok = Eat(CurrentToken.Type);
            lhs = Locate(new OpExp(lhs, OperFrom(tok.Type), Oper()), Result.Locate(lhs));
        }

        return lhs;
    }

    public Exp Cmp()
    {
        var lhs = Oper();
        while (new List<TokenType>
               {
                   TokenType.Eq, TokenType.Neq, TokenType.Le, TokenType.Lt, TokenType.Gt, TokenType.Ge
               }.Contains(CurrentToken.Type))
        {
            var tok = Eat(CurrentToken.Type);
            lhs = Locate(new OpExp(lhs, OperFrom(tok.Type), Cmp()), Result.Locate(lhs));
        }

        return lhs;
    }

    public Exp Assign()
    {
        var lhs = Cmp();
        while (new List<TokenType>
               {
                   TokenType.Assign
               }.Contains(CurrentToken.Type))
        {
            Eat(CurrentToken.Type);
            if (lhs is Var v)
                lhs = Locate(new AssignExp(v, Cmp()), Result.Locate(lhs));
            else
                Debug.Fail("Not a var: " + lhs);
        }

        return lhs;
    }

    public Exp Ternary()
    {
        var condition = Assign();
        if (CurrentToken.Type == TokenType.QMark)
        {
            Eat(TokenType.QMark);

            var lhs = Exp();
            Eat(TokenType.Colon);
            var rhs = Exp();
            condition = Locate(new IfExp(condition, lhs, rhs), Result.Locate(condition));
        }

        return condition;
    }

    /*
     * exp:
       | exps
       | INT
       | STRING
       | exp op exp
       | exp '?' exp ':' exp
       | '(' exps ')'
     */
    public Exp Exp()
    {
        return Ternary();
    }
}