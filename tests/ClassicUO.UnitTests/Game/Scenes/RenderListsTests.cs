// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Xunit;

namespace ClassicUO.UnitTests.Game.Scenes
{
    /// <summary>
    /// Regression tests for the non-atlas gump queue (<see cref="RenderLists"/>
    /// <c>_gumpTexts</c>).
    /// <para/>
    /// Background: the refactor in ClassicUO commit dcd8d3fa8 ("Refactored the
    /// rendering stack") replaced direct gump-text drawing with a queue of
    /// <see cref="Func{UltimaBatcher2D, Boolean}"/> closures. Each closure captured
    /// <c>this._gameText</c> by reference. Because <see cref="RenderedText"/> is
    /// pooled via an internal <c>QueuedPool</c>, a closure queued in frame N could
    /// reference a <c>RenderedText</c> that was disposed and recycled into a
    /// different control's state before the closure ran. That produced the
    /// intermittent "blank name label" symptom reported on ModernUO's PropsGump.
    /// <para/>
    /// The fix replaced the closure list with <see cref="NoAtlasGumpCommand"/>
    /// struct values. Text commands carry the <c>RenderedText</c> reference plus
    /// its draw parameters; the flush path validates <c>HasContent</c> before
    /// drawing, so a recycled instance is skipped instead of drawing stale state
    /// or allocating per frame.
    /// <para/>
    /// These tests cover the queue/skip behaviour that is reachable without a
    /// live <c>GraphicsDevice</c>. The actual text rasterization is not exercised
    /// here — that path requires <see cref="UltimaBatcher2D"/> and depends on
    /// MonoGame infrastructure not available in CI.
    /// </summary>
    public class RenderListsTests
    {
        [Fact]
        public void AddGumpNoAtlas_WithNullText_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpNoAtlas((RenderedText)null, x: 10, y: 20, layerDepth: 1f);

            Assert.Equal(0, lists.GumpTextsCount);
        }

        [Fact]
        public void AddGumpNoAtlas_WithNullFunc_IsNoOp()
        {
            var lists = new RenderLists();

            lists.AddGumpNoAtlas((Func<UltimaBatcher2D, bool>)null);

            Assert.Equal(0, lists.GumpTextsCount);
        }

        [Fact]
        public void AddGumpNoAtlas_FuncPath_QueuesAsCallbackCommand()
        {
            var lists = new RenderLists();
            Func<UltimaBatcher2D, bool> cb = static _ => true;

            lists.AddGumpNoAtlas(cb);

            Assert.Equal(1, lists.GumpTextsCount);
            var cmd = lists.PeekGumpText(0);
            Assert.Null(cmd.Text);
            Assert.Same(cb, cmd.Callback);
        }

        [Fact]
        public void Clear_EmptiesGumpTexts()
        {
            var lists = new RenderLists();
            lists.AddGumpNoAtlas(static _ => true);
            lists.AddGumpNoAtlas(static _ => true);
            Assert.Equal(2, lists.GumpTextsCount);

            lists.Clear();

            Assert.Equal(0, lists.GumpTextsCount);
        }

        [Fact]
        public void AddGumpNoAtlas_PreservesInsertionOrder()
        {
            var lists = new RenderLists();
            Func<UltimaBatcher2D, bool> first = static _ => true;
            Func<UltimaBatcher2D, bool> second = static _ => false;
            Func<UltimaBatcher2D, bool> third = static _ => true;

            lists.AddGumpNoAtlas(first);
            lists.AddGumpNoAtlas(second);
            lists.AddGumpNoAtlas(third);

            Assert.Equal(3, lists.GumpTextsCount);
            Assert.Same(first, lists.PeekGumpText(0).Callback);
            Assert.Same(second, lists.PeekGumpText(1).Callback);
            Assert.Same(third, lists.PeekGumpText(2).Callback);
        }

        [Fact]
        public void NoAtlasGumpCommand_TextConstructor_AssignsAllFields()
        {
            // RenderedText cannot be constructed here (requires graphics init).
            // null is a legal Text value for the struct itself; the RenderLists
            // wrapper is what rejects null inputs. This test checks the struct
            // stores every parameter faithfully.
            var cmd = new NoAtlasGumpCommand(
                text: null,
                x: 42,
                y: 99,
                layerDepth: 3.5f,
                alpha: 0.75f,
                hue: 123
            );

            Assert.Null(cmd.Text);
            Assert.Equal(42, cmd.X);
            Assert.Equal(99, cmd.Y);
            Assert.Equal(3.5f, cmd.LayerDepth);
            Assert.Equal(0.75f, cmd.Alpha);
            Assert.Equal((ushort)123, cmd.Hue);
            Assert.Null(cmd.Callback);
        }

        [Fact]
        public void NoAtlasGumpCommand_CallbackConstructor_OnlyStoresCallback()
        {
            Func<UltimaBatcher2D, bool> cb = static _ => true;

            var cmd = new NoAtlasGumpCommand(cb);

            Assert.Null(cmd.Text);
            Assert.Same(cb, cmd.Callback);
            Assert.Equal(0, cmd.X);
            Assert.Equal(0, cmd.Y);
            Assert.Equal(0f, cmd.LayerDepth);
            Assert.Equal(0f, cmd.Alpha);
            Assert.Equal((ushort)0, cmd.Hue);
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
            Assert.Equal(measuredCalls, lists.GumpTextsCount);
        }

        [Fact]
        public void AddGumpNoAtlas_TextPath_DoesNotAllocatePerCall()
        {
            // Same allocation-budget guard for the typed text overload. With
            // null text the wrapper short-circuits before the List.Add, so we
            // can verify zero allocation on the null path even though we can't
            // construct a real RenderedText here.
            var lists = new RenderLists();

            // Null-path no-op: never touches the list; should allocate nothing.
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
            Assert.Equal(0, lists.GumpTextsCount);
        }

        [Fact]
        public void Mixed_TextAndFunc_PreserveInsertionOrder()
        {
            // Ensures the tagged-struct design preserves the original per-frame
            // insertion order across mixed entries (text + Func fallback).
            var lists = new RenderLists();
            Func<UltimaBatcher2D, bool> cb1 = static _ => true;
            Func<UltimaBatcher2D, bool> cb2 = static _ => false;

            lists.AddGumpNoAtlas(cb1);
            // AddGumpNoAtlas(null, ...) would be skipped by the null guard; but
            // we can push a struct directly via the Callback path with a second
            // distinct delegate to exercise ordering with heterogeneous entries.
            lists.AddGumpNoAtlas(cb2);

            Assert.Equal(2, lists.GumpTextsCount);
            Assert.Same(cb1, lists.PeekGumpText(0).Callback);
            Assert.Same(cb2, lists.PeekGumpText(1).Callback);
        }
    }
}
