using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal struct TextOverheadEvent
{
    public uint Serial;
    public string Text;
    public string Name;
    public ushort Hue;
    public byte Font;
    public bool IsUnicode;   // 0xAE/cliloc = unicode font; 0x1C = ascii font
    public MessageType MessageType;
    public float Time;
}

internal readonly struct TextOverheadPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new TextOverHeadManager());

        var readTextOverHeadFn = ReadTextOverhead;
        app.AddSystem(readTextOverHeadFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<TextOverheadEvent> texts) => texts.HasEvents)
            .Build();
    }

    private static void ReadTextOverhead(
        Res<Time> time,
        Res<Profile> profile,
        EventReader<TextOverheadEvent> texts,
        EventWriter<Modding.Host.HostMessage> hostMsgs,
        Res<TextOverHeadManager> textOverHeadManager
    )
    {
        foreach (var text in texts.Read())
        {
            // Mirror legacy MessageManager.HandleMessage: render overhead for
            // EVERY message type (its `default:` arm) except guild/alliance
            // (journal-only) and the party-class types, which go overhead only
            // when the profile opts in. The old allow-list silently dropped any
            // type it didn't enumerate — a single-click name sent with an
            // unusual/less-common MessageType then never rendered. System-serial
            // (broadcast) lines never reach here; the packet handlers route
            // those to the system log.
            var render = text.MessageType switch
            {
                MessageType.Guild or MessageType.Alliance => false,
                MessageType.Command or MessageType.Encoded
                    or MessageType.System or MessageType.Party
                    => profile.Value.OverheadPartyMessages,
                _ => true,
            };
            if (!render)
                continue;

            var copyText = text;
            if (text.MessageType == MessageType.Party)
                copyText.Hue = profile.Value.PartyMessageHue;
            copyText.Time = time.Value.Total + TimeToLive(profile.Value, in text);

            textOverHeadManager.Value.Append(copyText);

            hostMsgs.Send(new Modding.Host.HostMessage.MessageReceived(
                copyText.MessageType,
                copyText.Text,
                copyText.Name,
                copyText.Serial,
                copyText.Hue,
                copyText.Font
            ));
        }
    }

    // Legacy MessageManager.CalculateTimeToLive: scaled mode keys the lifetime
    // to the wrapped line count (4s per line at delay 100); unscaled mode is
    // the fixed-point ~40ms-per-delay-unit form, ported bit-for-bit. Lines are
    // recovered from the measured wrapped height (legacy reads
    // RenderedText.LinesCount; the ECS layout doesn't expose it).
    private static float TimeToLive(Profile profile, in TextOverheadEvent t)
    {
        if (!profile.ScaleSpeechDelay)
        {
            long delay = (5497558140000L * profile.SpeechDelay) >> 32 >> 5;
            return (delay >> 31) + delay;
        }

        int speechDelay = Math.Max(10, profile.SpeechDelay);
        var fontId = t.IsUnicode ? t.Font : (ushort)(t.Font | UoFontRuntime.AsciiFlag);
        var (_, height) = UoFontRenderer.MeasureFont(t.Text, fontId, TextOverHeadManager.MaxWidth, allowHtml: false);
        var (_, lineHeight) = UoFontRenderer.MeasureFont("W", fontId, TextOverHeadManager.MaxWidth, allowHtml: false);
        int lines = lineHeight > 0 ? Math.Max(1, (height + lineHeight / 2) / lineHeight) : 1;
        return 4000f * lines * speechDelay / 100f;
    }
}

internal sealed class TextOverHeadManager
{
    // Wrap width for an overhead line (legacy ItemView/overhead uses ~200px).
    internal const int MaxWidth = 200;

    private readonly List<uint> _toRemove = new();
    private readonly Dictionary<uint, LinkedList<TextOverheadEvent>> _textOverHeadMap = new();
    private readonly LinkedList<LinkedList<TextOverheadEvent>> _mainLinkedList = new();

    public void Append(TextOverheadEvent text)
    {
        if (!_textOverHeadMap.TryGetValue(text.Serial, out var list))
        {
            list = new();
            _textOverHeadMap[text.Serial] = list;
        }

        if (list.Count >= 5)
            list.RemoveFirst();
        list.AddLast(text);

        _mainLinkedList.Remove(list);
        _mainLinkedList.AddLast(list);
    }

    public void Update(Time time, NetworkEntitiesMap networkEntities,
        IReadOnlyDictionary<uint, Rectangle> gumpAnchors = null)
    {
        foreach ((var serial, var list) in _textOverHeadMap)
        {
            // Read-only lookup: Get(commands, ...) returns a default
            // EntityCommands for unmapped serials whose .Id throws.
            // An item shown inside a gump (container slot, paperdoll equipment)
            // is anchored by serial but may not live in NetworkEntitiesMap —
            // equipped items in particular are tracked via EquipmentSlots, not
            // the world map. Keep its name as long as it has a live gump anchor,
            // else its single-click label would be purged the same frame it
            // arrives and never render.
            bool anchored = gumpAnchors != null && gumpAnchors.ContainsKey(serial);
            if ((!anchored && !networkEntities.TryGet(serial, out _)) || list.Count == 0)
            {
                _toRemove.Add(serial);
                continue;
            }

            var first = list.First;
            while (first != null)
            {
                var next = first.Next;

                if (first.Value.Time <= time.Total)
                    list.Remove(first);
                first = next;
            }
        }

        if (_toRemove.Count > 0)
        {
            foreach (var serial in _toRemove)
            {
                if (_textOverHeadMap.Remove(serial, out var list))
                    _mainLinkedList.Remove(list);
            }
            _toRemove.Clear();
        }
    }

    // Resolve the engine font id from the packet data: unicode keeps the raw
    // font index, ascii sets the AsciiFlag so UoFontRenderer uses the .mul
    // ascii path (and reads the hue from the tint's R/G bytes).
    private static ushort FontId(in TextOverheadEvent t)
        => t.IsUnicode ? t.Font : (ushort)(t.Font | UoFontRuntime.AsciiFlag);

    // Mirror legacy MessageManager.CreateMessage hue handling: mask to the hue
    // bits and clamp out-of-range values so an oversized packet hue doesn't
    // index garbage in the palette (wrong/weird colour).
    private static ushort NormalizeHue(ushort hue)
    {
        ushort c = (ushort)(hue & 0x3FFF);
        if (c >= 0x0BB8) c = 1;
        return c;
    }

    // Draws into the CALLER's already-open batch — invoked from inside
    // GuiRenderingPlugin's Phase 1 (the UI render target, logical pixels), the
    // only place UoFontRenderer's atlas path renders. Coords are RT-local logical
    // (world panel origin + camera.WorldToScreen); the Phase 2 blit scales them.
    // `plateTops` (optional): serial -> screen Y of that entity's visible
    // nameplate top (NameplateState.PlateTops). Speech for a plated entity
    // stacks upward from the PLATE's top edge instead of the head anchor, so
    // the two never overlap (legacy UpdateTextCoordsV shifts by the object-
    // handles gump height while it's displaying).
    // Overhead text sits above every world object but under gump windows:
    // beat any world render depth, lose float.MaxValue UI claims on ties.
    private const float PickDepth = float.MaxValue / 2f;

    // One message block placed this frame, in draw order (index 0 = bottom,
    // last = topmost — _mainLinkedList order, newest speaker last).
    private struct DrawEntry
    {
        public string Text;
        public ushort FontId;
        public ushort Hue;
        public ulong Ent;
        public uint Serial;
        public Rectangle Rect;
        public bool Transparent;
    }

    // Per-frame scratch; Render is single-threaded (GuiRenderingPlugin).
    private readonly List<DrawEntry> _drawList = new();

    public void Render(
        NetworkEntitiesMap networkEntities,
        UltimaBatcher2D batch,
        GameContext gameCtx,
        Camera camera,
        Query<Data<WorldPosition, ScreenPositionOffset>> query,
        MouseContext mouse,
        SelectedEntity selected,
        IReadOnlyDictionary<uint, float> plateTops = null
    )
    {
        var center = Isometric.IsoToScreen(gameCtx.CenterX, gameCtx.CenterY, gameCtx.CenterZ);
        var windowSize = new Vector2(camera.Bounds.Width, camera.Bounds.Height);
        center -= windowSize / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.CenterOffset;

        var panelOrigin = new Vector2(camera.Bounds.X, camera.Bounds.Y);
        var bounds = camera.Bounds;

        // Mouse in the same logical UI-RT space the text draws in. Hits only
        // count inside the game window — the scissor trims anything outside,
        // so an offscreen-clamped line must not pick.
        var mousePos = mouse.Position;
        bool mouseInView = bounds.Contains((int)mousePos.X, (int)mousePos.Y);

        // Pass 1: place every message block (rect only, no draw yet) so the
        // overlap pass below can see the full frame's layout.
        _drawList.Clear();

        foreach (var list in _mainLinkedList)
        {
            if (list.Count == 0 || list.First == null)
                continue;

            if (!networkEntities.TryGet(list.First.Value.Serial, out var entId) || !query.TryGet(entId, out var posRow))
                continue;

            (var worldPos, var offset) = posRow;

            var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
            if (!Unsafe.IsNullRef(ref offset))
                position += offset.Ref.Value;
            position -= center;
            position.X += 22f;
            position.Y += 22f;
            position.Y -= Constants.DEFAULT_CHARACTER_HEIGHT * 5;
            position = camera.WorldToScreen(position) + panelOrigin;

            // Newest line sits just above the head; older lines stack upward.
            // A visible nameplate owns the head slot — start above it instead.
            float y = position.Y;
            if (plateTops != null
                && plateTops.TryGetValue(list.First.Value.Serial, out var plateTop)
                && plateTop < y)
                y = plateTop;
            int stackStart = _drawList.Count;
            for (var node = list.Last; node != null; node = node.Previous)
            {
                var t = node.Value;
                if (string.IsNullOrEmpty(t.Text)) continue;

                var fontId = FontId(t);
                // Speech is plain text — don't let '<' trigger the HTML parser
                // (it would swallow the '<' and any "tag" after it).
                var (w, h) = UoFontRenderer.MeasureFont(t.Text, fontId, MaxWidth, allowHtml: false);
                if (h <= 0) continue;

                y -= h;

                // Clamp into the game window (legacy FixTextCoordinatesInScreen:
                // +4 left pad, each line clamped independently) so speech from
                // edge-of-screen entities stays readable. The bottom edge is
                // deliberately NOT clamped: text from entities below the view
                // would get pulled up over the bottom-center strip (system log /
                // chat bar) — let the game-window scissor trim it instead.
                int x = (int)(position.X - w / 2f);
                x = Math.Clamp(x, bounds.X + 4, Math.Max(bounds.X + 4, bounds.Right - w));
                int dy = (int)y;
                if (dy < bounds.Y) dy = bounds.Y;

                _drawList.Add(new DrawEntry
                {
                    Text = t.Text,
                    FontId = fontId,
                    Hue = t.Hue,
                    Ent = entId,
                    Serial = t.Serial,
                    Rect = new Rectangle(x, dy, w, h),
                });
            }

            // Placement walks newest->oldest (y stacks upward from the head);
            // draw order needs oldest-first so the newest line is topmost —
            // when the top-edge clamp piles lines onto the same y, the OLDER
            // line goes transparent, not the fresh one (legacy AddMessage
            // inserts new text at the head = highest priority).
            _drawList.Reverse(stackStart, _drawList.Count - stackStart);
        }

        // Pass 2: hover pick — topmost pixel-hit message wins (scan back to
        // front). Legacy TextRenderer.Draw: pixel-perfect check against the
        // rendered glyphs (border included) selects the text — claims the
        // OWNER entity, so click/dclick/targeting on the speech act on the
        // speaker (legacy maps TextObject -> ov.Owner).
        int hoverIdx = -1;
        if (mouseInView)
        {
            for (int i = _drawList.Count - 1; i >= 0; i--)
            {
                var e = _drawList[i];
                if (e.Rect.Contains((int)mousePos.X, (int)mousePos.Y)
                    && UoFontRenderer.PixelHit(
                        e.Text, e.FontId, MaxWidth,
                        (int)mousePos.X - e.Rect.X, (int)mousePos.Y - e.Rect.Y,
                        allowHtml: false, border: true))
                {
                    hoverIdx = i;
                    break;
                }
            }
        }

        if (hoverIdx >= 0)
        {
            var e = _drawList[hoverIdx];
            selected.Set(e.Ent, PickDepth, isText: true);

            // Legacy GameController.Render: a hovered world TextObject gets
            // WorldTextManager.MoveToTop every frame — it lifts above all
            // other speech and STAYS there after the cursor leaves. Lift the
            // hovered message to the end of this frame's draw order (drawn
            // last = on top, exempt from the fade below) and persist the
            // speaker's stack at the list tail for subsequent frames.
            MoveToTop(e.Serial);
            _drawList.RemoveAt(hoverIdx);
            _drawList.Add(e);
            hoverIdx = _drawList.Count - 1;
        }

        var entries = CollectionsMarshal.AsSpan(_drawList);

        // Pass 3: legacy TextRenderer.ProcessWorldText(doit)/Collides — a
        // message whose rect is overlapped by ANY message drawn above it
        // (later in draw order) renders half transparent, so the covering
        // text stays readable. Newest speech is topmost (Append moves the
        // speaker's stack to the list tail) and stays opaque.
        for (int i = 0; i < entries.Length; i++)
            for (int j = i + 1; j < entries.Length && !entries[i].Transparent; j++)
                if (entries[i].Rect.Intersects(entries[j].Rect))
                    entries[i].Transparent = true;

        // Pass 4: draw, bottom to top. Highlight (hue 0x35) only when the
        // claim actually won the frame (selected.Entity is last frame's
        // resolved pick), so a gump overlapping the text suppresses it like
        // legacy.
        for (int i = 0; i < entries.Length; i++)
        {
            ref var e = ref entries[i];

            var hue = i == hoverIdx && selected.Entity == e.Ent
                ? (ushort)0x0035
                : NormalizeHue(e.Hue);
            // 0x7F/255 — same half-fade legacy applies to covered text.
            UoFontRenderer.Draw(batch, e.Text, e.FontId, hue, e.Rect.X, e.Rect.Y, MaxWidth, 0f,
                allowHtml: false, alpha: e.Transparent ? 0x7F / 255f : 1f);
        }
    }

    // Floating name text for an item that lives inside a gump (a container
    // slot, a paperdoll equipment overlay). Such an item has no WorldPosition,
    // so the world Render above skips it (its query.TryGet fails). Legacy routes
    // these into the owning gump's AddText (MessageManager.cs:222 -> Container/
    // PaperDollGump.AddText); we anchor at the item's on-screen UI rect instead
    // and stack the lines upward from the slot's top edge. Called from
    // GuiRenderingPlugin after every gump has painted, so it sits on top and is
    // unclipped (a gump label is not world-clipped). `uiAnchors` maps the item
    // serial to its ComputedNode rect (logical UI-RT pixels — the same space
    // this batch draws in). No hover-pick / overlap-fade: a transient single
    // name needs none.
    public void RenderGumpAnchored(
        UltimaBatcher2D batch,
        IReadOnlyDictionary<uint, Rectangle> uiAnchors)
    {
        foreach (var list in _mainLinkedList)
        {
            if (list.Count == 0 || list.First == null)
                continue;
            if (!uiAnchors.TryGetValue(list.First.Value.Serial, out var rect))
                continue;

            float centerX = rect.X + rect.Width / 2f;
            float y = rect.Y;
            for (var node = list.Last; node != null; node = node.Previous)
            {
                var t = node.Value;
                if (string.IsNullOrEmpty(t.Text)) continue;

                var fontId = FontId(t);
                var (w, h) = UoFontRenderer.MeasureFont(t.Text, fontId, MaxWidth, allowHtml: false);
                if (h <= 0) continue;

                y -= h;
                int x = (int)(centerX - w / 2f);
                UoFontRenderer.Draw(batch, t.Text, fontId, NormalizeHue(t.Hue),
                    x, (int)y, MaxWidth, 0f, allowHtml: false, alpha: 1f);
            }
        }
    }

    // Legacy WorldTextManager.MoveToTop: lift a speaker's overhead stack to
    // the top of the z-order (drawn last). Called on hover; also the reason
    // hovered speech stays on top after the cursor moves away.
    public void MoveToTop(uint serial)
    {
        if (_textOverHeadMap.TryGetValue(serial, out var list))
        {
            _mainLinkedList.Remove(list);
            _mainLinkedList.AddLast(list);
        }
    }

    // Test seam: stack z-order, bottom to top (one serial per stack).
    internal IEnumerable<uint> ZOrderSerials()
    {
        foreach (var list in _mainLinkedList)
            if (list.First != null)
                yield return list.First.Value.Serial;
    }
}
