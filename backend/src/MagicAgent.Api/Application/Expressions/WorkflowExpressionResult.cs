using System.Collections.Generic;

namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Represents the output of evaluating a workflow expression.
/// </summary>
public sealed record WorkflowExpressionResult(
    bool Success,
    WorkflowExpressionValue Value,
    string? ErrorMessage = null,
    string? ErrorCode = null,
    IReadOnlyList<string>? ReferencedIdentifiers = null)
{
    public static WorkflowExpressionResult FromValue(
        WorkflowExpressionValue value,
        IReadOnlyList<string>? referencedIdentifiers = null) =>
        new(true, value, null, null, referencedIdentifiers ?? Array.Empty<string>());

    public static WorkflowExpressionResult FromError(
        string message,
        string? errorCode = null) =>
        new(false, WorkflowExpressionValue.Null(), message, errorCode, Array.Empty<string>());
}
