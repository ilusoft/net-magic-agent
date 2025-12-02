namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Represents a named value (variable or parameter) that can be consumed by the expression engine.
/// </summary>
public sealed record WorkflowExpressionBinding(string Name, WorkflowExpressionValue Value);
