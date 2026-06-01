using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AdminDb.Core.Configuration;
using AdminDb.Core.Database;
using AdminDb.Core.Modules;
using AdminDb.Database.Shared;

namespace AdminDb.Core;

internal static class ModuleDependencyInjection
{
    internal static IServiceCollection AddModuleDi(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            AdminDbConfig.Load(
                sp.GetRequiredService<InterfaceBridge>().SharpPath,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<AdminDbConfig>()));

        services.AddSingleton(sp =>
        {
            var bridge = sp.GetRequiredService<InterfaceBridge>();
            var dbProvider = bridge.SharpModuleManager
                .GetOptionalSharpModuleInterface<IDatabaseProvider>(IDatabaseProvider.Identity)?.Instance!;
            return new AdminDbRepository(dbProvider);
        });

        services.AddSingleton<AdminMountManager>();
        services.AddSingleton<SharedInterfaceModule>();
        services.AddSingleton<ReloadCommandModule>();
        services.AddSingleton<SqlRunnerModule>();

        services.AddSingleton<IModule>(sp => sp.GetRequiredService<AdminMountManager>());
        services.AddSingleton<IModule>(sp => sp.GetRequiredService<SharedInterfaceModule>());
        services.AddSingleton<IModule>(sp => sp.GetRequiredService<ReloadCommandModule>());
        services.AddSingleton<IModule>(sp => sp.GetRequiredService<SqlRunnerModule>());

        return services;
    }
}
