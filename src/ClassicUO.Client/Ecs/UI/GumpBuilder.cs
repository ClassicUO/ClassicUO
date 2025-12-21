
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;

namespace ClassicUO.Ecs;

internal unsafe sealed class GumpBuilder
{
    private readonly AssetsServer _assets;
    private readonly ClayUOCommandBuffer _buffer;

    public GumpBuilder(AssetsServer assets, ClayUOCommandBuffer buffer)
    {
        _assets = assets;
        _buffer = buffer;
    }

    public EntityCommands AddLabel(Commands commands, string text, Vector2? position = null, Vector2? size = null)
    {
        var node = ClayNode.Configure()
            .Width(size.HasValue ? size.Value.X : 0)
            .Height(size.HasValue ? size.Value.Y : 0)
            .Floating()
            .FloatingOffset(position?.X ?? 0, position?.Y ?? 0)
            .Text(text, 12, new Clay_Color(255, 255, 255, 255), 0)
            .Build();

        if (node.Floating.HasValue)
        {
            var floating = node.Floating.Value;
            floating.attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE;
            floating.clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT;
            node.Floating = floating;
        }

        var ent = commands.Spawn()
            .InsertBundle(new UOClayUiBundle()
            {
                Node = node
            });

        return ent;
    }

    public EntityCommands AddButton(Commands commands, (ushort normal, ushort pressed, ushort over) ids, Vector3 hue, Vector2? position = null)
    {
        return AddGump(commands, ids.normal, hue, position)
            .Insert(new UOButton() { Normal = ids.normal, Pressed = ids.pressed, Over = ids.over });
    }

    public EntityCommands AddGump(Commands commands, ushort id, Vector3 hue, Vector2? position = null)
    {
        ref readonly var gumpInfo = ref _assets.Gumps.GetGump(id);

        var node = ClayNode.Configure()
            .Width(gumpInfo.UV.Width)
            .Height(gumpInfo.UV.Height)
            .Floating()
            .FloatingOffset(position?.X ?? 0, position?.Y ?? 0)
            .Build();

        if (node.Floating.HasValue)
        {
            var floating = node.Floating.Value;
            floating.attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE;
            floating.clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT;
            node.Floating = floating;
        }

        _buffer.Add(new()
        {
            Type = ClayUOCommandType.Gump,
            Id = id,
            Hue = hue,
        });
        node.Custom = new Clay_CustomElementConfig()
        {
            customData = (void*)(nint)_buffer.Count
        };

        var ent = commands.Spawn()
            .InsertBundle(new UOClayUiBundle()
            {
                Node = node,
                UONodeData = new()
                {
                    Type = ClayUOCommandType.Gump,
                    Id = id,
                    Hue = hue,
                }
            });

        return ent;
    }

    public EntityCommands AddGumpNinePatch(Commands commands, ushort id, Vector3 hue, Vector2? position = null, Vector2? size = null)
    {
        ref readonly var gumpInfo = ref _assets.Gumps.GetGump(id);

        var node = ClayNode.Configure()
            .Width(size.HasValue ? size.Value.X : gumpInfo.UV.Width)
            .Height(size.HasValue ? size.Value.Y : gumpInfo.UV.Height)
            .Floating()
            .FloatingOffset(position?.X ?? 0, position?.Y ?? 0)
            .Build();

        if (node.Floating.HasValue)
        {
            var floating = node.Floating.Value;
            floating.attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE;
            floating.clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT;
            node.Floating = floating;
        }

        _buffer.Add(new()
        {
            Type = ClayUOCommandType.Gump,
            Id = id,
            Hue = hue,
        });
        node.Custom = new Clay_CustomElementConfig()
        {
            customData = (void*)(nint)_buffer.Count
        };

        var ent = commands.Spawn()
           .InsertBundle(new UOClayUiBundle()
           {
               Node = node,
               UONodeData = new()
               {
                   Type = ClayUOCommandType.GumpNinePatch,
                   Id = id,
                   Hue = hue,
               }
           });

        return ent;
    }

    public EntityCommands AddArt(Commands commands, ushort id, Vector3 hue, Vector2? position = null)
    {
        ref readonly var artInfo = ref _assets.Arts.GetArt(id);

        var node = ClayNode.Configure()
            .Width(artInfo.UV.Width)
            .Height(artInfo.UV.Height)
            .Floating()
            .FloatingOffset(position?.X ?? 0, position?.Y ?? 0)
            .Build();

        if (node.Floating.HasValue)
        {
            var floating = node.Floating.Value;
            floating.attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE;
            floating.clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT;
            node.Floating = floating;
        }

        _buffer.Add(new()
        {
            Type = ClayUOCommandType.Gump,
            Id = id,
            Hue = hue,
        });
        node.Custom = new Clay_CustomElementConfig()
        {
            customData = (void*)(nint)_buffer.Count
        };

        var ent = commands.Spawn()
            .InsertBundle(new UOClayUiBundle()
            {
                Node = node,
                UONodeData = new()
                {
                    Type = ClayUOCommandType.Art,
                    Id = id,
                    Hue = hue,
                }
            });

        return ent;
    }
}
