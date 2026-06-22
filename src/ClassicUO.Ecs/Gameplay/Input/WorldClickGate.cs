using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal static class WorldClickGate
{
    // True when the cursor is over the LIVE game world viewport right now (not a
    // gump, not the gutter, not the top bar). World-click consumers (single-click
    // name, double-click use, static name) must gate on THIS, not on
    // SelectedEntity: that resource is promoted a frame late (Clear runs in
    // PostUpdate, the window claim in Stage.Last) while the consumers read it in
    // Update, so a fast move from a world object onto a gump or the gutter leaves
    // the stale pick in SelectedEntity before the claim overwrites it — and the
    // consumer acts on it. Reading the cursor directly removes that race.
    //
    // The GameWindowUI viewport node is itself a UI hit (it carries a
    // BackgroundColor), so a hit on IT counts as "over world". Mirrors the
    // over-world test GameCursorPlugin uses for the directional hand.
    public static bool OverWorld(
        Vector2 mousePos,
        Camera camera,
        UiGesturePick pick,
        AssetsServer assets,
        Query<Data<GameScreenPlugin.GameWindowUI>> gameWin)
    {
        if (!camera.Bounds.Contains((int)mousePos.X, (int)mousePos.Y))
            return false;

        var hit = pick.Topmost(mousePos, assets);
        return !hit.Found || gameWin.Contains(hit.Entity);
    }
}
