using System;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Single-clicking a map static pops its tiledata name as a transient overhead
// label (legacy GameSceneInputHandler.OnLeftMouseUp `case Static`). Statics are
// local entities (no NetworkSerial), so the network-keyed TextOverHeadManager
// can't carry them — the label rides as a component on the static entity and
// dies with it (chunk unload despawns the entity, dropping the component for
// free). One label per static: a re-click overwrites and refreshes the timer,
// matching legacy's per-object MaxSize = 1.
internal struct StaticNameLabel
{
    public string Text;
    public float Expire;   // Time.Total ms at which it disappears
}

// Per-system scratch: the left-press anchor used to tell a click from a drag.
internal struct ClickLatch
{
    public bool Armed;
    public Vector2 PressPos;
}

internal readonly struct StaticNameLabelPlugin : IPlugin
{
    // Legacy MessageManager scales a label's lifetime by SpeechDelay; a fixed
    // few seconds is the unscaled default and good enough for a click pop.
    private const float LifetimeMs = 4000f;

    public void Build(App app)
    {
        var showFn = ShowStaticName;
        app.AddSystem(showFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            // A target cursor click on a static belongs to targeting, not naming.
            .RunIf((Res<TargetingState> targeting) => !targeting.Value.IsTargeting)
            .Build();

        var expireFn = ExpireStaticNames;
        app.AddSystem(expireFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();
    }

    private static void ShowStaticName(
        Res<MouseContext> mouse,
        Res<Time> time,
        Res<UOFileManager> fileManager,
        Res<GrabbedItem> grabbed,
        Res<SelectedEntity> selected,
        Local<ClickLatch> latch,
        Commands commands,
        Query<Data<Graphic>, Filter<With<IsStatic>, Without<NetworkSerial>>> statics)
    {
        // Anchor the press position ourselves rather than trusting
        // MouseContext.DraggingOffset (it reports an absolute position under the
        // agent harness's synthetic mouse, never a delta) so the drag rejection
        // below holds for both real and scripted input.
        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            latch.Value.Armed = true;
            latch.Value.PressPos = mouse.Value.Position;
        }

        if (!mouse.Value.IsReleased(MouseButtonType.Left) || !latch.Value.Armed)
            return;
        latch.Value.Armed = false;

        // Released after a drag (camera pan) or while holding an item -> not a
        // name click.
        var delta = mouse.Value.Position - latch.Value.PressPos;
        if (Math.Abs(delta.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
            || Math.Abs(delta.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            return;
        if (grabbed.Value.Serial != 0)
            return;

        // A pick that came from overhead speech text resolves to its owner, not
        // the static under the cursor.
        if (selected.Value.IsText)
            return;

        if (!statics.TryGet(selected.Value.Entity, out var row))
            return;

        var (entity, graphic) = row;
        ref readonly var data = ref fileManager.Value.TileData.StaticData[graphic.Ref.Value];

        var name = StringHelper.GetPluralAdjustedString(data.Name, data.Count > 1);
        if (string.IsNullOrEmpty(name))
            name = fileManager.Value.Clilocs.GetString(1020000 + graphic.Ref.Value, data.Name);
        if (string.IsNullOrEmpty(name))
            return;

        commands.Entity(entity.Ref).Insert(new StaticNameLabel
        {
            Text = name,
            Expire = time.Value.Total + LifetimeMs,
        });
    }

    private static void ExpireStaticNames(
        Res<Time> time,
        Commands commands,
        Query<Data<StaticNameLabel>> labels)
    {
        foreach (var (entity, label) in labels)
            if (time.Value.Total >= label.Ref.Expire)
                commands.Entity(entity.Ref).Remove<StaticNameLabel>();
    }
}

// Draws the static-name labels over the world, in the same UI-RT phase as
// overhead speech / HP bars (UO bitmap fonts only render in GuiRenderingPlugin's
// uiRt). Mirrors MobileHpOverheads.Render's camera math and anchor.
internal static class StaticNameOverheads
{
    private const ushort AsciiFont3 = 3 | UoFontRuntime.AsciiFlag;
    private const ushort LabelHue = 0x03B2;
    private const int MaxWidth = 200;

    public static void Render(
        UltimaBatcher2D batch,
        AssetsServer assets,
        GameContext gameCtx,
        Camera camera,
        Query<Data<WorldPosition, Graphic, StaticNameLabel>> labelsQ)
    {
        var center = Isometric.IsoToScreen(gameCtx.CenterX, gameCtx.CenterY, gameCtx.CenterZ);
        var windowSize = new Vector2(camera.Bounds.Width, camera.Bounds.Height);
        center -= windowSize / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.CenterOffset;
        var panelOrigin = new Vector2(camera.Bounds.X, camera.Bounds.Y);
        var bounds = camera.Bounds;

        foreach (var (_, worldPos, graphic, label) in labelsQ)
        {
            var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
            position -= center;
            position.X += 22f;
            position.Y += 22f;

            // Rise to the top of the static's art so the name sits above it.
            ref readonly var art = ref assets.Arts.GetArt(graphic.Ref.Value);
            float artH = art.Texture != null ? art.UV.Height : 44f;
            position.Y -= artH - 22f;

            position = camera.WorldToScreen(position) + panelOrigin;

            var (tw, th) = UoFontRenderer.MeasureFont(label.Ref.Text, AsciiFont3, MaxWidth, allowHtml: false);
            if (th <= 0) continue;

            // Clamp into the game viewport so a label anchored on an edge static
            // (or a tall roof whose top sits above the screen) stays readable
            // instead of vanishing — same treatment legacy gives overhead text.
            int x = Math.Clamp((int)(position.X - tw / 2f), bounds.X, Math.Max(bounds.X, bounds.Right - tw));
            int y = Math.Clamp((int)(position.Y - th), bounds.Y, Math.Max(bounds.Y, bounds.Bottom - th));

            UoFontRenderer.Draw(batch, label.Ref.Text, AsciiFont3, LabelHue, x, y, MaxWidth, 0f, allowHtml: false);
        }
    }
}
