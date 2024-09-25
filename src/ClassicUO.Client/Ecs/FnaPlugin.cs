using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

internal struct MouseContext
{
    public MouseState OldState, NewState;
}

internal struct KeyboardContext
{
    public KeyboardState OldState, NewState;
}

internal sealed class Time : SystemParam
{
    public float Total;
    public float Frame;
}

internal readonly struct FnaPlugin : IPlugin
{
    public bool WindowResizable { get; init; }
    public bool MouseVisible { get; init; }
    public bool VSync { get; init; }

    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new UoGame(MouseVisible, WindowResizable, VSync));
        scheduler.AddResource(new KeyboardContext());
        scheduler.AddResource(new MouseContext());
        scheduler.AddSystemParam(new Time());
        scheduler.AddEvent<KeyEvent>();
        scheduler.AddEvent<MouseEvent>();
        scheduler.AddEvent<WheelEvent>();

        scheduler.AddSystem(static (Res<UoGame> game, SchedulerState schedState) => {
            UnsafeFNAAccessor.BeforeLoop(game.Value);
            game.Value.RunOneFrame();
            schedState.AddResource(game.Value.GraphicsDevice);
            UnsafeFNAAccessor.GetSetRunApplication(game.Value) = true;
        }, Stages.Startup, ThreadingMode.Single);

        scheduler.AddSystem((Res<UoGame> game, Time time) => {
            game.Value.SuppressDraw();
            game.Value.Tick();

            time.Frame = (float)game.Value.GameTime.ElapsedGameTime.TotalSeconds;
            time.Total += time.Frame * 1000f;

            FrameworkDispatcher.Update();
        }, threadingType: ThreadingMode.Single).RunIf((SchedulerState state) => state.ResourceExists<UoGame>());


        scheduler.AddSystem((Res<GraphicsDevice> device) => device.Value.Clear(Color.Black), Stages.FrameStart, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());

        scheduler.AddSystem((Res<GraphicsDevice> device) => device.Value.Present(), Stages.FrameEnd, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());

        scheduler.AddSystem(() => Environment.Exit(0), Stages.AfterUpdate)
                .RunIf(static (Res<UoGame> game) => !UnsafeFNAAccessor.GetSetRunApplication(game.Value));

        scheduler.AddSystem((EventWriter<KeyEvent> writer, Res<KeyboardContext> keyboardCtx) => {
            foreach (var key in keyboardCtx.Value.OldState.GetPressedKeys())
                if (keyboardCtx.Value.NewState.IsKeyUp(key)) // [pressed] -> [released]
                    writer.Enqueue(new() { Action = 0, Key = key });

            foreach (var key in keyboardCtx.Value.NewState.GetPressedKeys())
                if (keyboardCtx.Value.OldState.IsKeyUp(key)) // [released] -> [pressed]
                    writer.Enqueue(new() { Action = 1, Key = key });
                else if (keyboardCtx.Value.OldState.IsKeyDown(key))
                    writer.Enqueue(new() { Action = 2, Key = key });
        }, Stages.FrameEnd).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem((EventWriter<MouseEvent> writer, EventWriter<WheelEvent> wheelWriter, Res<MouseContext> mouseCtx) => {
            if (mouseCtx.Value.NewState.LeftButton != mouseCtx.Value.OldState.LeftButton)
                writer.Enqueue(new() { Action = mouseCtx.Value.NewState.LeftButton, Button = Input.MouseButtonType.Left, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
            if (mouseCtx.Value.NewState.RightButton != mouseCtx.Value.OldState.RightButton)
                writer.Enqueue(new() { Action = mouseCtx.Value.NewState.RightButton, Button = Input.MouseButtonType.Right, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
            if (mouseCtx.Value.NewState.MiddleButton != mouseCtx.Value.OldState.MiddleButton)
                writer.Enqueue(new() { Action = mouseCtx.Value.NewState.MiddleButton, Button = Input.MouseButtonType.Middle, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
            if (mouseCtx.Value.NewState.XButton1 != mouseCtx.Value.OldState.XButton1)
                writer.Enqueue(new() { Action = mouseCtx.Value.NewState.XButton1, Button = Input.MouseButtonType.XButton1, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
            if (mouseCtx.Value.NewState.XButton2 != mouseCtx.Value.OldState.XButton2)
                writer.Enqueue(new() { Action = mouseCtx.Value.NewState.XButton2, Button = Input.MouseButtonType.XButton2, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });

            if (mouseCtx.Value.NewState.ScrollWheelValue != mouseCtx.Value.OldState.ScrollWheelValue)
                // FNA multiplies for 120 for some reason
                wheelWriter.Enqueue(new() { Value = (mouseCtx.Value.OldState.ScrollWheelValue - mouseCtx.Value.NewState.ScrollWheelValue) / 120 });
        }, Stages.FrameEnd).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem((Res<MouseContext> mouseCtx) =>
        {
            mouseCtx.Value.OldState = mouseCtx.Value.NewState;
            mouseCtx.Value.NewState = Mouse.GetState();
        }, Stages.FrameEnd).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem((Res<KeyboardContext> keyboardCtx) =>
        {
            keyboardCtx.Value.OldState = keyboardCtx.Value.NewState;
            keyboardCtx.Value.NewState = Keyboard.GetState();
        }, Stages.FrameEnd).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem((EventReader<KeyEvent> reader) => {
            foreach (var ev in reader)
                Console.WriteLine("key {0} is {1}", ev.Key, ev.Action switch
                {
                    0 => "up",
                    1 => "down",
                    2 => "pressed",
                    _ => "unknown"
                });
        });

        scheduler.AddSystem((EventReader<MouseEvent> reader) => {
            foreach (var ev in reader)
                Console.WriteLine("mouse button {0} is {1} at {2},{3}", ev.Button, ev.Action switch
                {
                    ButtonState.Pressed => "pressed",
                    ButtonState.Released => "released",
                    _ => "unknown"
                }, ev.X, ev.Y);
        }).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem((EventReader<WheelEvent> reader) => {
            foreach (var ev in reader)
                Console.WriteLine("wheel value {0}", ev.Value);
        }).RunIf((Res<UoGame> game) => game.Value.IsActive);
    }

    private struct KeyEvent
    {
        public byte Action;
        public Keys Key;
    }

    private struct MouseEvent
    {
        public ButtonState Action;
        public Input.MouseButtonType Button;
        public int X, Y;
    }

    private struct WheelEvent
    {
        public int Value;
    }

    private sealed class UnsafeFNAAccessor
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "RunApplication")]
        public static extern ref bool GetSetRunApplication(Microsoft.Xna.Framework.Game instance);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "BeforeLoop")]
        public static extern void BeforeLoop(Microsoft.Xna.Framework.Game instance);
    }
}

internal sealed class UoGame : Microsoft.Xna.Framework.Game
{
    public UoGame(bool mouseVisible, bool allowWindowResizing, bool vSync)
    {
        GraphicManager = new GraphicsDeviceManager(this)
        {
            SynchronizeWithVerticalRetrace = vSync,
            PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8
        };

        GraphicManager.PreparingDeviceSettings += (sender, e) =>
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                RenderTargetUsage.DiscardContents;
        };

        IsFixedTimeStep = false;
        IsMouseVisible = mouseVisible;
        Window.AllowUserResizing = allowWindowResizing;
    }

    public GraphicsDeviceManager GraphicManager { get; }
    public GameTime GameTime { get; private set; }

    protected override void Update(GameTime gameTime)
    {
        GameTime = gameTime;
        // I don't want to update things here, but on ecs systems instead
    }

    protected override void Draw(GameTime gameTime)
    {
        // I don't want to render things here, but on ecs systems instead
    }
}
