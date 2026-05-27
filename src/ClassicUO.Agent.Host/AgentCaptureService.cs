// SPDX-License-Identifier: BSD-2-Clause
//
// Backbuffer → PNG service. Called from the runtime-side service system
// (after the frame has been drawn; before Present). FNA's
// GraphicsDevice.GetBackBufferData reads the current backbuffer contents;
// Texture2D.SaveAsPng writes a portable PNG.
//
// The file is written to req.OutPath. Returning the PNG bytes inline in
// the JSON-RPC response would bloat the wire (a 1920x1080 PNG can easily
// be 1-2 MB after base64 encoding); the disk-roundtrip model keeps the
// JSON-RPC envelope small.

#nullable enable

using System;
using System.IO;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Agent.Host;

public static class AgentCaptureService
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

            return new JsonRpcResponse
            {
                Id = req.RequestId,
                Result = new JsonObject
                {
                    ["path"] = Path.GetFullPath(outPath),
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
}
