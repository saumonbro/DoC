namespace Absyn

module FsPretty =
    let indentUnit = "    "

    let indent level = String.replicate level indentUnit

    let operToString op =
        match op with
        | OpExp.Oper.Plus -> "+"
        | OpExp.Oper.Minus -> "-"
        | OpExp.Oper.Mul -> "*"
        | OpExp.Oper.Div -> "/"
        | OpExp.Oper.Mod -> "%"
        | OpExp.Oper.Eq -> "="
        | OpExp.Oper.Neq -> "!="
        | OpExp.Oper.Gt -> ">"
        | OpExp.Oper.Ge -> ">="
        | OpExp.Oper.Lt -> "<"
        | OpExp.Oper.Le -> "<="
        | _ -> failwith "Unknown operator"

    let rec print (n: Node) level : string =
        match n with
        // Expressions
        | :? IntExp as e -> e.Value
        | :? StringExp as e -> $"\"{e.Value}\""
        | :? SimpleVar as v -> v.Name
        | :? FieldVar as v -> $"{print v.Left level}.{v.Field}"
        | :? SubscriptVar as v -> $"{print v.Left level}[{print v.Idx level}]"
        | :? AssignExp as e -> $"{print e.Var level} := {print e.Value level}"
        | :? CallExp as e ->
            let args = e.Args |> Seq.map (fun a -> print a level) |> String.concat ", "
            $"{print e.Callee level}({args})"
        | :? OpExp as e -> $"({print e.Lhs level} {operToString e.Op} {print e.Rhs level})"
        | :? IfExp as e -> printIfExp e level
        | :? SeqExp as e -> e.Exps |> Seq.map (fun x -> print x level) |> String.concat "; "

        // Types
        | :? NameTy as t -> t.Name
        | :? ArrayTy as t -> $"{print t.ElementType level}[]"
        | :? RestrictTy as t -> $"{print t.Type level} with {t.Predicate}"
        | :? RecordTy as t -> printRecordTy t level

        // Declarations
        | :? VarDec as d -> printVarDec d level
        | :? FormalDec as d -> $"{d.Name} : {print d.Ty.Value level}"
        | :? TypeDec as d -> $"type {d.Name} := {print d.Ty level}"
        | :? FunDec as d -> printFunDec d level

        // Statements
        | :? ReturnStm as s -> $"return {print s.Exp level}"
        | :? IfStm as s -> printIfStm s level
        | :? Block as s -> printStmList s level
        | :? Sxp as s -> print s.Exp level

        | _ -> failwith $"Unknown node type: {n.GetType().Name}"

    and printIfExp e level =
        $"({print e.Cond level}) ? {print e.Then level} : {print e.Else level}"

    and printRecordTy t level =
        if t.Fields.Count = 0 then
            "{ }"
        else
            let fields = t.Fields |> Seq.map (fun f -> print f level) |> String.concat ", "
            $"{{ {fields} }}"

    and printOptTy d level =
        match d with
        | Some ty -> $" : {print ty level}"
        | None -> ""


    and printVarDec d level =
        let tyPart =
            match d.Ty with
            | Some ty -> $" : {print ty level}"
            | None -> ""

        $"let {d.Name}{tyPart} := {print d.Init level}"

    and printFunDec d level =
        let keyword = if d.Pure then "fun" else "proc"
        let formals = d.Formals |> Seq.map (fun f -> print f level) |> String.concat ", "

        let tyPart =
            match d.Ty with
            | Some ty -> $" : {print ty level}"
            | None -> ""

        match d.Stmts with
        | None -> ""
        | Some stms ->
        match stms.Stmts |> Seq.toList with
        | [ :? ReturnStm as ret ] -> $"{keyword} {d.Name}({formals}){tyPart} => {print ret.Exp level}"
        | _ -> $"{keyword} {d.Name}({formals}){tyPart}\n{printStmList stms level}"

    and printIfStm s level =
        let thenPart =
            match s.Then with
            | :? Block as sl -> $"\n{printStmList sl level}"
            | _ -> $"\n{indent (level + 1)}{print s.Then (level + 1)};"

        let elsePart =
            match s.Else with
            | None -> ""
            | Some stm ->
                match stm with
                | :? Block as sl -> $"\n{indent level}else\n{printStmList sl level}"
                | :? IfStm as elseIf -> $"\n{indent level}else {printIfStm elseIf level}"
                | _ -> $"\n{indent level}else\n{indent (level + 1)}{print stm (level + 1)};"

        $"if ({print s.Cond level}){thenPart}{elsePart}"

    and printStmList s level =
        let ind = indent (level + 1)

        let stmts =
            s.Stmts
            |> Seq.map (fun stm ->
                match stm with
                | :? IfStm as ifStm -> $"{ind}{printIfStm ifStm (level + 1)};"
                | :? FunDec as funDec -> $"{ind}{printFunDec funDec (level + 1)}"
                | :? TypeDec -> $"{ind}{print stm (level + 1)}"
                | _ -> $"{ind}{print stm (level + 1)};")
            |> String.concat "\n"

        $"{indent level}{{\n{stmts}\n{indent level}}}"

    let printProgram program =
        program |> List.map (fun d -> print d 0) |> String.concat "\n\n"

    let printProgramSeq program =
        program |> Seq.map (fun d -> print d 0) |> String.concat "\n\n"

type FsPretty public () =
    static member Print(node) = FsPretty.print node 0
    static member PrettyPrint(node) = printfn $"%s{FsPretty.print node 0}"

    static member PrintProgram(ast : Ast) = FsPretty.printProgramSeq ast.Decs
