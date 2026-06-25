using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Shared boilerplate for the login-flow scenes (login / server-select /
// character-select / character-create / login-error). Each scene tags every
// entity it spawns with its own scene-root marker (LoginScene, …) so a single
// DespawnAll<TMarker> tears the whole tree down; canvas + wallpaper are byte-
// identical across the fixed-640x480 screens, so they live here.
internal static class LoginSceneHelpers
{
    // Despawn every entity carrying the scene-root marker (children cascade).
    // Replaces the per-scene Cleanup/DeleteMenu/Teardown loop.
    internal static void DespawnAll<TMarker>(
        Commands commands, Query<Data<Node>, Filter<With<TMarker>>> query)
        where TMarker : struct
    {
        foreach (var (ent, _) in query)
            commands.Entity(ent.Ref).Despawn();
    }

    // Full-screen relative root + the chest's fixed 640x480 menu canvas, both
    // tagged with the scene marker (root.AddChild(mainMenu) wired internally).
    // Returns mainMenu — the parent for all chrome; absolute child positions are
    // UO logical pixels measured from (0,0) inside it (same coord space as main's
    // login gumps). EntityCommands is a ref struct, so this can't return a tuple;
    // the root is parentless and never needed by callers.
    internal static EntityCommands SpawnCanvas<TMarker>(Commands commands)
        where TMarker : struct
    {
        var root = commands.Spawn()
            .Insert<TMarker>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Percent(100),
                Height = Val.Percent(100),
            });

        var mainMenu = commands.Spawn()
            .Insert<TMarker>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Px(640),
                Height = Val.Px(480),
            });
        root.AddChild(mainMenu);

        return mainMenu;
    }

    // Tiled wallpaper 0x0150 + UO flag 0x0151 (main's LoginBackground, CV>=706400
    // branch), parented under the given node and tagged with the scene marker.
    // size lets the login-error backdrop tile to the surface size; the fixed
    // screens pass (640, 480).
    internal static void AddWallpaper<TMarker>(
        Commands commands, GumpBuilder builder, EntityCommands parent, Vector2 size)
        where TMarker : struct
    {
        parent.AddChild(builder.AddGumpTiled(
            commands, 0x0150, Vector3.UnitZ,
            new Vector2(0, 0), size)
            .Insert<TMarker>());
        parent.AddChild(builder.AddGump(
            commands, 0x0151, Vector3.UnitZ, new Vector2(0, 4))
            .Insert<TMarker>());
    }
}
