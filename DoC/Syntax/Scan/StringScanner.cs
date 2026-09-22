namespace DoC.Syntax.Scan;

public class StringScanner : Scanner
{
    private string _growing = "";

    public StringScanner()
    {
        Rules =
        [
            WithRegex("\"", (span, out match) =>
            {
                match = IResult.MakeScanner(new TopScanner(), new Token(TokenType.String, _growing));
                return span.Length;
            }),
            WithRegex(".", (span, out match) =>
            {
                match = IResult.MakeVoid();
                _growing += span;
                return span.Length;
            })
        ];
    }

    public override List<(IRule, ScanCallback<IResult>)> Rules { get; }
}