using System.Collections.Generic;

namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Metadata describing a helper available to workflow expressions.
/// </summary>
public sealed record WorkflowHelperDescriptor(
    string Name,
    WorkflowExpressionValueKind ReturnType,
    string? Description,
    IReadOnlyList<WorkflowHelperParameterDescriptor> Parameters);
