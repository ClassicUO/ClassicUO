// SPDX-License-Identifier: BSD-2-Clause
//
// Pixel-by-pixel raw-image diff with a per-channel tolerance. Inputs are
// CUORAW1 sidecars (BGRA top-down). Output: a PNG highlighting mismatched
// pixels in red on a desaturated copy of the baseline, plus stats.

using System;

namespace ClassicUO.Agent.Desktop.OsDriver;

internal sealed record ImageDiffResult(
    int Width,
    int Height,
    int TotalPixels,
    int MismatchedPixels,
    double MismatchedFraction,
    double AverageDelta,
    int MaxChannelDelta,
    byte[] DiffBgra);

internal static class ImageDiff
{
    public static ImageDiffResult Compare(
        byte[] a, int aw, int ah,
        byte[] b, int bw, int bh,
        int tolerance)
    {
        if (aw != bw || ah != bh)
            throw new InvalidOperationException(
                $"image sizes differ: {aw}x{ah} vs {bw}x{bh}; cannot diff");

        var total = aw * ah;
        var mismatched = 0;
        long deltaSum = 0;
        var maxDelta = 0;
        var diff = new byte[a.Length];

        for (var i = 0; i < total; i++)
        {
            var o = i * 4;
            var db = Math.Abs(a[o] - b[o]);
            var dg = Math.Abs(a[o + 1] - b[o + 1]);
            var dr = Math.Abs(a[o + 2] - b[o + 2]);
            var d = Math.Max(db, Math.Max(dg, dr));
            deltaSum += d;
            if (d > maxDelta) maxDelta = d;

            if (d > tolerance)
            {
                // Highlight: solid red.
                diff[o] = 0;
                diff[o + 1] = 0;
                diff[o + 2] = 255;
                diff[o + 3] = 255;
                mismatched++;
            }
            else
            {
                // Desaturated baseline (grayscale) at 50% so highlights pop.
                var gray = (byte)((a[o] + a[o + 1] + a[o + 2]) / 6);
                diff[o] = gray;
                diff[o + 1] = gray;
                diff[o + 2] = gray;
                diff[o + 3] = 255;
            }
        }

        return new ImageDiffResult(
            Width: aw,
            Height: ah,
            TotalPixels: total,
            MismatchedPixels: mismatched,
            MismatchedFraction: total == 0 ? 0 : (double)mismatched / total,
            AverageDelta: total == 0 ? 0 : (double)deltaSum / total,
            MaxChannelDelta: maxDelta,
            DiffBgra: diff);
    }
}
