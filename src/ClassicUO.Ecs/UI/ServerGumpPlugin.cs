// Server-pushed generic gumps (packets 0xB0 + 0xDD). The packet carries
// a layout string (gump-command DSL) and a string table for {text N} refs.
// The layout is tokenized + walked here and turned into Bevy.UI entities.
//
// Scope (v1):
//   * page, group, endgroup tracked. Page 0 is the default layer (always
//     visible); pages 1+ are mutually exclusive, active page defaults to 1
//     (matches OOP). Page-switch buttons (action=0) write CurrentPage on the
//     root; a PostUpdate sync flips each child's Node.Display to match.
//   * resizepic / gumppic / gumppichued / gumppicphued / gumppictiled /
//     tilepic / picinpic — sprite layout.
//   * button (action=1) → Send_GumpResponse(sender, gumpId, buttonId,
//     switches, entries). Switches and entry-text capture not wired yet —
//     we send empty arrays, matching what ModernUO admin menus need.
//   * text, croppedtext, htmlgump, xmfhtmlgump, xmfhtmlgumpcolor, xmfhtmltok
//     — labels (no HTML markup parsing; plain text only).
//   * checkbox, radio, textentry — rendered as bg sprite + placeholder label;
//     state capture deferred until the corresponding Bevy.UI widget lands.
//   * tooltip, itemproperty, noresize, mastergump, togglelimitgumpscale —
//     no-op for now.
//   * noclose / nodispose / nomove — flag the root (nomove tags UINoDrag, which
//     suppresses drag only; the gump stays a window for close / click-capture).

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct ServerGumpPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ServerGumpPositions());
        app.AddResource(new ServerGumpRegistry());
        app.AddObserver<On<PacketReceived<OnOpenGumpPacket_0xB0>>, Commands, ServerGumpParams>(SpawnOnB0);
        app.AddObserver<On<PacketReceived<OnOpenCompressedGumpPacket_0xDD>>, Commands, ServerGumpParams>(SpawnOnDD);

        // Per-frame page-visibility sync. Each ServerGumpChild reads its
        // root's CurrentPage and flips its own Node.Display in-place. Cheap:
        // server gumps spawn a few dozen children at most. Reading via
        // Query<Data<ServerGump>>.Get is O(1).
        //
        // Page 0 is the DEFAULT layer — always visible regardless of the active
        // page (mirrors OOP Control.AddToRenderLists: `c.Page == 0 || c.Page ==
        // ActivePage`). Most server gumps put ALL content on page 0 and never
        // switch. Pages 1+ are mutually exclusive among themselves: a page-N
        // child shows only when CurrentPage == N. CurrentPage defaults to 1 (OOP
        // Gump.Update forces ActivePage 0 → 1) so a gump's `page 1` block (if
        // any) is part of the initial view alongside page 0.
        Action<Query<Data<ServerGumpChild, Node>>, Query<Data<ServerGump>>> syncFn =
            (childQ, gumpQ) =>
            {
                foreach (var (_, child, node) in childQ)
                {
                    if (!gumpQ.Contains(child.Ref.RootEntity)) continue;
                    var (_, gump) = gumpQ.Get(child.Ref.RootEntity);
                    bool visible = child.Ref.Page == 0 || child.Ref.Page == gump.Ref.CurrentPage;
                    var target = visible ? Display.Flex : Display.None;
                    if (node.Ref.Display != target)
                        node.Ref.Display = target;
                }
            };
        app.AddSystem(syncFn).InStage(Stage.PostUpdate).Build();

        // Apply switchpage requests: a switchpage button's observer tags its
        // root with ServerGumpPageRequest (via Commands); here we copy it into
        // CurrentPage and drop the request. Runs in Update, before the
        // PostUpdate visibility sync.
        Action<Query<Data<ServerGump, ServerGumpPageRequest>>, Commands> applyPageFn =
            (q, cmd) =>
            {
                foreach (var (ent, gump, req) in q)
                {
                    gump.Ref.CurrentPage = req.Ref.Page;
                    cmd.Entity(ent.Ref).Remove<ServerGumpPageRequest>();
                }
            };
        app.AddSystem(applyPageFn).InStage(Stage.Update).Build();

        // Slide each scrollbar thumb to match its text container's live scroll
        // offset. LayoutSystem writes ScrollPosition.OffsetY post-layout from
        // Clay's ScrollContainerData; we read it here (PostUpdate) and set the
        // thumb's Node.Top. One-frame lag is invisible for a scrollbar.
        Action<Query<Data<ServerGumpScrollThumb, Node>>, Query<Data<ScrollPosition>>> thumbFn =
            (thumbQ, scrollQ) =>
            {
                foreach (var (_, thumb, node) in thumbQ)
                {
                    float ratio = 0f;
                    if (thumb.Ref.MaxScroll > 0f && scrollQ.Contains(thumb.Ref.ScrollEntity))
                    {
                        var (_, sp) = scrollQ.Get(thumb.Ref.ScrollEntity);
                        ratio = Math.Clamp(Math.Abs(sp.Ref.OffsetY) / thumb.Ref.MaxScroll, 0f, 1f);
                    }
                    var target = thumb.Ref.TrackTop + ratio * thumb.Ref.Travel;
                    if (node.Ref.Top.Value != target)
                        node.Ref.Top = Val.Px(target);
                }
            };
        app.AddSystem(thumbFn).InStage(Stage.PostUpdate).Build();

        // Drag the scrollbar thumb to scroll. Runs in PreUpdate (before
        // WindowDragPlugin.Drag in Update) so a press that lands on a thumb
        // claims the shared DragGate first — WindowDragPlugin then sees the
        // gate taken and won't drag the whole window instead. Delta-based:
        // map vertical mouse travel through (MaxScroll / Travel) onto the
        // container's ScrollPosition.OffsetY.
        var thumbDragFn = ScrollThumbDrag;
        app.AddSystem(thumbDragFn).InStage(Stage.PreUpdate).Build();

#if AGENT_BUILD
        app.AddResource(new DebugServerGumpQueue());
        Action<Commands, Res<DebugServerGumpQueue>> drainFn = DrainDebugGumps;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

#if AGENT_BUILD
    // Drains debug.openServerGump requests by emitting the same PacketReceived
    // trigger the network path fires, so the gump is built through the real
    // 0xB0 parse + BuildGump path (deterministic harness repro of server gumps).
    private static void DrainDebugGumps(Commands commands, Res<DebugServerGumpQueue> q)
    {
        if (q.Value.Pending.Count == 0) return;
        foreach (var r in q.Value.Pending)
            commands.EmitTrigger(new PacketReceived<OnOpenGumpPacket_0xB0>
            {
                Packet = BuildDebugPacket(r.GumpId, r.X, r.Y, r.Layout, r.Lines)
            });
        q.Value.Pending.Clear();
    }

    private static OnOpenGumpPacket_0xB0 BuildDebugPacket(uint gumpId, int x, int y, string layout, string[] lines)
    {
        // Serialize into the real 0xB0 wire body (sender onward; id+len are
        // stripped by the dispatcher) and Fill() — exact network parse path.
        var buf = new List<byte>();
        void U16(int v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        U32(0);                 // sender
        U32(gumpId);
        U32((uint)x); U32((uint)y);
        U16(layout.Length); foreach (var c in layout) buf.Add((byte)c);       // ASCII command
        U16(lines.Length);
        foreach (var ln in lines) { U16(ln.Length); foreach (var c in ln) U16(c); } // unicode BE
        var pkt = new OnOpenGumpPacket_0xB0();
        pkt.Fill(new StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif

    private struct ScrollThumbAnchor
    {
        public bool Active;
        public bool ClaimedGate;
        public ulong ScrollEntity;
        public float MouseY0;
        public float OffsetY0;
        public float Travel;
        public float MaxScroll;
    }

    private static void ScrollThumbDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<ScrollThumbAnchor> anchor,
        Query<Data<ServerGumpScrollThumb, ComputedNode, Node>> thumbsQ,
        Query<Data<ScrollPosition>> scrollQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left)
                 || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (anchor.Value.ClaimedGate && gate.Value.Mode == ActiveDrag.UIWindow)
                gate.Value.Mode = ActiveDrag.None;
            anchor.Value.Active = false;
            anchor.Value.ClaimedGate = false;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return; // another drag owns the gesture
            var pos = mouse.Value.Position;
            foreach (var (_, thumb, cn, node) in thumbsQ)
            {
                if (node.Ref.Display == Display.None) continue;
                if (thumb.Ref.MaxScroll <= 0f || thumb.Ref.Travel <= 0f) continue;
                var bb = cn.Ref;
                if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) continue;
                if (pos.X >= bb.Position.X + bb.Size.X || pos.Y >= bb.Position.Y + bb.Size.Y) continue;

                float off0 = 0f;
                if (scrollQ.Contains(thumb.Ref.ScrollEntity))
                {
                    var (_, sp0) = scrollQ.Get(thumb.Ref.ScrollEntity);
                    off0 = sp0.Ref.OffsetY;
                }
                anchor.Value = new ScrollThumbAnchor
                {
                    Active = true,
                    ClaimedGate = true,
                    ScrollEntity = thumb.Ref.ScrollEntity,
                    MouseY0 = pos.Y,
                    OffsetY0 = off0,
                    Travel = thumb.Ref.Travel,
                    MaxScroll = thumb.Ref.MaxScroll,
                };
                gate.Value.Mode = ActiveDrag.UIWindow;
                break;
            }
        }

        if (anchor.Value.Active && anchor.Value.Travel > 0f && scrollQ.Contains(anchor.Value.ScrollEntity))
        {
            var (_, sp) = scrollQ.Get(anchor.Value.ScrollEntity);
            float delta = mouse.Value.Position.Y - anchor.Value.MouseY0;
            sp.Ref.OffsetY = Math.Clamp(
                anchor.Value.OffsetY0 + delta * (anchor.Value.MaxScroll / anchor.Value.Travel),
                0f, anchor.Value.MaxScroll);
        }
    }

    private static void SpawnOnB0(
        On<PacketReceived<OnOpenGumpPacket_0xB0>> trig,
        Commands commands,
        ServerGumpParams p)
    {
        var pkt = trig.Event.Packet;
        var lines = new string[pkt.Lines.Count];
        for (var i = 0; i < pkt.Lines.Count; i++)
            lines[i] = pkt.Lines[i].Text ?? string.Empty;

        BuildGump(commands, p, pkt.Sender, pkt.GumpId, pkt.X, pkt.Y, pkt.Command ?? string.Empty, lines);
    }

    private static void SpawnOnDD(
        On<PacketReceived<OnOpenCompressedGumpPacket_0xDD>> trig,
        Commands commands,
        ServerGumpParams p)
    {
        var pkt = trig.Event.Packet;

        // Layout: zlib → utf-8 string.
        string layout = string.Empty;
        if (pkt.LayoutCompressedLength > 0 && pkt.LayoutDecompressedLength > 0)
        {
            var dl = (int)pkt.LayoutDecompressedLength;
            var buf = System.Buffers.ArrayPool<byte>.Shared.Rent(dl);
            try
            {
                var res = ZLib.Decompress(pkt.LayoutData.AsSpan(0, (int)pkt.LayoutCompressedLength), buf.AsSpan(0, dl));
                if (res == ZLib.ZLibError.Ok)
                    layout = System.Text.Encoding.UTF8.GetString(buf, 0, dl);
            }
            finally { System.Buffers.ArrayPool<byte>.Shared.Return(buf); }
        }

        // Lines: zlib → walk (ushort len BE, unicode BE chars).
        var lines = new string[pkt.LinesCount];
        if (pkt.LinesCount > 0 && pkt.LinesCompressedLength > 0 && pkt.LinesDecompressedLength > 0)
        {
            var dl = (int)pkt.LinesDecompressedLength;
            var buf = System.Buffers.ArrayPool<byte>.Shared.Rent(dl);
            try
            {
                var res = ZLib.Decompress(pkt.LinesData.AsSpan(0, (int)pkt.LinesCompressedLength), buf.AsSpan(0, dl));
                if (res == ZLib.ZLibError.Ok)
                {
                    var r = new StackDataReader(buf.AsSpan(0, dl));
                    for (var i = 0; i < pkt.LinesCount; i++)
                    {
                        if (r.Remaining < 2) { lines[i] = string.Empty; continue; }
                        int len = r.ReadUInt16BE();
                        lines[i] = len > 0 ? r.ReadUnicodeBE(len) : string.Empty;
                    }
                }
            }
            finally { System.Buffers.ArrayPool<byte>.Shared.Return(buf); }
        }

        BuildGump(commands, p, pkt.Sender, pkt.GumpId, pkt.X, pkt.Y, layout, lines);
    }

    private static void BuildGump(
        Commands commands,
        ServerGumpParams p,
        uint sender,
        uint gumpId,
        int x,
        int y,
        string layout,
        string[] lines)
    {
        // Tokenize the outer layout into command groups (one per {...} block).
        var outerParser = new TextFileParser(string.Empty, new[] { ' ' }, Array.Empty<char>(), new[] { '{', '}' });
        var cmdList = outerParser.GetTokens(layout);
        if (cmdList.Count == 0) return;

        // Replace-in-place: if a gump with this gumpId is already open, capture
        // its live screen position (honours user drags), despawn it (children
        // cascade via the relationship's DeleteDescendants), and rebuild at the
        // same spot. Mirrors OOP CreateGump's existing-gump reuse + position cache.
        //
        // Track open roots in a SYNCHRONOUS registry (gumpId → root entity), not
        // by querying ServerGump. The server bursts several pushes of the same
        // gump in one frame (fresh sender serial each: 0x00103EFD → 0x00104C81 →
        // …). Spawn/despawn go through Commands and only apply at the next sync
        // point, so a query can't see roots spawned by earlier pushes this frame
        // and they pile up. The registry is updated in-place here, so each push
        // sees the prior one immediately.
        var posKey = (ulong)gumpId;
        if (p.Registry.Value.ByGumpId.TryGetValue(gumpId, out var oldRoot))
        {
            // Only touch oldRoot if it's STILL a live ServerGump. The handle
            // carries a generation; if the entity was already despawned (e.g. an
            // activate button closed it) its id may have been recycled to an
            // unrelated entity with generation+1. Contains() checks the
            // generation, so a stale handle returns false — we must NOT despawn
            // it, or we'd nuke whatever live entity now holds that raw id.
            if (p.ExistingQ.Contains(oldRoot))
            {
                var (_, _, oldNode) = p.ExistingQ.Get(oldRoot);
                p.Positions.Value.ByKey[posKey] = new Vector2(oldNode.Ref.Left.Value, oldNode.Ref.Top.Value);
                commands.Entity(oldRoot).Despawn();
            }
            p.Registry.Value.ByGumpId.Remove(gumpId);
        }

        // Reuse the cached position on reopen / update; otherwise seed the cache
        // with the server-supplied origin so the next update lands in place.
        if (p.Positions.Value.ByKey.TryGetValue(posKey, out var cachedPos))
        {
            x = (int)cachedPos.X;
            y = (int)cachedPos.Y;
        }
        else
        {
            p.Positions.Value.ByKey[posKey] = new Vector2(x, y);
        }

        // Inner parser for each individual command — splits on space/comma,
        // tolerates @-quoted strings (used by xmfhtmltok args).
        var innerParser = new TextFileParser(string.Empty, new[] { ' ', ',' }, Array.Empty<char>(), new[] { '@', '@' });

        // Root: an empty entity that only carries the ServerGump marker for
        // dedup + child despawn lookup. The first resizepic encountered below
        // upgrades this entity with UOGumpBundle (UOCustomRender + UIMovable +
        // GlobalZIndex + Node sized to the bg sprite). For gumps without a
        // resizepic, the bbox tracker installs a sized Node at the end so the
        // root still has a hit-test surface.
        var z = p.ZCounter.Value.Bump();
        // CurrentPage = 1: UO's default active page is 1 (OOP Gump.Update bumps
        // ActivePage 0 → 1). Page-0 children are shared/always-visible; the
        // first `page 1` block is the initial view. A switchpage button sets
        // CurrentPage = N — see the visibility sync in Build.
        var rootCmd = commands.Spawn()
            .Insert(new ServerGump { Sender = sender, GumpId = gumpId, CurrentPage = 1 });

        var rootId = rootCmd.Id;
        // Synchronous registry update so a same-frame re-push despawns THIS root.
        p.Registry.Value.ByGumpId[gumpId] = rootId;

        int page = 0;
        int group = 0;
        bool nomove = false;
        float maxRight = 0f;
        float maxBottom = 0f;

        for (var cnt = 0; cnt < cmdList.Count; cnt++)
        {
            var gparams = innerParser.GetTokens(cmdList[cnt], false);
            if (gparams.Count == 0) continue;
            var entry = gparams[0];

            if (Eq(entry, "page"))
            {
                if (gparams.Count >= 2 && int.TryParse(gparams[1], out var pg)) page = pg;
                continue;
            }
            if (Eq(entry, "group") || Eq(entry, "endgroup")) { group++; continue; }
            if (Eq(entry, "nomove")) { nomove = true; continue; }
            if (Eq(entry, "noclose") || Eq(entry, "nodispose")) continue;
            if (Eq(entry, "noresize") || Eq(entry, "mastergump") || Eq(entry, "togglelimitgumpscale")) continue;
            if (Eq(entry, "tooltip") || Eq(entry, "itemproperty")) continue;
            if (entry == "\0") break;

            ulong childId = 0;

            int cx0 = 0, cy0 = 0, cw0 = 0, ch0 = 0;

            if (Eq(entry, "resizepic"))
            {
                // resizepic x y id w h
                if (gparams.Count >= 6 &&
                    int.TryParse(gparams[1], out var rx) && int.TryParse(gparams[2], out var ry) &&
                    ushort.TryParse(gparams[3], out var rid) &&
                    int.TryParse(gparams[4], out var rw) && int.TryParse(gparams[5], out var rh))
                {
                    // Background panel — a normal child at its gump-local
                    // (rx,ry). The root is a frame at the gump origin (x,y) so
                    // every child sits at its true gump-local coord; see the
                    // root-frame install at the end. (Earlier the first
                    // resizepic became the root AT (x+rx,y+ry), which shifted
                    // every other control by (rx,ry) and pushed edge content —
                    // text, CANCEL — outside the panel on gumps with a nonzero
                    // resizepic origin.)
                    childId = p.Builder.Value.AddGumpNinePatch(commands, rid, Vector3.UnitZ,
                        new Vector2(rx, ry), new Vector2(rw, rh)).Id;
                    cx0 = rx; cy0 = ry; cw0 = rw; ch0 = rh;
                }
            }
            else if (Eq(entry, "gumppic") || Eq(entry, "tilepicasgumppic"))
            {
                if (gparams.Count >= 4 &&
                    int.TryParse(gparams[1], out var gx) && int.TryParse(gparams[2], out var gy) &&
                    ushort.TryParse(gparams[3], out var gid))
                {
                    var hue = ParseHueArg(gparams, 4);
                    var info = p.Assets.Value.Gumps.GetGump(gid);
                    childId = p.Builder.Value.AddGump(commands, gid, ToShaderHue(hue), new Vector2(gx, gy)).Id;
                    cx0 = gx; cy0 = gy; cw0 = info.UV.Width; ch0 = info.UV.Height;
                }
            }
            else if (Eq(entry, "gumppichued") || Eq(entry, "gumppicphued"))
            {
                if (gparams.Count >= 5 &&
                    int.TryParse(gparams[1], out var gx) && int.TryParse(gparams[2], out var gy) &&
                    ushort.TryParse(gparams[3], out var gid))
                {
                    var hue = UInt16Converter.Parse(gparams[4]);
                    var info = p.Assets.Value.Gumps.GetGump(gid);
                    childId = p.Builder.Value.AddGump(commands, gid, ToShaderHue(hue), new Vector2(gx, gy)).Id;
                    cx0 = gx; cy0 = gy; cw0 = info.UV.Width; ch0 = info.UV.Height;
                }
            }
            else if (Eq(entry, "gumppictiled"))
            {
                if (gparams.Count >= 6 &&
                    int.TryParse(gparams[1], out var gx) && int.TryParse(gparams[2], out var gy) &&
                    int.TryParse(gparams[3], out var gw) && int.TryParse(gparams[4], out var gh) &&
                    ushort.TryParse(gparams[5], out var gid))
                {
                    childId = p.Builder.Value.AddGumpTiled(commands, gid, Vector3.UnitZ,
                        new Vector2(gx, gy), new Vector2(gw, gh)).Id;
                    cx0 = gx; cy0 = gy; cw0 = gw; ch0 = gh;
                }
            }
            else if (Eq(entry, "tilepic") || Eq(entry, "tilepichue"))
            {
                if (gparams.Count >= 4 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    ushort.TryParse(gparams[3], out var tg))
                {
                    var hue = gparams.Count >= 5 ? UInt16Converter.Parse(gparams[4]) : (ushort)0;
                    var info = p.Assets.Value.Arts.GetArt(tg);
                    childId = p.Builder.Value.AddArt(commands, tg, ToShaderHue(hue), new Vector2(tx, ty)).Id;
                    cx0 = tx; cy0 = ty; cw0 = info.UV.Width; ch0 = info.UV.Height;
                }
            }
            else if (Eq(entry, "button"))
            {
                if (gparams.Count >= 5 &&
                    int.TryParse(gparams[1], out var bx) && int.TryParse(gparams[2], out var by) &&
                    ushort.TryParse(gparams[3], out var normal) && ushort.TryParse(gparams[4], out var pressed))
                {
                    var action = gparams.Count >= 6 ? SafeInt(gparams[5]) : 0;
                    var toPage = gparams.Count >= 7 ? SafeInt(gparams[6]) : 0;
                    var btnId = gparams.Count >= 8 ? SafeInt(gparams[7]) : 0;

                    var btn = p.Builder.Value.AddButton(commands, (normal, pressed, normal),
                        Vector3.UnitZ, new Vector2(bx, by));

                    var capturedSender = sender;
                    var capturedGumpId = gumpId;
                    var capturedBtnId = btnId;
                    var capturedRootId = rootId;
                    var capturedToPage = toPage;

                    if (action != 0)
                    {
                        // Activate: reply to server, then close the gump. OOP
                        // Gump.OnButtonClick disposes after ReplyGump — any
                        // activate button dismisses its own window (the server
                        // pushes the follow-up gump separately).
                        btn.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, ResMut<ServerGumpRegistry> reg,
                            Query<Data<ServerGumpTextEntry, Text>> entriesQ) =>
                        {
                            net.Value.Send_GumpResponse(capturedSender, capturedGumpId, capturedBtnId,
                                Array.Empty<uint>(), CollectTextEntries(capturedRootId, entriesQ));
                            // Drop the registry entry so a later push of this
                            // gumpId never despawns this now-dead (recycled) id.
                            if (reg.Value.ByGumpId.TryGetValue(capturedGumpId, out var r) && r == capturedRootId)
                                reg.Value.ByGumpId.Remove(capturedGumpId);
                            cmd.Entity(capturedRootId).Despawn();
                        });
                    }
                    else
                    {
                        // SwitchPage: tag the root with a page request via
                        // Commands; ApplyPageRequests (Stage.Update) writes it
                        // to CurrentPage, then SyncPageVisibility re-displays.
                        btn.Observe((On<UiClick> _, Commands cmd) =>
                            cmd.Entity(capturedRootId).Insert(new ServerGumpPageRequest { Page = capturedToPage }));
                    }
                    childId = btn.Id;

                    var binfo = p.Assets.Value.Gumps.GetGump(normal);
                    cx0 = bx; cy0 = by; cw0 = binfo.UV.Width; ch0 = binfo.UV.Height;
                }
            }
            else if (Eq(entry, "text"))
            {
                // text x y hue lineId
                if (gparams.Count >= 5 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    ushort.TryParse(gparams[3], out var thue) &&
                    int.TryParse(gparams[4], out var lid))
                {
                    var text = SafeLine(lines, lid);
                    var color = HueToClayColor(p.Files.Value.Hues, thue);
                    childId = SpawnText(commands, new Vector2(tx, ty), text, color);
                    cx0 = tx; cy0 = ty; cw0 = 0; ch0 = 16;
                }
            }
            else if (Eq(entry, "croppedtext"))
            {
                if (gparams.Count >= 7 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    int.TryParse(gparams[3], out var tw) && int.TryParse(gparams[4], out var th) &&
                    ushort.TryParse(gparams[5], out var thue) &&
                    int.TryParse(gparams[6], out var lid))
                {
                    var text = SafeLine(lines, lid);
                    childId = SpawnWrappedText(commands, new Vector2(tx, ty), new Vector2(tw, th), text, thue, false, false, isHtml: false, out _);
                    cx0 = tx; cy0 = ty; cw0 = tw; ch0 = th;
                }
            }
            else if (Eq(entry, "htmlgump"))
            {
                // htmlgump x y w h lineId hasBg hasScroll
                if (gparams.Count >= 6 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    int.TryParse(gparams[3], out var tw) && int.TryParse(gparams[4], out var th) &&
                    int.TryParse(gparams[5], out var lid))
                {
                    bool hasBg = gparams.Count >= 7 && gparams[6] == "1";
                    bool hasScroll = gparams.Count >= 8 && gparams[7] != "0";
                    if (hasBg)
                    {
                        var bg = SpawnHtmlBackground(commands, p.Builder.Value,
                            new Vector2(tx, ty), new Vector2(tw, th), hasScroll, page, group, rootId);
                        if (tx + tw > maxRight)  maxRight  = tx + tw;
                        if (ty + th > maxBottom) maxBottom = ty + th;
                    }
                    var text = SafeLine(lines, lid);
                    var (txtX, txtY, txtW, txtH) = HtmlInnerRect(tx, ty, tw, th, hasBg, hasScroll);
                    childId = SpawnWrappedText(commands, new Vector2(txtX, txtY), new Vector2(txtW, txtH), text, 0, hasBg, hasScroll, isHtml: true, out var contentH);
                    if (hasScroll)
                        SpawnScrollBar(commands, p.Builder.Value, p.Assets.Value,
                            new Vector2(tx, ty), new Vector2(tw, th), page, group, rootId,
                            scrollEntity: childId, maxScroll: Math.Max(0, contentH - txtH));
                    cx0 = tx; cy0 = ty; cw0 = tw; ch0 = th;
                }
            }
            else if (Eq(entry, "xmfhtmlgump") || Eq(entry, "xmfhtmlgumpcolor"))
            {
                if (gparams.Count >= 6 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    int.TryParse(gparams[3], out var tw) && int.TryParse(gparams[4], out var th))
                {
                    bool hasBg = gparams.Count >= 7 && gparams[6] == "1";
                    bool hasScroll = gparams.Count >= 8 && gparams[7] != "0";
                    if (hasBg)
                    {
                        var bg = SpawnHtmlBackground(commands, p.Builder.Value,
                            new Vector2(tx, ty), new Vector2(tw, th), hasScroll, page, group, rootId);
                        if (tx + tw > maxRight)  maxRight  = tx + tw;
                        if (ty + th > maxBottom) maxBottom = ty + th;
                    }
                    var cliloc = ParseClilocId(gparams[5]);
                    var text = p.Files.Value.Clilocs.GetString(cliloc) ?? string.Empty;
                    // xmfhtmlgumpcolor carries an HTML start colour in gparams[8];
                    // 0x7FFF is the "default white" sentinel (OOP remaps to
                    // 0x00FFFFFF). Plain xmfhtmlgump has no colour → 0.
                    int htmlHue = 0;
                    if (Eq(entry, "xmfhtmlgumpcolor") && gparams.Count >= 9 && int.TryParse(gparams[8], out var hc))
                        htmlHue = hc == 0x7FFF ? 0x00FFFFFF : hc;
                    var (txtX, txtY, txtW, txtH) = HtmlInnerRect(tx, ty, tw, th, hasBg, hasScroll);
                    childId = SpawnWrappedText(commands, new Vector2(txtX, txtY), new Vector2(txtW, txtH), text, htmlHue, hasBg, hasScroll, isHtml: true, out var contentH);
                    if (hasScroll)
                        SpawnScrollBar(commands, p.Builder.Value, p.Assets.Value,
                            new Vector2(tx, ty), new Vector2(tw, th), page, group, rootId,
                            scrollEntity: childId, maxScroll: Math.Max(0, contentH - txtH));
                    cx0 = tx; cy0 = ty; cw0 = tw; ch0 = th;
                }
            }
            else if (Eq(entry, "xmfhtmltok"))
            {
                // xmfhtmltok x y w h hasBg hasScroll color clilocId @arg1@ ...
                if (gparams.Count >= 9 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    int.TryParse(gparams[3], out var tw) && int.TryParse(gparams[4], out var th))
                {
                    bool hasBg = gparams[5] == "1";
                    bool hasScroll = gparams[6] != "0";
                    // gparams[7] is the HTML start colour (0x7FFF → default white).
                    int htmlHue = 0;
                    if (int.TryParse(gparams[7], out var hc))
                        htmlHue = hc == 0x7FFF ? 0x00FFFFFF : hc;
                    if (hasBg)
                    {
                        var bg = SpawnHtmlBackground(commands, p.Builder.Value,
                            new Vector2(tx, ty), new Vector2(tw, th), hasScroll, page, group, rootId);
                        if (tx + tw > maxRight)  maxRight  = tx + tw;
                        if (ty + th > maxBottom) maxBottom = ty + th;
                    }
                    var cliloc = ParseClilocId(gparams[8]);
                    string text;
                    if (gparams.Count > 9)
                    {
                        var sb = new System.Text.StringBuilder();
                        for (var i = 9; i < gparams.Count; i++) { sb.Append('\t').Append(gparams[i]); }
                        text = p.Files.Value.Clilocs.Translate(cliloc, sb.ToString().Trim('@').Replace('@', '\t')) ?? string.Empty;
                    }
                    else
                    {
                        text = p.Files.Value.Clilocs.GetString(cliloc) ?? string.Empty;
                    }
                    var (txtX, txtY, txtW, txtH) = HtmlInnerRect(tx, ty, tw, th, hasBg, hasScroll);
                    childId = SpawnWrappedText(commands, new Vector2(txtX, txtY), new Vector2(txtW, txtH), text, htmlHue, hasBg, hasScroll, isHtml: true, out var contentH);
                    if (hasScroll)
                        SpawnScrollBar(commands, p.Builder.Value, p.Assets.Value,
                            new Vector2(tx, ty), new Vector2(tw, th), page, group, rootId,
                            scrollEntity: childId, maxScroll: Math.Max(0, contentH - txtH));
                    cx0 = tx; cy0 = ty; cw0 = tw; ch0 = th;
                }
            }
            else if (Eq(entry, "checkbox") || Eq(entry, "radio"))
            {
                if (gparams.Count >= 5 &&
                    int.TryParse(gparams[1], out var cx) && int.TryParse(gparams[2], out var cy) &&
                    ushort.TryParse(gparams[3], out var uncheckedId) && ushort.TryParse(gparams[4], out var checkedId))
                {
                    var initial = gparams.Count >= 6 && SafeInt(gparams[5]) != 0;
                    var assetId = initial ? checkedId : uncheckedId;
                    var info = p.Assets.Value.Gumps.GetGump(assetId);
                    childId = p.Builder.Value.AddGump(commands, assetId, Vector3.UnitZ, new Vector2(cx, cy)).Id;
                    cx0 = cx; cy0 = cy; cw0 = info.UV.Width; ch0 = info.UV.Height;
                }
            }
            else if (Eq(entry, "textentry") || Eq(entry, "textentrylimited"))
            {
                // textentry x y width height hue entryID textID [limit]
                // entryID (gparams[6]) is echoed back in the gump response;
                // textID (gparams[7]) is the initial text line.
                if (gparams.Count >= 8 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    int.TryParse(gparams[3], out var tw) && int.TryParse(gparams[4], out var th) &&
                    ushort.TryParse(gparams[5], out var thue) &&
                    ushort.TryParse(gparams[6], out var entryId) &&
                    int.TryParse(gparams[7], out var lid))
                {
                    var text = SafeLine(lines, lid);

                    // The hollow frame is the bounds-hittable focus region; the
                    // shared SpawnTextField hangs the editable glyph + caret +
                    // click-to-focus row off it (same primitive as the login
                    // fields). Tag the glyph with ServerGumpTextEntry so the
                    // gump-response button collects its value, and decorate all
                    // sub-entities with ServerGumpChild so page visibility hides
                    // them with the rest of the page.
                    var capRoot = rootId; var capPage = page; var capGroup = group;
                    var frame = commands.Spawn()
                        .Insert(new Node
                        {
                            Display = Display.Flex,
                            PositionType = PositionType.Absolute,
                            Left = Val.Px(tx), Top = Val.Px(ty),
                            Width = Val.Px(tw), Height = Val.Px(th),
                        });
                    var entryFont = new TextFont { FontId = UoFontRuntime.DefaultFont, Size = 18 };
                    var glyphId = GuiPlugin.SpawnTextField(
                        commands, frame, new Vector2(2, 2), entryFont, thue, text, masked: false,
                        decorate: e => e.Insert(new ServerGumpChild { RootEntity = capRoot, Page = capPage, Group = capGroup }));
                    commands.Entity(glyphId).Insert(new ServerGumpTextEntry { RootEntity = capRoot, EntryId = entryId });
                    childId = frame.Id;
                    cx0 = tx; cy0 = ty; cw0 = tw; ch0 = th;
                }
            }
            else if (Eq(entry, "picinpic") || Eq(entry, "picinpichued") || Eq(entry, "picinpicphued"))
            {
                // picinpic x y id sx sy sw sh — render the source sprite at dest (crop unsupported v1).
                if (gparams.Count >= 8 &&
                    int.TryParse(gparams[1], out var px) && int.TryParse(gparams[2], out var py) &&
                    ushort.TryParse(gparams[3], out var pid) &&
                    int.TryParse(gparams[6], out var pw) && int.TryParse(gparams[7], out var ph))
                {
                    childId = p.Builder.Value.AddGump(commands, pid, Vector3.UnitZ, new Vector2(px, py)).Id;
                    cx0 = px; cy0 = py; cw0 = pw; ch0 = ph;
                }
            }
            else if (Eq(entry, "buttontileart"))
            {
                // buttontileart x y normal pressed action toPage buttonId tileId hue tileX tileY
                // Render: button background sprite + overlay tile (art) on top.
                if (gparams.Count >= 5 &&
                    int.TryParse(gparams[1], out var bx) && int.TryParse(gparams[2], out var by) &&
                    ushort.TryParse(gparams[3], out var normal) && ushort.TryParse(gparams[4], out var pressed))
                {
                    var action = gparams.Count >= 6 ? SafeInt(gparams[5]) : 0;
                    var toPage = gparams.Count >= 7 ? SafeInt(gparams[6]) : 0;
                    var btnId  = gparams.Count >= 8 ? SafeInt(gparams[7]) : 0;
                    var btn = p.Builder.Value.AddButton(commands, (normal, pressed, normal),
                        Vector3.UnitZ, new Vector2(bx, by));
                    var capSender = sender; var capGumpId = gumpId; var capBtnId = btnId;
                    var capRoot = rootId; var capToPage = toPage;
                    if (action != 0)
                    {
                        btn.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, ResMut<ServerGumpRegistry> reg,
                            Query<Data<ServerGumpTextEntry, Text>> entriesQ) =>
                        {
                            net.Value.Send_GumpResponse(capSender, capGumpId, capBtnId,
                                Array.Empty<uint>(), CollectTextEntries(capRoot, entriesQ));
                            if (reg.Value.ByGumpId.TryGetValue(capGumpId, out var r) && r == capRoot)
                                reg.Value.ByGumpId.Remove(capGumpId);
                            cmd.Entity(capRoot).Despawn();
                        });
                    }
                    else
                    {
                        btn.Observe((On<UiClick> _, Commands cmd) =>
                            cmd.Entity(capRoot).Insert(new ServerGumpPageRequest { Page = capToPage }));
                    }
                    // Overlay tile art (parts[8] = tileId, parts[9] = hue, parts[10,11] = tileX,tileY).
                    if (gparams.Count >= 12 &&
                        ushort.TryParse(gparams[8], out var tileId) &&
                        ushort.TryParse(gparams[9], out var tileHue) &&
                        int.TryParse(gparams[10], out var tileX) && int.TryParse(gparams[11], out var tileY))
                    {
                        var tile = p.Builder.Value.AddArt(commands, tileId,
                            ToShaderHue(tileHue), new Vector2(bx + tileX, by + tileY));
                        commands.Entity(tile.Id).Insert(new ServerGumpChild { RootEntity = rootId, Page = page, Group = group });
                        commands.AddChild(rootId, tile.Id);
                    }
                    childId = btn.Id;
                    var binfo = p.Assets.Value.Gumps.GetGump(normal);
                    cx0 = bx; cy0 = by; cw0 = binfo.UV.Width; ch0 = binfo.UV.Height;
                }
            }
            else if (Eq(entry, "checkertrans"))
            {
                // checkertrans x y w h — translucent overlay (50% alpha rect).
                // OOP applies alpha to children overlapping the rect; we
                // approximate with a translucent black BackgroundColor box.
                if (gparams.Count >= 5 &&
                    int.TryParse(gparams[1], out var cx) && int.TryParse(gparams[2], out var cy) &&
                    int.TryParse(gparams[3], out var cw) && int.TryParse(gparams[4], out var ch))
                {
                    childId = commands.Spawn()
                        .Insert(new Node
                        {
                            Display = Display.Flex,
                            PositionType = PositionType.Absolute,
                            Left = Val.Px(cx), Top = Val.Px(cy),
                            Width = Val.Px(cw), Height = Val.Px(ch),
                        })
                        .Insert(new BackgroundColor(new ClayColor(0, 0, 0, 128)))
                        .Id;
                    cx0 = cx; cy0 = cy; cw0 = cw; ch0 = ch;
                }
            }
            else
            {
                Console.WriteLine($"[ServerGump] unhandled command: \"{entry}\" (cmd={cmdList[cnt]})");
            }

            if (childId != 0)
            {
                commands.Entity(childId).Insert(new ServerGumpChild { RootEntity = rootId, Page = page, Group = group });
                // Visibility (page 0 always shown; page N only while
                // CurrentPage == N) is applied by SyncPageVisibility — see
                // Build. Doing it here by
                // re-inserting a default Node would wipe position/size info
                // set above; the sync system flips Display in-place on the
                // existing Node component instead.
                commands.AddChild(rootId, childId);

                // Track gump-local extent so the root container has a real
                // hit-test surface for drag / right-click-close. Width=Auto
                // on a Floating Clay element collapses to 0 and makes the
                // whole gump unclickable. Span ALL pages: any page can be the
                // active view (default is page 1, not 0), so the hit surface
                // must cover the largest page's content.
                if (cx0 + cw0 > maxRight)  maxRight  = cx0 + cw0;
                if (cy0 + ch0 > maxBottom) maxBottom = cy0 + ch0;
            }
        }

        // Root is an invisible window anchor at the GUMP ORIGIN (x,y) sized to the
        // full content extent. The background resizepic (if any) is a normal child
        // at its own (rx,ry), so every control lands at its true gump-local coord
        // relative to this frame. It carries NO hit surface of its own: gestures
        // hit the actual sprite children (resizepic frame pixel-perfect via the
        // 9-slice mask, gumppics, text) and walk up to this UIMovable root —
        // matching legacy ResizePic.Contains, where a click on a transparent
        // corner/gap passes through instead of the whole rect capturing it. A
        // whole-bbox None surface here would defeat that pixel-perfect mask.
        // Only the root carries GlobalZIndex (threaded to descendants).
        if (maxRight > 0f && maxBottom > 0f)
        {
            commands.Entity(rootId)
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(x),
                    Top = Val.Px(y),
                    Width = Val.Px(maxRight),
                    Height = Val.Px(maxBottom),
                })
                .Insert<UIMovable>()
                .Insert(new GlobalZIndex(z));
        }

        // nomove disables ONLY drag — keep UIMovable so the gump stays a window
        // (right-click-close, click-capture-to-world, z-stack). Removing UIMovable
        // would make the whole gump fall through to the world (clicks ignored).
        if (nomove)
            commands.Entity(rootId).Insert<UINoDrag>();
    }

    // Wrapped text. Clay.NET (the .NET port shipped with TinyEcs.Bevy.UI)
    // does NOT implement text wrapping — it emits one render command per
    // Text element with the full unwrapped string. FontStashSharp's DrawText
    // honours embedded '\n' but has no max-width auto-wrap. So we pre-wrap
    // here: measure each word with the configured font, greedily pack words
    // into lines up to size.X pixels, joining with '\n'.
    // Mirrors OOP HtmlControl ctor (Game/UI/Controls/HtmlControl.cs):
    // when hasBg=1 it spawns a ResizePic 0x2486 inside the htmlgump area,
    // and the text region shrinks by 8px (4 per side) for the bg padding
    // plus 16px for the scrollbar (right edge) if hasScroll=true.
    private static (int X, int Y, int W, int H) HtmlInnerRect(int x, int y, int w, int h, bool hasBg, bool hasScroll)
    {
        int padW = hasScroll ? 16 : 0;
        if (hasBg)
        {
            // OOP shrinks the rendered text width by hasScrollbar:16 +
            // hasBackground:8. We position the inner text at +4,+4 inside
            // the bg sprite and shrink to leave room for the frame.
            return (x + 4, y + 4, Math.Max(0, w - padW - 8), Math.Max(0, h - 8));
        }
        return (x, y, Math.Max(0, w - padW), h);
    }

    // Inner ResizePic 0x2486 — the wood-frame backdrop OOP HtmlControl
    // spawns when the htmlgump command sets hasBg=1. Same size as the
    // htmlgump area (minus scrollbar width).
    private static ulong SpawnHtmlBackground(Commands commands, GumpBuilder builder, Vector2 position, Vector2 size, bool hasScroll, int page, int group, ulong rootId)
    {
        int padW = hasScroll ? 16 : 0;
        var bgWidth = Math.Max(1, (int)size.X - padW);
        var bgHeight = Math.Max(1, (int)size.Y);
        var bg = builder.AddGumpNinePatch(commands, 0x2486, Vector3.UnitZ, position, new Vector2(bgWidth, bgHeight));
        commands.Entity(bg.Id).Insert(new ServerGumpChild { RootEntity = rootId, Page = page, Group = group });
        commands.AddChild(rootId, bg.Id);
        return bg.Id;
    }

    // Pixels scrolled per up/down arrow click (~two text lines).
    private const float ScrollArrowStep = 40f;

    // The vertical scrollbar OOP's HtmlControl adds when hasScrollbar (the
    // non-flag ScrollBar: up/down arrow buttons + a 3-piece background track +
    // a slider thumb). Mirrors OOP ScrollBar.Draw geometry; placed at the box's
    // right edge (OOP `ScrollBar(Width - 14, 0, Height)`). The arrows scroll on
    // click and the thumb both tracks the live scroll (SyncScrollThumbs) and
    // drags it (ScrollThumbDragSystem).
    private static void SpawnScrollBar(Commands commands, GumpBuilder builder, AssetsServer assets, Vector2 position, Vector2 size, int page, int group, ulong rootId, ulong scrollEntity, float maxScroll)
    {
        const ushort UP = 251, UP_PRESSED = 250, DOWN = 253, DOWN_PRESSED = 252, BG_TOP = 257, BG_MID = 256, BG_BOTTOM = 255, SLIDER = 254;

        int upH = assets.Gumps.GetGump(UP).UV.Height;
        int downH = assets.Gumps.GetGump(DOWN).UV.Height;
        int bgTopH = assets.Gumps.GetGump(BG_TOP).UV.Height;
        int bgBottomH = assets.Gumps.GetGump(BG_BOTTOM).UV.Height;
        int bgW = assets.Gumps.GetGump(BG_TOP).UV.Width;
        int sliderW = assets.Gumps.GetGump(SLIDER).UV.Width;
        int sliderH = assets.Gumps.GetGump(SLIDER).UV.Height;

        int h = (int)size.Y;
        float sx = position.X + size.X - 14f;
        float y0 = position.Y;

        void Attach(ulong id)
        {
            commands.Entity(id).Insert(new ServerGumpChild { RootEntity = rootId, Page = page, Group = group });
            commands.AddChild(rootId, id);
        }

        int middleHeight = h - upH - downH - bgTopH - bgBottomH;
        if (middleHeight > 0)
        {
            Attach(builder.AddGump(commands, BG_TOP, Vector3.UnitZ, new Vector2(sx, y0 + upH)).Id);
            Attach(builder.AddGumpTiled(commands, BG_MID, Vector3.UnitZ, new Vector2(sx, y0 + upH + bgTopH), new Vector2(bgW, middleHeight)).Id);
            Attach(builder.AddGump(commands, BG_BOTTOM, Vector3.UnitZ, new Vector2(sx, y0 + h - downH - bgBottomH)).Id);
        }
        else
        {
            Attach(builder.AddGumpTiled(commands, BG_MID, Vector3.UnitZ, new Vector2(sx, y0 + upH), new Vector2(bgW, Math.Max(1, h - upH - downH))).Id);
        }

        // Up / down arrow buttons — click to scroll. Capture only immutable
        // values (scroll entity id + maxScroll); the buttons share this gump's
        // rebuild cohort, so the captured id stays valid for their lifetime.
        var capScroll = scrollEntity;
        var capMax = maxScroll;
        var upBtn = builder.AddButton(commands, (UP, UP_PRESSED, UP), Vector3.UnitZ, new Vector2(sx, y0));
        upBtn.Observe((On<UiClick> _, Query<Data<ScrollPosition>> sq) =>
        {
            if (sq.Contains(capScroll))
            {
                var (_, sp) = sq.Get(capScroll);
                sp.Ref.OffsetY = Math.Clamp(sp.Ref.OffsetY - ScrollArrowStep, 0f, capMax);
            }
        });
        Attach(upBtn.Id);

        var downBtn = builder.AddButton(commands, (DOWN, DOWN_PRESSED, DOWN), Vector3.UnitZ, new Vector2(sx, y0 + h - downH));
        downBtn.Observe((On<UiClick> _, Query<Data<ScrollPosition>> sq) =>
        {
            if (sq.Contains(capScroll))
            {
                var (_, sp) = sq.Get(capScroll);
                sp.Ref.OffsetY = Math.Clamp(sp.Ref.OffsetY + ScrollArrowStep, 0f, capMax);
            }
        });
        Attach(downBtn.Id);

        // Slider thumb. Tagged with ServerGumpScrollThumb so SyncScrollThumbs
        // tracks it to the text container's live scroll offset each frame.
        var thumbTop = y0 + upH;
        var travel = Math.Max(0f, h - upH - downH - sliderH);
        var thumbId = builder.AddGump(commands, SLIDER, Vector3.UnitZ, new Vector2(sx + (bgW - sliderW) / 2f, thumbTop)).Id;
        commands.Entity(thumbId).Insert(new ServerGumpScrollThumb
        {
            ScrollEntity = scrollEntity,
            TrackTop = thumbTop,
            Travel = travel,
            MaxScroll = maxScroll,
        });
        Attach(thumbId);
    }

    // OOP HtmlControl.InternalBuild default body colour (HTMLColor) for HTML
    // text. Untagged segments render in this colour; <basefont>/<a> tags
    // override per-segment. Mirrors the exact branch logic so the Help menu's
    // body text comes out black on the 0x2486 parchment instead of white.
    internal static uint HtmlStartColor(int hue, bool hasBg, bool hasScroll)
    {
        if (hue > 0)
        {
            if (hue == 0x00FFFFFF || hue == 0xFFFF || hue == 0xFF) return 0xFFFFFFFE;
            return (HuesHelper.Color16To32((ushort)hue) << 8) | 0xFF;
        }
        if (!hasBg)
            return hasScroll ? 0xFFFFFFFF : 0x010101FF;
        return 0x010101FF;
    }

    // `hue` is overloaded by command: a UO hue index for plain text
    // (text/croppedtext), or the HTML start colour for xmfhtmlgumpcolor/
    // xmfhtmltok (which can be 0x00FFFFFF — hence int, not ushort).
    // Gather the (entryId, text) pairs for every textentry field belonging to
    // the gump rooted at `rootId`, in the Tuple form Send_GumpResponse wants.
    // Empty when the gump has no text entries.
    private static Tuple<ushort, string>[] CollectTextEntries(
        ulong rootId,
        Query<Data<ServerGumpTextEntry, Text>> entriesQ)
    {
        var list = new List<Tuple<ushort, string>>();
        foreach (var (_, te, t) in entriesQ)
            if (te.Ref.RootEntity == rootId)
                list.Add(Tuple.Create(te.Ref.EntryId, t.Ref.Value ?? string.Empty));
        return list.Count == 0 ? Array.Empty<Tuple<ushort, string>>() : list.ToArray();
    }

    private static ulong SpawnWrappedText(Commands commands, Vector2 position, Vector2 size, string text, int hue, bool hasBg, bool hasScroll, bool isHtml, out int contentHeight)
    {
        // Measure + lay out the wrapped (optionally HTML) run, then render it
        // glyph-by-glyph from the shared font atlas via a UOCustomKind.WrappedText
        // node. Clay.NET's text-wrap pipeline doesn't honour parent-Width on the
        // .NET port, so the run is pre-wrapped to size.X by FontsLoader.
        //
        // Two-node structure so taller content scrolls instead of squishing:
        //   outer (Width/Height = container, Overflow.Scroll)
        //     └─ inner (Width=measuredW, Height=measuredH, WrappedText)
        // Outer clips + handles mouse-wheel scroll via Clay's ScrollConfig.
        // Without the inner wrapper the text would render at outer.bbox which
        // forces it into the container size and hides overflowing lines.
        // isHtml is decided by the COMMAND (param), not by sniffing for '<':
        // the htmlgump/xmfhtml* family is always HTML (mirrors OOP HtmlControl,
        // which sets _gameText.IsHTML = true unconditionally). A tag-free body
        // still needs the HTML parse so untagged chars pick up htmlStartColor
        // (black on the parchment bg) instead of falling through to the white
        // plain-text tint.
        var htmlStartColor = HtmlStartColor(hue, hasBg, hasScroll);
        var safeText = text ?? string.Empty;
        var (w, h) = UoFontRenderer.Measure(safeText, font: 1, (int)size.X, isHtml, htmlStartColor, htmlBg: !hasBg);
        contentHeight = h;

        bool needsScroll = h > size.Y;
        var outerCmd = commands.Spawn().Insert(new Node
        {
            Display = Display.Flex,
            PositionType = PositionType.Absolute,
            Left = Val.Px(position.X),
            Top = Val.Px(position.Y),
            Width = Val.Px(size.X),
            Height = Val.Px(size.Y),
            Overflow = needsScroll ? Overflow.Scroll : Overflow.Clip,
        });
        // ScrollPosition component is required for Bevy.UI to sync our
        // scroll writes through to Clay. LayoutSystem PreLayout walks
        // entities with this component and pushes component value into
        // Clay's ScrollContainerData. Writing Clay directly via
        // SetScrollPosition gets overridden by that sync; mutating the
        // component is the supported path.
        if (needsScroll)
            commands.Entity(outerCmd.Id).Insert(new ScrollPosition());

        if (w > 0 && h > 0)
        {
            var inner = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(w),
                    Height = Val.Px(h),
                })
                .Insert(new UiCustom
                {
                    Data = new UOCustomRender
                    {
                        Kind = UOCustomKind.WrappedText,
                        Hue = Vector3.UnitZ,
                        Text = safeText,
                        TextFont = 1,
                        // Only meaningful for plain text (HTML carries per-segment
                        // colour); the HTML colour rides in HtmlStartColor instead.
                        TextHue = (ushort)hue,
                        WrapWidth = (int)size.X,
                        IsHtml = isHtml,
                        HtmlStartColor = htmlStartColor,
                        HtmlBg = !hasBg,
                    },
                });
            commands.AddChild(outerCmd.Id, inner.Id);
        }
        return outerCmd.Id;
    }

    private static ulong SpawnText(Commands commands, Vector2 position, string text, ClayColor color)
    {
        return commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(position.X),
                Top = Val.Px(position.Y),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(text ?? string.Empty))
            .Insert(new TextFont { FontId = 1, Size = 16 })
            .Insert(new TextColor(color))
            .Id;
    }

    // UO hue → ClayColor. Mirrors OOP's Label path (RenderedText, IsUnicode):
    //   hue passed to HtmlControl/Label ctor as parts[3]
    //   ctor adds +1 (matches the +1 convention on the wire — server sends
    //     N-1, client uses N)
    //   RenderedText.Hue = (parts[3] + 1)
    //   Unicode draw path → GetUnicodeFontColor(_, Hue) → 16-bit color from
    //     hue palette ColorTable[8] (the primary fill cell)
    // GetUnicodeFontColor already does the (color - 1) shift internally, so
    // we pass (hue + 1) here.
    private static ClayColor HueToClayColor(HuesLoader hues, ushort hue)
    {
        if (hue == 0) return ClayColor.Black;
        // OOP RenderedText defaults cell=30 (RenderedText.Create cell=30 arg →
        // GenerateUnicode), the brightest end of the hue gradient. The tinted
        // ClayColor multiplies a white glyph drawn from the shared atlas
        // (UoFontRenderer.Draw → SHADER_RGB_TINT), so it keeps per-glyph alpha
        // exactly like OOP — a bright gold hue reads as bright gold, not the
        // muted olive cell=4 produced. Mirrors HueToTint (wrapped-text path),
        // which already uses 30. parts[3] + 1 mirrors OOP's Label ctor "+1".
        var packed = hues.GetPolygoneColor(30, (ushort)(hue + 1));
        if (packed == 0 || packed == 0xFF010101) return ClayColor.Black;
        byte r = (byte)(packed & 0xFF);
        byte g = (byte)((packed >> 8) & 0xFF);
        byte b = (byte)((packed >> 16) & 0xFF);
        return new ClayColor(r, g, b, 255);
    }


    internal static bool Eq(string a, string b)
        => string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);

    internal static int SafeInt(string s)
        => int.TryParse(s, out var v) ? v : 0;

    internal static string SafeLine(string[] lines, int idx)
        => (idx >= 0 && idx < lines.Length) ? (lines[idx] ?? string.Empty) : string.Empty;

    internal static int ParseClilocId(string s)
        => int.TryParse((s ?? string.Empty).Replace("#", string.Empty), out var v) ? v : 0;

    internal static ushort ParseHueArg(List<string> gp, int startIdx)
    {
        for (var i = startIdx; i < gp.Count; i++)
        {
            var tok = gp[i];
            if (tok.StartsWith("hue=", StringComparison.OrdinalIgnoreCase))
                return UInt16Converter.Parse(tok.Substring(4));
        }
        return 0;
    }

    internal static Vector3 ToShaderHue(ushort hue)
        => hue == 0 ? Vector3.UnitZ : new Vector3(hue, 1f, 1f);

    // Server text often contains HTML-ish markup (<basefont>, <br>, etc.).
    // Bevy.UI Text doesn't parse markup so strip tags to keep the label
    // readable. Real HTML rendering is a separate plugin.
    internal static string StripTags(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new System.Text.StringBuilder(s.Length);
        var inTag = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '<') { inTag = true; continue; }
            if (c == '>') { inTag = false; continue; }
            if (!inTag) sb.Append(c);
        }
        return sb.ToString();
    }
}

// Screen top-left per (sender, gumpId). Seeded from the packet on first open,
// refreshed from the live root on every rebuild so a server-pushed update
// reopens where the user last dragged it (mirrors OOP UIManager
// GetGumpCachePosition / SavePosition). Persists across close→reopen.
internal sealed class ServerGumpPositions
{
    // Keyed by gumpId (the stable gump identity; sender changes per push).
    public readonly Dictionary<ulong, Vector2> ByKey = new();
}

// Open server gumps keyed by gumpId → root entity. Maintained synchronously in
// BuildGump (NOT via a query) so a burst of pushes of the same gump in one frame
// each despawn the prior root before Commands have synced to the world.
internal sealed class ServerGumpRegistry
{
    public readonly Dictionary<uint, ulong> ByGumpId = new();
}

#if AGENT_BUILD
// Harness-only queue: debug.openServerGump pushes a layout here; the drain
// system emits the real 0xB0 trigger next frame.
internal sealed class DebugServerGumpQueue
{
    public readonly List<(uint GumpId, int X, int Y, string Layout, string[] Lines)> Pending = new();
}
#endif

// Pending page switch for a gump root. Written by a switchpage button's
// observer (via Commands), consumed + removed by ApplyPageRequests.
internal struct ServerGumpPageRequest
{
    public int Page;
}

internal struct ServerGump
{
    public uint Sender;
    public uint GumpId;
    // Active page index. Starts at 0 (the gump's initial view). Mutated by
    // action=0 buttons (SwitchPage); a system in PostUpdate shows only the
    // children whose Page equals this (pages are mutually exclusive).
    public int CurrentPage;
}

internal struct ServerGumpChild
{
    public ulong RootEntity;
    public int Page;
    public int Group;
}

// On a textentry field's glyph entity (the one SpawnTextField returns and the
// global editor mutates). On a gump-response button click, every entry whose
// RootEntity matches the clicked gump's root is collected into the response's
// (entryId, text) list — see CollectTextEntries.
internal struct ServerGumpTextEntry
{
    public ulong RootEntity;
    public ushort EntryId;
}

// Scrollbar slider thumb. SyncScrollThumbs reads the linked text container's
// ScrollPosition.OffsetY each frame and slides the thumb along its track to
// match (so the visual scrollbar tracks wheel scrolling). Travel/MaxScroll are
// 0 when the text fits — thumb stays parked at TrackTop.
internal struct ServerGumpScrollThumb
{
    public ulong ScrollEntity;
    public float TrackTop;
    public float Travel;
    public float MaxScroll;
}

internal sealed class ServerGumpParams : CompositeSystemParam
{
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<UOFileManager> Files;
    public readonly Res<UiZCounter> ZCounter;
    public readonly ResMut<ServerGumpPositions> Positions;
    public readonly ResMut<ServerGumpRegistry> Registry;
    public readonly Query<Data<ServerGump, Node>> ExistingQ;

    public ServerGumpParams()
    {
        Assets    = Add(new Res<AssetsServer>());
        Builder   = Add(new Res<GumpBuilder>());
        Files     = Add(new Res<UOFileManager>());
        ZCounter  = Add(new Res<UiZCounter>());
        Positions = Add(new ResMut<ServerGumpPositions>());
        Registry  = Add(new ResMut<ServerGumpRegistry>());
        ExistingQ = Add(new Query<Data<ServerGump, Node>>());
    }
}
