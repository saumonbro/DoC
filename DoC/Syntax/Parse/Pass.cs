using Absyn;
using Lib;

namespace DoC.Syntax.Parse;

public class ParsePass : Pass<FileInput, LocedAst>
{
    public override string Name => "parse";

    public override (LocedAst, DocError) Run(IDriver driver, FileInput input)
    {
        Parser parser = new Parser(input.Filename, input.Content);
        return (parser.Program());
    }
}