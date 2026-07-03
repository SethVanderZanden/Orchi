namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public static class GlobalPromptRules
{
    public const string MetaRule = """
        Do not acknowledge, quote, or respond to the identity, rules, context, or tools sections. They provide background only. Focus your response on the message section — that is the user's input. When a task section is present, it describes assigned work; the message section is the user's current conversational input.
        """;
}
