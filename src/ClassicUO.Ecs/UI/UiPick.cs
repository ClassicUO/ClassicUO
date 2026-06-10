// Shared UI hit-testing. Every gump gesture — drag, right-click-close, top-bar
// yield, container pickup, hover selection — needs the SAME answer: "what UO
// element is topmost under the cursor, pixel-perfect, and which window owns
// it?". Each system used to hand-roll that loop and they kept diverging (ClayId
// vs PaintOrder tiebreaks, roots-only scans that missed child sprites, z-blind
// vetoes). This is the one implementation they all call.

using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct UiHit
{
    public readonly ulong Entity;
    public readonly int PaintOrder;

    public UiHit(ulong entity, int paintOrder)
    {
        Entity = entity;
        PaintOrder = paintOrder;
    }

    public static readonly UiHit None = new(0, int.MinValue);
    public bool Found => Entity != 0;
}

internal static class UiPick
{
    // Topmost rendered, pixel-hit, visible element at `pos`. PaintOrder is the
    // render-command index Clay assigns in z-then-tree order, so the MAX
    // PaintOrder is whatever is drawn last = on top — z is already folded in
    // (and only window roots carry GlobalZIndex anyway, so a per-element z
    // compare isn't even possible). Iterates ALL rendered elements, not just
    // movable roots: an opaque child sprite (a container item, a paperdoll body
    // or equipment overlay) sitting over a window's transparent interior is the
    // real hit, instead of falling through to a window drawn behind it.
    // UiCustom is OPTIONAL: a plain text label (server-gump title, a Label
    // widget) or a solid-colour panel renders and gets a ComputedNode but
    // carries no UiCustom. Those ARE part of their window and must be hit —
    // treated as a solid bounding box, exactly like PixelHit's null-custom path.
    // But a bare LAYOUT node (a flex wrapper like a container's `content`, a
    // server gump's body wrapper) also gets a ComputedNode and paints NOTHING —
    // it must NOT be a hit, or the whole window becomes draggable / closable over
    // its transparent areas, defeating the bg sprite's pixel-perfect mask. So a
    // null-custom element only counts when it actually paints: it has Text
    // (glyphs) or BackgroundColor (solid rect). Text/BackgroundColor are OPTIONAL
    // in the query purely to make that distinction.
    public static UiHit Topmost(
        Vector2 pos,
        AssetsServer assets,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered)
        => Topmost(pos, assets, rendered, null, null);

    // `parents` (optional) enables overflow-clip: an element inside an ancestor
    // with Overflow.Scroll/Clip is only hittable within that ancestor's box —
    // matching the scissor the renderer applies. Without it a scrollable text
    // run's full-content bbox (taller than its viewport) would be grab-able past
    // the window edge. Pass it from gesture systems (drag/pickup/close); the
    // tooltip path may skip it (overflow text has no serial → no tooltip anyway).
    // `bounds` (optional) is the set of UiContainsByBounds elements — those are
    // hit anywhere inside their box (legacy ContainsByBounds), skipping the
    // pixel-perfect alpha mask. Pass it from the gesture systems (drag / close /
    // claim) so a window whose interior is transparent is still grab-able over
    // its gaps; leave it null elsewhere (pickup / tooltip stay pixel-perfect).
    public static UiHit Topmost(
        Vector2 pos,
        AssetsServer assets,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<UiContainsByBounds>> bounds = null)
    {
        var hit = UiHit.None;
        foreach (var (ent, computed, node, custom, bg, text) in rendered)
        {
            if (node.Ref.Display == Display.None) continue;
            bool boundsOnly = bounds != null && bounds.Contains(ent.Ref);
            // Bare layout node (a container's `content` flex wrapper, a text
            // scroll wrapper): no UiCustom surface, no text glyphs, no fill — it
            // paints nothing, so it's NOT a hit. Skipping it lets the gesture
            // reach whatever actually paints behind it (the bg sprite's mask),
            // instead of the whole window capturing clicks over its transparent
            // areas. A UiCustom present but with null Data is still an intentional
            // surface (None-kind drag frame) — keyed on the component, not Data.
            if (!boundsOnly && !custom.IsValid() && !bg.IsValid() && !text.IsValid()) continue;
            var render = custom.IsValid() ? custom.Ref.Render() : null;
            var bb = computed.Ref;
            if (!UiHitTest.PixelHit(assets, render, bb, pos, boundsOnly)) continue;
            if (parents != null && ClippedOutByAncestor(ent.Ref, pos, rendered, parents)) continue;
            if (bb.PaintOrder >= hit.PaintOrder)
                hit = new UiHit(ent.Ref, bb.PaintOrder);
        }
        return hit;
    }

    // True when `pos` falls outside the box of any Overflow.Scroll/Clip ancestor
    // of `entity` — i.e. the element is clipped away there and shouldn't be hit.
    private static bool ClippedOutByAncestor(
        ulong entity,
        Vector2 pos,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents)
    {
        ulong cur = entity;
        for (int i = 0; i < 32 && parents.Contains(cur); i++)
        {
            var (_, p) = parents.Get(cur);
            cur = (ulong)p.Ref.Id;
            if (cur == 0 || !rendered.Contains(cur)) continue;
            var (_, comp, node, _, _, _) = rendered.Get(cur);
            if (node.Ref.Overflow == Overflow.Visible) continue;
            var b = comp.Ref;
            if (pos.X < b.Position.X || pos.Y < b.Position.Y
                || pos.X >= b.Position.X + b.Size.X || pos.Y >= b.Position.Y + b.Size.Y)
                return true;
        }
        return false;
    }

    // Walk the Parent chain from `entity` up to the nearest UiMovable window root
    // (0 if none). The topmost hit is usually a child — an item, body, button —
    // of the window the gesture actually targets. Depth-capped against a cyclic
    // or malformed parent link.
    public static ulong MovableRoot(
        ulong entity,
        Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> movables,
        Query<Data<TinyEcs.Parent>> parents)
    {
        ulong cur = entity;
        for (int i = 0; i < 32 && cur != 0 && !movables.Contains(cur); i++)
        {
            if (!parents.Contains(cur)) return 0;
            var (_, parent) = parents.Get(cur);
            cur = (ulong)parent.Ref.Id;
        }
        return cur != 0 && movables.Contains(cur) ? cur : 0;
    }
}
