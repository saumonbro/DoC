using Lib;

namespace DoC.Syntax.Parse;

public class Driver : IDriver
{
    public (IPassParam, DocError) RunChain(IEnumerable<IPass> passNames, IPassParam input)
    {
        foreach (var pass in passNames)
        {
            var (result, error) = pass.Run(this, input);
            if (error.HasError)
            {
                return (result, error);
            }
            input = result;
        }
        return (input, new DocError());
    }

    public int Run(IEnumerable<IPass> passes, string file)
    {
        _fileLines = File.ReadAllLines(file).ToList();
        TheFile = new FileInput(file, string.Join('\n', _fileLines));
        var (result, error) = RunChain(passes, new FileInput(file, File.ReadAllText(file)));

        if (error.HasError)
        {
            error.Publish(Console.Error, this);
            return error.Code;
        }

        Console.WriteLine($"Result type: {result.GetType().Name}");
        return 0;
    }

    private List<string> _fileLines = [];

    public string GetLine(int n)
    {
        return _fileLines[n];
    }

    public FileInput TheFile
    {
        get => field ?? throw new InvalidOperationException("No file in driver");
        set;
    }
}