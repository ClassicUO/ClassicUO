using System;
using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Clay;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct GuiPlugin : IPlugin
{
    public void Build(App app)
    {
        // Bevy.UI must already be installed by this plugin: it owns the layout
        // resources/stages and our render plugin reads from UiRenderCommands.
        app.AddPlugin(new UiPlugin
        {
            TextMeasurer = new FontStashTextMeasurer(),
            LogicalSize = new System.Numerics.Vector2(1280, 720),
            MaxElements = 8192,
        });

        app
            .AddResource(new FocusedInput())
            .AddResource(new ImageCache())
            .AddResource(new UIScale())

            .AddSystem(Stage.Startup, (Commands commands, Res<AssetsServer> assets) =>
            {
                commands.InsertResource(new GumpBuilder(assets.Value));
            });

        var states = Enum.GetValues<GameState>();
        foreach (var state in states)
        {
            app.AddSystem((Res<FocusedInput> focusedInput) => focusedInput.Value.Entity = 0)
                .OnExit(state)
                .Build();
        }

        // Sync FNA surface size + pointer state into Bevy.UI before the layout stage.
        Action<Res<GraphicsDevice>, Res<UoGame>, ResMut<UiSurface>> syncSurfaceFn = SyncSurface;
        Action<Res<MouseContext>, Res<UoGame>, ResMut<UiPointer>> syncPointerFn = SyncPointer;
        Action<Res<Time>, Res<MouseContext>, ResMut<UiClayContext>> syncDeltaAndScrollFn = SyncDeltaAndScroll;
        Action<Query<Data<UOCustomRender, UOButton, Interaction>>> updateUOButtonsStateFn = UpdateUOButtonsState;
        Action<Query<Data<Text, MaskedText>, Filter<Changed<MaskedText>>>> syncMaskedTextFn = SyncMaskedText;

        app.AddSystem(Stage.First, syncSurfaceFn);
        app.AddSystem(Stage.First, syncPointerFn);
        app.AddSystem(Stage.First, syncDeltaAndScrollFn);
        // Must run after InteractionSystem.PostLayout writes Hovered/Pressed,
        // before UiRenderStage reads UOCustomRender.AssetId.
        app.AddSystem(updateUOButtonsStateFn)
            .InStage(UiPlugin.UiPostLayoutStage)
            .Build();
        // Mirror MaskedText.Value into Text as mask chars before layout reads Text.
        app.AddSystem(Stage.PreUpdate, syncMaskedTextFn);
    }

    private static void SyncSurface(
        Res<GraphicsDevice> device,
        Res<UoGame> game,
        ResMut<UiSurface> surface)
    {
        var pp = device.Value.PresentationParameters;
        var dpi = game.Value.DpiScale;
        if (dpi <= 0f) dpi = 1f;
        // Clay lays out in LOGICAL pixels (UO's native UI grid). Render pass
        // applies CreateScale(DpiScale) so layout fills the physical backbuffer
        // — mirrors main's RenderTargets pipeline.
        surface.Value.LogicalSize = new System.Numerics.Vector2(pp.BackBufferWidth / dpi, pp.BackBufferHeight / dpi);
        surface.Value.PhysicalSize = new System.Numerics.Vector2(pp.BackBufferWidth, pp.BackBufferHeight);
    }

    private static void SyncPointer(
        Res<MouseContext> mouseCtx,
        Res<UoGame> game,
        ResMut<UiPointer> pointer)
    {
        // MouseContext.Position is already LOGICAL pixels (see MouseContext.cs).
        // Clay layouts in logical too, so feed it through unchanged for both
        // real-mouse and AGENT_BUILD synthetic paths.
        var p = mouseCtx.Value.Position;
        pointer.Value.Position = new System.Numerics.Vector2(p.X, p.Y);
        pointer.Value.Down = mouseCtx.Value.IsPressed(MouseButtonType.Left);
        // WasDown is latched by InteractionSystem.PostLayout.
    }

    private static void SyncDeltaAndScroll(
        Res<Time> time,
        Res<MouseContext> mouseCtx,
        ResMut<UiClayContext> ctx)
    {
        ctx.Value.DeltaTime = time.Value.Frame;
        ctx.Value.ScrollDelta = new System.Numerics.Vector2(0, mouseCtx.Value.Wheel * 3);
        ctx.Value.EnableDragScrolling = false;
    }

    private static void UpdateUOButtonsState(
        Query<Data<UOCustomRender, UOButton, Interaction>> query)
    {
        foreach (var (custom, button, interaction) in query)
        {
            custom.Ref.AssetId = interaction.Ref switch
            {
                Interaction.Pressed => button.Ref.Pressed,
                Interaction.Hovered => button.Ref.Over,
                _ => button.Ref.Normal,
            };
        }
    }

    private static void SyncMaskedText(
        Query<Data<Text, MaskedText>, Filter<Changed<MaskedText>>> query)
    {
        foreach (var (text, masked) in query)
        {
            var value = masked.Ref.Value ?? string.Empty;
            text.Ref.Value = value.Length == 0
                ? string.Empty
                : new string(masked.Ref.MaskChar, value.Length);
        }
    }
}

// FontStashSharp-backed text measurer for Bevy.UI / Clay.
internal sealed class FontStashTextMeasurer : ITextMeasurer
{
    private const FontSystemEffect FONT_EFFECT = FontSystemEffect.Stroked;
    private const int FONT_EFFECT_AMOUNT = 1;

    public Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing)
    {
        if (text.IsEmpty)
            return new Dimensions(0, fontSize);

        var font = FontCache.GetFont(fontId);
        var dynFont = font.GetFont(fontSize);
        // FontStashSharp's MeasureString doesn't take ReadOnlySpan<char>; pay the
        // allocation here. Layout pass calls this once per text node per frame.
        var size = dynFont.MeasureString(
            text.ToString(),
            characterSpacing: letterSpacing,
            effect: FONT_EFFECT, effectAmount: FONT_EFFECT_AMOUNT);
        return new Dimensions(size.X, size.Y);
    }
}

// UO-specific render marker stored as a component on a UI entity. The render
// system maps Clay's element id -> entity at render time and pulls this struct.
internal enum UOCustomKind : byte
{
    Gump,
    GumpNinePatch,
    GumpTiled,
    Art,
    Land,
    Animation,
}

internal struct UOCustomRender
{
    public UOCustomKind Kind;
    public uint AssetId;
    public Vector3 Hue;
    // When true the renderer draws the same sprite a second time at +5/+5
    // to mirror legacy ItemGump.Draw's stacked-item visual (Amount > 1 &&
    // ItemData.IsStackable).
    public bool Stacked;
}

// Marker for the UO button widget. UpdateUOButtonsState rewrites the visible
// asset id on the entity's UOCustomRender based on Interaction state.
internal struct UOButton
{
    public ushort Normal, Pressed, Over;
}

// Marker tags carried over from the old plugin.
internal struct UIMovable;
internal struct TextInput;

// Real text storage for masked fields (passwords). SyncMaskedText keeps
// the sibling `Text` component populated with `MaskChar` repeated for the
// length of `Value`. Editors and consumers (e.g. login) read/write Value;
// the renderer only ever sees the masked Text.
internal struct MaskedText
{
    public string Value;
    public char MaskChar;
}

internal sealed class FocusedInput
{
    public ulong Entity { get; set; }
}

internal sealed class ImageCache : Dictionary<nint, Texture2D>;
