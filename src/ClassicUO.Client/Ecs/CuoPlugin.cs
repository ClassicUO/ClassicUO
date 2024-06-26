using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

struct GameContext
{
    public int Map;
    public ushort CenterX, CenterY;
    public sbyte CenterZ;
    public Vector2 CenterOffset;
    public uint PlayerSerial;
    public ClientFlags Protocol;
    public ClientVersion ClientVersion;
    public int MaxMapWidth, MaxMapHeight;
}


readonly struct CuoPlugin : IPlugin
{
    public unsafe void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new GameContext() { Map = -1 });
        scheduler.AddResource(Settings.GlobalSettings);

        scheduler.AddSystem((TinyEcs.World world) => {
            // force the component initialization. Queries must know before the components to search
            world.Entity<Renderable>();
            world.Entity<TileStretched>();
            world.Entity<WorldPosition>();
            world.Entity<Graphic>();
            world.Entity<NetworkSerial>();
            world.Entity<ContainedInto>();
        }, Stages.Startup);

        scheduler.AddSystem((Res<GameContext> gameCtx, Res<Settings> settings) => {
            ClientVersionHelper.IsClientVersionValid(
                settings.Value.ClientVersion,
                out gameCtx.Value.ClientVersion
            );
        }, Stages.Startup);

        scheduler.AddPlugin(new FnaPlugin() {
            WindowResizable = true,
            MouseVisible = true,
            VSync = true, // don't kill the gpu
        });
        scheduler.AddPlugin<AssetsPlugin>();
        scheduler.AddPlugin<TerrainPlugin>();
        scheduler.AddPlugin<NetworkPlugin>();
        scheduler.AddPlugin<RenderingPlugin>();


        // TODO: remove this once the UI is done
        scheduler.AddSystem((EventWriter<OnLoginRequest> writer, Res<Settings> settings) =>
            writer.Enqueue(new OnLoginRequest() {
                Address = settings.Value.IP,
                Port = settings.Value.Port,
        }), Stages.Startup);
    }
}
