// System-message log — the bottom-left scroll of server messages in the game
// scene (legacy SystemChatControl's ChatLineTime list). Server text arrives via
// cliloc (0xC1) and system speech (0x1C / 0xAE with serial 0xFFFFFFFF); each
// line shows for 10s (TIME_DISPLAY_SYSTEM_MESSAGE_TEXT) then drops off, up to 30
// lines. When the incoming text matches the last line, the last line is
// collapsed to "text [xN]" instead of adding a duplicate.

using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Emitted by packet handlers for any server text that belongs in the system log.
internal struct SystemMessageEvent
{
    public string Text;
    public ushort Hue;
    public byte Font;
}

internal sealed class SystemMessages
{
    internal sealed class Entry
    {
        public string BaseText;  // text as received, without the [xN] suffix
        public string Display;   // what gets drawn (BaseText, or "BaseText [xN]")
        public ushort Hue;
        public byte Font;
        public int Count;
        public float Expire;     // Time.Total when this line drops off
    }

    private const int Max = 30;
    private const float Lifetime = Game.Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT;

    public readonly List<Entry> Entries = new();

    public void Add(string text, ushort hue, byte font, float now)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Collapse an immediate repeat of the last line into "text [xN]".
        if (Entries.Count > 0)
        {
            var last = Entries[^1];
            if (last.BaseText == text)
            {
                last.Count++;
                last.Display = $"{text} [x{last.Count}]";
                last.Hue = hue;
                last.Font = font;
                last.Expire = now + Lifetime;
                return;
            }
        }

        if (Entries.Count >= Max) Entries.RemoveAt(0);
        Entries.Add(new Entry
        {
            BaseText = text,
            Display = text,
            Hue = hue,
            Font = font,
            Count = 1,
            Expire = now + Lifetime,
        });
    }

    public void Expire(float now)
    {
        for (int i = Entries.Count - 1; i >= 0; i--)
            if (Entries[i].Expire <= now)
                Entries.RemoveAt(i);
    }
}

internal readonly struct SystemMessagePlugin : IPlugin
{
    private const int FontSize = 18;
    private const int LeftMargin = 6;
    private const int BottomMargin = 6;
    private const int ChatBarHeight = 22; // input line + its translucent bar
    private const int BottomInset = 6;    // keep the bar off the very viewport edge
    private const int MaxWidth = 320;     // legacy ChatLineTime RenderedText maxWidth

    public void Build(App app)
    {
        app.AddResource(new SystemMessages());

        var consumeFn = Consume;
        app.AddSystem(consumeFn).InStage(Stage.Update)
            .RunIf((EventReader<SystemMessageEvent> r) => r.HasEvents)
            .Build();

        // Draw into the world render target (bound between the begin/end
        // rendering systems), over the terrain, anchored to the viewport's
        // bottom-left like legacy SystemChatControl.
        var renderFn = Render;
        app.AddSystem(renderFn).InStage(Stage.PostUpdate).SingleThreaded()
            .After("cuo:rendering:rendering")
            .Before("cuo:rendering:end")
            .RunIf((Commands cmds) => cmds.HasResource<GraphicsDevice>())
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .Build();
    }

    private static void Consume(
        Res<Time> time,
        Res<SystemMessages> msgs,
        EventReader<SystemMessageEvent> reader)
    {
        foreach (var ev in reader.Read())
            msgs.Value.Add(ev.Text, ev.Hue, ev.Font, time.Value.Total);
    }

    private static void Render(
        Res<UltimaBatcher2D> batch,
        Res<SystemMessages> msgs,
        Res<Camera> camera,
        Res<UOFileManager> files,
        Res<Time> time,
        Local<StringBuilder> sb,
        Local<List<string>> wrapBuf)
    {
        msgs.Value.Expire(time.Value.Total);

        if (msgs.Value.Entries.Count == 0) return;

        sb.Value ??= new StringBuilder();
        wrapBuf.Value ??= new List<string>();
        var b = batch.Value;
        var bounds = camera.Value.Bounds;
        b.Begin();

        // Leave room for the chat input field (now a Clay UI bar, ChatPlugin)
        // pinned to the bottom — system messages stack upward from just above it.
        float barTop = bounds.Height - BottomInset - ChatBarHeight;

        // System messages stack upward from just above the chat bar. Each line
        // wraps at a fixed width (legacy ChatLineTime: maxWidth 320).
        float y = barTop - BottomMargin;
        for (int i = msgs.Value.Entries.Count - 1; i >= 0 && y >= 0; i--)
        {
            var e = msgs.Value.Entries[i];
            var dynFont = FontCache.GetFont(e.Font == 0 ? (byte)0 : e.Font).GetFont(FontSize);
            var color = GetColor(files.Value.Hues, e.Hue);

            WrapLines(dynFont, e.Display, MaxWidth, sb.Value, wrapBuf.Value);

            // Draw the wrapped lines bottom-up so the block reads top-to-bottom.
            for (int li = wrapBuf.Value.Count - 1; li >= 0; li--)
            {
                sb.Value.Clear();
                sb.Value.Append(wrapBuf.Value[li]);
                var size = dynFont.MeasureString(sb.Value);
                y -= size.Y;
                if (y < 0) break;
                dynFont.DrawText(b, sb.Value, new Vector2(LeftMargin, y), color,
                    effect: FontStashSharp.FontSystemEffect.Stroked, effectAmount: 1);
            }
        }

        b.End();
    }

    // Greedy word-wrap at maxWidth (px). Breaks at the last whitespace that
    // fits, or mid-word if a single token is too long. Fills `lines`.
    private static void WrapLines(FontStashSharp.DynamicSpriteFont font, string text, float maxWidth, StringBuilder sb, List<string> lines)
    {
        lines.Clear();
        int start = 0;
        while (start < text.Length)
        {
            int lastSpace = -1;
            int i = start;
            for (; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i])) lastSpace = i;
                sb.Clear();
                sb.Append(text, start, i - start + 1);
                if (font.MeasureString(sb).X > maxWidth) break;
            }

            int cut = i >= text.Length ? text.Length : (lastSpace > start ? lastSpace : i);
            if (cut <= start) cut = Math.Min(start + 1, text.Length); // guard: always advance
            lines.Add(text.Substring(start, cut - start));

            start = cut;
            while (start < text.Length && char.IsWhiteSpace(text[start])) start++;
        }
        if (lines.Count == 0) lines.Add(string.Empty);
    }

    private static Color GetColor(HuesLoader hues, ushort hue)
    {
        var packed = HuesHelper.Color16To32(
            hues.GetColor16(HuesHelper.ColorToHue(Color.White), hue > 0 ? hue : ushort.MinValue)) | 0xFF_00_00_00;
        return new Color { PackedValue = packed };
    }
}
