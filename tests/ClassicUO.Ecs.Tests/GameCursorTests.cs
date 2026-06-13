using ClassicUO.Ecs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// The pure cursor-graphic selection pulled out of RenderGameCursor: the
// AssignGraphicByState priority chain and the war/peace art-row lookup. No
// render context — they take the resolved state booleans and return the slot
// index / art id, so the legacy precedence is locked here.
public class GameCursorTests
{
    [Fact]
    public void PickCursorIndex_targeting_beats_everything()
        => Assert.Equal(12, GameCursorPlugin.PickCursorIndex(targeting: true, dragging: true, textInput: true, overWorld: true, worldDirectionIndex: 5));

    [Fact]
    public void PickCursorIndex_dragging_beats_text_and_world()
        => Assert.Equal(8, GameCursorPlugin.PickCursorIndex(targeting: false, dragging: true, textInput: true, overWorld: true, worldDirectionIndex: 5));

    [Fact]
    public void PickCursorIndex_text_input_beats_world()
        => Assert.Equal(14, GameCursorPlugin.PickCursorIndex(targeting: false, dragging: false, textInput: true, overWorld: true, worldDirectionIndex: 5));

    [Fact]
    public void PickCursorIndex_over_world_returns_direction_slot()
        => Assert.Equal(5, GameCursorPlugin.PickCursorIndex(targeting: false, dragging: false, textInput: false, overWorld: true, worldDirectionIndex: 5));

    [Fact]
    public void PickCursorIndex_falls_back_to_neutral_hand()
        => Assert.Equal(9, GameCursorPlugin.PickCursorIndex(targeting: false, dragging: false, textInput: false, overWorld: false, worldDirectionIndex: 5));

    [Fact]
    public void CursorGraphic_picks_peace_row()
    {
        Assert.Equal((ushort)0x206A, GameCursorPlugin.CursorGraphic(warMode: false, index: 0));
        Assert.Equal((ushort)0x2073, GameCursorPlugin.CursorGraphic(warMode: false, index: 9));
    }

    [Fact]
    public void CursorGraphic_picks_war_row()
    {
        Assert.Equal((ushort)0x2053, GameCursorPlugin.CursorGraphic(warMode: true, index: 0));
        Assert.Equal((ushort)0x205F, GameCursorPlugin.CursorGraphic(warMode: true, index: 12));
    }
}
