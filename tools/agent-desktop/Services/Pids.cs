// SPDX-License-Identifier: BSD-2-Clause
//
// Persisted-rig registry. When the user runs `agent-desktop up --persist`,
// we drop a small JSON file under tools/agent-desktop/.runtime/pids.json
// so a subsequent `agent-desktop down` (in a different process) can find
// what to tear down. The .runtime/ directory is gitignored.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Agent.Desktop.Services;

internal static class Pids
{
    public sealed class ClientEntry
    {
        [JsonPropertyName("pid")] public int Pid { get; set; }
        [JsonPropertyName("port")] public int Port { get; set; }
    }

    public sealed class Registry
    {
        [JsonPropertyName("client")] public ClientEntry? Client { get; set; }
    }

    public static string PidsFilePath()
    {
        var dir = Path.Combine(AppContext.BaseDirectory);
        // AppContext.BaseDirectory points into bin/Debug/net9.0; we want
        // the file rooted at the project so tooling outside the build can
        // find it predictably. Walk up to the project directory.
        var projectRoot = FindProjectRoot(dir);
        return Path.Combine(projectRoot, ".runtime", "pids.json");
    }

    public static void SavePids(int clientPid, int port)
    {
        var path = PidsFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var registry = new Registry
        {
            Client = new ClientEntry { Pid = clientPid, Port = port }
        };
        var json = JsonSerializer.Serialize(registry, PidsJsonContext.Default.Registry);
        File.WriteAllText(path, json);
    }

    public static Registry? LoadPids()
    {
        var path = PidsFilePath();
        if (!File.Exists(path)) return null;
        try
        {
            var text = File.ReadAllText(path);
            return JsonSerializer.Deserialize(text, PidsJsonContext.Default.Registry);
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        var path = PidsFilePath();
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch { /* best effort */ }
        }
    }

    private static string FindProjectRoot(string start)
    {
        // Walk up looking for AgentDesktop.csproj. Falls back to the
        // current directory if we can't find it (shouldn't happen in
        // normal use but keeps the helper total).
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AgentDesktop.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return start;
    }
}

[JsonSerializable(typeof(Pids.Registry))]
[JsonSerializable(typeof(Pids.ClientEntry))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class PidsJsonContext : JsonSerializerContext;
