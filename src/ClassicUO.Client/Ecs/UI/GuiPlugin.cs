using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Clay_cs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;


internal sealed class FocusedInput
{
    public ulong Entity { get; set; }
}


internal readonly struct GuiPlugin : IPlugin
{
    private static unsafe Clay_Dimensions OnMeasureText(Clay_StringSlice slice, Clay_TextElementConfig* config, void* userData)
    {
        var raw = new ReadOnlySpan<byte>(slice.chars, slice.length);
        var text = Encoding.UTF8.GetString(raw);

        var font = FontCache.GetFont(config->fontId);
        var dynFont = font.GetFont(config->fontSize);
        var size = dynFont.MeasureString(text);

        return new Clay_Dimensions(size.X, size.Y);
    }

    private static unsafe void OnClayError(Clay_ErrorData errorData)
    {
        var raw = new ReadOnlySpan<byte>(errorData.errorText.chars, errorData.errorText.length);
        var text = Encoding.UTF8.GetString(raw);
        Console.WriteLine($"Clay error: {errorData.errorType} - {text}");
    }

    public unsafe void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new ClayUOCommandBuffer());
        scheduler.AddResource(new FocusedInput());

        scheduler.OnStartup((SchedulerState state, World world, Res<AssetsServer> assets) =>
            state.AddResource(new GumpBuilder(world, assets)),
        ThreadingMode.Single);

        scheduler.AddPlugin<LoginScreenPlugin>();
        scheduler.AddPlugin<ServerSelectionPlugin>();
        scheduler.AddPlugin<GameScreenPlugin>();

        var states = Enum.GetValues<GameState>();
        foreach (var state in states)
            scheduler.OnExit(state, (Res<FocusedInput> focusedInput) => focusedInput.Value.Entity = 0, ThreadingMode.Single);

        scheduler.OnStartup(() =>
        {
            var arenaHandle = Clay.CreateArena(Clay.MinMemorySize());
            var errorFn = (nint)(delegate*<Clay_ErrorData, void>)&OnClayError;
            var ctx = Clay.Initialize(arenaHandle, new() { width = 300, height = 300 }, errorFn);
            var measureTextFn = (nint)(delegate*<Clay_StringSlice, Clay_TextElementConfig*, void*, Clay_Dimensions>)&OnMeasureText;
            Clay.SetMeasureTextFunction(measureTextFn);
        }, ThreadingMode.Single);

        scheduler.OnUpdate((Res<GraphicsDevice> device, Res<MouseContext> mouseCtx, Time time) =>
        {
            Clay.SetLayoutDimensions(new()
            {
                width = device.Value.PresentationParameters.BackBufferWidth,
                height = device.Value.PresentationParameters.BackBufferHeight,
            });
            Clay.SetPointerState(new(mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y),
                mouseCtx.Value.IsPressed(Input.MouseButtonType.Left));
            Clay.UpdateScrollContainers(true, new(0, mouseCtx.Value.Wheel), time.Frame);
        }, ThreadingMode.Single);

        scheduler.OnUpdate((Query<Data<UINode, UOButton, UIInteractionState>, Changed<UIInteractionState>> query) =>
        {
            foreach ((var node, var button, var interaction) in query)
            {
                node.Ref.UOConfig.Id = interaction.Ref switch
                {
                    UIInteractionState.Over => button.Ref.Over,
                    UIInteractionState.Pressed => button.Ref.Pressed,
                    _ => button.Ref.Normal
                };
            }
        }, ThreadingMode.Single);

        scheduler.OnUpdate((Query<Data<Text>, Filter<With<TextInput>>> query, Res<FocusedInput> focusedInput) =>
        {
            var ok = false;
            var last = 0ul;
            foreach ((var ent, var textInput) in query)
            {
                if (focusedInput.Value.Entity == ent.Ref)
                {
                    ok = true;
                    continue;
                }

                if (ok)
                    last = ent.Ref;
            }

            if (ok && last == 0)
            {
                foreach ((var ent, var textInput) in query)
                {
                    last = ent.Ref;
                    break;
                }
            }

            if (last != 0)
            {
                focusedInput.Value.Entity = last;
            }
        }, ThreadingMode.Single)
        .RunIf((Res<KeyboardContext> keyboardCtx) => keyboardCtx.Value.IsPressedOnce(Microsoft.Xna.Framework.Input.Keys.Tab));

        scheduler.OnUpdate((EventReader<CharInputEvent> reader, Res<FocusedInput> focusedInput, Query<Data<Text>, Filter<With<TextInput>>> query) =>
        {
            (_, var node) = query.Get(focusedInput.Value.Entity);

            foreach (var c in reader)
                node.Ref.Value = TextComposer.Compose(node.Ref.Value, c.Value);
        }, ThreadingMode.Single)
        .RunIf((EventReader<CharInputEvent> reader, Res<FocusedInput> focusedInput, Query<Data<UINode>, Filter<With<TextInput>>> query)
            => !reader.IsEmpty && focusedInput.Value.Entity != 0 && query.Count() > 0);

        scheduler.OnAfterUpdate((
            Local<ulong> lastEntityPressed,
            Local<Texture2D> dumbTexture,
            Local<StringBuilder> sb,
            Res<UltimaBatcher2D> batcher,
            Res<AssetsServer> assets,
            Res<MouseContext> mouseCtx,
            Res<ClayUOCommandBuffer> commandBuffer,
            Res<FocusedInput> focusedInput,
            Query<Data<UINode>, Filter<With<Text>, With<TextInput>>> queryTextInput,
            Query<Data<UINode, UIInteractionState>> queryInteraction,
            Query<Data<UINode, Text, UIInteractionState, Children>,
                  Filter<Without<Parent>, Optional<Text>, Optional<UIInteractionState>, Optional<Children>>> query,
            Query<Data<UINode, Text, UIInteractionState, Children>,
                  Filter<With<Parent>, Optional<Text>, Optional<UIInteractionState>, Optional<Children>>> queryChildren
        ) =>
        {
            if (dumbTexture.Value == null)
            {
                dumbTexture.Value = new Texture2D(batcher.Value.GraphicsDevice, 1, 1);
                dumbTexture.Value.SetData([Color.White]);
            }

            commandBuffer.Value.Reset();

            Clay.BeginLayout();
            ulong found = 0;
            var lastInteraction = UIInteractionState.None;
            foreach ((var ent, var node, var text, var interaction, var children) in query)
                renderNodes(
                    ent.Ref,
                    ref found,
                    lastEntityPressed.Value,
                    ref lastInteraction,
                    ref node.Ref,
                    ref text.Ref,
                    ref interaction.Ref,
                    ref children.Ref,
                    mouseCtx,
                    commandBuffer,
                    queryChildren
                );

            lastEntityPressed.Value = found;

            if (found != 0)
            {
                (var ent, var node, var interaction) = queryInteraction.Get(found);
                if (!Unsafe.IsNullRef(ref interaction.Ref))
                {
                    interaction.Ref = lastInteraction;
                    ent.Ref.Set(lastInteraction);
                }

                if (lastInteraction == UIInteractionState.Pressed && queryTextInput.Contains(found))
                {
                    focusedInput.Value.Entity = found;
                }
            }

            var cmds = Clay.EndLayout();

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
                        {
                            ref readonly var t = ref cmd.renderData.text;
                            var font = FontCache.GetFont(t.fontId);

                            sb.Value ??= new();
                            sb.Value.Clear();

                            var rentedChars = ArrayPool<char>.Shared.Rent(t.stringContents.length);

                            try
                            {
                                var sp = new ReadOnlySpan<byte>(t.stringContents.chars, t.stringContents.length);
                                var charsWritten = Encoding.UTF8.GetChars(sp, rentedChars);

                                foreach (ref readonly var c in rentedChars.AsSpan(0, charsWritten))
                                {
                                    sb.Value.Append(c);
                                }

                                var dynFont = font.GetFont(t.fontSize);
                                dynFont.DrawText(
                                    b,
                                    sb.Value,
                                    new(boundingBox.x, boundingBox.y),
                                    new Color(t.textColor.r, t.textColor.g, t.textColor.b, t.textColor.a),
                                    characterSpacing: t.letterSpacing,
                                    lineSpacing: t.lineHeight,
                                    effect: FontStashSharp.FontSystemEffect.Stroked, effectAmount: 1
                                );
                            }
                            finally
                            {
                                ArrayPool<char>.Shared.Return(rentedChars);
                            }

                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                        ref readonly var config = ref cmd.renderData.rectangle;

                        b.Draw(dumbTexture.Value,
                            new Vector2((int)boundingBox.x, (int)boundingBox.y),
                            new Rectangle(0, 0, (int)boundingBox.width, (int)boundingBox.height),
                            new Color(config.backgroundColor.r, config.backgroundColor.g, config.backgroundColor.b, config.backgroundColor.a),
                            0f, Vector2.One, 0f);

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
                                    {
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
                                    }

                                case ClayUOCommandType.GumpNinePatch:
                                    {
                                        ref readonly var gumpInfo0 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 0);
                                        ref readonly var gumpInfo1 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 1);
                                        ref readonly var gumpInfo2 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 2);
                                        ref readonly var gumpInfo3 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 3);
                                        ref readonly var gumpInfo4 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 4 + 1);
                                        ref readonly var gumpInfo5 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 5 + 1);
                                        ref readonly var gumpInfo6 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 6 + 1);
                                        ref readonly var gumpInfo7 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 7 + 1);
                                        ref readonly var gumpInfo8 = ref assets.Value.Gumps.GetGump(uoCommand.Id + 4);

                                        var offsetTop = Math.Max(gumpInfo0.UV.Height, gumpInfo2.UV.Height) - gumpInfo1.UV.Height;
                                        var offsetBottom = Math.Max(gumpInfo5.UV.Height, gumpInfo7.UV.Height) - gumpInfo6.UV.Height;
                                        var offsetLeft = Math.Abs(Math.Max(gumpInfo0.UV.Width, gumpInfo5.UV.Width) - gumpInfo2.UV.Width);
                                        var offsetRight = Math.Max(gumpInfo2.UV.Width, gumpInfo7.UV.Width) - gumpInfo4.UV.Width;

                                        if (gumpInfo0.Texture != null)
                                            b.Draw(gumpInfo0.Texture, new Vector2(boundingBox.x, boundingBox.y), gumpInfo0.UV, uoCommand.Hue);

                                        if (gumpInfo1.Texture != null)
                                            b.DrawTiled(gumpInfo1.Texture,
                                            new(
                                                (int)boundingBox.x + gumpInfo0.UV.Width,
                                                (int)boundingBox.y,
                                                (int)boundingBox.width - gumpInfo0.UV.Width - gumpInfo2.UV.Width,
                                                gumpInfo1.UV.Height
                                            ),
                                            gumpInfo1.UV, uoCommand.Hue);

                                        if (gumpInfo2.Texture != null)
                                            b.Draw(gumpInfo2.Texture,
                                                new Vector2(boundingBox.x + (boundingBox.width - gumpInfo2.UV.Width), boundingBox.y + offsetTop),
                                                gumpInfo2.UV, uoCommand.Hue);

                                        if (gumpInfo3.Texture != null)
                                            b.DrawTiled(gumpInfo3.Texture,
                                           new(
                                               (int)boundingBox.x,
                                               (int)boundingBox.y + gumpInfo0.UV.Height,
                                               gumpInfo3.UV.Width,
                                               (int)boundingBox.height - gumpInfo0.UV.Height - gumpInfo5.UV.Height
                                           ),
                                           gumpInfo3.UV, uoCommand.Hue);

                                        if (gumpInfo4.Texture != null)
                                            b.DrawTiled(gumpInfo4.Texture,
                                           new(
                                               (int)boundingBox.x + ((int)boundingBox.width - gumpInfo4.UV.Width),
                                               (int)boundingBox.y + gumpInfo2.UV.Height,
                                               gumpInfo4.UV.Width,
                                               (int)boundingBox.height - gumpInfo2.UV.Height - gumpInfo7.UV.Height
                                           ),
                                           gumpInfo4.UV, uoCommand.Hue);

                                        if (gumpInfo5.Texture != null)
                                            b.Draw(gumpInfo5.Texture,
                                                new Vector2(boundingBox.x, boundingBox.y + (boundingBox.height - gumpInfo5.UV.Height)),
                                                gumpInfo5.UV, uoCommand.Hue);

                                        if (gumpInfo6.Texture != null)
                                            b.DrawTiled(gumpInfo6.Texture,
                                           new(
                                               (int)boundingBox.x + gumpInfo5.UV.Width,
                                               (int)boundingBox.y + ((int)boundingBox.height - gumpInfo6.UV.Height - offsetBottom),
                                               (int)boundingBox.width - gumpInfo5.UV.Width - gumpInfo7.UV.Width,
                                               gumpInfo6.UV.Height
                                           ),
                                           gumpInfo6.UV, uoCommand.Hue);

                                        if (gumpInfo7.Texture != null)
                                            b.Draw(gumpInfo7.Texture,
                                                new Vector2(boundingBox.x + (boundingBox.width - gumpInfo7.UV.Width), boundingBox.y + (boundingBox.height - gumpInfo7.UV.Height)),
                                                gumpInfo7.UV, uoCommand.Hue);

                                        if (gumpInfo8.Texture != null)
                                            b.DrawTiled(gumpInfo8.Texture,
                                           new(
                                               (int)boundingBox.x + gumpInfo0.UV.Width,
                                               (int)boundingBox.y + gumpInfo0.UV.Height,
                                               ((int)boundingBox.width - gumpInfo0.UV.Width - gumpInfo2.UV.Width) + (offsetLeft + offsetRight),
                                               (int)boundingBox.height - gumpInfo2.UV.Height - gumpInfo7.UV.Height
                                           ),
                                           gumpInfo8.UV, uoCommand.Hue);

                                        break;
                                    }

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
                EntityView ent,
                ref ulong found,
                ulong lastPressed,
                ref UIInteractionState newInteraction,
                ref UINode node, ref Text text, ref UIInteractionState interaction, ref Children children,
                MouseContext mouseCtx,
                ClayUOCommandBuffer commandBuffer,
                Query<Data<UINode, Text, UIInteractionState, Children>,
                      Filter<With<Parent>, Optional<Text>, Optional<UIInteractionState>, Optional<Children>>> query
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
                        if (lastPressed == ent)
                        {
                            if (mouseCtx.IsPressed(MouseButtonType.Left))
                            {
                                found = ent;
                                newInteraction = UIInteractionState.Pressed;
                            }
                            else if (interaction == UIInteractionState.Pressed)
                            {
                                found = ent;
                                newInteraction = UIInteractionState.Released;
                            }
                            else if (interaction == UIInteractionState.Over && Clay.IsHovered())
                            {
                                found = ent;
                                newInteraction = UIInteractionState.Over;
                            }
                        }
                    }
                    else
                    {
                        if (!mouseCtx.IsPressed(MouseButtonType.Left) && Clay.IsHovered())
                        {
                            found = ent;
                            newInteraction = UIInteractionState.Over;
                        }
                    }

                    if (interaction != UIInteractionState.None)
                    {
                        ent.Set(UIInteractionState.None);
                    }
                }

                if (!Unsafe.IsNullRef(ref text) && !string.IsNullOrEmpty(text.Value))
                {
                    if (text.ReplaceChar != 0)
                    {
                        char[] rentedBuffer = null;
                        Span<char> buffer = text.Value.Length < 256 ?
                            stackalloc char[text.Value.Length]
                            :
                            rentedBuffer = ArrayPool<char>.Shared.Rent(text.Value.Length);

                        try
                        {
                            buffer.Slice(0, text.Value.Length).Fill(text.ReplaceChar);
                            Clay.OpenTextElement(buffer.Slice(0, text.Value.Length), text.TextConfig);
                        }
                        finally
                        {
                            if (rentedBuffer != null)
                                ArrayPool<char>.Shared.Return(rentedBuffer);
                        }
                    }
                    else
                    {
                        Clay.OpenTextElement(text.Value, text.TextConfig);
                    }
                }

                if (!Unsafe.IsNullRef(ref children))
                {
                    foreach (var child in children)
                    {
                        (var childEnt, var childNode, var childText, var childInteraction, var childChildren) = query.Get(child);
                        renderNodes(
                            childEnt.Ref,
                            ref found,
                            lastPressed,
                            ref newInteraction,
                            ref childNode.Ref,
                            ref childText.Ref,
                            ref childInteraction.Ref,
                            ref childChildren.Ref,
                            mouseCtx,
                            commandBuffer,
                            query
                        );
                    }
                }

                Clay.CloseElement();
            }
        }, ThreadingMode.Single);
    }
}

struct UINode
{
    public Clay_ElementDeclaration Config;
    public ClayUOCommandData UOConfig;
}

enum UIInteractionState : byte
{
    None,
    Over,
    Pressed,
    Released,
}


struct UOButton
{
    public ushort Normal, Pressed, Over;
}

struct Text
{
    public string Value;
    public char ReplaceChar;
    public Clay_TextElementConfig TextConfig;
}

struct TextInput;


enum ClayUOCommandType : byte
{
    None,
    Text,
    Gump,
    GumpNinePatch,
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
