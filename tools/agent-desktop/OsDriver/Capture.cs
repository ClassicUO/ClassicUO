// SPDX-License-Identifier: BSD-2-Clause
//
// Screen capture for OS-level branch-agnostic driving. PrintWindow with
// PW_RENDERFULLCONTENT pulls a snapshot of the target window's client
// area including its DirectX/OpenGL/Vulkan-rendered swap chain (BitBlt
// off the window DC would return black for hardware-accelerated content).
//
// Output is a 32bpp PNG (alpha discarded, set to 255).

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ClassicUO.Agent.Desktop.OsDriver;

[SupportedOSPlatform("windows5.0")]
internal static class Capture
{
    public static byte[] CapturePng(IntPtr hWnd, out int width, out int height)
    {
        var pixels = CaptureBgra(hWnd, out width, out height);
        return PngEncode(pixels, width, height);
    }

    public static void CapturePngToFile(IntPtr hWnd, string path)
    {
        var bytes = CapturePng(hWnd, out _, out _);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, bytes);
    }

    public static byte[] CaptureBgra(IntPtr hWnd, out int width, out int height)
    {
        Win32.GetClientRect(hWnd, out var rect);
        width = rect.Width;
        height = rect.Height;
        if (width <= 0 || height <= 0)
            throw new InvalidOperationException("target window has zero client area");

        // Raise the target's Z-order so screen BitBlt sees its pixels.
        // SetForegroundWindow is unreliable from a non-foreground process
        // (Windows refuses focus theft). SetWindowPos with HWND_TOPMOST
        // doesn't require focus rights and reliably brings the window to
        // the top; we drop it back to non-topmost immediately so the user
        // can still alt-tab around afterward.
        Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
        Win32.SetWindowPos(hWnd, Win32.HWND_TOPMOST, 0, 0, 0, 0,
            Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
        Win32.SetWindowPos(hWnd, Win32.HWND_NOTOPMOST, 0, 0, 0, 0,
            Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
        System.Threading.Thread.Sleep(120);

        var clientOrigin = new Win32.POINT();
        Win32.ClientToScreen(hWnd, ref clientOrigin);

        var screenDc = Win32.GetDC(IntPtr.Zero);
        var memDc = Win32.CreateCompatibleDC(screenDc);
        var hBitmap = Win32.CreateCompatibleBitmap(screenDc, width, height);
        var oldObj = Win32.SelectObject(memDc, hBitmap);
        try
        {
            // BitBlt the window's client-area region off the screen DC.
            var ok = Win32.BitBlt(
                memDc, 0, 0, width, height,
                screenDc, clientOrigin.X, clientOrigin.Y, Win32.SRCCOPY);
            if (!ok)
            {
                // Last-resort fallback: PrintWindow. Usually returns black
                // for GPU-rendered swap chains, but better than nothing if
                // the screen BitBlt fails for some unforeseen reason.
                Win32.PrintWindow(hWnd, memDc, Win32.PW_CLIENTONLY | Win32.PW_RENDERFULLCONTENT);
            }

            var bmi = new Win32.BITMAPINFO
            {
                bmiHeader = new Win32.BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>(),
                    biWidth = width,
                    // Negative height ⇒ top-down rows. Default DIB layout is
                    // bottom-up which would force a vertical flip.
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = Win32.BI_RGB,
                    biSizeImage = (uint)(width * height * 4),
                },
            };
            var pixels = new byte[width * height * 4];
            Win32.GetDIBits(memDc, hBitmap, 0, (uint)height, pixels, ref bmi, Win32.DIB_RGB_COLORS);

            // Force alpha = 255 — PrintWindow leaves it unset on many drivers.
            for (var i = 3; i < pixels.Length; i += 4)
                pixels[i] = 255;

            return pixels;
        }
        finally
        {
            Win32.SelectObject(memDc, oldObj);
            Win32.DeleteObject(hBitmap);
            Win32.DeleteDC(memDc);
            Win32.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    // Minimal PNG encoder. Input is BGRA top-down; we swap to RGBA and
    // write a single IDAT chunk using zlib deflate via the BCL. Mutates
    // the input buffer in place during channel swap — pass a copy if you
    // need the BGRA preserved (e.g. for a paired raw sidecar).
    public static byte[] PngEncode(byte[] bgra, int width, int height)
    {
        // Convert BGRA → RGBA in place (alpha already 255).
        for (var i = 0; i < bgra.Length; i += 4)
        {
            (bgra[i], bgra[i + 2]) = (bgra[i + 2], bgra[i]);
        }

        // PNG scanlines need a filter byte per row.
        var stride = width * 4;
        var rawWithFilter = new byte[(stride + 1) * height];
        for (var y = 0; y < height; y++)
        {
            rawWithFilter[y * (stride + 1)] = 0; // filter type: None
            Buffer.BlockCopy(bgra, y * stride, rawWithFilter, y * (stride + 1) + 1, stride);
        }

        var compressed = ZlibDeflate(rawWithFilter);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Signature.
        bw.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        // IHDR.
        WriteChunk(bw, "IHDR", w =>
        {
            WriteBE(w, (uint)width);
            WriteBE(w, (uint)height);
            w.Write((byte)8);   // bit depth
            w.Write((byte)6);   // color type RGBA
            w.Write((byte)0);   // compression
            w.Write((byte)0);   // filter
            w.Write((byte)0);   // interlace
        });

        // IDAT.
        WriteChunk(bw, "IDAT", w => w.Write(compressed));

        // IEND.
        WriteChunk(bw, "IEND", _ => { });

        return ms.ToArray();
    }

    private static void WriteChunk(BinaryWriter bw, string type, Action<BinaryWriter> writeData)
    {
        using var inner = new MemoryStream();
        using var innerBw = new BinaryWriter(inner);
        writeData(innerBw);
        innerBw.Flush();
        var data = inner.ToArray();

        WriteBE(bw, (uint)data.Length);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        bw.Write(typeBytes);
        bw.Write(data);
        var crc = Crc32.Compute(typeBytes, data);
        WriteBE(bw, crc);
    }

    private static void WriteBE(BinaryWriter bw, uint v)
    {
        bw.Write((byte)((v >> 24) & 0xFF));
        bw.Write((byte)((v >> 16) & 0xFF));
        bw.Write((byte)((v >> 8) & 0xFF));
        bw.Write((byte)(v & 0xFF));
    }

    private static byte[] ZlibDeflate(byte[] data)
    {
        // zlib header (deflate, default window) + raw deflate stream + Adler32.
        using var ms = new MemoryStream();
        ms.WriteByte(0x78);
        ms.WriteByte(0x9C);
        using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            ds.Write(data, 0, data.Length);
        }
        var adler = Adler32.Compute(data);
        ms.WriteByte((byte)((adler >> 24) & 0xFF));
        ms.WriteByte((byte)((adler >> 16) & 0xFF));
        ms.WriteByte((byte)((adler >> 8) & 0xFF));
        ms.WriteByte((byte)(adler & 0xFF));
        return ms.ToArray();
    }
}

internal static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            t[i] = c;
        }
        return t;
    }

    public static uint Compute(byte[] a, byte[] b)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var x in a) crc = Table[(crc ^ x) & 0xFF] ^ (crc >> 8);
        foreach (var x in b) crc = Table[(crc ^ x) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }
}

internal static class Adler32
{
    public static uint Compute(byte[] data)
    {
        uint a = 1, b = 0;
        const uint Mod = 65521;
        foreach (var x in data)
        {
            a = (a + x) % Mod;
            b = (b + a) % Mod;
        }
        return (b << 16) | a;
    }
}
