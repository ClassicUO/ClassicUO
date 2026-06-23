using System;
using System.IO;
using ClassicUO.Ecs.Modding;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// End-to-end tests for the NEW component-model modding API (tinyecs:modding WIT),
// driving real Rust guest components through the real ModdingPlugin +
// wasmtime fork. Real bridge, real wasmtime, real TinyEcs World — no mocks.
//
// Methods in one class run serially, so sharing process cwd is safe.
public class EcsModdingRoundTripTests
{
    // Fresh isolated mod folder holding ONE mod as `<folder>/<mod>/{mod.wasm,
    // mod.json}` (the layout the plugin scans), cwd pointed at it, and a bare App
    // with the plugin. The folder name is deliberately NOT "ecs-mods": the real
    // mods deploy to `<exe>/ecs-mods/` (transitively, via the cuo project ref) and
    // the loader scans both the exe dir and cwd — a unique folder name keeps the
    // exe-dir copy out of view so each test sees exactly its one mod (e.g. so the
    // 14MB ui mod doesn't load in every test). Injected via ModdingConfig.ModFolder.
    private const string IsolatedModFolder = "iso-mods";

    // Resolve <repo>/ecs-mods/<modName>/mod.wasm (built by `make build-mods`), copy
    // it into a fresh isolated folder so only this one mod loads, point cwd there,
    // and return a bare App with the plugin (ModFolder injected for isolation).
    private static App BuildAppWith(string modName)
    {
        var srcWasm = Path.Combine(RepoRoot(), "ecs-mods", modName, "mod.wasm");
        Assert.True(File.Exists(srcWasm),
            $"mod not built: {srcWasm} — run `make build-mods` (or `make test`)");

        var root = Path.Combine(Path.GetTempPath(), "cuo-ecsmod-" + modName);
        var modsDir = Path.Combine(root, IsolatedModFolder);
        if (Directory.Exists(modsDir))
            Directory.Delete(modsDir, recursive: true);
        var modDir = Path.Combine(modsDir, modName);
        Directory.CreateDirectory(modDir);
        File.Copy(srcWasm, Path.Combine(modDir, "mod.wasm"), overwrite: true);
        File.WriteAllText(Path.Combine(modDir, "mod.json"),
            $"{{\"name\":\"{modName}\",\"version\":\"0.0.0\",\"wasm\":\"mod.wasm\",\"ruleset\":{{}}}}");
        Directory.SetCurrentDirectory(root);

        var app = new App(ThreadingMode.Single);
        app.AddResource(new ModdingConfig { ModFolder = IsolatedModFolder });
        app.AddPlugin<Modding.ModdingPlugin>();
        return app;
    }

    // Walk up from the test assembly dir to the repo root (the dir holding .git).
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git"))
                           && !File.Exists(Path.Combine(dir.FullName, ".git")))
            dir = dir.Parent;
        Assert.True(dir != null, "repo root (.git) not found above " + AppContext.BaseDirectory);
        return dir!.FullName;
    }

    [Fact]
    public void Mod_adds_button_to_topbar_and_reacts_to_click()
    {
        var app = BuildAppWith("topbar");

        // One-shot click injector: once a mod-owned interactive entity exists,
        // emit UiClick on it (the host observer should tag it ModClicked).
        app.AddSystem((Commands c, Query<Data<ModEntity>, Filter<With<Interaction>>> q, Local<int> fired) =>
        {
            if (fired.Value > 0)
                return;
            var emitted = false;
            foreach ((var e, var _) in q)
            {
                c.Entity(e.Ref).EmitTrigger(new UiClick { Position = default }, propagate: false);
                emitted = true;
            }
            if (emitted)
                fired.Value = 1;
        }).InStage(Stage.First).Build();

        app.RunStartup();

        var world = app.GetWorld();

        // Simulate the topbar gump: the group the mod looks for, with a Node so
        // the mod can read + widen its width.
        var topbar = world.Entity();
        topbar.Set<TopBarFull>();
        topbar.Set(new Node { Width = Val.Px(300), Height = Val.Px(27) });
        ulong topbarId = topbar.ID;

        // Frame 1: init finds the topbar, spawns + parents the button.
        // Later frames: click injected → observer tags → mod-tick bumps counter.
        for (var i = 0; i < 6; i++)
            app.Update();

        // Structure: caption "MOD" → child of the button → child of the topbar.
        ulong capId = 0;
        foreach ((var e, var t) in world.Query<Data<Text>>())
            if (t.Ref.Value == "MOD")
                capId = e.Ref;
        Assert.True(capId != 0, "mod did not spawn a 'MOD' caption");
        Assert.True(world.Has<TinyEcs.Parent>(capId), "caption not parented to a button");

        // 1) The button (caption's parent) is a UO-gump button parented under the topbar.
        ulong buttonId = (ulong)world.Get<TinyEcs.Parent>(capId).Id;
        Assert.True(world.Has<ModEntity>(buttonId), "button is not tagged mod-owned");
        Assert.True(world.Has<UiCustom>(buttonId), "button has no UO custom render (marble gump)");
        Assert.True(world.Has<TinyEcs.Parent>(buttonId), "button was not parented (set-parent failed)");
        Assert.Equal(topbarId, (ulong)world.Get<TinyEcs.Parent>(buttonId).Id);

        // 2) The click round-tripped: observer → cuo:ui/clicked → mod bumped counter.
        var clicks = int.MinValue;
        foreach ((var _, var c) in world.Query<Data<ModCounter>>())
            clicks = Math.Max(clicks, c.Ref.Value);

        Assert.True(clicks >= 1, $"click did not round-trip to the mod: counter={clicks}");
    }

    [Fact]
    public void Mod_finds_gump_by_traversal_and_injects_no_host_cooperation()
    {
        var app = BuildAppWith("topbar");
        app.RunStartup();

        var world = app.GetWorld();

        // Fake the options gump tree exactly as the host builds it — a root with
        // the EXISTING OptionsWindow marker and a child scroll viewport. The mod
        // gets no special tag; it must navigate via marker + traversal.
        var root = world.Entity();
        root.Set(new OptionsWindow());
        var vp = world.Entity();
        vp.Set(new ScrollPosition());
        vp.Set(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Val.Px(300), Height = Val.Px(200) });
        world.AddChild(root.ID, vp.ID);
        ulong vpId = vp.ID;

        for (var i = 0; i < 4; i++)
            app.Update();

        // Mod found the window by marker, walked children() to the ScrollPosition
        // node (via entity.get), and injected its section — parented to the viewport.
        ulong secId = 0;
        foreach ((var e, var n) in world.Query<Data<UiName>>())
            if (n.Ref.Value == "mod.options.section")
                secId = e.Ref;

        Assert.True(secId != 0, "mod did not inject via traversal");
        Assert.True(world.Has<TinyEcs.Parent>(secId), "section not parented");
        Assert.Equal(vpId, (ulong)world.Get<TinyEcs.Parent>(secId).Id);
    }

    // Phase 1: a mod replaces the host status gump entirely — despawns the host
    // StatusBarWindow, builds its own panel, reads the player's live stats, and
    // on a stat-lock click cycles StatLocks + sends a packet (net-send).
    [Fact]
    public void Mod_replaces_status_bar_reads_stats_and_sends_on_lock_click()
    {
        var app = BuildAppWith("status");
        // net-send target — Send no-ops offline, but the bridge → NetClient path runs.
        app.AddResource(new ClassicUO.Network.NetClient());

        // One-shot click injector: once the mod's lock buttons (mod-owned +
        // interactive) exist, emit UiClick on each so the host observer tags them.
        app.AddSystem((Commands c, Query<Data<ModEntity>, Filter<With<Interaction>>> q, Local<int> fired) =>
        {
            if (fired.Value > 0)
                return;
            var any = false;
            foreach ((var e, var _) in q)
            {
                c.Entity(e.Ref).EmitTrigger(new UiClick { Position = default }, propagate: false);
                any = true;
            }
            if (any)
                fired.Value = 1;
        }).InStage(Stage.First).Build();

        app.RunStartup();
        var world = app.GetWorld();

        // Modern client → the mod builds the AOS layout (stat-lock buttons + the
        // hits/stam/mana split), matching the host StatusGumpModern path.
        app.AddResource(new GameContext { ClientVersion = (ClassicUO.Utility.ClientVersion)0x07000000, PlayerName = "Tester" });

        // The host status bar is open...
        var hostBar = world.Entity();
        hostBar.Set<StatusBarWindow>();
        hostBar.Set(new Node());
        ulong hostBarId = hostBar.ID;

        // ...and the player carries live stats, as the packet handlers would set.
        var player = world.Entity();
        player.Set<Player>();
        player.Set(new Hits { Value = 42, MaxValue = 70 });
        player.Set(new Mana { Value = 10, MaxValue = 20 });
        player.Set(new Stamina { Value = 5, MaxValue = 30 });
        player.Set(new PlayerData { Str = 88, Dex = 77, Int = 66 });
        ulong playerId = player.ID;

        for (var i = 0; i < 6; i++)
            app.Update();

        // 1) Host status bar replaced (despawned, stays gone — it's static).
        Assert.False(world.Exists(hostBarId), "mod did not despawn the host status bar");

        // 2) Mod built its own labels, reflecting the player's live values. The
        //    modern AOS layout splits Hits/HitsMax into separate columns; labels
        //    are keyed by the StatusField id encoded in the UiName
        //    ("mod.status.lbl.{field}.{x}.{y}.{cw}"): 5=Hits, 6=HitsMax, 2=Str.
        string hitsText = null, hitsMaxText = null, strText = null;
        foreach ((var e, var n) in world.Query<Data<UiName>>())
        {
            var nm = n.Ref.Value;
            if (nm.StartsWith("mod.status.lbl.5.")) hitsText = world.Get<Text>(e.Ref).Value;
            else if (nm.StartsWith("mod.status.lbl.6.")) hitsMaxText = world.Get<Text>(e.Ref).Value;
            else if (nm.StartsWith("mod.status.lbl.2.")) strText = world.Get<Text>(e.Ref).Value;
        }
        Assert.Equal("42", hitsText);
        Assert.Equal("70", hitsMaxText);
        Assert.Equal("88", strText);

        // 3) Lock clicks cycled StatLocks up(0)→down(1) on the player (+ net-send
        //    ran without throwing).
        Assert.True(world.Has<StatLocks>(playerId), "lock click did not write StatLocks");
        var locks = world.Get<StatLocks>(playerId);
        Assert.True(locks.Str == 1 && locks.Dex == 1 && locks.Int == 1,
            $"locks not advanced to down(1): {locks.Str}/{locks.Dex}/{locks.Int}");

        // 4) Lock buttons opt out of window-drag (UiNoWindowDrag) so a LIVE press
        //    fires their UiClick instead of latching the movable panel's drag.
        ulong lockBtn = 0;
        foreach ((var e, var n) in world.Query<Data<UiName>>())
            if (n.Ref.Value == "mod.status.lock.0")
                lockBtn = e.Ref;
        Assert.True(lockBtn != 0, "mod did not build a Str lock button");
        Assert.True(world.Has<UiNoWindowDrag>(lockBtn), "lock button not opted out of window-drag");
        Assert.True(world.Has<Interaction>(lockBtn), "lock button not interactive");
        // Tagged with the host's StatLockButton marker → minimize-latch exempt in
        // HealthBarPlugin + the host Refresh keeps its lock graphic in sync.
        Assert.True(world.Has<StatLockButton>(lockBtn), "lock button missing host StatLockButton marker");

        // 5) The mod root carries StatusBarWindow, so the host treats it like the
        //    native gump (drag / right-click-close / click-minimize-to-healthbar).
        ulong root = 0;
        foreach ((var e, var n) in world.Query<Data<UiName>>())
            if (n.Ref.Value == "mod.status.root")
                root = e.Ref;
        Assert.True(root != 0, "mod did not build a panel root");
        Assert.True(world.Has<StatusBarWindow>(root), "mod root missing StatusBarWindow marker");
        // The minimize-region origin (legacy StatusGumpBase._point) round-trips
        // through the real wasm + bridge JSON, so the host only minimizes on a
        // click in the bottom-right corner — not anywhere on the panel (which
        // would also eat stat-lock/buff clicks). UOP modern → (540,180).
        var sb = world.Get<StatusBarWindow>(root);
        bool stamped = (sb.MinimizeX == 540f && sb.MinimizeY == 180f)   // UOP modern
                    || (sb.MinimizeX == 244f && sb.MinimizeY == 112f);  // classic old
        Assert.True(stamped, $"mod root minimize origin not stamped: {sb.MinimizeX},{sb.MinimizeY}");
    }

    // Phase 2: resource get/set through the real bridge (real App + registry +
    // IModResource + STJ). The wasm string marshalling is identical to the
    // proven component.get/set path, so this exercises the resource-specific half.
    [Fact]
    public void Resource_get_and_set_round_trips_through_the_bridge()
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(new GameContext { PlayerSerial = 0x1234, PlayerName = "Lord British", Map = 1, Season = 2 });

        var ctx = new ModHostContext
        {
            World = app.GetWorld(),
            Registry = CuoModdingRegistry.Build(),
            App = app,
        };
        var cmds = new CommandsImpl(ctx);

        // get — reads the live host resource.
        var json = cmds.ResourceGet("cuo:game/context");
        Assert.Contains("Lord British", json);

        // set — writes back the whitelisted fields.
        cmds.ResourceSet("cuo:game/context",
            "{\"PlayerSerial\":42,\"PlayerName\":\"Shadowlord\",\"Map\":3,\"Season\":1}");
        var gc = app.GetResource<GameContext>();
        Assert.Equal("Shadowlord", gc.PlayerName);
        Assert.Equal(3, gc.Map);
        Assert.Equal(42u, gc.PlayerSerial);

        // unregistered → "null", set is a no-op (no throw).
        Assert.Equal("null", cmds.ResourceGet("cuo:nope/missing"));
        cmds.ResourceSet("cuo:nope/missing", "{}");
    }

    // Incoming packets surface as a cuo:net/incoming custom event a mod observes
    // (here a host On<ModIncomingPacket> probe stands in for the mod's observer —
    // the same path ModEvent.RegisterObserver wires for guests). No entity, no
    // per-frame spawn/despawn.
    [Fact]
    public void Mod_observes_incoming_packets_via_custom_event()
    {
        var app = BuildAppWith("topbar"); // any mod loaded → tap gate opens

        var seen = new System.Collections.Generic.List<(byte id, string payload)>();
        app.AddObserver<On<ModIncomingPacket>>(t =>
            seen.Add((t.Event.Id, Convert.ToBase64String(t.Event.Payload))));

        app.RunStartup();
        Assert.True(app.GetResource<ModNetTap>().Tapped, "tap gate did not open with a mod loaded");

        // Emit one packet the way NetworkPlugin.PacketReader does (global trigger).
        app.AddSystem((Commands c, Local<int> fired) =>
        {
            if (fired.Value++ > 0) return;
            c.EmitTrigger(new ModIncomingPacket { Id = 0x11, Payload = new byte[] { 0x11, 0xAA, 0xBB } });
        }).InStage(Stage.Update).Build();

        app.Update();

        Assert.Single(seen);
        Assert.Equal(0x11, seen[0].id);
        Assert.Equal(Convert.ToBase64String(new byte[] { 0x11, 0xAA, 0xBB }), seen[0].payload);
    }

    // Full guest round-trip: a real WASM mod (ecs-netlog) observes cuo:net/incoming
    // via add_observer(Custom(..)) and LIVE-appends ID + size + hex rows as packets
    // arrive — no button click needed. Proves the custom-event reaches the guest AND
    // the guest spawns UI from it.
    [Fact]
    public void Netlog_mod_live_lists_incoming_packets()
    {
        var app = BuildAppWith("netlog");
        app.RunStartup();
        app.Update(); // mod-netlog-init spawns the window (one-shot guard)

        var world = app.GetWorld();
        Assert.True(FindByName(world, "netlog.root") != 0, "netlog window not spawned");

        // Incoming packets via the custom event (host On<ModIncomingPacket> → guest
        // on-packet). This is the leg no fixture covered before.
        app.AddSystem((Commands c, Local<int> fired) =>
        {
            if (fired.Value++ > 0) return;
            c.EmitTrigger(new ModIncomingPacket { Id = 0xB9, Payload = new byte[] { 0x10, 0x20, 0x30 } });
            c.EmitTrigger(new ModIncomingPacket { Id = 0x1B, Payload = new byte[] { 0xAA } });
        }).InStage(Stage.First).Build();

        // Rows append live across the next few frames — no STOP click required.
        for (var i = 0; i < 4; i++)
            app.Update();

        var header = false;
        var hex = false;
        foreach ((var _, var t) in world.Query<Data<Text>>())
        {
            if (t.Ref.Value.Contains("0xB9")) header = true;
            if (t.Ref.Value.Contains("10 20 30")) hex = true;
        }
        Assert.True(header, "live append did not build the packet header row (0xB9 size=3)");
        Assert.True(hex, "live append did not build the packet hex row (10 20 30)");
    }

    // Clicking the LABEL/mark overlay (not just the control rect) must trigger:
    // absolute children capture the pointer, so each carries its own Interaction
    // and its name routes to the parent's action. Here we click the mark text.
    [Fact]
    public void Netlog_autoscroll_checkbox_toggles_when_mark_clicked()
    {
        var app = BuildAppWith("netlog");
        app.RunStartup();
        app.Update(); // window + checkbox spawn

        var world = app.GetWorld();
        var markId = FindByName(world, "netlog.chk.mark");
        Assert.True(markId != 0, "checkbox mark missing");
        Assert.Equal("X", TextOf(world, "netlog.chk.mark")); // default on

        // Click the MARK (the text overlay), not the box rect behind it.
        app.AddSystem((Commands c, Local<int> fired) =>
        {
            if (fired.Value++ > 0) return;
            c.Entity(markId).EmitTrigger(new UiClick { Position = default }, propagate: false);
        }).InStage(Stage.First).Build();
        for (var i = 0; i < 6; i++)
            app.Update();

        Assert.Equal("", TextOf(world, "netlog.chk.mark")); // clicking the label toggled it off
    }

    // The FULL faithful port, the tooltip screen: the actual user-interface mod's
    // React StorybookScreen (IconButton + Tooltip + showcases, reused verbatim —
    // reconciler.ts/createElement.ts/components/ui), bundled into a
    // jco/componentize-js component; only the host boundary (HostWrapper) is
    // reimplemented over `commands`. Exercises the HOVER round-trip: hover is read
    // by polling cuo:ui/interaction (no host push), diffed frame-to-frame into
    // OnMouseEnter/OnMouseLeave; a tooltip's delay timer (driven by the pumped
    // engine clock) then shows the floating tooltip gump + content text.
    //
    // ONE test (single load): the 14MB React component is ~10x a Rust mod, and the
    // fork's wasmtime hits "Maximum number of functions reached" if loaded more
    // than once in the shared test process. Production loads each mod once.
    [Fact]
    public void Jco_react_storybook_renders_and_tooltip_shows_on_hover()
    {
        var app = BuildAppWith("ui");

        // The bare modding app has no Clay pointer pipeline, so Interaction never
        // flips to Hovered on its own. Simulate the cursor resting on the icon
        // gumps — the LEAF interactive elements (Interaction + UiCustom). Crucially
        // we do NOT flag the Tooltip wrapper Views (Interaction, no UiCustom): the
        // real Clay pipeline only flags the topmost interactive element, so the
        // tooltip arms ONLY if the guest walks the hovered child's parent chain up
        // to the wrapper that owns onMouseEnter. Flagging every interactive element
        // (as before) hid that bug — the wrapper was hovered directly.
        app.AddSystem((Commands c, Query<Data<ModEntity>, Filter<With<Interaction>, With<UiCustom>>> q) =>
        {
            foreach ((var e, var _) in q)
                c.Entity(e.Ref).Insert(Interaction.Hovered);
        }).InStage(Stage.First).Build();

        app.RunStartup(); // ui-startup mounts <StorybookScreen/>
        // Render the (large) tree, then let the hovered tooltip's delay timer fire
        // and re-render. The pump estimates ~16ms/frame, so 60 frames ~= 960ms,
        // past the 500ms default tooltip delay (and the delay:0 "Instant" one).
        for (var i = 0; i < 60; i++)
            app.Update();

        var world = app.GetWorld();

        // 1) Render fidelity: the screen title + the icon buttons' UO render (the
        //    IconButton gump 0x15a4 → a UiCustom).
        var hasTitle = false;
        var hasTooltip = false;
        foreach ((var _, var t) in world.Query<Data<Text>>())
        {
            var v = t.Ref.Value;
            if (v == "ClassicUO Design System") hasTitle = true;
            // 2) Hover round-trip: a tooltip's content text only exists once the
            //    hover arms its delay timer and the re-render mounts the overlay.
            if (v == "Instant tooltip" || v == "This is a basic tooltip") hasTooltip = true;
        }
        Assert.True(hasTitle, "StorybookScreen title did not render");

        var icons = 0;
        foreach ((var _, var _u) in world.Query<Data<UiCustom>>())
            icons++;
        Assert.True(icons >= 1, "StorybookScreen icon buttons did not render any UiCustom");

        Assert.True(hasTooltip, "Tooltip did not show on hover (no tooltip content text rendered)");

        // 3) Page swap: clicking a category label swaps the rendered story. Default
        //    page is "Display" (TooltipStory); clicking "Action" must render
        //    IconButtonStory. The entity carrying the visible "Action" glyphs must
        //    ITSELF be interactive — otherwise the click lands on a non-interactive
        //    glyph fragment while the interactive empty-text parent has a zero-size,
        //    unclickable box (the dead-click bug).
        ulong actionBtn = 0;
        foreach ((var e, var t) in world.Query<Data<Text>>())
            if (t.Ref.Value == "Action") { actionBtn = e.Ref; break; }
        Assert.True(actionBtn != 0, "category label 'Action' not rendered");
        Assert.True(world.Has<Interaction>(actionBtn),
            "category label 'Action' is not clickable: glyphs sit on a non-interactive fragment");

        // Click it the way the host bridge does (On<UiClick> -> ModClicked -> poll).
        app.AddSystem((Commands c, Local<int> fired) =>
        {
            if (fired.Value++ > 0) return;
            c.Entity(actionBtn).EmitTrigger(new UiClick { Position = default }, propagate: false);
        }).InStage(Stage.First).Build();

        for (var i = 0; i < 8; i++)
            app.Update();

        var onIconButtonPage = false;
        var stillOnTooltipPage = false;
        foreach ((var _, var t) in world.Query<Data<Text>>())
        {
            var v = t.Ref.Value;
            if (v == "Button with icon overlay supporting both gump and art icons") onIconButtonPage = true;
            if (v == "Hover information display component that wraps child elements") stillOnTooltipPage = true;
        }
        Assert.True(onIconButtonPage, "clicking 'Action' did not swap to the IconButton page");
        Assert.False(stillOnTooltipPage, "TooltipStory still rendered after swapping pages");
    }

    private static ulong FindByName(World world, string name)
    {
        foreach ((var e, var n) in world.Query<Data<UiName>>())
            if (n.Ref.Value == name)
                return e.Ref;
        return 0;
    }

    private static string TextOf(World world, string name)
    {
        var id = FindByName(world, name);
        foreach ((var e, var t) in world.Query<Data<Text>>())
            if (e.Ref == id)
                return t.Ref.Value;
        return null;
    }

    // Phase 3: mod reads live input state via a registered resource (mouse).
    [Fact]
    public void Mouse_input_resource_reads_through_the_bridge()
    {
        var app = new App(ThreadingMode.Single);
        var mouse = new TestMouseContext();
        // Two Left frames → button is HELD (IsPressed checks down in old+new).
        mouse.Frame(TestMouseContext.Left(123, 45), TestMouseContext.Left(123, 45));
        app.AddResource<MouseContext>(mouse);

        var ctx = new ModHostContext
        {
            World = app.GetWorld(),
            Registry = CuoModdingRegistry.Build(),
            App = app,
        };
        var cmds = new CommandsImpl(ctx);

        var json = cmds.ResourceGet("cuo:input/mouse");
        Assert.Contains("\"X\":123", json);
        Assert.Contains("\"Left\":true", json);
        Assert.Contains("\"Right\":false", json);
    }

    // Phase 4: a mod suppresses an incoming packet id (host PacketReader skips
    // dispatch for blocked ids — the toggle the bridge drives).
    [Fact]
    public void Packet_block_toggles_through_the_bridge()
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(new ModNetTap());
        var ctx = new ModHostContext
        {
            World = app.GetWorld(),
            Registry = CuoModdingRegistry.Build(),
            App = app,
        };
        // block/unblock live on the cuo:modding/net resource (game-specific),
        // not the generic tinyecs `commands`.
        var net = new NetImpl(ctx);
        var tap = app.GetResource<ModNetTap>();

        net.BlockPacket(0x11);
        Assert.Contains((byte)0x11, tap.Blocked);
        net.UnblockPacket(0x11);
        Assert.DoesNotContain((byte)0x11, tap.Blocked);
    }

    // Phase 4: a mod consumes a mouse button so host gameplay ignores it. Real
    // honor — MouseContext.IsPressed goes false once consumed (what host systems
    // check).
    [Fact]
    public void Mouse_consume_overrides_input_through_the_bridge()
    {
        var app = new App(ThreadingMode.Single);
        var mouse = new TestMouseContext();
        mouse.Frame(TestMouseContext.Left(10, 10), TestMouseContext.Left(10, 10)); // held
        app.AddResource<MouseContext>(mouse);
        var ctx = new ModHostContext
        {
            World = app.GetWorld(),
            Registry = CuoModdingRegistry.Build(),
            App = app,
        };
        // The generic bridge routes input-consume to a host hook; wire it exactly
        // as the plugin does (shared helper) so this exercises the real path.
        CuoModBridge.WireInput(ctx);
        var cmds = new CommandsImpl(ctx);

        Assert.True(mouse.IsPressed(ClassicUO.Input.MouseButtonType.Left));
        cmds.InputConsumeMouse((byte)ClassicUO.Input.MouseButtonType.Left);
        Assert.True(mouse.IsConsumed(ClassicUO.Input.MouseButtonType.Left));
        Assert.False(mouse.IsPressed(ClassicUO.Input.MouseButtonType.Left));
    }
}
