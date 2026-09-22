using Rule = (DoC.Syntax.Scan.IRule, DoC.Syntax.Scan.ScanCallback<DoC.Syntax.Scan.IResult>);


namespace DoC.Syntax.Scan;

public abstract class Scanner
{
    public abstract List<Rule> Rules { get; }


    protected static Rule WithRegex(string regex, ScanCallback<IResult> callback)
    {
        return (new RegexRule(regex), callback);
    }

    public static Rule WithLiteral(string regex, ScanCallback<IResult> callback)
    {
        return (new StringRule(regex), callback);
    }

    protected static Rule KeywordRules(string word, TokenType type)
    {
        return (new StringRule(word), (span, out match) =>
        {
            match = new TokenResult(new Token(type, span));
            return span.Length;
        });
    }

    protected static Rule RegexRule(string regex, TokenType type)
    {
        return (new RegexRule(regex), (span, out match) =>
        {
            match = new TokenResult(new Token(type, span.ToString()));
            return span.Length;
        });
    }
}