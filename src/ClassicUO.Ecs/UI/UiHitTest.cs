using System;
using Microsoft.Xna.Framework;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal static class UiHitTest
{
    // Pile offset for stacked items (Amount > 1) — must match the +5/+5 second
    // sprite drawn in GuiRenderingPlugin.
    private const float StackOffset = 5f;

    // Plain bounding-box point test. For plugins scanning their OWN elements
    // (top bar handles, popup rows); full picking goes through UiPick.Topmost.
    public static bool Contains(in ComputedNode bb, Vector2 pos)
        => pos.X >= bb.Position.X && pos.Y >= bb.Position.Y
        && pos.X < bb.Position.X + bb.Size.X && pos.Y < bb.Position.Y + bb.Size.Y;

    // True when pos lands on an OPAQUE pixel of the entity's UO sprite.
    // Mirrors main's Gumps/Arts PixelCheck: a click inside the bounding box
    // but over a fully-transparent pixel misses the sprite and passes through
    // to whatever is behind. Kinds without a pixel mask (tiled / nine-patch /
    // none) stay bbox-opaque.
    // boundsOnly: the element opted into legacy ContainsByBounds (UiContainsByBounds) —
    // the whole bounding box is a hit, skipping the per-kind alpha mask, so a click
    // on a transparent interior pixel still lands on it (drag / right-click-close).
    public static bool PixelHit(AssetsServer assets, UOCustomRender custom, in ComputedNode bb, Vector2 pos, bool boundsOnly = false)
    {
        // Stacked items (Amount > 1) draw a second sprite +5/+5 (see
        // GuiRenderingPlugin / ContainerGumpPlugin), so the pile extends 5px
        // past the node box on the right/bottom — widen the reject to match,
        // else clicks on the offset half of the pile fall through.
        float stackExt = (custom is not null && custom.Stacked) ? StackOffset : 0f;

        // Bounding-box reject first.
        if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) return false;
        if (pos.X >= bb.Position.X + bb.Size.X + stackExt) return false;
        if (pos.Y >= bb.Position.Y + bb.Size.Y + stackExt) return false;

        // Opted-in whole-bbox hit (UiContainsByBounds): inside the box is enough.
        if (boundsOnly) return true;

        // No custom payload on the element (shouldn't happen for UO sprites) —
        // treat the whole bounding box as opaque.
        if (custom is null) return true;

        switch (custom.Kind)
        {
            // MiniMap draws its baked texture at native bg-gump size; the bg
            // gump's own alpha mask is the correct hit mask (radar pixels only
            // fill where the frame is opaque), so share the plain Gump path.
            case UOCustomKind.MiniMap:
            case UOCustomKind.Gump:
            {
                ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                if (info.Texture == null || info.UV.Width <= 0 || info.UV.Height <= 0)
                    return true;
                // Plain Gump draws at native size (scale 1): map the cursor
                // straight into the source mask.
                float sx = bb.Size.X / info.UV.Width;
                float sy = bb.Size.Y / info.UV.Height;
                if (sx <= 0f || sy <= 0f) return true;
                int lx = (int)((pos.X - bb.Position.X) / sx);
                int ly = (int)((pos.Y - bb.Position.Y) / sy);
                return assets.Gumps.PixelCheck(custom.AssetId, lx, ly);
            }
            case UOCustomKind.GumpTiled:
            {
                ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                if (info.Texture == null || info.UV.Width <= 0 || info.UV.Height <= 0)
                    return true;
                // Tiled repeats the native sprite across the box (DrawTiled),
                // so wrap the local coord by the native tile size.
                int tx = ((int)(pos.X - bb.Position.X)) % info.UV.Width;
                int ty = ((int)(pos.Y - bb.Position.Y)) % info.UV.Height;
                return assets.Gumps.PixelCheck(custom.AssetId, tx, ty);
            }
            case UOCustomKind.Art:
            {
                ref readonly var info = ref assets.Arts.GetArt(custom.AssetId);
                if (info.Texture == null || info.UV.Width <= 0 || info.UV.Height <= 0)
                    return true;
                // Mirrors GuiRenderingPlugin's slot-art size rule: stretch to
                // bounds when oversized, native + centered otherwise. Two
                // independent per-axis scales since the oversized branch does
                // not preserve aspect.
                float artW = info.UV.Width;
                float artH = info.UV.Height;
                float boundW = bb.Size.X;
                float boundH = bb.Size.Y;
                float destW, destH, scaleX, scaleY;
                if (artW > boundW || artH > boundH)
                {
                    destW = boundW; destH = boundH;
                    scaleX = artW / boundW; scaleY = artH / boundH;
                }
                else
                {
                    destW = artW; destH = artH;
                    scaleX = 1f; scaleY = 1f;
                }
                float ox = bb.Position.X + (boundW - destW) * 0.5f;
                float oy = bb.Position.Y + (boundH - destH) * 0.5f;
                if (pos.X >= ox && pos.Y >= oy && pos.X < ox + destW && pos.Y < oy + destH)
                {
                    int lx = (int)((pos.X - ox) * scaleX);
                    int ly = (int)((pos.Y - oy) * scaleY);
                    if (assets.Arts.PixelCheck(custom.AssetId, lx, ly)) return true;
                }
                // Stacked: the pile's second sprite is drawn at +5/+5 at native
                // size (scale 1), so test it too — the offset half of the pile is
                // only covered by this sprite.
                if (custom.Stacked)
                {
                    float sx2 = bb.Position.X + StackOffset;
                    float sy2 = bb.Position.Y + StackOffset;
                    if (pos.X >= sx2 && pos.Y >= sy2 && pos.X < sx2 + artW && pos.Y < sy2 + artH
                        && assets.Arts.PixelCheck(custom.AssetId, (int)(pos.X - sx2), (int)(pos.Y - sy2)))
                        return true;
                }
                return false;
            }
            case UOCustomKind.GumpNinePatch:
                // resizepic window bg: pixel-perfect across the 9 slices, exactly
                // like legacy ResizePic.Contains — a click on a transparent corner
                // / gap passes through instead of capturing the whole box.
                return NinePatchHit(assets, custom.AssetId, in bb, pos);

            default:
                // None (invisible hit/drag surface) / GumpSlice: solid fill within
                // bounds.
                return true;
        }
    }

    // 9-slice pixel hit mirroring legacy ResizePic.Contains + the ECS ninepatch
    // renderer (GuiRenderingPlugin.DrawGumpNinePatch): corners drawn native,
    // edges/centre tiled. Graphic order: 0..3 = TL/top/TR/left, +4 = centre,
    // +5..+8 = right/BL/bottom/BR.
    private static bool NinePatchHit(AssetsServer assets, uint id, in ComputedNode bb, Vector2 pos)
    {
        int x = (int)bb.Position.X, y = (int)bb.Position.Y;
        int w = (int)bb.Size.X, h = (int)bb.Size.Y;

        ref readonly var g0 = ref assets.Gumps.GetGump(id + 0); // top-left
        ref readonly var g1 = ref assets.Gumps.GetGump(id + 1); // top
        ref readonly var g2 = ref assets.Gumps.GetGump(id + 2); // top-right
        ref readonly var g3 = ref assets.Gumps.GetGump(id + 3); // left
        ref readonly var g4 = ref assets.Gumps.GetGump(id + 5); // right
        ref readonly var g5 = ref assets.Gumps.GetGump(id + 6); // bottom-left
        ref readonly var g6 = ref assets.Gumps.GetGump(id + 7); // bottom
        ref readonly var g7 = ref assets.Gumps.GetGump(id + 8); // bottom-right
        ref readonly var g8 = ref assets.Gumps.GetGump(id + 4); // centre

        int offsetTop = Math.Max(g0.UV.Height, g2.UV.Height) - g1.UV.Height;
        int offsetBottom = Math.Max(g5.UV.Height, g7.UV.Height) - g6.UV.Height;
        int offsetLeft = Math.Abs(Math.Max(g0.UV.Width, g5.UV.Width) - g2.UV.Width);
        int offsetRight = Math.Max(g2.UV.Width, g7.UV.Width) - g4.UV.Width;

        if (HitNative(assets, id + 0, g0.UV, x, y, pos)) return true;
        if (HitNative(assets, id + 2, g2.UV, x + (w - g2.UV.Width), y + offsetTop, pos)) return true;
        if (HitNative(assets, id + 6, g5.UV, x, y + (h - g5.UV.Height), pos)) return true;
        if (HitNative(assets, id + 8, g7.UV, x + (w - g7.UV.Width), y + (h - g7.UV.Height), pos)) return true;

        int dw = w - g0.UV.Width - g2.UV.Width;
        if (dw >= 1 && HitTiled(assets, id + 1, g1.UV, x + g0.UV.Width, y, dw, g1.UV.Height, pos)) return true;

        int dh = h - g0.UV.Height - g5.UV.Height;
        if (dh >= 1 && HitTiled(assets, id + 3, g3.UV, x, y + g0.UV.Height, g3.UV.Width, dh, pos)) return true;

        dh = h - g2.UV.Height - g7.UV.Height;
        if (dh >= 1 && HitTiled(assets, id + 5, g4.UV, x + (w - g4.UV.Width), y + g2.UV.Height, g4.UV.Width, dh, pos)) return true;

        dw = w - g5.UV.Width - g7.UV.Width;
        if (dw >= 1 && HitTiled(assets, id + 7, g6.UV, x + g5.UV.Width, y + (h - g6.UV.Height - offsetBottom), dw, g6.UV.Height, pos)) return true;

        dw = (w - g0.UV.Width - g2.UV.Width) + (offsetLeft + offsetRight);
        dh = h - g2.UV.Height - g7.UV.Height;
        if (dw >= 1 && dh >= 1 && HitTiled(assets, id + 4, g8.UV, x + g0.UV.Width, y + g0.UV.Height, dw, dh, pos)) return true;

        return false;
    }

    // Corner piece drawn at native size: direct mask lookup at the local coord.
    private static bool HitNative(AssetsServer assets, uint id, Rectangle uv, int dx, int dy, Vector2 pos)
    {
        if (uv.Width <= 0 || uv.Height <= 0) return false;
        int lx = (int)pos.X - dx, ly = (int)pos.Y - dy;
        if (lx < 0 || ly < 0 || lx >= uv.Width || ly >= uv.Height) return false;
        return assets.Gumps.PixelCheck(id, lx, ly);
    }

    // Edge / centre piece tiled across (dw,dh): wrap the local coord by the tile.
    private static bool HitTiled(AssetsServer assets, uint id, Rectangle uv, int dx, int dy, int dw, int dh, Vector2 pos)
    {
        if (uv.Width <= 0 || uv.Height <= 0) return false;
        int lx = (int)pos.X - dx, ly = (int)pos.Y - dy;
        if (lx < 0 || ly < 0 || lx >= dw || ly >= dh) return false;
        return assets.Gumps.PixelCheck(id, lx % uv.Width, ly % uv.Height);
    }
}
