// SPDX-License-Identifier: BSD-2-Clause
//
// Locate the cuo-agents repo root by walking up from the running assembly.
// No top-level .sln on impl/ecs, so anchor on the canonical client csproj.

namespace ClassicUO.Agent.Desktop.Services;

internal static class RepoRoot
{
    public static string? Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, "src", "ClassicUO.Client", "ClassicUO.Client.csproj");
            if (File.Exists(marker))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
