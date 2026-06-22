using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Single-clicking a world item/mobile when the server has NOT enabled tooltips
// (OPL) requests its name with a 0x09 ClickRequest; the server replies with a
// 0x1C/0xAE Talk that the speech observers already turn into overhead text
// (TextOverHeadManager). This is the legacy DelayedObjectClickManager naming
// path — the StaticNameLabelPlugin sibling for network entities, except the
// name comes from the server, not local tiledata.
//
// The request is delayed by the double-click window: a double-click is a
// use-object gesture (UseObjectPlugin) and cancels the pending name request, so
// using something doesn't flash its name first. With tooltips enabled, the
// hover OPL path owns naming and this stays silent.
internal struct SingleClickNameLatch
{
    public bool Armed;
    public Vector2 PressPos;
    public bool Pending;
    public uint Serial;
    public float Deadline;
}

internal readonly struct SingleClickNamePlugin : IPlugin
{
    public void Build(App app)
    {
        // Where the server's name reply for a gump item should float: the screen
        // point the user clicked (legacy DelayedObjectClickManager.X/Y). Keyed by
        // serial so the render can place the reply when it arrives a few frames
        // later. Equipment/backpack overlays span the whole paperdoll window, so
        // their ComputedNode can't give a per-item anchor — the click point can.
        app.AddResource(new GumpNameClickPos());

        var fn = RequestSingleClickName;
        app.AddSystem(fn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

        // The sibling path for items that live inside a gump (a container slot,
        // a paperdoll equipment overlay). The world system above resolves its
        // target from SelectedEntity, which over a gump is the UI entity (no
        // NetworkSerial) -> it bails. Hit-test the gump element directly via
        // UiPick instead. Legacy fires this from ContainerGump/PaperdollGump
        // single-click (DelayedObjectClickManager.Set).
        var gumpFn = RequestGumpSingleClickName;
        app.AddSystem(gumpFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();
    }

    private static void RequestSingleClickName(
        Res<MouseContext> mouse,
        Res<Time> time,
        Res<NetClient> net,
        Res<GameContext> gameCtx,
        Res<GrabbedItem> grabbed,
        Res<SelectedEntity> selected,
        Res<TargetingState> targeting,
        Local<SingleClickNameLatch> latch,
        Query<Data<NetworkSerial>> entities)
    {
        // A target cursor click on an object belongs to targeting, not naming.
        // Clear the latch (don't just freeze it) so a pending request armed in
        // the double-click window before the cursor came up can't fire after the
        // target click clears IsTargeting.
        if (targeting.Value.IsTargeting)
        {
            latch.Value.Pending = false;
            latch.Value.Armed = false;
            return;
        }

        // Server-side tooltips on -> the hover OPL request owns naming.
        if (TooltipsEnabled(gameCtx.Value))
        {
            latch.Value.Pending = false;
            return;
        }

        // A second click landed -> the double-click (use-object) path owns it.
        if (latch.Value.Pending && mouse.Value.IsPressedDouble(MouseButtonType.Left))
            latch.Value.Pending = false;

        if (latch.Value.Pending && time.Value.Total >= latch.Value.Deadline)
        {
            net.Value.Send_ClickRequest(latch.Value.Serial);
            latch.Value.Pending = false;
        }

        // Anchor the press ourselves (MouseContext.DraggingOffset reports an
        // absolute position under the agent harness's synthetic mouse) so the
        // drag rejection holds for both real and scripted input.
        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            latch.Value.Armed = true;
            latch.Value.PressPos = mouse.Value.Position;
            return;
        }

        if (!mouse.Value.IsReleased(MouseButtonType.Left) || !latch.Value.Armed)
            return;
        latch.Value.Armed = false;

        // Released after a drag (camera pan) or while holding an item -> not a
        // name click.
        var delta = mouse.Value.Position - latch.Value.PressPos;
        if (Math.Abs(delta.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
            || Math.Abs(delta.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            return;
        if (grabbed.Value.Serial != 0)
            return;

        // A pick that came from overhead speech text resolves to its owner, not
        // the object under the cursor.
        if (selected.Value.IsText)
            return;

        // Statics carry no NetworkSerial (StaticNameLabelPlugin handles those);
        // a click over a gump window leaves SelectedEntity unclaimed -> bails.
        if (!entities.TryGet(selected.Value.Entity, out var row))
            return;

        var (_, serial) = row;
        var s = serial.Ref.Value;
        if (!SerialHelper.IsMobile(s) && !SerialHelper.IsItem(s))
            return;

        latch.Value.Pending = true;
        latch.Value.Serial = s;
        latch.Value.Deadline = time.Value.Total + MouseContext.DoubleClickDelta;
    }

    // Items inside a gump (container slot / paperdoll equipment) carry no
    // SelectedEntity claim usable by the world path, so hit-test the rendered UI
    // element under the cursor with the shared pixel-perfect UiPick and read the
    // serial off ContainerItemUI / PaperdollEquipUI. Same delayed-request latch
    // as the world path: a double-click (use/equip) inside the window cancels it.
    private static void RequestGumpSingleClickName(
        Res<MouseContext> mouse,
        Res<Time> time,
        Res<NetClient> net,
        Res<GameContext> gameCtx,
        Res<GrabbedItem> grabbed,
        Res<AssetsServer> assets,
        Res<TargetingState> targeting,
        UiGesturePick pick,
        Local<SingleClickNameLatch> latch,
        ResMut<GumpNameClickPos> clickPos,
        Query<Data<ContainerItemUI>> containerItems,
        Query<Data<PaperdollEquipUI>> equipItems,
        Query<Data<PaperdollBackpackUI>> backpackUi)
    {
        if (targeting.Value.IsTargeting)
        {
            latch.Value.Pending = false;
            latch.Value.Armed = false;
            return;
        }

        if (TooltipsEnabled(gameCtx.Value))
        {
            latch.Value.Pending = false;
            return;
        }

        if (latch.Value.Pending && mouse.Value.IsPressedDouble(MouseButtonType.Left))
            latch.Value.Pending = false;

        if (latch.Value.Pending && time.Value.Total >= latch.Value.Deadline)
        {
            net.Value.Send_ClickRequest(latch.Value.Serial);
            latch.Value.Pending = false;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            latch.Value.Armed = true;
            latch.Value.PressPos = mouse.Value.Position;
            return;
        }

        if (!mouse.Value.IsReleased(MouseButtonType.Left) || !latch.Value.Armed)
            return;
        latch.Value.Armed = false;

        var delta = mouse.Value.Position - latch.Value.PressPos;
        if (Math.Abs(delta.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
            || Math.Abs(delta.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            return;
        if (grabbed.Value.Serial != 0)
            return;

        var hit = pick.Topmost(mouse.Value.Position, assets.Value);
        if (!hit.Found)
            return;

        uint s = 0;
        if (containerItems.TryGet(hit.Entity, out var ciRow))
        {
            var (_, link) = ciRow;
            s = link.Ref.Serial;
        }
        else if (equipItems.TryGet(hit.Entity, out var peRow))
        {
            var (_, equip) = peRow;
            s = equip.Ref.ItemSerial;
        }
        else if (backpackUi.TryGet(hit.Entity, out var bpRow))
        {
            var (_, bp) = bpRow;
            s = bp.Ref.ItemSerial;
        }

        if (s == 0 || (!SerialHelper.IsMobile(s) && !SerialHelper.IsItem(s)))
            return;

        latch.Value.Pending = true;
        latch.Value.Serial = s;
        latch.Value.Deadline = time.Value.Total + MouseContext.DoubleClickDelta;
        // Remember the clicked element + the offset within it, so the reply
        // floats over the item and follows the gump as it moves.
        var offset = mouse.Value.Position;
        if (pick.Rendered.TryGet(hit.Entity, out var rRow))
        {
            var (_, cn, _, _, _, _) = rRow;
            // ComputedNode.Position is a Clay/System.Numerics vector, not XNA's
            // Vector2 — subtract per component.
            offset = new Vector2(offset.X - cn.Ref.Position.X, offset.Y - cn.Ref.Position.Y);
        }
        clickPos.Value.Map[s] = (hit.Entity, offset);
    }

    // Legacy ClientFeatures.SetFlags: tooltips live behind the paladin/necromancer
    // feature bit and the AOS-era client version.
    private static bool TooltipsEnabled(GameContext ctx)
        => (ctx.ClientFeatures & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0
           && ctx.ClientVersion >= ClientVersion.CV_308Z;
}

// Per item serial: the UI element clicked + the click offset within that
// element's box, so the overhead render can float the server's name reply over
// the item AND track it as the gump window moves (the element's ComputedNode is
// recomputed every frame). A fixed screen point would not follow the window.
// Mirrors legacy DelayedObjectClickManager.X/Y (click relative to the gump).
// ponytail: grows by distinct clicked serials over a session — trivially small.
internal sealed class GumpNameClickPos
{
    public readonly Dictionary<uint, (ulong Element, Vector2 Offset)> Map = new();
}
