using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Cli;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexCliArgumentBuilder(IOptions<CodexAgentOptions> options) : IAgentCliArgumentBuilder
{
    public IReadOnlyList<string> BuildArguments(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        string? entryScript)
    {
        CodexAgentOptions config = options.Value;
        var arguments = new List<string>();

        if (!string.IsNullOrWhiteSpace(entryScript))
        {
            arguments.Add(entryScript);
        }

        arguments.Add("exec");
        arguments.Add("--json");

        foreach (string defaultArg in config.DefaultArgs.Distinct(StringComparer.Ordinal))
        {
            arguments.Add(defaultArg);
        }

        if (!string.IsNullOrWhiteSpace(session.ModelId))
        {
            arguments.Add("--model");
            arguments.Add(session.ModelId);
        }

        IReadOnlyDictionary<string, string> configOverrides =
            ExcludeApprovalPolicy(session.CliConfigOverrides);
        AgentCliConfigArgs.AppendOverrides(arguments, configOverrides);

        AgentCliConfigArgs.AppendOverride(
            arguments,
            AgentCliOptionKinds.ApprovalPolicy,
            ResolveApprovalPolicy(session));

        if (session.ContextSizeTokens is int tokens and > 0
            && !session.CliConfigOverrides.ContainsKey("model_context_window"))
        {
            AgentCliConfigArgs.AppendOverride(arguments, "model_context_window", tokens.ToString());
        }

        AppendNonWhiteSpaceArgs(arguments, extraCliArgs);

        if (!string.IsNullOrWhiteSpace(session.ExternalSessionId))
        {
            arguments.Add("resume");
            arguments.Add(session.ExternalSessionId);
        }

        arguments.Add(prompt);
        return arguments;
    }

    private static IReadOnlyDictionary<string, string> ExcludeApprovalPolicy(
        IReadOnlyDictionary<string, string> overrides)
    {
        if (!overrides.ContainsKey(AgentCliOptionKinds.ApprovalPolicy))
        {
            return overrides;
        }

        return overrides
            .Where(pair => !string.Equals(pair.Key, AgentCliOptionKinds.ApprovalPolicy, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static string ResolveApprovalPolicy(ChatSession session)
    {
        if (session.CliConfigOverrides.TryGetValue(AgentCliOptionKinds.ApprovalPolicy, out string? value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim().Trim('"');
        }

        return CodexBuiltInCatalog.DefaultApprovalPolicyId;
    }

    private static void AppendNonWhiteSpaceArgs(List<string> arguments, IReadOnlyList<string> extraCliArgs)
    {
        foreach (string extraArg in extraCliArgs)
        {
            if (!string.IsNullOrWhiteSpace(extraArg))
            {
                arguments.Add(extraArg);
            }
        }
    }
}
