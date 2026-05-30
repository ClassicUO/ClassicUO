using ClassicUO.Ecs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Keyboard text-entry composition (text-input widgets, chat box). Pure string
// transform per typed char. The clipboard branch (v == 22) needs SDL and is
// intentionally not exercised here.
public class TextComposerTests
{
    [Fact]
    public void Appends_a_normal_char()
        => Assert.Equal("ab", TextComposer.Compose("a", 'b'));

    [Fact]
    public void Backspace_removes_last_char()
        => Assert.Equal("ab", TextComposer.Compose("abc", '\b'));

    [Fact]
    public void Backspace_on_empty_is_noop()
        => Assert.Equal("", TextComposer.Compose("", '\b'));

    [Fact]
    public void Tab_is_swallowed_not_inserted()
        => Assert.Equal("hi", TextComposer.Compose("hi", '\t'));

    [Fact]
    public void Carriage_return_is_normalized_to_newline()
        => Assert.Equal("hi\n", TextComposer.Compose("hi", '\r'));

    [Fact]
    public void Newline_is_appended_as_is()
        => Assert.Equal("hi\n", TextComposer.Compose("hi", '\n'));
}
