using System.Collections.Generic;
using System.Threading;

namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Provides discovery and invocation services for workflow helper functions.
/// </summary>
public interface IWorkflowHelperRegistry
{
    IReadOnlyList<WorkflowHelperDescriptor> GetDescriptors();

    bool TryInvoke(
        string helperName,
        IReadOnlyList<WorkflowExpressionValue> arguments,
        out WorkflowExpressionValue? result,
        out string? errorMessage,
        CancellationToken cancellationToken = default);
}
