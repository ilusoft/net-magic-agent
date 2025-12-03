using System;
using FluentAssertions;
using MagicAgent.Api.Application.AgentRunner;
using MagicAgent.Api.Application.Expressions;

namespace MagicAgent.Api.Tests;

public class WorkflowExpressionValueConverterTests
{
    [Theory]
    [InlineData(true, 1d)]
    [InlineData(false, 0d)]
    public void TryConvertToNumber_ConvertsBoolean(bool input, double expected)
    {
        var value = WorkflowExpressionValue.FromBoolean(input);

        WorkflowExpressionValueConverter.TryConvertToNumber(value, out var number).Should().BeTrue();
        number.Should().Be(expected);
    }

    [Fact]
    public void TryConvertToNumber_ParsesString()
    {
        var value = WorkflowExpressionValue.FromString("42.5");

        WorkflowExpressionValueConverter.TryConvertToNumber(value, out var number).Should().BeTrue();
        number.Should().Be(42.5d);
    }

    [Fact]
    public void TryConvertToDateTime_ParsesIsoString()
    {
        var value = WorkflowExpressionValue.FromString("2024-01-02T03:04:05Z");

        WorkflowExpressionValueConverter.TryConvertToDateTime(value, out var dateTime).Should().BeTrue();
        dateTime.Should().Be(DateTimeOffset.Parse("2024-01-02T03:04:05Z"));
    }

    [Fact]
    public void ConvertToJsonString_ReturnsErrorForInvalidJson()
    {
        var value = WorkflowExpressionValue.FromString("not json");

        var (converted, error) = WorkflowExpressionValueConverter.ConvertToJsonString(value);

        converted.Should().Be("not json");
        error.Should().Be("Enter valid JSON (object or array).");
    }

    [Fact]
    public void ConvertToJsonString_SerializesJsonNode()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse("{\"value\":42}");
        var value = WorkflowExpressionValue.FromJson(node);

        var (converted, error) = WorkflowExpressionValueConverter.ConvertToJsonString(value);

        converted.Should().Be("{\"value\":42}");
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("foo", true)]
    public void ToBoolean_EvaluatesStrings(string input, bool expected)
    {
        var value = WorkflowExpressionValue.FromString(input);
        WorkflowExpressionValueConverter.ToBoolean(value).Should().Be(expected);
    }

    [Fact]
    public void ToBoolean_EvaluatesNumber()
    {
        WorkflowExpressionValueConverter.ToBoolean(WorkflowExpressionValue.FromNumber(0)).Should().BeFalse();
        WorkflowExpressionValueConverter.ToBoolean(WorkflowExpressionValue.FromNumber(15)).Should().BeTrue();
    }
}
