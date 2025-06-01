using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct FnaPlugin : IPlugin
{
    public bool WindowResizable { get; init; }
    public bool MouseVisible { get; init; }
    public bool VSync { get; init; }

    public void Build(Scheduler scheduler)
    {
        scheduler.AddPlugin<TextHandlerPlugin>();
        scheduler.AddPlugin<FontsPlugin>();
        scheduler.AddPlugin<DelayedActionPlugin>();

        scheduler.AddResource(new UoGame(MouseVisible, WindowResizable, VSync));
        scheduler.AddSystemParam(new Time());

        scheduler.OnStartup(static (Res<UoGame> game, SchedulerState schedState) =>
        {
            game.Value.RunOneFrame();
            UnsafeFNAAccessor.BeforeLoop(game.Value);
            // game.Value.RunOneFrame();
            schedState.AddResource(game.Value.GraphicsDevice);
            schedState.AddResource(new KeyboardContext(game));
            schedState.AddResource(new MouseContext(game));
            UnsafeFNAAccessor.GetSetRunApplication(game.Value) = true;
        }, ThreadingMode.Single);

        scheduler.OnUpdate((Res<UoGame> game, Time time) =>
        {
            game.Value.SuppressDraw();
            game.Value.Tick();

            time.Frame = (float)game.Value.GameTime.ElapsedGameTime.TotalSeconds;
            time.Total += time.Frame * 1000f;

            FrameworkDispatcher.Update();
        }, ThreadingMode.Single)
        .RunIf((SchedulerState state) => state.ResourceExists<UoGame>());


        scheduler.OnFrameStart((Res<GraphicsDevice> device) => device.Value.Clear(Color.Black), ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());

        scheduler.OnFrameEnd((Res<GraphicsDevice> device) => device.Value.Present(), ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());

        scheduler.OnAfterUpdate(() => Environment.Exit(0), ThreadingMode.Single)
                .RunIf(static (Res<UoGame> game) => !UnsafeFNAAccessor.GetSetRunApplication(game.Value));

        scheduler.OnFrameEnd((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, Time time) =>
        {
            mouseCtx.Value.Update(time.Total);
            keyboardCtx.Value.Update(time.Total);
        }, ThreadingMode.Single);
    }

    private sealed class UnsafeFNAAccessor
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "RunApplication")]
        public static extern ref bool GetSetRunApplication(Microsoft.Xna.Framework.Game instance);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "BeforeLoop")]
        public static extern void BeforeLoop(Microsoft.Xna.Framework.Game instance);
    }
}

internal sealed class Time : SystemParam<TinyEcs.World>, IIntoSystemParam<World>
{
    public float Total;
    public float Frame;

    public static ISystemParam<World> Generate(World arg)
    {
        if (arg.Entity<Placeholder<Time>>().Has<Placeholder<Time>>())
            return arg.Entity<Placeholder<Time>>().Get<Placeholder<Time>>().Value;

        var time = new Time();
        arg.Entity<Placeholder<Time>>().Set(new Placeholder<Time>() { Value = time });
        return time;
    }

    private struct Placeholder<T> { public T Value; }
}
