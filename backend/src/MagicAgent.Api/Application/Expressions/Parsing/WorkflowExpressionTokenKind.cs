namespace MagicAgent.Api.Application.Expressions.Parsing;

/// <summary>
/// Represents the lexical token kinds produced by the workflow expression tokenizer.
/// </summary>
public enum WorkflowExpressionTokenKind
{
    EndOfInput = 0,
    Identifier,
    Number,
    String,
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    Comma,
    Dot,
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    Caret,
    Bang,
    Equals,
    NotEquals,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    AndAnd,
    OrOr,
    Question,
    Colon,
}
