using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct CuoPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddState(GameState.Loading);
        scheduler.AddResource(new GameContext() { Map = -1, MaxObjectsDistance = 32 });
        scheduler.AddResource(Settings.GlobalSettings);

        scheduler.OnStartup((Res<GameContext> gameCtx, Res<Settings> settings) =>
        {
            ClientVersionHelper.IsClientVersionValid(
                settings.Value.ClientVersion,
                out gameCtx.Value.ClientVersion
            );
        });

        scheduler.AddPlugin(new FnaPlugin()
        {
            WindowResizable = true,
            MouseVisible = true,
            VSync = true, // don't kill the gpu
        });
        scheduler.AddPlugin<AssetsPlugin>();
        scheduler.AddPlugin<TerrainPlugin>();
        scheduler.AddPlugin<GuiPlugin>();

        scheduler.AddPlugin<NetworkPlugin>();
        scheduler.AddPlugin<GameplayPlugin>();
        scheduler.AddPlugin<RenderingPlugin>();
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
    public int MaxMapWidth, MaxMapHeight;
    public int MaxObjectsDistance;
}

public enum GameState : byte
{
    Loading,
    LoginScreen,
    ServerSelection,
    CharacterSelection,
    LoginError,
    GameScreen
}