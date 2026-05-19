using System;
using ClassicUO.Renderer;
using Clay;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using XnaColor = Microsoft.Xna.Framework.Color;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

/// Translates Bevy.UI's published RenderCommand stream into UltimaBatcher2D
/// draw calls. Reads UiRenderCommands + UiClayContext (for the Clay->entity
/// map) so UOCustomRender markers can be resolved back to the source entity.
internal readonly struct GuiRenderingPlugin : IPlugin
{
    private const FontSystemEffect FONT_EFFECT = FontSystemEffect.Stroked;
    private const int FONT_EFFECT_AMOUNT = 1;

    private sealed class DumbTexture
    {
        public Texture2D? Texture;
    }

    public void Build(App app)
    {
        // Run in Bevy.UI's render stage so UiRenderCommands is guaranteed populated
        // (RenderSystem.Publish runs first in this stage by declaration order).
        Action<
            Local<DumbTexture>,
            Res<UltimaBatcher2D>,
            Res<AssetsServer>,
            Res<ImageCache>,
            Res<UiRenderCommands>,
            Res<UiClayContext>,
            Query<Data<UOCustomRender>>> renderFn = Render;

        app.AddSystem(renderFn)
            .InStage(UiPlugin.UiRenderStage)
            .SingleThreaded()
            .Label("cuo:gui_rendering")
            .Build();
    }

    private static void Render(
        Local<DumbTexture> dumbTexture,
        Res<UltimaBatcher2D> batcher,
        Res<AssetsServer> assets,
        Res<ImageCache> imageCache,
        Res<UiRenderCommands> renderCommands,
        Res<UiClayContext> uiCtx,
        Query<Data<UOCustomRender>> customQuery)
    {
        dumbTexture.Value.Texture ??= MakeWhitePixel(batcher.Value.GraphicsDevice);

        var b = batcher.Value;
        b.Begin();

        var cmds = renderCommands.Value.Span;

        // Bevy.UI keeps the Clay-id -> entity-id map internal. Rebuild a local
        // mapping for our UOCustomRender entities by replaying Clay's
        // ElementId.HashNumber over each candidate entity. Cheap: the number of
        // UO-custom UI entities is small compared to the total render-command
        // list.
        // (clayId, entityId) tuples scanned linearly — under a few dozen items
        // in practice. Swap for a dictionary if this becomes a hotspot.
        Span<(uint clayId, ulong entityId)> idMap = stackalloc (uint, ulong)[256];
        var idCount = 0;
        foreach (var (eid, _) in customQuery)
        {
            if (idCount >= idMap.Length)
                break;
            idMap[idCount++] = (ElementId.HashNumber((uint)eid.Ref).Id, eid.Ref);
        }

        foreach (ref readonly var cmd in cmds)
        {
            ref readonly var bb = ref cmd.BoundingBox;

            switch (cmd.CommandType)
            {
                case RenderCommandType.Text:
                    DrawText(b, in cmd);
                    break;

                case RenderCommandType.Rectangle:
                    DrawRectangle(b, dumbTexture.Value.Texture!, in cmd);
                    break;

                case RenderCommandType.Image:
                    DrawImage(b, imageCache.Value, in cmd);
                    break;

                case RenderCommandType.Custom:
                {
                    ulong entityId = 0;
                    for (var i = 0; i < idCount; i++)
                    {
                        if (idMap[i].clayId == cmd.Id)
                        {
                            entityId = idMap[i].entityId;
                            break;
                        }
                    }
                    if (entityId != 0 && customQuery.Contains(entityId))
                    {
                        var (_, customPtr) = customQuery.Get(entityId);
                        DrawCustom(b, assets.Value, in cmd, in customPtr.Ref);
                    }
                    break;
                }

                case RenderCommandType.ScissorStart:
                    b.ClipBegin((int)bb.X, (int)bb.Y, (int)bb.Width, (int)bb.Height);
                    break;

                case RenderCommandType.ScissorEnd:
                    b.ClipEnd();
                    break;

                // Border/Shadow not yet implemented for UO renderer.
            }
        }

        b.End();
    }

    private static Texture2D MakeWhitePixel(GraphicsDevice device)
    {
        var t = new Texture2D(device, 1, 1);
        t.SetData([XnaColor.White]);
        return t;
    }

    private static void DrawText(UltimaBatcher2D b, in RenderCommand cmd)
    {
        ref readonly var t = ref cmd.Text;
        if (string.IsNullOrEmpty(t.Text))
            return;

        var font = FontCache.GetFont(t.FontId);
        var dynFont = font.GetFont(t.FontSize);
        dynFont.DrawText(
            b,
            t.Text,
            new Vector2(cmd.BoundingBox.X, cmd.BoundingBox.Y),
            ToXnaColor(t.TextColor),
            characterSpacing: t.LetterSpacing,
            lineSpacing: t.LineHeight,
            layerDepth: cmd.ZIndex,
            effect: FONT_EFFECT, effectAmount: FONT_EFFECT_AMOUNT);
    }

    private static void DrawRectangle(UltimaBatcher2D b, Texture2D white, in RenderCommand cmd)
    {
        ref readonly var bb = ref cmd.BoundingBox;
        ref readonly var rect = ref cmd.Rectangle;

        if (rect.CornerRadius.TopLeft > 0)
        {
            b.DrawRoundedRectangleFilled(
                white,
                new Rectangle((int)bb.X, (int)bb.Y, (int)bb.Width, (int)bb.Height),
                rect.CornerRadius.TopLeft,
                ToXnaColor(rect.BackgroundColor),
                cmd.ZIndex);
        }
        else
        {
            b.Draw(
                white,
                new Vector2((int)bb.X, (int)bb.Y),
                new Rectangle(0, 0, (int)bb.Width, (int)bb.Height),
                ToXnaColor(rect.BackgroundColor),
                0f, Vector2.One, cmd.ZIndex);
        }
    }

    private static void DrawImage(UltimaBatcher2D b, ImageCache cache, in RenderCommand cmd)
    {
        ref readonly var img = ref cmd.Image;
        ref readonly var bb = ref cmd.BoundingBox;

        // We allow two flavours of UiImage.ImageData:
        //   - Texture2D directly
        //   - nint handle into ImageCache (kept around for GameScreenPlugin's
        //     render-target trick, which is being migrated)
        Texture2D? tex = img.ImageData as Texture2D;
        if (tex == null && img.ImageData is nint handle && cache.TryGetValue(handle, out var cached))
            tex = cached;

        if (tex == null || tex.IsDisposed)
            return;

        b.Draw(
            tex,
            new Vector2((int)bb.X, (int)bb.Y),
            new Rectangle(0, 0, (int)bb.Width, (int)bb.Height),
            ToXnaColor(img.BackgroundColor),
            0f, Vector2.One, cmd.ZIndex);
    }

    private static void DrawCustom(UltimaBatcher2D b, AssetsServer assets, in RenderCommand cmd, in UOCustomRender custom)
    {
        ref readonly var bb = ref cmd.BoundingBox;

        switch (custom.Kind)
        {
            case UOCustomKind.Gump:
            {
                ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                if (info.Texture != null)
                {
                    b.Draw(
                        info.Texture,
                        new Vector2(bb.X, bb.Y),
                        info.UV,
                        custom.Hue,
                        0f,
                        Vector2.Zero,
                        1f,
                        SpriteEffects.None,
                        cmd.ZIndex);
                }
                break;
            }

            case UOCustomKind.GumpNinePatch:
                DrawGumpNinePatch(b, assets, in cmd, in custom);
                break;

            case UOCustomKind.Art:
            {
                ref readonly var info = ref assets.Arts.GetArt(custom.AssetId);
                if (info.Texture != null)
                {
                    b.Draw(
                        info.Texture,
                        new Vector2(bb.X, bb.Y),
                        info.UV,
                        custom.Hue,
                        0f,
                        Vector2.Zero,
                        1f,
                        SpriteEffects.None,
                        cmd.ZIndex);
                }
                break;
            }

            case UOCustomKind.Land:
            case UOCustomKind.Animation:
                // Not yet implemented.
                break;
        }
    }

    private static void DrawGumpNinePatch(UltimaBatcher2D b, AssetsServer assets, in RenderCommand cmd, in UOCustomRender custom)
    {
        ref readonly var bb = ref cmd.BoundingBox;
        var id = custom.AssetId;
        var hue = custom.Hue;
        var z = cmd.ZIndex;

        // 9-patch layout (matches the previous implementation):
        //   0 1 2
        //   3 8 4
        //   5 6 7
        ref readonly var g0 = ref assets.Gumps.GetGump(id + 0);
        ref readonly var g1 = ref assets.Gumps.GetGump(id + 1);
        ref readonly var g2 = ref assets.Gumps.GetGump(id + 2);
        ref readonly var g3 = ref assets.Gumps.GetGump(id + 3);
        ref readonly var g4 = ref assets.Gumps.GetGump(id + 4 + 1);
        ref readonly var g5 = ref assets.Gumps.GetGump(id + 5 + 1);
        ref readonly var g6 = ref assets.Gumps.GetGump(id + 6 + 1);
        ref readonly var g7 = ref assets.Gumps.GetGump(id + 7 + 1);
        ref readonly var g8 = ref assets.Gumps.GetGump(id + 4);

        var offsetTop = Math.Max(g0.UV.Height, g2.UV.Height) - g1.UV.Height;
        var offsetBottom = Math.Max(g5.UV.Height, g7.UV.Height) - g6.UV.Height;
        var offsetLeft = Math.Abs(Math.Max(g0.UV.Width, g5.UV.Width) - g2.UV.Width);
        var offsetRight = Math.Max(g2.UV.Width, g7.UV.Width) - g4.UV.Width;

        if (g0.Texture != null)
            b.Draw(g0.Texture, new Vector2(bb.X, bb.Y), g0.UV, hue, z);

        if (g1.Texture != null)
            b.DrawTiled(g1.Texture,
                new Rectangle(
                    (int)bb.X + g0.UV.Width,
                    (int)bb.Y,
                    (int)bb.Width - g0.UV.Width - g2.UV.Width,
                    g1.UV.Height),
                g1.UV, hue, z);

        if (g2.Texture != null)
            b.Draw(g2.Texture,
                new Vector2(bb.X + (bb.Width - g2.UV.Width), bb.Y + offsetTop),
                g2.UV, hue, z);

        if (g3.Texture != null)
            b.DrawTiled(g3.Texture,
                new Rectangle(
                    (int)bb.X,
                    (int)bb.Y + g0.UV.Height,
                    g3.UV.Width,
                    (int)bb.Height - g0.UV.Height - g5.UV.Height),
                g3.UV, hue, z);

        if (g4.Texture != null)
            b.DrawTiled(g4.Texture,
                new Rectangle(
                    (int)bb.X + ((int)bb.Width - g4.UV.Width),
                    (int)bb.Y + g2.UV.Height,
                    g4.UV.Width,
                    (int)bb.Height - g2.UV.Height - g7.UV.Height),
                g4.UV, hue, z);

        if (g5.Texture != null)
            b.Draw(g5.Texture,
                new Vector2(bb.X, bb.Y + (bb.Height - g5.UV.Height)),
                g5.UV, hue, z);

        if (g6.Texture != null)
            b.DrawTiled(g6.Texture,
                new Rectangle(
                    (int)bb.X + g5.UV.Width,
                    (int)bb.Y + ((int)bb.Height - g6.UV.Height - offsetBottom),
                    (int)bb.Width - g5.UV.Width - g7.UV.Width,
                    g6.UV.Height),
                g6.UV, hue, z);

        if (g7.Texture != null)
            b.Draw(g7.Texture,
                new Vector2(bb.X + (bb.Width - g7.UV.Width), bb.Y + (bb.Height - g7.UV.Height)),
                g7.UV, hue, z);

        if (g8.Texture != null)
            b.DrawTiled(g8.Texture,
                new Rectangle(
                    (int)bb.X + g0.UV.Width,
                    (int)bb.Y + g0.UV.Height,
                    ((int)bb.Width - g0.UV.Width - g2.UV.Width) + (offsetLeft + offsetRight),
                    (int)bb.Height - g2.UV.Height - g7.UV.Height),
                g8.UV, hue, z);
    }

    private static XnaColor ToXnaColor(ClayColor c)
    {
        // Clay.Color stores floats in 0..255 range (see Color.cs in Clay.NET).
        return new XnaColor(
            (byte)Math.Clamp(c.R, 0f, 255f),
            (byte)Math.Clamp(c.G, 0f, 255f),
            (byte)Math.Clamp(c.B, 0f, 255f),
            (byte)Math.Clamp(c.A, 0f, 255f));
    }
}
