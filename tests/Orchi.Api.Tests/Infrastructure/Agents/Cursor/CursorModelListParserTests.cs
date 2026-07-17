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
    public void ParseLine_ParameterizedSlug_ReturnsEntry()
    {
        const string slug = "claude-opus-4-8[context=1m,effort=high,fast=false]";

        AgentModelListEntry? entry = CursorModelListParser.ParseLine(slug);

        Assert.NotNull(entry);
        Assert.Equal(slug, entry.ModelId);
    }

    [Fact]
    public void ParseLine_AnsiColoredTip_ReturnsNull()
    {
        string tip =
            "\u001b[2mTip: use \u001b[36m--model <id>\u001b[39m (or \u001b[36m%2Fmodel <id>\u001b[39m in interactive mode) to switch. Parameterized models also accept quoted overrides, e.g. \u001b[36m--model 'claude-opus-4-8[context=1m,effort=high,fast=false]'\u001b[39m.\u001b[22m";

        AgentModelListEntry? entry = CursorModelListParser.ParseLine(tip);

        Assert.Null(entry);
    }

    [Fact]
    public void ParseLine_PlainTip_ReturnsNull()
    {
        AgentModelListEntry? entry = CursorModelListParser.ParseLine(
            "Tip: use --model <id> (or %2Fmodel <id> in interactive mode) to switch.");

        Assert.Null(entry);
    }

    [Fact]
    public void Parse_MultilineOutput_SkipsTipAndKeepsSlugs()
    {
        string tip =
            "\u001b[2mTip: use \u001b[36m--model <id>\u001b[39m to switch.\u001b[22m";
        string output = string.Join(
            '\n',
            "claude-4.6-sonnet-medium-thinking",
            "gpt-5.3-codex (default)",
            tip,
            "composer-2.5-fast (current)");

        IReadOnlyList<AgentModelListEntry> entries = CursorModelListParser.Parse(output);

        Assert.Equal(3, entries.Count);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", entries[0].ModelId);
        Assert.True(entries[1].IsDefault);
        Assert.True(entries[2].IsCurrent);
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
