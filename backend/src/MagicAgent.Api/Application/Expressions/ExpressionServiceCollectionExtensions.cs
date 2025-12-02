using System;
using MagicAgent.Api.Application.AgentRunner;
using MagicAgent.Api.Application.Expressions.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagicAgent.Api.Application.Expressions
{
    public static class ExpressionServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowExpressionServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IWorkflowHelperRegistry>(_ =>
            {
                Type[] helperTypes =
                {
                    typeof(MathWorkflowHelpers),
                    typeof(DateWorkflowHelpers),
                    typeof(StringWorkflowHelpers),
                    typeof(JsonWorkflowHelpers),
                };

                return new WorkflowHelperRegistry(helperTypes);
            });

            services.AddSingleton<IWorkflowExpressionEvaluator>(sp =>
            {
                var registry = sp.GetRequiredService<IWorkflowHelperRegistry>();
                var logger = sp.GetRequiredService<ILogger<WorkflowExpressionEvaluator>>();
                var evaluator = new WorkflowExpressionEvaluator(registry, logger);
                WorkflowPlaceholderResolver.Configure(evaluator);
                return evaluator;
            });

            return services;
        }
    }
}
