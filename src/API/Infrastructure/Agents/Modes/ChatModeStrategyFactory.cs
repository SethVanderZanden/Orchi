namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class ChatModeStrategyFactory
{
    private readonly IReadOnlyDictionary<ChatMode, IChatModeStrategy> _strategies;

    public ChatModeStrategyFactory(IEnumerable<IChatModeStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(strategy => strategy.Mode);
    }

    public IChatModeStrategy GetStrategy(ChatMode mode)
    {
        if (_strategies.TryGetValue(mode, out IChatModeStrategy? strategy))
        {
            return strategy;
        }

        throw new InvalidOperationException($"No chat mode strategy registered for '{mode}'.");
    }
}
