using Absyn;
using Lib;

namespace DoC.Sema.Bind;

public class BindPass : Pass<LocedAst, BoundAst>
{
    public override string Name => "bind";

    public override (BoundAst, DocError) Run(IDriver driver, LocedAst input)
    {
        Binder binder = new Binder(input);
        return binder.Bind();
    }
}

