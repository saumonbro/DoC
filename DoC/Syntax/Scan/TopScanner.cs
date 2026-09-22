namespace DoC.Syntax.Scan;

public class TopScanner : Scanner
{
    public override List<(IRule, ScanCallback<IResult>)> Rules { get; } =
    [
        KeywordRules("if", TokenType.If),
        KeywordRules("while", TokenType.While),
        KeywordRules("else", TokenType.Else),
        KeywordRules("type", TokenType.Type),
        KeywordRules("fun", TokenType.Fun),
        KeywordRules("proc", TokenType.Proc),

        KeywordRules("let", TokenType.Let),
        KeywordRules("return", TokenType.Return),
        KeywordRules("with", TokenType.With),

        KeywordRules(";", TokenType.Semicolon),
        KeywordRules(":", TokenType.Colon),
        KeywordRules("(", TokenType.LParen),
        KeywordRules(")", TokenType.RParen),
        KeywordRules("[", TokenType.LBracket),
        KeywordRules("]", TokenType.RBracket),
        KeywordRules("{", TokenType.LBrace),
        KeywordRules("}", TokenType.RBrace),
        KeywordRules(".", TokenType.Dot),
        KeywordRules(",", TokenType.Comma),
        KeywordRules(":=", TokenType.Assign),
        KeywordRules("=", TokenType.Eq),
        KeywordRules("!=", TokenType.Neq),

        KeywordRules("<", TokenType.Lt),
        KeywordRules(">", TokenType.Gt),
        KeywordRules("<=", TokenType.Le),
        KeywordRules(">=", TokenType.Ge),

        // Arithmetic
        KeywordRules("=>", TokenType.Arrow),
        KeywordRules("+", TokenType.Plus),
        KeywordRules("-", TokenType.Minus),
        KeywordRules("*", TokenType.Mul),
        KeywordRules("/", TokenType.Div),
        KeywordRules("&&", TokenType.And),
        KeywordRules("||", TokenType.Or),
        KeywordRules("%", TokenType.Mod),
        KeywordRules("?", TokenType.QMark),


        RegexRule("[_a-zA-Z][_a-zA-Z0-9]*", TokenType.Identifier),
        RegexRule("[0-9]+", TokenType.Number),

        WithRegex("\"", (span, out match) =>
        {
            match = IResult.MakeScanner(new StringScanner());
            return span.Length;
        }),

        WithRegex("\\s", (span, out match) =>
        {
            match = new VoidResult();
            return span.Length;
        })
    ];
}