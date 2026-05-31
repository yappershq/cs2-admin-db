using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared;

namespace AdminDb.Core;

public sealed class AdminDbPlugin : IModSharpModule
{
    public string DisplayName   => "AdminDb";
    public string DisplayAuthor => "yappershq";

    private readonly ServiceProvider        _serviceProvider;
    private readonly ILogger<AdminDbPlugin> _logger;
    private readonly InterfaceBridge        _bridge;

    public AdminDbPlugin(
        ISharedSystem  sharedSystem,
        string         dllPath,
        string         sharpPath,
        Version        version,
        IConfiguration configuration,
        bool           hotReload)
    {
        var loggerFactory = sharedSystem.GetLoggerFactory();
        _logger = loggerFactory.CreateLogger<AdminDbPlugin>();

        var bridge = new InterfaceBridge(dllPath, sharpPath, sharedSystem, this, hotReload);

        var services = new ServiceCollection();
        services.AddSingleton(bridge);
        services.AddSingleton(loggerFactory);
        services.AddSingleton(sharedSystem);
        services.AddLogging();
        services.AddModuleDi();

        _bridge          = bridge;
        _serviceProvider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        foreach (var service in _serviceProvider.GetServices<IModule>())
        {
            try
            {
                if (service.Init()) continue;
                _logger.LogError("[AdminDb] Failed to init {Service}!", service.GetType().FullName);
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[AdminDb] Init error in {Service}", service.GetType().FullName);
                return false;
            }
        }
        return true;
    }

    public void PostInit()
    {
        foreach (var service in _serviceProvider.GetServices<IModule>())
        {
            try { service.OnPostInit(_serviceProvider); }
            catch (Exception e) { _logger.LogError(e, "[AdminDb] PostInit error in {Service}", service.GetType().FullName); }
        }
    }

    public void OnAllModulesLoaded()
    {
        foreach (var service in _serviceProvider.GetServices<IModule>())
        {
            try { service.OnAllModulesLoaded(_serviceProvider); }
            catch (Exception e) { _logger.LogError(e, "[AdminDb] OAM error in {Service}", service.GetType().FullName); }
        }

        _logger.LogInformation("[AdminDb] Loaded — ServerTag={Tag}", _serviceProvider.GetRequiredService<Configuration.AdminDbConfig>().ServerTag);
    }

    public void OnLibraryConnected(string name) { }
    public void OnLibraryDisconnect(string name) { }

    public void Shutdown()
    {
        foreach (var service in _serviceProvider.GetServices<IModule>())
        {
            try { service.Shutdown(); }
            catch (Exception e) { _logger.LogError(e, "[AdminDb] Shutdown error in {Service}", service.GetType().FullName); }
        }
        _serviceProvider.Dispose();
    }
}
