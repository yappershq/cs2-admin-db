using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AdminDb.Database.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Modules.CommandCenter.Shared;

namespace AdminDb.Core.Modules;

// Server-console-only SQL runner.
//
// Usage from RCON / pterodactyl console:
//   admindb_sql_file <name>     -> reads /game/sharp/configs/cs2-admin-db/sql/<name>.sql,
//                                  splits on ';' at line end, runs each statement,
//                                  logs result of each.
//
// File is plain-text SQL. Comments start with '--'. Empty statements skipped.
// Existing migrations remain owned by AdminDbRepository; this is a manual
// out-of-band repair / seed tool.
internal sealed class SqlRunnerModule : IModule
{
    private readonly InterfaceBridge          _bridge;
    private readonly ILogger<SqlRunnerModule> _logger;

    private IDatabaseProvider? _db;

    public SqlRunnerModule(InterfaceBridge bridge, ILogger<SqlRunnerModule> logger)
    {
        _bridge = bridge;
        _logger = logger;
    }

    public bool Init() => true;

    public void OnAllModulesLoaded(ServiceProvider provider)
    {
        var cc = _bridge.SharpModuleManager
            .GetOptionalSharpModuleInterface<ICommandCenter>(ICommandCenter.Identity)?.Instance;
        if (cc is null)
        {
            _logger.LogWarning("[AdminDb.Sql] ICommandCenter not found — admindb_sql_file disabled");
            return;
        }

        _db = _bridge.SharpModuleManager
            .GetOptionalSharpModuleInterface<IDatabaseProvider>(IDatabaseProvider.Identity)?.Instance;
        if (_db is null) return;

        var registry = cc.GetRegistry("AdminDb.Sql");
        registry.RegisterServerCommand(
            "admindb_sql_file",
            args =>
            {
                var name = args.GetArg(1);
                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("[AdminDb.Sql] usage: admindb_sql_file <filename-without-path>");
                    return;
                }

                // Confine reads to /game/sharp/configs/cs2-admin-db/sql/ to avoid
                // accidental traversal — RCON-gated already, this is belt+suspenders.
                if (name.Contains('/') || name.Contains('\\') || name.Contains(".."))
                {
                    _logger.LogWarning("[AdminDb.Sql] '{Name}' contains illegal path chars", name);
                    return;
                }

                var path = Path.Combine(_bridge.SharpPath, "configs", "cs2-admin-db", "sql", name);
                if (!File.Exists(path))
                {
                    _logger.LogWarning("[AdminDb.Sql] file not found: {Path}", path);
                    return;
                }

                _ = Task.Run(() => RunFileAsync(path));
            },
            "Run a SQL file from /game/sharp/configs/cs2-admin-db/sql/");

        _logger.LogInformation("[AdminDb.Sql] Registered admindb_sql_file server command");
    }

    private async Task RunFileAsync(string path)
    {
        try
        {
            var raw = await File.ReadAllTextAsync(path);
            // Strip line comments.
            var stripped = string.Join('\n',
                raw.Split('\n').Select(l =>
                {
                    var idx = l.IndexOf("--", StringComparison.Ordinal);
                    return idx >= 0 ? l[..idx] : l;
                }));

            var statements = stripped.Split(';')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();

            _logger.LogInformation("[AdminDb.Sql] {Path}: running {Count} statements", path, statements.Length);

            int ok = 0, failed = 0;
            for (int i = 0; i < statements.Length; i++)
            {
                var stmt = statements[i];
                try
                {
                    var affected = await _db!.ExecuteSqlAsync(stmt);
                    _logger.LogInformation("[AdminDb.Sql] [{I}/{N}] ok (affected={A}): {Sql}",
                        i + 1, statements.Length, affected, Preview(stmt));
                    ok++;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "[AdminDb.Sql] [{I}/{N}] FAILED: {Sql}",
                        i + 1, statements.Length, Preview(stmt));
                    failed++;
                }
            }

            _logger.LogInformation("[AdminDb.Sql] {Path} complete: {Ok} ok / {Failed} failed", path, ok, failed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AdminDb.Sql] RunFile crashed: {Path}", path);
        }
    }

    private static string Preview(string sql)
    {
        var oneLine = sql.Replace('\n', ' ').Replace('\r', ' ');
        return oneLine.Length > 120 ? oneLine[..120] + "..." : oneLine;
    }

    public void Shutdown() { }
}
