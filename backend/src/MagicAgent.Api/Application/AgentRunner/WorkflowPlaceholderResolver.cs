using System.Text.RegularExpressions;

namespace MagicAgent.Api.Application.AgentRunner;

internal static partial class WorkflowPlaceholderResolver
{
    private const string VariablePrefix = "var.";
    private const string ParameterPrefixShort = "param.";
    private const string ParameterPrefixLong = "parameter.";
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private static readonly IReadOnlyDictionary<string, string> EmptyDictionary = new Dictionary<string, string>(Comparer);

    internal static WorkflowParameterResolution ResolveDictionaryWithDebug(
        IDictionary<string, string>? source,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, string>? workflowParameters,
        string? stepInput,
        string? lastStepOutput)
    {
        if (source is null || source.Count == 0)
        {
            return new WorkflowParameterResolution(EmptyDictionary, new Dictionary<string, WorkflowParameterDebugInfo>(Comparer));
        }

        var resolved = new Dictionary<string, string>(source.Count, Comparer);
        var debug = new Dictionary<string, WorkflowParameterDebugInfo>(source.Count, Comparer);

        foreach (var kvp in source)
        {
            var (value, placeholders) = ResolveStringWithDebug(kvp.Value ?? string.Empty, variables, workflowParameters, stepInput, lastStepOutput);
            resolved[kvp.Key] = value;
            debug[kvp.Key] = new WorkflowParameterDebugInfo(
                kvp.Value ?? string.Empty,
                value,
                placeholders);
        }

        return new WorkflowParameterResolution(resolved, debug);
    }

    internal static IReadOnlyDictionary<string, string> ResolveDictionary(
        IDictionary<string, string>? source,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, string>? workflowParameters,
        string? stepInput,
        string? lastStepOutput)
    {
        return ResolveDictionaryWithDebug(source, variables, workflowParameters, stepInput, lastStepOutput).ResolvedValues;
    }

    internal static string ResolveString(
        string value,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, string>? workflowParameters,
        string? stepInput,
        string? lastStepOutput)
        => ResolveStringWithDebug(value, variables, workflowParameters, stepInput, lastStepOutput).ResolvedValue;

    private static (string ResolvedValue, IReadOnlyList<string> Placeholders) ResolveStringWithDebug(
        string value,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, string>? workflowParameters,
        string? stepInput,
        string? lastStepOutput)
    {
        var source = value ?? string.Empty;

        if (string.IsNullOrEmpty(source))
        {
            return (source, Array.Empty<string>());
        }

        var parameterDictionary = workflowParameters ?? EmptyDictionary;
        var placeholderList = new List<string>();
        var placeholderSet = new HashSet<string>(Comparer);

        void RecordPlaceholder(string expression)
        {
            if (!string.IsNullOrWhiteSpace(expression) && placeholderSet.Add(expression))
            {
                placeholderList.Add(expression);
            }
        }

        var resolved = PlaceholderPatternRegex.Replace(source, match =>
        {
            var expression = match.Groups["expr"].Value.Trim();

            if (string.IsNullOrEmpty(expression))
            {
                return match.Value;
            }

            if (Comparer.Equals(expression, "input"))
            {
                RecordPlaceholder(expression);
                return stepInput ?? string.Empty;
            }

            if (Comparer.Equals(expression, "lastOutput"))
            {
                RecordPlaceholder(expression);
                return lastStepOutput ?? string.Empty;
            }

            if (TryResolveWorkflowParameter(expression, parameterDictionary, out var parameterValue))
            {
                RecordPlaceholder(expression);
                return parameterValue ?? string.Empty;
            }

            var originalExpression = expression;
            if (expression.StartsWith(VariablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                expression = expression[VariablePrefix.Length..];
            }

            if (variables.TryGetValue(expression, out var variableValue))
            {
                RecordPlaceholder(originalExpression);
                return variableValue ?? string.Empty;
            }

            return match.Value;
        });

        var placeholders = placeholderList.Count == 0
            ? Array.Empty<string>()
            : placeholderList.ToArray();

        return (resolved, placeholders);
    }

    private static bool TryResolveWorkflowParameter(
        string expression,
        IReadOnlyDictionary<string, string> workflowParameters,
        out string? value)
    {
        value = null;

        if (workflowParameters.Count == 0)
        {
            return false;
        }

        string parameterKey;

        if (expression.StartsWith(ParameterPrefixShort, StringComparison.OrdinalIgnoreCase))
        {
            parameterKey = expression[ParameterPrefixShort.Length..];
        }
        else if (expression.StartsWith(ParameterPrefixLong, StringComparison.OrdinalIgnoreCase))
        {
            parameterKey = expression[ParameterPrefixLong.Length..];
        }
        else
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameterKey))
        {
            return false;
        }

        if (workflowParameters.TryGetValue(parameterKey, out var parameterValue))
        {
            value = parameterValue ?? string.Empty;
            return true;
        }

        return false;
    }

    [GeneratedRegex("\\{\\{(?<expr>[^{}]+)\\}\\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();

    private static Regex PlaceholderPatternRegex => PlaceholderPattern();

    internal sealed record WorkflowParameterResolution(
        IReadOnlyDictionary<string, string> ResolvedValues,
        IReadOnlyDictionary<string, WorkflowParameterDebugInfo> Debug);

    // WorkflowParameterDebugInfo is defined elsewhere
}