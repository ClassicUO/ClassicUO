using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

[TinyPlugin]
internal readonly partial struct FnaPlugin
{
    public bool WindowResizable { get; init; }
    public bool MouseVisible { get; init; }
    public bool VSync { get; init; }

    public void Build(Scheduler scheduler)
    {
        scheduler.AddPlugin<TextHandlerPlugin>();
        scheduler.AddPlugin<FontsPlugin>();
        scheduler.AddPlugin<DelayedActionPlugin>();
        scheduler.AddPlugin<CameraPlugin>();


        scheduler.AddResource(new UoGame(MouseVisible, WindowResizable, VSync));
        scheduler.AddSystemParam(new Time());
    }


    [TinySystem(Stages.Startup, ThreadingMode.Single)]
    private static void Setup(Res<UoGame> game, SchedulerState schedState)
    {
        game.Value.RunOneFrame();
        UnsafeFNAAccessor.BeforeLoop(game.Value);
        // game.Value.RunOneFrame();
        schedState.AddResource(game.Value.GraphicsDevice);
        schedState.AddResource(new KeyboardContext(game));
        schedState.AddResource(new MouseContext(game));
        UnsafeFNAAccessor.GetSetRunApplication(game.Value) = true;
    }

    private static bool OnCheckIfInGame(SchedulerState state) => state.ResourceExists<UoGame>();

    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(OnCheckIfInGame))]
    private static void Tick(Res<UoGame> game, Time time)
    {
        game.Value.SuppressDraw();
        game.Value.Tick();

        time.Frame = (float)game.Value.GameTime.ElapsedGameTime.TotalSeconds;
        time.Total += time.Frame * 1000f;

        FrameworkDispatcher.Update();
    }


    private static bool OnCheckGraphicsDeviceExists(SchedulerState state) => state.ResourceExists<GraphicsDevice>();

    [TinySystem(Stages.FrameStart, ThreadingMode.Single)]
    [RunIf(nameof(OnCheckGraphicsDeviceExists))]
    private static void ClearBackBuffer(Res<GraphicsDevice> device)
    {
        device.Value.Clear(Color.Black);
    }

    [TinySystem(Stages.FrameEnd, ThreadingMode.Single)]
    [RunIf(nameof(OnCheckGraphicsDeviceExists))]
    private static void Present(Res<GraphicsDevice> device)
    {
        device.Value.Present();
    }


    private static bool IsGameNotRunning(Res<UoGame> game) => !UnsafeFNAAccessor.GetSetRunApplication(game.Value);

    [TinySystem(Stages.AfterUpdate, ThreadingMode.Single)]
    [RunIf(nameof(IsGameNotRunning))]
    private static void Exit()
    {
        Environment.Exit(0);
    }

    private static bool IsGameRunning(Res<UoGame> game) => UnsafeFNAAccessor.GetSetRunApplication(game.Value);

    [TinySystem(Stages.FrameEnd, ThreadingMode.Single)]
    [RunIf(nameof(IsGameRunning))]
    private static void UpdateInputs(Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, Time time)
    {
        mouseCtx.Value.Update(time.Total);
        keyboardCtx.Value.Update(time.Total);
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
