using System;
using ClassicUO.Ecs.Logging;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Extensions.Logging;
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
        var inventoryClickFn = HandleInventoryClick;

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
            .Build()

            .AddSystem(inventoryClickFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<MouseContext> m) => m.Value.IsReleased(MouseButtonType.Left))
            .Build();
    }

    // Edge-trigger on left-button release while the inventory button is in the
    // Pressed interaction state. Same release-on-press idiom Bevy.UI uses for
    // its built-in Button widget.
    private static void HandleInventoryClick(
        Res<NetClient> network,
        Query<Data<Interaction>, With<InventoryButton>> buttonQuery,
        Single<Data<EquipmentSlots>, With<Player>> playerSlots,
        Query<Data<NetworkSerial>> serialQuery)
    {
        bool pressed = false;
        foreach (var (_, interaction) in buttonQuery)
        {
            if (interaction.Ref == Interaction.Pressed) { pressed = true; break; }
        }
        if (!pressed) return;

        if (!playerSlots.TryGet(out var data)) return;
        (_, var slots) = data;
        ref var s = ref slots.Ref;
        var backpackEnt = s[Layer.Backpack];
        if (backpackEnt == 0 || !serialQuery.TryGet(backpackEnt, out var serialRow)) return;
        var (_, serial) = serialRow;
        network.Value.Send_DoubleClick(serial.Ref.Value);
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

        // Menu bar (row, grow width, fixed height, right-aligned). Fixed
        // height avoids Auto-height + Percent(100) children producing zero or
        // jittery rows.
        const int MENU_BAR_HEIGHT = 32;
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
                Height = Val.Px(MENU_BAR_HEIGHT),
                Padding = UiRect.Symmetric(4, 2),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(bgMenu));

        // Inventory button — tagged with InventoryButton; a Stage.Update
        // system polls it and sends Send_DoubleClick on the player's backpack
        // serial. Server replies with 0x24 + 0x3C which the container plugins
        // pick up. Observer path is intentionally not used here because the
        // 4-param signature trips Observe's overload resolution.
        var inventoryBtn = commands.Spawn()
            .Insert<GameScene>()
            .Insert<InventoryButton>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Width = Val.Auto,
                Height = Val.Percent(100),
                Padding = UiRect.Symmetric(8, 0),
            })
            .Insert(new BackgroundColor(bgButton))
            .Insert(Interaction.None)
            .Insert(new Button());

        var inventoryLabel = commands.Spawn()
            .Insert<GameScene>()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text("Inventory"))
            .Insert(new TextFont { FontId = 0, Size = 16 })
            .Insert(new TextColor(ClayColor.White));

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
            .Observe((On<UiClick> _, ResMut<NextState<GameState>> state, Res<ILogger> logger) =>
            {
                logger.Value.LogInformation("Logout button pressed");
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

        inventoryBtn.AddChild(inventoryLabel);
        logoutBtn.AddChild(logoutLabel);
        totalBtn.AddChild(totalLabel);

        menuBar.AddChild(inventoryBtn);
        menuBar.AddChild(logoutBtn);
        menuBar.AddChild(totalBtn);

        root.AddChild(gameWindowBorder);
        root.AddChild(gameWindowBorderResize);
        root.AddChild(gameWindow);
        if (string.Equals(System.Environment.GetEnvironmentVariable("ECS_DEBUG_HUD"), "1"))
        {
            root.AddChild(menuBar);
            root.AddChild(selectionOverlay);
        }
        else
        {
            // Despawn the dev-only entities so orphan Nodes don't get
            // rendered at root-level positions by Clay.
            menuBar.Despawn();
            logoutBtn.Despawn();
            logoutLabel.Despawn();
            totalBtn.Despawn();
            totalLabel.Despawn();
            selectionOverlay.Despawn();
            selectionText.Despawn();
        }
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
    // (DragGate / ActiveDrag are file-scoped below this class.)

    private static void DragWindow(
        Res<MouseContext> mouseCtx,
        Res<Camera> camera,
        Res<DragGate> gate,
        Res<GrabbedItem> grabbed,
        Local<DragAnchor> anchor,
        Single<Data<Interaction, ComputedNode>, Filter<With<GameWindowBorderUI>, With<GameScene>>> queryBorder,
        Query<Data<ComputedNode>, Filter<With<UiMovable>>> movableQ
    )
    {
        // `IsPressed` semantics in MouseContext: true only when BOTH this and
        // the previous frame were Pressed (steady-state held). On the press-
        // once frame the previous state is Released, so IsPressed is false
        // there — we still need to handle it for the initial latch.
        bool held = mouseCtx.Value.IsPressed(MouseButtonType.Left)
                 || mouseCtx.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held || grabbed.Value.Serial != 0)
        {
            anchor.Value.Active = false;
            if (gate.Value.Mode == ActiveDrag.Move) gate.Value.Mode = ActiveDrag.None;
            return;
        }

        // Latch on press-once via direct bbox hit-test of the cursor against
        // the border's ComputedNode. Avoids the continuous-held pattern
        // (which lets the cursor wander onto the border mid-drag and latch
        // unexpectedly) and avoids the press-once Interaction-staleness
        // problem (Interaction is rewritten in UiPostLayoutStage).
        if (mouseCtx.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            if (!queryBorder.TryGet(out var data)) return;
            (_, _, var computed) = data;
            var bb = computed.Ref;
            // UI-space cursor: the border ComputedNode + camera.Bounds (fed into
            // the GameWindow node by SyncWindowToCamera) live in layout space,
            // which the renderer upscales by UiScale. Hit-test + drag delta must
            // match, or the window runs UiScale× ahead of the cursor.
            var pos = mouseCtx.Value.Position;
            // Border bbox is the OUTER rectangle (camera ± BORDER_SIZE/2),
            // which also covers the rendered world. Drag only when the click
            // landed on the border strip itself, i.e. inside the outer bbox
            // AND outside the inner camera viewport.
            if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) return;
            if (pos.X >= bb.Position.X + bb.Size.X) return;
            if (pos.Y >= bb.Position.Y + bb.Size.Y) return;
            var inner = camera.Value.Bounds;
            if (pos.X >= inner.X && pos.X < inner.X + inner.Width
                && pos.Y >= inner.Y && pos.Y < inner.Y + inner.Height) return;

            // Floating UiMovable windows draw above the game viewport border,
            // so if any of them sits under the cursor we let WindowDragPlugin
            // claim the gesture instead. Without this the border drag latches
            // first (registered earlier in the schedule) and the container
            // gump never moves.
            if (CursorOverAnyMovable(pos, movableQ)) return;

            anchor.Value = new DragAnchor
            {
                Active = true,
                Mouse = pos,
                X = camera.Value.Bounds.X,
                Y = camera.Value.Bounds.Y,
            };
            gate.Value.Mode = ActiveDrag.Move;
        }

        if (!anchor.Value.Active) return;

        var delta = mouseCtx.Value.Position - anchor.Value.Mouse;
        camera.Value.Bounds.X = anchor.Value.X + (int)delta.X;
        camera.Value.Bounds.Y = anchor.Value.Y + (int)delta.Y;
    }

    private static bool CursorOverAnyMovable(Vector2 pos, Query<Data<ComputedNode>, Filter<With<UiMovable>>> q)
    {
        foreach (var (_, computed) in q)
        {
            var bb = computed.Ref;
            if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) continue;
            if (pos.X >= bb.Position.X + bb.Size.X) continue;
            if (pos.Y >= bb.Position.Y + bb.Size.Y) continue;
            return true;
        }
        return false;
    }

    private static void ResizeWindow(
        Res<MouseContext> mouseCtx,
        Res<Camera> camera,
        Res<DragGate> gate,
        Res<GrabbedItem> grabbed,
        Local<DragAnchor> anchor,
        Single<Data<Interaction, ComputedNode>, Filter<With<GameWindowBorderResizeUI>, With<GameScene>>> queryHandle
    )
    {
        bool held = mouseCtx.Value.IsPressed(MouseButtonType.Left)
                 || mouseCtx.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held || grabbed.Value.Serial != 0)
        {
            anchor.Value.Active = false;
            if (gate.Value.Mode == ActiveDrag.Resize) gate.Value.Mode = ActiveDrag.None;
            return;
        }

        if (mouseCtx.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            if (!queryHandle.TryGet(out var data)) return;
            (_, _, var computed) = data;
            var bb = computed.Ref;
            // UI-space cursor — same reason as DragWindow: the resize handle node
            // and camera W/H are layout-space, upscaled by UiScale at render.
            var pos = mouseCtx.Value.Position;
            if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) return;
            if (pos.X >= bb.Position.X + bb.Size.X) return;
            if (pos.Y >= bb.Position.Y + bb.Size.Y) return;

            anchor.Value = new DragAnchor
            {
                Active = true,
                Mouse = mouseCtx.Value.Position,
                W = camera.Value.Bounds.Width,
                H = camera.Value.Bounds.Height,
            };
            gate.Value.Mode = ActiveDrag.Resize;
        }

        if (!anchor.Value.Active) return;

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

    // Allocate / reallocate the world RenderTarget2D at LOGICAL viewport
    // size (camera.Bounds dim, NOT scaled by DpiScale). The Clay panel
    // then samples this RT into its DPI-scaled display rect, producing
    // main's signature point-upscaled pixel-art look on hi-DPI displays
    // rather than a crisp 1:1 render.
    private static void BindRenderTarget(
        ResMut<RenderTarget2D> renderTarget,
        Res<UltimaBatcher2D> batch,
        Res<Camera> camera,
        Single<Data<UiImage>, Filter<With<GameWindowUI>, With<GameScene>>> queryWindow
    )
    {
        var device = batch.Value.GraphicsDevice;

        var w = System.Math.Max(1, camera.Value.Bounds.Width);
        var h = System.Math.Max(1, camera.Value.Bounds.Height);

        if (renderTarget.Value == null || renderTarget.Value.IsDisposed ||
            renderTarget.Value.Width != w ||
            renderTarget.Value.Height != h)
        {
            renderTarget.Value?.Dispose();
            renderTarget.Value = new RenderTarget2D(
                device,
                w,
                h,
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
            if (queryInfo.TryGet(ent, out var infoRow))
            {
                var (_, pos, gfx) = infoRow;
                if (gfx.IsValid())
                    label += $"\nGraphic: 0x{gfx.Ref.Value:X4}";
                if (pos.IsValid())
                    label += $"\nPos: {pos.Ref.X}, {pos.Ref.Y}, {pos.Ref.Z}";
            }
            if (querySerial.TryGet(ent, out var serialRow))
            {
                var (_, serial) = serialRow;
                if (serial.IsValid())
                    label += $"\nSerial: 0x{serial.Ref.Value:X8}";
            }
        }

        foreach (var (_, text) in queryText)
            text.Ref.Value = label;
    }

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<GameScene>>> query,
        Res<ILogger> logger)
    {
        logger.Value.LogTrace("[GameScreen] cleanup start");
        foreach (var (ent, _) in query)
            commands.Entity(ent.Ref).Despawn();
        logger.Value.LogTrace("[GameScreen] cleanup done");
    }

    // internal (not private): GameCursorPlugin queries it to tell "cursor is over
    // the game world" (a hit on this viewport node) from "over a gump".
    internal struct GameWindowUI;
    private struct GameWindowBorderUI;
    private struct GameWindowBorderResizeUI;
    private struct TotalEntitiesMenu;
    private struct TotalEntitiesText;
    private struct SelectedEntityText;

    private enum ButtonAction
    {
        Logout,
        OpenInventory,
    }
}

// Scene root marker — promoted to namespace-level internal so the modding
// registry (same assembly) can key a WIT scene path to it.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct GameScene;

internal struct InventoryButton;

// Cross-system drag arbitration. First system to claim the gesture wins;
// others see Mode != None at press-once and skip latching until release
// clears it. Shared between GameScreenPlugin (border move/resize) and
// WindowDragPlugin (floating UiMovable windows) so a press inside a stacked
// container can't simultaneously start a container drag AND a viewport drag.
internal enum ActiveDrag { None, Move, Resize, UIWindow }
internal sealed class DragGate { public ActiveDrag Mode; }
