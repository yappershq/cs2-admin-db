using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Enums;

namespace AdminDb.Core.Modules;

internal sealed class ReloadCommandModule : IModule
{
    private readonly InterfaceBridge                    _bridge;
    private readonly ILogger<ReloadCommandModule>       _logger;
    private readonly AdminMountManager                  _mountManager;

    public ReloadCommandModule(
        InterfaceBridge                 bridge,
        ILogger<ReloadCommandModule>    logger,
        AdminMountManager               mountManager)
    {
        _bridge       = bridge;
        _logger       = logger;
        _mountManager = mountManager;
    }

    public bool Init() => true;

    public void OnAllModulesLoaded(ServiceProvider provider)
    {
        var adminManager = _bridge.SharpModuleManager
            .GetOptionalSharpModuleInterface<IAdminManager>(IAdminManager.Identity)?.Instance;

        if (adminManager is null)
            return;

        adminManager.MountAdminManifest("AdminDb.Perms", () => new AdminTableManifest(
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["admindb"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "@admindb/reload",
                },
            },
            [],
            []));

        var registry = adminManager.GetCommandRegistry("AdminDb.Perms");
        registry.RegisterPermissions(ImmutableArray.Create("@admindb/reload"));
        registry.RegisterAdminCommand(
            "admin_reload",
            (client, args) =>
            {
                client?.Print(HudPrintChannel.Chat, "[AdminDb] Refreshing admins from DB...");
                System.Threading.Tasks.Task.Run(_mountManager.BuildAndMountAsync).ContinueWith(_ => { });
            },
            ImmutableArray.Create("@admindb/reload"));

        _logger.LogInformation("[AdminDb] Registered !admin_reload command");
    }

    public void Shutdown() { }
}
