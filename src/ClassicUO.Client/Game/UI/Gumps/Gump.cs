// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Gump : Control
    {
        public Gump(World world, uint local, uint server)
        {
            World = world;
            LocalSerial = local;
            ServerSerial = server;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public World World { get; }

        public bool CanBeSaved => GumpType != Gumps.GumpType.None;

        public virtual GumpType GumpType { get; }

        public bool InvalidateContents { get; set; }

        public uint MasterGumpSerial { get; set; }


        public override void Update()
        {
            if (InvalidateContents)
            {
                UpdateContents();
                InvalidateContents = false;
            }

            if (ActivePage == 0)
            {
                ActivePage = 1;
            }

            base.Update();
        }

        public override void Dispose()
        {
            Item it = World.Items.Get(LocalSerial);

            if (it != null && it.Opened)
            {
                it.Opened = false;
            }

            base.Dispose();
        }


        public virtual void Save(XmlTextWriter writer)
        {
            writer.WriteAttributeString("type", ((int) GumpType).ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("serial", LocalSerial.ToString());
        }

        public void SetInScreen()
        {
            Rectangle windowBounds = Client.Game.ClientBounds;
            Rectangle bounds = Bounds;
            bounds.X += windowBounds.X;
            bounds.Y += windowBounds.Y;

            if (windowBounds.Intersects(bounds))
            {
                return;
            }

            X = 0;
            Y = 0;
        }

        public virtual void Restore(XmlElement xml)
        {
        }

        public void RequestUpdateContents()
        {
            InvalidateContents = true;
        }

        protected virtual void UpdateContents()
        {
        }

        protected override void OnDragEnd(int x, int y)
        {
            Point position = Location;
            int halfWidth = Width - (Width >> 2);
            int halfHeight = Height - (Height >> 2);

            if (X < -halfWidth)
            {
                position.X = -halfWidth;
            }

            if (Y < -halfHeight)
            {
                position.Y = -halfHeight;
            }

            if (X > Client.Game.ClientBounds.Width - (Width - halfWidth))
            {
                position.X = Client.Game.ClientBounds.Width - (Width - halfWidth);
            }

            if (Y > Client.Game.ClientBounds.Height - (Height - halfHeight))
            {
                position.Y = Client.Game.ClientBounds.Height - (Height - halfHeight);
            }

            Location = position;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            return IsVisible && base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        // ───── Retained-mode render cache ─────
        //
        // When EnableRenderCache is true, the gump's emitted GumpDrawCommand
        // stream is cached between frames. The cache is replayed into the
        // per-frame unified RenderLists on subsequent frames as long as nothing
        // in the gump has changed. A change bumps _renderVersion, which causes
        // the next EmitCommandsInto call to rebuild the cache from scratch.
        //
        // Commit 4 lays the infrastructure but leaves EnableRenderCache default-
        // off so behaviour is identical to pre-cache. Commit 5 wires render-
        // affecting Control setters to call InvalidateRenderCache; Commit 6
        // adds a pure-translation fast path for dragged gumps.
        //
        // Gumps that emit any GumpCommandKind.Callback commands can still be
        // cached because the Func reference is stored in the struct; the
        // closure captures its own state. Cache invalidation is still the
        // owning gump's responsibility when that captured state would change.

        private List<GumpDrawCommand> _renderCache;
        private int _renderVersion;
        private int _lastBuiltRenderVersion = -1;
        private int _cachedAtX;
        private int _cachedAtY;
        // Starting layerDepth that was passed in when the cache was last built. The
        // per-gump depth slot is assigned by UIManager.Draw based on each gump's
        // position in the Gumps LinkedList: -10000 + N*GumpDepthStride. That slot
        // changes whenever the list is reordered (MakeTopMostGump on click) or when
        // gumps are added/removed in front of this one. Without tracking the entry
        // depth and shifting cached LayerDepth values on replay, cached commands
        // keep their old depths after a reorder, the GPU's depth-test (Default
        // DepthStencilState) sees stale values, and a behind-gump's text bleeds
        // through a now-in-front gump until the next forced rebuild.
        private float _cachedAtDepth;
        private bool _cacheContainsCallback;

        // Debug-build aid: warn once per gump type when a Callback command sneaks
        // into a retained cache. That combination is a silent performance
        // regression — the cache can never fast-path replay because closures
        // freeze captured positions — so a warning makes new regressions
        // immediately visible in logs. Three gumps are documented exceptions:
        // WorldMapGump, MiniMapGump, TextContainerGump.
        private static readonly HashSet<Type> s_cachedCallbackWarned = new();
        private static readonly HashSet<Type> s_cachedCallbackAllowed = new()
        {
            typeof(WorldMapGump),
            typeof(MiniMapGump),
            typeof(TextContainerGump),
        };

        /// <summary>
        /// Global toggle that forces the retained-mode cache on for every Gump
        /// regardless of its per-instance <see cref="EnableRenderCache"/> flag.
        /// Intended as a debug switch: flip it on to A/B the full system, observe
        /// metrics via <see cref="UI.Gumps.DebugGump"/>, then pin the cache on
        /// specific gumps whose invalidation coverage you've verified.
        /// </summary>
        public static bool DefaultEnableRenderCache;

        /// <summary>
        /// Opt-in flag for the retained-mode command cache. When true, the
        /// gump's emitted command stream is snapshotted and replayed unchanged
        /// on subsequent frames until <see cref="InvalidateRenderCache"/> fires.
        /// Default false to preserve pre-cache behaviour until callers are
        /// audited for render-affecting setters. The global
        /// <see cref="DefaultEnableRenderCache"/> overrides this when set.
        /// </summary>
        public bool EnableRenderCache { get; set; }

        private bool CacheEnabled => EnableRenderCache || DefaultEnableRenderCache;

        /// <summary>
        /// Bump the render version, forcing the next frame to rebuild the
        /// cached command stream. Safe to call every frame; the rebuild only
        /// actually runs on the next <see cref="EmitCommandsInto"/> call.
        /// </summary>
        internal void InvalidateRenderCache()
        {
            _renderVersion++;
        }

        /// <summary>
        /// Emit this gump's draw commands into the supplied <see cref="RenderLists"/>.
        /// If <see cref="EnableRenderCache"/> is true and the cache is current,
        /// replays the cached command stream without re-walking the control tree.
        /// Otherwise rebuilds the cache by calling <see cref="AddToRenderLists"/>.
        /// <para/>
        /// When the gump has only moved since the cache was built (pure translation,
        /// e.g. drag), the cached commands are replayed with a per-command offset
        /// applied — no rebuild of the child tree. Gumps whose cache contains any
        /// <see cref="GumpCommandKind.Callback"/> entries can't use the translation
        /// fast path because closures hold frozen position captures, so those fall
        /// through to a full rebuild instead.
        /// </summary>
        internal void EmitCommandsInto(RenderLists target, int gumpX, int gumpY, ref float layerDepth)
        {
            GumpRenderMetrics.GumpsRendered++;

            if (!CacheEnabled)
            {
                GumpRenderMetrics.CacheBypassed++;
                AddToRenderLists(target, gumpX, gumpY, ref layerDepth);
                return;
            }

            // Snapshot the entry depth before AddToRenderLists advances it: this is the
            // gump's current depth slot, what each command's LayerDepth needs to be
            // anchored to. We'll compare against _cachedAtDepth to detect z-stack moves.
            float gumpStartDepth = layerDepth;

            bool cacheStale = _renderCache == null || _renderVersion != _lastBuiltRenderVersion;
            bool positionChanged = gumpX != _cachedAtX || gumpY != _cachedAtY;
            bool depthChanged = gumpStartDepth != _cachedAtDepth;

            // A cached gump that only moved (in screen space or z-stack) can skip the
            // rebuild if and only if none of its cached commands is a Callback —
            // closures freeze captured positions AND captured depths, so neither a
            // translation nor a depth shift can correctly update them.
            if (!cacheStale && (positionChanged || depthChanged) && _cacheContainsCallback)
            {
                cacheStale = true;
            }

            if (cacheStale)
            {
                GumpRenderMetrics.CacheMisses++;
                RebuildRenderCache(target, gumpX, gumpY, gumpStartDepth, ref layerDepth);
                return;
            }

            if (positionChanged || depthChanged)
            {
                GumpRenderMetrics.CacheTranslationHits++;
                int dx = gumpX - _cachedAtX;
                int dy = gumpY - _cachedAtY;
                float dz = gumpStartDepth - _cachedAtDepth;

                // Emit the translated commands into the per-frame stream.
                target.AppendCommandsTranslated(_renderCache, dx, dy, dz);

                // Rebake the cache in-place so subsequent frames can take the
                // zero-delta direct-replay path instead of re-translating. This
                // is an O(commands) walk — the same cost we just paid for the
                // translated append — not a net regression.
                for (int i = 0; i < _renderCache.Count; i++)
                {
                    _renderCache[i] = _renderCache[i].WithOffset(dx, dy, dz);
                }
                _cachedAtX = gumpX;
                _cachedAtY = gumpY;
                _cachedAtDepth = gumpStartDepth;
                return;
            }

            // Position unchanged and cache fresh: direct replay.
            GumpRenderMetrics.CacheHits++;
            target.AppendCommands(_renderCache);
        }

        private void RebuildRenderCache(RenderLists target, int gumpX, int gumpY, float gumpStartDepth, ref float layerDepth)
        {
            int startIndex = target.GumpCommandCount;
            AddToRenderLists(target, gumpX, gumpY, ref layerDepth);
            int endIndex = target.GumpCommandCount;

            _renderCache ??= new List<GumpDrawCommand>(endIndex - startIndex);
            _renderCache.Clear();
            _cacheContainsCallback = false;
            for (int i = startIndex; i < endIndex; i++)
            {
                var cmd = target.PeekGumpCommand(i);
                _renderCache.Add(cmd);
                if (cmd.Kind == GumpCommandKind.Callback)
                {
                    _cacheContainsCallback = true;
                }
            }

            _lastBuiltRenderVersion = _renderVersion;
            _cachedAtX = gumpX;
            _cachedAtY = gumpY;
            _cachedAtDepth = gumpStartDepth;

#if DEBUG
            if (_cacheContainsCallback && EnableRenderCache)
            {
                var type = GetType();
                if (!s_cachedCallbackAllowed.Contains(type) && s_cachedCallbackWarned.Add(type))
                {
                    Log.Warn(
                        $"[RenderCache] {type.Name} emits a Callback command with EnableRenderCache=true; " +
                        "this forces a full rebuild every drag/resize frame. Convert closures to typed commands or " +
                        "add this type to Gump.s_cachedCallbackAllowed if the Callback is genuinely per-frame data."
                    );
                }
            }
#endif
        }

        public override void OnButtonClick(int buttonID)
        {
            if (!IsDisposed && LocalSerial != 0)
            {
                List<uint> switches = new List<uint>();
                List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked:
                            switches.Add(control.LocalSerial);

                            break;

                        case StbTextBox textBox:
                            entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));

                            break;
                    }
                }

                GameActions.ReplyGump
                (
                    LocalSerial,
                    // Seems like MasterGump serial does not work as expected.
                    /*MasterGumpSerial != 0 ? MasterGumpSerial :*/ ServerSerial,
                    buttonID,
                    switches.ToArray(),
                    entries.ToArray()
                );

                if (CanMove)
                {
                    UIManager.SavePosition(ServerSerial, Location);
                }
                else
                {
                    UIManager.RemovePosition(ServerSerial);
                }

                Dispose();
            }
        }

        protected override void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
            {
                return;
            }

            if (ServerSerial != 0)
            {
                OnButtonClick(0);
            }

            base.CloseWithRightClick();
        }

        public override void ChangePage(int pageIndex)
        {
            // For a gump, Page is the page that is drawing.
            ActivePage = pageIndex;
        }
    }
}