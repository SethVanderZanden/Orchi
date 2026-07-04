using Orchi.Api.Infrastructure.Agents.Cursor;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cursor;

public class CursorModelListParserTests
{
    [Fact]
    public void ParseLine_PlainSlug_ReturnsEntry()
    {
        AgentModelListEntry? entry = CursorModelListParser.ParseLine("claude-4.6-sonnet-medium-thinking");

        Assert.NotNull(entry);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", entry.ModelId);
        Assert.False(entry.IsDefault);
        Assert.False(entry.IsCurrent);
    }

    [Fact]
    public void ParseLine_WithDefaultSuffix_SetsIsDefault()
    {
        AgentModelListEntry? entry = CursorModelListParser.ParseLine("gpt-5.3-codex (default)");

        Assert.NotNull(entry);
        Assert.Equal("gpt-5.3-codex", entry.ModelId);
        Assert.True(entry.IsDefault);
        Assert.False(entry.IsCurrent);
    }

    [Fact]
    public void ParseLine_WithCurrentSuffix_SetsIsCurrent()
    {
        AgentModelListEntry? entry = CursorModelListParser.ParseLine("composer-2.5-fast (current)");

        Assert.NotNull(entry);
        Assert.Equal("composer-2.5-fast", entry.ModelId);
        Assert.False(entry.IsDefault);
        Assert.True(entry.IsCurrent);
    }

    [Fact]
    public void ParseLine_WithBothSuffixes_SetsBothFlags()
    {
        AgentModelListEntry? entry = CursorModelListParser.ParseLine("gpt-5.3-codex (default) (current)");

        Assert.NotNull(entry);
        Assert.Equal("gpt-5.3-codex", entry.ModelId);
        Assert.True(entry.IsDefault);
        Assert.True(entry.IsCurrent);
    }

    [Fact]
    public void Parse_MultilineOutput_ReturnsAllEntries()
    {
        const string output = """
            claude-4.6-sonnet-medium-thinking
            gpt-5.3-codex (default)
            composer-2.5-fast (current)
            """;

        IReadOnlyList<AgentModelListEntry> entries = CursorModelListParser.Parse(output);

        Assert.Equal(3, entries.Count);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", entries[0].ModelId);
        Assert.True(entries[1].IsDefault);
        Assert.True(entries[2].IsCurrent);
    }
}
