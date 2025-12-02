using System.Collections.Generic;
using FluentAssertions;
using MagicAgent.Api.Application.AgentRunner;
using MagicAgent.Api.Application.Expressions;
using MagicAgent.Api.Application.Expressions.Helpers;
using MagicAgent.Api.Application.Expressions.Parsing;
using Microsoft.Extensions.Logging.Abstractions;

namespace MagicAgent.Api.Tests;

public class ExpressionEvaluatorTests
{
    private static readonly WorkflowHelperRegistry HelperRegistry = new(new[]
    {
        typeof(MathWorkflowHelpers),
        typeof(DateWorkflowHelpers),
        typeof(StringWorkflowHelpers),
    });

    private static readonly WorkflowExpressionEvaluator Evaluator =
        new(HelperRegistry, NullLogger<WorkflowExpressionEvaluator>.Instance);

    static ExpressionEvaluatorTests()
    {
        WorkflowPlaceholderResolver.Configure(Evaluator);
    }

    [Fact]
    public void Parser_AllowsExpressionsWithMemberAccessAndSpaces()
    {
        Action act = () => WorkflowExpressionParser.Parse("abs(var.value) + param.scale");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("1 + 2 * 3", 7)]
    [InlineData("(1 + 2) * 3", 9)]
    [InlineData("-5 + 10", 5)]
    public void Evaluator_ComputesArithmeticExpressions(string expression, double expected)
    {
        var context = CreateContext();

        var result = Evaluator.Evaluate(expression, context);

        result.Success.Should().BeTrue(result.ErrorMessage ?? string.Empty);
        result.Value.NumberValue.Should().Be(expected);
    }

    [Fact]
    public void Evaluator_ResolvesVariablesAndParameters()
    {
        var context = CreateContext(
            variables: new Dictionary<string, WorkflowExpressionValue>
            {
                ["value"] = WorkflowExpressionValue.FromString("5"),
            },
            parameters: new Dictionary<string, WorkflowExpressionValue>
            {
                ["scale"] = WorkflowExpressionValue.FromString("3"),
            },
            stepInput: "4");

        var result = Evaluator.Evaluate("(var.value + param.scale) * input", context);

        result.Success.Should().BeTrue(result.ErrorMessage ?? string.Empty);
        result.Value.NumberValue.Should().Be(32);
        result.ReferencedIdentifiers.Should().Contain(["var.value", "param.scale", "input"]);
    }

    [Fact]
    public void Evaluator_ResolvesJsonMemberAndIndexAccess()
    {
        var payload = "{\"items\":[{\"value\":10},{\"value\":20}]}";
        var context = CreateContext(
            variables: new Dictionary<string, WorkflowExpressionValue>
            {
                ["payload"] = WorkflowExpressionValue.FromString(payload),
            });

        var expression = "var.payload.items[1].value + var.payload.items.length";

        var result = Evaluator.Evaluate(expression, context);

        result.Success.Should().BeTrue();
        result.Value.NumberValue.Should().Be(22);
    }

    [Fact]
    public void Evaluator_InvokesHelpers()
    {
        var context = CreateContext();

        var result = Evaluator.Evaluate("max(abs(-4), sqr(3))", context);

        result.Success.Should().BeTrue();
        result.Value.NumberValue.Should().Be(9);
        result.ReferencedIdentifiers.Should().Contain("max");
    }

    [Fact]
    public void Evaluator_UsesDateHelpers()
    {
        var context = CreateContext();

        var result = Evaluator.Evaluate(
            "dayOfWeek(dateAdd('2024-01-01T00:00:00Z', 1, 'day'), 'en')",
            context);

        result.Success.Should().BeTrue(result.ErrorMessage ?? string.Empty);
        result.Value.StringValue.Should().Be("Tuesday");
    }

    [Fact]
    public void Evaluator_UsesStringHelpers_ForNumericResult()
    {
        var context = CreateContext();

        var result = Evaluator.Evaluate("length(trim(toUpper('  hello  ')))", context);

        result.Success.Should().BeTrue(result.ErrorMessage ?? string.Empty);
        result.Value.NumberValue.Should().Be(5);
    }

    [Fact]
    public void Evaluator_UsesStringHelpers_ForSubstringAndReplace()
    {
        var context = CreateContext();

        var result = Evaluator.Evaluate(
            "substring(replace('Magic Agent', ' ', ''), 5, 5)",
            context);

        result.Success.Should().BeTrue(result.ErrorMessage ?? string.Empty);
        result.Value.StringValue.Should().Be("Agent");
    }

    [Fact]
    public void Evaluator_UsesJsonHelpers()
    {
        var context = CreateContext();

        var expression = "jsonToString(stringToJson('{\"value\":42}'))";

        var result = Evaluator.Evaluate(expression, context);

        result.Success.Should().BeTrue(result.ErrorMessage ?? string.Empty);
        result.Value.StringValue.Should().Be("{\"value\":42}");
    }

    [Fact]
    public void Evaluator_EvaluatesConditional()
    {
        var context = CreateContext(stepInput: "hello");

        var result = Evaluator.Evaluate("input == 'hello' ? 'match' : 'nope'", context);

        result.Success.Should().BeTrue();
        result.Value.StringValue.Should().Be("match");
    }

    [Fact]
    public void PlaceholderResolver_EvaluatesExpressions()
    {
        WorkflowPlaceholderResolver.Configure(Evaluator);

        WorkflowPlaceholderResolver.ResolveString(
                "Value=${{ 1 + 1 }}",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                null,
                null,
                null)
            .Should().Be("Value=2");

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["value"] = "-5",
        };
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["scale"] = "2",
        };

        var source = new Dictionary<string, string>
        {
            ["message"] = "Resolved=${{ abs(var.value) + param.scale }}",
        };

        var resolution = WorkflowPlaceholderResolver.ResolveDictionaryWithDebug(
            source,
            variables,
            parameters,
            stepInput: null,
            lastStepOutput: null);

        resolution.Debug["message"].Placeholders.Should().Contain(
            ["var.value", "param.scale"],
            "placeholders: {0}; errors: {1}; resolved: {2}",
            string.Join(", ", resolution.Debug["message"].Placeholders),
            string.Join("; ", resolution.Debug["message"].ExpressionErrors),
            resolution.ResolvedValues["message"]);

        resolution.Debug["message"].ExpressionErrors.Should().BeEmpty(
            string.Join(", ", resolution.Debug["message"].ExpressionErrors));
        resolution.ResolvedValues["message"].Should().Be("Resolved=7", resolution.Debug["message"].ResolvedValue);

        WorkflowPlaceholderResolver.ResolveString(
                "Resolved=${{ abs(var.value) + param.scale }}",
                variables,
                parameters,
                null,
                null)
            .Should().Be("Resolved=7");
    }

    private static WorkflowExpressionContext CreateContext(
        IReadOnlyDictionary<string, WorkflowExpressionValue>? variables = null,
        IReadOnlyDictionary<string, WorkflowExpressionValue>? parameters = null,
        string? stepInput = null,
        string? lastStepOutput = null)
    {
        return new WorkflowExpressionContext(
            variables ?? new Dictionary<string, WorkflowExpressionValue>(StringComparer.OrdinalIgnoreCase),
            parameters ?? new Dictionary<string, WorkflowExpressionValue>(StringComparer.OrdinalIgnoreCase),
            stepInput,
            lastStepOutput);
    }
}
