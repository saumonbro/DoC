namespace DoC.Syntax.Scan;

public interface IResult
{
    public static VoidResult MakeVoid()
    {
        return new VoidResult();
    }

    public static TokenResult MakeToken(Token token)
    {
        return new TokenResult(token);
    }

    public static ScannerResult MakeScanner(Scanner scanner, Token? token = null)
    {
        return new ScannerResult(scanner, token);
    }
}

public record TokenResult(Token Token) : IResult;

public record VoidResult : IResult;

public record ScannerResult(Scanner Scanner, Token? Token = null) : IResult;