// SPDX-License-Identifier: BSD-2-Clause
//
// Optional <repoRoot>/.env (KEY=VALUE) so client version, UO data dir, and
// login credentials live in one gitignored file instead of being pinned in
// settings.json and retyped on every command. Absent file → empty map, and
// callers fall back to their own defaults. Loaded once, cached.

namespace ClassicUO.Agent.Desktop.Services;

internal static class DotEnv
{
    private static Dictionary<string, string>? _cache;

    public static string? Get(string key)
        => Load().TryGetValue(key, out var v) ? v : null;

    public static IReadOnlyDictionary<string, string> Load()
    {
        if (_cache is not null)
            return _cache;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var root = RepoRoot.Find();
        var path = root is null ? null : Path.Combine(root, ".env");
        try
        {
            foreach (var raw in path is not null && File.Exists(path)
                ? File.ReadAllLines(path)
                : Array.Empty<string>())
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                var key = line[..eq].Trim();
                var val = line[(eq + 1)..].Trim();
                // Strip a single layer of matching surrounding quotes.
                if (val.Length >= 2 &&
                    ((val[0] == '"' && val[^1] == '"') || (val[0] == '\'' && val[^1] == '\'')))
                    val = val[1..^1];
                map[key] = val;
            }
        }
        catch
        {
            // A locked/unreadable .env shouldn't crash the whole CLI; callers
            // fall back to their own defaults on an empty map.
        }

        _cache = map;
        return _cache;
    }

    // Replace ${KEY} tokens with .env values. Unknown tokens are left
    // untouched so a real param value containing ${...} isn't mangled.
    public static string Expand(string text)
    {
        foreach (var (k, v) in Load())
            text = text.Replace("${" + k + "}", v);
        return text;
    }
}
