using System;
using Microsoft.Xna.Framework;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal static class UiHitTest
{
    // Pile offset for stacked items (Amount > 1) — must match the +5/+5 second
    // sprite drawn in GuiRenderingPlugin.
    private const float StackOffset = 5f;

    // True when pos lands on an OPAQUE pixel of the entity's UO sprite.
    // Mirrors main's Gumps/Arts PixelCheck: a click inside the bounding box
    // but over a fully-transparent pixel misses the sprite and passes through
    // to whatever is behind. Kinds without a pixel mask (tiled / nine-patch /
    // none) stay bbox-opaque.
    public static bool PixelHit(AssetsServer assets, UOCustomRender custom, in ComputedNode bb, Vector2 pos)
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
            default:
                // GumpNinePatch (resizepic window bg) / None: solid fill within
                // bounds — a stretched nine-patch has no meaningful per-pixel
                // mask, and a window background should capture clicks anywhere
                // inside it (drag, right-click-close, click-capture).
                return true;
        }
    }
}
