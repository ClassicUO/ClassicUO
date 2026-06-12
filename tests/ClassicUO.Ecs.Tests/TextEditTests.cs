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
}
