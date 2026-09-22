namespace Lib;

public readonly struct Location(string filename, int line, int column)
{
    public override string ToString()
    {
        return filename + ":" + line + ":" + column;
    }

    public Location Newline(int step = 1)
    {
        return new Location(filename, line + step, 0);
    }

    public Location Step(int step = 1)
    {
        return new Location(filename, line, column + step);
    }

    public string Filename => filename;
    public int Line => line;
    public int Column => column;
}