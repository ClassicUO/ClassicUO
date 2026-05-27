// SPDX-License-Identifier: BSD-2-Clause
//
// Synthetic mouse + keyboard input. Synthesis goes through MouseContext's
// AGENT_BUILD-only synth state path, not via real OS input — so events
// reach the ECS without focus theft, raise-Z tricks, or OS cursor
// movement that interferes with the human running the agent.
//
// Multi-frame sequences (click = down+up = 2 frames, double-click = 4
// frames) are enqueued as separate frames on AgentServerState.
// PendingMouseFrames. AdvanceSyntheticMouseSystem drains one per tick so
// MouseContext.Update sees one (oldState, newState) transition per
// frame; coalescing multiple transitions inside a frame would defeat
// IsPressedOnce / IsPressedDouble bookkeeping.
//
// All input verbs return { queued: N } where N is the number of frames
// the caller should wait before the input is fully consumed (about
// 16-17 ms per frame at 60 FPS; the CLI sleeps N*20 ms to be safe).

#if AGENT_BUILD
#nullable enable

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Host;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class InputHandlers
{
    public static void Register(AgentDispatcher<GameController> d)
    {
        d.Register("input.mouseMove", MouseMove);
        d.Register("input.mouseDown", MouseDown);
        d.Register("input.mouseUp", MouseUp);
        d.Register("input.mouseClick", MouseClick);
        d.Register("input.mouseDoubleClick", MouseDoubleClick);
        d.Register("input.mouseHold", MouseHold);
        d.Register("input.mouseRelease", MouseRelease);
        d.Register("input.clear", InputClear);
        d.Register("input.type", Type);
    }

    public static JsonRpcResponse MouseMove(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        var current = ctx.State.CurrentMouseSynth;
        ctx.State.PendingMouseFrames.Enqueue(new SynthMouseFrame
        {
            X = x, Y = y,
            Left = current.Left, Middle = current.Middle, Right = current.Right,
        });
        return Ok(req, 1);
    }

    public static JsonRpcResponse MouseDown(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        if (!TryGetButton(req, out var btn, out err)) return err!;
        var s = ctx.State.CurrentMouseSynth;
        s = SetButton(s, btn, ButtonState.Pressed);
        s.X = x; s.Y = y;
        ctx.State.PendingMouseFrames.Enqueue(s);
        return Ok(req, 1);
    }

    public static JsonRpcResponse MouseUp(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        if (!TryGetButton(req, out var btn, out err)) return err!;
        var s = ctx.State.CurrentMouseSynth;
        s = SetButton(s, btn, ButtonState.Released);
        s.X = x; s.Y = y;
        ctx.State.PendingMouseFrames.Enqueue(s);
        return Ok(req, 1);
    }

    public static JsonRpcResponse MouseClick(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        if (!TryGetButton(req, out var btn, out err)) return err!;
        var s = ctx.State.CurrentMouseSynth;
        s.X = x; s.Y = y;
        var down = SetButton(s, btn, ButtonState.Pressed);
        // UiPointer.Down latches off MouseContext.IsPressed, which requires
        // BOTH oldState and newState to be Pressed. Single-frame Pressed
        // shows up as IsPressedOnce only; the UI press-edge detector in
        // InteractionSystem.PostLayout never sees Down=true. Hold Pressed
        // for two consecutive frames so the second frame trips IsPressed
        // → UiPointer.Down=true → press edge fires.
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(down);
        var up = SetButton(down, btn, ButtonState.Released);
        ctx.State.PendingMouseFrames.Enqueue(up);
        return Ok(req, 3);
    }

    public static JsonRpcResponse MouseDoubleClick(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        if (!TryGetButton(req, out var btn, out err)) return err!;
        var s = ctx.State.CurrentMouseSynth;
        s.X = x; s.Y = y;
        var down = SetButton(s, btn, ButtonState.Pressed);
        var up = SetButton(down, btn, ButtonState.Released);
        // Each click is enqueued as (down, down, up) so the press edge
        // detector sees two consecutive Pressed frames (matches MouseClick).
        // A single d/u/d/u sequence registered only as IsPressedOnce and
        // the second UiClick was lost.
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(up);
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(up);
        return Ok(req, 6);
    }

    public static JsonRpcResponse MouseHold(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
        => MouseDown(req, in ctx);

    public static JsonRpcResponse MouseRelease(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
        => MouseUp(req, in ctx);

    public static JsonRpcResponse InputClear(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
    {
        ctx.State.PendingMouseFrames.Clear();
        ctx.State.CurrentMouseSynth = default;
        ClassicUO.Input.Mouse.AgentClearSynthetic();
        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["cleared"] = true } };
    }

    public static JsonRpcResponse Type(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
    {
        if (req.Params is not JsonElement p || p.ValueKind != JsonValueKind.Object)
            return AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.InvalidParams,
                "input.type expects { text }");
        if (!p.TryGetProperty("text", out var tEl) || tEl.ValueKind != JsonValueKind.String)
            return AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.InvalidParams,
                "input.type: 'text' must be a string");

        var text = tEl.GetString() ?? string.Empty;
        var pushed = PushTextInputEvents(text, in ctx);
        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["pushed"] = pushed } };
    }

    // Main's GameController consumes SDL_EVENT_TEXT_INPUT directly from its
    // poll loop and forwards to UIManager.KeyboardFocusControl.InvokeTextInput
    // and Scene.OnTextInput. Bypass SDL entirely and call those same sinks —
    // works regardless of whether FNA is on SDL2 or SDL3.
    private static int PushTextInputEvents(string text, in AgentRpcContext<GameController> ctx)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        UIManager.KeyboardFocusControl?.InvokeTextInput(text);
        ctx.Runtime.Scene?.OnTextInput(text);
        return text.Length;
    }

    private static SynthMouseFrame SetButton(SynthMouseFrame s, MouseButton b, ButtonState v)
    {
        switch (b)
        {
            case MouseButton.Left: s.Left = v; break;
            case MouseButton.Middle: s.Middle = v; break;
            case MouseButton.Right: s.Right = v; break;
        }
        return s;
    }

    private enum MouseButton { Left, Middle, Right }

    private static bool TryGetXY(JsonRpcRequest req, out int x, out int y, out JsonRpcResponse? err)
    {
        x = 0; y = 0; err = null;
        if (req.Params is not JsonElement p || p.ValueKind != JsonValueKind.Object)
        {
            err = AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.InvalidParams,
                "expects an object with 'x' and 'y'");
            return false;
        }
        if (!p.TryGetProperty("x", out var ex) || !ex.TryGetInt32(out x) ||
            !p.TryGetProperty("y", out var ey) || !ey.TryGetInt32(out y))
        {
            err = AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.InvalidParams,
                "x and y must be integers");
            return false;
        }
        return true;
    }

    private static bool TryGetButton(JsonRpcRequest req, out MouseButton btn, out JsonRpcResponse? err)
    {
        btn = MouseButton.Left;
        err = null;
        if (req.Params is not JsonElement p) return true; // default Left
        if (!p.TryGetProperty("button", out var be) || be.ValueKind != JsonValueKind.String)
            return true; // default Left
        var s = be.GetString();
        switch (s)
        {
            case "left": btn = MouseButton.Left; return true;
            case "middle": btn = MouseButton.Middle; return true;
            case "right": btn = MouseButton.Right; return true;
            default:
                err = AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.InvalidParams,
                    $"button must be left|middle|right, got '{s}'");
                return false;
        }
    }

    private static JsonRpcResponse Ok(JsonRpcRequest req, int queuedFrames)
        => new() { Id = req.Id, Result = new JsonObject { ["queued"] = queuedFrames } };
}

#endif
