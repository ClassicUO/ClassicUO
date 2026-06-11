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
            Res<UiRenderCommands>,
            Res<State<GameState>>,
            Res<NetworkEntitiesMap>,
            Res<Time>,
            Res<GameContext>,
            Res<Camera>,
            Res<RenderTarget2D>,
            Res<NameplateState>,
            WorldOverlayParams> renderFn = Render;

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
        Res<UiRenderCommands> renderCommands,
        Res<State<GameState>> gameState,
        Res<NetworkEntitiesMap> networkEntities,
        Res<Time> time,
        Res<GameContext> gameCtx,
        Res<Camera> camera,
        Res<RenderTarget2D> worldRt,
        Res<NameplateState> nameplates,
        WorldOverlayParams overlay)
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
                    // Overhead world text is drawn right after the game-window
                    // image (the world RT) so it sits ON the world but UNDER any
                    // gump windows, which paint later in this loop. UoFontRenderer
                    // only renders into this UI RT. Scissor to the game window so
                    // overheads never bleed outside it (clamping keeps them
                    // readable; the scissor catches anything that still pokes out).
                    if (gameState.Value.Current == GameState.GameScreen
                        && ReferenceEquals(cmd.Image.ImageData, worldRt.Value))
                    {
                        var camBounds = camera.Value.Bounds;
                        b.ClipBegin(camBounds.X, camBounds.Y, camBounds.Width, camBounds.Height);
                        // HP overheads first: speech (and everything painted
                        // after) sits on top of the bars/labels like legacy.
                        MobileHpOverheads.Render(b, assets.Value, overlay.Profile.Value,
                            gameCtx.Value, camera.Value, overlay.Mobiles, nameplates.Value.PlateTops);
                        overlay.Overhead.Value.Update(time.Value, networkEntities.Value);
                        overlay.Overhead.Value.Render(networkEntities.Value, b, gameCtx.Value, camera.Value, overlay.OverheadAnchors, nameplates.Value.PlateTops);
                        b.ClipEnd();
                    }
                    break;

                case RenderCommandType.Custom:
                {
                    // The UOCustomRender instance rides directly in the command
                    // (UiCustom.Data -> CustomData). It's a reference, so the
                    // post-layout button system's graphic/hue changes are
                    // already reflected here — no entity lookup, no side map.
                    if (cmd.Custom.CustomData is UOCustomRender custom)
                        DrawCustom(b, assets.Value, dumbTexture.Value.Texture!, in cmd, custom);
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

                case RenderCommandType.Border:
                    DrawBorder(b, dumbTexture.Value.Texture!, in cmd);
                    break;

                // Shadow not yet implemented for UO renderer.
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

    // Inset border: four edge quads of the white pixel tinted by the border
    // color. Clay emits Border framing an element (and the pointer-highlight
    // debug overlay). Each side honors its own width.
    private static void DrawBorder(UltimaBatcher2D b, Texture2D white, in RenderCommand cmd)
    {
        ref readonly var bb = ref cmd.BoundingBox;
        ref readonly var border = ref cmd.Border;
        var color = ToXnaColor(border.Color);

        int x = (int)bb.X, y = (int)bb.Y, w = (int)bb.Width, h = (int)bb.Height;
        int l = border.Width.Left, r = border.Width.Right, t = border.Width.Top, bo = border.Width.Bottom;

        if (t > 0) DrawQuad(b, white, x, y, w, t, color, cmd.ZIndex);
        if (bo > 0) DrawQuad(b, white, x, y + h - bo, w, bo, color, cmd.ZIndex);
        if (l > 0) DrawQuad(b, white, x, y, l, h, color, cmd.ZIndex);
        if (r > 0) DrawQuad(b, white, x + w - r, y, r, h, color, cmd.ZIndex);
    }

    private static void DrawQuad(UltimaBatcher2D b, Texture2D white, int x, int y, int w, int h, XnaColor color, int z)
    {
        b.Draw(white, new Vector2(x, y), new Rectangle(0, 0, w, h), color, 0f, Vector2.One, z);
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
        UoFontRenderer.Draw(
            b,
            t.Text,
            t.FontId,
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

    private static void DrawCustom(UltimaBatcher2D b, AssetsServer assets, Texture2D white, in RenderCommand cmd, UOCustomRender custom)
    {
        ref readonly var bb = ref cmd.BoundingBox;

        switch (custom.Kind)
        {
            case UOCustomKind.HueGrid:
            {
                // Dye palette: blit one solid swatch per cell from the baked
                // colors (the UO hue shader can't tint a plain white pixel, so
                // ColorPickerPlugin pre-resolves each hue to RGBA), then a 2px
                // white dot over the selected cell (mirrors legacy ColorPickerBox).
                var colors = custom.GridColors;
                if (colors == null || custom.GridRows <= 0 || custom.GridCols <= 0)
                    break;
                int rows = custom.GridRows, cols = custom.GridCols;
                int cw = custom.CellW, ch = custom.CellH;
                int ox = (int)bb.X, oy = (int)bb.Y;
                for (int i = 0; i < colors.Length && i < rows * cols; i++)
                {
                    int r = i / cols, c = i % cols;
                    uint p = colors[i];
                    var col = new XnaColor((byte)(p & 0xFF), (byte)((p >> 8) & 0xFF), (byte)((p >> 16) & 0xFF), (byte)((p >> 24) & 0xFF));
                    DrawQuad(b, white, ox + c * cw, oy + r * ch, cw, ch, col, cmd.ZIndex);
                }
                int si = custom.SelectedIndex;
                if (si >= 0 && si < rows * cols)
                {
                    int sc = si % cols, sr = si / cols;
                    int dx = ox + (int)(cw * (sc + 0.5f)) - 1;
                    int dy = oy + (int)(ch * (sr + 0.5f)) - 1;
                    DrawQuad(b, white, dx, dy, 2, 2, XnaColor.White, cmd.ZIndex);
                }
                break;
            }
            case UOCustomKind.Gump:
            {
                ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                if (info.Texture != null)
                {
                    // Snap to integer logical pixels. The RT is point-sampled at
                    // logical size and text / rect / image / ninepatch siblings
                    // all floor; drawing this sprite at fractional coords made it
                    // drift up to 1px against them while a gump window is dragged
                    // (Left/Top go fractional) — read as a flicker.
                    b.Draw(
                        info.Texture,
                        new Vector2((int)bb.X, (int)bb.Y),
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
                            new Vector2((int)bb.X + 5, (int)bb.Y + 5),
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

            case UOCustomKind.GumpSlice:
            {
                ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                if (info.Texture != null)
                {
                    var src = new Rectangle(info.UV.X + custom.SrcX, info.UV.Y + custom.SrcY, custom.SrcW, custom.SrcH);
                    if (custom.SliceTiled)
                    {
                        b.DrawTiled(
                            info.Texture,
                            new Rectangle((int)bb.X, (int)bb.Y, (int)bb.Width, (int)bb.Height),
                            src,
                            custom.Hue,
                            cmd.ZIndex);
                    }
                    else
                    {
                        b.Draw(
                            info.Texture,
                            new Vector2((int)bb.X, (int)bb.Y),
                            src,
                            custom.Hue,
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
                    // Slot art (paperdoll equipment): crop the source to the art's
                    // real (non-transparent) bounds and centre that region in the
                    // node box — native size when it fits, scaled to the box when
                    // larger. Mirrors OOP PaperdollGump.ItemGumpFixed exactly so a
                    // small icon (e.g. a ring) sits centred, not in the corner.
                    if (custom.ArtRealBounds)
                    {
                        var rb = assets.Arts.GetRealArtBounds(custom.AssetId);
                        if (rb.Width > 0 && rb.Height > 0)
                        {
                            int boxW = bb.Width  > 0 ? (int)bb.Width  : rb.Width;
                            int boxH = bb.Height > 0 ? (int)bb.Height : rb.Height;
                            int dW = rb.Width  < boxW ? rb.Width  : boxW;
                            int dH = rb.Height < boxH ? rb.Height : boxH;
                            int oX = rb.Width  < boxW ? (boxW >> 1) - (dW >> 1) : 0;
                            int oY = rb.Height < boxH ? (boxH >> 1) - (dH >> 1) : 0;
                            b.Draw(
                                info.Texture,
                                new Rectangle((int)bb.X + oX, (int)bb.Y + oY, dW, dH),
                                new Rectangle(info.UV.X + rb.X, info.UV.Y + rb.Y, rb.Width, rb.Height),
                                custom.Hue,
                                0f,
                                Vector2.Zero,
                                SpriteEffects.None,
                                cmd.ZIndex);
                            break;
                        }
                    }

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
                            new Vector2((int)bb.X + 5, (int)bb.Y + 5),
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

            case UOCustomKind.MiniMap:
            {
                // Baked radar texture already contains the bg frame + radar
                // pixels + dots (MiniMapPlugin starts each bake from a copy of
                // the bg gump's pixels). Draw it if present; fall back to the
                // raw bg gump on the first frame before the first bake.
                if (custom.Dynamic != null && !custom.Dynamic.IsDisposed)
                {
                    b.Draw(
                        custom.Dynamic,
                        new Vector2((int)bb.X, (int)bb.Y),
                        new Rectangle(0, 0, custom.Dynamic.Width, custom.Dynamic.Height),
                        custom.Hue,
                        0f, Vector2.Zero, 1f, SpriteEffects.None, cmd.ZIndex);
                }
                else
                {
                    ref readonly var info = ref assets.Gumps.GetGump(custom.AssetId);
                    if (info.Texture != null)
                        b.Draw(info.Texture, new Vector2((int)bb.X, (int)bb.Y), info.UV, custom.Hue,
                            0f, Vector2.Zero, 1f, SpriteEffects.None, cmd.ZIndex);
                }
                break;
            }

            case UOCustomKind.WrappedText:
                // Text-selection highlight behind the glyphs: one rect per visual
                // line (multi-row selections the single overlay node can't draw).
                if (custom.SelEnd > custom.SelStart)
                {
                    var selColor = new XnaColor(70, 110, 180, 120);
                    foreach (var (rx, ry, rw, rh) in UoFontRenderer.SelectionRects(
                        custom.Text, custom.TextFont, custom.WrapWidth, custom.IsHtml,
                        custom.HtmlStartColor, custom.HtmlBg, custom.SelStart, custom.SelEnd))
                        DrawQuad(b, white, (int)(bb.X + rx), (int)(bb.Y + ry), (int)rw, (int)rh, selColor, cmd.ZIndex);
                }
                UoFontRenderer.DrawWrapped(
                    b,
                    custom.Text,
                    custom.TextFont,
                    custom.TextHue,
                    custom.WrapWidth,
                    custom.IsHtml,
                    custom.HtmlStartColor,
                    custom.HtmlBg,
                    (int)bb.X,
                    (int)bb.Y,
                    cmd.ZIndex,
                    custom.TextCenter ? ClassicUO.Assets.TEXT_ALIGN_TYPE.TS_CENTER : ClassicUO.Assets.TEXT_ALIGN_TYPE.TS_LEFT,
                    custom.TextAscii);
                // Caret bar on top of the glyphs (in-content so it scrolls/clips
                // with the text inside a scroll box).
                if (custom.CaretOn)
                    DrawQuad(b, white, (int)(bb.X + custom.CaretX), (int)(bb.Y + custom.CaretY),
                        2, (int)MathF.Max(1, custom.CaretH), new XnaColor(0, 0, 0, 255), cmd.ZIndex);
                break;

            case UOCustomKind.None:
                // Invisible hit/drag surface — draws nothing on purpose.
                break;
            case UOCustomKind.Animation:
            {
                // Mobile animation player: AssetId = body graphic, AnimAction =
                // group, AnimDir = stored direction (0..4), AnimFrame = frame
                // (wrapped). The frame is centered in the node box.
                byte dir = (byte)Math.Clamp((int)custom.AnimDir, 0, 4);
                var frames = assets.Animations.GetAnimationFrames(
                    (ushort)custom.AssetId, custom.AnimAction, dir, out _, out _);
                if (frames.IsEmpty)
                    break;
                ref readonly var frame = ref frames[custom.AnimFrame % frames.Length];
                if (frame.Texture == null)
                    break;
                // Same size rule as Art/Land: fit to the node box when the frame
                // exceeds it (a horse body in a 44px vendor row), else native
                // centered. Legacy ShopItem clamps the same way (min(size, 45)).
                float aw = frame.UV.Width;
                float ah = frame.UV.Height;
                float abw = bb.Width  > 0 ? bb.Width  : aw;
                float abh = bb.Height > 0 ? bb.Height : ah;
                float adw, adh;
                if (aw > abw || ah > abh) { adw = abw; adh = abh; }
                else { adw = aw; adh = ah; }
                b.Draw(
                    frame.Texture,
                    new Rectangle(
                        (int)(bb.X + (abw - adw) * 0.5f),
                        (int)(bb.Y + (abh - adh) * 0.5f),
                        (int)adw,
                        (int)adh),
                    frame.UV,
                    custom.Hue,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    cmd.ZIndex);
                break;
            }

            case UOCustomKind.DynTexture:
            {
                // Plugin-owned texture (treasure-map multimap bake) stretched to
                // the node box + the plotted course polyline over it.
                if (custom.Dynamic == null)
                    break;
                b.Draw(
                    custom.Dynamic,
                    new Rectangle((int)bb.X, (int)bb.Y, (int)bb.Width, (int)bb.Height),
                    custom.Dynamic.Bounds,
                    custom.Hue,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    cmd.ZIndex);
                var pts = custom.Points;
                if (pts != null)
                    for (int i = 0; i + 1 < pts.Count; i++)
                        b.DrawLine(
                            white,
                            new Vector2(bb.X + pts[i].X, bb.Y + pts[i].Y),
                            new Vector2(bb.X + pts[i + 1].X, bb.Y + pts[i + 1].Y),
                            Vector3.UnitZ,
                            1,
                            cmd.ZIndex);
                break;
            }

            case UOCustomKind.WorldMap:
            {
                if (custom.Tag is WorldMapRenderData worldMap)
                    WorldMapGumpPlugin.DrawWorldMap(b, white, bb.X, bb.Y, bb.Width, bb.Height, worldMap, cmd.ZIndex);
                break;
            }

            case UOCustomKind.Land:
            {
                // Land tile (ART block < 0x4000): a 44x44 diamond. Fit to the node
                // box when it exceeds it, else draw native-size centered — same
                // size rule as Art so the asset IDE can frame either kind.
                ref readonly var info = ref assets.Arts.GetLand(custom.AssetId);
                if (info.Texture == null || info.UV.Width <= 0 || info.UV.Height <= 0)
                    break;
                float lw = info.UV.Width;
                float lh = info.UV.Height;
                float lbw = bb.Width  > 0 ? bb.Width  : lw;
                float lbh = bb.Height > 0 ? bb.Height : lh;
                float ldw, ldh;
                if (lw > lbw || lh > lbh) { ldw = lbw; ldh = lbh; }
                else { ldw = lw; ldh = lh; }
                b.Draw(
                    info.Texture,
                    new Rectangle(
                        (int)(bb.X + (lbw - ldw) * 0.5f),
                        (int)(bb.Y + (lbh - ldh) * 0.5f),
                        (int)ldw,
                        (int)ldh),
                    info.UV,
                    custom.Hue,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    cmd.ZIndex);
                break;
            }
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
