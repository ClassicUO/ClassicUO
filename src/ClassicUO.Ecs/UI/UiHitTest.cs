using System;
using Microsoft.Xna.Framework;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal static class UiHitTest
{
    // True when pos lands on an OPAQUE pixel of the entity's UO sprite.
    // Mirrors main's Gumps/Arts PixelCheck: a click inside the bounding box
    // but over a fully-transparent pixel misses the sprite and passes through
    // to whatever is behind. Kinds without a pixel mask (tiled / nine-patch /
    // none) stay bbox-opaque.
    public static bool PixelHit(AssetsServer assets, in UOCustomRender custom, in ComputedNode bb, Vector2 pos)
    {
        // Bounding-box reject first.
        if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) return false;
        if (pos.X >= bb.Position.X + bb.Size.X) return false;
        if (pos.Y >= bb.Position.Y + bb.Size.Y) return false;

        switch (custom.Kind)
        {
            case UOCustomKind.Gump:
            case UOCustomKind.GumpNinePatch:
            {
                ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                if (info.Texture == null || info.UV.Width <= 0 || info.UV.Height <= 0)
                    return true;
                // Proportional map from box to native mask. Plain Gump draws at
                // native size (scale 1); NinePatch stretches to the box, so the
                // box/native ratio maps the cursor back into the source sprite.
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
                if (pos.X < ox || pos.Y < oy || pos.X >= ox + destW || pos.Y >= oy + destH)
                    return false;
                int lx = (int)((pos.X - ox) * scaleX);
                int ly = (int)((pos.Y - oy) * scaleY);
                return assets.Arts.PixelCheck(custom.AssetId, lx, ly);
            }
            default:
                // GumpTiled / GumpNinePatch / None: solid fill within bounds.
                return true;
        }
    }
}
