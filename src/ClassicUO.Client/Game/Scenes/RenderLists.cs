// SPDX-License-Identifier: BSD-2-Clause
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Disambiguate: ClassicUO ships its own SpriteFont in ClassicUO.Renderer
// alongside MonoGame's Microsoft.Xna.Framework.Graphics.SpriteFont. The
// UltimaBatcher2D.DrawString overloads take the ClassicUO one (see Fonts.cs),
// so that's what StringFont commands store.
using SpriteFont = ClassicUO.Renderer.SpriteFont;

namespace ClassicUO.Game.Scenes
{
    /// <summary>
    /// Discriminator for <see cref="GumpDrawCommand"/> entries in the gump
    /// command stream. Each kind drives a different branch of the flush loop.
    /// </summary>
    internal enum GumpCommandKind : byte
    {
        /// <summary>Typed text draw: renders a <see cref="RenderedText"/> at (X, Y).</summary>
        Text,

        /// <summary>
        /// Typed scrolled-text draw: renders a <see cref="RenderedText"/> using its
        /// scroll-window <see cref="RenderedText.Draw"/> overload. Source stores the
        /// scroll rectangle (X = scrollX, Y = scrollY, Width = windowW, Height = windowH).
        /// Used by HtmlControl, StbTextBox, and the login textbox.
        /// </summary>
        TextScrolled,

        /// <summary>
        /// Typed clipped-text draw: renders a <see cref="RenderedText"/> clipped to a
        /// sub-rectangle using the
        /// <c>Draw(batcher, swidth, sheight, dx, dy, dwidth, dheight, offsetX, offsetY, depth, hue)</c>
        /// overload. Dest stores (dx, dy, dwidth, dheight); Source stores
        /// (offsetX, offsetY, swidth, sheight). Used by NameOverheadGump for
        /// camera-space clipped names, and by the legacy JournalGump for its
        /// scroll viewport.
        /// </summary>
        TextClipped,

        /// <summary>Typed sprite draw: renders a texture sub-rect (Source) into a screen rect (Dest).</summary>
        Sprite,

        /// <summary>
        /// Typed tiled sprite draw: renders a texture sub-rect repeated across a destination
        /// rect via <see cref="UltimaBatcher2D.DrawTiled"/>. Same fields as <see cref="Sprite"/>.
        /// Used by ResizePic centre strips, GumpPicTiled, GumpPicWithWidth, HSliderBar,
        /// ScrollBar middle segment.
        /// </summary>
        SpriteTiled,

        /// <summary>
        /// Typed <see cref="SpriteFont"/> string draw (not to be confused with
        /// <see cref="RenderedText"/>). Stores the font reference in <see cref="GumpDrawCommand.Font"/>,
        /// the text body in <see cref="GumpDrawCommand.TextString"/>, and the position in
        /// (X, Y). Used by DebugGump, NetworkStatsGump, and other HUD overlays that render
        /// via MonoGame's SpriteFont atlas.
        /// </summary>
        StringFont,

        /// <summary>
        /// Typed line draw (<see cref="UltimaBatcher2D.DrawLine"/>): a rotated 1-px-tall sprite
        /// spanning two screen points. Source.Location stores the start point and Dest.Location
        /// stores the end point (both in pixels); Alpha stores the stroke thickness. Used by
        /// MapGump to connect plotted pins.
        /// </summary>
        Line,

        /// <summary>Push a scissor rectangle (Dest) onto the clip stack. Flushes the current batch first.</summary>
        ClipPush,

        /// <summary>Pop the most recent scissor rectangle. Flushes the current batch first.</summary>
        ClipPop,

        /// <summary>Arbitrary <see cref="Func{UltimaBatcher2D, Boolean}"/> fallback. Preserved for callers
        /// that haven't been migrated to typed kinds yet; allocates a closure at enqueue time.</summary>
        Callback,
    }

    /// <summary>
    /// A single entry in a <see cref="RenderLists"/> gump command stream. Structs are kept in
    /// insertion order and flushed in order; adjacent Sprite entries that share a texture and
    /// clip state are batched into the same GPU draw call.
    /// <para/>
    /// Fields are split by <see cref="Kind"/>:
    /// <list type="bullet">
    /// <item>Text: <see cref="Text"/>, <see cref="X"/>, <see cref="Y"/>, <see cref="Alpha"/>, <see cref="Hue"/>, <see cref="LayerDepth"/>.</item>
    /// <item>Sprite: <see cref="Texture"/>, <see cref="Source"/>, <see cref="Dest"/>, <see cref="HueVector"/>, <see cref="LayerDepth"/>.</item>
    /// <item>ClipPush: <see cref="Dest"/> (the scissor rectangle).</item>
    /// <item>ClipPop: no payload.</item>
    /// <item>Callback: <see cref="Callback"/>.</item>
    /// </list>
    /// Use the factory constructors rather than populating the struct manually.
    /// </summary>
    internal readonly struct GumpDrawCommand
    {
        public readonly GumpCommandKind Kind;

        // Text payload (also stores X/Y and Alpha/Hue for text rendering).
        public readonly RenderedText Text;
        public readonly int X;
        public readonly int Y;
        public readonly float Alpha;
        public readonly ushort Hue;

        // Sprite payload (Dest also stores the clip rect for ClipPush).
        public readonly Texture2D Texture;
        public readonly Rectangle Source;
        public readonly Rectangle Dest;
        public readonly Vector3 HueVector;

        // StringFont payload.
        public readonly SpriteFont Font;
        public readonly string TextString;

        // Common.
        public readonly float LayerDepth;

        // Callback fallback payload.
        public readonly Func<UltimaBatcher2D, bool> Callback;

        /// <summary>Factory: typed text command.</summary>
        public static GumpDrawCommand CreateText(RenderedText text, int x, int y, float layerDepth, float alpha, ushort hue)
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.Text,
                text: text,
                x: x,
                y: y,
                alpha: alpha,
                hue: hue,
                texture: null,
                source: default,
                dest: default,
                hueVector: default,
                font: null,
                textString: null,
                layerDepth: layerDepth,
                callback: null
            );
        }

        /// <summary>Factory: typed sprite command.</summary>
        public static GumpDrawCommand CreateSprite(Texture2D texture, Rectangle source, Rectangle dest, Vector3 hueVector, float layerDepth)
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.Sprite,
                text: null,
                x: 0,
                y: 0,
                alpha: 0f,
                hue: 0,
                texture: texture,
                source: source,
                dest: dest,
                hueVector: hueVector,
                font: null,
                textString: null,
                layerDepth: layerDepth,
                callback: null
            );
        }

        /// <summary>Factory: tiled-sprite command (<see cref="UltimaBatcher2D.DrawTiled"/>).</summary>
        public static GumpDrawCommand CreateSpriteTiled(Texture2D texture, Rectangle source, Rectangle dest, Vector3 hueVector, float layerDepth)
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.SpriteTiled,
                text: null,
                x: 0,
                y: 0,
                alpha: 0f,
                hue: 0,
                texture: texture,
                source: source,
                dest: dest,
                hueVector: hueVector,
                font: null,
                textString: null,
                layerDepth: layerDepth,
                callback: null
            );
        }

        /// <summary>
        /// Factory: typed <see cref="SpriteFont"/> string draw. The text body is stored by
        /// reference (assumed immutable for the lifetime of the cached command; callers that
        /// recompute their string every frame should bump the render version accordingly).
        /// </summary>
        public static GumpDrawCommand CreateStringFont(SpriteFont font, string text, int x, int y, Vector3 hueVector, float layerDepth)
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.StringFont,
                text: null,
                x: x,
                y: y,
                alpha: 0f,
                hue: 0,
                texture: null,
                source: default,
                dest: default,
                hueVector: hueVector,
                font: font,
                textString: text,
                layerDepth: layerDepth,
                callback: null
            );
        }

        /// <summary>
        /// Factory: typed clipped-text command. <paramref name="dest"/> is (dx, dy,
        /// dwidth, dheight); <paramref name="sourceRect"/> encodes (offsetX, offsetY,
        /// swidth, sheight) for the <c>Draw(batcher, swidth, sheight, dx, dy, dwidth,
        /// dheight, offsetX, offsetY, depth, hue)</c> overload.
        /// </summary>
        public static GumpDrawCommand CreateTextClipped(
            RenderedText text,
            Rectangle dest,
            Rectangle sourceRect,
            float layerDepth,
            ushort hue
        )
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.TextClipped,
                text: text,
                x: 0,
                y: 0,
                alpha: 0f,
                hue: hue,
                texture: null,
                source: sourceRect,
                dest: dest,
                hueVector: default,
                font: null,
                textString: null,
                layerDepth: layerDepth,
                callback: null
            );
        }

        /// <summary>
        /// Factory: typed scrolled-text command. <paramref name="scrollWindow"/> maps to
        /// the (sx, sy, width, height) parameters of <see cref="RenderedText.Draw"/>'s
        /// scroll-window overload.
        /// </summary>
        public static GumpDrawCommand CreateTextScrolled(
            RenderedText text,
            int x,
            int y,
            Rectangle scrollWindow,
            float layerDepth,
            ushort hue
        )
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.TextScrolled,
                text: text,
                x: x,
                y: y,
                alpha: 0f,
                hue: hue,
                texture: null,
                source: scrollWindow,
                dest: default,
                hueVector: default,
                font: null,
                textString: null,
                layerDepth: layerDepth,
                callback: null
            );
        }

        /// <summary>
        /// Factory: typed rotated-line command. Start and end are stored as Source.Location
        /// and Dest.Location respectively; <paramref name="stroke"/> is stored in Alpha.
        /// </summary>
        public static GumpDrawCommand CreateLine(Texture2D texture, Vector2 start, Vector2 end, Vector3 hueVector, float stroke, float layerDepth)
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.Line,
                text: null,
                x: 0,
                y: 0,
                alpha: stroke,
                hue: 0,
                texture: texture,
                source: new Rectangle((int)start.X, (int)start.Y, 0, 0),
                dest: new Rectangle((int)end.X, (int)end.Y, 0, 0),
                hueVector: hueVector,
                font: null,
                textString: null,
                layerDepth: layerDepth,
                callback: null
            );
        }

        /// <summary>Factory: push a scissor rectangle.</summary>
        public static GumpDrawCommand CreateClipPush(Rectangle rect)
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.ClipPush,
                text: null,
                x: 0,
                y: 0,
                alpha: 0f,
                hue: 0,
                texture: null,
                source: default,
                dest: rect,
                hueVector: default,
                font: null,
                textString: null,
                layerDepth: 0f,
                callback: null
            );
        }

        /// <summary>Factory: pop the most recent scissor rectangle.</summary>
        public static GumpDrawCommand CreateClipPop()
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.ClipPop,
                text: null,
                x: 0,
                y: 0,
                alpha: 0f,
                hue: 0,
                texture: null,
                source: default,
                dest: default,
                hueVector: default,
                font: null,
                textString: null,
                layerDepth: 0f,
                callback: null
            );
        }

        /// <summary>Factory: arbitrary closure fallback.</summary>
        public static GumpDrawCommand CreateCallback(Func<UltimaBatcher2D, bool> callback)
        {
            return new GumpDrawCommand(
                kind: GumpCommandKind.Callback,
                text: null,
                x: 0,
                y: 0,
                alpha: 0f,
                hue: 0,
                texture: null,
                source: default,
                dest: default,
                hueVector: default,
                font: null,
                textString: null,
                layerDepth: 0f,
                callback: callback
            );
        }

        private GumpDrawCommand(
            GumpCommandKind kind,
            RenderedText text,
            int x,
            int y,
            float alpha,
            ushort hue,
            Texture2D texture,
            Rectangle source,
            Rectangle dest,
            Vector3 hueVector,
            SpriteFont font,
            string textString,
            float layerDepth,
            Func<UltimaBatcher2D, bool> callback
        )
        {
            Kind = kind;
            Text = text;
            X = x;
            Y = y;
            Alpha = alpha;
            Hue = hue;
            Texture = texture;
            Source = source;
            Dest = dest;
            HueVector = hueVector;
            Font = font;
            TextString = textString;
            LayerDepth = layerDepth;
            Callback = callback;
        }

        /// <summary>
        /// Returns a copy of this command with its screen-space position shifted by
        /// (<paramref name="dx"/>, <paramref name="dy"/>) and its <see cref="LayerDepth"/>
        /// shifted by <paramref name="dz"/>. Used by the gump-level retained cache to
        /// re-emit stored commands when the owning gump has only been translated (e.g.
        /// dragged) or moved within the gump z-stack (e.g. clicked-to-front, which shifts
        /// its position in <c>UIManager.Gumps</c> and therefore its starting depth slot)
        /// since the cache was built, without rebuilding the whole tree.
        /// <para/>
        /// Text and Sprite kinds shift their destination position; ClipPush shifts its
        /// scissor rectangle; ClipPop and TextScrolled's scroll-window UV are position-
        /// independent. ClipPush, ClipPop, and Callback ignore <paramref name="dz"/>
        /// because their flush path does not consult <see cref="LayerDepth"/>. Callback
        /// commands cannot be translated because their captured state is frozen inside
        /// the closure — callers must not request a translated replay of a buffer that
        /// contains Callback entries.
        /// </summary>
        public GumpDrawCommand WithOffset(int dx, int dy, float dz = 0f)
        {
            if (dx == 0 && dy == 0 && dz == 0f)
            {
                return this;
            }

            float newDepth = LayerDepth + dz;

            return Kind switch
            {
                GumpCommandKind.Text =>
                    new GumpDrawCommand(Kind, Text, X + dx, Y + dy, Alpha, Hue, Texture, Source, Dest, HueVector,
                        Font, TextString, newDepth, Callback),

                GumpCommandKind.TextScrolled =>
                    // Source carries the scroll sub-window (sx, sy, sw, sh) in its own
                    // coordinate space; don't shift Source. Only the anchor moves.
                    new GumpDrawCommand(Kind, Text, X + dx, Y + dy, Alpha, Hue, Texture, Source, Dest, HueVector,
                        Font, TextString, newDepth, Callback),

                GumpCommandKind.TextClipped =>
                    // Dest is the screen rect; Source is (offsetX, offsetY, swidth, sheight)
                    // in text-local space. Shift Dest, leave Source untouched.
                    new GumpDrawCommand(Kind, Text, X, Y, Alpha, Hue, Texture, Source,
                        new Rectangle(Dest.X + dx, Dest.Y + dy, Dest.Width, Dest.Height),
                        HueVector, Font, TextString, newDepth, Callback),

                GumpCommandKind.Sprite =>
                    new GumpDrawCommand(Kind, Text, X, Y, Alpha, Hue, Texture, Source,
                        new Rectangle(Dest.X + dx, Dest.Y + dy, Dest.Width, Dest.Height),
                        HueVector, Font, TextString, newDepth, Callback),

                GumpCommandKind.SpriteTiled =>
                    new GumpDrawCommand(Kind, Text, X, Y, Alpha, Hue, Texture, Source,
                        new Rectangle(Dest.X + dx, Dest.Y + dy, Dest.Width, Dest.Height),
                        HueVector, Font, TextString, newDepth, Callback),

                GumpCommandKind.StringFont =>
                    new GumpDrawCommand(Kind, Text, X + dx, Y + dy, Alpha, Hue, Texture, Source, Dest, HueVector,
                        Font, TextString, newDepth, Callback),

                GumpCommandKind.ClipPush =>
                    new GumpDrawCommand(Kind, Text, X, Y, Alpha, Hue, Texture, Source,
                        new Rectangle(Dest.X + dx, Dest.Y + dy, Dest.Width, Dest.Height),
                        HueVector, Font, TextString, LayerDepth, Callback),

                GumpCommandKind.Line =>
                    // Shift both endpoints: Source.Location is the line start, Dest.Location is the end.
                    new GumpDrawCommand(Kind, Text, X, Y, Alpha, Hue, Texture,
                        new Rectangle(Source.X + dx, Source.Y + dy, Source.Width, Source.Height),
                        new Rectangle(Dest.X + dx, Dest.Y + dy, Dest.Width, Dest.Height),
                        HueVector, Font, TextString, newDepth, Callback),

                _ => this,   // ClipPop has no position or depth; Callback must not be reached here.
            };
        }
    }

    /// <summary>
    /// Represents an ordered queue of GameObjects to be rendered.
    /// The order is determined by the draw order, not by the insertion order.
    /// Implementation for sorting and processing is passed as delegates.
    /// </summary>
    internal class RenderLists
    {
        private readonly List<GameObject> _tiles = [];
        private readonly List<GameObject> _stretchedTiles = [];
        private readonly List<GameObject> _statics = [];
        private readonly List<GameObject> _animations = [];
        private readonly List<GameObject> _effects = [];
        private readonly List<GameObject> _transparentObjects = [];

        /// <summary>
        /// Unified gump command stream. Replaces the previous split between
        /// <c>_gumpSprites</c> (atlas closures) and <c>_gumpTexts</c> (text / misc closures).
        /// Insertion order across sprites, text, and clips is now preserved, and adjacent
        /// typed commands sharing the same texture batch into a single GPU draw call during flush.
        /// </summary>
        private readonly List<GumpDrawCommand> _gumpCommands = [];

        public void Clear()
        {
            _tiles.Clear();
            _stretchedTiles.Clear();
            _statics.Clear();
            _animations.Clear();
            _effects.Clear();
            _transparentObjects.Clear();
            _gumpCommands.Clear();
        }

        public void Add(GameObject toRender, bool isTransparent = false)
        {
            if (isTransparent)
            {
                _transparentObjects.Add(toRender);
                return;
            }

            switch (toRender)
            {
                case Land land:
                    if (land.IsStretched)
                    {
                        _stretchedTiles.Add(toRender);
                    }
                    else
                    {
                        _tiles.Add(toRender);
                    }
                    break;

                case Static:
                case Multi:
                    _statics.Add(toRender);
                    break;

                case Mobile:
                    _animations.Add(toRender);
                    break;

                case Item item:
                    if (item.IsCorpse)
                    {
                        _animations.Add(toRender);
                    }
                    else
                    {
                        _statics.Add(toRender);
                    }
                    break;

                case GameEffect:
                    _effects.Add(toRender);
                    break;

                default:
                    break;
            }
        }

        // ───── Gump command stream API ─────

        /// <summary>
        /// Fallback overload for atlas-based gump sprites that haven't been migrated to the
        /// typed <see cref="AddGumpSprite"/> path yet. Allocates a closure per enqueue; prefer
        /// the typed overload in new code.
        /// </summary>
        public void AddGumpWithAtlas(Func<UltimaBatcher2D, bool> toRender)
        {
            if (toRender == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateCallback(toRender));
        }

        /// <summary>
        /// Queue a typed sprite draw (texture + source UV + destination rect + hue/alpha vector
        /// + layer depth). Zero-allocation; the flush path batches adjacent Sprite commands that
        /// share a texture into a single GPU draw call.
        /// </summary>
        public void AddGumpSprite(Texture2D texture, Rectangle source, Rectangle dest, Vector3 hueVector, float layerDepth)
        {
            if (texture == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateSprite(texture, source, dest, hueVector, layerDepth));
        }

        /// <summary>
        /// Convenience overload that uses the entire texture as the source rectangle. Useful
        /// for solid-colour rectangles drawn against a 1×1 cached texture from
        /// <c>SolidColorTextureCache</c>.
        /// </summary>
        public void AddGumpSprite(Texture2D texture, Rectangle dest, Vector3 hueVector, float layerDepth)
        {
            if (texture == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateSprite(
                texture,
                new Rectangle(0, 0, texture.Width, texture.Height),
                dest,
                hueVector,
                layerDepth
            ));
        }

        /// <summary>
        /// Queue a typed tiled sprite draw — the flush path calls
        /// <see cref="UltimaBatcher2D.DrawTiled"/>, which expands one command into multiple
        /// batcher quads internally to tile the source rectangle across the destination.
        /// Used by 9-slice borders, tiled backgrounds, and slider tracks.
        /// </summary>
        public void AddGumpSpriteTiled(Texture2D texture, Rectangle source, Rectangle dest, Vector3 hueVector, float layerDepth)
        {
            if (texture == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateSpriteTiled(texture, source, dest, hueVector, layerDepth));
        }

        /// <summary>
        /// Queue a typed <see cref="SpriteFont"/> string draw. Distinct from
        /// <see cref="AddGumpNoAtlas(RenderedText, int, int, float, float, ushort)"/>: that
        /// renders a pre-laid-out <see cref="RenderedText"/> via the font glyph atlas; this
        /// renders a raw string via MonoGame's SpriteFont path. Used by HUD overlays
        /// (DebugGump, NetworkStatsGump, etc.).
        /// <para/>
        /// The text reference is captured as-is. Callers that mutate the string's contents
        /// between frames (most HUDs update every ~100 ms) should bump their gump's render
        /// version so the cache rebuilds with the new text.
        /// </summary>
        public void AddGumpString(SpriteFont font, string text, int x, int y, Vector3 hueVector, float layerDepth)
        {
            if (font == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateStringFont(font, text, x, y, hueVector, layerDepth));
        }

        /// <summary>
        /// Queue a typed <see cref="RenderedText"/> draw. Zero-allocation; the flush path guards
        /// against destroyed/recycled text via <see cref="RenderedText.HasContent"/>.
        /// </summary>
        public void AddGumpNoAtlas(RenderedText text, int x, int y, float layerDepth, float alpha = 1f, ushort hue = 0)
        {
            if (text == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateText(text, x, y, layerDepth, alpha, hue));
        }

        /// <summary>
        /// Queue a typed scrolled <see cref="RenderedText"/> draw for controls that
        /// expose a sub-window of their laid-out text (HtmlControl, StbTextBox, login
        /// textbox). <paramref name="scrollWindow"/> is (scrollX, scrollY, windowW, windowH).
        /// </summary>
        public void AddGumpNoAtlasScrolled(RenderedText text, int x, int y, Rectangle scrollWindow, float layerDepth, ushort hue = 0)
        {
            if (text == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateTextScrolled(text, x, y, scrollWindow, layerDepth, hue));
        }

        /// <summary>
        /// Queue a typed clipped <see cref="RenderedText"/> draw using the
        /// (swidth, sheight, dx, dy, dwidth, dheight, offsetX, offsetY) overload.
        /// </summary>
        /// <param name="text">Source rendered text.</param>
        /// <param name="dest">Destination rect on screen (dx, dy, dwidth, dheight).</param>
        /// <param name="sourceOffset">Source sub-rect in text-local space
        /// (offsetX, offsetY, swidth, sheight).</param>
        public void AddGumpNoAtlasClipped(
            RenderedText text,
            Rectangle dest,
            Rectangle sourceOffset,
            float layerDepth,
            ushort hue = 0
        )
        {
            if (text == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateTextClipped(text, dest, sourceOffset, layerDepth, hue));
        }

        /// <summary>
        /// Fallback overload for non-sprite non-text callers that haven't been migrated to the
        /// typed <see cref="PushClip"/>/<see cref="PopClip"/> or <see cref="AddGumpSprite"/>
        /// paths yet. Allocates a closure per enqueue; prefer the typed overloads in new code.
        /// </summary>
        public void AddGumpNoAtlas(Func<UltimaBatcher2D, bool> toRender)
        {
            if (toRender == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateCallback(toRender));
        }

        /// <summary>
        /// Queue a rotated line (<see cref="UltimaBatcher2D.DrawLine"/>) between two screen
        /// points with the given stroke thickness. Used for connecting plotted pins on maps.
        /// </summary>
        public void AddGumpLine(Texture2D texture, Vector2 start, Vector2 end, Vector3 hueVector, float stroke, float layerDepth)
        {
            if (texture == null)
            {
                return;
            }

            _gumpCommands.Add(GumpDrawCommand.CreateLine(texture, start, end, hueVector, stroke, layerDepth));
        }

        /// <summary>
        /// Push a scissor rectangle onto the gump clip stack. Subsequent commands render clipped
        /// to this rectangle until the matching <see cref="PopClip"/>.
        /// </summary>
        public void PushClip(Rectangle rect)
        {
            _gumpCommands.Add(GumpDrawCommand.CreateClipPush(rect));
        }

        /// <summary>
        /// Pop the most recent scissor rectangle pushed via <see cref="PushClip"/>.
        /// </summary>
        public void PopClip()
        {
            _gumpCommands.Add(GumpDrawCommand.CreateClipPop());
        }

        /// <summary>
        /// Bulk-append a pre-built block of commands (typically a per-gump retained cache)
        /// into this frame's command stream. Preserves insertion order within the block
        /// so batching and clip-stack ordering remain correct.
        /// </summary>
        public void AppendCommands(List<GumpDrawCommand> source)
        {
            if (source == null || source.Count == 0)
            {
                return;
            }

            _gumpCommands.EnsureCapacity(_gumpCommands.Count + source.Count);
            _gumpCommands.AddRange(source);
        }

        /// <summary>
        /// Append a pre-built block of commands with each command's position shifted by
        /// (<paramref name="dx"/>, <paramref name="dy"/>) and its <see cref="GumpDrawCommand.LayerDepth"/>
        /// shifted by <paramref name="dz"/>. Used by the gump-level retained cache when the
        /// owning gump has only been translated (e.g. dragged) or moved within the gump
        /// z-stack (e.g. clicked-to-front, which shifts its position in
        /// <c>UIManager.Gumps</c> and therefore its starting depth slot) since the cache
        /// was built — lets the gump reuse its cached command stream without rebuilding.
        /// <para/>
        /// The caller must ensure <paramref name="source"/> contains no
        /// <see cref="GumpCommandKind.Callback"/> entries: closures hold their own frozen
        /// position and depth captures that this method can't translate.
        /// </summary>
        public void AppendCommandsTranslated(List<GumpDrawCommand> source, int dx, int dy, float dz = 0f)
        {
            if (source == null || source.Count == 0)
            {
                return;
            }

            _gumpCommands.EnsureCapacity(_gumpCommands.Count + source.Count);
            var span = CollectionsMarshal.AsSpan(source);
            for (int i = 0; i < span.Length; i++)
            {
                _gumpCommands.Add(span[i].WithOffset(dx, dy, dz));
            }
        }

        // ───── Test accessors ─────

        internal int GumpCommandCount => _gumpCommands.Count;
        internal GumpDrawCommand PeekGumpCommand(int index) => _gumpCommands[index];

        // ───── Draw pipeline ─────

        public int DrawRenderLists(UltimaBatcher2D batcher, sbyte maxGroundZ)
        {
            int result = DrawRenderList(batcher, _tiles, maxGroundZ) +
                   DrawRenderList(batcher, _stretchedTiles, maxGroundZ) +
                   DrawRenderList(batcher, _statics, maxGroundZ) +
                   DrawRenderList(batcher, _animations, maxGroundZ) +
                   DrawRenderList(batcher, _effects, maxGroundZ);

            if (_transparentObjects.Count > 0 || _gumpCommands.Count > 0)
            {
                result += DrawRenderList(batcher, _transparentObjects, maxGroundZ);
                result += DrawGumpCommands(batcher, _gumpCommands);
            }

            return result;
        }

        public int DrawRenderLists(UltimaBatcher2D batcher, sbyte maxGroundZ, List<Chunk> visibleChunks, int offsetX, int offsetY)
        {
            int result = 0;

            // Build visible indices for all chunks (skip rebuild if visibility unchanged)
            foreach (var chunk in visibleChunks)
            {
                var mesh = chunk.Mesh;
                if (mesh.Land.Count > 0)
                    mesh.Land.BuildVisibleIndices();
                if (mesh.Statics.Count > 0)
                    mesh.Statics.BuildVisibleIndices();
            }

            // Draw chunk mesh land tiles from GPU buffers with per-frame visibility
            batcher.SetWorldOffset(offsetX, offsetY);
            foreach (var chunk in visibleChunks)
                result += DrawMeshLayer(batcher, chunk.Mesh.Land);
            batcher.ResetWorldOffset();

            // Draw excluded land tiles (animated water, etc.)
            result += DrawRenderList(batcher, _tiles, maxGroundZ);
            result += DrawRenderList(batcher, _stretchedTiles, maxGroundZ);

            // Draw chunk mesh statics from GPU buffers with per-frame visibility
            batcher.SetWorldOffset(offsetX, offsetY);
            foreach (var chunk in visibleChunks)
                result += DrawMeshLayer(batcher, chunk.Mesh.Statics);
            batcher.ResetWorldOffset();

            // Draw excluded statics + animations + effects
            result += DrawRenderList(batcher, _statics, maxGroundZ) +
                   DrawRenderList(batcher, _animations, maxGroundZ) +
                   DrawRenderList(batcher, _effects, maxGroundZ);

            if (_transparentObjects.Count > 0 || _gumpCommands.Count > 0)
            {
                //batcher.SetStencil(DepthStencilState.DepthRead);
                result += DrawRenderList(batcher, _transparentObjects, maxGroundZ);
                result += DrawGumpCommands(batcher, _gumpCommands);
                //batcher.SetStencil(null);
            }

            return result;
        }

        private static int DrawMeshLayer(UltimaBatcher2D batcher, MeshLayer layer)
        {
            if (layer.VisibleSpriteCount == 0 || layer.VertexBuffer == null || layer.VertexBuffer.IsDisposed)
                return 0;

            layer.FlushAlphaChanges();

            var indexBuffer = batcher.GetDynamicIndexBuffer(layer.VisibleSpriteCount * 6);
            layer.UploadVisibleIndices(indexBuffer);

            batcher.GraphicsDevice.SetVertexBuffer(layer.VertexBuffer);
            batcher.GraphicsDevice.Indices = indexBuffer;

            for (int i = 0; i < layer.VisibleRunCount; i++)
            {
                ref var run = ref layer.VisibleRuns[i];
                batcher.DrawDirectIndexed(run.Texture, run.Start * 6, run.Count * 2, layer.Count * 4);
            }

            return layer.VisibleSpriteCount;
        }

        private static int DrawRenderList(UltimaBatcher2D batcher, List<GameObject> renderList, sbyte maxGroundZ)
        {
            int done = 0;

            foreach (var obj in renderList)
            {
                if (obj.Z <= maxGroundZ)
                {
                    float depth = obj.CalculateDepthZ();

                    if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y, depth))
                    {
                        done++;
                    }
                }
            }

            return done;
        }

        /// <summary>
        /// Flush the unified gump command buffer. Walks the commands in insertion order,
        /// dispatches on <see cref="GumpCommandKind"/>, and relies on the underlying
        /// <see cref="UltimaBatcher2D"/> to batch adjacent draws that share a texture.
        /// Clip commands translate into <see cref="UltimaBatcher2D.ClipBegin"/>/
        /// <see cref="UltimaBatcher2D.ClipEnd"/> calls which implicitly flush the batch.
        /// </summary>
        private static int DrawGumpCommands(UltimaBatcher2D batcher, List<GumpDrawCommand> commands)
        {
            int done = 0;

            // AsSpan avoids the List<T> enumerator allocation on the hot path.
            var span = CollectionsMarshal.AsSpan(commands);
            GumpRenderMetrics.CommandsEmitted += span.Length;

            // ClipBegin can return false when the requested rectangle is empty or
            // its intersection with the parent scissor is empty; in that case the
            // underlying ScissorStack is NOT pushed. The matching ClipPop must
            // therefore skip ClipEnd, otherwise it pops an empty stack and throws.
            // Track the per-push success in a small inline stack. 32 levels is
            // far more than any real gump nests.
            Span<bool> clipPushed = stackalloc bool[32];
            int clipDepth = 0;

            for (int i = 0; i < span.Length; i++)
            {
                ref readonly var cmd = ref span[i];

                switch (cmd.Kind)
                {
                    case GumpCommandKind.Text:
                    {
                        GumpRenderMetrics.TextCommands++;
                        // HasContent rejects destroyed/empty text, which can happen when the
                        // underlying pooled RenderedText was returned to its pool between
                        // enqueue and flush (see QueuedPool<RenderedText>).
                        if (cmd.Text != null && cmd.Text.HasContent &&
                            cmd.Text.Draw(batcher, cmd.X, cmd.Y, cmd.LayerDepth, cmd.Alpha, cmd.Hue))
                        {
                            done++;
                        }
                        break;
                    }

                    case GumpCommandKind.TextScrolled:
                    {
                        GumpRenderMetrics.TextScrolledCommands++;
                        if (cmd.Text != null && cmd.Text.HasContent)
                        {
                            // Source carries (scrollX, scrollY, windowW, windowH) for the
                            // scroll-window RenderedText.Draw overload.
                            if (cmd.Text.Draw(
                                batcher,
                                cmd.X,
                                cmd.Y,
                                cmd.Source.X,
                                cmd.Source.Y,
                                cmd.Source.Width,
                                cmd.Source.Height,
                                cmd.LayerDepth,
                                cmd.Hue > 0 ? cmd.Hue : -1
                            ))
                            {
                                done++;
                            }
                        }
                        break;
                    }

                    case GumpCommandKind.TextClipped:
                    {
                        GumpRenderMetrics.TextCommands++;
                        if (cmd.Text != null && cmd.Text.HasContent)
                        {
                            // Source = (offsetX, offsetY, swidth, sheight)
                            // Dest   = (dx, dy, dwidth, dheight)
                            if (cmd.Text.Draw(
                                batcher,
                                cmd.Source.Width,   // swidth
                                cmd.Source.Height,  // sheight
                                cmd.Dest.X,         // dx
                                cmd.Dest.Y,         // dy
                                cmd.Dest.Width,     // dwidth
                                cmd.Dest.Height,    // dheight
                                cmd.Source.X,       // offsetX
                                cmd.Source.Y,       // offsetY
                                cmd.LayerDepth,
                                cmd.Hue
                            ))
                            {
                                done++;
                            }
                        }
                        break;
                    }

                    case GumpCommandKind.Sprite:
                    {
                        GumpRenderMetrics.SpriteCommands++;
                        if (cmd.Texture != null && !cmd.Texture.IsDisposed)
                        {
                            batcher.Draw(cmd.Texture, cmd.Dest, cmd.Source, cmd.HueVector, cmd.LayerDepth);
                            done++;
                        }
                        break;
                    }

                    case GumpCommandKind.SpriteTiled:
                    {
                        GumpRenderMetrics.SpriteCommands++;
                        if (cmd.Texture != null && !cmd.Texture.IsDisposed)
                        {
                            batcher.DrawTiled(cmd.Texture, cmd.Dest, cmd.Source, cmd.HueVector, cmd.LayerDepth);
                            done++;
                        }
                        break;
                    }

                    case GumpCommandKind.StringFont:
                    {
                        GumpRenderMetrics.TextCommands++;
                        if (cmd.Font != null && !string.IsNullOrEmpty(cmd.TextString))
                        {
                            batcher.DrawString(cmd.Font, cmd.TextString, cmd.X, cmd.Y, cmd.HueVector, cmd.LayerDepth);
                            done++;
                        }
                        break;
                    }

                    case GumpCommandKind.Line:
                    {
                        GumpRenderMetrics.SpriteCommands++;
                        if (cmd.Texture != null && !cmd.Texture.IsDisposed)
                        {
                            batcher.DrawLine(
                                cmd.Texture,
                                new Vector2(cmd.Source.X, cmd.Source.Y),
                                new Vector2(cmd.Dest.X, cmd.Dest.Y),
                                cmd.HueVector,
                                cmd.Alpha,
                                cmd.LayerDepth
                            );
                            done++;
                        }
                        break;
                    }

                    case GumpCommandKind.ClipPush:
                    {
                        GumpRenderMetrics.ClipCommands++;
                        bool pushed = batcher.ClipBegin(cmd.Dest.X, cmd.Dest.Y, cmd.Dest.Width, cmd.Dest.Height);
                        if (clipDepth < clipPushed.Length)
                        {
                            clipPushed[clipDepth] = pushed;
                        }
                        clipDepth++;
                        break;
                    }

                    case GumpCommandKind.ClipPop:
                    {
                        GumpRenderMetrics.ClipCommands++;
                        if (clipDepth > 0)
                        {
                            clipDepth--;
                            if (clipDepth < clipPushed.Length && clipPushed[clipDepth])
                            {
                                batcher.ClipEnd();
                            }
                        }
                        break;
                    }

                    case GumpCommandKind.Callback:
                    {
                        GumpRenderMetrics.CallbackCommands++;
                        if (cmd.Callback != null && cmd.Callback.Invoke(batcher))
                        {
                            done++;
                        }
                        break;
                    }
                }
            }

            return done;
        }
    }
}
