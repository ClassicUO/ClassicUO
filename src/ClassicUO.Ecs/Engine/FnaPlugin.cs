using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct FnaPlugin : IPlugin
{
    public bool WindowResizable { get; init; }
    public bool MouseVisible { get; init; }
    public bool VSync { get; init; }

    public void Build(App app)
    {
        app
            .AddPlugin<TextHandlerPlugin>()
            .AddPlugin<FontsPlugin>()
            .AddPlugin<DelayedActionPlugin>()
            .AddPlugin<CameraPlugin>()

            .AddResource(new UoGame(MouseVisible, WindowResizable, VSync))
            .AddResource(new Time())

            .AddSystem(static (Res<UoGame> game, Commands commands) =>
            {
                game.Value.RunOneFrame();
                UnsafeFNAAccessor.BeforeLoop(game.Value);
                // game.Value.RunOneFrame();
                commands.InsertResource(game.Value.GraphicsDevice);
                commands.InsertResource(new KeyboardContext(game.Value));
                commands.InsertResource(new MouseContext(game.Value));
                UnsafeFNAAccessor.GetSetRunApplication(game.Value) = true;
            })
            .InStage(Stage.Startup)
            .Build()

            .AddSystem((Res<UoGame> game, Res<Time> time) =>
            {
                game.Value.SuppressDraw();
                game.Value.Tick();

                time.Value.Frame = (float)game.Value.GameTime.ElapsedGameTime.TotalSeconds;
                time.Value.Total += time.Value.Frame * 1000f;

                FrameworkDispatcher.Update();
            })
            .InStage(Stage.Update)
            .SingleThreaded()
            .RunIf((Commands cmds) => cmds.HasResource<UoGame>())
            .Build()

            .AddSystem((Res<GraphicsDevice> device) => device.Value.Clear(Color.Black))
            .InStage(Stage.First)
            .SingleThreaded()
            .RunIf((Commands cmds) => cmds.HasResource<GraphicsDevice>())
            .RunIf((Res<UoGame> game) => game.Value.IsDrawable)
            .Build()

            .AddSystem((Res<GraphicsDevice> device) => device.Value.Present())
            .InStage(Stage.Last)
            .SingleThreaded()
            .RunIf((Commands cmds) => cmds.HasResource<GraphicsDevice>())
            .RunIf((Res<UoGame> game) => game.Value.IsDrawable)
            .Build()

            .AddSystem(_ => Environment.Exit(0))
            .InStage(Stage.PostUpdate)
            .Label("cuo:app_exit")
            .SingleThreaded()
            .RunIf(static (Res<UoGame> game) => !UnsafeFNAAccessor.GetSetRunApplication(game.Value))
            .Build()

            .AddSystem((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, Res<Time> time) =>
            {
                mouseCtx.Value.Update(time.Value.Total);
                keyboardCtx.Value.Update(time.Value.Total);
            })
            .InStage(Stage.First)
            .SingleThreaded()
            .Build();
    }

    // True once FNA has cleared its RunApplication flag (window close / quit) and
    // the process is about to call Environment.Exit(0). Lets the persistence layer
    // flush to disk on the same frame, before "cuo:app_exit" runs.
    public static bool IsAppExiting(UoGame game) => !UnsafeFNAAccessor.GetSetRunApplication(game);

    private sealed class UnsafeFNAAccessor
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "RunApplication")]
        public static extern ref bool GetSetRunApplication(Microsoft.Xna.Framework.Game instance);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "BeforeLoop")]
        public static extern void BeforeLoop(Microsoft.Xna.Framework.Game instance);
    }
}

