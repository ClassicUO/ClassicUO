using System;
using System.Runtime.CompilerServices;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Clay_cs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct GuiPlugin : IPlugin
{
    public unsafe void Build(Scheduler scheduler)
    {
        scheduler.AddSystem(() =>
        {
            var arenaHandle = Clay.CreateArena(Clay.MinMemorySize());
            Clay.Initialize(arenaHandle, new() { width = 300, height = 300 }, 0);
        }, Stages.Startup, ThreadingMode.Single);

        scheduler.AddSystem((Res<GraphicsDevice> device, Res<MouseContext> mouseCtx, Time time) =>
        {
            Clay.SetLayoutDimensions(new()
            {
                width = device.Value.PresentationParameters.BackBufferWidth,
                height = device.Value.PresentationParameters.BackBufferHeight,
            });
            Clay.SetPointerState(new(mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y),
                mouseCtx.Value.IsPressed(Input.MouseButtonType.Left));
            Clay.UpdateScrollContainers(true, new (0, mouseCtx.Value.Wheel), time.Frame);
        }, Stages.Update, ThreadingMode.Single);

        scheduler.AddSystem((
            Res<UltimaBatcher2D> batcher,
            Res<MouseContext> mouseCtx,
            Local<Texture2D> dumbTexture,
            Res<AssetsServer> assets,
            Time time,
            Query<Data<UINode, Children>, Filter<With<Parent>, Optional<Children>>> query,
            Query<Data<UINode, Children>, Filter<Optional<Children>>> queryChildren
        ) =>
        {
            if (dumbTexture.Value == null)
            {
                dumbTexture.Value = new Texture2D(batcher.Value.GraphicsDevice, 1, 1);
                dumbTexture.Value.SetData([Color.White]);
            }

            Clay.BeginLayout();
            foreach ((var ent, var node, var children) in query)
                renderNodes(ref node.Ref, ref children.Ref, queryChildren);
            var cmds = Clay.EndLayout();

            var b = batcher.Value;
            b.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);

            b.Begin(null, Matrix.Identity);
            b.SetSampler(SamplerState.PointClamp);
            b.SetStencil(DepthStencilState.Default);

            Console.WriteLine("cmds count: {0}", cmds.length);

            var span = new ReadOnlySpan<Clay_RenderCommand>(cmds.internalArray, cmds.length);
            foreach (ref readonly var cmd in span)
            {
                Console.WriteLine("cmds type: {0}", cmd.commandType);

                ref readonly var boundingBox = ref cmd.boundingBox;

                switch (cmd.commandType)
                {
                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                        ref readonly var config = ref cmd.renderData.rectangle;

                        b.Draw(
                            dumbTexture.Value,
                            new Rectangle((int)boundingBox.x, (int)boundingBox.y, (int)boundingBox.width, (int)boundingBox.height),
                            Vector3.UnitZ
                        );

                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
                        ref readonly var img = ref cmd.renderData.image;
                        ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump((uint)(nint)img.imageData);

                        b.Draw
                        (
                            gumpInfo.Texture,
                            new Vector2(boundingBox.x, boundingBox.y),
                            gumpInfo.UV,
                            Vector3.UnitZ,
                            0.0f,
                            Vector2.Zero,
                            1.0f,
                            SpriteEffects.None,
                            0f
                        );

                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
                        b.ClipBegin((int)boundingBox.x, (int)boundingBox.y, (int)boundingBox.width, (int)boundingBox.height);
                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
                        b.ClipEnd();
                        break;
                }
            }

            b.SetSampler(null);
            b.SetStencil(null);
            b.End();

            static void renderNodes(ref UINode node, ref Children children,
                Query<Data<UINode, Children>, Filter<Optional<Children>>> query)
            {
                Clay.OpenElement();
                Clay.ConfigureOpenElement(node.Config);

                if (!string.IsNullOrEmpty(node.Text))
                {
                    Clay.OpenTextElement(node.Text, node.TextConfig);
                }

                if (!Unsafe.IsNullRef(ref children))
                {
                    foreach (var child in children)
                    {
                        (var childNode, var childChildren) = query.Get(child);
                        renderNodes(ref childNode.Ref, ref childChildren.Ref, query);
                    }
                }

                Clay.CloseElement();
            }
        }, Stages.AfterUpdate, ThreadingMode.Single);


        scheduler.AddSystem((World ecs, Res<AssetsServer> assets) =>
        {
            ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(0x085d);

            var clayEnt = ecs.Entity()
                .Set(new UINode()
                {
                    Config = {
                        backgroundColor = new (48, 48, 48, 255),
                        layout = {
                            sizing = {
                                width = Clay_SizingAxis.Percent(0.3f),
                                height = Clay_SizingAxis.Percent(0.3f),
                            },
                            // childAlignment = {
                            //     x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            //     y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
                            // },
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            childGap = 8
                        },
                        scroll = {
                            vertical = true
                        }
                    }
                });

            for (var i = 0; i < 100; ++i)
            {
                // var childText = ecs.Entity()
                //     .Set(new UINode()
                //     {
                //         Text = "hello",
                //         TextConfig = {
                //             fontId = 0,
                //             fontSize = 20,
                //             textColor = new (255, 255, 255, 255)
                //         },
                //         Config = {
                //             backgroundColor = new (68, 68, 68, 255),
                //             layout = {
                //                 sizing = {
                //                     width = Clay_SizingAxis.Percent(0.5f),
                //                     height = Clay_SizingAxis.Percent(0.5f),
                //                 }
                //             }
                //         }
                //     });

                var childImage = ecs.Entity()
                    .Set(new UINode()
                    {
                        Config = {
                            // backgroundColor = new (68, 68, 68, 255),
                            layout = {
                                sizing = {
                                    width = Clay_SizingAxis.Fixed(gumpInfo.UV.Width),
                                    height = Clay_SizingAxis.Fixed(gumpInfo.UV.Height),
                                }
                            },
                            image = {
                                imageData = ((nint)0x085d).ToPointer(),
                                sourceDimensions = {
                                    width = gumpInfo.UV.Width,
                                    height = gumpInfo.UV.Height
                                }
                            }
                        }
                    });


                // clayEnt.AddChild(childText);
                clayEnt.AddChild(childImage);
            }
        }, Stages.Startup, ThreadingMode.Single);
    }
}

struct UINode
{
    public string Text;
    public Clay_TextElementConfig TextConfig;
    public Clay_ElementDeclaration Config;
}