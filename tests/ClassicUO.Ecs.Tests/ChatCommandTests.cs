using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// The client-side chat command dispatcher: a chat line beginning with '-' is
// intercepted (ChatPlugin) and emitted as a ChatCommandEvent; ChatCommandPlugin
// routes it. Pure host ECS — no wasm. Drives the trigger directly and asserts
// `-mods` toggles the panel + renders a row (toggle + reload) per loaded mod.
//
// The mod LIFECYCLE itself (enable/disable/reload disposing/re-instantiating the
// wasm) is only exercisable against a real mod instance and is verified at runtime,
// not here — ModControl.Enable/Disable/Reload just enqueue from the gump's side.
public class ChatCommandTests
{
    private static int Count<T>(World world) where T : struct
    {
        var n = 0;
        foreach (var _ in world.Query<Data<T>>())
            n++;
        return n;
    }

    private static App BuildApp(ModControl control)
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(control);              // normally the modding runtime
        app.AddResource(new UiZCounter());     // normally a UI plugin
        app.AddPlugin<ChatCommandPlugin>();
        return app;
    }

    [Fact]
    public void Mods_command_toggles_the_panel_open_then_closed()
    {
        var app = BuildApp(new ModControl());

        // Emit `-mods` on frame 0 and again on frame 2, the way ChatPlugin does.
        app.AddSystem((Commands c, Local<int> frame) =>
        {
            if (frame.Value == 0 || frame.Value == 2)
                c.EmitTrigger(new ChatCommandEvent { Name = "mods", Args = "" });
            frame.Value++;
        }).InStage(Stage.First).Build();

        app.RunStartup();
        var world = app.GetWorld();

        app.Update(); // frame 0: opens
        Assert.Equal(1, Count<ModsPanelWindow>(world));

        app.Update(); // frame 1: stays open
        Assert.Equal(1, Count<ModsPanelWindow>(world));

        app.Update(); // frame 2: toggles closed (root despawn cascades)
        Assert.Equal(0, Count<ModsPanelWindow>(world));
    }

    [Fact]
    public void Panel_renders_a_toggle_and_label_per_loaded_mod()
    {
        var control = new ModControl();
        control.Mods.Add(new ModInfo { Name = "topbar", Version = "0.1.0", Enabled = true });
        control.Mods.Add(new ModInfo { Name = "ui", Version = "0.1.0", Enabled = false });
        var app = BuildApp(control);

        app.AddSystem((Commands c, Local<int> frame) =>
        {
            if (frame.Value++ == 0)
                c.EmitTrigger(new ChatCommandEvent { Name = "mods", Args = "" });
        }).InStage(Stage.First).Build();

        app.RunStartup();
        var world = app.GetWorld();

        app.Update();
        Assert.Equal(1, Count<ModsPanelWindow>(world));
        Assert.Equal(2, Count<ModToggleControl>(world)); // one enable/disable toggle per mod
        Assert.Equal(2, Count<ModRowLabel>(world));      // one name/version label per mod
    }

    [Fact]
    public void Unknown_command_does_nothing()
    {
        var app = BuildApp(new ModControl());

        app.AddSystem((Commands c, Local<int> frame) =>
        {
            if (frame.Value++ == 0)
                c.EmitTrigger(new ChatCommandEvent { Name = "bogus", Args = "" });
        }).InStage(Stage.First).Build();

        app.RunStartup();
        var world = app.GetWorld();

        app.Update();
        Assert.Equal(0, Count<ModsPanelWindow>(world));
    }
}
