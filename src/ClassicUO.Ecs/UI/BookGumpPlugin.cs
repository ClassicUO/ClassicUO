// Book gump — ECS port of Game/UI/Gumps/ModernBookGump.cs.
//
// Server opens a book with 0x93 (old fixed-size header) or 0xD4 (length-
// prefixed header): serial + editable flag + page count + title + author. The
// client replies with a 0x66 page request and the server streams page lines
// back in 0x66; for editable books the client pushes edited pages back through
// 0x66 and the title/author through 0xD4 (or 0x93 when the book was opened
// with the old packet).
//
// Layout mirrors legacy: open-book background 0x1FE, page-flip arrows 0x1FF /
// 0x200, title + author on the left side of the first spread, two 156x166
// 10-line pages per spread (left x=38, right x=223, text at y=26), page number
// labels at y=220.
//
// Legacy runs ONE text box flowing across every page; here each page is an
// independent field, and NormalizeFlow recreates the flow: after every edit
// the whole book is re-wrapped and re-sliced into MaxBookLines-line pages, so
// typing past a page's last line pushes the overflow onto the next page (and
// deleting pulls it back), with the caret/focus following the flowed text —
// flipping the spread when it crosses to a page that isn't displayed.
//
// Movable / right-click-close / topmost-on-click come from the shared
// WindowDrag infra (UiMovable root, pixel-perfect via the 0x1FE sprite).
// Edited pages are flushed on page flip (like legacy SetActivePage) and on
// close — close is detected by FlushClosed diffing a per-frame snapshot
// against the live windows, because the despawn happens inside the shared
// CloseOnRightClick and OnRemove-on-despawn observers are unreliable.

using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Book window root, keyed by the item serial it shows.
internal struct BookWindow
{
    public uint Serial;
    public ushort PageCount;
    public bool Editable;
    // Title/author updates go out as 0xD4 when the book was opened with 0xD4,
    // as 0x93 when it came from the old packet.
    public bool UseNewHeader;
    public int ActivePage; // 1..MaxPage spread index (AP1 = right page only)
    public int SoundedPage; // last spread a page-flip sound played for
    // Read-only books may only receive the currently displayed pages; track
    // which wire pages (1-based) we already have so a flip requests the rest.
    public HashSet<int> KnownPages;
}

// On a page-gated element: shown/hidden by UpdateVisibility from the window's
// ActivePage. Page > 0 is a book page (1-based, page k shows on spread
// (k+1)/2+... see rule in UpdateVisibility); 0 = first-spread header (title /
// author); -1 = back arrow (hidden on the first spread); -2 = forward arrow
// (hidden on the last).
internal struct BookPageVis { public ulong Window; public int Page; }

// On a page's text element (editable glyph or read-only WrappedText custom):
// the owning window + wire page + the text as last agreed with the server, so
// flip/close flushes only pages that actually changed.
internal struct BookPageText { public ulong Window; public int Page; public string Original; }

// On the editable title/author glyphs.
internal struct BookHeaderText { public ulong Window; public bool IsAuthor; public string Original; }

internal readonly struct BookGumpPlugin : IPlugin
{
    private const ushort BackgroundGump = 0x1FE;
    private const ushort BackArrow = 0x1FF, ForwardArrow = 0x200;
    private const int MaxBookLines = 10;
    private const int LeftX = 38, RightX = 223;
    private const int UpperMargin = 26;
    private const int PageWidth = 156, PageHeight = 166;
    private const byte Font = 1; // unicode font 1 (legacy DefaultFont for CV > 2.0)
    private const uint TextColorBlack = 0x010101FF; // RGBA, near-black like profile/tip text

    public void Build(App app)
    {
        app.AddObserver<On<PacketReceived<OnOpenBookPacket_0x93>>, Commands, BookParams>(OnOpenBookOld);
        app.AddObserver<On<PacketReceived<OnOpenBookAltPacket_0xD4>>, Commands, BookParams>(OnOpenBookNew);
        app.AddObserver<On<PacketReceived<OnBookPagesPacket_0x66>>, BookContentParams>(OnBookPages);

        var visFn = UpdateVisibility;
        app.AddSystem(visFn).InStage(Stage.Update).Build();

        // After TextEditPlugin's WriteBack (GuiPlugin registers first; order
        // within a stage follows registration), so the just-typed text is in
        // the field before the book re-flows it across pages.
        var flowFn = NormalizeFlow;
        app.AddSystem(flowFn).InStage(Stage.Update).Build();

        var flushFn = FlushClosed;
        app.AddSystem(flushFn).InStage(Stage.Update).Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

        // Page-flip sound (legacy ModernBookGump SetActivePage on each flip).
        var pageSoundFn = PlayPageFlipSound;
        app.AddSystem(pageSoundFn).InStage(Stage.Update).Build();

        // Book open sound (legacy ModernBookGump SetActivePage on construct).
        // BookWindow inserts once per open; content packets only fill text.
        app.AddObserver((
            OnInsert<BookWindow> _,
            ResMut<AudioState> audio,
            Res<AssetsServer> assets,
            Res<ClassicUO.Configuration.Profile> profile,
            Res<Time> time) =>
            audio.Value.PlaySound(assets.Value, profile.Value, time.Value.Total, 0x0055));

#if AGENT_BUILD
        app.AddResource(new DebugBookQueue());
        Action<Commands, ResMut<DebugBookQueue>> drainFn = DrainDebugBook;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

    private static int MaxPage(ushort pageCount) => (pageCount >> 1) + 1;

    private static void PlayPageFlipSound(
        Query<Data<BookWindow>> windowsQ,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<ClassicUO.Configuration.Profile> profile,
        Res<Time> time)
    {
        foreach (var (_, win) in windowsQ)
        {
            if (win.Ref.ActivePage == win.Ref.SoundedPage) continue;
            win.Ref.SoundedPage = win.Ref.ActivePage;
            audio.Value.PlaySound(assets.Value, profile.Value, time.Value.Total, 0x0055);
        }
    }

    private static void OnOpenBookOld(On<PacketReceived<OnOpenBookPacket_0x93>> trig, Commands commands, BookParams p)
    {
        var pk = trig.Event.Packet;
        BuildBook(commands, p, pk.Serial, pk.PageCount, pk.Title, pk.Author, pk.IsEditable, useNewHeader: false);
    }

    private static void OnOpenBookNew(On<PacketReceived<OnOpenBookAltPacket_0xD4>> trig, Commands commands, BookParams p)
    {
        var pk = trig.Event.Packet;
        BuildBook(commands, p, pk.Serial, pk.PageCount, pk.Title, pk.Author, pk.IsEditable, useNewHeader: true);
    }

    private static void BuildBook(
        Commands commands,
        BookParams p,
        uint serial,
        ushort pageCount,
        string title,
        string author,
        bool editable,
        bool useNewHeader)
    {
        // A repeat open for the same serial rebuilds the window (legacy updates
        // it in place; the server re-streams page content after our request).
        foreach (var (ent, win) in p.ExistingQ)
        {
            if (win.Ref.Serial != serial) continue;
            commands.Entity(ent.Ref).Despawn();
        }

        var assets = p.Assets.Value;
        var builder = p.Builder.Value;
        int bgW = GumpW(assets, BackgroundGump), bgH = GumpH(assets, BackgroundGump);

        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(100), Top = Val.Px(100),
                Width = Val.Px(bgW), Height = Val.Px(bgH),
            })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(p.ZCounter.Value.Bump()));
        var rootId = root.Id;

        var bg = builder.AddGump(commands, BackgroundGump, Vector3.UnitZ, new Vector2(0, 0));
        commands.AddChild(rootId, bg.Id);

        var back = builder.AddButton(commands, (BackArrow, BackArrow, BackArrow), Vector3.UnitZ, new Vector2(0, 0))
            .Insert(new BookPageVis { Window = rootId, Page = -1 });
        back.Observe((On<UiClick> _, Res<NetClient> net, BookFlipParams fp) => Flip(rootId, -1, net.Value, fp));
        commands.AddChild(rootId, back.Id);

        var fwd = builder.AddButton(commands, (ForwardArrow, ForwardArrow, ForwardArrow), Vector3.UnitZ, new Vector2(356, 0))
            .Insert(new BookPageVis { Window = rootId, Page = -2 });
        fwd.Observe((On<UiClick> _, Res<NetClient> net, BookFlipParams fp) => Flip(rootId, +1, net.Value, fp));
        commands.AddChild(rootId, fwd.Id);

        // Title + author on the left side of the first spread (legacy puts them
        // at 40/60 and 40/160 with a "by" label between).
        SpawnHeaderField(commands, rootId, new Vector2(40, 60), title ?? string.Empty, editable, isAuthor: false);
        var by = SpawnWrappedText(commands, ClassicUO.Resources.ResGumps.By, 60, out _);
        commands.Entity(by)
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(40), Top = Val.Px(130),
                Width = Val.Px(60), Height = Val.Px(20),
            })
            .Insert(new BookPageVis { Window = rootId, Page = 0 });
        commands.AddChild(rootId, by);
        SpawnHeaderField(commands, rootId, new Vector2(40, 160), author ?? string.Empty, editable, isAuthor: true);

        // One field per book page; UpdateVisibility shows the active spread's
        // pair. Page k (1-based, == wire page) draws right when odd, left when
        // even — page 1 is the right page of the first spread.
        for (int k = 1; k <= pageCount; k++)
        {
            int x = (k % 2) == 1 ? RightX : LeftX;

            var frame = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    FlexDirection = FlexDirection.Column,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(x), Top = Val.Px(UpperMargin),
                    Width = Val.Px(PageWidth), Height = Val.Px(PageHeight),
                    Overflow = Overflow.Scroll,
                })
                .Insert(new ScrollPosition())
                .Insert(new BookPageVis { Window = rootId, Page = k });
            commands.AddChild(rootId, frame.Id);

            int capPage = k;
            if (editable)
            {
                GuiPlugin.SpawnTextField(commands, frame, new Vector2(0, 0),
                    new TextFont { FontId = Font, Size = 16 }, new ClayColor(30, 30, 30, 255),
                    string.Empty, masked: false,
                    decorate: e => e.Insert(new BookPageText { Window = rootId, Page = capPage, Original = string.Empty }),
                    multiline: true, wrapWidth: PageWidth);
            }
            else
            {
                // Read-only page: a bare WrappedText custom the 0x66 observer
                // fills in. Fixed to the page box; MaxBookLines lines fit.
                var text = commands.Spawn()
                    .Insert(new Node { Display = Display.Flex, Width = Val.Px(PageWidth), Height = Val.Px(PageHeight) })
                    .Insert(new UiCustom
                    {
                        Data = new UOCustomRender
                        {
                            Kind = UOCustomKind.WrappedText,
                            Hue = Vector3.UnitZ,
                            Text = string.Empty,
                            TextFont = Font,
                            WrapWidth = PageWidth,
                            IsHtml = true,
                            HtmlStartColor = TextColorBlack,
                            HtmlBg = false,
                        },
                    })
                    .Insert(new BookPageText { Window = rootId, Page = capPage, Original = string.Empty });
                commands.AddChild(frame.Id, text.Id);
            }

            var label = SpawnWrappedText(commands, k.ToString(), 40, out _);
            commands.Entity(label)
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(x + 80), Top = Val.Px(220),
                    Width = Val.Px(40), Height = Val.Px(20),
                })
                .Insert(new BookPageVis { Window = rootId, Page = k });
            commands.AddChild(rootId, label);
        }

        commands.Entity(rootId).Insert(new BookWindow
        {
            Serial = serial,
            PageCount = pageCount,
            Editable = editable,
            UseNewHeader = useNewHeader,
            ActivePage = 1,
            SoundedPage = 1,
            KnownPages = new HashSet<int>(),
        });

        // Legacy always asks for page 1 on open; servers that push the whole
        // content unsolicited just answer with everything.
        if (p.Net.Value.IsConnected)
            p.Net.Value.Send_BookPageDataRequest(serial, 1);
    }

    // Title/author slot: an editable single-line field for writable books, a
    // plain wrapped-text label otherwise. Both gated to the first spread.
    private static void SpawnHeaderField(Commands commands, ulong rootId, Vector2 pos, string text, bool editable, bool isAuthor)
    {
        if (editable)
        {
            var frame = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                    Width = Val.Px(155), Height = Val.Px(25),
                })
                .Insert(new BookPageVis { Window = rootId, Page = 0 });
            GuiPlugin.SpawnTextField(commands, frame, new Vector2(0, 2),
                new TextFont { FontId = Font, Size = 16 }, new ClayColor(30, 30, 30, 255),
                text, masked: false,
                decorate: e => e.Insert(new BookHeaderText { Window = rootId, IsAuthor = isAuthor, Original = text }));
            commands.AddChild(rootId, frame.Id);
        }
        else
        {
            var label = SpawnWrappedText(commands, text, 155, out _);
            if (label == 0) return;
            commands.Entity(label)
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                    Width = Val.Px(155), Height = Val.Px(25),
                })
                .Insert(new BookPageVis { Window = rootId, Page = 0 });
            commands.AddChild(rootId, label);
        }
    }

    // Bare wrapped-text node (caller positions it by re-inserting the Node).
    private static ulong SpawnWrappedText(Commands commands, string text, int width, out int height)
    {
        height = 0;
        if (string.IsNullOrEmpty(text)) return 0;
        var (w, h) = UoFontRenderer.Measure(text, Font, width, isHtml: true, TextColorBlack, htmlBg: false);
        height = h;

        return commands.Spawn()
            .Insert(new Node { Display = Display.Flex, Width = Val.Px(Math.Max(w, 1)), Height = Val.Px(Math.Max(h, 1)) })
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
                },
            }).Id;
    }

    // 0x66 — apply streamed page lines to the matching window's page fields.
    private static void OnBookPages(On<PacketReceived<OnBookPagesPacket_0x66>> trig, BookContentParams p)
    {
        var packet = trig.Event.Packet;

        ulong rootId = 0;
        bool editable = false;
        ushort pageCount = 0;
        foreach (var (ent, win) in p.WindowsQ)
        {
            if (win.Ref.Serial != packet.Serial) continue;
            rootId = ent.Ref;
            editable = win.Ref.Editable;
            pageCount = win.Ref.PageCount;
            foreach (var page in packet.Pages)
                win.Ref.KnownPages.Add(page.Number);
            break;
        }
        if (rootId == 0 || packet.Pages == null) return;

        foreach (var page in packet.Pages)
        {
            // Server lines are the wrapped visual lines; joining with '\n'
            // reproduces the breaks (the field re-wraps at the same width).
            string text = string.Join("\n", page.Lines);

            foreach (var (_, custom, pageText) in p.PagesQ)
            {
                if (pageText.Ref.Window != rootId || pageText.Ref.Page != page.Number) continue;
                custom.Ref.Render().Text = text;
                pageText.Ref.Original = text;
                break;
            }
        }

        // Editable books flow as one buffer (legacy ServerSetBookText joins all
        // lines and re-wraps); re-slice now and re-baseline Original to the
        // sliced layout so the server-driven shuffle never reads as a user edit.
        if (editable)
        {
            var texts = CollectPageTexts(rootId, pageCount, p.PagesQ);
            var slices = SliceBook(string.Concat(texts), pageCount, out _);
            foreach (var (_, custom, pageText) in p.PagesQ)
            {
                if (pageText.Ref.Window != rootId) continue;
                int k = pageText.Ref.Page;
                if (k < 1 || k > pageCount) continue;
                custom.Ref.Render().Text = slices[k];
                pageText.Ref.Original = slices[k];
            }
        }
    }

    private static string[] CollectPageTexts(ulong rootId, int pageCount, Query<Data<UiCustom, BookPageText>> pagesQ)
    {
        var texts = new string[pageCount + 1];
        Array.Fill(texts, string.Empty);
        foreach (var (_, custom, pageText) in pagesQ)
        {
            if (pageText.Ref.Window != rootId) continue;
            int k = pageText.Ref.Page;
            if (k < 1 || k > pageCount) continue;
            texts[k] = custom.Ref.Render().Text ?? string.Empty;
        }
        texts[0] = string.Empty;
        return texts;
    }

    // Wrap the whole book text and cut it into pages of MaxBookLines visual
    // lines. The
    // wrap MUST be the PLAIN wrap: its line Counts are raw-buffer indices that
    // partition the string exactly. The HTML wrap (what the field renders for
    // its near-black color) reflows/collapses, so its Counts don't add up to
    // the text length and slicing through it EATS characters — same reason
    // TextEditPlugin maps the caret through the plain wrap. Text past the last
    // page is dropped (legacy caps the box at 53*8*pages chars); `consumed`
    // reports how much fit.
    private static string[] SliceBook(string concat, int pageCount, out int consumed)
    {
        var slices = new string[pageCount + 1];
        Array.Fill(slices, string.Empty);
        var lines = UoFontRenderer.WrapLines(concat, Font, PageWidth);
        int li = 0, pos = 0;
        for (int k = 1; k <= pageCount; k++)
        {
            int take = 0;
            for (int j = 0; j < MaxBookLines && li < lines.Count; j++, li++)
                take += lines[li].Count;
            take = Math.Min(take, concat.Length - pos);
            if (take > 0)
            {
                slices[k] = concat.Substring(pos, take);
                pos += take;
            }
        }
        consumed = pos;
        return slices;
    }

    // Re-flow an editable book after a user edit: gather all page texts in
    // order, re-wrap as one buffer, slice back into 8-line pages, and move the
    // caret (and the displayed spread) with the text it sits in. The active
    // editor buffer is resynced in place — SyncActiveEditor only reloads on a
    // focus CHANGE, so a same-field trim would otherwise leave the stale long
    // buffer to be written back next frame, undoing the flow.
    private static void NormalizeFlow(
        ResMut<ActiveTextEdit> edit,
        ResMut<FocusedInput> focused,
        Local<Dictionary<ulong, string>> lastNorm,
        Query<Data<BookWindow>> windowsQ,
        Query<Data<UiCustom, BookPageText>> pagesQ)
    {
        lastNorm.Value ??= new Dictionary<ulong, string>();

        foreach (var (rootEnt, win) in windowsQ)
        {
            if (!win.Ref.Editable) continue;
            ulong root = rootEnt.Ref;
            int pc = win.Ref.PageCount;

            var texts = CollectPageTexts(root, pc, pagesQ);
            var ids = new ulong[pc + 1];
            foreach (var (ent, _, pageText) in pagesQ)
            {
                if (pageText.Ref.Window != root) continue;
                int k = pageText.Ref.Page;
                if (k >= 1 && k <= pc) ids[k] = ent.Ref;
            }

            string concat = string.Concat(texts);
            if (lastNorm.Value.TryGetValue(root, out var prev) && string.Equals(prev, concat, StringComparison.Ordinal))
                continue;

            // Caret as an absolute offset into the whole book, BEFORE slicing.
            int focusedK = 0, caretAbs = -1;
            for (int k = 1; k <= pc; k++)
                if (ids[k] != 0 && ids[k] == focused.Value.Entity) { focusedK = k; break; }
            if (focusedK != 0)
            {
                caretAbs = Math.Clamp(edit.Value.State.Cursor, 0, texts[focusedK].Length);
                for (int k = 1; k < focusedK; k++) caretAbs += texts[k].Length;
            }

            var slices = SliceBook(concat, pc, out int consumed);
            if (caretAbs > consumed) caretAbs = consumed;

            foreach (var (_, custom, pageText) in pagesQ)
            {
                if (pageText.Ref.Window != root) continue;
                int k = pageText.Ref.Page;
                if (k < 1 || k > pc) continue;
                if (!string.Equals(texts[k], slices[k], StringComparison.Ordinal))
                    custom.Ref.Render().Text = slices[k];
            }
            lastNorm.Value[root] = string.Concat(slices);

            if (focusedK == 0 || caretAbs < 0) continue;

            // Land the caret on the page that now holds its text (a boundary
            // caret stays on the earlier page; a char that flowed forward has
            // caretAbs past the boundary and lands on the next page).
            int newK = pc, cumStart = 0;
            for (int k = 1; k <= pc; k++)
            {
                if (caretAbs <= cumStart + slices[k].Length) { newK = k; break; }
                cumStart += slices[k].Length;
            }
            int rel = Math.Clamp(caretAbs - cumStart, 0, slices[newK].Length);

            if (ids[newK] != 0 && focused.Value.Entity != ids[newK])
                focused.Value.Entity = ids[newK];

            var a = edit.Value;
            a.Entity = ids[newK];
            a.Multiline = true;
            a.Masked = false;
            a.WrapWidth = PageWidth;
            a.IsHtml = true;
            a.HtmlStartColor = TextColorBlack;
            a.HtmlBg = false;
            a.FontId = Font;
            a.Buffer.Clear();
            a.Buffer.Append(slices[newK]);
            a.State.Cursor = rel;
            a.State.SelectStart = rel;
            a.State.SelectEnd = rel;

            // Follow the caret across spreads like legacy RealignCaretAndActivePage.
            int spread = (newK >> 1) + 1;
            if (win.Ref.ActivePage != spread)
                win.Ref.ActivePage = spread;
        }
    }

    // Page flip from the arrows. Editable books flush changed pages + header
    // first (legacy SetActivePage); read-only books request the pages of the
    // incoming spread that the server hasn't streamed yet.
    private static void Flip(ulong rootId, int dir, NetClient net, BookFlipParams p)
    {
        if (!p.WindowsQ.TryGet(rootId, out var winRow)) return;
        var (_, win) = winRow;
        int max = MaxPage(win.Ref.PageCount);
        int next = Math.Clamp(win.Ref.ActivePage + dir, 1, max);

        if (win.Ref.Editable)
        {
            FlushWindow(rootId, ref win.Ref, net, p.PagesQ, p.HeadersQ, updateOriginals: true);
        }
        else if (net.IsConnected && next != win.Ref.ActivePage)
        {
            int right = next * 2 - 1, left = next * 2 - 2;
            if (left >= 1 && left <= win.Ref.PageCount && !win.Ref.KnownPages.Contains(left))
                net.Send_BookPageDataRequest(win.Ref.Serial, (ushort)left);
            if (right <= win.Ref.PageCount && !win.Ref.KnownPages.Contains(right))
                net.Send_BookPageDataRequest(win.Ref.Serial, (ushort)right);
        }

        win.Ref.ActivePage = next;
    }

    // Send every dirty piece of an editable book: header via 0xD4/0x93 when
    // title or author moved off the last agreed value, each changed page as 8
    // re-wrapped lines via 0x66.
    private static void FlushWindow(
        ulong rootId,
        ref BookWindow win,
        NetClient net,
        Query<Data<UiCustom, BookPageText>> pagesQ,
        Query<Data<Text, BookHeaderText>> headersQ,
        bool updateOriginals)
    {
        if (!net.IsConnected) return;

        string title = null, author = null;
        bool headerDirty = false;
        foreach (var (_, text, header) in headersQ)
        {
            if (header.Ref.Window != rootId) continue;
            var value = text.Ref.Value ?? string.Empty;
            if (header.Ref.IsAuthor) author = value; else title = value;
            if (!string.Equals(value, header.Ref.Original, StringComparison.Ordinal))
            {
                headerDirty = true;
                if (updateOriginals) header.Ref.Original = value;
            }
        }

        if (headerDirty)
        {
            if (win.UseNewHeader)
                net.Send_BookHeaderChanged(win.Serial, title ?? string.Empty, author ?? string.Empty);
            else
                net.Send_BookHeaderChanged_Old(win.Serial, title ?? string.Empty, author ?? string.Empty);
        }

        foreach (var (_, custom, pageText) in pagesQ)
        {
            if (pageText.Ref.Window != rootId) continue;
            var value = custom.Ref.Render().Text ?? string.Empty;
            if (string.Equals(value, pageText.Ref.Original, StringComparison.Ordinal)) continue;

            net.Send_BookPageData(win.Serial, SplitPageLines(value), pageText.Ref.Page);
            if (updateOriginals) pageText.Ref.Original = value;
        }
    }

    // Re-wrap a page's text the way the renderer draws it and emit up to
    // MaxBookLines lines (clipping overflow, padding with empties). Most servers
    // expect at most 8 lines per page, so trailing empty lines past 8 are
    // trimmed off — lines 9-10 go out only when they actually hold text. Empty
    // lines between text stay; the page's "\n"-only lines already stripped here.
    private static string[] SplitPageLines(string text)
    {
        var result = new string[MaxBookLines];
        var lines = UoFontRenderer.WrapLines(text, Font, PageWidth);
        for (int i = 0; i < MaxBookLines; i++)
        {
            if (i < lines.Count)
            {
                var (start, count, _, _) = lines[i];
                result[i] = text.Substring(start, Math.Min(count, text.Length - start)).Replace("\n", "");
            }
            else
            {
                result[i] = string.Empty;
            }
        }

        int lineCount = MaxBookLines;
        while (lineCount > 8 && result[lineCount - 1].Length == 0)
            lineCount--;
        if (lineCount < result.Length)
            Array.Resize(ref result, lineCount);

        return result;
    }

    // Show/hide page-gated elements from their window's ActivePage. Spread AP
    // shows page k when k == 2*AP-1 (right) or k == 2*AP-2 (left); the header
    // belongs to the first spread; the arrows clamp at the ends.
    private static void UpdateVisibility(
        Query<Data<Node, BookPageVis>> visQ,
        Query<Data<BookWindow>> windowsQ)
    {
        foreach (var (_, node, vis) in visQ)
        {
            if (!windowsQ.TryGet(vis.Ref.Window, out var winRow)) continue;
            var (_, win) = winRow;
            int ap = win.Ref.ActivePage;

            bool visible = vis.Ref.Page switch
            {
                0 => ap == 1,
                -1 => ap > 1,
                -2 => ap < MaxPage(win.Ref.PageCount),
                var k => k == ap * 2 - 1 || k == ap * 2 - 2,
            };

            var target = visible ? Display.Flex : Display.None;
            if (node.Ref.Display != target)
                node.Ref.Display = target;
        }
    }

    private sealed class BookSnapshot
    {
        public BookWindow Window;
        public string Title, Author, TitleOriginal, AuthorOriginal;
        public readonly List<(int Page, string Text, string Original)> Pages = new();
    }

    // Closing a book must push pending edits (legacy flushes from
    // CloseWithRightClick), but the despawn happens inside the shared
    // WindowDragPlugin path. Snapshot every editable book's live text each
    // frame; when a window vanishes from the query, diff its last snapshot and
    // send what changed.
    private static void FlushClosed(
        Res<NetClient> net,
        Local<Dictionary<ulong, BookSnapshot>> snaps,
        Query<Data<BookWindow>> windowsQ,
        Query<Data<UiCustom, BookPageText>> pagesQ,
        Query<Data<Text, BookHeaderText>> headersQ)
    {
        snaps.Value ??= new Dictionary<ulong, BookSnapshot>();

        List<ulong> gone = null;
        foreach (var (root, snap) in snaps.Value)
        {
            if (windowsQ.Contains(root)) continue;
            (gone ??= new List<ulong>()).Add(root);

            if (!net.Value.IsConnected) continue;
            bool headerDirty = !string.Equals(snap.Title, snap.TitleOriginal, StringComparison.Ordinal)
                || !string.Equals(snap.Author, snap.AuthorOriginal, StringComparison.Ordinal);
            if (headerDirty)
            {
                if (snap.Window.UseNewHeader)
                    net.Value.Send_BookHeaderChanged(snap.Window.Serial, snap.Title ?? string.Empty, snap.Author ?? string.Empty);
                else
                    net.Value.Send_BookHeaderChanged_Old(snap.Window.Serial, snap.Title ?? string.Empty, snap.Author ?? string.Empty);
            }
            foreach (var (page, text, original) in snap.Pages)
            {
                if (string.Equals(text, original, StringComparison.Ordinal)) continue;
                net.Value.Send_BookPageData(snap.Window.Serial, SplitPageLines(text), page);
            }
        }
        if (gone != null)
            foreach (var root in gone)
                snaps.Value.Remove(root);

        foreach (var (ent, win) in windowsQ)
        {
            if (!win.Ref.Editable) continue;
            if (!snaps.Value.TryGetValue(ent.Ref, out var snap))
                snaps.Value[ent.Ref] = snap = new BookSnapshot();
            snap.Window = win.Ref;
            snap.Pages.Clear();

            foreach (var (_, text, header) in headersQ)
            {
                if (header.Ref.Window != ent.Ref) continue;
                if (header.Ref.IsAuthor) { snap.Author = text.Ref.Value; snap.AuthorOriginal = header.Ref.Original; }
                else { snap.Title = text.Ref.Value; snap.TitleOriginal = header.Ref.Original; }
            }
            foreach (var (_, custom, pageText) in pagesQ)
            {
                if (pageText.Ref.Window != ent.Ref) continue;
                snap.Pages.Add((pageText.Ref.Page, custom.Ref.Render().Text, pageText.Ref.Original));
            }
        }
    }

    private static void Despawn(
        Commands commands,
        Query<Data<TinyEcs.Children>, Filter<With<BookWindow>>> windowsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

#if AGENT_BUILD
    // Drains debug.openBook by replaying the same triggers the network path
    // fires: an 0xD4 open on the first frame, then a 0x66 with sample page
    // lines on the next (the window's entities exist only after the open's
    // commands apply).
    private static void DrainDebugBook(Commands commands, ResMut<DebugBookQueue> q)
    {
        if (q.Value.PendingPages)
        {
            q.Value.PendingPages = false;
            if (q.Value.PageLines != null)
                commands.EmitTrigger(new PacketReceived<OnBookPagesPacket_0x66>
                {
                    Packet = BuildDebugPagesPacket(q.Value.Serial, q.Value.PageLines)
                });
            return;
        }

        if (!q.Value.PendingOpen) return;
        q.Value.PendingOpen = false;
        q.Value.PendingPages = true;
        commands.EmitTrigger(new PacketReceived<OnOpenBookAltPacket_0xD4>
        {
            Packet = BuildDebugOpenPacket(q.Value.Serial, q.Value.Editable, q.Value.PageCount, q.Value.Title, q.Value.Author)
        });
    }

    private static OnOpenBookAltPacket_0xD4 BuildDebugOpenPacket(uint serial, bool editable, ushort pages, string title, string author)
    {
        var buf = new List<byte>();
        void U16(ushort v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        buf.Add((byte)(serial >> 24)); buf.Add((byte)(serial >> 16)); buf.Add((byte)(serial >> 8)); buf.Add((byte)serial);
        buf.Add(1);
        buf.Add((byte)(editable ? 1 : 0));
        U16(pages);
        U16((ushort)(title ?? string.Empty).Length);
        foreach (var c in title ?? string.Empty) buf.Add((byte)c);
        U16((ushort)(author ?? string.Empty).Length);
        foreach (var c in author ?? string.Empty) buf.Add((byte)c);

        var pkt = new OnOpenBookAltPacket_0xD4();
        pkt.Fill(new StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }

    private static OnBookPagesPacket_0x66 BuildDebugPagesPacket(uint serial, List<List<string>> pageLines)
    {
        var buf = new List<byte>();
        void U16(ushort v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        buf.Add((byte)(serial >> 24)); buf.Add((byte)(serial >> 16)); buf.Add((byte)(serial >> 8)); buf.Add((byte)serial);
        U16((ushort)pageLines.Count);
        for (int i = 0; i < pageLines.Count; i++)
        {
            U16((ushort)(i + 1));
            U16((ushort)pageLines[i].Count);
            foreach (var line in pageLines[i])
            {
                foreach (var c in line) buf.Add((byte)c);
                buf.Add(0);
            }
        }

        var pkt = new OnBookPagesPacket_0x66();
        pkt.Fill(new StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif
}

internal sealed class BookParams : CompositeSystemParam
{
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<AssetsServer> Assets;
    public readonly Res<UiZCounter> ZCounter;
    public readonly Res<NetClient> Net;
    public readonly Query<Data<BookWindow>> ExistingQ;

    public BookParams()
    {
        Builder = Add(new Res<GumpBuilder>());
        Assets = Add(new Res<AssetsServer>());
        ZCounter = Add(new Res<UiZCounter>());
        Net = Add(new Res<NetClient>());
        ExistingQ = Add(new Query<Data<BookWindow>>());
    }
}

internal sealed class BookContentParams : CompositeSystemParam
{
    public readonly Query<Data<BookWindow>> WindowsQ;
    public readonly Query<Data<UiCustom, BookPageText>> PagesQ;

    public BookContentParams()
    {
        WindowsQ = Add(new Query<Data<BookWindow>>());
        PagesQ = Add(new Query<Data<UiCustom, BookPageText>>());
    }
}

internal sealed class BookFlipParams : CompositeSystemParam
{
    public readonly Query<Data<BookWindow>> WindowsQ;
    public readonly Query<Data<UiCustom, BookPageText>> PagesQ;
    public readonly Query<Data<Text, BookHeaderText>> HeadersQ;

    public BookFlipParams()
    {
        WindowsQ = Add(new Query<Data<BookWindow>>());
        PagesQ = Add(new Query<Data<UiCustom, BookPageText>>());
        HeadersQ = Add(new Query<Data<Text, BookHeaderText>>());
    }
}

#if AGENT_BUILD
internal sealed class DebugBookQueue
{
    public bool PendingOpen;
    public bool PendingPages;
    public uint Serial;
    public bool Editable;
    public ushort PageCount;
    public string Title = string.Empty, Author = string.Empty;
    public List<List<string>> PageLines;
}
#endif
