using System.Collections.Generic;
using MagicAgent.Api.Application.AgentRunner;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MagicAgent.Api.Tests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAgentDefinitionsProvider>();
            services.RemoveAll<IAgentDefinitionValueResolver>();

            services.AddSingleton<IAgentDefinitionsProvider>(_ => new TestAgentDefinitionsProvider());
            services.AddSingleton<IAgentDefinitionValueResolver, PassthroughAgentDefinitionValueResolver>();
        });
    }

    private sealed class TestAgentDefinitionsProvider : IAgentDefinitionsProvider
    {
        private static readonly AgentDefinition TestAgentDefinition = new()
        {
            Id = "chat-agent",
            Name = "Test Chat Agent",
            Description = "Integration test agent",
            DefaultParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["model"] = "test-model",
            },
            Steps = new List<AgentStepDefinition>
            {
                new()
                {
                    Name = "fallback-echo",
                    Type = "echo",
                    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["message"] = "[agent-framework-fallback] {{input}}",
                    },
                    VariableTypes = new Dictionary<string, WorkflowVariableDataType>(StringComparer.OrdinalIgnoreCase),
                    Options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    Outcomes = new List<AgentStepOutcomeDefinition>
                    {
                        new()
                        {
                            Name = "complete",
                            NextStep = null,
                            Condition = null,
                            EndWorkflow = true,
                            Order = 1,
                        },
                    },
                    IsStartStep = true,
                },
            },
            Tools = new List<AgentToolDefinition>(),
        };

        public Task<AgentDefinitionsDocument> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            var document = new AgentDefinitionsDocument
            {
                Agents = new List<AgentDefinition> { TestAgentDefinition },
            };

            return Task.FromResult(document);
        }

        public Task<AgentDefinition?> GetAgentDefinitionAsync(string agentId, CancellationToken cancellationToken = default)
        {
            var definition = string.Equals(agentId, TestAgentDefinition.Id, StringComparison.OrdinalIgnoreCase)
                ? TestAgentDefinition
                : null;

            return Task.FromResult(definition);
        }

        public Task SaveDefinitionsAsync(AgentDefinitionsDocument document, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PassthroughAgentDefinitionValueResolver : IAgentDefinitionValueResolver
    {
        public AgentDefinition Resolve(AgentDefinition definition) => definition;
    }
}
