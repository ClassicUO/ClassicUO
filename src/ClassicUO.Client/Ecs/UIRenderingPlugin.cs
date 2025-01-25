using System.Runtime.CompilerServices;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;


internal struct UIRenderingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        // drawing
        scheduler.AddSystem((
            Res<Renderer.UltimaBatcher2D> batch,
            Res<AssetsServer> assets,
            Local<Texture2D> dumbTexture,
            Query<
                Data<GuiBounds, GuiInputState, ZIndex, GuiUOInteractionWidget>,
                Filter<With<Gui>, Optional<GuiUOInteractionWidget>>> query) =>
        {
            var b = batch.Value;

            // TODO: remove this
            if (dumbTexture.Value == null)
            {
                dumbTexture.Value = new Texture2D(b.GraphicsDevice, 1, 1);
                dumbTexture.Value.SetData([Color.White]);
            }

            b.Begin(null, Matrix.Identity);

            foreach ((var bounds, var inputState, var zIndex, var uoWidget) in query)
            {
                var hue = inputState.Ref switch {
                    GuiInputState.Over => new Vector3(0x22, 1, 1f),
                    GuiInputState.Pressed => new Vector3(0x33, 1, 1f),
                    _ => Vector3.UnitZ
                };

                if (Unsafe.IsNullRef(ref uoWidget.Ref))
                {
                    b.DrawRectangle
                    (
                        dumbTexture,
                        bounds.Ref.Value.X,
                        bounds.Ref.Value.Y,
                        bounds.Ref.Value.Width,
                        bounds.Ref.Value.Height,
                        hue,
                        zIndex.Ref.Value
                    );
                }
                else
                {
                    var graphic = inputState.Ref switch {
                        GuiInputState.Over => uoWidget.Ref.Over,
                        GuiInputState.Pressed => uoWidget.Ref.Pressed,
                        _ => uoWidget.Ref.Normal
                    };

                    ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(graphic);

                    if (gumpInfo.Texture != null)
                    {
                        bounds.Ref.Value.Width = gumpInfo.UV.Width;
                        bounds.Ref.Value.Height = gumpInfo.UV.Height;

                        b.Draw
                        (
                            gumpInfo.Texture,
                            new Vector2(bounds.Ref.Value.X, bounds.Ref.Value.Y),
                            gumpInfo.UV,
                            Vector3.UnitZ
                        );
                    }
                }
            }

            b.End();

        }, Stages.AfterUpdate, ThreadingMode.Single);


        // input
        scheduler.AddSystem((
            World world,
            Query<Data<GuiBounds, ZIndex, GuiInputState, Children>, Filter<With<Gui>, Optional<Children>>> query,
            // Query<Data<Bounds>, Filter<With<Gui>, Without<Parent>>> queryChildren,
            Res<MouseContext> mouseCtx,
            Local<EntityView?> selected) =>
        {
            var mousePos = mouseCtx.Value.Position;
            var mousePosOff = mouseCtx.Value.PositionOffset;
            var length = mousePosOff.Length();

            var isDragging = mouseCtx.Value.IsPressed(Input.MouseButtonType.Left);
            var isReleased = mouseCtx.Value.IsReleased(Input.MouseButtonType.Left);

            if (isReleased && selected.Value.HasValue)
            {
                selected.Value = null;
            }

            var newState = GuiInputState.None;
            foreach ((var ent, var bounds, var zIndex, var inputState, var children) in query)
            {
                if (selected.Value.HasValue)
                {
                    if (selected.Value.Value == ent.Ref.ID && isDragging)
                    {
                        newState = GuiInputState.Pressed;

                        if (length > 0)
                        {
                            bounds.Ref.Value.X += (int)mousePosOff.X;
                            bounds.Ref.Value.Y += (int)mousePosOff.Y;

                            moveChildren(world, ref children.Ref, in mousePosOff);

                            static void moveChildren(World world, ref Children children, ref readonly Vector2 offset)
                            {
                                if (Unsafe.IsNullRef(ref children))
                                    return;

                                foreach (var child in children)
                                {
                                    ref var childBounds = ref world.Get<GuiBounds>(child);
                                    childBounds.Value.X += (int)offset.X;
                                    childBounds.Value.Y += (int)offset.Y;

                                    moveChildren(world, ref world.Get<Children>(child), in offset);
                                }
                            }
                        }
                    }
                }
                else if (bounds.Ref.Value.Contains((int)mousePos.X, (int)mousePos.Y))
                {
                    if (!isDragging)
                    {
                        selected.Value = ent.Ref;
                        newState = GuiInputState.Over;
                    }
                }

                inputState.Ref = GuiInputState.None;
            }

            if (selected.Value.HasValue)
            {
                selected.Value.Value.Set(newState);
            }
        }, Stages.Update, ThreadingMode.Single);

        scheduler.AddSystem((TinyEcs.World world) =>
        {
            static EntityView basicWidget(World ecs, Rectangle? bounds = null)
                => ecs.Entity()
                    .Add<Gui>()
                    .Set(new ZIndex())
                    .Set(GuiInputState.None)
                    .Set(new GuiBounds() { Value = bounds ?? Rectangle.Empty});

            var child3 = basicWidget(world, new() { X = 100, Y = 100, Width = 120, Height = 50 });

            var root = basicWidget(world, new () { X = 100, Y = 200, Width = 120, Height = 90 });

            var child0 = basicWidget(world)
                .Set(new GuiUOInteractionWidget() { Normal = 0x1589, Pressed = 0x158B, Over = 0x158A });
            var child1 = basicWidget(world)
                .Set(new GuiUOInteractionWidget() { Normal = 0x085d, Pressed = 0x085e, Over = 0x085f });

            root.AddChild(child0);
            root.AddChild(child1);

            child1.AddChild(child3);

        }, Stages.Startup, ThreadingMode.Single);

    }
}


struct Gui;

internal enum GuiInputState
{
    None,
    Over,
    Pressed,
}

struct GuiBounds { public Rectangle Value; }

struct ZIndex { public float Value; }

struct GuiText { public string Value; }

struct GuiUOInteractionWidget
{
    public ushort Normal, Pressed, Over;
}
