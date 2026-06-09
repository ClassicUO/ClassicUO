// Host-side stb_textedit integration for the shared editable text fields
// (login, chat, server-gump entries — anything SpawnTextField makes). Drives
// the StbTextEdit state machine from one ActiveTextEdit resource (only one
// field holds keyboard focus at a time), so the fields get a real caret index,
// arrow/home/end navigation, shift-selection, click + drag selection, and
// Ctrl+A/C/X/V/Z — instead of naive append/backspace.
//
// Rendering: SpawnTextField gives each field two absolute overlay nodes — a
// selection rect (behind the glyphs) and a 2px caret bar — positioned every
// frame by measuring the display text up to the cursor / selection bounds with
// UoFontRuntime. Only the focused field's overlays are shown.
//
// Fields with bespoke editors (split number box, skills rename) carry TextInput
// but NOT EditableText, so they keep their own readers + flex caret (CaretBlink)
// and are untouched here.

using System;
using System.Text;
using ClassicUO.Input;
using Microsoft.Xna.Framework.Input;
using StbTextEdit;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// The single live text editor: the StbTextEdit state + a buffer for the focused
// field. Also the ITextEditHandler so StbTextEdit can read/measure/mutate the
// buffer. Width queries go through UoFontRuntime in the field's font so the
// caret / selection / click hit-testing line up with the rendered glyphs.
internal sealed class ActiveTextEdit : ITextEditHandler
{
    public ulong Entity;            // focused editable glyph, 0 = none
    public ushort FontId;
    public bool Masked;
    public char MaskChar = '*';
    public bool Multiline;          // value lives in a WrappedText custom; '\n' allowed
    public int WrapWidth = 200;     // wrap column for multiline fields
    // The multiline glyph renders through a WrappedText custom that may be HTML
    // (it is, for the near-black profile text). WrapLines must measure with the
    // same flags or the per-line height — and so the caret Y — drifts vs render.
    public bool IsHtml;
    public uint HtmlStartColor = 0xFFFFFFFF;
    public bool HtmlBg;

    // A press on a field records (entity, logical screen-x) here via its
    // UiPointerDown observer; RouteMouse converts it to a caret position. Via the
    // observer because the raw mouse button is consumed by the focus interaction.
    public ulong PendingClickEntity;
    public float PendingClickX;
    public float PendingClickY;

    // True only while a left-drag that *began* on this field is in progress. Gates
    // the drag-select branch so holding the button down after a press that started
    // outside the field (anywhere else on screen) doesn't extend the selection.
    public bool MouseSelecting;
    public readonly TextEditState State = new(singleLine: true);
    public readonly StringBuilder Buffer = new();

    // In-app clipboard (Ctrl+C/X/V). Not the OS clipboard — keeps the feature
    // self-contained without an SDL P/Invoke; cross-app paste isn't supported.
    public static string Clipboard = string.Empty;

    public string Display() => Masked ? new string(MaskChar, Buffer.Length) : Buffer.ToString();

    // Width in px of the display text's first `count` chars, in the field font.
    public float WidthUpTo(int count)
    {
        if (count <= 0) return 0;
        var s = Display();
        if (count > s.Length) count = s.Length;
        return UoFontRenderer.MeasureFont(s.Substring(0, count), FontId, int.MaxValue, allowHtml: false).Width;
    }

    // ── ITextEditHandler ──────────────────────────────────────────────
    public int Length => Buffer.Length;
    public char GetChar(int index) => Masked ? MaskChar : Buffer[index];

    public float GetCharWidth(int lineStartIndex, int charIndex)
    {
        var s = Display();
        if (charIndex < 0 || charIndex >= s.Length) return 0;
        if (s[charIndex] == '\n') return 0; // newline has no width
        int start = Math.Clamp(lineStartIndex, 0, charIndex);
        int rel = charIndex - start;
        float a = rel <= 0 ? 0 : UoFontRenderer.MeasureFont(s.Substring(start, rel), FontId, int.MaxValue, allowHtml: false).Width;
        float b = UoFontRenderer.MeasureFont(s.Substring(start, rel + 1), FontId, int.MaxValue, allowHtml: false).Width;
        return b - a;
    }

    public void LayoutRow(out TextEditRow row, int lineStartIndex)
    {
        float lh = LineHeight;
        if (Multiline)
        {
            // Each wrapped/explicit line is one row; stb walks rows by start index
            // for navigation and by accumulated row HEIGHT when mapping a click's y
            // to a line. PLAIN wrap gives NumChars/Start in raw-buffer indices (HTML
            // mode miscounts the '\n'); the HTML wrap (what's drawn) gives the row
            // height — feeding stb the plain height made a click map to a lower line
            // than the one under the cursor (the rendered lines are taller).
            var plain = UoFontRenderer.WrapLines(Display(), FontId, WrapWidth);
            var html = IsHtml ? UoFontRenderer.WrapLines(Display(), FontId, WrapWidth, IsHtml, HtmlStartColor, HtmlBg) : plain;
            for (int i = 0; i < plain.Count; i++)
            {
                var ln = plain[i];
                if (ln.Start != lineStartIndex) continue;
                float lnH = (i < html.Count ? html[i].Height : ln.Height) is var hh && hh > 0 ? hh : lh;
                row = new TextEditRow { X0 = 0, X1 = ln.Width, BaselineYDelta = lnH, YMin = 0, YMax = lnH, NumChars = ln.Count };
                return;
            }
            // Trailing empty line (cursor past the last newline) or empty buffer.
            row = new TextEditRow { X0 = 0, X1 = 0, BaselineYDelta = lh, YMin = 0, YMax = lh, NumChars = Math.Max(0, Buffer.Length - lineStartIndex) };
            return;
        }

        // Single-line: the whole buffer is one row. YMax must be > 0 (and > the
        // click y, which we always pass as 0) or stb's Click can't locate the row
        // — a zero-height row contains no point, so the caret never moves.
        float w = WidthUpTo(Buffer.Length);
        row = new TextEditRow
        {
            X0 = 0,
            X1 = w,
            BaselineYDelta = lh,
            YMin = 0,
            YMax = lh,
            NumChars = Buffer.Length,
        };
    }

    public float LineHeight => UoFontRenderer.MeasureFont("Wg", FontId, int.MaxValue, allowHtml: false).Height is var h && h > 0 ? h : 20;

    // Caret pixel position for a cursor index: (x within its line, y = stacked
    // line tops). Single-line collapses to (WidthUpTo, 0).
    public (float X, float Y) CaretXY(int cursor)
    {
        var s = Display();
        if (!Multiline) return (WidthUpTo(cursor), 0);
        // PLAIN wrap drives index→line + x (raw-buffer char counts). The HTML wrap
        // (what's actually drawn) drives the Y pitch — HTML mode changes per-line
        // MaxHeight, so stacking by the plain height made the caret drift up a few
        // px per line. Same break points, so the two lists line up by index.
        var lines = UoFontRenderer.WrapLines(s, FontId, WrapWidth);
        var rlines = IsHtml ? UoFontRenderer.WrapLines(s, FontId, WrapWidth, IsHtml, HtmlStartColor, HtmlBg) : lines;
        float y = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            var ln = lines[i];
            int end = ln.Start + ln.Count;
            bool last = i == lines.Count - 1;
            // A cursor exactly AT a line end that follows a hard '\n' belongs to
            // the next line's start, not this line's tail — so don't match here
            // (let the loop fall through). Only claim the boundary on the last
            // line, and only when it isn't a trailing newline (that becomes a
            // fresh empty row handled after the loop).
            bool endsWithNewline = end > ln.Start && end <= s.Length && s[end - 1] == '\n';
            if (cursor < end || (last && !endsWithNewline))
            {
                int rel = Math.Clamp(cursor - ln.Start, 0, ln.Count);
                float x = rel <= 0 ? 0 : UoFontRenderer.MeasureFont(s.Substring(ln.Start, rel), FontId, int.MaxValue, allowHtml: false).Width;
                return (x, y);
            }
            float rh = i < rlines.Count ? rlines[i].Height : ln.Height;
            y += rh > 0 ? rh : LineHeight;
        }
        // Cursor past the last rendered line (trailing newline): caret on a fresh
        // empty row below, at column 0.
        return (0, y);
    }

    public bool InsertChars(int index, ReadOnlySpan<char> chars)
    {
        Buffer.Insert(index, chars.ToString());
        return true;
    }

    public void DeleteChars(int index, int count) => Buffer.Remove(index, count);
}

// On a field's glyph: how to find the text's logical screen origin for mouse
// caret/selection. Frame's ComputedNode.Position is in scaled px (logical *
// DpiScale); divide by DpiScale and add OffsetX (the content row's logical
// left) to get the glyph's logical x.
internal struct TextFieldGeom { public ulong Frame; public float OffsetX; }

// Marker on the caret bar overlay of a SpawnTextField field. Target = the glyph
// entity (FocusedInput target). PositionTextEditOverlays shows + positions it
// only while its field is the active editor. OffsetX/OffsetY shift the bar from
// the overlay parent's origin to the rendered text's origin — zero for host
// fields (the overlay's parent row IS the text origin), the padding/centering
// inset for mod fields, where the overlay parents to the padded node itself.
// MaxX/MaxY (parent-local, 0 = unbounded) hide/clip the overlay past the
// field's content right/bottom edge: the overlay is an ABSOLUTE child, and
// Clay's ClipContent doesn't scissor floating elements, so a caret past a
// clipped field's box would float over whatever sits right of (or below) it.
internal struct TextEditCaret { public ulong Target; public float OffsetX, OffsetY, MaxX, MaxY; }

// Marker on the selection-highlight overlay. Target = the glyph entity.
internal struct TextEditSelection { public ulong Target; public float OffsetX, OffsetY, MaxX, MaxY; }

internal readonly struct TextEditPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ActiveTextEdit());

        Action<Res<FocusedInput>, ResMut<ActiveTextEdit>,
            Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>>,
            Query<Data<MaskedText>, Filter<With<EditableText>>>,
            Query<Data<TextFont>, With<EditableText>>,
            Query<Data<UiCustom, MultilineText>, With<EditableText>>> syncFn = SyncActiveEditor;
        Action<Res<MouseContext>, ResMut<ActiveTextEdit>,
            Query<Data<ComputedNode>>, Query<Data<TextFieldGeom>>> mouseFn = RouteMouse;
        Action<Res<KeyboardContext>, ResMut<ActiveTextEdit>> keysFn = RouteKeys;
        Action<EventReader<CharInputEvent>, ResMut<ActiveTextEdit>> charsFn = RouteChars;
        Action<ResMut<ActiveTextEdit>,
            Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>>,
            Query<Data<MaskedText>, Filter<With<EditableText>>>,
            Query<Data<UiCustom>, Filter<With<EditableText>, With<MultilineText>>>> writeBackFn = WriteBack;
        Action<Res<ActiveTextEdit>, Res<Time>,
            Query<Data<Node, TextEditCaret>>,
            Query<Data<Node, TextEditSelection>>,
            Query<Data<UiCustom>, With<MultilineText>>> overlayFn = PositionOverlays;
        Action<Query<Data<Node, UiCustom, TextFont, MultilineText>>> sizeFn = SizeMultilineGlyphs;

        // Declaration order is preserved within a stage (no explicit labels
        // needed). PreUpdate: sync focus, then position overlays. Update: route
        // keys, route chars, write the buffer back to the field's Text.
        app.AddSystem(syncFn).InStage(Stage.PreUpdate).Build();
        app.AddSystem(overlayFn).InStage(Stage.PreUpdate).Build();
        app.AddSystem(mouseFn).InStage(Stage.Update).Build();
        app.AddSystem(keysFn).InStage(Stage.Update).Build();
        app.AddSystem(charsFn).InStage(Stage.Update).Build();
        app.AddSystem(writeBackFn).InStage(Stage.Update).Build();
        // After WriteBack so the wrapped glyph box tracks the just-edited text.
        app.AddSystem(sizeFn).InStage(Stage.Update).Build();
    }

    // Mouse caret placement + drag-select. A press recorded by the field's
    // observer (PendingClick) sets the caret at the clicked glyph; holding +
    // moving extends the selection. localX is in the glyph's logical text space:
    // ComputedNode.Position, the mouse, and the handler's measured widths are
    // all logical px, so the glyph origin is just the frame's x + the content
    // row's left offset.
    private static void RouteMouse(
        Res<MouseContext> mouse,
        ResMut<ActiveTextEdit> edit,
        Query<Data<ComputedNode>> computedQ,
        Query<Data<TextFieldGeom>> geomQ)
    {
        var a = edit.Value;
        if (a.Entity == 0)
        {
            a.PendingClickEntity = 0;
            return;
        }

        // Text origin (top-left of the rendered glyphs) in logical px.
        // Multiline: a multiline field's frame is an absolute, contentless wrapper
        // that gets NO ComputedNode, so the frame path (below) dropped every click
        // — caret never moved, no drag-select. The glyph itself always renders, so
        // it has a ComputedNode whose Position is exactly the text's top-left, and
        // a real Y for mapping the click to the right wrapped line.
        // Single-line: keep the original frame + OffsetX path unchanged.
        float glyphLogicalX, glyphLogicalY;
        if (a.Multiline)
        {
            if (!computedQ.Contains(a.Entity)) { a.PendingClickEntity = 0; return; }
            var (_, glyphCn) = computedQ.Get(a.Entity);
            glyphLogicalX = glyphCn.Ref.Position.X;
            glyphLogicalY = glyphCn.Ref.Position.Y;
        }
        else
        {
            if (!geomQ.Contains(a.Entity)) { a.PendingClickEntity = 0; return; }
            var (_, geom) = geomQ.Get(a.Entity);
            if (!computedQ.Contains(geom.Ref.Frame)) { a.PendingClickEntity = 0; return; }
            var (_, frameCn) = computedQ.Get(geom.Ref.Frame);
            glyphLogicalX = frameCn.Ref.Position.X + geom.Ref.OffsetX;
            glyphLogicalY = 0;
        }

        // stb maps the click via LayoutRow at this y; single-line is one row at
        // y=0, multiline needs the real row (line) the cursor was dropped on.
        float LocalY(float screenY) => a.Multiline ? screenY - glyphLogicalY : 0;

        if (!mouse.Value.IsPressed(MouseButtonType.Left))
            a.MouseSelecting = false;

        if (a.PendingClickEntity == a.Entity)
        {
            a.PendingClickEntity = 0;
            a.MouseSelecting = true;
            TextEdit.Click(a, a.State, a.PendingClickX - glyphLogicalX, LocalY(a.PendingClickY));
        }
        else if (a.MouseSelecting && mouse.Value.IsPressed(MouseButtonType.Left))
        {
            TextEdit.Drag(a, a.State, mouse.Value.Position.X - glyphLogicalX, LocalY(mouse.Value.Position.Y));
        }
    }

    // Keep ActiveTextEdit pointed at the focused EditableText field: (re)load the
    // buffer when focus moves to a new field, resync if the field's text was
    // changed externally (e.g. ChatPlugin clears it on submit), clear when focus
    // leaves all editable fields.
    private static void SyncActiveEditor(
        Res<FocusedInput> focused,
        ResMut<ActiveTextEdit> edit,
        Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>> textQ,
        Query<Data<MaskedText>, Filter<With<EditableText>>> maskedQ,
        Query<Data<TextFont>, With<EditableText>> fontQ,
        Query<Data<UiCustom, MultilineText>, With<EditableText>> multiQ)
    {
        var e = focused.Value.Entity;
        var a = edit.Value;

        bool isMulti = e != 0 && multiQ.Contains(e);
        bool isMasked = e != 0 && !isMulti && maskedQ.Contains(e);
        bool isPlain = e != 0 && !isMulti && !isMasked && textQ.Contains(e);
        if (!isMulti && !isMasked && !isPlain) { a.Entity = 0; return; }

        string current;
        int wrapWidth = a.WrapWidth;
        bool isHtml = false; uint htmlStartColor = 0xFFFFFFFF; bool htmlBg = false;
        if (isMulti)
        {
            var (_, c, m) = multiQ.Get(e);
            var r = c.Ref.Render();
            current = r.Text ?? string.Empty; wrapWidth = m.Ref.WrapWidth;
            isHtml = r.IsHtml; htmlStartColor = r.HtmlStartColor; htmlBg = r.HtmlBg;
        }
        else if (isMasked) { var (_, mt) = maskedQ.Get(e); current = mt.Ref.Value ?? string.Empty; }
        else { var (_, t) = textQ.Get(e); current = t.Ref.Value ?? string.Empty; }

        bool newField = a.Entity != e;
        bool externallyChanged = !newField && !string.Equals(current, a.Buffer.ToString(), StringComparison.Ordinal)
            // only treat as external if it doesn't match what we'd render (masked
            // Text is the mask, not the real value — compare against real)
            && current != (a.Masked ? a.Buffer.ToString() : current);

        if (newField || externallyChanged)
        {
            a.Entity = e;
            a.Masked = isMasked;
            a.Multiline = isMulti;
            a.WrapWidth = wrapWidth;
            a.IsHtml = isHtml;
            a.HtmlStartColor = htmlStartColor;
            a.HtmlBg = htmlBg;
            ushort fid = UoFontRuntime.DefaultFont;
            if (fontQ.Contains(e)) { var (_, f) = fontQ.Get(e); fid = f.Ref.FontId; }
            a.FontId = fid;
            a.Buffer.Clear();
            a.Buffer.Append(current);
            a.State.Initialize(singleLine: !isMulti);
            a.State.Cursor = a.Buffer.Length;
        }
    }

    private static void RouteKeys(Res<KeyboardContext> kb, ResMut<ActiveTextEdit> edit)
    {
        var a = edit.Value;
        if (a.Entity == 0) return;

        bool shift = kb.Value.IsPressed(Keys.LeftShift) || kb.Value.IsPressed(Keys.RightShift);
        bool ctrl = kb.Value.IsPressed(Keys.LeftControl) || kb.Value.IsPressed(Keys.RightControl);

        if (ctrl)
        {
            if (kb.Value.IsPressedOnce(Keys.A)) { TextEdit.Key(a, a.State, TextEditKey.TextStart); TextEdit.Key(a, a.State, TextEditKey.TextEnd, shift: true); }
            if (kb.Value.IsPressedOnce(Keys.C)) { var s = Selected(a); if (s.Length > 0) ActiveTextEdit.Clipboard = s; }
            if (kb.Value.IsPressedOnce(Keys.X)) { var s = Selected(a); if (s.Length > 0) { ActiveTextEdit.Clipboard = s; TextEdit.Cut(a, a.State); } }
            if (kb.Value.IsPressedOnce(Keys.V) && ActiveTextEdit.Clipboard.Length > 0) TextEdit.Paste(a, a.State, ActiveTextEdit.Clipboard.AsSpan());
            if (kb.Value.IsPressedOnce(Keys.Z)) TextEdit.Key(a, a.State, shift ? TextEditKey.Redo : TextEditKey.Undo);
            if (kb.Value.IsPressedOnce(Keys.Left)) TextEdit.Key(a, a.State, TextEditKey.WordLeft, shift);
            if (kb.Value.IsPressedOnce(Keys.Right)) TextEdit.Key(a, a.State, TextEditKey.WordRight, shift);
            return;
        }

        if (kb.Value.IsPressedOnce(Keys.Left)) TextEdit.Key(a, a.State, TextEditKey.Left, shift);
        if (kb.Value.IsPressedOnce(Keys.Right)) TextEdit.Key(a, a.State, TextEditKey.Right, shift);
        if (kb.Value.IsPressedOnce(Keys.Home)) TextEdit.Key(a, a.State, TextEditKey.LineStart, shift);
        if (kb.Value.IsPressedOnce(Keys.End)) TextEdit.Key(a, a.State, TextEditKey.LineEnd, shift);
        if (kb.Value.IsPressedOnce(Keys.Delete)) TextEdit.Key(a, a.State, TextEditKey.Delete);
        // Multiline fields: arrows move between rows, Enter inserts a newline.
        // Enter newline comes from the key (not the char path): FNA also
        // synthesizes Enter as a TextInput '\r' (char 13), but RouteChars drops
        // '\r' so it doesn't double up here.
        if (a.Multiline)
        {
            if (kb.Value.IsPressedOnce(Keys.Up)) TextEdit.Key(a, a.State, TextEditKey.Up, shift);
            if (kb.Value.IsPressedOnce(Keys.Down)) TextEdit.Key(a, a.State, TextEditKey.Down, shift);
            if (kb.Value.IsPressedOnce(Keys.Enter)) TextEdit.InputChar(a, a.State, '\n');
        }
        // Backspace is handled in RouteChars (this client delivers it as the
        // '\b' TextInput char); handling Keys.Back here too would double-delete.
    }

    private static void RouteChars(EventReader<CharInputEvent> reader, ResMut<ActiveTextEdit> edit)
    {
        var a = edit.Value;
        if (a.Entity == 0) return;

        foreach (var ev in reader.Read())
        {
            var ch = ev.Value;
            if (ch == '\b') { TextEdit.Key(a, a.State, TextEditKey.Backspace); continue; }
            // Multiline fields accept a newline (typed paste or Enter delivered as
            // a char); single-line drops it. Other control chars aren't edits.
            // Drop '\r' entirely: FNA delivers Enter as a '\r' TextInput char,
            // but RouteKeys already inserts the newline from Keys.Enter — handling
            // it here too would double up (and pasted Windows "\r\n" would too).
            // A real '\n' (e.g. pasted unix text) still inserts once.
            if (ch == '\r') continue;
            if (ch == '\n') { if (a.Multiline) TextEdit.InputChar(a, a.State, '\n'); continue; }
            // FNA synthesizes Home/End/Tab (2/3/9) and Delete (127) as TextInput
            // control chars; RouteKeys owns those, so drop them here.
            if (ch < ' ' || ch == (char)127) continue;
            TextEdit.InputChar(a, a.State, ch);
        }
    }

    // Push the edited buffer back into the field's Text (plain) or MaskedText
    // (SyncMaskedText then mirrors the mask chars into Text for the renderer).
    private static void WriteBack(
        ResMut<ActiveTextEdit> edit,
        Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>> textQ,
        Query<Data<MaskedText>, Filter<With<EditableText>>> maskedQ,
        Query<Data<UiCustom>, Filter<With<EditableText>, With<MultilineText>>> multiQ)
    {
        var a = edit.Value;
        if (a.Entity == 0) return;

        var value = a.Buffer.ToString();
        if (a.Multiline)
        {
            if (multiQ.Contains(a.Entity))
            {
                var (_, c) = multiQ.Get(a.Entity);
                if (!string.Equals(c.Ref.Render().Text, value, StringComparison.Ordinal))
                    c.Ref.Render().Text = value;
            }
        }
        else if (a.Masked)
        {
            if (maskedQ.Contains(a.Entity))
            {
                var (_, mt) = maskedQ.Get(a.Entity);
                if (!string.Equals(mt.Ref.Value, value, StringComparison.Ordinal))
                    mt.Ref.Value = value;
            }
        }
        else if (textQ.Contains(a.Entity))
        {
            var (_, t) = textQ.Get(a.Entity);
            if (!string.Equals(t.Ref.Value, value, StringComparison.Ordinal))
                t.Ref.Value = value;
        }
    }

    private static void PositionOverlays(
        Res<ActiveTextEdit> edit,
        Res<Time> time,
        Query<Data<Node, TextEditCaret>> carets,
        Query<Data<Node, TextEditSelection>> selections,
        Query<Data<UiCustom>, With<MultilineText>> multiGlyphs)
    {
        var a = edit.Value;
        bool on = (int)(time.Value.Total / 530f) % 2 == 0;
        int s0 = Math.Min(a.State.SelectStart, a.State.SelectEnd);
        int s1 = Math.Max(a.State.SelectStart, a.State.SelectEnd);

        foreach (var (_, node, caret) in carets)
        {
            // Multiline draws its caret in the WrappedText renderer (in-content, so
            // it scrolls/clips with the text); the overlay node serves single-line.
            if (caret.Ref.Target != a.Entity || a.Entity == 0 || a.Multiline)
            {
                node.Ref.Display = Display.None;
                continue;
            }
            var (cx, cy) = a.CaretXY(a.State.Cursor);
            float caretX = cx + caret.Ref.OffsetX;
            if (caret.Ref.MaxX > 0 && caretX > caret.Ref.MaxX)
            {
                node.Ref.Display = Display.None;
                continue;
            }
            // Size the bar to the glyph ink (cap-top to descender), not the full
            // font cell / font.Size — the cell's top leading made the caret overshoot
            // above the text. Same metric the multiline (renderer) caret uses.
            var (caretTop, caretH) = UoFontRenderer.CaretMetrics(a.FontId);
            float caretY = cy + caretTop + caret.Ref.OffsetY;
            if (caret.Ref.MaxY > 0)
            {
                if (caretY >= caret.Ref.MaxY)
                {
                    node.Ref.Display = Display.None;
                    continue;
                }
                caretH = MathF.Min(caretH, caret.Ref.MaxY - caretY);
            }
            node.Ref.Display = on ? Display.Flex : Display.None;
            node.Ref.Left = Val.Px(caretX);
            node.Ref.Top = Val.Px(caretY);
            node.Ref.Height = Val.Px(caretH);
        }

        foreach (var (_, node, sel) in selections)
        {
            // Multiline draws its selection in the WrappedText renderer (per-line
            // rects, set below) — the single overlay node only serves single-line
            // / masked fields. Hide it for multiline.
            if (sel.Ref.Target != a.Entity || a.Entity == 0 || s0 == s1 || a.Multiline)
            {
                node.Ref.Display = Display.None;
                continue;
            }
            var (x0, y0) = a.CaretXY(s0);
            var (x1, _) = a.CaretXY(s1);
            float selLeft = x0 + sel.Ref.OffsetX;
            float selRight = x1 + sel.Ref.OffsetX;
            if (sel.Ref.MaxX > 0)
            {
                selRight = MathF.Min(selRight, sel.Ref.MaxX);
                if (selLeft >= selRight)
                {
                    node.Ref.Display = Display.None;
                    continue;
                }
            }
            float selTop = y0 + sel.Ref.OffsetY;
            if (sel.Ref.MaxY > 0)
            {
                if (selTop >= sel.Ref.MaxY)
                {
                    node.Ref.Display = Display.None;
                    continue;
                }
                node.Ref.Height = Val.Px(MathF.Min(a.LineHeight, sel.Ref.MaxY - selTop));
            }
            node.Ref.Display = Display.Flex;
            node.Ref.Left = Val.Px(selLeft);
            node.Ref.Top = Val.Px(selTop);
            node.Ref.Width = Val.Px(MathF.Max(1, selRight - selLeft));
        }

        // Multiline: stamp the selection range + caret onto the focused field's
        // WrappedText custom so GuiRenderingPlugin paints them in-content (scroll/
        // clip safe); clear on every other multiline field.
        foreach (var (ent, custom) in multiGlyphs)
        {
            var render = custom.Ref.Render();
            if (render == null) continue;
            bool focused = ent.Ref == a.Entity && a.Multiline;
            bool selActive = focused && s0 != s1;
            render.SelStart = selActive ? s0 : -1;
            render.SelEnd = selActive ? s1 : -1;
            if (focused)
            {
                var (cx, cy) = a.CaretXY(a.State.Cursor);
                // Size the caret to the font's glyph ink (cap-top to descender),
                // not the full line cell — the line cell has top leading that made
                // the bar overshoot above the text.
                var (caretTop, caretH) = UoFontRenderer.CaretMetrics(a.FontId);
                render.CaretOn = on;
                render.CaretX = cx;
                render.CaretY = cy + caretTop;
                render.CaretH = caretH;
            }
            else
            {
                render.CaretOn = false;
            }
        }
    }

    // Size each multiline field's wrapped glyph box to its content so it grows as
    // lines are added (caret Top maps within it) and the parchment/footer below
    // it stays correct. Runs for all multiline fields, not just the focused one.
    private static void SizeMultilineGlyphs(Query<Data<Node, UiCustom, TextFont, MultilineText>> q)
    {
        foreach (var (_, node, custom, font, multi) in q)
        {
            var render = custom.Ref.Render();
            var text = render.Text ?? string.Empty;
            int total = 0;
            foreach (var ln in UoFontRenderer.WrapLines(text, font.Ref.FontId, multi.Ref.WrapWidth, render.IsHtml, render.HtmlStartColor, render.HtmlBg))
                total += ln.Height > 0 ? ln.Height : font.Ref.Size;
            node.Ref.Height = Val.Px(Math.Max(total, font.Ref.Size));
            node.Ref.Width = Val.Px(multi.Ref.WrapWidth);
        }
    }

    private static string Selected(ActiveTextEdit a)
    {
        int s0 = Math.Min(a.State.SelectStart, a.State.SelectEnd);
        int s1 = Math.Max(a.State.SelectStart, a.State.SelectEnd);
        if (s0 == s1) return string.Empty;
        return a.Buffer.ToString(s0, s1 - s0);
    }
}
