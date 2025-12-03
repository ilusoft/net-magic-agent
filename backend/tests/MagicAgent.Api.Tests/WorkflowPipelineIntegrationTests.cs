using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using MagicAgent.Api.Application.AgentRunner;
using MagicAgent.Api.Application.Expressions;
using MagicAgent.Api.Application.Expressions.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace MagicAgent.Api.Tests;

public class WorkflowPipelineIntegrationTests
{
    private static readonly WorkflowExpressionEvaluator Evaluator;
    private static readonly MethodInfo ConvertVariableMethod = typeof(DefaultAgentRunner)
        .GetMethod("ConvertWorkflowVariableValue", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ConvertWorkflowVariableValue not found via reflection.");
    private static readonly MethodInfo BuildVariableExpressionValuesMethod = typeof(DefaultAgentRunner)
        .GetMethod("BuildVariableExpressionValues", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("BuildVariableExpressionValues not found via reflection.");
    private static readonly Type WorkflowVariableStateType = ConvertVariableMethod.ReturnType;

    static WorkflowPipelineIntegrationTests()
    {
        var registry = new WorkflowHelperRegistry(new[]
        {
            typeof(MathWorkflowHelpers),
            typeof(DateWorkflowHelpers),
            typeof(StringWorkflowHelpers),
            typeof(JsonWorkflowHelpers),
            typeof(ArrayWorkflowHelpers),
        });

        Evaluator = new WorkflowExpressionEvaluator(registry, NullLogger<WorkflowExpressionEvaluator>.Instance);
        WorkflowPlaceholderResolver.Configure(Evaluator);
    }

    [Fact]
    public void SetVariables_AssignsTypedValues_AndOutcomeEvaluatesExpressions()
    {
        var stepParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["score"] = "${{ param.base + 2 }}",
            ["approved"] = "${{ param.base >= 8 }}",
        };

        var workflowParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["base"] = "8",
        };

        var variableTypes = new Dictionary<string, WorkflowVariableDataType>(StringComparer.OrdinalIgnoreCase)
        {
            ["score"] = WorkflowVariableDataType.Number,
            ["approved"] = WorkflowVariableDataType.Boolean,
        };

        // Resolve expressions for the setVariables step.
        var resolution = WorkflowPlaceholderResolver.ResolveDictionaryWithDebug(
            stepParameters,
            variables: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            workflowParameters,
            stepInput: null,
            lastStepOutput: null);

        var workflowVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var workflowStates = CreateWorkflowStateDictionary();

        foreach (var kvp in resolution.Values)
        {
            var targetType = variableTypes[kvp.Key];
            var state = InvokeConvert(kvp.Value, WorkflowExpressionValueConverter.ToStringValue(kvp.Value), targetType);
            workflowVariables[kvp.Key] = GetProperty<string>(state, "ConvertedValue");
            workflowStates[kvp.Key] = state;
        }

        // Build expression dictionaries like DefaultAgentRunner would.
        var expressionVariables = InvokeBuildVariableExpressionValues(workflowVariables, workflowStates);
        var expressionParameters = new Dictionary<string, WorkflowExpressionValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["threshold"] = WorkflowExpressionValue.FromNumber(10),
        };

        var definition = new AgentDefinition
        {
            Id = "test",
            Steps =
            [
                new AgentStepDefinition
                {
                    Name = "SetVars",
                    Type = "setVariables",
                    Parameters = stepParameters,
                    VariableTypes = variableTypes,
                    Outcomes =
                    [
                        new AgentStepOutcomeDefinition
                        {
                            Name = "assign-high",
                            NextStep = "Echo",
                            Condition = new AgentStepOutcomeConditionDefinition
                            {
                                Expression = "var.score >= param.threshold && var.approved",
                            },
                        },
                    ],
                },
                new AgentStepDefinition { Name = "Echo", Type = "echo", Parameters = new Dictionary<string, string>() },
            ],
        };

        var stepLookup = new Dictionary<string, AgentStepDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["SetVars"] = definition.Steps[0],
            ["Echo"] = definition.Steps[1],
        };

        var outcome = StepOutcomeResolver.ResolveNextStep(
            definition,
            stepLookup,
            definition.Steps[0],
            stepInput: null,
            output: string.Empty,
            lastOutput: null,
            variables: expressionVariables,
            parameters: expressionParameters,
            expressionEvaluator: Evaluator,
            logger: NullLogger.Instance);

        outcome.Outcome.Should().Be("assign-high");
        outcome.NextStep.Should().Be("Echo");
        outcome.EndWorkflow.Should().BeFalse();
    }

    [Fact]
    public void SetVariables_AssignsJsonPayload_AndOutcomeEvaluatesNestedExpressions()
    {
        var stepParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["payload"] = """
            {
                "customer": {
                    "tier": "${{ param.tier }}",
                    "age": ${{ param.base }}
                },
                "orders": [
                    { "total": ${{ param.base + 10 }}, "items": 3 },
                    { "total": ${{ param.base - 1 }}, "items": 1 }
                ]
            }
            """,
        };

        var workflowParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["base"] = "8",
            ["tier"] = "gold",
        };

        var variableTypes = new Dictionary<string, WorkflowVariableDataType>(StringComparer.OrdinalIgnoreCase)
        {
            ["payload"] = WorkflowVariableDataType.Json,
        };

        var resolution = WorkflowPlaceholderResolver.ResolveDictionaryWithDebug(
            stepParameters,
            variables: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            workflowParameters,
            stepInput: null,
            lastStepOutput: null);

        var workflowVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var workflowStates = CreateWorkflowStateDictionary();

        foreach (var kvp in resolution.Values)
        {
            var state = InvokeConvert(kvp.Value, WorkflowExpressionValueConverter.ToStringValue(kvp.Value), variableTypes[kvp.Key]);
            workflowVariables[kvp.Key] = GetProperty<string>(state, "ConvertedValue");
            workflowStates[kvp.Key] = state;
        }

        var expressionVariables = InvokeBuildVariableExpressionValues(workflowVariables, workflowStates);
        var expressionParameters = new Dictionary<string, WorkflowExpressionValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["threshold"] = WorkflowExpressionValue.FromNumber(15),
            ["requiredTier"] = WorkflowExpressionValue.FromString("gold"),
        };

        var definition = new AgentDefinition
        {
            Id = "json-test",
            Steps =
            [
                new AgentStepDefinition
                {
                    Name = "SetPayload",
                    Type = "setVariables",
                    Parameters = stepParameters,
                    VariableTypes = variableTypes,
                    Outcomes =
                    [
                        new AgentStepOutcomeDefinition
                        {
                            Name = "route-json",
                            NextStep = "ProcessJson",
                            Condition = new AgentStepOutcomeConditionDefinition
                            {
                                Expression = "var.payload.customer.tier == param.requiredTier && var.payload.orders[0].total >= param.threshold && var.payload.orders.length == 2",
                            },
                        },
                    ],
                },
                new AgentStepDefinition { Name = "ProcessJson", Type = "echo", Parameters = new Dictionary<string, string>() },
            ],
        };

        var stepLookup = new Dictionary<string, AgentStepDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["SetPayload"] = definition.Steps[0],
            ["ProcessJson"] = definition.Steps[1],
        };

        var outcome = StepOutcomeResolver.ResolveNextStep(
            definition,
            stepLookup,
            definition.Steps[0],
            stepInput: null,
            output: string.Empty,
            lastOutput: null,
            variables: expressionVariables,
            parameters: expressionParameters,
            expressionEvaluator: Evaluator,
            logger: NullLogger.Instance);

        outcome.Outcome.Should().Be("route-json");
        outcome.NextStep.Should().Be("ProcessJson");
        outcome.EndWorkflow.Should().BeFalse();
    }

    private static object InvokeConvert(WorkflowExpressionValue typedValue, string rawValue, WorkflowVariableDataType targetType)
        => ConvertVariableMethod.Invoke(null, new object[] { typedValue, rawValue, targetType })
            ?? throw new InvalidOperationException("ConvertWorkflowVariableValue returned null.");

    private static IReadOnlyDictionary<string, WorkflowExpressionValue> InvokeBuildVariableExpressionValues(
        IDictionary<string, string> rawVariables,
        IDictionary workflowStates)
        => (IReadOnlyDictionary<string, WorkflowExpressionValue>)BuildVariableExpressionValuesMethod.Invoke(null, new object[] { rawVariables, workflowStates })!;

    private static IDictionary CreateWorkflowStateDictionary()
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), WorkflowVariableStateType);
        return (IDictionary)Activator.CreateInstance(dictionaryType, StringComparer.OrdinalIgnoreCase)!;
    }

    private static T GetProperty<T>(object state, string propertyName)
    {
        var property = state.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on workflow variable state.");

        return (T)property.GetValue(state)!;
    }
}
