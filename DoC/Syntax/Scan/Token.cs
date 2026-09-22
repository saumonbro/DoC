using Absyn;
using Lib;

namespace DoC.Syntax.Scan;

public enum TokenType
{
    None,

    // Value Tokens
    Identifier,
    Number,
    String,

    // Keywords
    If,
    Else,
    While,
    Type,
    Fun,
    Proc,
    Let,
    Return,
    With,

    // Grammar
    Dot,
    Comma,
    LParen,
    RParen,

    LBracket,
    RBracket,

    LBrace,
    RBrace,

    Colon,
    Semicolon,

    // Arithmetic
    Arrow,
    Plus,
    Minus,
    Mul,
    Div,
    Mod,
    And,
    Or,
    QMark,

    Assign,
    Eq,
    Neq,
    Lt,
    Le,
    Gt,
    Ge
}

public record Token(TokenType Type, string Value, Location Location = default)
{
    public Token WithLocation(Location location)
    {
        return this with { Location = location };
    }
}