using ClassicUO.Ecs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
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

    private static Query<Data<TextInput>> TextInputs(App app)
    {
        var q = new Query<Data<TextInput>>();
        q.Initialize(app); q.Fetch(app);
        return q;
    }

    private static Query<Data<Interaction>, Filter<Without<UiMovable>>> Interactives(App app)
    {
        var q = new Query<Data<Interaction>, Filter<Without<UiMovable>>>();
        q.Initialize(app); q.Fetch(app);
        return q;
    }

    private static Query<Data<TinyEcs.Children>> Children(App app)
    {
        var q = new Query<Data<TinyEcs.Children>>();
        q.Initialize(app); q.Fetch(app);
        return q;
    }

    private static bool IsTextInput(App app, ulong entity)
        => GameCursorPlugin.IsTextInput(entity, TextInputs(app), Interactives(app), Children(app));

    // Mirrors an Options "hue" row: an interactive row (Interaction for the hover
    // tint) that CONTAINS a text field — the field box carries TextInput; the
    // glyph below it does too; a plain label sits beside the field. The I-beam
    // must light over the field box / glyph, but NOT over the whole row.
    private static (App app, ulong row, ulong label, ulong frame, ulong content, ulong glyph) HueRow()
    {
        var app = new App();
        var w = app.GetWorld();

        var row = w.Entity().Set(Interaction.None).ID;                          // interactive (hover tint), no TextInput
        var label = w.Entity().Set(new Text("Poison hue")).ID;                 // plain child, not interactive
        var frame = w.Entity().Set(Interaction.None).Set(new TextInput()).ID;  // the field box
        var content = w.Entity().Set(Interaction.None).ID;                     // field content row, no TextInput
        var glyph = w.Entity().Set(new TextInput()).ID;                        // focusable glyph, not interactive

        w.AddChild(row, label);
        w.AddChild(row, frame);
        w.AddChild(frame, content);
        w.AddChild(content, glyph);
        return (app, row, label, frame, content, glyph);
    }

    [Fact]
    public void IsTextInput_true_over_the_field_box()
    {
        var (app, _, _, frame, _, _) = HueRow();
        Assert.True(IsTextInput(app, frame));
    }

    [Fact]
    public void IsTextInput_true_over_the_glyph_and_its_content_row()
    {
        var (app, _, _, _, content, glyph) = HueRow();
        Assert.True(IsTextInput(app, glyph));
        Assert.True(IsTextInput(app, content)); // descends into the non-interactive glyph
    }

    // The bug: hovering the row's label area lit the I-beam over the whole row
    // because the down-walk crossed into the contained field. An interactive
    // ancestor must NOT inherit a nested field's I-beam.
    [Fact]
    public void IsTextInput_false_over_interactive_row_that_only_contains_a_field()
    {
        var (app, row, label, _, _, _) = HueRow();
        Assert.False(IsTextInput(app, row));
        Assert.False(IsTextInput(app, label));
    }
}
