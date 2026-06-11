using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            switch (text.MessageType)
            {
                // Party lines go overhead only when the profile opts in (legacy
                // MessageManager's Party case); the journal capture keeps its
                // copy regardless.
                case MessageType.Party when profile.Value.OverheadPartyMessages:
                case MessageType.Regular:
                case MessageType.Emote:
                case MessageType.Focus:
                case MessageType.Spell:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Label:
                case MessageType.Limit3Spell:
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
                    break;
            }
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

    public void Update(Time time, NetworkEntitiesMap networkEntities)
    {
        foreach ((var serial, var list) in _textOverHeadMap)
        {
            // Read-only lookup: Get(commands, ...) returns a default
            // EntityCommands for unmapped serials whose .Id throws.
            if (!networkEntities.TryGet(serial, out _) || list.Count == 0)
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
    public void Render(
        NetworkEntitiesMap networkEntities,
        UltimaBatcher2D batch,
        GameContext gameCtx,
        Camera camera,
        Query<Data<WorldPosition, ScreenPositionOffset>> query,
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

                UoFontRenderer.Draw(batch, t.Text, fontId, NormalizeHue(t.Hue), x, dy, MaxWidth, 0f, allowHtml: false);
            }
        }
    }
}
