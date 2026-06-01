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
using ClassicUO.Ecs;
using Microsoft.Xna.Framework.Input;
using TinyEcs.Bevy;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class InputHandlers
{
    public static void Register(AgentDispatcher<App> d)
    {
        d.Register("input.mouseMove", MouseMove);
        d.Register("input.mouseDown", MouseDown);
        d.Register("input.mouseUp", MouseUp);
        d.Register("input.mouseClick", MouseClick);
        d.Register("input.mouseDoubleClick", MouseDoubleClick);
        d.Register("input.mouseHold", MouseHold);
        d.Register("input.mouseRelease", MouseRelease);
        d.Register("input.mouseWheel", MouseWheel);
        d.Register("input.clear", InputClear);
        d.Register("input.type", Type);
        d.Register("debug.openSpellbook", DebugOpenSpellbook);
    }

    // Test-only: deterministically open a spellbook without a server item.
    // Populates SpellbookStore with the given (or full) spell mask and fires the
    // ContainerOpenedEvent the 0x24 0xFFFF path would. Optional "graphic" param
    // selects the school via the item graphic (default 0x0EFA == Magery); "bits"
    // sets the present-spell mask. Handlers run on the game thread (see
    // AgentRpcContext) so direct resource/event access is safe.
    public static JsonRpcResponse DebugOpenSpellbook(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        ulong bits = ulong.MaxValue; // all spells present
        ushort graphic = 0x0EFA;     // Magery spellbook item graphic
        if (req.Params is JsonElement p && p.ValueKind == JsonValueKind.Object)
        {
            if (p.TryGetProperty("bits", out var bEl) && bEl.TryGetUInt64(out var b))
                bits = b;
            if (p.TryGetProperty("graphic", out var gEl) && gEl.TryGetUInt16(out var g))
                graphic = g;
        }

        // Distinct serial per graphic so each school opens its own window (a real
        // server gives every spellbook item its own serial).
        uint serial = 0x40000000u | graphic;

        var store = ctx.Runtime.GetResource<SpellbookStore>();
        store.BySerial[serial] = new SpellbookData { School = SpellSchools.Resolve(graphic), Bitfields = bits };
        store.Revision++;
        ctx.Runtime.SendEvent(new ContainerOpenedEvent(serial, 0xFFFF));
        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["opened"] = true } };
    }

    public static JsonRpcResponse MouseMove(JsonRpcRequest req, in AgentRpcContext<App> ctx)
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

    public static JsonRpcResponse MouseDown(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        if (!TryGetButton(req, out var btn, out err)) return err!;
        var s = ctx.State.CurrentMouseSynth;
        s = SetButton(s, btn, ButtonState.Pressed);
        s.X = x; s.Y = y;
        ctx.State.PendingMouseFrames.Enqueue(s);
        return Ok(req, 1);
    }

    public static JsonRpcResponse MouseUp(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        if (!TryGetButton(req, out var btn, out err)) return err!;
        var s = ctx.State.CurrentMouseSynth;
        s = SetButton(s, btn, ButtonState.Released);
        s.X = x; s.Y = y;
        ctx.State.PendingMouseFrames.Enqueue(s);
        return Ok(req, 1);
    }

    public static JsonRpcResponse MouseClick(JsonRpcRequest req, in AgentRpcContext<App> ctx)
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

    public static JsonRpcResponse MouseDoubleClick(JsonRpcRequest req, in AgentRpcContext<App> ctx)
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
        // the second UiClick was lost — see AGENTS.md pitfalls (now stale).
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(up);
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(down);
        ctx.State.PendingMouseFrames.Enqueue(up);
        return Ok(req, 6);
    }

    // input.mouseWheel { x, y, delta } — delta in notches (+up / -down). Moves
    // the synthetic cursor to (x,y) and applies the wheel on that frame so the
    // UI scroll container under the cursor receives it.
    public static JsonRpcResponse MouseWheel(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        if (!TryGetXY(req, out var x, out var y, out var err)) return err!;
        int delta = 0;
        if (req.Params is JsonElement p && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("delta", out var dEl) && dEl.TryGetInt32(out var d))
            delta = d;
        var s = ctx.State.CurrentMouseSynth;
        s.X = x; s.Y = y; s.Wheel = delta;
        ctx.State.PendingMouseFrames.Enqueue(s);
        // Follow-up frame with Wheel=0 so the per-frame delta resets next tick.
        var rest = s; rest.Wheel = 0;
        ctx.State.PendingMouseFrames.Enqueue(rest);
        return Ok(req, 2);
    }

    public static JsonRpcResponse MouseHold(JsonRpcRequest req, in AgentRpcContext<App> ctx)
        => MouseDown(req, in ctx);

    public static JsonRpcResponse MouseRelease(JsonRpcRequest req, in AgentRpcContext<App> ctx)
        => MouseUp(req, in ctx);

    public static JsonRpcResponse InputClear(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        ctx.State.PendingMouseFrames.Clear();
        ctx.State.CurrentMouseSynth = default;
        ctx.Runtime.GetResource<MouseContext>().AgentClearSynthetic();
        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["cleared"] = true } };
    }

    public static JsonRpcResponse Type(JsonRpcRequest req, in AgentRpcContext<App> ctx)
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

    // Queue typed text into AgentServerState. A per-frame system in
    // AgentServerPlugin drains it and emits CharInputEvent via the
    // engine's EventWriter — same channel the real keyboard path uses
    // through TextInputEXT.TextInput → CharInputEvent. SDL_PushEvent
    // would be the more direct route but is brittle on the SDL3 path
    // where bindings forward to a native dll with a different event
    // struct layout.
    private static int PushTextInputEvents(string text, in AgentRpcContext<App> ctx)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        foreach (var ch in text)
        {
            ctx.State.PendingTypedChars.Enqueue(ch);
        }
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
