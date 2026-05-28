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
//   * noclose / nodispose / nomove — flag the root (nomove strips UIMovable).

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
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
        bool rootBgAssigned = false;
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
                    if (!rootBgAssigned)
                    {
                        commands.Entity(rootId).InsertBundle(new UOGumpBundle
                        {
                            Position = new Vector2(x + rx, y + ry),
                            Size = new Vector2(rw, rh),
                            BackgroundId = rid,
                            Hue = Vector3.UnitZ,
                            ZOrder = z,
                            Kind = UOCustomKind.GumpNinePatch,
                        });
                        rootBgAssigned = true;
                        cx0 = rx; cy0 = ry; cw0 = rw; ch0 = rh;
                        continue;
                    }

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
                        btn.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, ResMut<ServerGumpRegistry> reg) =>
                        {
                            net.Value.Send_GumpResponse(capturedSender, capturedGumpId, capturedBtnId,
                                Array.Empty<uint>(), Array.Empty<Tuple<ushort, string>>());
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
                    childId = SpawnWrappedText(commands, new Vector2(tx, ty), new Vector2(tw, th), text, thue);
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
                    childId = SpawnWrappedText(commands, new Vector2(txtX, txtY), new Vector2(txtW, txtH), text, 0);
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
                    var (txtX, txtY, txtW, txtH) = HtmlInnerRect(tx, ty, tw, th, hasBg, hasScroll);
                    childId = SpawnWrappedText(commands, new Vector2(txtX, txtY), new Vector2(txtW, txtH), text, 0);
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
                    childId = SpawnWrappedText(commands, new Vector2(txtX, txtY), new Vector2(txtW, txtH), text, 0);
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
                if (gparams.Count >= 8 &&
                    int.TryParse(gparams[1], out var tx) && int.TryParse(gparams[2], out var ty) &&
                    int.TryParse(gparams[3], out var tw) && int.TryParse(gparams[4], out var th) &&
                    ushort.TryParse(gparams[5], out var thue) &&
                    int.TryParse(gparams[7], out var lid))
                {
                    var text = SafeLine(lines, lid);
                    childId = SpawnWrappedText(commands, new Vector2(tx, ty), new Vector2(tw, th), text, thue);
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
                        btn.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, ResMut<ServerGumpRegistry> reg) =>
                        {
                            net.Value.Send_GumpResponse(capSender, capGumpId, capBtnId,
                                Array.Empty<uint>(), Array.Empty<Tuple<ushort, string>>());
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

        // Apply the computed extent to the root when no resizepic absorbed it.
        // Give it an invisible Custom surface (UOCustomKind.None) so it gets a
        // ComputedNode + solid hit-test and behaves like any gump window:
        // draggable, right-click-closable, and click-capturing over its whole
        // area (otherwise empty regions fall through to the world).
        if (!rootBgAssigned && maxRight > 0f && maxBottom > 0f)
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
                .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
                .Insert(Interaction.None)
                .Insert<UIMovable>()
                .Insert(new GlobalZIndex(z));
        }

        if (nomove)
            commands.Entity(rootId).Remove<UIMovable>();
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

    private static ulong SpawnWrappedText(Commands commands, Vector2 position, Vector2 size, string text, ushort hue)
    {
        // Pre-bake text into a Texture2D via FontsLoader. Handles HTML
        // markup AND word-wrap to size.X internally. Render as UiImage so
        // the existing DrawImage path picks it up — Clay.NET's text-wrap
        // pipeline doesn't honour parent-Width on the .NET port.
        //
        // Two-node structure so taller content scrolls instead of squishing:
        //   outer (Width/Height = container, Overflow.Scroll)
        //     └─ inner (Width=texW, Height=texH, UiImage)
        // Outer clips + handles mouse-wheel scroll via Clay's ScrollConfig.
        // Without the inner wrapper the UiImage would render at outer.bbox
        // which forces the texture into the container size and hides
        // overflowing lines.
        bool isHtml = !string.IsNullOrEmpty(text) && text.IndexOf('<') >= 0;
        var (tex, w, h) = UoFontRenderer.Bake(text ?? string.Empty, font: 1, hue, (int)size.X, isHtml);

        bool needsScroll = tex != null && h > size.Y;
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

        if (tex != null)
        {
            var inner = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(w),
                    Height = Val.Px(h),
                })
                .Insert(new UiImage
                {
                    ImageData = tex,
                    SourceSize = new System.Numerics.Vector2(w, h),
                    Tint = ClayColor.White,
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
        // OOP RenderedText defaults cell=30 → ColorTable[30] (brightest end
        // of the hue gradient). Cell=8 is the dim center; using that made
        // every text element render dark grey/olive. parts[3] + 1 mirrors
        // OOP's Label ctor "+1" offset.
        // OOP RenderedText defaults cell=30 (brightest end of gradient) and
        // blends per-glyph alpha against the gump bg, so even pale tints
        // remain visible. Bevy.UI TTF text is rendered as solid color with
        // no per-pixel blend — picking the bright end produces near-white
        // text that vanishes on a cream gump. cell=4 sits near the middle
        // of the gradient and reads correctly on both dark and light bg.
        var packed = hues.GetPolygoneColor(4, (ushort)(hue + 1));
        if (packed == 0 || packed == 0xFF010101) return ClayColor.Black;
        byte r = (byte)(packed & 0xFF);
        byte g = (byte)((packed >> 8) & 0xFF);
        byte b = (byte)((packed >> 16) & 0xFF);
        return new ClayColor(r, g, b, 255);
    }


    private static bool Eq(string a, string b)
        => string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);

    private static int SafeInt(string s)
        => int.TryParse(s, out var v) ? v : 0;

    private static string SafeLine(string[] lines, int idx)
        => (idx >= 0 && idx < lines.Length) ? (lines[idx] ?? string.Empty) : string.Empty;

    private static int ParseClilocId(string s)
        => int.TryParse((s ?? string.Empty).Replace("#", string.Empty), out var v) ? v : 0;

    private static ushort ParseHueArg(List<string> gp, int startIdx)
    {
        for (var i = startIdx; i < gp.Count; i++)
        {
            var tok = gp[i];
            if (tok.StartsWith("hue=", StringComparison.OrdinalIgnoreCase))
                return UInt16Converter.Parse(tok.Substring(4));
        }
        return 0;
    }

    private static Vector3 ToShaderHue(ushort hue)
        => hue == 0 ? Vector3.UnitZ : new Vector3(hue, 1f, 1f);

    // Server text often contains HTML-ish markup (<basefont>, <br>, etc.).
    // Bevy.UI Text doesn't parse markup so strip tags to keep the label
    // readable. Real HTML rendering is a separate plugin.
    private static string StripTags(string s)
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
