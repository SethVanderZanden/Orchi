namespace Orchi.Api.Infrastructure.UserPreferences;

public static class UserPreferencesExtensions
{
    public static IServiceCollection AddOrchiUserPreferences(this IServiceCollection services)
    {
        services.AddSingleton<IUserPreferenceStore, EfUserPreferenceStore>();
        services.AddSingleton<IUserPreferenceService, UserPreferenceService>();

        return services;
    }
}
