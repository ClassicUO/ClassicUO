using Microsoft.Xna.Framework;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Marker for a UO gump window root. Window-management systems
// (WindowDragPlugin) and the focus-order z key off this. Children of the
// root are NOT tagged — only the window root.
internal struct UOGump;

// Spawns a UO gump window in one bundle. By design a UOGump:
//   * is positioned absolutely (UO gumps float, never flow),
//   * carries its focus order on the ROOT only (GlobalZIndex); children
//     inherit that z via LayoutSystem, so there is no per-sprite z and no
//     propagation system,
//   * is closable by right-click and movable by drag — both fall out of the
//     UIMovable marker that WindowDragPlugin already drives.
// The background sprite renders through UOCustomRender (Gump kind).
internal struct UOGumpBundle : IBundle
{
    public Vector2 Position;
    public Vector2 Size;
    public ushort BackgroundId;
    public Vector3 Hue;
    public int ZOrder;

    public readonly void Insert(EntityCommands entity)
    {
        entity
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(Position.X),
                Top = Val.Px(Position.Y),
                Width = Val.Px(Size.X),
                Height = Val.Px(Size.Y),
            })
            .Insert(new UiCustom())
            .Insert(new UOCustomRender
            {
                Kind = UOCustomKind.Gump,
                AssetId = BackgroundId,
                Hue = Hue,
            })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UIMovable>()
            .Insert(new GlobalZIndex(ZOrder));
    }
}
