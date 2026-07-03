using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes.Prompt;

public class OrchiPromptRendererTests
{
    private readonly OrchiPromptRenderer _renderer = new();

    [Fact]
    public void Render_OmitsEmptySections()
    {
        var document = new OrchiPromptDocument
        {
            Rules = "Follow instructions.",
            Message = "hi",
        };

        string prompt = _renderer.Render(document);

        Assert.Equal("<orchi><rules>Follow instructions.</rules><message>hi</message></orchi>", prompt);
        Assert.DoesNotContain("<identity>", prompt);
        Assert.DoesNotContain("<context>", prompt);
        Assert.DoesNotContain("<tools>", prompt);
        Assert.DoesNotContain("<task>", prompt);
    }

    [Fact]
    public void Render_UsesCDataWhenContentContainsAmpersand()
    {
        var document = new OrchiPromptDocument
        {
            Message = "foo & bar",
        };

        string prompt = _renderer.Render(document);

        Assert.Contains("<message><![CDATA[foo & bar]]></message>", prompt);
    }

    [Fact]
    public void Render_UsesCDataWhenContentContainsAngleBrackets()
    {
        var document = new OrchiPromptDocument
        {
            Message = "if (x < 5) use <!-- old -->",
        };

        string prompt = _renderer.Render(document);

        Assert.Contains("<message><![CDATA[if (x < 5) use <!-- old -->]]></message>", prompt);
    }

    [Fact]
    public void FormatSectionBody_UsesPlainTextWhenNoSpecialCharacters()
    {
        string body = OrchiPromptRenderer.FormatSectionBody("Plan a refactor");

        Assert.Equal("Plan a refactor", body);
    }
}
