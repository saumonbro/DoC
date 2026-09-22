using Absyn;
using Lib;

namespace DoC.Sema.Type;

public class TypePass : Pass<BoundAst, TypedAst>
{
    public override string Name => "type";
    public override (TypedAst, DocError) Run(IDriver driver, BoundAst input)
    {
        TypeChecker typer = new TypeChecker(input);
        return typer.TypeCheck();
    }

}
