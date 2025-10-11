
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal sealed class GumpBuilder
{
    private readonly AssetsServer _assets;

    public GumpBuilder(AssetsServer assets)
    {
        _assets = assets;
    }

    public EntityCommands AddLabel(Commands commands, string text, Vector2? position = null, Vector2? size = null)
    {
        var ent = commands.Spawn()
            .CreateUINode(new UINode()
            {
                Config = {
                    // id = Clay.Id(ent.ID.ToString()),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.X : 0),
                            height = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.Y : 0),
                        }
                    },
                    floating = {
                        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                        attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
                        offset = {
                            x = position?.X ?? 0,
                            y = position?.Y ?? 0
                        }
                    }
                }
            }).Insert(new Text()
            {
                Value = text,
                TextConfig = {
                    fontId = 0,
                    fontSize = 12,
                    textColor = new (255, 255, 255, 255),
                },
            });

        return ent;
    }

    public EntityCommands AddButton(Commands commands, (ushort normal, ushort pressed, ushort over) ids, Vector3 hue, Vector2? position = null)
    {
        return AddGump(commands, ids.normal, hue, position)
            .Insert(new UIMouseAction())
            .Insert(new UOButton() { Normal = ids.normal, Pressed = ids.pressed, Over = ids.over });
    }

    public EntityCommands AddGump(Commands commands, ushort id, Vector3 hue, Vector2? position = null)
    {
        ref readonly var gumpInfo = ref _assets.Gumps.GetGump(id);
        var ent = commands.Spawn()
            .CreateUINode(new UINode()
            {
                Config = {
                    // id = Clay.Id(ent.ID.ToString()),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(gumpInfo.UV.Width),
                            height = Clay_SizingAxis.Fixed(gumpInfo.UV.Height),
                        }
                    },
                    floating = {
                        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                        attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
                        offset = {
                            x = position?.X ?? 0,
                            y = position?.Y ?? 0
                        }
                    }
                },
                UOConfig = {
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
        var ent = commands.Spawn()
            .CreateUINode(new UINode()
            {
                Config = {
                    // id = Clay.Id(ent.ID.ToString()),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.X : gumpInfo.UV.Width),
                            height = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.Y : gumpInfo.UV.Height),
                        }
                    },
                    floating = {
                        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                        attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
                        offset = {
                            x = position?.X ?? 0,
                            y = position?.Y ?? 0
                        }
                    }
                },
                UOConfig = {
                    Type = ClayUOCommandType.GumpNinePatch,
                    Id = id,
                    Hue = hue,
                }
            });
        return ent;
    }
}
