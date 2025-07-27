using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Clay_cs;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly unsafe struct GuiRenderingPlugin : IPlugin
{
    private const FontSystemEffect FONT_EFFECT = FontSystemEffect.Stroked;
    private const int FONT_EFFECT_AMOUNT = 1;

    public void Build(Scheduler scheduler)
    {
        scheduler.OnAfterUpdate((
            Local<Texture2D> dumbTexture,
            Local<StringBuilder> sb,
            Res<UltimaBatcher2D> batcher,
            Res<AssetsServer> assets,
            Res<MouseContext> mouseCtx,
            Res<ClayUOCommandBuffer> commandBuffer,
            Res<ImageCache> imageCache,
            Res<FocusedInput> focusedInput,
            Query<Data<UINode>, Filter<With<Text>, With<TextInput>>> queryTextInput,
            Query<Data<UINode, UIMouseAction>> queryInteraction,
            Query<Data<UINode, Text, UIMouseAction, Children>,
                  Filter<Without<Parent>, Optional<Text>, Optional<UIMouseAction>, Optional<Children>>> query,
            Query<Data<UINode, Text, UIMouseAction, Children>,
                  Filter<With<Parent>, Optional<Text>, Optional<UIMouseAction>, Optional<Children>>> queryChildren
        ) =>
        {
            if (dumbTexture.Value == null)
            {
                dumbTexture.Value = new Texture2D(batcher.Value.GraphicsDevice, 1, 1);
                dumbTexture.Value.SetData([Color.White]);
            }

            commandBuffer.Value.Reset();

            Clay.BeginLayout();
            foreach (var (node, text, interaction, children) in query)
            {
                renderNodes(
                    ref node.Ref,
                    ref text.Ref,
                    ref interaction.Ref,
                    ref children.Ref,
                    commandBuffer,
                    queryChildren
                );
            }

            // (var ent, var node, var mouseAction) = queryInteraction.Get(found);
            // if (mouseAction.IsValid())
            // {
            //     if (mouseAction.Ref.State != lastMouseAction.State || mouseAction.Ref.Button != lastMouseAction.Button)
            //     {
            //         mouseAction.Ref = lastMouseAction;
            //         ent.Ref.Set(lastMouseAction);
            //     }
            // }

            // if (lastMouseAction is { State: UIInteractionState.Pressed, Button: MouseButtonType.Left } && queryTextInput.Contains(found))
            // {
            //     focusedInput.Value.Entity = found;
            // }

            var cmds = Clay.EndLayout();

            var b = batcher.Value;
            b.Begin();

            foreach (ref readonly var cmd in cmds)
            {
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
                                var charsWritten = Encoding.UTF8.GetChars(sp, rentedChars.AsSpan(0, t.stringContents.length));

                                sb.Value.Append(rentedChars.AsSpan(0, charsWritten));
                                var dynFont = font.GetFont(t.fontSize);
                                dynFont.DrawText(
                                    b,
                                    sb.Value,
                                    new(boundingBox.x, boundingBox.y),
                                    toColor(in t.textColor),
                                    characterSpacing: t.letterSpacing,
                                    lineSpacing: t.lineHeight,
                                    layerDepth: cmd.zIndex,
                                    effect: FONT_EFFECT, effectAmount: FONT_EFFECT_AMOUNT
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

                        if (config.cornerRadius.topLeft > 0)
                        {
                            var radius = config.cornerRadius.topLeft;

                            b.DrawRoundedRectangleFilled(dumbTexture.Value,
                                new Rectangle((int)boundingBox.x, (int)boundingBox.y, (int)boundingBox.width, (int)boundingBox.height),
                                radius,
                                toColor(in config.backgroundColor), cmd.zIndex);
                        }
                        else
                        {
                            b.Draw(dumbTexture.Value,
                                new Vector2((int)boundingBox.x, (int)boundingBox.y),
                                new Rectangle(0, 0, (int)boundingBox.width, (int)boundingBox.height),
                                toColor(in config.backgroundColor),
                                0f, Vector2.One, cmd.zIndex);
                        }

                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
                        {
                            ref readonly var img = ref cmd.renderData.image;

                            if (!imageCache.Value.TryGetValue((nint)img.imageData, out var texture) || texture.IsDisposed)
                            {
                                continue;
                            }

                            b.Draw(texture,
                               new Vector2((int)boundingBox.x, (int)boundingBox.y),
                               new Rectangle(0, 0, (int)boundingBox.width, (int)boundingBox.height),
                               toColor(in img.backgroundColor),
                               0f, Vector2.One, cmd.zIndex);

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
                                                cmd.zIndex
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
                                            b.Draw(gumpInfo0.Texture, new Vector2(boundingBox.x, boundingBox.y), gumpInfo0.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo1.Texture != null)
                                            b.DrawTiled(gumpInfo1.Texture,
                                            new(
                                                (int)boundingBox.x + gumpInfo0.UV.Width,
                                                (int)boundingBox.y,
                                                (int)boundingBox.width - gumpInfo0.UV.Width - gumpInfo2.UV.Width,
                                                gumpInfo1.UV.Height
                                            ),
                                            gumpInfo1.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo2.Texture != null)
                                            b.Draw(gumpInfo2.Texture,
                                                new Vector2(boundingBox.x + (boundingBox.width - gumpInfo2.UV.Width), boundingBox.y + offsetTop),
                                                gumpInfo2.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo3.Texture != null)
                                            b.DrawTiled(gumpInfo3.Texture,
                                               new(
                                                   (int)boundingBox.x,
                                                   (int)boundingBox.y + gumpInfo0.UV.Height,
                                                   gumpInfo3.UV.Width,
                                                   (int)boundingBox.height - gumpInfo0.UV.Height - gumpInfo5.UV.Height
                                               ),
                                            gumpInfo3.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo4.Texture != null)
                                            b.DrawTiled(gumpInfo4.Texture,
                                               new(
                                                   (int)boundingBox.x + ((int)boundingBox.width - gumpInfo4.UV.Width),
                                                   (int)boundingBox.y + gumpInfo2.UV.Height,
                                                   gumpInfo4.UV.Width,
                                                   (int)boundingBox.height - gumpInfo2.UV.Height - gumpInfo7.UV.Height
                                               ),
                                            gumpInfo4.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo5.Texture != null)
                                            b.Draw(gumpInfo5.Texture,
                                                new Vector2(boundingBox.x, boundingBox.y + (boundingBox.height - gumpInfo5.UV.Height)),
                                                gumpInfo5.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo6.Texture != null)
                                            b.DrawTiled(gumpInfo6.Texture,
                                           new(
                                               (int)boundingBox.x + gumpInfo5.UV.Width,
                                               (int)boundingBox.y + ((int)boundingBox.height - gumpInfo6.UV.Height - offsetBottom),
                                               (int)boundingBox.width - gumpInfo5.UV.Width - gumpInfo7.UV.Width,
                                               gumpInfo6.UV.Height
                                           ),
                                           gumpInfo6.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo7.Texture != null)
                                            b.Draw(gumpInfo7.Texture,
                                                new Vector2(boundingBox.x + (boundingBox.width - gumpInfo7.UV.Width), boundingBox.y + (boundingBox.height - gumpInfo7.UV.Height)),
                                                gumpInfo7.UV, uoCommand.Hue, cmd.zIndex);

                                        if (gumpInfo8.Texture != null)
                                            b.DrawTiled(gumpInfo8.Texture,
                                           new(
                                               (int)boundingBox.x + gumpInfo0.UV.Width,
                                               (int)boundingBox.y + gumpInfo0.UV.Height,
                                               ((int)boundingBox.width - gumpInfo0.UV.Width - gumpInfo2.UV.Width) + (offsetLeft + offsetRight),
                                               (int)boundingBox.height - gumpInfo2.UV.Height - gumpInfo7.UV.Height
                                           ),
                                           gumpInfo8.UV, uoCommand.Hue, cmd.zIndex);

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
                                            cmd.zIndex
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

                continue;

                static Color toColor(ref readonly Clay_Color c)
                {
                    return new Color
                    (
                        c.r > 1f ? MathHelper.Clamp(c.r / 255f, 0f, 1f) : c.r,
                        c.g > 1f ? MathHelper.Clamp(c.g / 255f, 0f, 1f) : c.g,
                        c.b > 1f ? MathHelper.Clamp(c.b / 255f, 0f, 1f) : c.b,
                        c.a > 1f ? MathHelper.Clamp(c.a / 255f, 0f, 1f) : c.a
                    );
                }
            }

            b.End();

            static void renderNodes
            (
                ref UINode node, ref Text text, ref UIMouseAction interaction, ref Children children,
                ClayUOCommandBuffer commandBuffer,
                Query<Data<UINode, Text, UIMouseAction, Children>,
                      Filter<With<Parent>, Optional<Text>, Optional<UIMouseAction>, Optional<Children>>> query
            )
            {
                Clay.OpenElement();

                var config = node.Config;
                if (node.UOConfig.Type != ClayUOCommandType.None)
                {
                    config.custom.customData = (void*)commandBuffer.AddCommand(node.UOConfig);
                }

                if (config.clip.horizontal || config.clip.vertical)
                {
                    config.clip.childOffset = Clay.GetScrollOffset();
                }

                if (!Unsafe.IsNullRef(ref interaction) && interaction.IsHovered)
                {
                    config.backgroundColor.a = 0.3f;
                    config.backgroundColor.r = 1 * config.backgroundColor.a;
                    config.backgroundColor.g = 0 * config.backgroundColor.a;
                    config.backgroundColor.b = 0 * config.backgroundColor.a;
                }

                Clay.ConfigureOpenElement(config);

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
                        if (!query.Contains(child))
                            continue;

                        (var childNode,
                         var childText,
                         var childInteraction,
                         var childChildren) = query.Get(child);

                        renderNodes(
                            ref childNode.Ref,
                            ref childText.Ref,
                            ref childInteraction.Ref,
                            ref childChildren.Ref,
                            commandBuffer,
                            query
                        );
                    }
                }

                Clay.CloseElement();
            }
        });
    }
}
