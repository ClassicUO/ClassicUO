using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

/// Spawns ClassicUO-flavoured UI entities backed by Bevy.UI's Node/Interaction model.
/// Each helper returns an EntityCommands so callers can chain .Insert(...) /
/// .AddChild(...). Positioning uses absolute (floating) placement to mimic the
/// behaviour of the old gump/Clay-cs path.
internal sealed class GumpBuilder
{
    private readonly AssetsServer _assets;

    public GumpBuilder(AssetsServer assets)
    {
        _assets = assets;
    }

    /// Spawn a floating text label. If position is supplied, the node is placed
    /// absolutely relative to its parent at that offset.
    public EntityCommands AddLabel(Commands commands, string text, Vector2? position = null, Vector2? size = null)
    {
        var node = MakeFloatingNode(position, size, autoFit: !size.HasValue);

        return commands.Spawn()
            .Insert(node)
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = 0, Size = 12 })
            .Insert(new TextColor(ClayColor.White));
    }

    /// Spawn a UO button. The visible asset id is rewritten by the GuiPlugin
    /// in reaction to Interaction state changes (normal/over/pressed).
    public EntityCommands AddButton(Commands commands, (ushort normal, ushort pressed, ushort over) ids, Vector3 hue, Vector2? position = null)
    {
        return AddGump(commands, ids.normal, hue, position)
            .Insert(new UOButton { Normal = ids.normal, Pressed = ids.pressed, Over = ids.over })
            .Insert(Interaction.None)
            .Insert(new Button());
    }

    /// Spawn a draggable, stackable, right-click-closable, click-capturing
    /// gump window root. Mirrors main's Game/UI/Controls/Gump.cs defaults
    /// (CanMove + CanCloseWithRightClick on every gump). Caller is responsible
    /// for AddChild'ing controls onto the returned entity. zCounter.Bump()
    /// sets the initial focus z so the new window draws on top.
    public EntityCommands AddGumpRoot(Commands commands, ushort id, Vector3 hue, Vector2 position, UiZCounter zCounter)
    {
        ref readonly var gumpInfo = ref _assets.Gumps.GetGump(id);
        var size = new Vector2(gumpInfo.UV.Width, gumpInfo.UV.Height);

        return commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(position.X),
                Top = Val.Px(position.Y),
                Width = Val.Px(size.X),
                Height = Val.Px(size.Y),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = id,
                    Hue = hue,
                }
            })
            .Insert(Interaction.None)
            .Insert<UIMovable>()
            .Insert(new GlobalZIndex(zCounter.Bump()));
    }

    /// Spawn a UO gump window via UOGumpBundle. Resolves the background sprite
    /// size from assets and stamps the focus z on the root only — children
    /// added under it inherit that z at layout time (no propagation). Movable +
    /// right-click-close come from the UIMovable marker the bundle carries.
    public EntityCommands SpawnUOGump(Commands commands, ushort bgId, Vector3 hue, Vector2 position, UiZCounter zCounter)
    {
        ref readonly var gumpInfo = ref _assets.Gumps.GetGump(bgId);
        return commands.SpawnBundle(new UOGumpBundle
        {
            Position = position,
            Size = new Vector2(gumpInfo.UV.Width, gumpInfo.UV.Height),
            BackgroundId = bgId,
            Hue = hue,
            ZOrder = zCounter.Bump(),
        });
    }

    /// Spawn a single gump sprite at the given position.
    public EntityCommands AddGump(Commands commands, ushort id, Vector3 hue, Vector2? position = null)
    {
        ref readonly var gumpInfo = ref _assets.Gumps.GetGump(id);
        var size = new Vector2(gumpInfo.UV.Width, gumpInfo.UV.Height);
        var node = MakeFloatingNode(position, size);

        return commands.Spawn()
            .Insert(node)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = id,
                    Hue = hue,
                }
            });
    }

    /// Spawn a tiled gump (single sprite tiled to fill the given size).
    /// Used for backgrounds like LoginBackground's 0x0150 wallpaper.
    public EntityCommands AddGumpTiled(Commands commands, ushort id, Vector3 hue, Vector2 position, Vector2 size)
    {
        var node = MakeFloatingNode(position, size);
        return commands.Spawn()
            .Insert(node)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.GumpTiled,
                    AssetId = id,
                    Hue = hue,
                }
            });
    }

    /// Spawn a sub-rectangle slice of a gump sprite (legacy GumpPicTexture).
    /// `src` selects the source region; `tiled` tiles it across the node box,
    /// otherwise it's drawn once at the box origin. Node size = `size`.
    public EntityCommands AddGumpSlice(Commands commands, ushort id, Vector3 hue, Vector2 position, Vector2 size, Rectangle src, bool tiled)
    {
        var node = MakeFloatingNode(position, size);
        return commands.Spawn()
            .Insert(node)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.GumpSlice,
                    AssetId = id,
                    Hue = hue,
                    SrcX = (ushort)src.X,
                    SrcY = (ushort)src.Y,
                    SrcW = (ushort)src.Width,
                    SrcH = (ushort)src.Height,
                    SliceTiled = tiled,
                }
            });
    }

    /// Spawn a nine-patch (scalable) gump. The supplied size overrides the
    /// natural sprite size.
    public EntityCommands AddGumpNinePatch(Commands commands, ushort id, Vector3 hue, Vector2? position = null, Vector2? size = null)
    {
        ref readonly var gumpInfo = ref _assets.Gumps.GetGump(id);
        var resolved = size ?? new Vector2(gumpInfo.UV.Width, gumpInfo.UV.Height);
        var node = MakeFloatingNode(position, resolved);

        return commands.Spawn()
            .Insert(node)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.GumpNinePatch,
                    AssetId = id,
                    Hue = hue,
                }
            });
    }

    /// Spawn an item/art sprite at the given position.
    public EntityCommands AddArt(Commands commands, ushort id, Vector3 hue, Vector2? position = null)
    {
        ref readonly var artInfo = ref _assets.Arts.GetArt(id);
        var size = new Vector2(artInfo.UV.Width, artInfo.UV.Height);
        var node = MakeFloatingNode(position, size);

        return commands.Spawn()
            .Insert(node)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.Art,
                    AssetId = id,
                    Hue = hue,
                }
            });
    }

    /// Spawn an item/art sprite clamped to an explicit box. The renderer
    /// scales art larger than the box down to fit (preserving aspect, never
    /// upscaling) and centers it — used for equipment-slot item icons.
    public EntityCommands AddArtSized(Commands commands, ushort id, Vector3 hue, Vector2 position, Vector2 size)
    {
        var node = MakeFloatingNode(position, size);
        return commands.Spawn()
            .Insert(node)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.Art,
                    AssetId = id,
                    Hue = hue,
                    ArtRealBounds = true,
                }
            });
    }

    /// Default UO checkbox graphics (classic empty / checked box).
    public const ushort CheckboxOff = 0x00D2;
    public const ushort CheckboxOn = 0x00D3;
    /// Classic vertical scrollbar thumb + background gump ids (ScrollBar.cs).
    public const ushort ScrollThumb = 254;
    public const ushort ScrollBackground = 256;
    /// Classic horizontal slider knob gump id (HSliderBar.cs default style).
    public const ushort SliderKnob = 0x845;

    /// Spawn a UO checkbox. CheckboxPlugin toggles the sibling Checkbox.Checked
    /// on click + fires CheckboxChanged (observe it for the value); the GuiPlugin
    /// swaps the Off/On sprite to match. Caller positions a label separately.
    public EntityCommands AddCheckbox(Commands commands, bool isChecked, Vector2? position = null, ushort off = CheckboxOff, ushort on = CheckboxOn, Vector3 hue = default)
    {
        return AddGump(commands, isChecked ? on : off, hue, position)
            .Insert(new UOCheckbox { Off = off, On = on })
            .Insert(new Checkbox { Checked = isChecked })
            .Insert(Interaction.None);
    }

    /// Spawn a vertical scrollbar bound to a scrollable container (an entity with
    /// Overflow.Scroll + ScrollPosition). Builds the track (tiled background) +
    /// a draggable thumb child; ScrollbarPlugin positions/drags the thumb from
    /// the target's scroll data. Returns the track entity.
    public EntityCommands AddVScrollbar(Commands commands, ulong target, Vector2 position, float height, Vector3 hue = default)
    {
        ref readonly var thumbInfo = ref _assets.Gumps.GetGump(ScrollThumb);
        var width = thumbInfo.UV.Width;

        var track = commands.Spawn()
            .Insert(MakeFloatingNode(position, new Vector2(width, height)))
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.GumpTiled, AssetId = ScrollBackground, Hue = hue } })
            .Insert(new Scrollbar { Target = target, Orientation = ScrollbarOrientation.Vertical, MinThumbLength = 16f })
            .Insert(Interaction.None)
            // Inside a draggable window the thumb/track press must drive the
            // scrollbar, not latch a window move.
            .Insert<UINoWindowDrag>();

        var thumb = commands.Spawn()
            .Insert(MakeFloatingNode(new Vector2(0, 0), new Vector2(width, thumbInfo.UV.Height)))
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = ScrollThumb, Hue = hue } })
            .Insert(new ScrollbarThumb())
            .Insert(new ScrollbarDragState())
            .Insert(Interaction.None)
            .Insert<UINoWindowDrag>();

        commands.AddChild(track.Id, thumb.Id);
        return track;
    }

    /// Spawn a horizontal slider over [min,max]. Builds the track + a draggable
    /// knob child; SliderPlugin positions/drags the knob and fires SliderChanged.
    /// Returns the track entity (carries the Slider component to read Value).
    /// thumbGump != 0 renders the knob as that gump sprite (sized from the asset)
    /// instead of the default solid-color rect.
    public EntityCommands AddHSlider(Commands commands, float min, float max, float value, Vector2 position, float width, Vector3 hue = default, ushort thumbGump = 0)
    {
        int knobW = 12, knobH = 16;
        if (thumbGump != 0)
        {
            ref readonly var ti = ref _assets.Gumps.GetGump(thumbGump);
            knobW = ti.UV.Width; knobH = ti.UV.Height;
        }

        // The track is an invisible custom LEAF (UOCustomKind.None: emits a Custom
        // command, draws nothing, gets a ComputedNode + bbox hit-test). A leaf keeps
        // its declared Px width; a plain container would FIT to its single floating
        // child and collapse to the thumb width, leaving the thumb zero travel. The
        // thumb is positioned absolutely by SliderPlugin (it does not rely on Clay to
        // flow it), exactly like the scrollbar thumb under its GumpTiled-leaf track.
        var track = commands.Spawn()
            .Insert(MakeFloatingNode(position, new Vector2(width, knobH)))
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None } })
            .Insert(new Slider { Min = min, Max = max, Value = value, ThumbLength = knobW, Orientation = ScrollbarOrientation.Horizontal })
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            // A slider lives inside movable gump windows; without this a press to
            // drag the knob would latch the whole window instead.
            .Insert<UINoWindowDrag>();

        // SliderPlugin overwrites Width/Height/Left each frame. Default knob is a
        // solid-color rect; thumbGump renders the classic gump sprite instead.
        var knob = commands.Spawn()
            .Insert(MakeFloatingNode(new Vector2(0, 0), new Vector2(knobW, knobH)))
            .Insert(new SliderThumb())
            .Insert(new SliderDragState())
            .Insert(Interaction.None)
            .Insert<UINoWindowDrag>();

        if (thumbGump != 0)
            // Z is alpha; a zero hue vector draws fully transparent. Default to opaque.
            // UiContainsByBounds: a UiCustom leaf is otherwise hit-tested pixel-perfect
            // (GuiPlugin.PixelHitTest); bounds-hit keeps the knob fully draggable like
            // the solid-rect thumb (which had no UiCustom and so was always hittable).
            knob
                .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = thumbGump, Hue = hue == default ? Vector3.UnitZ : hue } })
                .Insert<UiContainsByBounds>();
        else
            knob.Insert(new BackgroundColor(new ClayColor(210, 215, 225, 255)));

        commands.AddChild(track.Id, knob.Id);
        return track;
    }

    private static Node MakeFloatingNode(Vector2? position, Vector2? size, bool autoFit = false)
    {
        var node = Node.Default;

        if (size.HasValue)
        {
            node.Width = Val.Px(size.Value.X);
            node.Height = Val.Px(size.Value.Y);
        }
        else if (autoFit)
        {
            // Default: let Clay fit-to-content.
            node.Width = Val.Auto;
            node.Height = Val.Auto;
        }

        if (position.HasValue)
        {
            node.PositionType = PositionType.Absolute;
            node.Left = Val.Px(position.Value.X);
            node.Top = Val.Px(position.Value.Y);
        }

        return node;
    }
}
