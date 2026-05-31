using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AdminDb.Database.Extensions;
using AdminDb.Database.Provider;
using AdminDb.Database.Shared;
using Sharp.Shared;
using SqlSugar;

namespace AdminDb.Database;

public sealed class AdminDbDatabasePlugin : IModSharpModule
{
    public string DisplayName   => "AdminDb.Database";
    public string DisplayAuthor => "yappershq";

    private readonly InterfaceBridge                      _bridge;
    private readonly ILogger<AdminDbDatabasePlugin>       _logger;
    private readonly ServiceProvider                      _serviceProvider;

    public AdminDbDatabasePlugin(
        ISharedSystem   sharedSystem,
        string?         dllPath,
        string?         sharpPath,
        Version?        version,
        IConfiguration? coreConfiguration,
        bool            hotReload)
    {
        ArgumentNullException.ThrowIfNull(sharpPath);

        _bridge = new InterfaceBridge(this, sharedSystem, sharpPath);
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<AdminDbDatabasePlugin>();

        var configuration = LoadConfiguration(sharpPath);

        var services = new ServiceCollection();
        services.AddSingleton(_bridge);
        services.AddSingleton(sharedSystem);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(sharedSystem.GetLoggerFactory());

        var dbType = (configuration["Database:Type"] ?? "mysql").ToLowerInvariant();

        _logger.LogInformation("[AdminDb.Database] Using SqlSugar ({DbType})", dbType);

        services.AddSingleton<ISqlSugarClient>(_ =>
            new SqlSugarScope(SqlSugarAdminDbProvider.BuildConnectionConfig(configuration)));
        services.AddSingleton<IDatabaseProvider, SqlSugarAdminDbProvider>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public bool Init() => true;

    public void PostInit()
    {
        var provider = _serviceProvider.GetRequiredService<IDatabaseProvider>();
        _bridge.SharpModuleManager.RegisterSharpModuleInterface<IDatabaseProvider>(
            _bridge.Module, IDatabaseProvider.Identity, provider);

        _logger.LogInformation("[AdminDb.Database] Registered IDatabaseProvider ({Id})", IDatabaseProvider.Identity);
    }

    public void OnAllModulesLoaded() { }
    public void OnLibraryConnected(string name) { }
    public void OnLibraryDisconnect(string name) { }

    public void Shutdown() => _serviceProvider.Dispose();

    private const string DefaultConfig =
        """
        {
            "Database": {
                "Type": "mysql",
                "Host": "localhost",
                "Port": 3306,
                "Database": "cs2admins",
                "User": "root",
                "Password": ""
            }
        }
        """;

    private static IConfigurationRoot LoadConfiguration(string sharpPath)
    {
        var configPath = Path.Combine(sharpPath, "configs", "cs2-admin-db", "cs2-admin-db.database.jsonc");

        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, DefaultConfig);
        }

        return new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();
    }
}
