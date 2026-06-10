// A small host-side "Options" test window opened from the top bar's Debug
// button. Not a UO gump — just a draggable panel of labelled buttons used to
// drive client-local features that have no in-world trigger yet (e.g. the dye
// ColorPicker). Buttons are BackgroundColor rows (no gump assets) so the window
// always renders regardless of the loaded dataset.

using System;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct OptionsGumpPlugin : IPlugin
{
    private const int Width = 220;
    private const int Height = 168;

    private static readonly (string Label, Action<Commands, OptionsButtonParams> OnClick)[] s_rows =
    {
        ("Open Color Picker", static (cmd, p) =>
            ColorPickerPlugin.Open(cmd, p.Assets.Value, p.Builder.Value, p.Files.Value.Hues,
                p.ZCounter.Value, p.Surface.Value, serial: 0, graphic: 0x0FAB)),
        ("Demo Button (logs)", static (_, _) => Console.WriteLine("[Options] demo button clicked")),
    };

    public void Build(App app)
    {
        Action<Commands, Query<Data<OptionsWindow>>> teardownFn =
            (cmd, q) => { foreach (var (ent, _) in q) cmd.Entity(ent.Ref).Despawn(); };
        app.AddSystem(teardownFn).OnExit(GameState.GameScreen).Build();
    }

    internal static void OpenOrFocus(Commands commands, UiZCounter zc, UiSurface surface, Query<Data<OptionsWindow>> existing)
    {
        foreach (var (ent, _) in existing)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zc.Bump()));
            return;
        }

        var z = zc.Bump();
        var cx = MathF.Max(0, (surface.LogicalSize.X - Width) * 0.5f);
        var cy = MathF.Max(0, (surface.LogicalSize.Y - Height) * 0.5f);

        var rootId = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(cx), Top = Val.Px(cy),
                Width = Val.Px(Width), Height = Val.Px(Height),
            })
            .Insert(new BackgroundColor(new ClayColor(34, 34, 40, 235)))
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(z))
            .Insert(new OptionsWindow())
            .Id;

        // Title.
        commands.AddChild(rootId, commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(12), Top = Val.Px(8),
                Width = Val.Px(Width - 24), Height = Val.Px(20),
            })
            .Insert(new Text("Options"))
            .Insert(new TextFont { FontId = 1, Size = 14 })
            .Insert(new TextColor(new ClayColor(255, 255, 255, 255)))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Id);

        for (var i = 0; i < s_rows.Length; i++)
        {
            var onClick = s_rows[i].OnClick;
            var row = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(12), Top = Val.Px(36 + i * 40),
                    Width = Val.Px(Width - 24), Height = Val.Px(32),
                })
                .Insert(new BackgroundColor(new ClayColor(70, 72, 84, 255)))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            // Click fires on press+release inside the row.
            row.Observe((On<UiClick> _, Commands cmd, OptionsButtonParams p) => onClick(cmd, p));

            // Caption (non-interactive child so clicks bubble to the row).
            commands.AddChild(row.Id, commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(10), Top = Val.Px(8),
                    Width = Val.Px(Width - 44), Height = Val.Px(18),
                })
                .Insert(new Text(s_rows[i].Label))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(new ClayColor(255, 255, 255, 255)))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Id);

            commands.AddChild(rootId, row.Id);
        }
    }
}

internal struct OptionsWindow;

// Resources a row's click handler may need (the Color Picker row opens the dye
// window, which needs the asset/hue/layout services).
internal sealed class OptionsButtonParams : CompositeSystemParam
{
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<UOFileManager> Files;
    public readonly Res<UiZCounter> ZCounter;
    public readonly Res<UiSurface> Surface;

    public OptionsButtonParams()
    {
        Assets   = Add(new Res<AssetsServer>());
        Builder  = Add(new Res<GumpBuilder>());
        Files    = Add(new Res<UOFileManager>());
        ZCounter = Add(new Res<UiZCounter>());
        Surface  = Add(new Res<UiSurface>());
    }
}
