// SPDX-License-Identifier: BSD-2-Clause
//
// Spawn + readiness helpers for a ModernUO server launched from MODERNUO_PATH
// (.env). Optional: if the key is unset the harness assumes an externally
// managed server already listening on the shard port, exactly as before.
// MODERNUO_PATH may point at the server executable itself or at the
// distribution directory containing it.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ClassicUO.Agent.Desktop.Services;

internal static class ServerProcess
{
    // Resolve MODERNUO_PATH to the server executable. null when the key is
    // absent (external server) or the resolved file doesn't exist (caller
    // warns and falls back to an external server).
    public static string? ResolveExecutable()
    {
        var raw = DotEnv.Get("MODERNUO_PATH");
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (File.Exists(raw))
            return raw;

        // A directory — look for the apphost inside it.
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "ModernUO.exe"
            : "ModernUO";
        var candidate = Path.Combine(raw, exeName);
        return File.Exists(candidate) ? candidate : null;
    }

    public static Process Spawn(string exePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            // ModernUO reads modernuo.json / world saves relative to CWD.
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var child = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!child.Start())
            throw new InvalidOperationException("failed to start ModernUO (Process.Start returned false)");
        return child;
    }

    // ponytail: TCP-accept probe only — ModernUO has no RPC to ask "world
    // loaded?". Good enough: login races a still-booting world otherwise.
    public static async Task<bool> WaitForListeningAsync(
        int port, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("127.0.0.1", port, ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                await Task.Delay(250, ct);
            }
        }
        return false;
    }
}
