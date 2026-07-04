using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class ChatPersistenceEndpointTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"orchi-persist-{Guid.NewGuid():N}.db");
    private SharedDatabaseWebApplicationFactory? _firstFactory;
    private HttpClient? _firstClient;
    private bool _disposed;

    public Task InitializeAsync()
    {
        _firstFactory = new SharedDatabaseWebApplicationFactory(_databasePath);
        _firstFactory.InitializeDatabase();
        _firstClient = _firstFactory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _disposed = true;
        _firstClient?.Dispose();
        _firstFactory?.Dispose();

        foreach (string path in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Chat_SurvivesApiRestart()
    {
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_firstClient!, workspace);

        HttpResponseMessage createResponse = await _firstClient!.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspaceId));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);

        _firstClient.Dispose();
        _firstFactory!.Dispose();
        _firstClient = null;
        _firstFactory = null;

        await using var secondFactory = new SharedDatabaseWebApplicationFactory(_databasePath);
        secondFactory.InitializeDatabase();
        using HttpClient secondClient = secondFactory.CreateClient();

        HttpResponseMessage listResponse = await secondClient.GetAsync("/chats");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        ChatSummaryResponse[]? chats = await listResponse.Content.ReadFromJsonAsync<ChatSummaryResponse[]>();
        Assert.NotNull(chats);
        Assert.Contains(chats, chat => chat.Id == created.Id);

        HttpResponseMessage getResponse = await secondClient.GetAsync($"/chats/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        ChatDetailResponse? detail = await getResponse.Content.ReadFromJsonAsync<ChatDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
    }
}
