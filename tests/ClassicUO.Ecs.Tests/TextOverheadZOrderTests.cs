using ClassicUO.Ecs;
using ClassicUO.Game.Data;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Z-order contract of the overhead text manager (legacy WorldTextManager
// parity): Append moves the speaker's stack topmost; MoveToTop (hover) lifts
// a stack and the lift persists.
public class TextOverheadZOrderTests
{
    private static TextOverheadEvent Msg(uint serial, string text = "x") => new()
    {
        Serial = serial,
        Text = text,
        Hue = 0x3B2,
        IsUnicode = true,
        MessageType = MessageType.Regular,
        Time = float.MaxValue,
    };

    [Fact]
    public void Append_NewSpeakerGoesTopmost()
    {
        var mgr = new TextOverHeadManager();
        mgr.Append(Msg(1));
        mgr.Append(Msg(2));
        Assert.Equal(new uint[] { 1, 2 }, mgr.ZOrderSerials());
    }

    [Fact]
    public void Append_ExistingSpeakerLiftsItsStack()
    {
        var mgr = new TextOverHeadManager();
        mgr.Append(Msg(1));
        mgr.Append(Msg(2));
        mgr.Append(Msg(1, "again"));
        Assert.Equal(new uint[] { 2, 1 }, mgr.ZOrderSerials());
    }

    [Fact]
    public void MoveToTop_LiftsHoveredStack_AndPersists()
    {
        var mgr = new TextOverHeadManager();
        mgr.Append(Msg(1));
        mgr.Append(Msg(2));
        mgr.Append(Msg(3));

        mgr.MoveToTop(1);
        Assert.Equal(new uint[] { 2, 3, 1 }, mgr.ZOrderSerials());

        // Persistent: nothing reorders it back.
        Assert.Equal(new uint[] { 2, 3, 1 }, mgr.ZOrderSerials());
    }

    [Fact]
    public void MoveToTop_UnknownSerial_NoOp()
    {
        var mgr = new TextOverHeadManager();
        mgr.Append(Msg(1));
        mgr.MoveToTop(99);
        Assert.Equal(new uint[] { 1 }, mgr.ZOrderSerials());
    }
}
