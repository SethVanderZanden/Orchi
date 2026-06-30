using Orchi.Api.Infrastructure.Agents.Cursor;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cursor;

public class CursorToolLabelFormatterTests
{
    [Fact]
    public void Format_WriteToolCallStarted_IncludesVerbAndDetail()
    {
        string label = CursorToolLabelFormatter.Format("writeToolCall", "started", "README.md");

        Assert.Equal("Writing README.md", label);
    }

    [Fact]
    public void Format_GrepToolCallCompleted_IncludesStatusSuffix()
    {
        string label = CursorToolLabelFormatter.Format("grepToolCall", "completed", "Program.cs");

        Assert.Equal("Searching Program.cs (completed)", label);
    }

    [Fact]
    public void Format_UnknownToolCall_StripsSuffixAndIncludesDetail()
    {
        string label = CursorToolLabelFormatter.Format("mcpToolCall", "started", "-y some-package");

        Assert.Equal("mcp -y some-package", label);
    }

    [Fact]
    public void Format_UnknownToolWithoutDetail_ReturnsNameOnly()
    {
        string label = CursorToolLabelFormatter.Format("customAction", "started", null);

        Assert.Equal("customAction", label);
    }
}
