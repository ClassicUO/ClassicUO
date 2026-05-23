// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop shot --pid <id> --out <path>` — capture the target
// window's client area to a PNG, plus a paired .raw.bin for diff input.

using System;
using System.CommandLine;
using System.IO;
using System.Runtime.Versioning;
using System.Text.Json;
using ClassicUO.Agent.Desktop.OsDriver;

namespace ClassicUO.Agent.Desktop.Commands;

[SupportedOSPlatform("windows5.0")]
internal static class ShotCommand
{
    public static Command Build()
    {
        var pid = new Option<int?>("--pid", "Target process id.");
        var titleSub = new Option<string?>("--title", "Substring of target window title (alternative to --pid).");
        var outPath = new Option<string>("--out", "PNG output path. A paired .raw.bin sidecar is written next to it.") { IsRequired = true };

        var cmd = new Command("shot", "Capture the target game window's client area as PNG + raw sidecar.");
        cmd.AddOption(pid);
        cmd.AddOption(titleSub);
        cmd.AddOption(outPath);

        cmd.SetHandler((int? pidVal, string? titleVal, string outVal) =>
        {
            var hwnd = TargetResolver.Resolve(pidVal, titleVal);
            if (hwnd == IntPtr.Zero)
            {
                CliJson.Error("could not locate target window");
                Environment.ExitCode = 1;
                return;
            }

            var bgra = Capture.CaptureBgra(hwnd, out var w, out var h);

            // Two copies: one BGRA preserved for the raw sidecar, one
            // converted in place to RGBA for the PNG encoder.
            var bgraForRaw = (byte[])bgra.Clone();
            var pngBytes = Capture.PngEncode(bgra, w, h);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outVal))!);
            File.WriteAllBytes(outVal, pngBytes);
            var rawPath = Path.ChangeExtension(outVal, ".raw.bin");
            RawImage.Write(rawPath, bgraForRaw, w, h);

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                status = "shot",
                path = Path.GetFullPath(outVal),
                raw = Path.GetFullPath(rawPath),
                width = w,
                height = h,
            }));
        }, pid, titleSub, outPath);

        return cmd;
    }
}
