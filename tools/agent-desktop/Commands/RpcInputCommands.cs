// SPDX-License-Identifier: BSD-2-Clause
//
// rpc-* commands: drive the running cuo.agent client via JSON-RPC input
// verbs (input.mouseMove / mouseClick / mouseDoubleClick / mouseHold /
// mouseRelease) instead of OS-level SendInput. In-process synthesis
// goes through MouseContext's synth-state path, so events reach the
// ECS without focus theft or raise-Z tricks.
//
// Each verb call returns { queued: N }; the CLI sleeps roughly
// N * frameMs before exiting so the next CLI invocation observes a
// settled mouse state. --frame-ms defaults to 20 ms (60 FPS plus
// safety margin).

using System;
using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;

namespace ClassicUO.Agent.Desktop.Commands;

internal static class RpcInputCommands
{
    public static Command BuildMove()
        => BuildXY("rpc-mouse-move", "input.mouseMove", "Move synthetic mouse cursor.");

    public static Command BuildClick()
        => BuildButton("rpc-click", "input.mouseClick", "Single click via synthetic mouse.");

    public static Command BuildDoubleClick()
        => BuildButton("rpc-double-click", "input.mouseDoubleClick", "Double click via synthetic mouse.");

    public static Command BuildHold()
        => BuildButton("rpc-mouse-hold", "input.mouseHold", "Press a synthetic button and keep it pressed.");

    public static Command BuildRelease()
        => BuildButton("rpc-mouse-release", "input.mouseRelease", "Release a previously-held synthetic button.");

    public static Command BuildClear()
    {
        var host = new Option<string?>("--host");
        var port = new Option<int?>("--port");
        var cmd = new Command("rpc-input-clear", "Reset synthetic mouse state and disable agent synth mode.");
        cmd.AddOption(host);
        cmd.AddOption(port);
        cmd.SetHandler(async (string? h, int? p) =>
        {
            var (host2, port2) = RpcClient.ResolveEndpoint(h, p);
            await using var rpc = await RpcClient.ConnectAsync(host2, port2);
            var resp = await rpc.CallAsync("input.clear");
            EmitResponse(resp);
        }, host, port);
        return cmd;
    }

    public static Command BuildType()
    {
        var host = new Option<string?>("--host");
        var port = new Option<int?>("--port");
        var text = new Option<string>("--text", "Text to type into the focused input.") { IsRequired = true };

        var cmd = new Command("rpc-type",
            "Push SDL_TEXTINPUT events with --text to the running agent. Focused control receives via FNA TextInputEXT.TextInput / HandleSdlEvent SDL_TEXTINPUT case.");
        cmd.AddOption(host);
        cmd.AddOption(port);
        cmd.AddOption(text);
        cmd.SetHandler(async (string? h, int? p, string txt) =>
        {
            var (host2, port2) = RpcClient.ResolveEndpoint(h, p);
            await using var rpc = await RpcClient.ConnectAsync(host2, port2);
            var resp = await rpc.CallAsync("input.type", new { text = txt });
            EmitResponse(resp);
        }, host, port, text);
        return cmd;
    }

    public static Command BuildDoubleClickSerial()
    {
        var host = new Option<string?>("--host");
        var port = new Option<int?>("--port");
        var serial = new Option<string>("--serial",
            "Serial: hex (0x40000001), decimal, or 'player'.") { IsRequired = true };

        var cmd = new Command("rpc-double-click-serial",
            "Send Send_DoubleClick(serial) without routing through a pixel click. Use 'player' to target the local player.");
        cmd.AddOption(host);
        cmd.AddOption(port);
        cmd.AddOption(serial);
        cmd.SetHandler(async (string? h, int? p, string s) =>
        {
            var (host2, port2) = RpcClient.ResolveEndpoint(h, p);
            await using var rpc = await RpcClient.ConnectAsync(host2, port2);
            var resp = await rpc.CallAsync("input.doubleClickSerial", new { serial = s });
            EmitResponse(resp);
            if (resp.Error is not null) Environment.ExitCode = 1;
        }, host, port, serial);
        return cmd;
    }

    public static Command BuildShot()
    {
        var host = new Option<string?>("--host");
        var port = new Option<int?>("--port");
        var outPath = new Option<string>("--out", "Server-side output path for the PNG.") { IsRequired = true };

        var cmd = new Command("rpc-shot",
            "Capture the backbuffer via FNA GetBackBufferData and write a PNG server-side.");
        cmd.AddOption(host);
        cmd.AddOption(port);
        cmd.AddOption(outPath);
        cmd.SetHandler(async (string? h, int? p, string outVal) =>
        {
            var (host2, port2) = RpcClient.ResolveEndpoint(h, p);
            await using var rpc = await RpcClient.ConnectAsync(host2, port2);
            var resp = await rpc.CallAsync(RpcVerbs.CaptureShot, new { path = outVal });
            EmitResponse(resp);
            if (resp.Error is not null) Environment.ExitCode = 1;
        }, host, port, outPath);
        return cmd;
    }

    private static Command BuildXY(string name, string verb, string desc)
    {
        var host = new Option<string?>("--host");
        var port = new Option<int?>("--port");
        var x = new Option<int>("--x") { IsRequired = true };
        var y = new Option<int>("--y") { IsRequired = true };
        var frameMs = new Option<int>("--frame-ms", () => 20);

        var cmd = new Command(name, desc);
        cmd.AddOption(host);
        cmd.AddOption(port);
        cmd.AddOption(x);
        cmd.AddOption(y);
        cmd.AddOption(frameMs);
        cmd.SetHandler(async (string? h, int? p, int xv, int yv, int fm) =>
        {
            await CallAndPace(h, p, verb, new { x = xv, y = yv }, fm);
        }, host, port, x, y, frameMs);
        return cmd;
    }

    private static Command BuildButton(string name, string verb, string desc)
    {
        var host = new Option<string?>("--host");
        var port = new Option<int?>("--port");
        var x = new Option<int>("--x") { IsRequired = true };
        var y = new Option<int>("--y") { IsRequired = true };
        var btn = new Option<string>("--button", () => "left", "left|middle|right");
        var frameMs = new Option<int>("--frame-ms", () => 20);

        var cmd = new Command(name, desc);
        cmd.AddOption(host);
        cmd.AddOption(port);
        cmd.AddOption(x);
        cmd.AddOption(y);
        cmd.AddOption(btn);
        cmd.AddOption(frameMs);
        cmd.SetHandler(async (string? h, int? p, int xv, int yv, string bv, int fm) =>
        {
            await CallAndPace(h, p, verb, new { x = xv, y = yv, button = bv.ToLowerInvariant() }, fm);
        }, host, port, x, y, btn, frameMs);
        return cmd;
    }

    private static async Task CallAndPace(string? h, int? p, string verb, object @params, int frameMs)
    {
        var (host, port) = RpcClient.ResolveEndpoint(h, p);
        await using var rpc = await RpcClient.ConnectAsync(host, port);
        var resp = await rpc.CallAsync(verb, @params);
        EmitResponse(resp);
        if (resp.Error is not null) { Environment.ExitCode = 1; return; }

        // Pace based on queued-frame count so press/release transitions
        // have settled before the caller proceeds.
        var queued = 1;
        try
        {
            if (resp.Result is JsonObject o && o["queued"] is JsonNode q)
                queued = q.GetValue<int>();
        }
        catch { /* keep default */ }

        await Task.Delay(Math.Max(1, queued * frameMs));
    }

    private static void EmitResponse(JsonRpcResponse resp)
    {
        Console.WriteLine(JsonSerializer.Serialize(resp, AgentJsonContext.Default.JsonRpcResponse));
    }
}
