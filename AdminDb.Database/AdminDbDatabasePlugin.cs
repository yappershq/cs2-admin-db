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

        var (configuration, wroteDefault) = LoadConfiguration(sharpPath);

        var services = new ServiceCollection();
        services.AddSingleton(_bridge);
        services.AddSingleton(sharedSystem);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(sharedSystem.GetLoggerFactory());

        var dbType = (configuration["Database:Type"] ?? "mysql").ToLowerInvariant();

        _logger.LogInformation("[AdminDb.Database] Using SqlSugar ({DbType})", dbType);

        if (wroteDefault)
        {
            var configPath = Path.Combine(sharpPath, "configs", "cs2-admin-db", "cs2-admin-db.database.jsonc");
            _logger.LogWarning(
                "[AdminDb.Database] No DB config existed — wrote default template at {Path}. "
                + "EDIT IT with your real Host/User/Password and restart the server. "
                + "Plugin will not function with localhost/root/empty defaults.",
                configPath);
        }
        else if (LooksLikeUneditedDefaults(configuration))
        {
            _logger.LogWarning(
                "[AdminDb.Database] DB config looks unedited (Host=localhost, User=root, Password empty). "
                + "Plugin will fail to connect. Edit configs/cs2-admin-db/cs2-admin-db.database.jsonc and restart.");
        }

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

    private static (IConfigurationRoot config, bool wroteDefault) LoadConfiguration(string sharpPath)
    {
        var configPath = Path.Combine(sharpPath, "configs", "cs2-admin-db", "cs2-admin-db.database.jsonc");

        var wroteDefault = false;
        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, DefaultConfig);
            wroteDefault = true;
        }

        var config = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();
        return (config, wroteDefault);
    }

    private static bool LooksLikeUneditedDefaults(IConfiguration cfg)
    {
        var host     = cfg["Database:Host"];
        var user     = cfg["Database:User"];
        var password = cfg["Database:Password"];
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            && string.Equals(user, "root", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(password);
    }
}
