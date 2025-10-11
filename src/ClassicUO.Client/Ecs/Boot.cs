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

        app.AddPlugin<NetworkPlugin>();
        app.AddPlugin<GameplayPlugin>();
        app.AddPlugin<RenderingPlugin>();

        app.AddPlugin<ModdingPlugin>();
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
    public ClientFlags Protocol;
    public CharacterListFlags ClientFeatures;
    public ClientVersion ClientVersion;
    public int MaxObjectsDistance;
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
