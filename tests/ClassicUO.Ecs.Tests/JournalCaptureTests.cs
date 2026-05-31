using ClassicUO.Ecs;
using ClassicUO.Game.Data;
using TinyEcs.Bevy;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Behavioural tests for the journal capture path, run against a REAL App +
// schedule (per repo rule: real ECS in tests). The capture system reads the
// TextOverheadEvent stream and appends the journalled subset to JournalStore
// with a classified TextType — no assets/GPU needed, so a headless App suffices.
public class JournalCaptureTests
{
    private static App MakeApp()
    {
        var app = new App();
        app.AddState(GameState.Loading);
        app.AddResource(new Time());
        app.AddResource(new AssetsServer()); // empty: LayoutWindow has no windows to size
        app.AddResource<MouseContext>(new TestMouseContext()); // ResizeDrag reads mouse
        app.AddResource(new DragGate());
        app.AddPlugin<JournalPlugin>();
        // Deterministic timestamp for assertions.
        app.GetResource<JournalTimeProvider>().Source = static () => "12:34";
        return app;
    }

    private static TextOverheadEvent Msg(string text, MessageType type, ushort hue = 0, string name = "", uint serial = 1)
        => new() { Text = text, Name = name, Hue = hue, MessageType = type, Serial = serial };

    [Fact]
    public void Capture_journals_speech_and_system_but_skips_command_types()
    {
        var app = MakeApp();
        var store = app.GetResource<JournalStore>();

        app.SendEvent(Msg("hello", MessageType.Regular));
        app.SendEvent(Msg("a spell", MessageType.Spell));
        app.SendEvent(Msg("world saving", MessageType.System, serial: 0xFFFFFFFF, name: "System"));
        app.SendEvent(Msg("encoded blob", MessageType.Encoded)); // dropped
        app.SendEvent(Msg("a command", MessageType.Command));    // dropped
        app.Update();

        Assert.Equal(3, store.Entries.Count);
        Assert.Equal("hello", store.Entries[0].Text);
        Assert.Equal("a spell", store.Entries[1].Text);
        Assert.Equal("world saving", store.Entries[2].Text);
    }

    [Fact]
    public void Capture_classifies_text_type_from_message_and_serial()
    {
        var app = MakeApp();
        var store = app.GetResource<JournalStore>();

        app.SendEvent(Msg("npc speaks", MessageType.Regular, serial: 0x4001));      // Object
        app.SendEvent(Msg("broadcast", MessageType.Regular, serial: 0xFFFFFFFF));   // System
        app.SendEvent(Msg("guildie", MessageType.Guild, serial: 0x4002));           // GuildAlly
        app.Update();

        Assert.Equal(JournalTextType.Object, store.Entries[0].TextType);
        Assert.Equal(JournalTextType.System, store.Entries[1].TextType);
        Assert.Equal(JournalTextType.GuildAlly, store.Entries[2].TextType);
    }

    [Fact]
    public void Capture_stamps_name_hue_and_timestamp()
    {
        var app = MakeApp();
        var store = app.GetResource<JournalStore>();

        app.SendEvent(Msg("greetings", MessageType.Regular, hue: 42, name: "Guard"));
        app.Update();

        Assert.Single(store.Entries);
        Assert.Equal("Guard", store.Entries[0].Name);
        Assert.Equal(42, store.Entries[0].Hue);
        Assert.Equal("12:34", store.Entries[0].TimeLabel);
    }

    [Fact]
    public void Store_revision_advances_and_caps_oldest_out()
    {
        var store = new JournalStore();
        var r0 = store.Revision;
        store.Append(new JournalEntry { Text = "one" });
        Assert.True(store.Revision > r0);

        for (var i = 0; i < JournalStore.MaxEntries + 50; i++)
            store.Append(new JournalEntry { Text = $"line{i}" });

        Assert.Equal(JournalStore.MaxEntries, store.Entries.Count);
        Assert.Equal($"line{JournalStore.MaxEntries + 49}", store.Entries[^1].Text);
    }
}
