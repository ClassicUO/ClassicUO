using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;


internal abstract class InputContext<T>
{
    protected Microsoft.Xna.Framework.Game _game;

    protected InputContext(Microsoft.Xna.Framework.Game game) => _game = game;

    public abstract bool IsPressed(T input);
    public abstract bool IsPressedOnce(T input);
    public abstract bool IsReleased(T input);

    public virtual void Update(float deltaTime) { }
}

internal sealed class MouseContext : InputContext<MouseButtonType>
{
    private static float DCLICK_DELTA = 300;

    private MouseState _oldState, _newState;
    private float _lastClickTime, _currentTime;
    private MouseButtonType? _lastClickButton;

    internal MouseContext(Microsoft.Xna.Framework.Game game) : base (game) { }


    public Vector2 Position => new(_newState.X, _newState.Y);
    public Vector2 PositionOffset => new (_newState.X - _oldState.X, _newState.Y - _oldState.Y);


    public override bool IsPressed(MouseButtonType input) => VerifyCondition(input, ButtonState.Pressed, ButtonState.Pressed);

    public override bool IsPressedOnce(MouseButtonType input) => VerifyCondition(input, ButtonState.Pressed, ButtonState.Released);

    public override bool IsReleased(MouseButtonType input) => VerifyCondition(input, ButtonState.Released, ButtonState.Released);

    public bool IsPressedDouble(MouseButtonType input)
    {
        if (IsPressedOnce(input))
        {
            if (_lastClickButton == input && _lastClickTime + DCLICK_DELTA > _currentTime)
            {
                _lastClickButton = null;
                return true;
            }

            _lastClickButton = input;
            _lastClickTime = _currentTime;
        }

        return false;
    }

    public override void Update(float deltaTime)
    {
        _oldState = _newState;
        _newState = Microsoft.Xna.Framework.Input.Mouse.GetState();
        _currentTime = deltaTime;

        base.Update(deltaTime);
    }

    private bool VerifyCondition(MouseButtonType button, ButtonState stateNew, ButtonState stateOld)
        => _game.IsActive && button switch {
                MouseButtonType.Left => _newState.LeftButton == stateNew && _oldState.LeftButton == stateOld,
                MouseButtonType.Middle => _newState.MiddleButton == stateNew && _oldState.MiddleButton == stateOld,
                MouseButtonType.Right => _newState.RightButton == stateNew && _oldState.RightButton == stateOld,
                MouseButtonType.XButton1 => _newState.XButton1 == stateNew&& _oldState.XButton1 == stateOld,
                MouseButtonType.XButton2 => _newState.XButton2 == stateNew && _oldState.XButton2 == stateOld,
                _ => false
            };
}

internal sealed class KeyboardContext : InputContext<Keys>
{
    private KeyboardState _oldState, _newState;

    internal KeyboardContext(Microsoft.Xna.Framework.Game game) : base (game) { }


    public override bool IsPressed(Keys input) => _game.IsActive && _newState.IsKeyDown(input) && _oldState.IsKeyDown(input);

    public override bool IsPressedOnce(Keys input) => _game.IsActive && _newState.IsKeyDown(input) && _oldState.IsKeyUp(input);

    public override bool IsReleased(Keys input) => _game.IsActive && _newState.IsKeyUp(input) && _oldState.IsKeyDown(input);

    public Keys[] GetPressedKeys() => _newState.GetPressedKeys();

    public override void Update(float deltaTime)
    {
        _oldState = _newState;
        _newState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        base.Update(deltaTime);
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

internal readonly struct FnaPlugin : IPlugin
{
    public bool WindowResizable { get; init; }
    public bool MouseVisible { get; init; }
    public bool VSync { get; init; }

    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new UoGame(MouseVisible, WindowResizable, VSync));
        scheduler.AddSystemParam(new Time());
        scheduler.AddEvent<KeyEvent>();
        scheduler.AddEvent<MouseEvent>();
        scheduler.AddEvent<WheelEvent>();

        scheduler.AddSystem(static (Res<UoGame> game, SchedulerState schedState) => {
            UnsafeFNAAccessor.BeforeLoop(game.Value);
            game.Value.RunOneFrame();
            schedState.AddResource(game.Value.GraphicsDevice);
            schedState.AddResource(new KeyboardContext(game));
            schedState.AddResource(new MouseContext(game));
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

        scheduler.AddSystem(() => Environment.Exit(0), Stages.AfterUpdate, ThreadingMode.Single)
                .RunIf(static (Res<UoGame> game) => !UnsafeFNAAccessor.GetSetRunApplication(game.Value));

        // scheduler.AddSystem((EventWriter<KeyEvent> writer, Res<KeyboardContext> keyboardCtx) => {
        //     foreach (var key in keyboardCtx.Value.OldState.GetPressedKeys())
        //         if (keyboardCtx.Value.NewState.IsKeyUp(key)) // [pressed] -> [released]
        //             writer.Enqueue(new() { Action = 0, Key = key });

        //     foreach (var key in keyboardCtx.Value.NewState.GetPressedKeys())
        //         if (keyboardCtx.Value.OldState.IsKeyUp(key)) // [released] -> [pressed]
        //             writer.Enqueue(new() { Action = 1, Key = key });
        //         else if (keyboardCtx.Value.OldState.IsKeyDown(key))
        //             writer.Enqueue(new() { Action = 2, Key = key });
        // }, Stages.FrameEnd).RunIf((Res<UoGame> game) => game.Value.IsActive);

        // scheduler.AddSystem((EventWriter<MouseEvent> writer, EventWriter<WheelEvent> wheelWriter, Res<MouseContext> mouseCtx) => {
        //     if (mouseCtx.Value.NewState.LeftButton != mouseCtx.Value.OldState.LeftButton)
        //         writer.Enqueue(new() { Action = mouseCtx.Value.NewState.LeftButton, Button = Input.MouseButtonType.Left, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
        //     if (mouseCtx.Value.NewState.RightButton != mouseCtx.Value.OldState.RightButton)
        //         writer.Enqueue(new() { Action = mouseCtx.Value.NewState.RightButton, Button = Input.MouseButtonType.Right, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
        //     if (mouseCtx.Value.NewState.MiddleButton != mouseCtx.Value.OldState.MiddleButton)
        //         writer.Enqueue(new() { Action = mouseCtx.Value.NewState.MiddleButton, Button = Input.MouseButtonType.Middle, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
        //     if (mouseCtx.Value.NewState.XButton1 != mouseCtx.Value.OldState.XButton1)
        //         writer.Enqueue(new() { Action = mouseCtx.Value.NewState.XButton1, Button = Input.MouseButtonType.XButton1, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });
        //     if (mouseCtx.Value.NewState.XButton2 != mouseCtx.Value.OldState.XButton2)
        //         writer.Enqueue(new() { Action = mouseCtx.Value.NewState.XButton2, Button = Input.MouseButtonType.XButton2, X = mouseCtx.Value.NewState.X, Y = mouseCtx.Value.NewState.Y });

        //     if (mouseCtx.Value.NewState.ScrollWheelValue != mouseCtx.Value.OldState.ScrollWheelValue)
        //         // FNA multiplies for 120 for some reason
        //         wheelWriter.Enqueue(new() { Value = (mouseCtx.Value.OldState.ScrollWheelValue - mouseCtx.Value.NewState.ScrollWheelValue) / 120 });
        // }, Stages.FrameEnd).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, Time time) =>
        {
            mouseCtx.Value.Update(time.Total);
            keyboardCtx.Value.Update(time.Total);
        }, Stages.FrameEnd);

        // scheduler.AddSystem((Res<KeyboardContext> keyboardCtx) =>
        // {
        //     keyboardCtx.Value.OldState = keyboardCtx.Value.NewState;
        //     keyboardCtx.Value.NewState = Keyboard.GetState();
        // }, Stages.FrameEnd).RunIf((Res<UoGame> game) => game.Value.IsActive);

        // scheduler.AddSystem((EventReader<KeyEvent> reader) => {
        //     foreach (var ev in reader)
        //         Console.WriteLine("key {0} is {1}", ev.Key, ev.Action switch
        //         {
        //             0 => "up",
        //             1 => "down",
        //             2 => "pressed",
        //             _ => "unknown"
        //         });
        // });

        // scheduler.AddSystem((EventReader<MouseEvent> reader) => {
        //     foreach (var ev in reader)
        //         Console.WriteLine("mouse button {0} is {1} at {2},{3}", ev.Button, ev.Action switch
        //         {
        //             ButtonState.Pressed => "pressed",
        //             ButtonState.Released => "released",
        //             _ => "unknown"
        //         }, ev.X, ev.Y);
        // }).RunIf((Res<UoGame> game) => game.Value.IsActive);

        // scheduler.AddSystem((EventReader<WheelEvent> reader) => {
        //     foreach (var ev in reader)
        //         Console.WriteLine("wheel value {0}", ev.Value);
        // }).RunIf((Res<UoGame> game) => game.Value.IsActive);
    }

    private struct KeyEvent
    {
        public byte Action;
        public Keys Key;
    }

    private struct MouseEvent
    {
        public ButtonState Action;
        public MouseButtonType Button;
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
