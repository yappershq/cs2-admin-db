using System.IO;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Managers;

namespace AdminDb.Database;

internal sealed class InterfaceBridge
{
    public static InterfaceBridge Instance { get; private set; } = null!;

    public string               SharpPath          { get; }
    public string               ConfigPath         { get; }
    public IModSharp            ModSharp           { get; }
    public ILoggerFactory       LoggerFactory      { get; }
    public ISharpModuleManager  SharpModuleManager { get; }
    public AdminDbDatabasePlugin Module            { get; }

    public InterfaceBridge(AdminDbDatabasePlugin module, ISharedSystem sharedSystem, string sharpPath)
    {
        Instance = this;

        Module    = module;
        SharpPath = sharpPath;
        ConfigPath = Path.GetFullPath(Path.Combine(sharpPath, "configs"));

        ModSharp           = sharedSystem.GetModSharp();
        LoggerFactory      = sharedSystem.GetLoggerFactory();
        SharpModuleManager = sharedSystem.GetSharpModuleManager();

        Directory.CreateDirectory(ConfigPath);
    }
}
