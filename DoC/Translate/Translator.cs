using Absyn;
using DoC.Sema.Type;
using Lib;
using LLVMSharp;

namespace DoC.Translate;

public class Translator(TypedAst theAst) : ReadonlyAstVisitor<TypedAst>(theAst)
{
    public override (TypedAst, DocError) Work()
    {
        throw new NotImplementedException();
    }

    public override Unit Visit(Node node)
    {
        return Unit.Value;
    }
}