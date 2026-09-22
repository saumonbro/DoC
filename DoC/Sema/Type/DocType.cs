namespace DoC.Sema.Type;

public enum TyKind
{
    Alias,
    Array,
    Bool,
    
}

public abstract class Trait
{
    public virtual bool TraitEqual(Trait tr)
    {
        return GetType() == tr.GetType() ;
    }
}

public class Callable : Trait
{
    public static Callable Instance { get; } = new ();
    private Callable(){}
}

public abstract class DocType
{
    public List<Trait> TypeTraits = [];

    public bool HasTrait(Trait trait)
    {
        return TypeTraits.Any(t => t.TraitEqual(trait));
    }
}

public class Function(List<DocType> args, DocType ret) : DocType
{
    public List<DocType> Args { get; private set; } = args;
    public DocType Return = ret;
}

public class Array(DocType Held) : DocType
{
    public DocType ElementType = Held;
}

public class Int(int size) : DocType
{
    public int SizeOf = size;

    public static readonly Int I32 = new Int(32);
}

public class Alias(string name, DocType inner) : DocType
{
    public string Name => name;
    public DocType Actual => inner;
}

public class Uint(int size) : DocType
{
    public int SizeOf = size;
}
public class Char : DocType{}
public class Float : DocType{}
public class String : DocType{}

public abstract class Adt : DocType
{
 //   protected List<DocType> Args = [];
}

public class Struct(List<(string, DocType)> fields) : Adt
{
    public List<(string, DocType)> Fields => fields;

    public DocType? FieldType(string field)
    {
        if (Fields.All(e => e.Item1 != field))
            return null;
        return Fields.First(e => e.Item1 == field).Item2;
    }
}
public class Union : Adt{}
public class Sum : Adt{}
public class Bool : DocType
{
}

public class Any : DocType
{
}

public class Postponed : DocType
{
    public void AddCandidate(DocType candidate)
    {
        Candidates.Add(candidate);
    }
    
    public void RemoveCandidate(DocType candidate)
    {
        Candidates.Remove(candidate);
    }
    public List<DocType> Candidates { get; protected set; }
}

public class ErrorTy : DocType
{
    public static ErrorTy Instance { get; } = new ();
    private ErrorTy(){}
}

public class Void : DocType
{
    public static Void Instance { get; } = new ();
    private Void(){}
}
