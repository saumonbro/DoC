using Absyn;
using DoC.Sema.Type;
using Lib;

namespace DoC.Sema;

public class Sema(TypedAst theAst) : AstVisitor<TypedAst, TypedAst>(theAst)
{
    public override (TypedAst, DocError) Work()
    {
        throw new NotImplementedException();
    }
}