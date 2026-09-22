namespace Lib;

public interface IPassParam;

public class FileInput(string filename, string content) : IPassParam
{
    public string Filename = filename;
    public string Content = content;
}

public interface IPass
{
    string Name { get; }
    Type InputType { get; }
    Type OutputType { get; }
    (IPassParam, DocError) Run(IDriver driver, IPassParam input);
}

public interface IDriver
{
    FileInput TheFile { get; protected set; }
    public string GetLine(int n);
}

public abstract class Pass<TIn, TOut> : IPass
    where TIn : IPassParam
    where TOut : IPassParam
{
    public abstract string Name { get; }
    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    (IPassParam, DocError) IPass.Run(IDriver driver, IPassParam input)
    {
        if (input is not TIn arg)
        {
            throw new InvalidOperationException($"Cannot cast {input.GetType()} to {typeof(TIn)}");
        }

        return Run(driver, arg);
    }

    public abstract (TOut, DocError) Run(IDriver driver, TIn input);
}

public static class PassRegistry
{
    private static readonly Dictionary<string, IPass> Passes = new();

    public static void Register(IPass pass)
    {
        Passes[pass.Name] = pass;
    }

    public static IEnumerable<string> AvailablePasses => Passes.Keys;

    public static IPass? FromArg(string name) => Passes.GetValueOrDefault(name[2..]);

    private static Result<List<IPass>> FromArgs(IEnumerable<string> names)
    {
        List<IPass> passList = [];
        foreach (var name in names)
        {
            var pass = FromArg(name);
            if (pass == null)
                return new Exception($"Pass {name} does not exist");

            passList.Add(pass);
        }

        return passList;
    }

    private static Result<List<IPass>> ValidateChain(IEnumerable<IPass> passes, Type initialInputType)
    {
        var currentType = initialInputType;
        List<IPass> res = [];
        foreach (var pass in passes)
        {
            if (!pass.InputType.IsAssignableFrom(currentType) && !currentType.IsAbstract)
                return new Exception($"Pass '{pass.Name}' expects input type '{pass.InputType.Name}' but received '{currentType.Name}'");

            currentType = pass.OutputType;
            res.Add(pass);
        }

        return res;
    }

    public static Result<List<IPass>> MakePassChain(IEnumerable<string> args)
    {
        var passChain = args
            .Where(a => a.StartsWith("--"))
            .ToList();

        if (passChain.Count == 0)
        {
            return new Exception("No passes specified. Available passes:" +
                                 AvailablePasses.Select(name => $"  --{name}"));
        }

        var passes = FromArgs(passChain);

        if (passes.IsFailure)
        {
            return passes;
        }

        return ValidateChain(passes.Value, typeof(FileInput));
    }
}