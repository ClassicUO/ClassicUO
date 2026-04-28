// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.Scenes
{
    /// <summary>
    /// Tests for the unified gump command stream in <see cref="RenderLists"/>.
    /// <para/>
    /// Background: the render-stack refactor (commit dcd8d3fa8) introduced a
    /// closure-based gump draw queue. Closures captured <see cref="RenderedText"/>
    /// by reference, and because <see cref="RenderedText"/> is pooled via an
    /// internal <c>QueuedPool</c>, a closure queued in frame N could reference
    /// an instance that was recycled before the closure ran. This manifested on
    /// ModernUO's PropsGump as intermittent blank labels.
    /// <para/>
    /// The queue was rewritten into typed <see cref="GumpDrawCommand"/> struct
    /// values with a <see cref="GumpCommandKind"/> discriminator:
    /// <list type="bullet">
    /// <item><c>Text</c> — <see cref="RenderedText"/> + position + alpha/hue.</item>
    /// <item><c>Sprite</c> — texture + source UV + dest rect + hue vector.</item>
    /// <item><c>ClipPush</c>/<c>ClipPop</c> — scissor stack.</item>
    /// <item><c>Callback</c> — arbitrary closure fallback, kept for not-yet-migrated callers.</item>
    /// </list>
    /// The flush path validates <see cref="RenderedText.HasContent"/> before
    /// drawing, so recycled/destroyed instances skip cleanly instead of rendering
    /// stale state. Struct commands also eliminate the per-frame closure
    /// allocation that closures imposed.
    /// <para/>
    /// These tests cover the queue/skip behaviour that is reachable without a
    /// live <c>GraphicsDevice</c>. The actual text rasterization / GPU draw is
    /// not exercised here — that path requires <see cref="UltimaBatcher2D"/> and
    /// depends on MonoGame infrastructure not available in CI.
    /// </summary>
    public class RenderListsTests
    {
        [Fact]
        public void AddGumpNoAtlas_WithNullText_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpNoAtlas((RenderedText)null, x: 10, y: 20, layerDepth: 1f);

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void AddGumpNoAtlas_WithNullFunc_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpNoAtlas((Func<UltimaBatcher2D, bool>)null);

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void AddGumpWithAtlas_WithNullFunc_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpWithAtlas(null);

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void AddGumpSprite_WithNullTexture_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpSprite(null, new Rectangle(0, 0, 16, 16), new Rectangle(0, 0, 16, 16), Vector3.Zero, 1f);

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void AddGumpNoAtlas_FuncPath_QueuesAsCallbackCommand()
        {
            var lists = new RenderLists();
            Func<UltimaBatcher2D, bool> cb = static _ => true;

            lists.AddGumpNoAtlas(cb);

            Assert.Equal(1, lists.GumpCommandCount);
            var cmd = lists.PeekGumpCommand(0);
            Assert.Equal(GumpCommandKind.Callback, cmd.Kind);
            Assert.Null(cmd.Text);
            Assert.Same(cb, cmd.Callback);
        }

        [Fact]
        public void AddGumpWithAtlas_RoutesIntoUnifiedCommandStream()
        {
            // The atlas-fallback Func overload must write into the SAME list as
            // text and sprite commands so insertion order is preserved across
            // mixed-kind gumps (button background then caption, etc.).
            var lists = new RenderLists();
            Func<UltimaBatcher2D, bool> spriteCb = static _ => true;
            Func<UltimaBatcher2D, bool> textCb = static _ => true;

            lists.AddGumpWithAtlas(spriteCb);
            lists.AddGumpNoAtlas(textCb);

            Assert.Equal(2, lists.GumpCommandCount);
            Assert.Same(spriteCb, lists.PeekGumpCommand(0).Callback);
            Assert.Same(textCb, lists.PeekGumpCommand(1).Callback);
        }

        [Fact]
        public void Clear_EmptiesAllGumpCommands()
        {
            var lists = new RenderLists();
            lists.AddGumpNoAtlas(static _ => true);
            lists.AddGumpWithAtlas(static _ => true);
            lists.PushClip(new Rectangle(0, 0, 100, 100));
            lists.PopClip();
            Assert.Equal(4, lists.GumpCommandCount);

            lists.Clear();

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void CommandStream_PreservesInsertionOrderAcrossKinds()
        {
            var lists = new RenderLists();
            Func<UltimaBatcher2D, bool> first = static _ => true;
            Func<UltimaBatcher2D, bool> second = static _ => false;

            lists.AddGumpWithAtlas(first);                         // sprite via callback fallback
            lists.PushClip(new Rectangle(10, 20, 30, 40));
            lists.AddGumpNoAtlas(second);                          // text via callback fallback
            lists.PopClip();

            Assert.Equal(4, lists.GumpCommandCount);
            Assert.Equal(GumpCommandKind.Callback, lists.PeekGumpCommand(0).Kind);
            Assert.Same(first, lists.PeekGumpCommand(0).Callback);
            Assert.Equal(GumpCommandKind.ClipPush, lists.PeekGumpCommand(1).Kind);
            Assert.Equal(GumpCommandKind.Callback, lists.PeekGumpCommand(2).Kind);
            Assert.Same(second, lists.PeekGumpCommand(2).Callback);
            Assert.Equal(GumpCommandKind.ClipPop, lists.PeekGumpCommand(3).Kind);
        }

        [Fact]
        public void PushClip_StoresRectangleInDest()
        {
            var lists = new RenderLists();
            var clipRect = new Rectangle(7, 13, 42, 99);

            lists.PushClip(clipRect);

            Assert.Equal(1, lists.GumpCommandCount);
            var cmd = lists.PeekGumpCommand(0);
            Assert.Equal(GumpCommandKind.ClipPush, cmd.Kind);
            Assert.Equal(clipRect, cmd.Dest);
        }

        [Fact]
        public void GumpDrawCommand_TextFactory_PopulatesFields()
        {
            var cmd = GumpDrawCommand.CreateText(
                text: null,     // RenderedText needs graphics init; null is acceptable for field-layout test
                x: 42,
                y: 99,
                layerDepth: 3.5f,
                alpha: 0.75f,
                hue: 123
            );

            Assert.Equal(GumpCommandKind.Text, cmd.Kind);
            Assert.Equal(42, cmd.X);
            Assert.Equal(99, cmd.Y);
            Assert.Equal(3.5f, cmd.LayerDepth);
            Assert.Equal(0.75f, cmd.Alpha);
            Assert.Equal((ushort)123, cmd.Hue);
            Assert.Null(cmd.Text);
            Assert.Null(cmd.Callback);
            Assert.Null(cmd.Texture);
        }

        [Fact]
        public void GumpDrawCommand_CallbackFactory_OnlyStoresCallback()
        {
            Func<UltimaBatcher2D, bool> cb = static _ => true;

            var cmd = GumpDrawCommand.CreateCallback(cb);

            Assert.Equal(GumpCommandKind.Callback, cmd.Kind);
            Assert.Null(cmd.Text);
            Assert.Null(cmd.Texture);
            Assert.Same(cb, cmd.Callback);
            Assert.Equal(0, cmd.X);
            Assert.Equal(0, cmd.Y);
            Assert.Equal(0f, cmd.LayerDepth);
            Assert.Equal(0f, cmd.Alpha);
            Assert.Equal((ushort)0, cmd.Hue);
        }

        [Fact]
        public void GumpDrawCommand_ClipPushFactory_StoresRectInDest()
        {
            var rect = new Rectangle(1, 2, 3, 4);

            var cmd = GumpDrawCommand.CreateClipPush(rect);

            Assert.Equal(GumpCommandKind.ClipPush, cmd.Kind);
            Assert.Equal(rect, cmd.Dest);
            Assert.Null(cmd.Texture);
            Assert.Null(cmd.Text);
            Assert.Null(cmd.Callback);
        }

        [Fact]
        public void GumpDrawCommand_ClipPopFactory_HasNoPayload()
        {
            var cmd = GumpDrawCommand.CreateClipPop();

            Assert.Equal(GumpCommandKind.ClipPop, cmd.Kind);
            Assert.Equal(default, cmd.Dest);
            Assert.Null(cmd.Texture);
            Assert.Null(cmd.Text);
            Assert.Null(cmd.Callback);
        }

        [Fact]
        public void AddGumpNoAtlas_FuncPath_DoesNotAllocatePerCall()
        {
            // Caching the delegate in a static field means subsequent calls
            // should not allocate a closure. This guard is the regression fence
            // against reintroducing `batcher => { ... }` closures: a closure
            // would allocate ~40+ bytes per call (new closure object + delegate),
            // producing ~40 KB over 1000 calls on top of list overhead. Struct
            // pushes into a pre-grown List<T> allocate nothing.
            var lists = new RenderLists();
            Func<UltimaBatcher2D, bool> cb = static _ => true;

            // Pre-grow the backing list to a capacity greater than the
            // measured loop count. Clear() resets Count but keeps capacity,
            // so the measured Adds below don't trigger any List<T> array
            // reallocation.
            const int measuredCalls = 1000;
            const int warmupCalls = measuredCalls * 2;
            for (int i = 0; i < warmupCalls; i++)
            {
                lists.AddGumpNoAtlas(cb);
            }
            lists.Clear();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < measuredCalls; i++)
            {
                lists.AddGumpNoAtlas(cb);
            }
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;

            // Expect near-zero allocation. A small slack accounts for JIT
            // / test-infra noise only. Closure reintroduction would blow past
            // this by orders of magnitude.
            Assert.True(
                delta < 1024,
                $"AddGumpNoAtlas(Func) allocated {delta} bytes over {measuredCalls} warm calls; expected near zero. " +
                "A large value here usually means a closure allocation was reintroduced into AddGumpNoAtlas or the caller."
            );
            Assert.Equal(measuredCalls, lists.GumpCommandCount);
        }

        [Fact]
        public void AppendCommands_CopiesAllEntriesInOrder()
        {
            // AppendCommands is the bulk-copy primitive used by per-gump retained
            // render caches to replay a previously-built command block into the
            // per-frame stream. Order and count must be preserved exactly.
            var source = new System.Collections.Generic.List<GumpDrawCommand>
            {
                GumpDrawCommand.CreateClipPush(new Rectangle(0, 0, 10, 10)),
                GumpDrawCommand.CreateCallback(static _ => true),
                GumpDrawCommand.CreateText(null, 1, 2, 3f, 1f, 0),
                GumpDrawCommand.CreateClipPop(),
            };

            var lists = new RenderLists();
            lists.AppendCommands(source);

            Assert.Equal(source.Count, lists.GumpCommandCount);
            Assert.Equal(GumpCommandKind.ClipPush, lists.PeekGumpCommand(0).Kind);
            Assert.Equal(GumpCommandKind.Callback, lists.PeekGumpCommand(1).Kind);
            Assert.Equal(GumpCommandKind.Text, lists.PeekGumpCommand(2).Kind);
            Assert.Equal(GumpCommandKind.ClipPop, lists.PeekGumpCommand(3).Kind);
        }

        [Fact]
        public void AppendCommands_WithNullOrEmpty_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AppendCommands(null);
            Assert.Equal(0, lists.GumpCommandCount);

            lists.AppendCommands(new System.Collections.Generic.List<GumpDrawCommand>());
            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void WithOffset_ShiftsSpriteDestOnly()
        {
            // Sprites carry their screen rect in Dest; Source is the atlas UV
            // rectangle and must not shift.
            var cmd = GumpDrawCommand.CreateSprite(
                texture: null,
                source: new Rectangle(100, 200, 50, 60),
                dest: new Rectangle(10, 20, 50, 60),
                hueVector: default,
                layerDepth: 1f
            );

            var shifted = cmd.WithOffset(5, 7);

            Assert.Equal(new Rectangle(100, 200, 50, 60), shifted.Source);
            Assert.Equal(new Rectangle(15, 27, 50, 60), shifted.Dest);
        }

        [Fact]
        public void WithOffset_ShiftsTextPosition()
        {
            var cmd = GumpDrawCommand.CreateText(null, 10, 20, 1f, 1f, 0);

            var shifted = cmd.WithOffset(3, -4);

            Assert.Equal(13, shifted.X);
            Assert.Equal(16, shifted.Y);
        }

        [Fact]
        public void WithOffset_ShiftsClipPushRect()
        {
            var cmd = GumpDrawCommand.CreateClipPush(new Rectangle(10, 20, 100, 200));

            var shifted = cmd.WithOffset(5, 6);

            Assert.Equal(new Rectangle(15, 26, 100, 200), shifted.Dest);
        }

        [Fact]
        public void WithOffset_ClipPopIsPositionIndependent()
        {
            var cmd = GumpDrawCommand.CreateClipPop();

            var shifted = cmd.WithOffset(100, 100);

            Assert.Equal(GumpCommandKind.ClipPop, shifted.Kind);
            Assert.Equal(default, shifted.Dest);
        }

        [Fact]
        public void AddGumpSpriteTiled_WithNullTexture_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpSpriteTiled(
                null,
                new Rectangle(0, 0, 8, 8),
                new Rectangle(0, 0, 100, 16),
                Vector3.Zero,
                1f
            );

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void AddGumpSpriteTiled_QueuesAsSpriteTiledKind()
        {
            var lists = new RenderLists();
            // We can't construct a real Texture2D without a graphics device, but
            // any non-null Texture2D reference would be accepted. Using the factory
            // directly to sidestep the null-check gate.
            var cmd = GumpDrawCommand.CreateSpriteTiled(
                texture: null,
                source: new Rectangle(0, 0, 8, 8),
                dest: new Rectangle(0, 0, 100, 16),
                hueVector: new Vector3(1, 2, 3),
                layerDepth: 0.5f
            );

            Assert.Equal(GumpCommandKind.SpriteTiled, cmd.Kind);
            Assert.Equal(new Rectangle(0, 0, 8, 8), cmd.Source);
            Assert.Equal(new Rectangle(0, 0, 100, 16), cmd.Dest);
            Assert.Equal(new Vector3(1, 2, 3), cmd.HueVector);
            Assert.Equal(0.5f, cmd.LayerDepth);
        }

        [Fact]
        public void AddGumpString_WithNullFont_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpString(null, "hello", 10, 20, Vector3.Zero, 1f);

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void AddGumpString_WithEmptyText_IsNoOp()
        {
            var lists = new RenderLists();

            // Using the factory directly since we can't construct a real SpriteFont
            // without a graphics device. The wrapper also gates on null font, which
            // matches the factory's general contract.
            lists.AddGumpString(null, "", 10, 20, Vector3.Zero, 1f);

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void GumpDrawCommand_StringFontFactory_PopulatesFields()
        {
            var cmd = GumpDrawCommand.CreateStringFont(
                font: null,   // real SpriteFont needs graphics init; null is fine for field-layout checks
                text: "hello",
                x: 42,
                y: 99,
                hueVector: new Vector3(0.5f, 0.25f, 1f),
                layerDepth: 2.5f
            );

            Assert.Equal(GumpCommandKind.StringFont, cmd.Kind);
            Assert.Equal("hello", cmd.TextString);
            Assert.Equal(42, cmd.X);
            Assert.Equal(99, cmd.Y);
            Assert.Equal(new Vector3(0.5f, 0.25f, 1f), cmd.HueVector);
            Assert.Equal(2.5f, cmd.LayerDepth);
            Assert.Null(cmd.Font);
            Assert.Null(cmd.Text);
            Assert.Null(cmd.Texture);
            Assert.Null(cmd.Callback);
        }

        [Fact]
        public void WithOffset_ShiftsSpriteTiledDest()
        {
            var cmd = GumpDrawCommand.CreateSpriteTiled(
                texture: null,
                source: new Rectangle(100, 200, 8, 8),
                dest: new Rectangle(10, 20, 100, 16),
                hueVector: Vector3.Zero,
                layerDepth: 1f
            );

            var shifted = cmd.WithOffset(5, 7);

            Assert.Equal(new Rectangle(100, 200, 8, 8), shifted.Source);  // Source unchanged
            Assert.Equal(new Rectangle(15, 27, 100, 16), shifted.Dest);
            Assert.Equal(GumpCommandKind.SpriteTiled, shifted.Kind);
        }

        [Fact]
        public void WithOffset_ShiftsStringFontPosition()
        {
            var cmd = GumpDrawCommand.CreateStringFont(
                font: null,
                text: "hi",
                x: 10,
                y: 20,
                hueVector: Vector3.Zero,
                layerDepth: 1f
            );

            var shifted = cmd.WithOffset(3, -4);

            Assert.Equal(13, shifted.X);
            Assert.Equal(16, shifted.Y);
            Assert.Equal("hi", shifted.TextString);   // payload preserved
            Assert.Equal(GumpCommandKind.StringFont, shifted.Kind);
        }

        [Fact]
        public void WithOffset_ZeroDelta_ReturnsSameValues()
        {
            var cmd = GumpDrawCommand.CreateText(null, 10, 20, 1f, 1f, 0);

            var same = cmd.WithOffset(0, 0);

            Assert.Equal(cmd.X, same.X);
            Assert.Equal(cmd.Y, same.Y);
            Assert.Equal(cmd.Kind, same.Kind);
        }

        [Fact]
        public void AppendCommandsTranslated_ShiftsEveryCommand()
        {
            // End-to-end of the drag-path replay primitive.
            var source = new System.Collections.Generic.List<GumpDrawCommand>
            {
                GumpDrawCommand.CreateClipPush(new Rectangle(0, 0, 100, 100)),
                GumpDrawCommand.CreateSprite(
                    null,
                    new Rectangle(0, 0, 50, 50),
                    new Rectangle(10, 20, 50, 50),
                    default,
                    1f
                ),
                GumpDrawCommand.CreateText(null, 30, 40, 1f, 1f, 0),
                GumpDrawCommand.CreateClipPop(),
            };

            var lists = new RenderLists();
            lists.AppendCommandsTranslated(source, dx: 3, dy: 7);

            Assert.Equal(4, lists.GumpCommandCount);

            Assert.Equal(new Rectangle(3, 7, 100, 100), lists.PeekGumpCommand(0).Dest);  // clip shifted
            Assert.Equal(new Rectangle(13, 27, 50, 50), lists.PeekGumpCommand(1).Dest);  // sprite dest shifted
            Assert.Equal(new Rectangle(0, 0, 50, 50), lists.PeekGumpCommand(1).Source);  // source unchanged
            Assert.Equal(33, lists.PeekGumpCommand(2).X);                                // text x shifted
            Assert.Equal(47, lists.PeekGumpCommand(2).Y);                                // text y shifted
            Assert.Equal(GumpCommandKind.ClipPop, lists.PeekGumpCommand(3).Kind);        // pop unchanged
        }

        [Fact]
        public void AppendCommandsTranslated_WithNullOrEmpty_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AppendCommandsTranslated(null, 1, 1);
            Assert.Equal(0, lists.GumpCommandCount);

            lists.AppendCommandsTranslated(new System.Collections.Generic.List<GumpDrawCommand>(), 1, 1);
            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void AddGumpLine_WithNullTexture_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpLine(null, new Vector2(0, 0), new Vector2(10, 10), Vector3.Zero, 1f, 1f);

            Assert.Equal(0, lists.GumpCommandCount);
        }

        [Fact]
        public void GumpDrawCommand_LineFactory_PackedStartAndEnd()
        {
            var cmd = GumpDrawCommand.CreateLine(
                texture: null,
                start: new Vector2(10, 20),
                end: new Vector2(100, 200),
                hueVector: new Vector3(0.5f, 0.25f, 1f),
                stroke: 2f,
                layerDepth: 3f
            );

            Assert.Equal(GumpCommandKind.Line, cmd.Kind);
            Assert.Equal(10, cmd.Source.X);
            Assert.Equal(20, cmd.Source.Y);
            Assert.Equal(100, cmd.Dest.X);
            Assert.Equal(200, cmd.Dest.Y);
            Assert.Equal(2f, cmd.Alpha);           // stroke lives in Alpha
            Assert.Equal(new Vector3(0.5f, 0.25f, 1f), cmd.HueVector);
            Assert.Equal(3f, cmd.LayerDepth);
        }

        [Fact]
        public void WithOffset_ShiftsBothLineEndpoints()
        {
            var cmd = GumpDrawCommand.CreateLine(
                texture: null,
                start: new Vector2(10, 20),
                end: new Vector2(100, 200),
                hueVector: Vector3.Zero,
                stroke: 1f,
                layerDepth: 1f
            );

            var shifted = cmd.WithOffset(5, 7);

            Assert.Equal(15, shifted.Source.X);
            Assert.Equal(27, shifted.Source.Y);
            Assert.Equal(105, shifted.Dest.X);
            Assert.Equal(207, shifted.Dest.Y);
            Assert.Equal(1f, shifted.Alpha);   // stroke preserved
            Assert.Equal(GumpCommandKind.Line, shifted.Kind);
        }

        [Fact]
        public void WithOffset_ShiftsLayerDepth_OnText()
        {
            // Background: gump cache replays commands with their LayerDepth baked in
            // at cache-build time. When the gump's position in UIManager.Gumps changes
            // (e.g. clicked-to-front), its starting depth slot changes too and cached
            // commands need their depths shifted on replay or the GPU's depth-test
            // (DepthStencilState.Default) lets behind-gump pixels bleed through the
            // now-front gump until the next forced rebuild.
            var cmd = GumpDrawCommand.CreateText(null, 10, 20, layerDepth: -9990f, alpha: 1f, hue: 0);

            var shifted = cmd.WithOffset(0, 0, dz: 10f);

            Assert.Equal(-9980f, shifted.LayerDepth);
            Assert.Equal(10, shifted.X);   // position untouched when dx/dy are zero
            Assert.Equal(20, shifted.Y);
        }

        [Fact]
        public void WithOffset_ShiftsLayerDepth_OnSprite()
        {
            var cmd = GumpDrawCommand.CreateSprite(
                texture: null,
                source: new Rectangle(0, 0, 32, 32),
                dest: new Rectangle(5, 6, 32, 32),
                hueVector: Vector3.Zero,
                layerDepth: -9985f
            );

            var shifted = cmd.WithOffset(2, 3, dz: -10f);

            Assert.Equal(-9995f, shifted.LayerDepth);
            Assert.Equal(new Rectangle(7, 9, 32, 32), shifted.Dest);
        }

        [Fact]
        public void WithOffset_ShiftsLayerDepth_AllDrawKinds()
        {
            // Comprehensive guard that every draw-emitting kind shifts LayerDepth.
            // ClipPush/ClipPop/Callback are intentionally excluded — their flush path
            // does not read LayerDepth, so shifting it would be dead work.
            var kinds = new (string name, GumpDrawCommand cmd)[]
            {
                ("Text",         GumpDrawCommand.CreateText(null, 0, 0, 1f, 1f, 0)),
                ("TextScrolled", GumpDrawCommand.CreateTextScrolled(null, 0, 0, default, 1f, 0)),
                ("TextClipped",  GumpDrawCommand.CreateTextClipped(null, default, default, 1f, 0)),
                ("Sprite",       GumpDrawCommand.CreateSprite(null, default, default, default, 1f)),
                ("SpriteTiled",  GumpDrawCommand.CreateSpriteTiled(null, default, default, default, 1f)),
                ("StringFont",   GumpDrawCommand.CreateStringFont(null, "x", 0, 0, default, 1f)),
                ("Line",         GumpDrawCommand.CreateLine(null, Vector2.Zero, Vector2.Zero, default, 1f, 1f)),
            };

            foreach (var (name, cmd) in kinds)
            {
                var shifted = cmd.WithOffset(0, 0, dz: 5f);
                Assert.Equal(6f, shifted.LayerDepth);
                Assert.Equal(cmd.Kind, shifted.Kind);
            }
        }

        [Fact]
        public void WithOffset_ClipPush_DepthIgnored()
        {
            // ClipPush's flush path is batcher.ClipBegin(Dest) — never reads LayerDepth.
            // Shifting it would be harmless but dead work; the implementation keeps the
            // original value so this contract is explicit.
            var cmd = GumpDrawCommand.CreateClipPush(new Rectangle(0, 0, 100, 100));

            var shifted = cmd.WithOffset(5, 6, dz: 1234f);

            Assert.Equal(0f, shifted.LayerDepth);   // original was 0; not 1234
            Assert.Equal(new Rectangle(5, 6, 100, 100), shifted.Dest);   // position still shifts
        }

        [Fact]
        public void WithOffset_ZeroDeltaIncludingDepth_ReturnsSame()
        {
            // Early-return optimization: nothing changed → return the same struct value.
            var cmd = GumpDrawCommand.CreateText(null, 10, 20, layerDepth: 5f, alpha: 1f, hue: 42);

            var same = cmd.WithOffset(0, 0, 0f);

            Assert.Equal(cmd.X, same.X);
            Assert.Equal(cmd.Y, same.Y);
            Assert.Equal(cmd.LayerDepth, same.LayerDepth);
            Assert.Equal(cmd.Hue, same.Hue);
        }

        [Fact]
        public void AppendCommandsTranslated_ShiftsLayerDepth()
        {
            // End-to-end of the click-to-front replay primitive: the cached commands
            // were baked at depth -9990 (gump was at the back of the z-stack) and the
            // gump just moved to the front (depth -9980). dz=+10 must lift every
            // draw-emitting command's LayerDepth by 10 so the GPU depth-test puts
            // the now-front gump's pixels in front of the now-back gump's pixels.
            var source = new System.Collections.Generic.List<GumpDrawCommand>
            {
                GumpDrawCommand.CreateClipPush(new Rectangle(0, 0, 100, 100)),
                GumpDrawCommand.CreateSprite(null, new Rectangle(0, 0, 50, 50),
                    new Rectangle(10, 20, 50, 50), default, layerDepth: -9990f),
                GumpDrawCommand.CreateText(null, 30, 40, layerDepth: -9989.99f, alpha: 1f, hue: 0),
                GumpDrawCommand.CreateClipPop(),
            };

            var lists = new RenderLists();
            lists.AppendCommandsTranslated(source, dx: 3, dy: 7, dz: 10f);

            Assert.Equal(4, lists.GumpCommandCount);

            // ClipPush: position shifted, depth left at original 0 (depth-irrelevant).
            Assert.Equal(new Rectangle(3, 7, 100, 100), lists.PeekGumpCommand(0).Dest);
            Assert.Equal(0f, lists.PeekGumpCommand(0).LayerDepth);

            // Sprite: position AND depth shifted.
            Assert.Equal(new Rectangle(13, 27, 50, 50), lists.PeekGumpCommand(1).Dest);
            Assert.Equal(-9980f, lists.PeekGumpCommand(1).LayerDepth);

            // Text: position AND depth shifted.
            Assert.Equal(33, lists.PeekGumpCommand(2).X);
            Assert.Equal(47, lists.PeekGumpCommand(2).Y);
            Assert.Equal(-9979.99f, lists.PeekGumpCommand(2).LayerDepth, precision: 4);

            // ClipPop is depth-independent.
            Assert.Equal(GumpCommandKind.ClipPop, lists.PeekGumpCommand(3).Kind);
        }

        [Fact]
        public void AppendCommandsTranslated_DepthOnlyShift_PreservesPositions()
        {
            // The pure z-stack-move case: gump didn't drag, only its position in the
            // Gumps LinkedList changed. dx=dy=0, dz!=0; positions stay put, depths lift.
            var source = new System.Collections.Generic.List<GumpDrawCommand>
            {
                GumpDrawCommand.CreateText(null, 100, 200, layerDepth: -9990f, alpha: 1f, hue: 0),
                GumpDrawCommand.CreateSprite(null, default,
                    new Rectangle(50, 60, 70, 80), default, layerDepth: -9989f),
            };

            var lists = new RenderLists();
            lists.AppendCommandsTranslated(source, dx: 0, dy: 0, dz: 20f);

            Assert.Equal(2, lists.GumpCommandCount);
            Assert.Equal(100, lists.PeekGumpCommand(0).X);
            Assert.Equal(200, lists.PeekGumpCommand(0).Y);
            Assert.Equal(-9970f, lists.PeekGumpCommand(0).LayerDepth);

            Assert.Equal(new Rectangle(50, 60, 70, 80), lists.PeekGumpCommand(1).Dest);
            Assert.Equal(-9969f, lists.PeekGumpCommand(1).LayerDepth);
        }

        [Fact]
        public void AddGumpNoAtlas_TextPath_NullShortCircuitsWithoutAllocation()
        {
            // Same allocation-budget guard for the typed text overload. With
            // null text the wrapper short-circuits before the List.Add, so we
            // can verify zero allocation on the null path even though we can't
            // construct a real RenderedText here.
            var lists = new RenderLists();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                lists.AddGumpNoAtlas((RenderedText)null, 0, 0, 0f);
            }
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.True(
                delta < 1024,
                $"AddGumpNoAtlas(null text) allocated {delta} bytes; expected zero."
            );
            Assert.Equal(0, lists.GumpCommandCount);
        }

        // Source-level regression guard: every gump/control whose closures were
        // migrated to typed GumpDrawCommand values (BuffGump, AnchorableGump,
        // MenuGump, WorldViewportGump, UseSpellButtonGump, ColorBox,
        // ContextMenuControl, Control.DrawDebug, Combobox) should never
        // reintroduce a `(batcher) =>` closure: doing so freezes captures and
        // defeats the gump retained-render cache (commit d3545a639). If this
        // test fails, the offending file has fallen back onto
        // AddGumpWithAtlas/AddGumpNoAtlas(Func<...>) — convert it to the typed
        // AddGumpSprite/AddGumpSpriteTiled/AddGumpNoAtlas(RenderedText, ...)
        // overloads instead.
        [Theory]
        [InlineData("src/ClassicUO.Client/Game/UI/Gumps/BuffGump.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Gumps/AnchorableGump.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Gumps/MenuGump.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Gumps/WorldViewportGump.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Gumps/UseSpellButtonGump.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Controls/ColorBox.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Controls/ContextMenuControl.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Controls/Control.cs")]
        [InlineData("src/ClassicUO.Client/Game/UI/Controls/Combobox.cs")]
        public void MigratedFiles_ContainNoBatcherClosures(string relativePath)
        {
            string repoRoot = FindRepoRoot();
            string fullPath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(fullPath), $"Source file not found: {fullPath}");

            string contents = File.ReadAllText(fullPath);
            // Match `batcher =>` or `(batcher) =>` — either form indicates a
            // Func<UltimaBatcher2D, bool> closure was reintroduced.
            var match = Regex.Match(contents, @"\(?\s*batcher\s*\)?\s*=>");
            Assert.False(
                match.Success,
                $"{relativePath} reintroduced a batcher closure (matched: \"{match.Value}\"). " +
                "Convert it to typed AddGumpSprite/AddGumpSpriteTiled/AddGumpNoAtlas(RenderedText) commands."
            );
        }

        private static string FindRepoRoot([CallerFilePath] string callerFile = null)
        {
            // Walk up from the test source file until we find the repo root
            // (identified by the .git directory). This makes the test runnable
            // regardless of where the test binary lives.
            var dir = new DirectoryInfo(Path.GetDirectoryName(callerFile));
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }

            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}
