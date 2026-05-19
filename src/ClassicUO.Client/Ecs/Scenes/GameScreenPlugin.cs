using System;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct GameScreenPlugin : IPlugin
{
    const int BORDER_SIZE = 10;

    public void Build(App app)
    {
        var setupFn = Setup;
        var cleanupFn = Cleanup;
        var updateEntitiesCountFn = UpdateEntitiesCount;
        var updateSelectedEntityFn = UpdateSelectedEntity;
        var dragWindowFn = DragWindow;
        var resizeWindowFn = ResizeWindow;
        var syncWindowToCameraFn = SyncWindowToCamera;
        var bindRenderTargetFn = BindRenderTarget;

        app
            .AddResource<RenderTarget2D>(null!)
            .AddResource(new DragGate())

            .AddSystem(setupFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            .AddSystem(cleanupFn)
            .OnExit(GameState.GameScreen)
            .Build()

            // Drag/resize before the rest of Update so the layout sees fresh sizes.
            .AddSystem(dragWindowFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(resizeWindowFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            // Push current Node positions/sizes into Camera.Bounds so the world
            // renderer matches the on-screen game window.
            .AddSystem(syncWindowToCameraFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            // (Re)create the RenderTarget2D when the backbuffer changes, then
            // bind it to the game-window entity's UiImage so the GUI renderer
            // draws the latest frame each tick.
            .AddSystem(bindRenderTargetFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(updateEntitiesCountFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<Time> time, Local<float> lastAccess) =>
            {
                if (time.Value.Total > lastAccess.Value)
                {
                    lastAccess.Value = time.Value.Total + 250f;
                    return true;
                }
                return false;
            })
            .Build()

            .AddSystem(updateSelectedEntityFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();
    }

    private static void Setup(Commands commands)
    {
        var bgRoot = new ClayColor(18, 18, 18, 0);          // transparent
        var bgBorder = new ClayColor(38, 38, 38, 255);
        var bgResize = new ClayColor(255, 0, 0, 255);
        var bgWindow = new ClayColor(255, 255, 255, 255);
        var bgMenu   = new ClayColor(0, 0, 0, 255);
        var bgButton = new ClayColor(0, 0, 127, 255);

        // Root: full-screen column with padding.
        var root = commands.Spawn()
            .Insert<GameScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Percent(100),
                Height = Val.Percent(100),
                Padding = UiRect.All(4),
            })
            .Insert(new BackgroundColor(bgRoot));

        // Game window border (the draggable handle). Floating/absolute so we
        // can mutate Left/Top each frame.
        var gameWindowBorder = commands.Spawn()
            .Insert<GameScene>()
            .Insert<GameWindowBorderUI>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0),
                Top = Val.Px(0),
                Width = Val.Px(BORDER_SIZE),
                Height = Val.Px(BORDER_SIZE),
            })
            .Insert(new BackgroundColor(bgBorder))
            .Insert(Interaction.None)
            .Insert(new Button());

        // Resize handle (bottom-right corner). Same absolute story.
        var gameWindowBorderResize = commands.Spawn()
            .Insert<GameScene>()
            .Insert<GameWindowBorderResizeUI>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0),
                Top = Val.Px(0),
                Width = Val.Px(BORDER_SIZE),
                Height = Val.Px(BORDER_SIZE),
            })
            .Insert(new BackgroundColor(bgResize))
            .Insert(Interaction.None)
            .Insert(new Button());

        // Game window itself — holds the world render target as a UiImage.
        var gameWindow = commands.Spawn()
            .Insert<GameScene>()
            .Insert<GameWindowUI>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0),
                Top = Val.Px(0),
                Width = Val.Px(BORDER_SIZE),
                Height = Val.Px(BORDER_SIZE),
            })
            .Insert(new BackgroundColor(bgWindow))
            // BindRenderTarget will overwrite ImageData with the real texture
            // once the RenderTarget2D resource has been created.
            .Insert(new UiImage { ImageData = null, Tint = ClayColor.White });

        // Menu bar (row, grow width, fit height, right-aligned).
        var menuBar = commands.Spawn()
            .Insert<GameScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.End,
                AlignItems = AlignItems.Center,
                Width = Val.Percent(100),
                Height = Val.Auto,
                Padding = UiRect.All(4),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(bgMenu));

        // Logout button.
        var logoutBtn = commands.Spawn()
            .Insert<GameScene>()
            .Insert(ButtonAction.Logout)
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Width = Val.Auto,
                Height = Val.Percent(100),
                Padding = UiRect.All(4),
            })
            .Insert(new BackgroundColor(bgButton))
            .Insert(Interaction.None)
            .Insert(new Button())
            .Observe((On<UiClick> _, ResMut<NextState<GameState>> state) =>
            {
                Console.WriteLine("Logout button pressed");
                state.Value.Set(GameState.LoginScreen);
            });

        // Logout label (child of the logout button so it inherits position).
        var logoutLabel = commands.Spawn()
            .Insert<GameScene>()
            .Insert(new Node
            {
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text("Logout"))
            .Insert(new TextFont { FontId = 0, Size = 18 })
            .Insert(new TextColor(ClayColor.White));

        // Entity-count display.
        var totalBtn = commands.Spawn()
            .Insert<GameScene>()
            .Insert<TotalEntitiesMenu>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Width = Val.Auto,
                Height = Val.Percent(100),
                Padding = UiRect.All(4),
            })
            .Insert(new BackgroundColor(bgButton));

        var totalLabel = commands.Spawn()
            .Insert<GameScene>()
            .Insert<TotalEntitiesText>()
            .Insert(new Node
            {
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text("Total entities: 0"))
            .Insert(new TextFont { FontId = 0, Size = 18 })
            .Insert(new TextColor(ClayColor.White));

        // Selected entity overlay — semi-transparent panel, top-right.
        var bgOverlay = new ClayColor(0, 0, 0, 160);
        var selectionOverlay = commands.Spawn()
            .Insert<GameScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Top = Val.Px(48),
                Right = Val.Px(8),
                Width = Val.Auto,
                Height = Val.Auto,
                Padding = UiRect.All(8),
            })
            .Insert(new BackgroundColor(bgOverlay));

        var selectionText = commands.Spawn()
            .Insert<GameScene>()
            .Insert<SelectedEntityText>()
            .Insert(new Node
            {
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text("No selection"))
            .Insert(new TextFont { FontId = 0, Size = 16 })
            .Insert(new TextColor(ClayColor.White));

        selectionOverlay.AddChild(selectionText);

        logoutBtn.AddChild(logoutLabel);
        totalBtn.AddChild(totalLabel);

        menuBar.AddChild(logoutBtn);
        menuBar.AddChild(totalBtn);

        root.AddChild(gameWindowBorder);
        root.AddChild(gameWindowBorderResize);
        root.AddChild(gameWindow);
        root.AddChild(menuBar);
        root.AddChild(selectionOverlay);
    }

    // Anchor at press; derive bounds from absolute mouse delta. Avoids
    // accumulating per-frame deltas (sub-pixel truncation, clamp-eaten
    // offsets, initial-frame jump that includes pre-press movement).
    private struct DragAnchor
    {
        public bool Active;
        public Vector2 Mouse;
        public int X, Y, W, H;
    }

    // Cross-system gate. Whichever drag system latches first owns the gesture
    // until the mouse is released. Prevents the border from hijacking a resize
    // mid-drag (the cursor often slides off the handle onto the border).
    private enum ActiveDrag { None, Move, Resize }
    private sealed class DragGate { public ActiveDrag Mode; }

    private static void DragWindow(
        Res<MouseContext> mouseCtx,
        Res<Camera> camera,
        Res<DragGate> gate,
        Local<DragAnchor> anchor,
        Single<Data<Interaction>, Filter<With<GameWindowBorderUI>, With<GameScene>>> queryBorder
    )
    {
        if (!mouseCtx.Value.IsPressed(MouseButtonType.Left))
        {
            anchor.Value.Active = false;
            if (gate.Value.Mode == ActiveDrag.Move) gate.Value.Mode = ActiveDrag.None;
            return;
        }

        if (!anchor.Value.Active)
        {
            if (gate.Value.Mode != ActiveDrag.None) return; // someone else owns the gesture
            if (!queryBorder.TryGet(out var data))
                return;
            (_, var interaction) = data;
            if (interaction.Ref != Interaction.Pressed)
                return;
            anchor.Value = new DragAnchor
            {
                Active = true,
                Mouse = mouseCtx.Value.Position,
                X = camera.Value.Bounds.X,
                Y = camera.Value.Bounds.Y,
            };
            gate.Value.Mode = ActiveDrag.Move;
        }

        var delta = mouseCtx.Value.Position - anchor.Value.Mouse;
        camera.Value.Bounds.X = anchor.Value.X + (int)delta.X;
        camera.Value.Bounds.Y = anchor.Value.Y + (int)delta.Y;
    }

    private static void ResizeWindow(
        Res<MouseContext> mouseCtx,
        Res<Camera> camera,
        Res<DragGate> gate,
        Local<DragAnchor> anchor,
        Single<Data<Interaction>, Filter<With<GameWindowBorderResizeUI>, With<GameScene>>> queryHandle
    )
    {
        if (!mouseCtx.Value.IsPressed(MouseButtonType.Left))
        {
            anchor.Value.Active = false;
            if (gate.Value.Mode == ActiveDrag.Resize) gate.Value.Mode = ActiveDrag.None;
            return;
        }

        if (!anchor.Value.Active)
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            if (!queryHandle.TryGet(out var data))
                return;
            (_, var interaction) = data;
            if (interaction.Ref != Interaction.Pressed)
                return;
            anchor.Value = new DragAnchor
            {
                Active = true,
                Mouse = mouseCtx.Value.Position,
                W = camera.Value.Bounds.Width,
                H = camera.Value.Bounds.Height,
            };
            gate.Value.Mode = ActiveDrag.Resize;
        }

        var delta = mouseCtx.Value.Position - anchor.Value.Mouse;
        var newW = anchor.Value.W + (int)delta.X;
        var newH = anchor.Value.H + (int)delta.Y;
        if (newW < 300) newW = 300;
        if (newH < 300) newH = 300;
        camera.Value.Bounds.Width = newW;
        camera.Value.Bounds.Height = newH;
    }

    // Pull camera bounds into the three floating UI elements so what you see
    // on screen matches where the world renderer is drawing.
    private static void SyncWindowToCamera(
        Res<Camera> camera,
        Single<Data<Node>, Filter<With<GameWindowBorderUI>, With<GameScene>>> queryBorder,
        Single<Data<Node>, Filter<With<GameWindowBorderResizeUI>, With<GameScene>>> queryResize,
        Single<Data<Node>, Filter<With<GameWindowUI>, With<GameScene>>> queryWindow
    )
    {
        var b = camera.Value.Bounds;

        if (queryBorder.TryGet(out var borderData))
        {
            (_, var node) = borderData;
            node.Ref.Left = Val.Px(b.X - BORDER_SIZE * 0.5f);
            node.Ref.Top = Val.Px(b.Y - BORDER_SIZE * 0.5f);
            node.Ref.Width = Val.Px(b.Width + BORDER_SIZE);
            node.Ref.Height = Val.Px(b.Height + BORDER_SIZE);
        }

        if (queryResize.TryGet(out var resizeData))
        {
            (_, var node) = resizeData;
            node.Ref.Left = Val.Px(b.X + b.Width);
            node.Ref.Top = Val.Px(b.Y + b.Height);
            node.Ref.Width = Val.Px(BORDER_SIZE);
            node.Ref.Height = Val.Px(BORDER_SIZE);
        }

        if (queryWindow.TryGet(out var winData))
        {
            (_, var node) = winData;
            node.Ref.Left = Val.Px(b.X);
            node.Ref.Top = Val.Px(b.Y);
            node.Ref.Width = Val.Px(b.Width);
            node.Ref.Height = Val.Px(b.Height);
        }
    }

    // Allocate / reallocate the RenderTarget2D and rebind it to the game-window
    // entity's UiImage. Mirrors the old AdjustCameraAndBounds tail.
    private static void BindRenderTarget(
        ResMut<RenderTarget2D> renderTarget,
        Res<UltimaBatcher2D> batch,
        Single<Data<UiImage>, Filter<With<GameWindowUI>, With<GameScene>>> queryWindow
    )
    {
        var device = batch.Value.GraphicsDevice;
        var pp = device.PresentationParameters;

        if (renderTarget.Value == null || renderTarget.Value.IsDisposed ||
            renderTarget.Value.Width != pp.BackBufferWidth ||
            renderTarget.Value.Height != pp.BackBufferHeight)
        {
            renderTarget.Value?.Dispose();
            renderTarget.Value = new RenderTarget2D(
                device,
                pp.BackBufferWidth,
                pp.BackBufferHeight,
                false,
                SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        }

        if (queryWindow.TryGet(out var data))
        {
            (_, var img) = data;
            img.Ref.ImageData = renderTarget.Value;
            img.Ref.SourceSize = new System.Numerics.Vector2(renderTarget.Value.Width, renderTarget.Value.Height);
            img.Ref.Tint = ClayColor.White;
        }
    }

    private static void UpdateEntitiesCount(
        Query<Data<Text>, With<TotalEntitiesText>> query,
        Query<Empty, With<IsTile>> queryTiles,
        Query<Empty, With<IsStatic>> queryStatics
    )
    {
        var total = 0; // world.EntityCount;
        var countTiles = queryTiles.Count();
        var countStatics = queryStatics.Count();
        foreach (var (_, text) in query)
        {
            text.Ref.Value = $"Total entities: {total} - tiles: {countTiles} - statics: {countStatics}";
        }
    }

    private static void UpdateSelectedEntity(
        Res<SelectedEntity> selected,
        Query<Data<Text>, With<SelectedEntityText>> queryText,
        Query<Data<WorldPosition, Graphic>, Filter<Optional<WorldPosition>, Optional<Graphic>>> queryInfo,
        Query<Data<NetworkSerial>, Filter<Optional<NetworkSerial>>> querySerial
    )
    {
        var ent = selected.Value.Entity;
        string label;
        if (ent == 0)
        {
            label = "No selection";
        }
        else
        {
            label = $"Entity: 0x{ent:X}";
            if (queryInfo.Contains(ent))
            {
                var (_, pos, gfx) = queryInfo.Get(ent);
                if (gfx.IsValid())
                    label += $"\nGraphic: 0x{gfx.Ref.Value:X4}";
                if (pos.IsValid())
                    label += $"\nPos: {pos.Ref.X}, {pos.Ref.Y}, {pos.Ref.Z}";
            }
            if (querySerial.Contains(ent))
            {
                var (_, serial) = querySerial.Get(ent);
                if (serial.IsValid())
                    label += $"\nSerial: 0x{serial.Ref.Value:X8}";
            }
        }

        foreach (var (_, text) in queryText)
            text.Ref.Value = label;
    }

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<GameScene>>> query)
    {
        Console.WriteLine("[GameScreen] cleanup start");
        foreach (var (ent, _) in query)
            commands.Entity(ent.Ref).Despawn();
        Console.WriteLine("[GameScreen] cleanup done");
    }

    private struct GameScene;
    private struct GameWindowUI;
    private struct GameWindowBorderUI;
    private struct GameWindowBorderResizeUI;
    private struct TotalEntitiesMenu;
    private struct TotalEntitiesText;
    private struct SelectedEntityText;

    private enum ButtonAction
    {
        Logout
    }
}
