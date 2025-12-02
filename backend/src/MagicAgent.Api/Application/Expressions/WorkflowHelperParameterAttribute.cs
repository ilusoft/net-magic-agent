using System;

namespace MagicAgent.Api.Application.Expressions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class WorkflowHelperParameterAttribute : Attribute
{
    public WorkflowHelperParameterAttribute(string parameterName)
    {
        ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
    }

    /// <summary>
    /// Name of the method parameter this metadata describes.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Optional friendly name displayed to users. Defaults to the parameter name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Optional override for the parameter type exposed in metadata.
    /// </summary>
    public WorkflowExpressionValueKind? Type { get; set; }

    public string? Description { get; set; }

    public bool IsOptional { get; set; }
}
