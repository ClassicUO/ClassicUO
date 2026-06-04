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

    // A press on a field records (entity, logical screen-x) here via its
    // UiPointerDown observer; RouteMouse converts it to a caret position. Via the
    // observer because the raw mouse button is consumed by the focus interaction.
    public ulong PendingClickEntity;
    public float PendingClickX;
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
        => WidthUpTo(charIndex + 1) - WidthUpTo(charIndex);

    public void LayoutRow(out TextEditRow row, int lineStartIndex)
    {
        // Single-line: the whole buffer is one row. YMax must be > 0 (and > the
        // click y, which we always pass as 0) or stb's Click can't locate the row
        // — a zero-height row contains no point, so the caret never moves.
        float w = WidthUpTo(Buffer.Length);
        row = new TextEditRow
        {
            X0 = 0,
            X1 = w,
            BaselineYDelta = LineHeight,
            YMin = 0,
            YMax = LineHeight,
            NumChars = Buffer.Length,
        };
    }

    private float LineHeight => UoFontRenderer.MeasureFont("Wg", FontId, int.MaxValue, allowHtml: false).Height is var h && h > 0 ? h : 20;

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
// only while its field is the active editor.
internal struct TextEditCaret { public ulong Target; }

// Marker on the selection-highlight overlay. Target = the glyph entity.
internal struct TextEditSelection { public ulong Target; }

internal readonly struct TextEditPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ActiveTextEdit());

        Action<Res<FocusedInput>, ResMut<ActiveTextEdit>,
            Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>>>,
            Query<Data<MaskedText>, Filter<With<EditableText>>>,
            Query<Data<TextFont>, With<EditableText>>> syncFn = SyncActiveEditor;
        Action<Res<MouseContext>, ResMut<ActiveTextEdit>,
            Query<Data<ComputedNode>>, Query<Data<TextFieldGeom>>> mouseFn = RouteMouse;
        Action<Res<KeyboardContext>, ResMut<ActiveTextEdit>> keysFn = RouteKeys;
        Action<EventReader<CharInputEvent>, ResMut<ActiveTextEdit>> charsFn = RouteChars;
        Action<ResMut<ActiveTextEdit>,
            Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>>>,
            Query<Data<MaskedText>, Filter<With<EditableText>>>> writeBackFn = WriteBack;
        Action<Res<ActiveTextEdit>, Res<Time>,
            Query<Data<Node, TextEditCaret>>,
            Query<Data<Node, TextEditSelection>>> overlayFn = PositionOverlays;

        // Declaration order is preserved within a stage (no explicit labels
        // needed). PreUpdate: sync focus, then position overlays. Update: route
        // keys, route chars, write the buffer back to the field's Text.
        app.AddSystem(syncFn).InStage(Stage.PreUpdate).Build();
        app.AddSystem(overlayFn).InStage(Stage.PreUpdate).Build();
        app.AddSystem(mouseFn).InStage(Stage.Update).Build();
        app.AddSystem(keysFn).InStage(Stage.Update).Build();
        app.AddSystem(charsFn).InStage(Stage.Update).Build();
        app.AddSystem(writeBackFn).InStage(Stage.Update).Build();
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
        if (a.Entity == 0 || !geomQ.Contains(a.Entity))
        {
            a.PendingClickEntity = 0;
            return;
        }

        var (_, geom) = geomQ.Get(a.Entity);
        if (!computedQ.Contains(geom.Ref.Frame))
        {
            a.PendingClickEntity = 0;
            return;
        }

        var (_, frameCn) = computedQ.Get(geom.Ref.Frame);
        float glyphLogicalX = frameCn.Ref.Position.X + geom.Ref.OffsetX;

        if (a.PendingClickEntity == a.Entity)
        {
            a.PendingClickEntity = 0;
            TextEdit.Click(a, a.State, a.PendingClickX - glyphLogicalX, 0);
        }
        else if (mouse.Value.IsPressed(MouseButtonType.Left))
        {
            TextEdit.Drag(a, a.State, mouse.Value.Position.X - glyphLogicalX, 0);
        }
    }

    // Keep ActiveTextEdit pointed at the focused EditableText field: (re)load the
    // buffer when focus moves to a new field, resync if the field's text was
    // changed externally (e.g. ChatPlugin clears it on submit), clear when focus
    // leaves all editable fields.
    private static void SyncActiveEditor(
        Res<FocusedInput> focused,
        ResMut<ActiveTextEdit> edit,
        Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>>> textQ,
        Query<Data<MaskedText>, Filter<With<EditableText>>> maskedQ,
        Query<Data<TextFont>, With<EditableText>> fontQ)
    {
        var e = focused.Value.Entity;
        var a = edit.Value;

        bool isMasked = e != 0 && maskedQ.Contains(e);
        bool isPlain = e != 0 && !isMasked && textQ.Contains(e);
        if (!isMasked && !isPlain) { a.Entity = 0; return; }

        string current;
        if (isMasked) { var (_, mt) = maskedQ.Get(e); current = mt.Ref.Value ?? string.Empty; }
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
            ushort fid = UoFontRuntime.DefaultFont;
            if (fontQ.Contains(e)) { var (_, f) = fontQ.Get(e); fid = f.Ref.FontId; }
            a.FontId = fid;
            a.Buffer.Clear();
            a.Buffer.Append(current);
            a.State.Initialize(singleLine: true);
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
            // Other control chars (enter/tab/newline) aren't text edits.
            if (ch < ' ') continue;
            TextEdit.InputChar(a, a.State, ch);
        }
    }

    // Push the edited buffer back into the field's Text (plain) or MaskedText
    // (SyncMaskedText then mirrors the mask chars into Text for the renderer).
    private static void WriteBack(
        ResMut<ActiveTextEdit> edit,
        Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>>> textQ,
        Query<Data<MaskedText>, Filter<With<EditableText>>> maskedQ)
    {
        var a = edit.Value;
        if (a.Entity == 0) return;

        var value = a.Buffer.ToString();
        if (a.Masked)
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
        Query<Data<Node, TextEditSelection>> selections)
    {
        var a = edit.Value;
        bool on = (int)(time.Value.Total / 530f) % 2 == 0;

        foreach (var (_, node, caret) in carets)
        {
            if (caret.Ref.Target != a.Entity || a.Entity == 0)
            {
                node.Ref.Display = Display.None;
                continue;
            }
            node.Ref.Display = on ? Display.Flex : Display.None;
            node.Ref.Left = Val.Px(a.WidthUpTo(a.State.Cursor));
        }

        foreach (var (_, node, sel) in selections)
        {
            int s0 = Math.Min(a.State.SelectStart, a.State.SelectEnd);
            int s1 = Math.Max(a.State.SelectStart, a.State.SelectEnd);
            if (sel.Ref.Target != a.Entity || a.Entity == 0 || s0 == s1)
            {
                node.Ref.Display = Display.None;
                continue;
            }
            float x0 = a.WidthUpTo(s0);
            float x1 = a.WidthUpTo(s1);
            node.Ref.Display = Display.Flex;
            node.Ref.Left = Val.Px(x0);
            node.Ref.Width = Val.Px(MathF.Max(1, x1 - x0));
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
