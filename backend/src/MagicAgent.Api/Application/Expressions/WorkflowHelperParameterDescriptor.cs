namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Describes a parameter accepted by a workflow helper function.
/// </summary>
public sealed record WorkflowHelperParameterDescriptor(
    string Name,
    WorkflowExpressionValueKind Type,
    string? Description = null,
    bool IsOptional = false);
