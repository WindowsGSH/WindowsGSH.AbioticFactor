using System.Diagnostics;
using System.Globalization;
using System.Text;
using WindowsGSH.Core.Modules;
using WindowsGSH.Core.Query;
using WindowsGSH.Core.Servers;

namespace WindowsGSH.Modules.AbioticFactor;

public sealed class AbioticFactorModule : IGameServerModule, IManifestBackedModule, IModuleExistingServerImportCapability, IModulePortCapability
{
    private ModuleManifest? _manifest; private string _moduleDirectory = AppContext.BaseDirectory;
    private ModuleManifest Manifest => _manifest ??= ModuleManifest.Load(Path.Combine(_moduleDirectory, "module.json"));
    public string Id => Manifest.Id; public string Name => Manifest.Name; public string Version => Manifest.Version;
    public ModuleCapabilities Capabilities => Manifest.ToCapabilities(false, false);
    public SteamInstallDefinition? SteamInstall => Manifest.ToSteamInstall(); public ModuleRuntimeDefinition Runtime => Manifest.ToRuntime();
    public void Configure(ModuleManifest manifest, string moduleDirectory) { _manifest = manifest; _moduleDirectory = moduleDirectory; }
    public IReadOnlyList<ConfigFieldDefinition> GetConfigFields() => Manifest.ToConfigFields(); public IReadOnlyList<ServerPortDefinition> GetPorts() => Manifest.ToPorts();
    public IReadOnlyList<ServerAddonDefinition> GetAddonDefinitions() => Manifest.ToAddons(); public IReadOnlyList<ServerBackupTargetDefinition> GetBackupTargets() => Manifest.ToBackupTargets();
    public string GetServerName(IReadOnlyDictionary<string, object?> settings) => Get(settings, "server.name", "Abiotic Factor Server");
    public ServerDisplayInfo GetDisplayInfo(ServerInstance instance) => new("0.0.0.0", Get(instance.Settings, "network.port", "7777"), Get(instance.Settings, "server.maxPlayers", "6"));
    public Task<IReadOnlyDictionary<string, object?>> ReadConfigFileSettingsAsync(ServerInstance instance, CancellationToken cancellationToken) => AbioticSandboxSettings.ReadAsync(instance, cancellationToken);
    public Task WriteConfigFileSettingsAsync(ServerInstance instance, CancellationToken cancellationToken) => AbioticSandboxSettings.WriteAsync(instance, cancellationToken);
    public Task<InstallPlan> CreateInstallPlanAsync(ServerInstance instance, CancellationToken cancellationToken) => Task.FromResult(new InstallPlan("steamcmd", $"+force_install_dir \"{instance.InstallPath}\" +login anonymous +app_update {SteamInstall?.AppId} validate +quit", instance.InstallPath, ["Abiotic Factor Dedicated Server is available anonymously through SteamCMD.", "World settings remain in the vendor-managed Saved directory."]));

    public Task<ProcessStartInfo> CreateStartInfoAsync(ServerInstance instance, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateName(Get(instance.Settings, "server.world", "Cascade"), "World Save Name");
        var info = new ProcessStartInfo { FileName = Path.Combine(instance.InstallPath, Runtime.StartPath), WorkingDirectory = instance.InstallPath, UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var value in new[] { "-log", "-newconsole", "-useperfthreads", "-NoAsyncLoadingThread", $"-MaxServerPlayers={Number(instance, "server.maxPlayers", 6, 1, 24)}", $"-Port={Number(instance, "network.port", 7777, 1, 65535)}", $"-QueryPort={Number(instance, "network.queryPort", 27015, 1, 65535)}", $"-ServerPassword={Get(instance.Settings, "server.password", "")}", $"-SteamServerName={Get(instance.Settings, "server.name", "Abiotic Factor Server")}", $"-WorldSaveName={Get(instance.Settings, "server.world", "Cascade")}" }) info.ArgumentList.Add(value);
        var extra = Split(Get(instance.Settings, "server.additionalArguments", ""));
        for (var i = 0; i < extra.Count; i++) { if (!Managed(extra[i])) info.ArgumentList.Add(extra[i]); else if (!extra[i].Contains('=') && i + 1 < extra.Count && !extra[i + 1].StartsWith('-')) i++; }
        return Task.FromResult(info);
    }
    public async Task<Process?> StartAsync(ServerInstance instance, CancellationToken cancellationToken) { if (!IsInstallValid(instance)) throw new FileNotFoundException("Abiotic Factor server executable was not found.", Path.Combine(instance.InstallPath, Runtime.StartPath)); var process = new Process { StartInfo = await CreateStartInfoAsync(instance, cancellationToken), EnableRaisingEvents = true }; process.Start(); return process; }
    public async Task StopAsync(ServerInstance instance, CancellationToken cancellationToken) { foreach (var process in ServerProcessLocator.FindProcesses(this, instance.InstallPath)) using (process) { if (process.HasExited) continue; process.CloseMainWindow(); await Task.Delay(5000, cancellationToken); if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(cancellationToken); } } }
    public bool IsInstallValid(ServerInstance instance) => File.Exists(Path.Combine(instance.InstallPath, Runtime.StartPath));
    public string? GetConsoleLogPath(ServerInstance instance) => Path.Combine(instance.InstallPath, "AbioticFactor", "Saved", "Logs");
    public Task<QueryResult> QueryAsync(ServerInstance instance, CancellationToken cancellationToken) => Task.FromResult(new QueryResult(ModuleServerStatus.Unknown, Message: "WindowsGSH uses process status for Abiotic Factor."));
    public Task<string> ExecuteRconCommandAsync(ServerInstance instance, string command, CancellationToken cancellationToken) => throw new NotSupportedException("Abiotic Factor RCON is not implemented.");
    public Task<IReadOnlyList<Process>> StartAddonProcessesAsync(ServerInstance instance, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Process>>([]);
    public ServerAddonStatus GetAddonStatus(ServerInstance instance, string addonId) => new(addonId, false, false, "Unknown addon"); public Task InstallAddonAsync(ServerInstance instance, string addonId, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task RemoveAddonAsync(ServerInstance instance, string addonId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public bool CanImport(string path) { try { return File.Exists(Path.Combine(Resolve(path), Runtime.StartPath)); } catch { return false; } }
    public async Task<ModuleExistingServerImportProbe> PreviewImportAsync(string path, CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); var source = Path.GetFullPath(path); var install = Resolve(source); if (!File.Exists(Path.Combine(install, Runtime.StartPath))) throw new InvalidDataException("Abiotic Factor server executable was not found."); var warnings = Directory.Exists(Path.Combine(install, "AbioticFactor", "Saved")) ? new List<string>() : ["No Saved directory was found; the server may not have been started yet."];var world=FindWorld(install);var settings=new Dictionary<string,object?>();if(world is not null){settings["server.world"]=world;var probe=new ServerInstance("import","Abiotic Factor",Id,source,install,Path.Combine(source,"ServerConfig.json"),settings);foreach(var pair in await AbioticSandboxSettings.ReadAsync(probe,cancellationToken))settings[pair.Key]=pair.Value;}return new ModuleExistingServerImportProbe(Path.GetFileName(source), install, settings, warnings); }
    private static string? FindWorld(string install) { var worlds = Path.Combine(install, "AbioticFactor", "Saved", "SaveGames", "Server", "Worlds"); return Directory.Exists(worlds) ? Directory.EnumerateDirectories(worlds).Select(Path.GetFileName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) : null; }
    private string Resolve(string path) { var source = Path.GetFullPath(path); if (File.Exists(Path.Combine(source, Runtime.StartPath))) return source; var files = Path.Combine(source, "serverfiles"); return File.Exists(Path.Combine(files, Runtime.StartPath)) ? files : source; }
    private static bool Managed(string value) { var key = value.Split('=', 2)[0]; return new[] { "-log", "-newconsole", "-useperfthreads", "-NoAsyncLoadingThread", "-MaxServerPlayers", "-Port", "-QueryPort", "-ServerPassword", "-SteamServerName", "-WorldSaveName" }.Contains(key, StringComparer.OrdinalIgnoreCase); }
    private static List<string> Split(string value) { var result = new List<string>(); var token = new StringBuilder(); var quoted = false; foreach (var c in value) { if (c == '"') { quoted = !quoted; continue; } if (char.IsWhiteSpace(c) && !quoted) { Add(); continue; } token.Append(c); } if (quoted) throw new InvalidDataException("Additional Arguments contains an unmatched quote."); Add(); return result; void Add() { if (token.Length == 0) return; result.Add(token.ToString()); token.Clear(); } }
    private static void ValidateName(string value, string label) { if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value is "." or "..") throw new InvalidDataException(label + " is invalid."); }
    private static int Number(ServerInstance instance, string key, int fallback, int min, int max) => int.TryParse(Get(instance.Settings, key, fallback.ToString(CultureInfo.InvariantCulture)), out var value) && value >= min && value <= max ? value : fallback;
    private static string Get(IReadOnlyDictionary<string, object?> settings, string key, string fallback) => settings.TryGetValue(key, out var value) && value is not null ? value.ToString()!.Trim() : fallback;
}
