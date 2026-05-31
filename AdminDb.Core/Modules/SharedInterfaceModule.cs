using System;
using AdminDb.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdminDb.Core.Modules;

internal sealed class SharedInterfaceModule : IModule, IAdminDbShared
{
    private readonly InterfaceBridge                  _bridge;
    private readonly ILogger<SharedInterfaceModule>   _logger;
    private readonly AdminMountManager                _mountManager;

    public event Action? OnRefreshed;

    public SharedInterfaceModule(
        InterfaceBridge                 bridge,
        ILogger<SharedInterfaceModule>  logger,
        AdminMountManager               mountManager)
    {
        _bridge       = bridge;
        _logger       = logger;
        _mountManager = mountManager;
    }

    public bool Init() => true;

    public void OnPostInit(ServiceProvider provider)
    {
        _bridge.SharpModuleManager.RegisterSharpModuleInterface<IAdminDbShared>(
            _bridge.Module, IAdminDbShared.Identity, this);
        _logger.LogInformation("[AdminDb] Registered IAdminDbShared ({Id})", IAdminDbShared.Identity);
    }

    public void RefreshNow()
    {
        _ = System.Threading.Tasks.Task.Run(_mountManager.BuildAndMountAsync);
    }

    internal void RaiseRefreshed()
    {
        try { OnRefreshed?.Invoke(); }
        catch (Exception e) { _logger.LogError(e, "[AdminDb] OnRefreshed subscriber threw"); }
    }
}
