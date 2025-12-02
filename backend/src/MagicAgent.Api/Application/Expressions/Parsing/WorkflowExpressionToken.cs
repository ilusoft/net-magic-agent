namespace MagicAgent.Api.Application.Expressions.Parsing;

/// <summary>
/// Represents a single lexical token.
/// </summary>
public readonly record struct WorkflowExpressionToken(
    WorkflowExpressionTokenKind Kind,
    string Text,
    int Position);
