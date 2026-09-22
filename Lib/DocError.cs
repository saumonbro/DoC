namespace Lib;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public record LogEntry(LogLevel Level, string Message, Location? Location = null);

public class DocError
{
    public int Code
    {
        get => Errors.Count != 0 ? 1 : field;
        set;
    }

    public IReadOnlyList<LogEntry> Entries => _entries.AsReadOnly();

    public IReadOnlyList<string> Errors => _entries
        .Where(e => e.Level == LogLevel.Error)
        .Select(e => e.Message)
        .ToList()
        .AsReadOnly();

    public static bool operator +(DocError docError)
    {
        return docError._entries.Any(e => e.Level == LogLevel.Error) || docError.Code != 0;
    }

    public bool HasError => +this;

    private readonly List<LogEntry> _entries = [];

    public DocError(string message)
    {
        _entries = [new LogEntry(LogLevel.Error, message)];
    }

    public DocError()
    {
    }

    public DocError(IEnumerable<string> errors)
    {
        _entries = errors.Select(e => new LogEntry(LogLevel.Error, e)).ToList();
    }

    public void Debug(string message) => _entries.Add(new LogEntry(LogLevel.Debug, message));
    public void Debug(Location location, string message) => _entries.Add(new LogEntry(LogLevel.Debug, message, location));

    public void Info(string message) => _entries.Add(new LogEntry(LogLevel.Info, message));
    public void Info(Location location, string message) => _entries.Add(new LogEntry(LogLevel.Info, message, location));

    public void Warning(string message) => _entries.Add(new LogEntry(LogLevel.Warning, message));
    public void Warning(Location location, string message) => _entries.Add(new LogEntry(LogLevel.Warning, message, location));

    public void Error(string message) => _entries.Add(new LogEntry(LogLevel.Error, message));
    public void Error(Location location, string message) => _entries.Add(new LogEntry(LogLevel.Error, message, location));

    public void Publish(TextWriter stream, IDriver? driver = null)
    {
        var isConsole = stream == Console.Out || stream == Console.Error;

        foreach (var entry in _entries)
        {
            var originalColor = isConsole ? Console.ForegroundColor : ConsoleColor.White;

            if (isConsole)
            {
                Console.ForegroundColor = entry.Level switch
                {
                    LogLevel.Debug => ConsoleColor.Green,
                    LogLevel.Info => ConsoleColor.Blue,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => originalColor
                };
            }

            var prefix = entry.Level switch
            {
                LogLevel.Debug => "[DEBUG] ",
                LogLevel.Info => "[INFO] ",
                LogLevel.Warning => "[WARN] ",
                LogLevel.Error => "[ERROR] ",
                _ => ""
            };

            if (entry.Location is { } loc)
            {
                stream.WriteLine($"{prefix}{loc}: {entry.Message}");

                if (driver != null)
                {
                    var line = driver.GetLine(loc.Line);
                    if (isConsole) Console.ForegroundColor = originalColor;
                    stream.WriteLine($"       | {line}");
                    stream.WriteLine($"       | {new string(' ', loc.Column)}^");
                }
            }
            else
            {
                stream.WriteLine($"{prefix}{entry.Message}");
            }

            if (isConsole) Console.ForegroundColor = originalColor;
        }
    }
}
