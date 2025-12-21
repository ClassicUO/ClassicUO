using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Clay_cs;
using Flexbox;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;

namespace ClassicUO.Ecs;

internal readonly struct GuiPlugin : IPlugin
{
    private const FontSystemEffect FONT_EFFECT = FontSystemEffect.Stroked;
    private const int FONT_EFFECT_AMOUNT = 1;

    public unsafe void Build(App app)
    {
        var updateInputStateFn = UpdateInputState;
        var updateUOButtonStateFn = UpdateUOButtonsState;
        // var updateFocusedInputFn = UpdateFocusedInput;
        // var readCharInputsFn = ReadCharInputs;
        // var moveFocusedElementsByMouseFn = MoveFocusedElementsByMouse;
        // var handleNodeStatesFn = HandleNodeStates;

        // Add Clay UI plugin with text measurement function
        var clayOptions = new ClayUiOptions
        {
            LayoutDimensions = new Clay_Dimensions(800, 600),
            ArenaSize = 1024 * 1024 * 2, // 2MB for UI
            EnableDebugMode = true,
            EnableCulling = true,
            MeasureTextFunction = OnMeasureText,
            ErrorHandler = OnClayError
        };

        app.AddClayUi(clayOptions);

        app
            .AddResource(new ClayUOCommandBuffer())
            .AddResource(new FocusedInput())
            .AddResource(new ImageCache())

            // Initialize ClayElementId when UINode is added to an entity
            // .AddObserver((OnAdd<UINode> trigger, Commands commands) =>
            // {
            //     var entityId = trigger.EntityId;
            //     commands.Entity(entityId).Insert(new ClayElementId(Clay.Id(entityId.ToString())));
            // })

            .AddSystem(Stage.Startup, (Commands commands, Res<AssetsServer> assets, Res<ClayUOCommandBuffer> buffer) =>
            {
                commands.InsertResource(new GumpBuilder(assets.Value, buffer.Value));
            });

        var states = Enum.GetValues<GameState>();

        foreach (var state in states)
        {
            app.AddSystem((Res<FocusedInput> focusedInput) => focusedInput.Value.Entity = 0)
                .OnExit(state)
                .Build();
        }

        app
            .AddSystem(Stage.First, updateInputStateFn)
            .AddSystem(Stage.Update, updateUOButtonStateFn)

            // .AddSystem(Stage.Update, updateUOButtonsStateFn)

            // .AddSystem(updateFocusedInputFn)
            // .InStage(Stage.Update)
            // .RunIf((Res<KeyboardContext> keyboardCtx) => keyboardCtx.Value.IsPressedOnce(Microsoft.Xna.Framework.Input.Keys.Tab))
            // .Build()

            // .AddSystem(readCharInputsFn)
            // .InStage(Stage.Update)
            // .RunIf((EventReader<CharInputEvent> reader,
            //         Res<FocusedInput> focusedInput,
            //         Query<Data<UINode>, Filter<With<TextInput>>> query) =>
            //            reader.HasEvents && focusedInput.Value.Entity != 0 && query.Count() > 0)
            // .Build()

            // .AddSystem(Stage.Update, moveFocusedElementsByMouseFn)
            // .AddSystem(Stage.Last, handleNodeStatesFn)
            ;
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

    private static void OnClayError(Clay_ErrorData errorData)
    {
        unsafe
        {
            var raw = new ReadOnlySpan<byte>(errorData.errorText.chars, errorData.errorText.length);
            var text = Encoding.UTF8.GetString(raw);
            Console.WriteLine($"Clay error: {errorData.errorType} - {text}");
        }
    }

    private static void UpdateUOButtonsState(
        Query<Data<ClayUOCommandData, UOButton, ClayInteraction>, Changed<ClayInteraction>> query
    )
    {
        foreach (var (data, button, interaction) in query)
        {
            data.Ref.Id = interaction.Ref switch
            {
                { State: ClayInteractionState.Pressed } => button.Ref.Pressed,
                { State: ClayInteractionState.Hovered } => button.Ref.Over,
                _ => button.Ref.Normal
            };
        }
    }

    private static void UpdateInputState(
        Res<GraphicsDevice> device,
        Res<MouseContext> mouseCtx,
        Res<Time> time,
        ResMut<ClayUiState> uiState,
        ResMut<ClayPointerState> pointerState,
        ResMut<ClayTextInputState> textInputState,
        EventReader<CharInputEvent> charInputReader
    )
    {
        // Update layout dimensions
        uiState.Value.LayoutDimensions = new Clay_Dimensions
        {
            width = device.Value.PresentationParameters.BackBufferWidth,
            height = device.Value.PresentationParameters.BackBufferHeight
        };

        // Update pointer state
        var wasPressed = pointerState.Value.IsLeftDown;
        var isPressed = mouseCtx.Value.IsPressed(MouseButtonType.Left);

        pointerState.Value.Position = new(mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y);
        pointerState.Value.IsLeftDown = isPressed;
        pointerState.Value.IsLeftPressed = mouseCtx.Value.IsPressedOnce(MouseButtonType.Left);
        pointerState.Value.IsLeftReleased = wasPressed && !isPressed;
        pointerState.Value.ScrollDelta = new(0, mouseCtx.Value.Wheel * 3);
        pointerState.Value.DeltaTime = time.Value.Frame;
        pointerState.Value.EnableDragScrolling = false;

        // Update text input state
        foreach (var charEvent in charInputReader.Read())
        {
            textInputState.Value.AddChar(charEvent.Value);
        }
    }

    // private static void UpdateFocusedInput(
    //     Query<Data<Text>, Filter<With<TextInput>>> query,
    //     Res<FocusedInput> focusedInput
    // )
    // {
    //     var ok = false;
    //     var last = 0ul;
    //     foreach ((var ent, var textInput) in query)
    //     {
    //         if (focusedInput.Value.Entity == ent.Ref)
    //         {
    //             ok = true;
    //             continue;
    //         }

    //         if (ok)
    //             last = ent.Ref;
    //     }

    //     if (ok && last == 0)
    //     {
    //         foreach ((var ent, var textInput) in query)
    //         {
    //             last = ent.Ref;
    //             break;
    //         }
    //     }

    //     if (last != 0)
    //     {
    //         focusedInput.Value.Entity = last;
    //     }
    // }

    // private static void ReadCharInputs(
    //     EventReader<CharInputEvent> reader,
    //     Res<FocusedInput> focusedInput,
    //     Query<Data<Text>, Filter<With<TextInput>>> query)
    // {
    //     if (!query.Contains(focusedInput.Value.Entity))
    //         return;

    //     (_, var node) = query.Get(focusedInput.Value.Entity);

    //     foreach (var c in reader.Read())
    //         node.Ref.Value = TextComposer.Compose(node.Ref.Value, c.Value);
    // }
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

internal struct UOClayUiBundle : IBundle
{
    public ClayNode Node { get; init; }
    public ClayUOCommandData UONodeData { get; init; }

    public void Insert(EntityView entity)
    {
        entity.Set(Node);
        entity.Set(UONodeData);
    }

    public void Insert(EntityCommands entity)
    {
        entity.Insert(Node);
        entity.Insert(UONodeData);
    }
}

internal struct UIMovable;
internal struct TextInput;

internal sealed class FocusedInput
{
    public ulong Entity { get; set; }
}

internal sealed class ImageCache : Dictionary<nint, Texture2D>;

internal sealed class ClayUOCommandBuffer : List<ClayUOCommandData>;
