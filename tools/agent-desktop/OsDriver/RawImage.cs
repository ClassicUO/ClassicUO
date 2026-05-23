// SPDX-License-Identifier: BSD-2-Clause
//
// Simple raw-pixel sidecar format so image-diff can run post-hoc without
// decoding PNG. Format:
//   magic "CUORAW1\0"     8 bytes
//   width                 int32 little-endian
//   height                int32 little-endian
//   channels              int32 little-endian  (always 4)
//   bgra pixels           width*height*4 bytes, top-down, alpha=255

using System;
using System.IO;

namespace ClassicUO.Agent.Desktop.OsDriver;

internal static class RawImage
{
    private static readonly byte[] Magic = System.Text.Encoding.ASCII.GetBytes("CUORAW1\0");

    public static void Write(string path, byte[] bgra, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        bw.Write(Magic);
        bw.Write(width);
        bw.Write(height);
        bw.Write(4);
        bw.Write(bgra);
    }

    public static (byte[] Bgra, int Width, int Height) Read(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        var magic = br.ReadBytes(8);
        if (magic.Length != 8 || !magic.AsSpan().SequenceEqual(Magic))
            throw new InvalidDataException($"not a CUORAW1 file: {path}");
        var w = br.ReadInt32();
        var h = br.ReadInt32();
        var ch = br.ReadInt32();
        if (ch != 4) throw new InvalidDataException("unsupported channel count");
        var pixels = br.ReadBytes(w * h * 4);
        return (pixels, w, h);
    }
}
