using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using MagicAgent.Api.Application.AgentRunner;

namespace MagicAgent.Api.Application.Expressions;

internal static class ExpressionValidationContextFactory
{
    public static WorkflowExpressionContext Create(ExpressionValidationContextPayload? payload)
    {
        if (payload is null)
        {
            return new WorkflowExpressionContext();
        }

        var variables = ConvertMap(payload.Variables);
        var parameters = ConvertMap(payload.Parameters);
        var runtimeState = ConvertMap(payload.RuntimeState);
        var stepInput = ConvertToDisplayString(payload.StepInput);
        var lastStepOutput = ConvertToDisplayString(payload.LastStepOutput);

        return new WorkflowExpressionContext(
            variables,
            parameters,
            runtimeState,
            stepInput,
            lastStepOutput);
    }

    private static IReadOnlyDictionary<string, WorkflowExpressionValue> ConvertMap(
        IReadOnlyDictionary<string, ExpressionValidationContextValue>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new Dictionary<string, WorkflowExpressionValue>(StringComparer.OrdinalIgnoreCase);
        }

        var map = new Dictionary<string, WorkflowExpressionValue>(source.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in source)
        {
            map[kvp.Key] = ConvertDescriptor(kvp.Value);
        }

        return map;
    }

    private static string? ConvertToDisplayString(ExpressionValidationContextValue? descriptor)
    {
        if (descriptor is null)
        {
            return null;
        }

        return ConvertDescriptor(descriptor).ToDisplayString();
    }

    private static WorkflowExpressionValue ConvertDescriptor(ExpressionValidationContextValue? descriptor)
    {
        if (descriptor is null)
        {
            return WorkflowExpressionValue.FromString(string.Empty);
        }

        var targetType = descriptor.Type ?? WorkflowVariableDataType.String;
        var rawValue = descriptor.Value;

        return targetType switch
        {
            WorkflowVariableDataType.Number => ConvertNumber(rawValue),
            WorkflowVariableDataType.Boolean => ConvertBoolean(rawValue),
            WorkflowVariableDataType.DateTime => ConvertDateTime(rawValue),
            WorkflowVariableDataType.Json => ConvertJson(rawValue),
            _ => WorkflowExpressionValue.FromString(rawValue ?? string.Empty),
        };
    }

    private static WorkflowExpressionValue ConvertNumber(string? value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return WorkflowExpressionValue.FromNumber(number);
        }

        return WorkflowExpressionValue.FromNumber(0d);
    }

    private static WorkflowExpressionValue ConvertBoolean(string? value)
    {
        if (bool.TryParse(value, out var boolean))
        {
            return WorkflowExpressionValue.FromBoolean(boolean);
        }

        return WorkflowExpressionValue.FromBoolean(false);
    }

    private static WorkflowExpressionValue ConvertDateTime(string? value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return WorkflowExpressionValue.FromDateTime(parsed);
        }

        return WorkflowExpressionValue.FromDateTime(DateTimeOffset.UtcNow);
    }

    private static WorkflowExpressionValue ConvertJson(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                var node = JsonNode.Parse(value);
                if (node is not null)
                {
                    return WorkflowExpressionValue.FromJson(node);
                }
            }
            catch
            {
                // fall back to empty object
            }
        }

        return WorkflowExpressionValue.FromJson(new JsonObject());
    }
}
