// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop diff --a <raw> --b <raw> --out <png>` — compute a
// pixel-by-pixel diff between two CUORAW1 sidecars (produced by `shot`)
// and write a highlighted PNG plus a JSON line of stats.

using System;
using System.CommandLine;
using System.IO;
using System.Runtime.Versioning;
using ClassicUO.Agent.Desktop.OsDriver;

namespace ClassicUO.Agent.Desktop.Commands;

[SupportedOSPlatform("windows5.0")]
internal static class DiffCommand
{
    public static Command Build()
    {
        var aOpt = new Option<string>("--a", "Baseline .raw.bin path.") { IsRequired = true };
        var bOpt = new Option<string>("--b", "Candidate .raw.bin path.") { IsRequired = true };
        var outOpt = new Option<string>("--out", "Diff PNG output path.") { IsRequired = true };
        var tolOpt = new Option<int>("--tolerance", () => 4,
            "Per-channel byte tolerance below which pixels are considered equal.");

        var cmd = new Command("diff", "Compare two .raw.bin sidecars; write a highlighted PNG and JSON stats.");
        cmd.AddOption(aOpt);
        cmd.AddOption(bOpt);
        cmd.AddOption(outOpt);
        cmd.AddOption(tolOpt);

        cmd.SetHandler((string aPath, string bPath, string outPath, int tol) =>
        {
            var (a, aw, ah) = RawImage.Read(aPath);
            var (b, bw, bh) = RawImage.Read(bPath);
            if (aw != bw || ah != bh)
            {
                CliJson.Error($"size mismatch: {aw}x{ah} vs {bw}x{bh}");
                Environment.ExitCode = 1;
                return;
            }

            var result = ImageDiff.Compare(a, aw, ah, b, bw, bh, tol);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            var pngBytes = Capture.PngEncode(result.DiffBgra, result.Width, result.Height);
            File.WriteAllBytes(outPath, pngBytes);

            CliJson.Status(new
            {
                status = "diff",
                a = Path.GetFullPath(aPath),
                b = Path.GetFullPath(bPath),
                outPath = Path.GetFullPath(outPath),
                width = result.Width,
                height = result.Height,
                totalPixels = result.TotalPixels,
                mismatchedPixels = result.MismatchedPixels,
                mismatchedFraction = result.MismatchedFraction,
                averageDelta = result.AverageDelta,
                maxChannelDelta = result.MaxChannelDelta,
                tolerance = tol,
            });
        }, aOpt, bOpt, outOpt, tolOpt);

        return cmd;
    }
}
