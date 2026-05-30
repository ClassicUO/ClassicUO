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
    // widget) renders and gets a ComputedNode but carries no UiCustom. Those
    // are part of their window and must be hit — treated as a solid bounding
    // box, exactly like PixelHit's null-custom path. Skipping them let a click
    // on a window's title text fall through to the window drawn behind it,
    // which the drag system then raised.
    public static UiHit Topmost(
        Vector2 pos,
        AssetsServer assets,
        Query<Data<ComputedNode, Node, UiCustom>, Filter<Optional<UiCustom>>> rendered)
    {
        var hit = UiHit.None;
        foreach (var (ent, computed, node, custom) in rendered)
        {
            if (node.Ref.Display == Display.None) continue;
            var bb = computed.Ref;
            var render = custom.IsValid() ? custom.Ref.Render() : null;
            if (!UiHitTest.PixelHit(assets, render, bb, pos)) continue;
            if (bb.PaintOrder >= hit.PaintOrder)
                hit = new UiHit(ent.Ref, bb.PaintOrder);
        }
        return hit;
    }

    // Walk the Parent chain from `entity` up to the nearest UIMovable window root
    // (0 if none). The topmost hit is usually a child — an item, body, button —
    // of the window the gesture actually targets. Depth-capped against a cyclic
    // or malformed parent link.
    public static ulong MovableRoot(
        ulong entity,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movables,
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
