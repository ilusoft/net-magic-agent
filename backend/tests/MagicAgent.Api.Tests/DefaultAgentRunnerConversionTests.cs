using System;
using System.Reflection;
using FluentAssertions;
using MagicAgent.Api.Application.AgentRunner;
using MagicAgent.Api.Application.Expressions;

namespace MagicAgent.Api.Tests;

public class DefaultAgentRunnerConversionTests
{
    private static readonly MethodInfo ConvertMethod = typeof(DefaultAgentRunner)
        .GetMethod("ConvertWorkflowVariableValue", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ConvertWorkflowVariableValue not found via reflection.");

    [Fact]
    public void ConvertWorkflowVariableValue_UsesTypedNumber()
    {
        var typedValue = WorkflowExpressionValue.FromNumber(12.34);
        var state = InvokeConvert(typedValue, "ignored", WorkflowVariableDataType.Number);

        GetProperty<string>(state, "ConvertedValue").Should().Be("12.34");
        GetProperty<WorkflowVariableDataType>(state, "Type").Should().Be(WorkflowVariableDataType.Number);
        GetProperty<string?>(state, "Error").Should().BeNull();
    }

    [Fact]
    public void ConvertWorkflowVariableValue_InvalidNumberFallsBackToString()
    {
        var typedValue = WorkflowExpressionValue.FromString("not-a-number");
        var state = InvokeConvert(typedValue, "not-a-number", WorkflowVariableDataType.Number);

        GetProperty<string>(state, "ConvertedValue").Should().Be("not-a-number");
        GetProperty<WorkflowVariableDataType>(state, "Type").Should().Be(WorkflowVariableDataType.String);
        GetProperty<string?>(state, "Error").Should().Contain("parse number");
    }

    [Fact]
    public void ConvertWorkflowVariableValue_ParsesDateTimeFromTypedValue()
    {
        var dateTime = DateTimeOffset.Parse("2024-05-01T10:20:30Z");
        var typedValue = WorkflowExpressionValue.FromDateTime(dateTime);
        var state = InvokeConvert(typedValue, "ignored", WorkflowVariableDataType.DateTime);

        GetProperty<string>(state, "ConvertedValue").Should().Be(dateTime.ToString("O"));
        GetProperty<string?>(state, "Error").Should().BeNull();
    }

    [Fact]
    public void ConvertWorkflowVariableValue_ParsesJsonNode()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse("{\"value\":42}");
        var typedValue = WorkflowExpressionValue.FromJson(node);
        var state = InvokeConvert(typedValue, "ignored", WorkflowVariableDataType.Json);

        GetProperty<string>(state, "ConvertedValue").Should().Be("{\"value\":42}");
        GetProperty<string?>(state, "Error").Should().BeNull();
    }

    [Fact]
    public void ConvertWorkflowVariableValue_ConvertsBoolean()
    {
        var typedValue = WorkflowExpressionValue.FromBoolean(true);
        var state = InvokeConvert(typedValue, "ignored", WorkflowVariableDataType.Boolean);

        GetProperty<string>(state, "ConvertedValue").Should().Be("true");
        GetProperty<string?>(state, "Error").Should().BeNull();
    }

    private static object InvokeConvert(WorkflowExpressionValue typedValue, string rawValue, WorkflowVariableDataType targetType)
        => ConvertMethod.Invoke(null, new object[] { typedValue, rawValue, targetType })
            ?? throw new InvalidOperationException("ConvertWorkflowVariableValue returned null.");

    private static T GetProperty<T>(object state, string propertyName)
    {
        var property = state.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on workflow variable state.");

        return (T)property.GetValue(state)!;
    }
}
