using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Agents.ListAgentModes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class ListAgentModesEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ListAgentModesEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAgentModes_ReturnsRegisteredModes()
    {
        HttpResponseMessage response = await _client.GetAsync("/agents/modes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ListAgentModes.ModeResponse[]? modes =
            await response.Content.ReadFromJsonAsync<ListAgentModes.ModeResponse[]>();

        Assert.NotNull(modes);
        Assert.Contains(modes, mode => mode.Id == "default" && mode.Label == "Default");
        Assert.Contains(
            modes,
            mode => mode.Id == "orchestration" && mode.Label == "Orchestration" && mode.Description is not null);
        Assert.Contains(
            modes,
            mode => mode.Id == "review" && mode.Label == "Review" && mode.Description is not null);
        Assert.DoesNotContain(modes, mode => mode.Id == "implementation");
    }
}
