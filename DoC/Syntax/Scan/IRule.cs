namespace DoC.Syntax.Scan;

public interface IRule;

public record RegexRule(string Pattern) : IRule;

public record StringRule(string Rule) : IRule;