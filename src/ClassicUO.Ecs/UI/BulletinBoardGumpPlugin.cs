// Port of main's Game/UI/Gumps/BulletinBoardGump.cs.
//
// All traffic rides 0x71 subtypes. Server → client: 0 opens the board (gump
// 0x087A + name + the scrollable message list), 1 appends a message summary
// row ("poster - subject - datetime", double-click requests the body), 2 opens
// a message window (parchment scroll 0x0820 with author/date/title + body).
// Client → server: 3 request message, 5 post (the editor window the board's
// post hitbox opens, or Reply), 6 remove own message.
//
// Message windows come in three variants like legacy: 0 = editor (editable
// subject + body, Post button), 1 = reading someone else's message (Reply
// button), 2 = reading your own (Remove button). Variant for an incoming
// message is picked by poster == player name.
//
// Movable / right-click-close / stacking come from the shared WindowDrag infra.
// Legacy disposes message windows with the board; here they close independently
// (each is its own UiMovable window).

using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Board window root, keyed by the board item serial.
internal struct BulletinBoardWindow
{
    public uint Serial;
    public ulong ListNode;
    // serial -> row entity, so a re-sent summary replaces its row (legacy
    // AddBulletinObject disposes the matching child first).
    public Dictionary<uint, ulong> Rows;
}

// A summary row: double-click requests the message body (0x71 subtype 3).
internal struct BulletinRow { public uint Board; public uint Msg; }

// Message / editor window root. Subject and Body glyph ids are read back when
// the Post button fires (subject is a single-line Text field, body a multiline
// WrappedText field).
internal struct BulletinItemWindow
{
    public uint Board;
    public uint MsgSerial;
    public byte Variant; // 0 editor, 1 reply-able, 2 removable
    public ulong SubjectGlyph;
    public ulong BodyGlyph;
}

// Summary rows whose board window isn't in the world yet: the type 0 open and
// its type 1 summaries arrive in one packet batch, so the summaries' observers
// run before the board's spawn commands have flushed. They park here and a
// retry system lands them once the window exists.
internal sealed class BulletinPendingRows : List<(uint Board, uint Msg, string Preview)> { }

internal readonly struct BulletinBoardGumpPlugin : IPlugin
{
    private const ushort BoardGraphic = 0x087A;
    private const ushort RowPinGraphic = 0x1523;
    private const ushort ScrollGraphic = 0x0820;   // parchment caps/body (same as profile)
    private const ushort PostBannerGraphic = 0x0883;
    private const ushort PostBtnGraphic = 0x0886, ReplyBtnGraphic = 0x0884, RemoveBtnGraphic = 0x0885;
    private const ushort DividerGraphic = 0x0835;
    private const int RowHeight = 18;
    private const int ListX = 127, ListY = 159, ListW = 241, ListH = RowHeight * 9;
    private const int ItemHeight = 408;            // legacy ExpandableScroll default
    private const int CapOverlap = 14;
    private const int BodyWidth = 220;
    private const byte Font = 1;                   // unicode font 1
    private const uint TextColorBlack = 0x010101FF;

    public void Build(App app)
    {
        app.AddResource(new BulletinPendingRows());

        app.AddObserver<On<PacketReceived<OnBulletinBoardPacket_0x71>>, Commands, BulletinParams>(OnPacket);

        var flushFn = FlushPendingRows;
        app.AddSystem(flushFn).InStage(Stage.Update)
            .RunIf((Res<BulletinPendingRows> pending) => pending.Value.Count > 0)
            .Build();

        var despawnFn = DespawnAll;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugBulletinQueue());
        Action<Commands, ResMut<DebugBulletinQueue>> drainFn = DrainDebugBulletin;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

    private static void OnPacket(
        On<PacketReceived<OnBulletinBoardPacket_0x71>> trig,
        Commands commands,
        BulletinParams p)
    {
        var k = trig.Event.Packet;
        switch (k.Type)
        {
            case 0:
                BuildBoard(commands, p, k.BoardSerial, k.BoardName);
                break;

            case 1:
                if (!AddSummaryRow(commands, p, k.BoardSerial, k.MessageSerial ?? 0, k.MessagePreview))
                    p.Pending.Value.Add((k.BoardSerial, k.MessageSerial ?? 0, k.MessagePreview));
                break;

            case 2:
            {
                string body = k.Lines.Count > 0 ? string.Join("\n", k.Lines).TrimStart() : string.Empty;
                byte variant = (byte)(k.Author == p.GameCtx.Value.PlayerName ? 2 : 1);
                BuildItemWindow(commands, p, k.BoardSerial, k.MessageSerial ?? 0, k.Author, k.Subject, k.DateTime, body, variant);
                break;
            }
        }
    }

    private static void BuildBoard(Commands commands, BulletinParams p, uint serial, string name)
    {
        // Re-open replaces (legacy GetGump(serial)?.Dispose() + Add).
        foreach (var (ent, win) in p.BoardsQ)
            if (win.Ref.Serial == serial)
                commands.Entity(ent.Ref).Despawn();

        var builder = p.Builder.Value;
        var root = builder.SpawnUOGump(commands, BoardGraphic, Vector3.UnitZ, new Vector2(180, 60), p.ZCounter.Value);
        ulong rootId = root.Id;

        // Board name, centered over the top plaque (legacy Label at 159,36 w170).
        var title = SpawnWrappedText(commands, new Vector2(159, 36), 170, name, center: true);
        if (title != 0) commands.AddChild(rootId, title);

        // "Post message" hitbox over the baked quill art (legacy HitBox 15,170,80,80).
        var capBoard = serial;
        var post = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(15), Top = Val.Px(170),
                Width = Val.Px(80), Height = Val.Px(80),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
        post.Observe((On<UiClick> _, Commands cmd, BulletinParams bp) =>
        {
            // Blank editor: post a new message to the board (msgSerial 0).
            BuildItemWindow(cmd, bp, capBoard, 0, bp.GameCtx.Value.PlayerName ?? string.Empty,
                string.Empty, DateTime.Now.ToShortDateString(), string.Empty, variant: 0);
        });
        commands.AddChild(rootId, post.Id);

        // Scrollable summary list (legacy ScrollArea 127,159,241, 18*9).
        var list = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute,
                Left = Val.Px(ListX), Top = Val.Px(ListY),
                Width = Val.Px(ListW), Height = Val.Px(ListH),
                Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition());
        commands.AddChild(rootId, list.Id);

        commands.Entity(rootId).Insert(new BulletinBoardWindow
        {
            Serial = serial,
            ListNode = list.Id,
            Rows = new Dictionary<uint, ulong>(),
        });
    }

    private static void FlushPendingRows(Commands commands, BulletinParams p)
    {
        for (int i = p.Pending.Value.Count - 1; i >= 0; i--)
        {
            var (board, msg, preview) = p.Pending.Value[i];
            if (AddSummaryRow(commands, p, board, msg, preview))
                p.Pending.Value.RemoveAt(i);
        }
    }

    private static bool AddSummaryRow(Commands commands, BulletinParams p, uint board, uint msgSerial, string preview)
    {
        foreach (var (ent, win) in p.BoardsQ)
        {
            if (win.Ref.Serial != board) continue;

            if (win.Ref.Rows.Remove(msgSerial, out var old))
                commands.Entity(old).Despawn();

            var row = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(ListW - 11), Height = Val.Px(RowHeight),
                })
                .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
                .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>()
                .Insert(new BulletinRow { Board = board, Msg = msgSerial });

            var pin = p.Builder.Value.AddGump(commands, RowPinGraphic, Vector3.UnitZ, new Vector2(0, 0));
            commands.AddChild(row.Id, pin.Id);
            var text = SpawnWrappedText(commands, new Vector2(23, 1), ListW - 34, preview, center: false);
            if (text != 0) commands.AddChild(row.Id, text);

            var capBoard = board;
            var capMsg = msgSerial;
            row.Observe((On<UiDoubleClick> _, Res<NetClient> net) =>
                net.Value.Send_BulletinBoardRequestMessage(capBoard, capMsg));

            commands.AddChild(win.Ref.ListNode, row.Id);
            win.Ref.Rows[msgSerial] = row.Id;
            return true;
        }
        return false;
    }

    private static void BuildItemWindow(
        Commands commands, BulletinParams p,
        uint board, uint msgSerial, string poster, string subject, string datetime, string body, byte variant)
    {
        // One message window per board at a time (legacy disposes the previous).
        foreach (var (ent, win) in p.ItemsQ)
            if (win.Ref.Board == board)
                commands.Entity(ent.Ref).Despawn();

        var assets = p.Assets.Value;
        var builder = p.Builder.Value;

        int topW = GumpW(assets, ScrollGraphic), topH = GumpH(assets, ScrollGraphic);
        int midW = GumpW(assets, (ushort)(ScrollGraphic + 2));
        int botW = GumpW(assets, (ushort)(ScrollGraphic + 3)), botH = GumpH(assets, (ushort)(ScrollGraphic + 3));
        int width = Math.Max(topW, Math.Max(midW, botW));

        // Bare root: parchment children are the pixel-perfect hit surface
        // (profile-gump pattern).
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(400), Top = Val.Px(120),
                Width = Val.Px(width), Height = Val.Px(ItemHeight),
            })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(p.ZCounter.Value.Bump()));
        ulong rootId = root.Id;

        var mid = builder.AddGumpTiled(commands, (ushort)(ScrollGraphic + 2), Vector3.UnitZ,
            new Vector2((width - midW) / 2f, topH - CapOverlap), new Vector2(midW, ItemHeight - topH - botH + CapOverlap * 2));
        commands.AddChild(rootId, mid.Id);
        var top = builder.AddGump(commands, ScrollGraphic, Vector3.UnitZ, new Vector2((width - topW) / 2f, 0));
        commands.AddChild(rootId, top.Id);
        var bottom = builder.AddGump(commands, (ushort)(ScrollGraphic + 3), Vector3.UnitZ, new Vector2((width - botW) / 2f, ItemHeight - botH));
        commands.AddChild(rootId, bottom.Id);

        if (variant == 0)
        {
            var banner = builder.AddGump(commands, PostBannerGraphic, Vector3.UnitZ, new Vector2(97, 12));
            commands.AddChild(rootId, banner.Id);
        }

        // Header labels (legacy unicode path: Author 30,40 / Date 30,58 / Title 30,77).
        AttachText(commands, rootId, new Vector2(30, 40), 60, "Author:");
        AttachText(commands, rootId, new Vector2(85, 40), 180, poster);
        AttachText(commands, rootId, new Vector2(30, 58), 60, "Date:");
        AttachText(commands, rootId, new Vector2(85, 58), 180, datetime);
        AttachText(commands, rootId, new Vector2(30, 77), 60, "Title:");

        // Subject: editable single-line field in the editor, static text otherwise.
        ulong subjectGlyph = 0;
        if (variant == 0)
        {
            var frame = commands.Spawn().Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(85), Top = Val.Px(77),
                Width = Val.Px(150), Height = Val.Px(20),
            });
            subjectGlyph = GuiPlugin.SpawnTextField(commands, frame, new Vector2(0, 0),
                new TextFont { FontId = Font, Size = 16 }, new ClayColor(0, 0, 0, 255), subject, masked: false);
            commands.AddChild(rootId, frame.Id);
        }
        else
        {
            var subj = SpawnWrappedText(commands, new Vector2(85, 77), 150, subject, center: false);
            if (subj != 0) commands.AddChild(rootId, subj);
        }

        var divider = builder.AddGumpTiled(commands, DividerGraphic, Vector3.UnitZ, new Vector2(30, 106), new Vector2(235, 4));
        commands.AddChild(rootId, divider.Id);

        // Body: scrollable viewport (legacy ScrollArea 0,120,272,224 with the
        // text at x40 w220); editable multiline in the editor.
        ulong bodyGlyph = 0;
        var bodyFrame = commands.Spawn().Insert(new Node
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column,
            PositionType = PositionType.Absolute,
            Left = Val.Px(40), Top = Val.Px(120),
            Width = Val.Px(BodyWidth), Height = Val.Px(ItemHeight - 120 - 70),
            Overflow = Overflow.Scroll,
        }).Insert(new ScrollPosition());
        if (variant == 0)
        {
            bodyGlyph = GuiPlugin.SpawnTextField(commands, bodyFrame, new Vector2(0, 0),
                new TextFont { FontId = Font, Size = 16 }, new ClayColor(0, 0, 0, 255), body, masked: false,
                multiline: true, wrapWidth: BodyWidth);
        }
        else
        {
            var text = SpawnWrappedText(commands, new Vector2(0, 0), BodyWidth, body, center: false, flowing: true);
            if (text != 0) commands.AddChild(bodyFrame.Id, text);
        }
        commands.AddChild(rootId, bodyFrame.Id);

        commands.Entity(rootId).Insert(new BulletinItemWindow
        {
            Board = board,
            MsgSerial = msgSerial,
            Variant = variant,
            SubjectGlyph = subjectGlyph,
            BodyGlyph = bodyGlyph,
        });

        // Action button (legacy y = Height - 50): Post / Reply / Remove.
        var capRoot = rootId;
        var capBoard = board;
        var capMsg = msgSerial;
        switch (variant)
        {
            case 0:
            {
                var postBtn = builder.AddButton(commands, (PostBtnGraphic, PostBtnGraphic, PostBtnGraphic), Vector3.UnitZ, new Vector2(37, ItemHeight - 50))
                    .Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
                postBtn.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, BulletinParams bp) =>
                {
                    if (!bp.ItemsQ.TryGet(capRoot, out var itemRow)) return;
                    var (_, w) = itemRow;
                    net.Value.Send_BulletinBoardPostMessage(capBoard, capMsg,
                        ReadSingleLine(bp, w.Ref.SubjectGlyph), ReadMultiline(bp, w.Ref.BodyGlyph));
                    cmd.Entity(capRoot).Despawn();
                });
                commands.AddChild(rootId, postBtn.Id);
                break;
            }

            case 1:
            {
                var capSubject = subject;
                var capDate = datetime;
                var replyBtn = builder.AddButton(commands, (ReplyBtnGraphic, ReplyBtnGraphic, ReplyBtnGraphic), Vector3.UnitZ, new Vector2(37, ItemHeight - 50))
                    .Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
                replyBtn.Observe((On<UiClick> _, Commands cmd, BulletinParams bp) =>
                {
                    BuildItemWindow(cmd, bp, capBoard, capMsg, bp.GameCtx.Value.PlayerName ?? string.Empty,
                        "RE: " + capSubject, capDate, string.Empty, variant: 0);
                });
                commands.AddChild(rootId, replyBtn.Id);
                break;
            }

            case 2:
            {
                var removeBtn = builder.AddButton(commands, (RemoveBtnGraphic, RemoveBtnGraphic, RemoveBtnGraphic), Vector3.UnitZ, new Vector2(235, ItemHeight - 50))
                    .Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
                removeBtn.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, BulletinParams bp) =>
                {
                    net.Value.Send_BulletinBoardRemoveMessage(capBoard, capMsg);
                    cmd.Entity(capRoot).Despawn();
                });
                commands.AddChild(rootId, removeBtn.Id);
                break;
            }
        }
    }

    // Single-line field value lives in the glyph's Text component.
    private static string ReadSingleLine(BulletinParams p, ulong glyph)
    {
        if (glyph == 0 || !p.TextQ.TryGet(glyph, out var textRow)) return string.Empty;
        var (_, t) = textRow;
        return t.Ref.Value ?? string.Empty;
    }

    // Multiline field value lives in the WrappedText custom render.
    private static string ReadMultiline(BulletinParams p, ulong glyph)
    {
        if (glyph == 0 || !p.CustomQ.TryGet(glyph, out var customRow)) return string.Empty;
        var (_, c) = customRow;
        return c.Ref.Render()?.Text ?? string.Empty;
    }

    // Wrapped near-black parchment text, sized to its measured content.
    // `flowing` skips absolute positioning so the node flows in a flex column
    // (the scrollable body viewport).
    private static ulong SpawnWrappedText(Commands commands, Vector2 position, int width, string text, bool center, bool flowing = false)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var (w, h) = UoFontRenderer.Measure(text, Font, width, isHtml: true, TextColorBlack, htmlBg: false);
        if (w <= 0 || h <= 0)
            return 0;

        var node = flowing
            ? new Node { Display = Display.Flex, Width = Val.Px(width), Height = Val.Px(h) }
            : new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(position.X), Top = Val.Px(position.Y),
                Width = Val.Px(width), Height = Val.Px(h),
            };

        return commands.Spawn()
            .Insert(node)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = text,
                    TextFont = Font,
                    WrapWidth = width,
                    IsHtml = true,
                    HtmlStartColor = TextColorBlack,
                    HtmlBg = false,
                    TextCenter = center,
                },
            }).Id;
    }

    private static void AttachText(Commands commands, ulong parent, Vector2 pos, int width, string text)
    {
        var id = SpawnWrappedText(commands, pos, width, text, center: false);
        if (id != 0) commands.AddChild(parent, id);
    }

    private static void DespawnAll(
        Commands commands,
        Query<Data<BulletinBoardWindow>> boardsQ,
        Query<Data<BulletinItemWindow>> itemsQ,
        ResMut<BulletinPendingRows> pending)
    {
        pending.Value.Clear();
        foreach (var (ent, _) in boardsQ)
            commands.Entity(ent.Ref).Despawn();
        foreach (var (ent, _) in itemsQ)
            commands.Entity(ent.Ref).Despawn();
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

#if AGENT_BUILD
    // Drains debug.openBulletinBoard: emits the real 0x71 triggers — a type 0
    // open followed by a few type 1 summaries — through the observer path.
    private static void DrainDebugBulletin(Commands commands, ResMut<DebugBulletinQueue> q)
    {
        if (!q.Value.Pending) return;
        q.Value.Pending = false;

        uint board = q.Value.Serial;
        commands.EmitTrigger(new PacketReceived<OnBulletinBoardPacket_0x71>
        {
            Packet = BuildPacket(0, board, b =>
            {
                WriteFixedUtf8(b, "Town Bulletin Board", 22);
            })
        });
        for (uint i = 0; i < 3; i++)
        {
            uint msg = 0x100 + i;
            var poster = i == 0 ? "Seer" : $"Citizen {i}";
            var subj = $"Notice #{i + 1}";
            commands.EmitTrigger(new PacketReceived<OnBulletinBoardPacket_0x71>
            {
                Packet = BuildPacket(1, board, b =>
                {
                    WriteU32(b, msg);
                    WriteU32(b, 0);
                    WriteCounted(b, poster);
                    WriteCounted(b, subj);
                    WriteCounted(b, "1/1/2026");
                })
            });
        }

        if (!q.Value.OpenMessage) return;
        q.Value.OpenMessage = false;
        commands.EmitTrigger(new PacketReceived<OnBulletinBoardPacket_0x71>
        {
            Packet = BuildPacket(2, board, b =>
            {
                WriteU32(b, 0x100);
                WriteCounted(b, "Seer");
                WriteCounted(b, "Notice #1");
                WriteCounted(b, "1/1/2026");
                WriteU32(b, 0); // message id
                b.Add(0);       // attachments
                b.Add(3);       // lines
                WriteCounted(b, "Hear ye, hear ye!");
                WriteCounted(b, "The town meeting is at dusk.");
                WriteCounted(b, "- The Seer");
            })
        });
    }

    private static OnBulletinBoardPacket_0x71 BuildPacket(byte type, uint board, Action<List<byte>> body)
    {
        var buf = new List<byte> { type };
        WriteU32(buf, board);
        body(buf);
        var pkt = new OnBulletinBoardPacket_0x71();
        pkt.Fill(new IO.StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }

    private static void WriteU32(List<byte> b, uint v)
    {
        b.Add((byte)(v >> 24)); b.Add((byte)(v >> 16)); b.Add((byte)(v >> 8)); b.Add((byte)v);
    }

    private static void WriteCounted(List<byte> b, string s)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        b.Add((byte)(bytes.Length + 1));
        b.AddRange(bytes);
        b.Add(0);
    }

    private static void WriteFixedUtf8(List<byte> b, string s, int size)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        for (int i = 0; i < size; i++)
            b.Add(i < bytes.Length ? bytes[i] : (byte)0);
    }
#endif
}

internal sealed class BulletinParams : CompositeSystemParam
{
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<AssetsServer> Assets;
    public readonly Res<UiZCounter> ZCounter;
    public readonly Res<GameContext> GameCtx;
    public readonly ResMut<BulletinPendingRows> Pending;
    public readonly Query<Data<BulletinBoardWindow>> BoardsQ;
    public readonly Query<Data<BulletinItemWindow>> ItemsQ;
    public readonly Query<Data<Text>> TextQ;
    public readonly Query<Data<UiCustom>> CustomQ;

    public BulletinParams()
    {
        Builder = Add(new Res<GumpBuilder>());
        Assets = Add(new Res<AssetsServer>());
        ZCounter = Add(new Res<UiZCounter>());
        GameCtx = Add(new Res<GameContext>());
        Pending = Add(new ResMut<BulletinPendingRows>());
        BoardsQ = Add(new Query<Data<BulletinBoardWindow>>());
        ItemsQ = Add(new Query<Data<BulletinItemWindow>>());
        TextQ = Add(new Query<Data<Text>>());
        CustomQ = Add(new Query<Data<UiCustom>>());
    }
}

#if AGENT_BUILD
internal sealed class DebugBulletinQueue
{
    public bool Pending;
    // Also emit a type 2 message body (opens the read window, variant 1).
    public bool OpenMessage;
    public uint Serial = 0x4000B0A2;
}
#endif
