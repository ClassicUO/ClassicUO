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
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Host;
using ClassicUO.Assets;
using ClassicUO.Ecs;
using Microsoft.Xna.Framework;
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
        d.Register("debug.openVendor", DebugOpenVendor);
        d.Register("debug.openPopup", DebugOpenPopup);
        d.Register("debug.openSplit", DebugOpenSplit);
        d.Register("debug.openServerGump", DebugOpenServerGump);
        d.Register("debug.dumpLayout", DebugDumpLayout);
    }

    // Test-only: toggle "print the UI element stack under each left-click" (set
    // {"enable": false} to turn off; default toggles on). Output goes to the
    // client console as [Layout] lines — topmost element first — for inspecting
    // the pixel-perfect hit-test against the real layout.
    public static JsonRpcResponse DebugDumpLayout(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        var dump = ctx.Runtime.GetResource<DebugLayoutDump>();
        bool enable = !dump.DumpOnClick;
        if (req.Params is JsonElement p && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("enable", out var ee) && (ee.ValueKind == JsonValueKind.True || ee.ValueKind == JsonValueKind.False))
            enable = ee.GetBoolean();
        dump.DumpOnClick = enable;
        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["dumpOnClick"] = enable } };
    }

    // Test-only: spawn a NOTICE-style server gump (resizepic frame + scrollable
    // htmlgump + OK/Cancel buttons) through the real 0xB0 path. Reproduces the
    // server-html-gump drag case deterministically. Optional x/y/gumpId.
    public static JsonRpcResponse DebugOpenServerGump(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        int x = 100, y = 100; uint gumpId = 0xD00D;
        string? layout = null;
        string[]? lines = null;
        if (req.Params is JsonElement p && p.ValueKind == JsonValueKind.Object)
        {
            if (p.TryGetProperty("x", out var xe) && xe.TryGetInt32(out var xv)) x = xv;
            if (p.TryGetProperty("y", out var ye) && ye.TryGetInt32(out var yv)) y = yv;
            if (p.TryGetProperty("gumpId", out var ge) && ge.TryGetUInt32(out var gv)) gumpId = gv;
            if (p.TryGetProperty("layout", out var le) && le.ValueKind == JsonValueKind.String) layout = le.GetString();
            if (p.TryGetProperty("lines", out var lne) && lne.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var el in lne.EnumerateArray()) list.Add(el.GetString() ?? string.Empty);
                lines = list.ToArray();
            }
        }

        layout ??= "{ resizepic 0 0 2486 420 230 }{ htmlgump 20 20 360 180 0 1 1 }{ button 30 195 247 248 1 0 1 }{ button 200 195 242 241 1 0 0 }";
        lines ??= new[]
        {
            "This is a long NOTICE-style html body. It should wrap and scroll inside the box. " +
            "Line two of the body text. Line three. Line four to force the content taller than the " +
            "box so a scrollbar and the scroll wrapper exist, reproducing the html drag case.",
        };

        var q = ctx.Runtime.GetResource<DebugServerGumpQueue>();
        q.Pending.Add((gumpId, x, y, layout, lines));
        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["opened"] = true, ["gumpId"] = gumpId } };
    }

    // Test-only: open the split-stack menu without a server item / drag. The
    // live path is drag-only (PickupPlugin intercepts a stackable drag), which
    // the synthetic mouse can't drive, so seed SplitPrompt directly and let
    // SplitMenuPlugin.OpenWindow build it. The gump anchors at the cursor minus
    // (80,40), so move the synthetic mouse first for deterministic placement.
    // Optional "amount" sets the stack size (slider max); default 750.
    public static JsonRpcResponse DebugOpenSplit(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        int amount = 750;
        if (req.Params is JsonElement p && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("amount", out var ae) && ae.TryGetInt32(out var av) && av > 1)
            amount = av;

        var prompt = ctx.Runtime.GetResource<SplitPrompt>();
        prompt.Open = true;
        prompt.Built = false;
        prompt.HasPos = true;
        prompt.PosX = 250;
        prompt.PosY = 180;
        prompt.PendingSerial = 0x40001234u;
        prompt.Graphic = 0x0EED;   // gold pile
        prompt.Hue = 0;
        prompt.MaxAmount = amount;
        prompt.SourceUiEntity = 0;
        prompt.SourceContainer = 0;
        prompt.OriginalGraphic = 0x0EED;
        prompt.OriginalHue = 0;
        prompt.OriginalAmount = (ushort)amount;
        prompt.OriginalX = 0;
        prompt.OriginalY = 0;
        prompt.OriginalZ = 0;
        prompt.OriginalContainer = 0;
        prompt.OriginalGridIndex = 0;
        prompt.OriginalFromSlot = false;

        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["opened"] = true, ["amount"] = amount } };
    }

    // Test-only: open a context/popup menu without a server entity. Mirrors the
    // 0xBF 0x14 path by setting PopupMenuState directly. Real cliloc strings are
    // pulled from the loaded Cliloc file so the rendered labels are realistic;
    // one entry is flagged disabled (greyed) to exercise that hue.
    public static JsonRpcResponse DebugOpenPopup(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        int x = 120, y = 80;
        if (req.Params is JsonElement p && p.ValueKind == JsonValueKind.Object)
        {
            if (p.TryGetProperty("x", out var xe) && xe.TryGetInt32(out var xv)) x = xv;
            if (p.TryGetProperty("y", out var ye) && ye.TryGetInt32(out var yv)) y = yv;
        }

        var fm = ctx.Runtime.GetResource<UOFileManager>();
        var items = new List<OnExtendedCommandPacket_0xBF.PopupMenuItemData>();
        ushort idx = 0;
        // Scan a dense cliloc range for non-empty strings to use as menu labels.
        for (int c = 3000122; c < 3002000 && items.Count < 5; c++)
        {
            var s = fm.Clilocs.GetString(c);
            if (string.IsNullOrEmpty(s)) continue;
            bool disabled = items.Count == 2;   // grey the third entry
            items.Add(new OnExtendedCommandPacket_0xBF.PopupMenuItemData
            {
                Cliloc = c,
                Index = idx,
                Hue = disabled ? (ushort)0x0386 : (ushort)0xFFFF,
                ReplacedHue = 0,
                Flags = (ushort)(disabled ? 0x01 : 0x00),
            });
            idx++;
        }

        var state = ctx.Runtime.GetResource<PopupMenuState>();
        state.SetPending(0x00000001u, items.ToArray());
        state.RequestPos = new Vector2(x, y);

        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["opened"] = true, ["count"] = items.Count } };
    }

    // Test-only: open a vendor gump without a server NPC. Sell mode is fully
    // faked (store + a few entries). Buy mode just opens the buy panels (items
    // come from the vendor's shop containers on a real server) to verify the
    // buy art + gold label.
    public static JsonRpcResponse DebugOpenVendor(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        uint serial = 0x4000BEEF;
        bool isBuy = false;
        if (req.Params is JsonElement p && p.ValueKind == JsonValueKind.Object
            && p.TryGetProperty("isBuy", out var bEl) && bEl.ValueKind == JsonValueKind.True)
            isBuy = true;

        var store = ctx.Runtime.GetResource<VendorStore>();
        if (!isBuy)
        {
            store.SellByVendor[serial] = new System.Collections.Generic.List<VendorEntry>
            {
                new() { Serial = 0x4001, Graphic = 0x0EED, Hue = 0, Amount = 250, Price = 5,  Name = "Gold Coin" },
                new() { Serial = 0x4002, Graphic = 0x0F3F, Hue = 0, Amount = 12,  Price = 18, Name = "Arrow" },
                new() { Serial = 0x4003, Graphic = 0x13B2, Hue = 0, Amount = 1,   Price = 220, Name = "Bow" },
                new() { Serial = 0x4004, Graphic = 0x0F0E, Hue = 0, Amount = 40,  Price = 3,  Name = "Bandage" },
                new() { Serial = 0x4005, Graphic = 0x097F, Hue = 0, Amount = 5,   Price = 75, Name = "Leather Tunic" },
            };
        }
        store.Revision++;
        ctx.Runtime.SendEvent(new VendorOpenedEvent(serial, isBuy));
        return new JsonRpcResponse { Id = req.Id, Result = new JsonObject { ["opened"] = true } };
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
