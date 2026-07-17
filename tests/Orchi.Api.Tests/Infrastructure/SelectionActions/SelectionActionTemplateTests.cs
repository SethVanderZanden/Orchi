using Orchi.Api.Infrastructure.SelectionActions;

namespace Orchi.Api.Tests.Infrastructure.SelectionActions;

public class SelectionActionTemplateTests
{
    [Fact]
    public void Apply_ReplacesSelectedTextPlaceholder()
    {
        string result = SelectionActionTemplate.Apply(
            "Please define \"{{selected text}}\" for me in simple terms with an example use-case.",
            "middleware");

        Assert.Equal(
            "Please define \"middleware\" for me in simple terms with an example use-case.",
            result);
    }

    [Fact]
    public void ContainsSelectedTextPlaceholder_IsWhitespaceInsensitive()
    {
        Assert.True(SelectionActionTemplate.ContainsSelectedTextPlaceholder("x {{ Selected Text }} y"));
        Assert.False(SelectionActionTemplate.ContainsSelectedTextPlaceholder("no placeholder"));
    }
}
