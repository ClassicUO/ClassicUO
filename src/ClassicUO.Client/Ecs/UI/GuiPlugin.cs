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

        scheduler.AddSystem((Query<Data<UINode, UOButton, UIInteractionState>> query) =>
        {
            foreach ((var ent, var node, var button, var interaction) in query)
            {
                node.Ref.UOConfig.Id = interaction.Ref switch
                {
                    UIInteractionState.Over => button.Ref.Over,
                    UIInteractionState.Pressed or UIInteractionState.Handled => button.Ref.Pressed,
                    _ => button.Ref.Normal
                };
            }
        }, Stages.Update, ThreadingMode.Single);

        scheduler.AddSystem((
            Local<ulong> lastEntityPressed,
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

            if (lastEntityPressed.Value != 0 && mouseCtx.Value.IsReleased(MouseButtonType.Left))
            {
                lastEntityPressed.Value = 0;
            }

            commandBuffer.Value.Reset();

            Clay.BeginLayout();
            ulong found = 0;
            var lastInteraction = UIInteractionState.None;
            foreach ((var ent, var node, var interaction, var children) in query)
                renderNodes(ent.Ref, ref found, lastEntityPressed.Value, ref lastInteraction, ref node.Ref, ref interaction.Ref, ref children.Ref, mouseCtx, commandBuffer, queryChildren);
            var cmds = Clay.EndLayout();

            if (found != 0)
            {
                lastEntityPressed.Value = found;
                (_, var interaction, _) = queryChildren.Get(found);
                if (!Unsafe.IsNullRef(ref interaction.Ref))
                    interaction.Ref = lastInteraction;
            }

            var b = batcher.Value;
            b.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);

            b.Begin(null, Matrix.Identity);
            b.SetSampler(SamplerState.PointClamp);
            b.SetStencil(DepthStencilState.Default);

            // Console.WriteLine("cmds count: {0}", cmds.length);

            var span = new ReadOnlySpan<Clay_RenderCommand>(cmds.internalArray, cmds.length);
            foreach (ref readonly var cmd in span)
            {
                // Console.WriteLine("cmds type: {0}", cmd.commandType);

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
                                            new Vector2(boundingBox.x, boundingBox.y),
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
                                            new Vector2(boundingBox.x, boundingBox.y),
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
                ulong ent,
                ref ulong found,
                ulong lastPressed,
                ref UIInteractionState newInteraction,
                ref UINode node, ref UIInteractionState interaction, ref Children children,
                MouseContext mouseCtx,
                ClayUOCommandBuffer commandBuffer,
                Query<Data<UINode, UIInteractionState, Children>, Filter<Optional<UIInteractionState>, Optional<Children>>> query
            )
            {
                Clay.OpenElement();

                var config = node.Config;
                if (node.UOConfig.Type != ClayUOCommandType.None)
                {
                    config.custom.customData = (void*)commandBuffer.AddCommand(node.UOConfig);
                }

                Clay.ConfigureOpenElement(config);

                if (!Unsafe.IsNullRef(ref interaction))
                {
                    if (lastPressed != 0)
                    {
                        if (lastPressed == ent && mouseCtx.IsPressed(MouseButtonType.Left))
                        {
                            if (interaction != UIInteractionState.Handled)
                                newInteraction = UIInteractionState.Pressed;
                            else
                                newInteraction = UIInteractionState.Handled;
                            found = ent;
                        }
                    }
                    else
                    {
                        if (!mouseCtx.IsPressed(MouseButtonType.Left) && Clay.IsHovered())
                        {
                            found = ent;
                            newInteraction = UIInteractionState.Over;
                        }

                        interaction = UIInteractionState.None;
                    }
                }

                if (!string.IsNullOrEmpty(node.Text))
                {
                    Clay.OpenTextElement(node.Text, node.TextConfig);
                }

                if (!Unsafe.IsNullRef(ref children))
                {
                    foreach (var child in children)
                    {
                        (var childEnt, var childNode, var childInteraction, var childChildren) = query.Get(child);
                        renderNodes(childEnt.Ref, ref found, lastPressed, ref newInteraction, ref childNode.Ref, ref childInteraction.Ref, ref childChildren.Ref, mouseCtx, commandBuffer, query);
                    }
                }

                Clay.CloseElement();
            }
        }, Stages.AfterUpdate, ThreadingMode.Single);
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
    Over,
    Pressed,
    Handled,
}


struct UOButton
{
    public ushort Normal, Pressed, Over;
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
}