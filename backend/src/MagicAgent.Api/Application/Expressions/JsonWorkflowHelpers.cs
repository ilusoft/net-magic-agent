using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MagicAgent.Api.Application.Expressions.Helpers;

public static class JsonWorkflowHelpers
{
    [WorkflowHelper("stringToJson", ReturnType = WorkflowExpressionValueKind.Json, Description = "Parses a JSON string into an object node.")]
    [WorkflowHelperParameter("value", Description = "JSON text to parse.")]
    public static JsonNode? StringToJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("JSON text must be provided.", nameof(value));
        }

        try
        {
            return JsonNode.Parse(value);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("The provided value is not valid JSON.", nameof(value), ex);
        }
    }

    [WorkflowHelper("jsonToString", ReturnType = WorkflowExpressionValueKind.String, Description = "Serializes a JSON node back to string form.")]
    [WorkflowHelperParameter("value", Description = "JSON value to serialize.")]
    public static string JsonToString(JsonNode? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false,
        });
    }
}