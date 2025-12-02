using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MagicAgent.Api.Application.AgentRunner;

namespace MagicAgent.Api.Tests;

public class ApiIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Get_ReturnsOkStatus()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AgentRun_Post_ReturnsAgentFrameworkFallback()
    {
        var response = await _client.PostAsJsonAsync("/api/agents/chat-agent/runs", new { input = "hello there" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<AgentWorkflowResultDto>();

        summary.Should().NotBeNull();
        summary!.AgentId.Should().Be("chat-agent");
        summary.Status.Should().Be("completed");
        summary.LastStep.Should().NotBeNull();
        summary.LastStep!.Output.Should().Contain("[agent-framework-fallback]");
    }

    private sealed record AgentWorkflowResultDto(
        string AgentId,
        string Status,
        AgentStepExecutionResult? LastStep,
        string? ConversationId);

    [Fact]
    public async Task AgentRun_Post_WithUnknownAgent_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/agents/missing/runs", new { input = "ping" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

}
