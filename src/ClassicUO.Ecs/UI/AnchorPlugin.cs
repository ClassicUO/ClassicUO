// Window anchoring — drag a movable gump near another of the same kind and it
// snaps flush to the nearest edge; anchored windows form a group that drags and
// (when one closes) shrinks together. ECS reimagining of legacy AnchorManager.
//
// Anchorable kinds (legacy ANCHOR_TYPE): Spell groups mix spell-cast / combat-
// ability / racial / skill buttons; Healthbar groups healthbars. Only same-kind
// windows anchor.
//
// Model is intentionally simpler than legacy's occupancy matrix: a window stores
// only its group id (AnchorMember), snap is edge-to-edge against the nearest
// overlapping neighbour, and move-together replays the dragged window's per-frame
// delta onto its siblings.
// ponytail: no per-cell occupancy — anchoring 3+ windows to the SAME side can
// overlap. Upgrade to a 2D matrix (legacy AnchorGroup) if that ever bites.

using System;
using ClassicUO.Configuration;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal enum AnchorKind : byte { Spell, Healthbar }

// Tag on a movable window root that may anchor to same-kind windows.
internal struct Anchorable { public AnchorKind Kind; }

// Group membership: every window in one anchored block shares a GroupId (from
// AnchorCounter). Dies with the entity, so closing a window auto-leaves its group.
internal struct AnchorMember { public int GroupId; }

internal sealed class AnchorCounter { public int Next = 1; }

// Holds the live drop-preview entity (the translucent ghost shown at the snap
// target while dragging). One reused entity, despawned when the drag ends.
internal sealed class AnchorPreview { public ulong Entity; }

internal readonly struct AnchorPlugin : IPlugin
{
    // Silver, 50% alpha (legacy AnchorableGump ghost).
    private static readonly ClayColor s_previewColor = new(192, 192, 192, 128);

    public void Build(App app)
    {
        app.AddResource(new AnchorCounter());
        app.AddResource(new AnchorPreview());
        var moveFn = MoveGroupTogether;
        var snapFn = SnapOnDrop;
        var previewFn = UpdateDropPreview;
        // Stage.Last: after WindowDragPlugin.Drag (Stage.Update) has written the
        // dragged window's position and published ActiveWindowDrag this frame.
        app.AddSystem(moveFn).InStage(Stage.Last).Build();
        app.AddSystem(snapFn).InStage(Stage.Last).Build();
        app.AddSystem(previewFn).InStage(Stage.Last).Build();
    }

    // While a grouped window is dragged, carry its siblings by the same per-frame
    // delta. Alt (when not in HoldAltToMoveGumps mode) detaches instead, matching
    // legacy AnchorableGump.OnMove.
    private static void MoveGroupTogether(
        Res<ActiveWindowDrag> drag,
        Res<KeyboardContext> keyboard,
        Res<Profile> profile,
        Commands commands,
        Query<Data<AnchorMember, Node>> membersQ)
    {
        var owner = drag.Value.Owner;
        if (owner == 0 || !membersQ.TryGet(owner, out var ownerRow)) return;
        var (_, ownerMember, _) = ownerRow;
        int group = ownerMember.Ref.GroupId;

        bool alt = keyboard.Value.IsPressed(Keys.LeftAlt) || keyboard.Value.IsPressed(Keys.RightAlt);
        if (alt && !profile.Value.HoldAltToMoveGumps)
        {
            // Detach the dragged window (it keeps moving solo — Drag already wrote
            // its position). A group left with a single member dissolves too
            // (legacy 2-member group dissolves on detach).
            commands.Entity(owner).Remove<AnchorMember>();
            ulong lone = 0; int remaining = 0;
            foreach (var (e, m, _) in membersQ)
            {
                if (e.Ref == owner || m.Ref.GroupId != group) continue;
                remaining++; lone = e.Ref;
            }
            if (remaining == 1) commands.Entity(lone).Remove<AnchorMember>();
            return;
        }

        var d = drag.Value.FrameDelta;
        if (d == Vector2.Zero) return;
        foreach (var (e, m, n) in membersQ)
        {
            if (e.Ref == owner || m.Ref.GroupId != group) continue;
            float x = n.Ref.Left.Type == ValType.Px ? n.Ref.Left.Value : 0f;
            float y = n.Ref.Top.Type == ValType.Px ? n.Ref.Top.Value : 0f;
            n.Ref.PositionType = PositionType.Absolute;
            n.Ref.Left = Val.Px(MathF.Round(x + d.X));
            n.Ref.Top = Val.Px(MathF.Round(y + d.Y));
        }
    }

    // On release of an ungrouped anchorable window, snap it flush to the nearest
    // edge of the closest overlapping same-kind window and merge their groups.
    private static void SnapOnDrop(
        Res<ActiveWindowDrag> drag,
        Commands commands,
        ResMut<AnchorCounter> counter,
        Query<Data<Anchorable, Node, ComputedNode>> anchorablesQ,
        Query<Data<AnchorMember>> membersQ)
    {
        var owner = drag.Value.JustReleased;
        if (owner == 0) return;
        if (membersQ.Contains(owner)) return;             // already in a group — stays put
        if (!anchorablesQ.TryGet(owner, out var ownerRow)) return;
        var (_, ownerKind, ownerNode, ownerBb) = ownerRow;

        if (!TryFindSnap(owner, ownerKind.Ref.Kind, ownerBb.Ref, anchorablesQ, out var pos, out var host))
            return;

        ownerNode.Ref.PositionType = PositionType.Absolute;
        ownerNode.Ref.Left = Val.Px(MathF.Round(pos.X));
        ownerNode.Ref.Top = Val.Px(MathF.Round(pos.Y));

        // Merge: join the host's group, or mint a fresh one for the pair.
        int group;
        if (membersQ.TryGet(host, out var hostMember)) { var (_, hm) = hostMember; group = hm.Ref.GroupId; }
        else { group = counter.Value.Next++; commands.Entity(host).Insert(new AnchorMember { GroupId = group }); }
        commands.Entity(owner).Insert(new AnchorMember { GroupId = group });
    }

    // Live translucent ghost at the snap target while an ungrouped anchorable
    // window is being dragged over a same-kind neighbour (legacy ghost preview).
    private static void UpdateDropPreview(
        Res<ActiveWindowDrag> drag,
        ResMut<AnchorPreview> preview,
        Commands commands,
        Query<Data<Anchorable, Node, ComputedNode>> anchorablesQ,
        Query<Data<AnchorMember>> membersQ,
        Query<Data<Node>> nodeQ)
    {
        var owner = drag.Value.Owner;
        bool show = false;
        Vector2 pos = default;
        float sw = 0f, sh = 0f;
        if (owner != 0 && !membersQ.Contains(owner) && anchorablesQ.TryGet(owner, out var ownerRow))
        {
            var (_, ownerKind, _, ownerBb) = ownerRow;
            if (TryFindSnap(owner, ownerKind.Ref.Kind, ownerBb.Ref, anchorablesQ, out pos, out _))
            {
                show = true;
                sw = ownerBb.Ref.Size.X;
                sh = ownerBb.Ref.Size.Y;
            }
        }

        if (!show)
        {
            // No target this frame: drop the ghost entirely once the drag ends,
            // otherwise just hide it (reused on the next overlapping frame).
            if (preview.Value.Entity != 0)
            {
                if (owner == 0) { commands.Entity(preview.Value.Entity).Despawn(); preview.Value.Entity = 0; }
                else if (nodeQ.TryGet(preview.Value.Entity, out var hideRow)) { var (_, hn) = hideRow; hn.Ref.Display = Display.None; }
            }
            return;
        }

        if (preview.Value.Entity == 0 || !nodeQ.Contains(preview.Value.Entity))
        {
            // Spawn once; positioned next frame (Commands is deferred). A bare
            // BackgroundColor node renders as a translucent filled rect.
            var ghost = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                    Width = Val.Px(sw), Height = Val.Px(sh),
                })
                .Insert(new BackgroundColor(s_previewColor))
                .Insert(new GlobalZIndex(40000));
            preview.Value.Entity = ghost.Id;
            return;
        }

        if (nodeQ.TryGet(preview.Value.Entity, out var prow))
        {
            var (_, pn) = prow;
            pn.Ref.Display = Display.Flex;
            pn.Ref.PositionType = PositionType.Absolute;
            pn.Ref.Left = Val.Px(MathF.Round(pos.X));
            pn.Ref.Top = Val.Px(MathF.Round(pos.Y));
            pn.Ref.Width = Val.Px(sw);
            pn.Ref.Height = Val.Px(sh);
        }
    }

    // Nearest-edge snap target for `owner` against the closest overlapping
    // same-kind window. Shared by drop (commit) and preview (ghost).
    private static bool TryFindSnap(
        ulong owner, AnchorKind kind, ComputedNode ob,
        Query<Data<Anchorable, Node, ComputedNode>> anchorablesQ,
        out Vector2 pos, out ulong host)
    {
        pos = default; host = 0;
        ulong best = 0; float bestDist = float.MaxValue;
        ComputedNode hbb = default;
        foreach (var (e, k, _, bb) in anchorablesQ)
        {
            if (e.Ref == owner || k.Ref.Kind != kind) continue;
            var h = bb.Ref;
            // AABB overlap (legacy IsOverlapping) — any touch counts.
            if (ob.Position.X > h.Position.X + h.Size.X || ob.Position.X + ob.Size.X < h.Position.X) continue;
            if (ob.Position.Y > h.Position.Y + h.Size.Y || ob.Position.Y + ob.Size.Y < h.Position.Y) continue;
            float dist = MathF.Abs(ob.Position.X - h.Position.X) + MathF.Abs(ob.Position.Y - h.Position.Y);
            if (dist < bestDist) { bestDist = dist; best = e.Ref; hbb = h; }
        }
        if (best == 0) return false;

        // Edge pick (legacy GetAnchorDirection): scale each axis by the host size
        // so a wide host prefers vertical stacking and a tall one horizontal.
        float dx = ob.Position.X - hbb.Position.X;
        float dy = ob.Position.Y - hbb.Position.Y;
        float xs = MathF.Abs(dx) * 100f / MathF.Max(1f, hbb.Size.X);
        float ys = MathF.Abs(dy) * 100f / MathF.Max(1f, hbb.Size.Y);
        float nx, ny;
        if (xs > ys)
        {
            if (dx > 0) { nx = hbb.Position.X + hbb.Size.X; ny = hbb.Position.Y; }   // right
            else        { nx = hbb.Position.X - ob.Size.X;  ny = hbb.Position.Y; }   // left
        }
        else
        {
            if (dy > 0) { nx = hbb.Position.X;              ny = hbb.Position.Y + hbb.Size.Y; } // bottom
            else        { nx = hbb.Position.X;              ny = hbb.Position.Y - ob.Size.Y;  } // top
        }

        pos = new Vector2(nx, ny);
        host = best;
        return true;
    }
}
