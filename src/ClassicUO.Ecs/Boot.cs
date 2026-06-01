using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct CuoPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddState(GameState.Loading);
        app.AddResource(new GameContext() { Map = -1, MaxObjectsDistance = 32 });
        app.AddResource(Settings.GlobalSettings);
        app.AddResource(new NetClient());

        app.AddSystem(Stage.Startup, (ResMut<GameContext> gameCtx, Res<Settings> settings) =>
        {
            ClientVersionHelper.IsClientVersionValid(
                settings.Value.ClientVersion,
                out gameCtx.Value.ClientVersion
            );
        });

        app.AddPlugin(new FnaPlugin()
        {
            WindowResizable = true,
            MouseVisible = true,
            VSync = true, // don't kill the gpu
        });
        app.AddPlugin<AssetsPlugin>();
        app.AddPlugin<TerrainPlugin>();
        app.AddPlugin<GuiPlugin>();

        app.AddPlugin<LoginScreenPlugin>();
        app.AddPlugin<GameScreenPlugin>();
        app.AddPlugin<TopBarPlugin>();
        app.AddPlugin<MiniMapPlugin>();
        app.AddPlugin<JournalPlugin>();
        app.AddPlugin<StatusBarPlugin>();
        app.AddPlugin<HealthBarPlugin>();
        app.AddPlugin<PartyPlugin>();
        app.AddPlugin<PartyGumpPlugin>();
        app.AddPlugin<SkillsGumpPlugin>();
        app.AddPlugin<SpellbookGumpPlugin>();

        app.AddPlugin<NetworkPlugin>();
        app.AddPlugin<GameplayPlugin>();
        app.AddPlugin<RenderingPlugin>();
        // After RenderingPlugin: TargetingPlugin's cursor render system orders
        // .After("cuo:gui_rendering"), a label RenderingPlugin's GuiRenderingPlugin
        // child registers. The label must exist before TargetingPlugin.Build runs.
        app.AddPlugin<TargetingPlugin>();

        app.AddPlugin<ModdingPlugin>();

#if AGENT_BUILD
        // Dev-loop agent server: TCP+JSON-RPC listener on loopback,
        // gated by -p:AGENT_BUILD=true. Stripped entirely from prod builds.
        app.AddPlugin<Agent.AgentServerPlugin>();
#endif
    }
}

struct GameContext
{
    public int Map;
    public ushort CenterX, CenterY;
    public sbyte CenterZ;
    public Vector2 CenterOffset;
    public bool FreeView;
    public uint PlayerSerial;
    public string PlayerName;
    public ClientFlags Protocol;
    public CharacterListFlags ClientFeatures;
    public ClientVersion ClientVersion;
    public int MaxObjectsDistance;
    // Last server-pushed season (0=Spring, 1=Summer, 2=Fall, 3=Winter,
    // 4=Desolation) and the music index that came with it. Set by the
    // 0xBC handler; consumed by terrain tile-art selection and the
    // music plugin once those are wired into ECS.
    public byte Season;
    public byte SeasonMusicIndex;
    // Last server-pushed light levels (0..30; lower = darker). Consumed
    // by the world light pass once it's ported. Cached here so the
    // lighting plugin can pick them up at startup.
    public byte ServerLightLevel;
    public byte PersonalLightLevel;
}

public enum GameState : byte
{
    Loading,
    LoginScreen,
    ServerSelection,
    CharacterSelection,
    CharacterCreation,
    LoginError,
    GameScreen
}
