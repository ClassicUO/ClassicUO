using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Clay_cs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct GuiPlugin : IPlugin
{
    public unsafe void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new ClayUOCommandBuffer());

        scheduler.AddPlugin<MainScreenPlugin>();

        scheduler.AddSystem(() =>
        {
            var arenaHandle = Clay.CreateArena(Clay.MinMemorySize());
            var ctx = Clay.Initialize(arenaHandle, new() { width = 300, height = 300 }, 0);
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
            Clay.UpdateScrollContainers(true, new(0, mouseCtx.Value.Wheel), time.Frame);
        }, Stages.Update, ThreadingMode.Single);

        scheduler.AddSystem((
            Res<UltimaBatcher2D> batcher,
            Local<Texture2D> dumbTexture,
            Res<AssetsServer> assets,
            Res<MouseContext> mouseCtx,
            Res<ClayUOCommandBuffer> commandBuffer,
            Query<Data<UINode, UIInteractionState, Children>, Filter<With<Parent>, Optional<UIInteractionState>, Optional<Children>>> query,
            Query<Data<UINode, UIInteractionState, Children>, Filter<Optional<UIInteractionState>, Optional<Children>>> queryChildren
        ) =>
        {
            if (dumbTexture.Value == null)
            {
                dumbTexture.Value = new Texture2D(batcher.Value.GraphicsDevice, 1, 1);
                dumbTexture.Value.SetData([Color.White]);
            }

            commandBuffer.Value.Reset();

            Clay.BeginLayout();
            foreach ((var node, var interaction, var children) in query)
                renderNodes(ref node.Ref, ref interaction.Ref, ref children.Ref, mouseCtx, commandBuffer, queryChildren);
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
                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
                        ref readonly var text = ref cmd.renderData.text;

                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                        ref readonly var config = ref cmd.renderData.rectangle;

                        b.Draw(
                            dumbTexture.Value,
                            new Rectangle((int)boundingBox.x, (int)boundingBox.y, (int)boundingBox.width, (int)boundingBox.height),
                            new Vector3(0x44, 1, 1)
                        );

                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
                        {
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
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM:
                        {
                            ref readonly var custom = ref cmd.renderData.custom;
                            var commandIndex = ((int)custom.customData) - 1;
                            ref readonly var uoCommand = ref commandBuffer.Value.GetCommand(commandIndex);

                            switch (uoCommand.Type)
                            {
                                case ClayUOCommandType.Gump:
                                    ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(uoCommand.Id);
                                    if (gumpInfo.Texture != null)
                                    {
                                        b.Draw
                                        (
                                            gumpInfo.Texture,
                                            uoCommand.Position,
                                            gumpInfo.UV,
                                            uoCommand.Hue,
                                            0.0f,
                                            Vector2.Zero,
                                            1.0f,
                                            SpriteEffects.None,
                                            0f
                                        );
                                    }
                                    break;

                                case ClayUOCommandType.Art:
                                    ref readonly var artInfo = ref assets.Value.Arts.GetArt(uoCommand.Id);
                                    if (artInfo.Texture != null)
                                    {
                                        b.Draw
                                        (
                                            artInfo.Texture,
                                            uoCommand.Position,
                                            artInfo.UV,
                                            uoCommand.Hue,
                                            0.0f,
                                            Vector2.Zero,
                                            1.0f,
                                            SpriteEffects.None,
                                            0f
                                        );
                                    }
                                    break;

                                case ClayUOCommandType.Text:
                                    break;

                                case ClayUOCommandType.Animation:
                                    break;
                            }
                            break;
                        }

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

            static void renderNodes
            (
                ref UINode node, ref UIInteractionState interaction, ref Children children,
                MouseContext mouseCtx,
                ClayUOCommandBuffer commandBuffer,
                Query<Data<UINode, UIInteractionState, Children>, Filter<Optional<UIInteractionState>, Optional<Children>>> query
            )
            {
                Clay.OpenElement();

                if (!Unsafe.IsNullRef(ref interaction))
                {
                    interaction = Clay.IsHovered() ? mouseCtx.IsPressed(MouseButtonType.Left) ? UIInteractionState.Pressed : UIInteractionState.Hover : UIInteractionState.None;
                }

                var config = node.Config;
                if (node.UOConfig.Type != ClayUOCommandType.None)
                {
                    config.custom.customData = (void*)commandBuffer.AddCommand(node.UOConfig);
                }

                Clay.ConfigureOpenElement(config);

                if (!string.IsNullOrEmpty(node.Text))
                {
                    Clay.OpenTextElement(node.Text, node.TextConfig);
                }

                if (!Unsafe.IsNullRef(ref children))
                {
                    foreach (var child in children)
                    {
                        (var childNode, var childInteraction, var childChildren) = query.Get(child);
                        renderNodes(ref childNode.Ref, ref childInteraction.Ref, ref childChildren.Ref, mouseCtx, commandBuffer, query);
                    }
                }

                Clay.CloseElement();
            }
        }, Stages.AfterUpdate, ThreadingMode.Single);


        scheduler.AddSystem((World ecs, Res<AssetsServer> assets) =>
        {
            return;
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
                    })
                    .Set(UIInteractionState.None);


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
    public ClayUOCommandData UOConfig;
}

enum UIInteractionState : byte
{
    None,
    Hover,
    Pressed
}

enum ClayUOCommandType : byte
{
    None,
    Text,
    Gump,
    Art,
    Land,
    Animation,
}

[StructLayout(LayoutKind.Sequential)]
internal struct ClayUOCommandData
{
    public ClayUOCommandType Type;

    public uint Id;
    public Vector2 Position;
    public Vector3 Hue;
}

internal sealed class ClayUOCommandBuffer
{
    private ClayUOCommandData[] _commands;
    private int _index;
    private const int DefaultCapacity = 256;

    public ClayUOCommandBuffer()
    {
        _commands = new ClayUOCommandData[DefaultCapacity];
        _index = 0;
    }

    public void Reset()
    {
        _index = 0;
    }

    public nint AddCommand(in ClayUOCommandData command)
    {
        EnsureCapacity();
        _commands[_index] = command;
        return (nint)(++_index);
    }

    public ref readonly ClayUOCommandData GetCommand(int index)
    {
        if (index < 0 || index >= _commands.Length)
            throw new IndexOutOfRangeException($"Command index {index} is out of range");

        return ref _commands[index];
    }

    private void EnsureCapacity()
    {
        if (_index >= _commands.Length)
        {
            Array.Resize(ref _commands, _commands.Length * 2);
        }
    }

    public int Count => _index;

    public nint AddGumpCommand(uint gumpIndex, Vector2 position, Vector3 hue)
    {
        return AddCommand(new ClayUOCommandData
        {
            Type = ClayUOCommandType.Gump,
            Id = gumpIndex,
            Position = position,
            Hue = hue
        });
    }

    public nint AddArtCommand(uint artIndex, Vector2 position, Vector3 hue)
    {
        return AddCommand(new ClayUOCommandData
        {
            Type = ClayUOCommandType.Art,
            Id = artIndex,
            Position = position,
            Hue = hue
        });
    }

    public nint AddTextCommand(uint textIndex, Vector2 position, Vector3 hue)
    {
        return AddCommand(new ClayUOCommandData
        {
            Type = ClayUOCommandType.Text,
            Id = textIndex,
            Position = position,
            Hue = hue
        });
    }

    public nint AddAnimationCommand(uint animIndex, Vector2 position, Vector3 hue)
    {
        return AddCommand(new ClayUOCommandData
        {
            Type = ClayUOCommandType.Animation,
            Id = animIndex,
            Position = position,
            Hue = hue
        });
    }
}