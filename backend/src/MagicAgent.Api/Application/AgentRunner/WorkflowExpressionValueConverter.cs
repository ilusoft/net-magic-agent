using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAgent.Api.Application.Expressions;

namespace MagicAgent.Api.Application.AgentRunner;

internal static class WorkflowExpressionValueConverter
{
    public static string ToStringValue(WorkflowExpressionValue? value) => value?.ToDisplayString() ?? string.Empty;

    public static bool TryConvertToNumber(WorkflowExpressionValue value, out double number)
    {
        switch (value.Kind)
        {
            case WorkflowExpressionValueKind.Number:
                number = value.NumberValue ?? 0d;
                return true;
            case WorkflowExpressionValueKind.Boolean:
                number = (value.BooleanValue ?? false) ? 1d : 0d;
                return true;
            case WorkflowExpressionValueKind.String:
                return double.TryParse(value.StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
            case WorkflowExpressionValueKind.DateTime:
                number = value.DateTimeValue?.ToUnixTimeMilliseconds() ?? 0d;
                return true;
            default:
                number = 0d;
                return false;
        }
    }

    public static bool TryConvertToDateTime(WorkflowExpressionValue value, out DateTimeOffset dateTime)
    {
        switch (value.Kind)
        {
            case WorkflowExpressionValueKind.DateTime:
                if (value.DateTimeValue.HasValue)
                {
                    dateTime = value.DateTimeValue.Value;
                    return true;
                }
                break;
            case WorkflowExpressionValueKind.String:
                if (DateTimeOffset.TryParse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTime))
                {
                    return true;
                }
                break;
        }

        dateTime = default;
        return false;
    }

    public static (string ConvertedValue, string? Error) ConvertToJsonString(WorkflowExpressionValue value)
    {
        switch (value.Kind)
        {
            case WorkflowExpressionValueKind.Json when value.JsonValue is not null:
                return (value.JsonValue.ToJsonString(), null);
            case WorkflowExpressionValueKind.String:
                var raw = value.StringValue ?? string.Empty;
                try
                {
                    using var document = JsonDocument.Parse(raw);
                    return (document.RootElement.GetRawText(), null);
                }
                catch (JsonException)
                {
                    return (raw, "Enter valid JSON (object or array).");
                }
            default:
                return (value.ToDisplayString(), "Enter valid JSON (object or array).");
        }
    }

    public static bool ToBoolean(WorkflowExpressionValue value)
    {
        return value.Kind switch
        {
            WorkflowExpressionValueKind.Boolean => value.BooleanValue ?? false,
            WorkflowExpressionValueKind.Number => Math.Abs(value.NumberValue ?? 0d) > double.Epsilon,
            WorkflowExpressionValueKind.String => !string.IsNullOrWhiteSpace(value.StringValue),
            WorkflowExpressionValueKind.Json => value.JsonValue is not null,
            WorkflowExpressionValueKind.DateTime => value.DateTimeValue.HasValue,
            _ => false,
        };
    }
}
