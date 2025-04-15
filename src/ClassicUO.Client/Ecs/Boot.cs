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
        scheduler.AddResource(new GameContext() { Map = -1 });
        scheduler.AddResource(Settings.GlobalSettings);

        scheduler.AddSystem((Res<GameContext> gameCtx, Res<Settings> settings) =>
        {
            ClientVersionHelper.IsClientVersionValid(
                settings.Value.ClientVersion,
                out gameCtx.Value.ClientVersion
            );
        }, Stages.Startup);

        scheduler.AddPlugin(new FnaPlugin()
        {
            WindowResizable = true,
            MouseVisible = true,
            VSync = true, // don't kill the gpu
        });
        scheduler.AddPlugin<AssetsPlugin>();
        scheduler.AddPlugin<TerrainPlugin>();
        scheduler.AddPlugin<NetworkPlugin>();
        scheduler.AddPlugin<ChatPlugin>();
        scheduler.AddPlugin<PickupPlugin>();
        scheduler.AddPlugin<MobAnimationsPlugin>();
        scheduler.AddPlugin<PlayerMovementPlugin>();
        scheduler.AddPlugin<WorldRenderingPlugin>();
        scheduler.AddPlugin<TextOverheadPlugin>();
        scheduler.AddPlugin<UIRenderingPlugin>();
        scheduler.AddPlugin<GuiPlugin>();

        // TODO: remove this once the UI is done
        scheduler.AddSystem((EventWriter<OnLoginRequest> writer, Res<Settings> settings) =>
            writer.Enqueue(new OnLoginRequest()
            {
                Address = settings.Value.IP,
                Port = settings.Value.Port,
            }), Stages.Startup);

        scheduler
            .AddSystem((TinyEcs.World world) => Console.WriteLine("Archetypes removed: {0}", world.RemoveEmptyArchetypes()), threadingType: ThreadingMode.Single)
            .RunIf(
                (Time time, Local<float> updateTime) =>
                {
                    if (updateTime.Value > time.Total)
                        return false;

                    updateTime.Value = time.Total + 3000f;
                    return true;
                });
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
    public ClientVersion ClientVersion;
    public int MaxMapWidth, MaxMapHeight;
}
