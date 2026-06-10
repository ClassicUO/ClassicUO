// Port of legacy Game/Managers/TargetManager.cs to the ECS branch.
//
// Flow: the server pushes 0x6C (OnTargetCursorPacket) to ask the client to
// pick a target. We flip TargetingState.IsTargeting on; the targeting cursor
// reticle renders at the mouse, normal world interaction (pickup / use) is
// gated off (RunIf(!IsTargeting) on those plugins), and the NEXT left-click on
// a world object/tile sends the 0x6C response (Send_TargetObject for an entity,
// Send_TargetXYZ for ground/static) echoing the server's cursorID + cursorType.
// Right-click or Escape cancels (Send_TargetCancel).
//
// World selection reuses Res<SelectedEntity> — the same one-frame-lagged hit
// the pickup/use systems read — so targeting picks exactly what the highlight
// shows. Object vs position is decided by whether the hit entity carries a
// NetworkSerial (mobile/ground item) or is a land/static tile.

using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct TargetingPlugin : IPlugin
{
    // The targeting reticle cursor (legacy _cursorData[war, 12]) is drawn by
    // GameCursorPlugin — it owns the whole cursor (border-clip, hotspot, felucca
    // hue) so the reticle gets the same treatment as every other cursor state.
    // This plugin only owns the targeting STATE + click/cancel resolution.

    public void Build(App app)
    {
        app.AddResource(new TargetingState());

        // Server -> client target request. Cancel (type 3) just clears; any
        // other type arms targeting with the server's echoed cursor id/type.
        app.AddObserver((
            On<PacketReceived<OnTargetCursorPacket_0x6C>> trig,
            ResMut<TargetingState> targeting) =>
        {
            var packet = trig.Event.Packet;
            if (packet.CursorType >= (byte)TargetType.Cancel)
            {
                targeting.Value.Clear();
                return;
            }

            targeting.Value.IsTargeting = true;
            targeting.Value.Mode = packet.CursorTarget;
            targeting.Value.CursorId = packet.CursorId;
            targeting.Value.CursorType = packet.CursorType;
        });

        var clickFn = ResolveTargetClick;
        var cancelFn = CancelTarget;

        // Stage.First so the target/cancel consume their mouse button before the
        // pickup latch (also Stage.First, gated off while targeting) and before
        // PreUpdate's right-click gump-close / world movement see it.
        app
            .AddSystem(cancelFn)
                .InStage(Stage.First)
                .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
                .RunIf((Res<TargetingState> t) => t.Value.IsTargeting)
                .Build()
            .AddSystem(clickFn)
                .InStage(Stage.First)
                .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
                .RunIf((Res<TargetingState> t) => t.Value.IsTargeting)
                .RunIf((Res<MouseContext> m) => m.Value.IsPressedOnce(Input.MouseButtonType.Left))
                .Build();
    }

    private static void ResolveTargetClick(
        Res<MouseContext> mouse,
        ResMut<TargetingState> targeting,
        Res<SelectedEntity> selected,
        Res<NetClient> net,
        Query<Data<NetworkSerial, Graphic, WorldPosition>> objectQ,
        Query<Data<Graphic, WorldPosition>, Filter<With<IsTile>>> landQ,
        Query<Data<Graphic, WorldPosition>, Filter<With<IsStatic>>> staticQ)
    {
        // The click belongs to targeting whether or not it lands on something —
        // swallow it so a miss doesn't fall through to movement/pickup.
        mouse.Value.Consume(Input.MouseButtonType.Left);

        var ent = selected.Value.Entity;
        if (ent == 0)
            return;

        var cursorId = targeting.Value.CursorId;
        var cursorType = targeting.Value.CursorType;

        if (objectQ.TryGet(ent, out var objectRow))
        {
            var (_, serial, graphic, pos) = objectRow;
            net.Value.Send_TargetObject(
                serial.Ref.Value, graphic.Ref.Value,
                pos.Ref.X, pos.Ref.Y, pos.Ref.Z, cursorId, cursorType);
            targeting.Value.Clear();
            return;
        }

        // Land sends graphic 0; a static sends its own graphic (legacy parity).
        if (landQ.TryGet(ent, out var landRow))
        {
            var (_, _, pos) = landRow;
            net.Value.Send_TargetXYZ(0, pos.Ref.X, pos.Ref.Y, pos.Ref.Z, cursorId, cursorType);
            targeting.Value.Clear();
            return;
        }

        if (staticQ.TryGet(ent, out var staticRow))
        {
            var (_, graphic, pos) = staticRow;
            net.Value.Send_TargetXYZ(graphic.Ref.Value, pos.Ref.X, pos.Ref.Y, pos.Ref.Z, cursorId, cursorType);
            targeting.Value.Clear();
        }
    }

    private static void CancelTarget(
        Res<MouseContext> mouse,
        Res<KeyboardContext> keyboard,
        ResMut<TargetingState> targeting,
        Res<NetClient> net)
    {
        bool rightClick = mouse.Value.IsPressedOnce(Input.MouseButtonType.Right);
        bool escape = keyboard.Value.IsPressedOnce(Keys.Escape);
        if (!rightClick && !escape)
            return;

        if (rightClick)
            mouse.Value.Consume(Input.MouseButtonType.Right);

        net.Value.Send_TargetCancel(targeting.Value.Mode, targeting.Value.CursorId, targeting.Value.CursorType);
        targeting.Value.Clear();
    }

    // Legacy TargetType (0x6C cursorType byte). Cancel is the "not targeting"
    // sentinel the server uses to clear an active cursor.
    private enum TargetType : byte
    {
        Neutral = 0,
        Harmful = 1,
        Beneficial = 2,
        Cancel = 3,
    }
}

// Singleton mirroring the live state of legacy TargetManager: whether a target
// cursor is up and the server-echoed cursor id/type/mode needed to build the
// 0x6C response.
internal sealed class TargetingState
{
    public bool IsTargeting { get; set; }
    public byte Mode { get; set; }        // 0 object, 1 position, 2 multi
    public uint CursorId { get; set; }
    public byte CursorType { get; set; }  // 0 neutral, 1 harmful, 2 beneficial

    public void Clear()
    {
        IsTargeting = false;
        Mode = 0;
        CursorId = 0;
        CursorType = 0;
    }
}
