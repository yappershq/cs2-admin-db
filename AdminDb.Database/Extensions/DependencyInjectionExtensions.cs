using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdminDb.Database.Extensions;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddLogging(
        this IServiceCollection services,
        ILoggerFactory factory)
    {
        services.AddSingleton(factory);
        services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        return services;
    }
}
