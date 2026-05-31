using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Managers;

namespace AdminDb.Core;

internal sealed class InterfaceBridge
{
    internal string  DllPath   { get; }
    internal string  SharpPath { get; }
    internal bool    HotReload { get; }

    internal IModSharpModule    Module             { get; }
    internal ISharedSystem      SharedSystem       { get; }
    internal ISharpModuleManager SharpModuleManager { get; }
    internal IModSharp           ModSharp           { get; }
    internal ILoggerFactory      LoggerFactory      { get; }

    public InterfaceBridge(
        string          dllPath,
        string          sharpPath,
        ISharedSystem   sharedSystem,
        IModSharpModule module,
        bool            hotReload)
    {
        DllPath   = dllPath;
        SharpPath = sharpPath;
        HotReload = hotReload;
        Module    = module;

        SharedSystem       = sharedSystem;
        SharpModuleManager = sharedSystem.GetSharpModuleManager();
        ModSharp           = sharedSystem.GetModSharp();
        LoggerFactory      = sharedSystem.GetLoggerFactory();
    }
}
