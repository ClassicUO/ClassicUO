// SPDX-License-Identifier: BSD-2-Clause
//
// Backbuffer → PNG service. Called from ServiceCaptureRequestSystem at
// Stage.PostUpdate (after game.Tick has Drawn the frame; before Present
// at Stage.Last). FNA's GraphicsDevice.GetBackBufferData reads the
// current backbuffer contents; Texture2D.SaveAsPng writes a portable
// PNG.
//
// The file is written to req.OutPath. Returning the PNG bytes inline in
// the JSON-RPC response would bloat the wire (a 1920x1080 PNG can easily
// be 1-2 MB after base64 encoding); the disk-roundtrip model matches the
// existing OS-driver workflow and keeps the JSON-RPC envelope small.

#if AGENT_BUILD
#nullable enable

using System;
using System.IO;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Agent;

internal static class AgentCaptureService
{
    public static JsonRpcResponse Run(GraphicsDevice device, CaptureRequest req)
    {
        try
        {
            var pp = device.PresentationParameters;
            var w = pp.BackBufferWidth;
            var h = pp.BackBufferHeight;
            if (w <= 0 || h <= 0)
            {
                return AgentServer.ErrorResponse(
                    req.RequestId,
                    JsonRpcErrorCodes.CaptureUnavailable,
                    "backbuffer has zero dimensions");
            }

            var pixels = new Color[w * h];
            device.GetBackBufferData(pixels);

            // Encode via FNA's built-in PNG writer (StbImageWrite under the
            // hood). Build a transient Texture2D so SaveAsPng can be used.
            using var tex = new Texture2D(device, w, h);
            tex.SetData(pixels);

            string? outPath = req.OutPath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                return AgentServer.ErrorResponse(
                    req.RequestId,
                    JsonRpcErrorCodes.InvalidParams,
                    "capture.shot: 'path' is required");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            using (var fs = File.Create(outPath))
            {
                tex.SaveAsPng(fs, w, h);
            }

            // Paired raw sidecar matches tools/agent-desktop/OsDriver/RawImage.cs
            // CUORAW1 format so the CLI's `diff` command can post-hoc
            // compare two captures without decoding PNG.
            var rawPath = Path.ChangeExtension(outPath, ".raw.bin");
            WriteRawSidecar(rawPath, pixels, w, h);

            return new JsonRpcResponse
            {
                Id = req.RequestId,
                Result = new JsonObject
                {
                    ["path"] = Path.GetFullPath(outPath),
                    ["raw"] = Path.GetFullPath(rawPath),
                    ["width"] = w,
                    ["height"] = h,
                },
            };
        }
        catch (Exception ex)
        {
            return AgentServer.ErrorResponse(
                req.RequestId,
                JsonRpcErrorCodes.InternalError,
                $"capture failed: {ex.Message}");
        }
    }

    private static readonly byte[] s_rawMagic = System.Text.Encoding.ASCII.GetBytes("CUORAW1\0");

    private static void WriteRawSidecar(string path, Color[] pixels, int width, int height)
    {
        // CUORAW1: magic + width + height + channels + BGRA top-down.
        // FNA's Color is RGBA byte order; convert to BGRA for the sidecar.
        var bgra = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            var o = i * 4;
            bgra[o]     = c.B;
            bgra[o + 1] = c.G;
            bgra[o + 2] = c.R;
            bgra[o + 3] = 255;
        }

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        bw.Write(s_rawMagic);
        bw.Write(width);
        bw.Write(height);
        bw.Write(4);
        bw.Write(bgra);
    }
}

#endif
