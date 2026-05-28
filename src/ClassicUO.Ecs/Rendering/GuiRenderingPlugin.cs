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

    // Persistent UI render target. Mirrors main's RenderTargets.UiRenderTarget:
    // sized at backbuffer/DPI (logical 640x480 at 800x600 / 1.25), repopulated
    // each frame, then blitted to the backbuffer with LinearClamp at fractional
    // DPI so neighbouring sprites don't seam — sampling happens ONCE per RT
    // pixel during the blit instead of per sprite during layout.
    private sealed class UiRtState
    {
        public RenderTarget2D? Rt;
        public int Width;
        public int Height;
    }

    public void Build(App app)
    {
        // Run in Bevy.UI's render stage so UiRenderCommands is guaranteed populated
        // (RenderSystem.Publish runs first in this stage by declaration order).
        Action<
            Local<DumbTexture>,
            Local<UiRtState>,
            Res<UltimaBatcher2D>,
            Res<AssetsServer>,
            Res<ImageCache>,
            Res<UoGame>,
            Res<UiRenderCommands>> renderFn = Render;

        app.AddSystem(renderFn)
            .InStage(UiPlugin.UiRenderStage)
            .SingleThreaded()
            .Label("cuo:gui_rendering")
            .Build();
    }

    private static void Render(
        Local<DumbTexture> dumbTexture,
        Local<UiRtState> uiRt,
        Res<UltimaBatcher2D> batcher,
        Res<AssetsServer> assets,
        Res<ImageCache> imageCache,
        Res<UoGame> game,
        Res<UiRenderCommands> renderCommands)
    {
        dumbTexture.Value.Texture ??= MakeWhitePixel(batcher.Value.GraphicsDevice);

        var b = batcher.Value;
        var device = b.GraphicsDevice;
        var pp = device.PresentationParameters;
        var dpi = game.Value.DpiScale;
        if (dpi <= 0f) dpi = 1f;

        // Allocate / resize the off-screen UI render target at LOGICAL size.
        // All UI sprites draw into it at 1:1 (no scaling), so adjacent tiles
        // share exact pixel boundaries — no seams.
        var logicalW = Math.Max(1, (int)(pp.BackBufferWidth / dpi));
        var logicalH = Math.Max(1, (int)(pp.BackBufferHeight / dpi));
        if (uiRt.Value.Rt == null || uiRt.Value.Rt.IsDisposed
            || uiRt.Value.Width != logicalW || uiRt.Value.Height != logicalH)
        {
            uiRt.Value.Rt?.Dispose();
            uiRt.Value.Rt = new RenderTarget2D(
                device, logicalW, logicalH, false,
                pp.BackBufferFormat, pp.DepthStencilFormat,
                pp.MultiSampleCount, RenderTargetUsage.DiscardContents);
            uiRt.Value.Width = logicalW;
            uiRt.Value.Height = logicalH;
        }

        // Phase 1: render UI into the RT at logical pixels with PointClamp.
        device.SetRenderTarget(uiRt.Value.Rt);
        device.Clear(XnaColor.Transparent);

        b.Begin(null, Matrix.Identity);
        b.SetSampler(SamplerState.PointClamp);

        var cmds = renderCommands.Value.Span;

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
                    // The UOCustomRender instance rides directly in the command
                    // (UiCustom.Data -> CustomData). It's a reference, so the
                    // post-layout button system's graphic/hue changes are
                    // already reflected here — no entity lookup, no side map.
                    if (cmd.Custom.CustomData is UOCustomRender custom)
                        DrawCustom(b, assets.Value, in cmd, custom);
                    break;
                }

                case RenderCommandType.ScissorStart:
                    // RT is at logical size; scissor uses logical coords too.
                    b.ClipBegin(
                        (int)bb.X,
                        (int)bb.Y,
                        (int)bb.Width,
                        (int)bb.Height);
                    break;

                case RenderCommandType.ScissorEnd:
                    b.ClipEnd();
                    break;

                // Border/Shadow not yet implemented for UO renderer.
            }
        }

        b.End();

        // Phase 2: blit RT to the backbuffer scaled to physical size.
        // LinearClamp at fractional DPI smooths the single upscale step
        // without the per-sprite seam artifacts that PointClamp + scaled
        // sprite draws produced.
        device.SetRenderTarget(null);
        b.Begin();
        b.SetSampler(dpi == Math.Floor(dpi)
            ? SamplerState.PointClamp
            : SamplerState.LinearClamp);
        b.Draw(
            uiRt.Value.Rt,
            new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight),
            Vector3.UnitZ);
        b.SetSampler(null);
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

        // UO bitmap font rendering via UoFontRenderer. Bevy.UI's Text
        // component encodes the chosen UO font in TextFont.FontId; FontSize
        // is ignored (UO bitmap fonts are fixed-size per font index). The
        // ClayColor on TextColor becomes the per-draw tint applied to the
        // white-baked bitmap.
        var font = UoFontRuntime.ResolveFont(t.FontId);
        UoFontRenderer.Draw(
            b,
            t.Text,
            font,
            ToXnaColor(t.TextColor),
            (int)cmd.BoundingBox.X,
            (int)cmd.BoundingBox.Y,
            (int)cmd.BoundingBox.Width,
            cmd.ZIndex);
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

    private static void DrawCustom(UltimaBatcher2D b, AssetsServer assets, in RenderCommand cmd, UOCustomRender custom)
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
                    if (custom.Stacked)
                    {
                        b.Draw(
                            info.Texture,
                            new Vector2(bb.X + 5, bb.Y + 5),
                            info.UV,
                            custom.Hue,
                            0f,
                            Vector2.Zero,
                            1f,
                            SpriteEffects.None,
                            cmd.ZIndex);
                    }
                }
                break;
            }

            case UOCustomKind.GumpNinePatch:
                DrawGumpNinePatch(b, assets, in cmd, in custom);
                break;

            case UOCustomKind.GumpTiled:
            {
                ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                if (info.Texture != null)
                {
                    b.DrawTiled(
                        info.Texture,
                        new Rectangle((int)bb.X, (int)bb.Y, (int)bb.Width, (int)bb.Height),
                        info.UV,
                        custom.Hue,
                        cmd.ZIndex);
                }
                break;
            }

            case UOCustomKind.Art:
            {
                ref readonly var info = ref assets.Arts.GetArt(custom.AssetId);
                if (info.Texture != null && info.UV.Width > 0 && info.UV.Height > 0)
                {
                    // Size rule for slot-clamped item art:
                    //   * item bounds > slot in either dim → fill slot bounds
                    //     exactly (no aspect preserve; UO item art is roughly
                    //     square so distortion is mild),
                    //   * item bounds ≤ slot in both dims → draw at native
                    //     size, centered.
                    // Replaces aspect-preserve "contain" which shrunk elongated
                    // art so the short dim looked very small in the slot.
                    float artW = info.UV.Width;
                    float artH = info.UV.Height;
                    float boundW = bb.Width  > 0 ? bb.Width  : artW;
                    float boundH = bb.Height > 0 ? bb.Height : artH;
                    float destW, destH;
                    if (artW > boundW || artH > boundH)
                    {
                        destW = boundW;
                        destH = boundH;
                    }
                    else
                    {
                        destW = artW;
                        destH = artH;
                    }
                    var destRect = new Rectangle(
                        (int)(bb.X + (boundW - destW) * 0.5f),
                        (int)(bb.Y + (boundH - destH) * 0.5f),
                        (int)destW,
                        (int)destH);
                    b.Draw(
                        info.Texture,
                        destRect,
                        info.UV,
                        custom.Hue,
                        0f,
                        Vector2.Zero,
                        SpriteEffects.None,
                        cmd.ZIndex);
                    if (custom.Stacked)
                    {
                        b.Draw(
                            info.Texture,
                            new Vector2(bb.X + 5, bb.Y + 5),
                            info.UV,
                            custom.Hue,
                            0f,
                            Vector2.Zero,
                            1f,
                            SpriteEffects.None,
                            cmd.ZIndex);
                    }
                }
                break;
            }

            case UOCustomKind.None:
                // Invisible hit/drag surface — draws nothing on purpose.
                break;
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

        // Snap bounding box to integers up-front so every piece (corner draws
        // + tiled fills) shares the exact same pixel grid. Mixing float corner
        // placements with int tiled rects left sub-pixel seams that flashed
        // as gaps after the SpriteBatch CreateScale(dpi) magnified them.
        int x = (int)bb.X;
        int y = (int)bb.Y;
        int w = (int)bb.Width;
        int h = (int)bb.Height;

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
            b.Draw(g0.Texture, new Vector2(x, y), g0.UV, hue, z);

        if (g1.Texture != null)
            b.DrawTiled(g1.Texture,
                new Rectangle(
                    x + g0.UV.Width,
                    y,
                    w - g0.UV.Width - g2.UV.Width,
                    g1.UV.Height),
                g1.UV, hue, z);

        if (g2.Texture != null)
            b.Draw(g2.Texture,
                new Vector2(x + (w - g2.UV.Width), y + offsetTop),
                g2.UV, hue, z);

        if (g3.Texture != null)
            b.DrawTiled(g3.Texture,
                new Rectangle(
                    x,
                    y + g0.UV.Height,
                    g3.UV.Width,
                    h - g0.UV.Height - g5.UV.Height),
                g3.UV, hue, z);

        if (g4.Texture != null)
            b.DrawTiled(g4.Texture,
                new Rectangle(
                    x + (w - g4.UV.Width),
                    y + g2.UV.Height,
                    g4.UV.Width,
                    h - g2.UV.Height - g7.UV.Height),
                g4.UV, hue, z);

        if (g5.Texture != null)
            b.Draw(g5.Texture,
                new Vector2(x, y + (h - g5.UV.Height)),
                g5.UV, hue, z);

        if (g6.Texture != null)
            b.DrawTiled(g6.Texture,
                new Rectangle(
                    x + g5.UV.Width,
                    y + (h - g6.UV.Height - offsetBottom),
                    w - g5.UV.Width - g7.UV.Width,
                    g6.UV.Height),
                g6.UV, hue, z);

        if (g7.Texture != null)
            b.Draw(g7.Texture,
                new Vector2(x + (w - g7.UV.Width), y + (h - g7.UV.Height)),
                g7.UV, hue, z);

        if (g8.Texture != null)
            b.DrawTiled(g8.Texture,
                new Rectangle(
                    x + g0.UV.Width,
                    y + g0.UV.Height,
                    (w - g0.UV.Width - g2.UV.Width) + (offsetLeft + offsetRight),
                    h - g2.UV.Height - g7.UV.Height),
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
