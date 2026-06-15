using ClassicUO.Ecs;
using Xunit;
using Action = ClassicUO.Ecs.TextEditPlugin.TextCharAction;

namespace ClassicUO.Ecs.Tests;

// Unit tests for the TextInput char classifier — the control-char rules in
// RouteChars that decide insert vs backspace vs newline vs drop. This is the
// parity-sensitive bit (\r/\n/Tab/Delete handling) that has been a repeat
// defect source; the rest of the editor delegates to the shared TextEdit lib.
public class TextEditTests
{
    [Fact]
    public void Backspace_char_is_a_backspace()
        => Assert.Equal(Action.Backspace, TextEditPlugin.ClassifyChar('\b', multiline: false));

    [Fact]
    public void Carriage_return_is_always_dropped()
    {
        Assert.Equal(Action.Drop, TextEditPlugin.ClassifyChar('\r', multiline: false));
        Assert.Equal(Action.Drop, TextEditPlugin.ClassifyChar('\r', multiline: true));
    }

    [Fact]
    public void Newline_inserts_only_in_multiline_fields()
    {
        Assert.Equal(Action.Drop, TextEditPlugin.ClassifyChar('\n', multiline: false));
        Assert.Equal(Action.Newline, TextEditPlugin.ClassifyChar('\n', multiline: true));
    }

    [Theory]
    [InlineData((char)2)]    // FNA Home
    [InlineData((char)3)]    // FNA End
    [InlineData((char)9)]    // Tab
    [InlineData((char)127)]  // Delete
    public void Synthesized_control_chars_are_dropped(char ch)
        => Assert.Equal(Action.Drop, TextEditPlugin.ClassifyChar(ch, multiline: false));

    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('5')]
    [InlineData(' ')]
    public void Printable_chars_insert(char ch)
        => Assert.Equal(Action.Insert, TextEditPlugin.ClassifyChar(ch, multiline: false));

    // ── Double-click word selection (SelectWordAt) ──────────────────────

    private static (int Start, int End) SelectWordAt(string text, int cursor)
    {
        var a = new ActiveTextEdit();
        a.Buffer.Append(text);
        a.State.Cursor = cursor;
        TextEditPlugin.SelectWordAt(a);
        return (a.State.SelectStart, a.State.SelectEnd);
    }

    [Fact]
    public void Double_click_inside_word_selects_whole_word()
    {
        // "foo bar baz", caret index 5 = inside "bar"
        Assert.Equal((4, 7), SelectWordAt("foo bar baz", 5));
    }

    [Fact]
    public void Double_click_at_word_start_selects_word()
        => Assert.Equal((4, 7), SelectWordAt("foo bar baz", 4));

    [Fact]
    public void Caret_past_word_on_following_space_anchors_left()
    {
        // Click landed between "bar" and the space (caret index 7) -> select "bar".
        Assert.Equal((4, 7), SelectWordAt("foo bar baz", 7));
    }

    [Fact]
    public void Caret_at_buffer_end_selects_last_word()
        => Assert.Equal((8, 11), SelectWordAt("foo bar baz", 11));

    [Fact]
    public void Double_click_on_whitespace_selects_nothing()
    {
        // Caret index 4 lands between two spaces — whitespace on both sides, so
        // there is no adjacent word to anchor on.
        var (s, e) = SelectWordAt("foo  bar", 4);
        Assert.Equal(s, e); // empty selection
    }

    [Fact]
    public void Empty_buffer_selects_nothing()
    {
        var (s, e) = SelectWordAt("", 0);
        Assert.Equal(s, e);
    }
}
