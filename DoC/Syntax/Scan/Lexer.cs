using System.Text.RegularExpressions;
using Lib;

namespace DoC.Syntax.Scan;

public class Lexer(string filename, string input)
{
    private readonly List<Token> _tokens = [];

    //private Token? _current;
    private int _currentIdx;

    private Scanner _currentScanner = new TopScanner();
    private int _idx;

    private Location _location = new(filename, 0, 0);

    private bool Done => _idx >= input.Length;
    private static Token DoneToken => new(TokenType.None, string.Empty);

    private Token CurrentToken => _tokens[_currentIdx];

    private void AddToken(Token token)
    {
        _tokens.Add(token);
    }

    /// <summary>
    ///     LookAhead(0) == Peek
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public Token LookAhead(int n = 1)
    {
        while (_currentIdx + n >= _tokens.Count)
        {
            if (Done) return DoneToken;
            ReadNext();
        }

        return _tokens[_currentIdx + n];
    }

    private string MakePattern(IRule rule)
    {
        return rule switch
        {
            StringRule sr => Regex.Escape(sr.Rule),
            RegexRule rr => rr.Pattern,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void Advance(int steps)
    {
        var lines = 0;
        var column = _location.Column;
        for (var i = 0; i < steps; i++)
        {
            if (input[_idx] == '\n')
            {
                lines += 1;
                column = 0;
            }
            else
            {
                column++;
            }

            _idx++;
        }

        _location = new Location(_location.Filename, _location.Line + lines, column);
    }

    private void Advance()
    {
        _location = input[_idx] == '\n' ? _location.Newline() : _location.Step();
        _idx++;
    }

    private string CurrentLine()
    {
        int i = _idx;
        int first = -1, last = -1;
        while (i > 0 && input[i] != '\n')
        {
            i--;
        }

        first = i;
        i = _idx;
        while (i < input.Length && input[i] != '\n')
        {
            i++;
        }

        last = i;
        return input[first..last];
    }

    private Token ReadNext()
    {
        if (Done)
            return DoneToken;

        var longestMatch = -1;
        Token? token = null;

        for (var i = 0; i < _currentScanner.Rules.Count; i++)
        {
            var (rule, matcher) = _currentScanner.Rules[i];
            var match = Regex.Match(input.AsSpan(_idx).ToString(), "^" + MakePattern(rule));

            if (!match.Success)
                continue;

            var currentMatch = matcher(match.Value, out var t);
            if (currentMatch <= longestMatch)
                continue;

            switch (t)
            {
                case VoidResult:
                    Advance(currentMatch);
                    //_idx += a;
                    return ReadNext();
                case TokenResult tr:
                    longestMatch = currentMatch;
                    token = tr.Token;
                    break;
                case ScannerResult sc:
                    _currentScanner = sc.Scanner;
                    //_idx += a;
                    Advance(currentMatch);

                    if (sc.Token == null)
                        return ReadNext();

                    AddToken(sc.Token.WithLocation(_location));
                    return CurrentToken;
            }
        }

        if (token == null)
            throw new Exception($"Matcher not found at char {input[_idx]} on {CurrentLine()}");

        //_idx += longestMatch;
        AddToken(token.WithLocation(_location));
        Advance(longestMatch);
        return CurrentToken;
    }

    public Token Peek()
    {
        if (_currentIdx == _tokens.Count) ReadNext();
        if (_currentIdx >= _tokens.Count)
            return DoneToken;
        return CurrentToken;
    }

    public Token Pop()
    {
        var res = Peek();
      //  Console.WriteLine($"Popped {res}");
        _currentIdx += 1;
        return res;
    }
}