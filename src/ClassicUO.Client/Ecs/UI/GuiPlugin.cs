using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Clay_cs;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

public struct TESTME;

internal readonly struct GuiPlugin : IPlugin
{
    private const FontSystemEffect FONT_EFFECT = FontSystemEffect.Stroked;
    private const int FONT_EFFECT_AMOUNT = 1;

    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new ClayUOCommandBuffer());
        scheduler.AddResource(new FocusedInput());
        scheduler.AddResource(new ImageCache());

        scheduler.OnStartup((SchedulerState state, World world, Res<AssetsServer> assets) =>
            state.AddResource(new GumpBuilder(world, assets)));


        var states = Enum.GetValues<GameState>();
        foreach (var state in states)
            scheduler.OnExit(state, (Res<FocusedInput> focusedInput) => focusedInput.Value.Entity = 0);

        var setupClayFn = SetupClay;
        scheduler.OnStartup(setupClayFn);

        var setClayWorkspaceDimensionsFn = SetClayWorkspaceDimensions;
        scheduler.OnUpdate(setClayWorkspaceDimensionsFn);

        var updateUOButtonsStateFn = UpdateUOButtonsState;
        scheduler.OnUpdate(updateUOButtonsStateFn);

        var updateFocusedInputFn = UpdateFocusedInput;
        scheduler.OnUpdate(updateFocusedInputFn)
            .RunIf((Res<KeyboardContext> keyboardCtx) => keyboardCtx.Value.IsPressedOnce(Microsoft.Xna.Framework.Input.Keys.Tab));

        var readCharInputsFn = ReadCharInputs;
        scheduler.OnUpdate(readCharInputsFn)
            .RunIf((EventReader<CharInputEvent> reader,
                    Res<FocusedInput> focusedInput,
                    Query<Data<UINode>, Filter<With<TextInput>>> query) =>
                        !reader.IsEmpty && focusedInput.Value.Entity != 0 && query.Count() > 0);

        var moveFocusedElementsByMouseFn = MoveFocusedElementsByMouse;
        scheduler.OnUpdate(moveFocusedElementsByMouseFn);

        var handleNodeStatesFn = HandleNodeStates;
        scheduler.OnFrameEnd(handleNodeStatesFn);

        // scheduler.OnFrameEnd((Query<Data<UINode>, With<TESTME>> q) =>
        // {
        //     foreach (var (ent, node) in q)
        //     {
        //         ent.Ref.Delete();
        //         // Console.WriteLine("3 - TESTME {0} gen: {1}", ent.Ref.ID.RealId(), ent.Ref.Generation);
        //     }
        // });

        // scheduler.OnFrameEnd((World world) =>
        // {
        //     var ent = world.Entity().Add<TESTME>().SetUINode(new()
        //     {
        //         Config = {
        //             backgroundColor = new(1f, 0, 0, 1f),
        //             layout = {
        //                 sizing = {
        //                     width = Clay_SizingAxis.Fixed(100),
        //                     height= Clay_SizingAxis.Fixed(100)
        //                 }
        //             }
        //         }
        //     });

        //     // Console.WriteLine("1 - TESTME {0} gen: {1}", ent.ID.RealId(), ent.Generation);
        // });
    }


    private static unsafe Clay_Dimensions OnMeasureText(
        Clay_StringSlice slice,
        Clay_TextElementConfig* config,
        void* userData
    )
    {
        var raw = new ReadOnlySpan<byte>(slice.chars, slice.length);
        var text = Encoding.UTF8.GetString(raw);

        var font = FontCache.GetFont(config->fontId);
        var dynFont = font.GetFont(config->fontSize);
        var size = dynFont.MeasureString(
            text,
            characterSpacing: config->letterSpacing,
            lineSpacing: config->lineHeight,
            effect: FONT_EFFECT, effectAmount: FONT_EFFECT_AMOUNT);

        return new Clay_Dimensions(size.X, size.Y);
    }

    private static unsafe void OnClayError(Clay_ErrorData errorData)
    {
        var raw = new ReadOnlySpan<byte>(errorData.errorText.chars, errorData.errorText.length);
        var text = Encoding.UTF8.GetString(raw);
        Console.WriteLine($"Clay error: {errorData.errorType} - {text}");
    }

    private static unsafe void SetupClay()
    {
        var arenaHandle = Clay.CreateArena(Clay.MinMemorySize());
        var errorFn = (nint)(delegate*<Clay_ErrorData, void>)&OnClayError;
        var ctx = Clay.Initialize(arenaHandle, new() { width = 300, height = 300 }, errorFn);
        var measureTextFn = (nint)(delegate*<Clay_StringSlice, Clay_TextElementConfig*, void*, Clay_Dimensions>)&OnMeasureText;
        Clay.SetMeasureTextFunction(measureTextFn);

        Clay.SetDebugModeEnabled(true);
    }

    private static void SetClayWorkspaceDimensions(
        Res<GraphicsDevice> device,
        Res<MouseContext> mouseCtx,
        Time time
    )
    {
        Clay.SetLayoutDimensions(new()
        {
            width = device.Value.PresentationParameters.BackBufferWidth,
            height = device.Value.PresentationParameters.BackBufferHeight,
        });
        Clay.SetPointerState(new(mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y),
                             mouseCtx.Value.IsPressed(Input.MouseButtonType.Left));
        Clay.UpdateScrollContainers(true, new(0, mouseCtx.Value.Wheel * 3), time.Frame);
    }

    private static void UpdateUOButtonsState(
        Query<Data<UINode, UOButton, UIMouseAction>, Changed<UIMouseAction>> query
    )
    {
        foreach ((var node, var button, var interaction) in query)
        {
            node.Ref.UOConfig.Id = interaction.Ref switch
            {
                { IsPressed: false, WasPressed: false, IsHovered: true } => button.Ref.Over,
                { IsPressed: true, Button: MouseButtonType.Left } => button.Ref.Pressed,
                _ => button.Ref.Normal
            };
        }
    }

    private static void UpdateFocusedInput(
        Query<Data<Text>, Filter<With<TextInput>>> query,
        Res<FocusedInput> focusedInput
    )
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
    }

    private static void ReadCharInputs(
        EventReader<CharInputEvent> reader,
        Res<FocusedInput> focusedInput,
        Query<Data<Text>, Filter<With<TextInput>>> query)
    {
        if (!query.Contains(focusedInput.Value.Entity))
            return;

        (_, var node) = query.Get(focusedInput.Value.Entity);

        foreach (var c in reader)
            node.Ref.Value = TextComposer.Compose(node.Ref.Value, c.Value);
    }

    private static void MoveFocusedElementsByMouse(
        Res<MouseContext> mouseCtx,
        Query<Data<UINode, UIMouseAction>, With<UIMovable>> query
    )
    {
        foreach ((var node, var interaction) in query)
        {
            if (interaction.Ref is { IsPressed: true, Button: MouseButtonType.Left })
            {
                node.Ref.Config.floating.offset.x += mouseCtx.Value.PositionOffset.X;
                node.Ref.Config.floating.offset.y += mouseCtx.Value.PositionOffset.Y;
            }
        }
    }

    private static void HandleNodeStates(
        Res<MouseContext> mouseCtx,
        Res<FocusedInput> focusedInput,
        Query<Data<UINode, UIMouseAction>> query
    )
    {
        var pointerOverIds = Clay.GetPointerOverIds();
        foreach ((var ent, var node, var interaction) in query)
        {
            var isHovered = containsId(in node.Ref.Config.id, pointerOverIds);

            interaction.Ref.WasHovered = interaction.Ref.IsHovered;
            interaction.Ref.IsHovered = isHovered;
            interaction.Ref.WasPressed = interaction.Ref.IsPressed;
            var oldButton = interaction.Ref.Button;

            MouseButtonType? buttonPressed = null;
            for (var button = MouseButtonType.None + 1; button < MouseButtonType.Size; button++)
            {
                if ((isHovered && mouseCtx.Value.IsPressedOnce(button)) ||
                    (interaction.Ref.WasPressed && mouseCtx.Value.IsPressed(button)))
                {
                    buttonPressed = button;
                }
            }

            if (buttonPressed.HasValue)
            {
                interaction.Ref.IsPressed = true;
                interaction.Ref.Button = buttonPressed.Value;
                focusedInput.Value.Entity = ent.Ref; // Set focused input to the current entity
            }
            else
            {
                interaction.Ref.IsPressed = false;
                interaction.Ref.Button = interaction.Ref.WasPressed ? interaction.Ref.Button : MouseButtonType.None;
            }

            var isChanged = interaction.Ref.WasPressed != interaction.Ref.IsPressed ||
                            interaction.Ref.WasHovered != interaction.Ref.IsHovered ||
                            interaction.Ref.Button != oldButton;

            if (isChanged)
            {
                ent.Ref.Set(interaction.Ref);
            }
        }

        static bool containsId(ref readonly Clay_ElementId id, ReadOnlySpan<Clay_ElementId> pointerOverIds)
        {
            foreach (ref readonly var elemId in pointerOverIds)
            {
                if (elemId.id == id.id)
                    return true;
            }

            return false;
        }
    }
}

internal struct UINode
{
    public Clay_ElementDeclaration Config;
    public ClayUOCommandData UOConfig;
}

struct UIMouseAction
{
    public bool WasHovered, IsHovered;
    public bool WasPressed, IsPressed;
    public MouseButtonType Button;
}

struct UIMovable;

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

struct TextFragment;


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

internal sealed class FocusedInput
{
    public ulong Entity { get; set; }
}

internal sealed class ImageCache : Dictionary<nint, Texture2D>;

internal sealed class ClayUOCommandBuffer : List<ClayUOCommandData>;

internal static class GuiPluginEx
{
    /// <summary>
    /// This function sets the UINode & UIMouseAction components
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public static EntityView SetUINode(this EntityView ent, UINode node)
    {
        if (node.Config.id.id == 0)
            node.Config.id = Clay.Id(ent.ID.RealId().ToString());
        return ent.Set(node).Set(new UIMouseAction());
    }
}
