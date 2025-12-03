using System.Collections.Generic;
using System.Text.Json.Serialization;
using MagicAgent.Api.Application.AgentRunner;

namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Describes the optional context payload that can be provided during expression validation.
/// </summary>
public sealed record ExpressionValidationContextPayload
{
    [JsonPropertyName("variables")]
    public Dictionary<string, ExpressionValidationContextValue>? Variables { get; init; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, ExpressionValidationContextValue>? Parameters { get; init; }

    [JsonPropertyName("runtimeState")]
    public Dictionary<string, ExpressionValidationContextValue>? RuntimeState { get; init; }

    [JsonPropertyName("stepInput")]
    public ExpressionValidationContextValue? StepInput { get; init; }

    [JsonPropertyName("lastStepOutput")]
    public ExpressionValidationContextValue? LastStepOutput { get; init; }
}

/// <summary>
/// Represents a single typed value descriptor that can be converted into a workflow expression value.
/// </summary>
public sealed record ExpressionValidationContextValue
{
    [JsonPropertyName("type")]
    public WorkflowVariableDataType? Type { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
