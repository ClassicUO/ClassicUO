using System;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Clay_cs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;
using TinyEcs.UI.Bevy;
using TinyEcs.UI;

namespace ClassicUO.Ecs;

internal readonly struct GameScreenPlugin : IPlugin
{
    const int BORDER_SIZE = 10;

    public void Build(App app)
    {
        var setupFn = Setup;
        var cleanupFn = Cleanup;
        var updateEntitiesCountFn = UpdateEntitiesCount;
        var adjustCameraAndBoundsFn = AdjustCameraAndBounds;

        app
            .AddResource<RenderTarget2D>(null)

            .AddSystem(setupFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            .AddSystem(cleanupFn)
            .OnExit(GameState.GameScreen)
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

            .AddSystem(adjustCameraAndBoundsFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf(w => w.HasResource<Camera>())
            .Build();
    }

    private static void Setup(Commands commands, Res<GumpBuilder> gumpBuilder, Res<ClayUOCommandBuffer> clay)
    {
        var root = commands.Spawn()
            .Insert<GameScene>()
            .Insert(ClayNode.Configure()
                .WidthGrow()
                .HeightGrow()
                .Column()
                .Padding(4)
                .Background(18, 18, 18, 0)
                .Build());

        var floating = new Clay_FloatingElementConfig()
        {
            attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
            clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
            zIndex = -1
        };

        var gameWindowBorder = commands.Spawn()
            .Insert(ClayNode.Configure()
                .Width(BORDER_SIZE)
                .Height(BORDER_SIZE)
                .Background(38, 38, 38, 255)
                .Floating()
                .Build())
            .Insert(new ClayInteraction())
            .Insert<GameWindowBorderUI>()
            .Insert<GameScene>();

        var gameWindowBorderResize = commands.Spawn()
            .Insert(ClayNode.Configure()
                .Width(BORDER_SIZE)
                .Height(BORDER_SIZE)
                .Background(255, 0, 0, 255)
                .Floating()
                .Build())
            .Insert(new ClayInteraction())
            .Insert<GameWindowBorderResizeUI>()
            .Insert<GameScene>();

        var gameWindow = commands.Spawn()
            .Insert(ClayNode.Configure()
                .Width(BORDER_SIZE)
                .Height(BORDER_SIZE)
                .Background(255, 255, 255, 255)
                .Floating()
                .Build())
            .Insert<GameWindowUI>()
            .Insert<GameScene>();



        var menuBar = commands.Spawn()
            .Insert<GameScene>()
            .Insert(ClayNode.Configure()
                .WidthGrow()
                .HeightFit()
                .Row()
                .Gap(4)
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Padding(4)
                .Background(0, 0, 0, 255)
                .Build());

        var menuBarItem = commands.Spawn()
            .Insert<GameScene>()
            .Insert(ButtonAction.Logout)
            .Insert(ClayNode.Configure()
                .WidthFit()
                .HeightGrow()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Padding(4)
                .Background(0, 0, 127, 255)
                .Text("Logout", 18, new Clay_Color(255, 255, 255, 255))
                .Build())
            .Observe<On<ClayPointerEvent>, Res<NextState<GameState>>>(LogoutButtonHandler);

        var menuBarItem2 = commands.Spawn()
            .Insert(ClayNode.Configure()
                .WidthFit()
                .HeightGrow()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Padding(4)
                .Background(0, 0, 127, 255)
                .Text("Total entities: 0", 18, new Clay_Color(255, 255, 255, 255))
                .Build())
            .Insert<GameScene>()
            .Insert<TotalEntitiesMenu>();

        menuBar.AddChild(menuBarItem);
        menuBar.AddChild(menuBarItem2);

        root.AddChild(gameWindowBorder);
        root.AddChild(gameWindowBorderResize);
        root.AddChild(gameWindow);

        // root.AddChild(gameWindowRoot);
        root.AddChild(menuBar);
    }


    class CameraBounds
    {
        public Rectangle Rectangle;
    }

    private static void AdjustCameraAndBounds(
        Res<Camera> camera,
        ResMut<RenderTarget2D> renderTarget,
        Res<ImageCache> imageCache,
        Res<UltimaBatcher2D> batch,
        Res<MouseContext> mouseCtx,
        Local<CameraBounds> lastSize,
        Single<Data<ClayNode, ClayInteraction>, Filter<With<GameWindowBorderUI>, With<GameScene>>> queryGameWindowBorder,
        Single<Data<ClayNode, ClayInteraction>, Filter<With<GameWindowBorderResizeUI>, With<GameScene>>> queryGameWindowBorderResize,
        Single<Data<ClayNode>, Filter<With<GameWindowUI>, With<GameScene>>> queryGameWindow
    )
    {
        (var nodeBorder, var interaction) = queryGameWindowBorder.Get();
        (var nodeBorderResize, var interactionResize) = queryGameWindowBorderResize.Get();


        if (interaction.Ref.State == ClayInteractionState.Pressed && interaction.Ref.PressedButtons.HasFlag(ClayMouseButton.Left))
        {
            camera.Value.Bounds.X += (int)mouseCtx.Value.PositionOffset.X;
            camera.Value.Bounds.Y += (int)mouseCtx.Value.PositionOffset.Y;
        }

        if (interactionResize.Ref.State == ClayInteractionState.Pressed && interactionResize.Ref.PressedButtons.HasFlag(ClayMouseButton.Left))
        {
            ref var newBounds = ref lastSize.Value.Rectangle;
            newBounds.Width += (int)mouseCtx.Value.PositionOffset.X;
            newBounds.Height += (int)mouseCtx.Value.PositionOffset.Y;

            if (newBounds.Width >= 300)
                camera.Value.Bounds.Width = newBounds.Width;
            if (newBounds.Height >= 300)
                camera.Value.Bounds.Height = newBounds.Height;
        }
        else
        {
            lastSize.Value.Rectangle = camera.Value.Bounds;
        }

        if (nodeBorderResize.Ref.Floating.HasValue)
        {
            var floating = nodeBorderResize.Ref.Floating.Value;
            floating.offset = new()
            {
                x = camera.Value.Bounds.X + camera.Value.Bounds.Width + BORDER_SIZE - nodeBorderResize.Ref.Layout.sizing.width.size.minMax.max,
                y = camera.Value.Bounds.Y + camera.Value.Bounds.Height + BORDER_SIZE - nodeBorderResize.Ref.Layout.sizing.height.size.minMax.max,
            };
            nodeBorderResize.Ref.Floating = floating;
        }

        if (nodeBorder.Ref.Floating.HasValue)
        {
            var floating = nodeBorder.Ref.Floating.Value;
            floating.offset = new()
            {
                x = camera.Value.Bounds.X - BORDER_SIZE * 0.5f,
                y = camera.Value.Bounds.Y - BORDER_SIZE * 0.5f,
            };
            nodeBorder.Ref.Floating = floating;
        }

        var layout = nodeBorder.Ref.Layout;
        layout.sizing = new()
        {
            width = Clay_SizingAxis.Fixed(camera.Value.Bounds.Width + BORDER_SIZE),
            height = Clay_SizingAxis.Fixed(camera.Value.Bounds.Height + BORDER_SIZE),
        };
        nodeBorder.Ref.Layout = layout;


        (_, var node) = queryGameWindow.Get();
        if (node.Ref.Floating.HasValue)
        {
            var floating = node.Ref.Floating.Value;
            floating.offset = new()
            {
                x = camera.Value.Bounds.X,
                y = camera.Value.Bounds.Y,
            };
            node.Ref.Floating = floating;
        }

        var nodeLayout = node.Ref.Layout;
        nodeLayout.sizing = new()
        {
            width = Clay_SizingAxis.Fixed(camera.Value.Bounds.Width),
            height = Clay_SizingAxis.Fixed(camera.Value.Bounds.Height),
        };
        node.Ref.Layout = nodeLayout;

        if (renderTarget.Value == null || renderTarget.Value.IsDisposed ||
            renderTarget.Value.Width != batch.Value.GraphicsDevice.PresentationParameters.BackBufferWidth ||
            renderTarget.Value.Height != batch.Value.GraphicsDevice.PresentationParameters.BackBufferHeight)
        {
            renderTarget.Value?.Dispose();
            renderTarget.Value = new(
                batch.Value.GraphicsDevice,
                batch.Value.GraphicsDevice.PresentationParameters.BackBufferWidth,
                batch.Value.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

            imageCache.Value[renderTarget.Value.GetHashCode()] = renderTarget.Value;
        }
        else
        {
            unsafe
            {
                var image = node.Ref.Image ?? new Clay_ImageElementConfig();
                image.imageData = (void*)renderTarget.Value.GetHashCode();
                node.Ref.Image = image;
            }
        }
    }

    private static void LogoutButtonHandler(
        On<ClayPointerEvent> trigger,
        Res<NextState<GameState>> state
    )
    {
        if (!trigger.Event.IsLeftButton || trigger.Event.EventType != ClayPointerEventType.Click)
            return;

        Console.WriteLine("Logout button pressed");
        state.Value.Set(GameState.LoginScreen);
    }

    private static void UpdateEntitiesCount(
        Commands commands,
        Query<Data<ClayNode>, With<TotalEntitiesMenu>> query,
        Query<Empty, With<IsTile>> queryTiles,
        Query<Empty, With<IsStatic>> queryStatics
    )
    {
        var total = 0; // world.EntityCount;
        var countTiles = queryTiles.Count();
        var countStatics = queryStatics.Count();
        foreach ((var entity, var node) in query)
        {
            var newText = $"Total entities: {total} - tiles: {countTiles} - statics: {countStatics}";
            // Update the text in the ClayNode
            if (node.Ref.Text.HasValue)
            {
                var textConfig = node.Ref.Text.Value.Config;
                // Create a new node with updated text
                var updatedNode = ClayNode.Configure()
                    .WidthFit()
                    .HeightGrow()
                    .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                    .Padding(4)
                    .Background(0, 0, 127, 255)
                    .Text(newText, textConfig.fontSize, textConfig.textColor)
                    .Build();
                commands.Entity(entity.Ref).Insert(updatedNode);
            }
        }
    }

    private static void Cleanup(Commands commands, Query<Data<ClayNode>, Filter<Without<Parent>, With<GameScene>>> query)
    {
        Console.WriteLine("[GameScreen] cleanup start");
        foreach ((var ent, _) in query)
            commands.Entity(ent.Ref).Despawn();
        Console.WriteLine("[GameScreen] cleanup done");
    }

    private struct GameScene;
    private struct GameWindowUI;
    private struct GameWindowBorderUI;
    private struct GameWindowBorderResizeUI;
    private struct TotalEntitiesMenu;

    private enum ButtonAction
    {
        Logout
    }
}
