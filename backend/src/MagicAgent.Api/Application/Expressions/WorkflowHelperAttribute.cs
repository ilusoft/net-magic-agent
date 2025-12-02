using System;

namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Decorates a static helper method so it can be discovered by the workflow expression engine.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class WorkflowHelperAttribute : Attribute
{
    public WorkflowHelperAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Name used within expressions (case-insensitive).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional human-readable description for documentation/metadata endpoints.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Declares the helper's return type for metadata consumers.
    /// </summary>
    public WorkflowExpressionValueKind ReturnType { get; set; } = WorkflowExpressionValueKind.String;
}
