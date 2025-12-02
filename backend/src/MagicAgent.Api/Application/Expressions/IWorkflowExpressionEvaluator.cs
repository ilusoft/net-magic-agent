using System.Threading;

namespace MagicAgent.Api.Application.Expressions;

/// <summary>
/// Evaluates workflow expressions within a given context.
/// </summary>
public interface IWorkflowExpressionEvaluator
{
    WorkflowExpressionResult Evaluate(
        string expression,
        WorkflowExpressionContext context,
        CancellationToken cancellationToken = default);
}
