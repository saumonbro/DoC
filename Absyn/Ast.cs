using System.Collections.ObjectModel;
using Lib;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Absyn;

public abstract partial class Ast(List<Dec> decs)
{
    public List<Dec> Decs { get; } = decs;
}

public partial class Ast : IPassParam;

public interface ILocated
{
    public Dictionary<Node, Location> Locs { get; protected set; }
}

public class LocedAst(List<Dec> decs, Dictionary<Node, Location> locs) : Ast(decs)
{
    public LocedAst(LocedAst e) : this(e.Decs, e.Locs)
    {
    }

    protected readonly Dictionary<Node, Location> Locs = locs;

    public void Locate(Node n, Location loc)
    {
        Locs[n] = loc;
    }
    
    public Location Locate(Node n)
    {
        return Locs[n];
    }
}

public class BoundAst : LocedAst
{
    protected Dictionary<SimpleVar, List<IdDec>> Defs;
    protected Dictionary<NameTy, TypeDec> TyDefs;

    public BoundAst(LocedAst loced, Dictionary<SimpleVar, List<IdDec>> defs, Dictionary<NameTy, TypeDec> tyDefs) :
        base(loced)
    {
        TyDefs = tyDefs;
        Defs = defs;
    }

    public BoundAst(BoundAst other) : base(other)
    {
        Defs = other.Defs;
        TyDefs = other.TyDefs;
    }

    public List<IdDec> Def(SimpleVar v) => Defs.TryGetValue(v, out var candidates) ? candidates : [];
    public void Def(SimpleVar v, IdDec def) => Defs.AddOrCreate(v, def);
    public void Def(SimpleVar v, List<IdDec> defs) => defs.ForEach(def => Defs.AddOrCreate(v, def));
    
    public TypeDec? Def(NameTy v) => TyDefs.GetValueOrDefault(v);
    public void Def(NameTy v, TypeDec def) => TyDefs[v] = def;
}

