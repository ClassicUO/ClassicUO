using Microsoft.Xna.Framework;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Marker for a UO gump window root. Window-management systems
// (WindowDragPlugin) and the focus-order z key off this. Children of the
// root are NOT tagged — only the window root.
internal struct UOGump;

// Marks an interactive UO gump root that captures the whole bounding box for
// hit-testing (legacy ContainsByBounds) instead of pixel-perfect alpha. The
// GuiPlugin PixelHitTest hook returns true for these so a click on a
// transparent bg slot (e.g. a healthbar's bar cutouts, the status panel's
// see-through frame) still lands on the window. Used by gumps whose whole
// surface is a click target — healthbar (drag/dclick), status bar (click).
internal struct UiContainsByBounds;

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
            .Insert<UIMovable>()
            .Insert(new GlobalZIndex(ZOrder));
    }
}
