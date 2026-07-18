namespace Orchi.Api.Infrastructure.Scripts.Actions;

public interface IScriptActionStrategyFactory
{
    IScriptActionStrategy GetStrategy(string kind);
}

public sealed class ScriptActionStrategyFactory(IEnumerable<IScriptActionStrategy> strategies)
    : IScriptActionStrategyFactory
{
    private readonly Dictionary<string, IScriptActionStrategy> _strategies =
        strategies.ToDictionary(strategy => strategy.Kind, StringComparer.OrdinalIgnoreCase);

    public IScriptActionStrategy GetStrategy(string kind)
    {
        if (_strategies.TryGetValue(kind, out IScriptActionStrategy? strategy))
        {
            return strategy;
        }

        throw new InvalidOperationException($"No script action strategy registered for '{kind}'.");
    }
}
