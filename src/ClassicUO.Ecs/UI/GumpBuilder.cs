using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
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
                }
            });
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
