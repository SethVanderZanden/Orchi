using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Attachments;
using Orchi.Api.Infrastructure.Agents.Attachments.Models;
using Orchi.Api.Infrastructure.Agents.Attachments.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Orchi.Api.Tests.Infrastructure.Agents.Attachments;

public sealed class ChatAttachmentServiceTests : IDisposable
{
    private readonly string _blobRoot;
    private readonly string _workspacePath;

    public ChatAttachmentServiceTests()
    {
        _blobRoot = Path.Combine(Path.GetTempPath(), "orchi-attachment-tests", Guid.NewGuid().ToString("N"));
        _workspacePath = Path.Combine(Path.GetTempPath(), "orchi-workspace-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspacePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_blobRoot))
        {
            Directory.Delete(_blobRoot, recursive: true);
        }

        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
    }

    [Fact]
    public void WorkspaceRelativePath_uses_attachment_folder_layout()
    {
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        string relative = AttachmentPaths.WorkspaceRelative(id, "report.csv");
        Assert.Equal(".orchi/attachments/11111111-1111-1111-1111-111111111111/report.csv", relative);
    }

    [Fact]
    public async Task PrepareTurnAsync_materializes_files_into_workspace()
    {
        var store = new InMemoryChatAttachmentStore();
        ChatAttachmentService service = CreateService(store);
        Guid chatId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();

        await using var stream = new MemoryStream("name,value\na,1"u8.ToArray());
        Result<StoredAttachment> staged = await service.StageUploadAsync(
            chatId,
            "data.csv",
            "text/csv",
            stream.Length,
            stream,
            CancellationToken.None);

        Assert.True(staged.IsSuccess);

        Result<AgentAttachmentContext> prepared = await service.PrepareTurnAsync(
            chatId,
            messageId,
            _workspacePath,
            [staged.Value.Id],
            CancellationToken.None);

        Assert.True(prepared.IsSuccess);
        string absolutePath = AttachmentPaths.Absolute(_workspacePath, staged.Value.WorkspaceRelativePath);
        Assert.True(File.Exists(absolutePath));
        Assert.Contains("name,value", await File.ReadAllTextAsync(absolutePath));
    }

    [Fact]
    public async Task OnWorkspaceChangedAsync_rematerializes_existing_attachments()
    {
        var store = new InMemoryChatAttachmentStore();
        ChatAttachmentService service = CreateService(store);
        Guid chatId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();

        await using var stream = new MemoryStream("hello"u8.ToArray());
        Result<StoredAttachment> staged = await service.StageUploadAsync(
            chatId,
            "notes.txt",
            "text/plain",
            stream.Length,
            stream,
            CancellationToken.None);

        Assert.True(staged.IsSuccess);

        await service.PrepareTurnAsync(
            chatId,
            messageId,
            _workspacePath,
            [staged.Value.Id],
            CancellationToken.None);

        string newWorkspace = Path.Combine(Path.GetTempPath(), "orchi-workspace-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(newWorkspace);

        try
        {
            await service.OnWorkspaceChangedAsync(chatId, newWorkspace, CancellationToken.None);
            string absolutePath = AttachmentPaths.Absolute(newWorkspace, staged.Value.WorkspaceRelativePath);
            Assert.True(File.Exists(absolutePath));
        }
        finally
        {
            Directory.Delete(newWorkspace, recursive: true);
        }
    }

    private ChatAttachmentService CreateService(IChatAttachmentStore store) =>
        new(
            store,
            Options.Create(new AttachmentOptions { BlobRoot = _blobRoot }),
            NullLogger<ChatAttachmentService>.Instance);

    private sealed class InMemoryChatAttachmentStore : IChatAttachmentStore
    {
        private readonly List<StoredAttachment> _attachments = [];

        public Task<StoredAttachment> CreateStagedAsync(
            CreateStagedAttachmentModel model,
            CancellationToken cancellationToken)
        {
            var stored = new StoredAttachment(
                model.Id,
                model.ChatId,
                null,
                model.FileName,
                model.ContentType,
                model.SizeBytes,
                model.WorkspaceRelativePath,
                model.ExtractedText,
                DateTimeOffset.UtcNow,
                0);
            _attachments.Add(stored);
            return Task.FromResult(stored);
        }

        public Task<bool> LinkToMessageAsync(
            Guid attachmentId,
            Guid chatId,
            Guid messageId,
            int ordinal,
            CancellationToken cancellationToken)
        {
            int index = _attachments.FindIndex(attachment => attachment.Id == attachmentId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            StoredAttachment current = _attachments[index];
            _attachments[index] = current with { MessageId = messageId, Ordinal = ordinal };
            return Task.FromResult(true);
        }

        public Task<StoredAttachment?> GetAsync(
            Guid chatId,
            Guid attachmentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_attachments.FirstOrDefault(attachment =>
                attachment.ChatId == chatId && attachment.Id == attachmentId));

        public Task<IReadOnlyList<StoredAttachment>> ListByChatAsync(
            Guid chatId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoredAttachment>>(
                _attachments.Where(attachment => attachment.ChatId == chatId).ToArray());

        public Task<IReadOnlyList<StoredAttachment>> ListByMessageAsync(
            Guid chatId,
            Guid messageId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoredAttachment>>(
                _attachments
                    .Where(attachment => attachment.ChatId == chatId && attachment.MessageId == messageId)
                    .ToArray());

        public Task<bool> DeleteStagedAsync(
            Guid chatId,
            Guid attachmentId,
            CancellationToken cancellationToken)
        {
            int removed = _attachments.RemoveAll(attachment =>
                attachment.ChatId == chatId
                && attachment.Id == attachmentId
                && attachment.MessageId is null);

            return Task.FromResult(removed > 0);
        }
    }
}
