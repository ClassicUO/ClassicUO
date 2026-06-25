// Aura circles — ECS port of legacy Game/Managers/AuraManager.cs (+ Aura) and
// the MobileView / GameCursor draw calls.
//
//   * Feet aura (AuraUnderFeetType): a soft notoriety-hued circle under EVERY
//     mobile. Mode 0 off, 1 only in warmode, 2 while Ctrl+Shift held, 3 always.
//     Party members use PartyAuraHue when PartyAura is on (legacy MobileView).
//     Drawn INSIDE WorldRenderingPlugin.RenderBodies at the body depth minus a
//     bias, so the world depth buffer sorts it UNDER the mobile (DrawCircle).
//   * Mouse aura (AuraOnMouse): a circle under the cursor while a target cursor
//     is up, hued by the target type. A cursor effect → drawn on top in the GUI
//     overlay pass (RenderMouse), like legacy GameCursor.
//
// The radial gradient texture is generated once from the batcher's device.

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Ecs;

internal static class AuraOverlay
{
    private const int Radius = 40;
    private static Texture2D _texture;

    // Legacy Aura._blend: straight (non-premultiplied) source-alpha so the
    // gradient blends as a soft glow.
    private static readonly BlendState s_blend = new()
    {
        ColorSourceBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.SourceAlpha,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaDestinationBlend = Blend.InverseSourceAlpha,
    };

    // Is the feet aura on this frame? (warmode/ctrl+shift resolved by the caller.)
    public static bool FeetEnabled(Profile profile, bool warmode, bool ctrlShift) => profile.AuraUnderFeetType switch
    {
        1 => warmode,
        2 => ctrlShift,
        3 => true,
        _ => false,
    };

    // Feet-aura hue: party hue for party members (when enabled), else notoriety.
    public static ushort FeetHue(Profile profile, NotorietyFlag noto, bool inParty)
        => profile.PartyAura && inParty ? profile.PartyAuraHue : NotorietyHelper.NotorietyHue(profile, noto);

    // Draw the circle centred on (x, y). Depth is irrelevant: the feet pass runs
    // with the depth buffer OFF (it's a ground decal painted after statics, before
    // bodies — bodies then paint over it, so it reads as "under the feet"), and
    // the mouse overlay has no depth buffer either. Flips the blend like legacy
    // and restores the batch default.
    public static void DrawCircle(UltimaBatcher2D batch, int x, int y, ushort hue)
    {
        EnsureTexture(batch.GraphicsDevice);
        x -= Radius;
        y -= Radius;
        var hueVec = ShaderHueTranslator.GetHueVector(hue, false, 1f);
        batch.SetBlendState(s_blend);
        batch.Draw(_texture, new Rectangle(x, y, Radius * 2, Radius * 2), new Rectangle(0, 0, Radius * 2, Radius * 2), hueVec, 0f);
        batch.SetBlendState(null);
    }

    // Cursor aura while targeting (GUI overlay pass, on top of the world).
    public static void RenderMouse(UltimaBatcher2D batch, Profile profile, TargetingState targeting, Vector2 mousePos)
    {
        if (!profile.AuraOnMouse || !targeting.IsTargeting)
            return;

        // Legacy GameCursor: neutral 0x03b2, harmful 0x0023, beneficial 0x005A.
        ushort hue = targeting.CursorType switch
        {
            1 => 0x0023,
            2 => 0x005A,
            _ => 0x03b2,
        };
        DrawCircle(batch, (int)mousePos.X, (int)mousePos.Y, hue);
    }

    private static void EnsureTexture(GraphicsDevice device)
    {
        if (_texture != null && !_texture.IsDisposed)
            return;

        int w = Radius * 2;
        var data = new Color[w * w];
        for (int y = 0; y < w; y++)
            for (int x = 0; x < w; x++)
            {
                int dist = (int)Math.Sqrt(Math.Pow(x - Radius, 2) + Math.Pow(y - Radius, 2));
                data[x + y * w] = dist > Radius ? Color.Transparent : Color.White * (1f - dist / (float)Radius);
            }

        _texture = new Texture2D(device, w, w, false, SurfaceFormat.Color);
        _texture.SetData(data);
    }

}
