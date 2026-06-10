using Microsoft.Xna.Framework;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Marker for a UO gump window root. Window-management systems
// (WindowDragPlugin) and the focus-order z key off this. Children of the
// root are NOT tagged — only the window root.
internal struct UOGump;

// UiContainsByBounds (legacy ContainsByBounds — whole bbox is the hit target,
// e.g. healthbar cutouts, status panel's see-through frame) now lives in
// TinyEcs.Bevy.UI: InteractionSystem honors it before the PixelHitTest hook;
// UiPick consults the same marker on its raw-mouse path.

// Spawns a UO gump window in one bundle. By design a UOGump:
//   * is positioned absolutely (UO gumps float, never flow),
//   * carries its focus order on the ROOT only (GlobalZIndex); children
//     inherit that z via LayoutSystem, so there is no per-sprite z and no
//     propagation system,
//   * is closable by right-click and movable by drag — both fall out of the
//     UiMovable marker that WindowDragPlugin already drives.
// The background sprite renders through UOCustomRender (Gump kind).
internal struct UOGumpBundle : IBundle
{
    public Vector2 Position;
    public Vector2 Size;
    public ushort BackgroundId;
    public Vector3 Hue;
    public int ZOrder;
    // Kind selects the background sprite shape. Server gumps (resizepic) use
    // GumpNinePatch — single-sprite paperdoll/container windows use Gump.
    public UOCustomKind Kind;

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
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = Kind == default ? UOCustomKind.Gump : Kind,
                    AssetId = BackgroundId,
                    Hue = Hue,
                }
            })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(ZOrder));
    }
}
