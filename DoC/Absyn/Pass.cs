using Absyn;
using Lib;

namespace DoC.Absyn;

public class PrettyPass : Pass<Ast, Ast>
{
    public override string Name => "pretty";

    public override (Ast, DocError) Run(IDriver driver, Ast input)
    {
        string pretty = Pretty.PrintProgram(input);
        Console.WriteLine(pretty);
        return (input, new DocError());
    }
}

