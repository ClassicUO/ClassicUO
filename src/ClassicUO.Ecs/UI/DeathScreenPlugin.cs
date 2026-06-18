// Death screen — ECS port of legacy PacketHandlers.DeathScreen (0x2C) +
// GameScene.CheckDeathScreen. On the server death packet (action != 1) the
// death music plays and, when EnableDeathScreen is set, a centred "You are
// dead." overlay shows for DEATH_SCREEN_TIMER ms. The grey world is already
// handled separately (EnableBlackWhiteEffect in WorldRenderingPlugin).

using ClassicUO.Configuration;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal sealed class DeathScreenState { public ulong Root; public float Expire; }

internal struct DeathScreenText;

internal readonly struct DeathScreenPlugin : IPlugin
{
    private const int DeathMusicIndex = 42;     // legacy AudioManager.DeathMusicIndex
    private const float DeathScreenMs = 1500f;  // legacy Constants.DEATH_SCREEN_TIMER

    public void Build(App app)
    {
        app.AddResource(new DeathScreenState());

        // Coexists with the InGamePacketsPlugin stub observer for 0x2C (observers
        // fan out — both fire).
        app.AddObserver<On<PacketReceived<OnShowDeathScreenPacket_0x2C>>,
            Commands,
            Res<Profile>,
            ResMut<AudioState>,
            Res<AssetsServer>,
            Res<Settings>,
            Res<Time>,
            Res<UiSurface>,
            ResMut<DeathScreenState>>(OnDeath);

        var expireFn = ExpireDeathScreen;
        app.AddSystem(expireFn).InStage(Stage.Update).Build();

        var despawnFn = DespawnOnExit;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    private static void OnDeath(
        On<PacketReceived<OnShowDeathScreenPacket_0x2C>> trig,
        Commands commands,
        Res<Profile> profile,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Settings> settings,
        Res<Time> time,
        Res<UiSurface> surface,
        ResMut<DeathScreenState> state)
    {
        // action 1 = "resurrect / not actually dead" (legacy skips it).
        if (trig.Event.Packet.Action == 1)
            return;

        audio.Value.PlayMusic(
            assets.Value, profile.Value, settings.Value, time.Value.Total,
            DeathMusicIndex, isWarmode: true);

        if (!profile.Value.EnableDeathScreen)
            return;

        if (state.Value.Root != 0)
            commands.Entity(state.Value.Root).Despawn();

        // Parentless root: fixed pixel size from the surface (Percent collapses on
        // a root) + GlobalZIndex so it paints over the world; centres the label.
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0),
                Top = Val.Px(0),
                Width = Val.Px(surface.Value.LogicalSize.X),
                Height = Val.Px(surface.Value.LogicalSize.Y),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            })
            .Insert(new GlobalZIndex(short.MaxValue - 2))
            .Insert<DeathScreenText>();

        commands.AddChild(root.Id, commands.Spawn()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text("You are dead."))
            .Insert(new TextFont { FontId = 1, Size = 22 })
            .Insert(new TextColor(new ClayColor(255, 255, 255, 255)))
            .Id);

        state.Value.Root = root.Id;
        state.Value.Expire = time.Value.Total + DeathScreenMs;
    }

    private static void ExpireDeathScreen(
        Commands commands,
        Res<Time> time,
        ResMut<DeathScreenState> state)
    {
        if (state.Value.Root == 0 || time.Value.Total < state.Value.Expire)
            return;
        commands.Entity(state.Value.Root).Despawn();
        state.Value.Root = 0;
    }

    private static void DespawnOnExit(
        Commands commands,
        ResMut<DeathScreenState> state,
        Query<Data<DeathScreenText>> q)
    {
        foreach (var (ent, _) in q)
            commands.Entity(ent.Ref).Despawn();
        state.Value.Root = 0;
    }
}
