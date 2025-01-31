using System;
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
                Data<GuiBounds, GuiInputState, ZIndex, GuiUOInteractionWidget, Hue>,
                Filter<With<Gui>, Optional<GuiUOInteractionWidget>, Optional<Hue>>> query) =>
        {
            var b = batch.Value;

            // TODO: remove this
            if (dumbTexture.Value == null)
            {
                dumbTexture.Value = new Texture2D(b.GraphicsDevice, 1, 1);
                dumbTexture.Value.SetData([Color.White]);
            }

            b.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);

            b.Begin(null, Matrix.Identity);
            b.SetSampler(SamplerState.PointClamp);
            b.SetStencil(DepthStencilState.Default);

            foreach ((var bounds, var inputState, var zIndex, var uoWidget, var color) in query)
            {
                var hue = inputState.Ref switch {
                    GuiInputState.Over => new Vector3(0x44, 1, 1f),
                    GuiInputState.Pressed => new Vector3(38, 1, 1f),
                    _ => Unsafe.IsNullRef(ref color.Ref) ? Vector3.UnitZ : new Vector3(color.Ref.Value, 1, 1f)
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
                            hue,
                            0.0f,
                            Vector2.Zero,
                            1.0f,
                            SpriteEffects.None,
                            zIndex.Ref.Value
                        );
                    }
                }
            }

            b.SetSampler(null);
            b.SetStencil(null);
            b.End();

        }, Stages.AfterUpdate, ThreadingMode.Single);


        // input
        scheduler.AddSystem((
            World world,
            Query<Data<GuiBounds, ZIndex, GuiInputState, Children>, Filter<With<Gui>, Optional<Children>>> query,
            Query<Data<GuiBounds, Children>, Filter<With<Gui>, Without<Parent>, Optional<Children>>> queryChildren,
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
            float? lastZIndex = null;
            var found = EntityView.Invalid;
            foreach ((var ent, var bounds, var zIndex, var inputState, var children) in query)
            {
                if (selected.Value.HasValue)
                {
                    if (selected.Value.Value == ent.Ref.ID && isDragging)
                    {
                        newState = GuiInputState.Pressed;
                        found = ent.Ref;

                        if (length > 0)
                        {
                            bounds.Ref.Value.X += (int)mousePosOff.X;
                            bounds.Ref.Value.Y += (int)mousePosOff.Y;

                            moveChildren(ref children.Ref, in mousePosOff, queryChildren);

                            static void moveChildren(
                                ref Children children,
                                ref readonly Vector2 offset,
                                Query<Data<GuiBounds, Children>, Filter<With<Gui>, Without<Parent>, Optional<Children>>> queryChildren)
                            {
                                if (Unsafe.IsNullRef(ref children))
                                    return;

                                foreach (var child in children)
                                {
                                    (var id, var childBounds, var childChildren) = queryChildren.Get(child);
                                    if (id.Ref != child)
                                    {

                                    }
                                    childBounds.Ref.Value.X += (int)offset.X;
                                    childBounds.Ref.Value.Y += (int)offset.Y;
                                    moveChildren(ref childChildren.Ref, in offset, queryChildren);
                                }
                            }
                        }
                    }
                }
                else if (bounds.Ref.Value.Contains((int)mousePos.X, (int)mousePos.Y))
                {
                    if (!isDragging)
                    {
                        if (!lastZIndex.HasValue || lastZIndex.Value <= zIndex.Ref.Value)
                        {
                            found = ent.Ref;
                            newState = GuiInputState.Over;
                            lastZIndex = zIndex.Ref.Value;
                        }
                    }
                }

                inputState.Ref = GuiInputState.None;
            }

            if (found != 0)
            {
                selected.Value = found;
                selected.Value.Value.Set(newState);
            }
        }, Stages.Update, ThreadingMode.Single);


        scheduler.AddSystem((Query<Data<GuiInputState>, With<Gui>> query, Res<MouseContext> mouseCtx) =>
        {
            foreach ((var ent, var inputState) in query)
            {
                if (inputState.Ref == GuiInputState.Pressed)
                {
                    Console.WriteLine("{0} has been pressed", ent.Ref.ID);
                }
            }

            if (mouseCtx.Value.IsPressedDouble(Input.MouseButtonType.Left))
            {
                Console.WriteLine("double click");
            }

        }, Stages.Update, ThreadingMode.Single);

        scheduler.AddSystem((TinyEcs.World world) =>
        {
            static EntityView basicWidget(World ecs, Rectangle? bounds = null, float zIndex = 0)
                => ecs.Entity()
                    .Add<Gui>()
                    .Set(new ZIndex() { Value = zIndex })
                    .Set(GuiInputState.None)
                    .Set(new GuiBounds() { Value = bounds ?? Rectangle.Empty});


            var root = basicWidget(world, new () { X = 100, Y = 200, Width = 120, Height = 90 })
                .Add<GuiRoot>();

           var child0 = basicWidget(world, zIndex: 2)
                .Set(new GuiUOInteractionWidget() { Normal = 0x1589, Pressed = 0x158B, Over = 0x158A });
            var child1 = basicWidget(world, zIndex: 0.1f)
                .Set(new GuiUOInteractionWidget() { Normal = 0x085d, Pressed = 0x085e, Over = 0x085f });
            var child2 = basicWidget(world, new Rectangle(30, 30, 0, 0), 1)
                .Set(new Hue() { Value = 0x44 })
                .Set(new GuiUOInteractionWidget() { Normal = 0x085d, Pressed = 0x085e, Over = 0x085f });

            root.AddChild(child0);
            root.AddChild(child1);
            root.AddChild(child2);
        }, Stages.Startup, ThreadingMode.Single);

    }
}


struct Gui;
struct GuiRoot;

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
