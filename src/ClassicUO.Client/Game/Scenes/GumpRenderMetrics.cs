// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Scenes
{
    /// <summary>
    /// Per-frame counters for the unified gump command stream and the retained-mode
    /// render cache. All fields are reset at the start of each UIManager.Draw via
    /// <see cref="BeginFrame"/> and surfaced in-game through <see cref="UI.Gumps.DebugGump"/>
    /// so cache behaviour can be compared A/B against the pre-cache draw path.
    /// <para/>
    /// These are deliberately static fields rather than a struct+property API: the
    /// bump sites are in the render hot path (per-command flush loop, per-gump emit),
    /// so a direct increment keeps the instrumentation free when nothing reads the
    /// counters. The DebugGump only polls the fields every 100 ms.
    /// </summary>
    internal static class GumpRenderMetrics
    {
        // ───── Cache outcomes (one bump per EmitCommandsInto call) ─────

        /// <summary>Total number of gumps whose commands were emitted this frame.</summary>
        public static int GumpsRendered;

        /// <summary>Gumps whose cache was replayed without rebuild or translation
        /// (position unchanged, version unchanged).</summary>
        public static int CacheHits;

        /// <summary>Gumps whose cache was replayed with a translation offset (drag path).</summary>
        public static int CacheTranslationHits;

        /// <summary>Gumps that rebuilt their cache this frame (dirty or first build).</summary>
        public static int CacheMisses;

        /// <summary>Gumps with <c>EnableRenderCache = false</c> — the cache path is bypassed
        /// and every frame walks the full control tree as before.</summary>
        public static int CacheBypassed;

        // ───── Command counts (one bump per emitted command) ─────

        public static int CommandsEmitted;
        public static int SpriteCommands;
        public static int TextCommands;
        public static int TextScrolledCommands;
        public static int ClipCommands;
        public static int CallbackCommands;

        // ───── Batcher statistics (read from UltimaBatcher2D after UI flush) ─────

        public static int BatcherTextureSwitches;
        public static int BatcherFlushes;

        public static void BeginFrame()
        {
            GumpsRendered = 0;
            CacheHits = 0;
            CacheTranslationHits = 0;
            CacheMisses = 0;
            CacheBypassed = 0;

            CommandsEmitted = 0;
            SpriteCommands = 0;
            TextCommands = 0;
            TextScrolledCommands = 0;
            ClipCommands = 0;
            CallbackCommands = 0;

            BatcherTextureSwitches = 0;
            BatcherFlushes = 0;
        }
    }
}
