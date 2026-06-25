// Port of legacy Game/Managers/HealthLinesManager.cs + Entity.UpdateHits —
// the "Show mobiles HP" overheads: a [NN%] label above the head and/or a
// 34x8 notoriety-hued line bar at the feet of every mobile whose hits are
// known (HitsMax > 0, i.e. the server sent a status/health packet).
//
// Drawn immediately after the world RT image inside GuiRenderingPlugin's
// Phase 1 (same hook as overhead speech): direct UltimaBatcher2D draws in
// RT-local logical coords, ON the world but UNDER gump windows. No UI
// entities — nothing here participates in UiPick, so clicks pass through
// to the mobile exactly like legacy.
//
// Profile linkage (live, read per frame):
//   ShowMobilesHP    master toggle
//   MobileHPType     0 = percentage, 1 = line, 2 = both
//   MobileHPShowWhen 0 = always, 1 = skip when full, 2 = smart (bar always,
//                    text only when damaged)
//   Innocent/Friend/Criminal/CanAttack/Enemy/Murderer hues (bar tint)
//
// Omitted vs legacy: the UseNewTargetSystem bracket frames and the
// last-target/last-attack force-draw — ECS targeting keeps no last-target
// state yet.

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

internal static class MobileHpOverheads
{
    private const int BarWidth = 34;
    private const int BarHeight = 8;
    private const ushort BackgroundGump = 0x1068;
    private const ushort FillGump = 0x1069;
    private const ushort AsciiFont3 = 3 | UoFontRuntime.AsciiFlag;

    // pct is always 0..100 (Math.Clamp below), so the "[NN%]" head label is one
    // of 101 fixed strings — built once instead of interpolated each frame.
    private static readonly string[] PercentLabels = BuildPercentLabels();

    private static string[] BuildPercentLabels()
    {
        var labels = new string[101];
        for (int i = 0; i <= 100; i++)
            labels[i] = $"[{i}%]";
        return labels;
    }

    public static void Render(
        UltimaBatcher2D batch,
        AssetsServer assets,
        Profile profile,
        GameContext gameCtx,
        Camera camera,
        Query<Data<NetworkSerial, WorldPosition, ScreenPositionOffset, Graphic, Hits, Notoriety, ServerFlags, EquipmentSlots>,
            Filter<Optional<ScreenPositionOffset>, Optional<Hits>, Optional<Notoriety>, Optional<ServerFlags>, Optional<EquipmentSlots>,
                Without<ContainedInto>>> mobilesQ,
        System.Collections.Generic.IReadOnlyDictionary<uint, float> plateTops)
    {
        if (!profile.ShowMobilesHP)
            return;

        int mode = profile.MobileHPType;
        int showWhen = profile.MobileHPShowWhen;

        // Same camera math as TextOverHeadManager.Render — these draws share
        // its render context (UI RT, logical pixels).
        var center = Isometric.IsoToScreen(gameCtx.CenterX, gameCtx.CenterY, gameCtx.CenterZ);
        var windowSize = new Vector2(camera.Bounds.Width, camera.Bounds.Height);
        center -= windowSize / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.CenterOffset;
        var panelOrigin = new Vector2(camera.Bounds.X, camera.Bounds.Y);
        var bounds = camera.Bounds;

        foreach (var (_, serial, worldPos, offset, graphic, hits, noto, flags, equip) in mobilesQ)
        {
            if (!SerialHelper.IsMobile(serial.Ref.Value)) continue;
            if (!hits.IsValid()) continue;

            int max = hits.Ref.MaxValue;
            int cur = hits.Ref.Value;
            if (max == 0) continue;
            if (showWhen == 1 && cur == max) continue;
            int pct = Math.Clamp(cur * 100 / max, 0, 100);

            // Legacy anchor: RealScreenPosition + offset + (22+5, 22+5).
            var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
            if (offset.IsValid())
                position += offset.Ref.Value;
            position -= center;
            position.X += 22f + 5f;
            position.Y += 22f + 5f;

            var f = flags.IsValid() ? flags.Ref.Value : Flags.None;
            bool isDead = IsDeadBody(graphic.Ref.Value);
            bool mounted = equip.IsValid() && equip.Ref[GameLayer.Mount] != 0;

            // ---- [NN%] label above the head ----
            if (mode != 1 && !isDead && pct != 0 && (showWhen <= 1 || cur != max))
            {
                // Fixed standing frame (dir 0, group 0, frame 0) like the
                // nameplate anchor, so the label doesn't bounce with the walk.
                assets.Animations.GetAnimationDimensions(
                    0, graphic.Ref.Value, 0, 0, mounted, 0,
                    out _, out int animCenterY, out _, out int animHeight);

                var p1 = position;
                p1.Y -= animHeight + animCenterY + 8 + 22;
                bool flyingGargoyle = (f & Flags.Flying) != 0 && IsGargoyle(graphic.Ref.Value);
                if (flyingGargoyle) p1.Y -= 22;
                else if (!mounted) p1.Y += 22;

                p1 = camera.WorldToScreen(p1) + panelOrigin;

                var text = PercentLabels[pct];
                var (tw, th) = UoFontRenderer.MeasureFont(text, AsciiFont3, 200, allowHtml: false);
                p1.X -= tw / 2f + 5f;
                p1.Y -= th;

                // A visible nameplate owns the head slot — sit above it
                // (legacy shifts by the object-handles gump height).
                if (plateTops != null
                    && plateTops.TryGetValue(serial.Ref.Value, out var plateTop)
                    && p1.Y + th > plateTop)
                    p1.Y = plateTop - th - 5f;

                if (p1.X >= bounds.X && p1.X + tw <= bounds.Right
                    && p1.Y >= bounds.Y && p1.Y + th <= bounds.Bottom)
                    UoFontRenderer.Draw(batch, text, AsciiFont3, TextHue(pct), (int)p1.X, (int)p1.Y, 200, 0f, allowHtml: false);
            }

            // ---- line bar at the feet ----
            if (mode >= 1)
            {
                var p = position;
                p.X -= 5f;
                p = camera.WorldToScreen(p) + panelOrigin;
                p.X -= BarWidth / 2f;
                p.Y -= BarHeight / 2f;

                if (p.X < bounds.X || p.X > bounds.Right - BarWidth
                    || p.Y < bounds.Y || p.Y > bounds.Bottom - BarHeight)
                    continue;

                // Legacy dims everything but the player (and the active
                // target, which ECS doesn't track yet).
                float alpha = serial.Ref.Value == gameCtx.PlayerSerial ? 1f : 0.5f;
                ushort notoHue = NotorietyHelper.NotorietyHue(profile,
                    noto.IsValid() ? noto.Ref.Value : NotorietyFlag.Unknown);

                int x = (int)p.X, y = (int)p.Y;
                int per = BarWidth * pct / 100;

                ref readonly var bg = ref assets.Gumps.GetGump(BackgroundGump);
                if (bg.Texture != null)
                    batch.Draw(
                        bg.Texture,
                        new Rectangle(x, y, bg.UV.Width, bg.UV.Height),
                        bg.UV,
                        ShaderHueTranslator.GetHueVector(notoHue, false, alpha),
                        0f);

                ref readonly var fill = ref assets.Gumps.GetGump(FillGump);
                if (fill.Texture == null) continue;

                // Depleted region in red (legacy hue 0x21), with the same
                // 2px seam fudge.
                if (cur != max)
                {
                    int seam = per >> 2 == 0 ? per : 2;
                    int w = BarWidth - per - seam / 2;
                    if (w > 0)
                        batch.DrawTiled(
                            fill.Texture,
                            new Rectangle(x + per - seam, y, w, fill.UV.Height),
                            fill.UV,
                            ShaderHueTranslator.GetHueVector(0x21, false, alpha),
                            0f);
                }

                if (per > 0)
                {
                    // Blue, green when poisoned, yellow on YellowBar status.
                    // Bit 0x04 is Poisoned pre-7.0 but Flying on 7.0+ (same value),
                    // so only treat it as poison on older clients — otherwise a
                    // flying gargoyle would show a green bar. SA poison isn't tracked.
                    ushort hue = 90;
                    if (gameCtx.ClientVersion < ClientVersion.CV_7000 && (f & Flags.Poisoned) != 0) hue = 63;
                    else if ((f & Flags.YellowBar) != 0) hue = 53;

                    batch.DrawTiled(
                        fill.Texture,
                        new Rectangle(x, y, per, fill.UV.Height),
                        fill.UV,
                        ShaderHueTranslator.GetHueVector(hue, false, alpha),
                        0f);
                }
            }
        }
    }

    // Legacy Entity.UpdateHits color thresholds.
    private static ushort TextHue(int pct)
        => pct < 30 ? (ushort)0x0021
         : pct < 50 ? (ushort)0x0030
         : pct < 80 ? (ushort)0x0058
         : (ushort)0x0044;

    // Legacy Mobile.IsDead ghost bodies (same set as WorldRenderingPlugin).
    private static bool IsDeadBody(ushort graphic)
        => graphic == 0x0192 || graphic == 0x0193 ||
           (graphic >= 0x025F && graphic <= 0x0260) ||
           graphic == 0x02B6 || graphic == 0x02B7;

    // Legacy Mobile.IsGargoyle — flying gargoyles get the -22 anchor shift.
    private static bool IsGargoyle(ushort graphic)
        => graphic == 0x029A || graphic == 0x029B || graphic == 0x02B6 || graphic == 0x02B7;
}

// Hits for OTHER mobiles only arrive when the server volunteers them (the
// 0xA1 family fires on hits CHANGE, normalized 0-25 for non-owners) or when
// the client asks (0x11 status reply). An idle, never-damaged mobile carries
// no hits data at all — legacy shows no line for it either unless something
// (health bar open, attack, target) requested status. With the overheads
// enabled the whole point is seeing every mobile, so fire a one-shot
// Send_StatusRequest per unknown mobile, throttled per frame, mirroring
// NameplatePlugin's name fetch.
//
// Profile.PollMobileStatus (OSI mode, default off) switches to a 500ms
// close-status + status-request sweep over every visible mobile instead —
// OSI normalizes non-owner hits to a fixed scale and won't re-send unless
// they change, so the one-shot value goes stale.
internal sealed class HpStatusRequests
{
    public readonly System.Collections.Generic.HashSet<uint> Sent = new();
    public float NextPrune;
    public float NextPoll;
}

internal readonly struct MobileHpOverheadPlugin : IPlugin
{
    private const int RequestsPerFrame = 5;
    private const float PruneIntervalMs = 5000f;
    private const float PollIntervalMs = 500f;

    public void Build(App app)
    {
        app.AddResource(new HpStatusRequests());

        var requestFn = RequestMissingHits;
        app.AddSystem(requestFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .RunIf((Res<Profile> p) => p.Value.ShowMobilesHP)
            .Build();
    }

    private static void RequestMissingHits(
        Res<NetClient> net,
        Res<Time> time,
        Res<GameContext> gameCtx,
        Res<Profile> profile,
        Res<HpStatusRequests> state,
        Query<Data<NetworkSerial, Hits>, Filter<Optional<Hits>, Without<ContainedInto>>> mobilesQ)
    {
        var st = state.Value;

        // OSI mode: the server normalizes other mobiles' hits and only pushes
        // on change, so the one-shot reply goes stale. Close the status
        // subscription and re-request for EVERY visible mobile each tick —
        // the close (0xBF 0x0C) makes the server treat the next 0x34 as a
        // fresh open and answer with current values (legacy
        // RequestMobileStatus(force: true) round-trip).
        if (profile.Value.PollMobileStatus)
        {
            if (time.Value.Total < st.NextPoll) return;
            st.NextPoll = time.Value.Total + PollIntervalMs;

            foreach (var (_, serial, _) in mobilesQ)
            {
                var s = serial.Ref.Value;
                if (!SerialHelper.IsMobile(s) || s == gameCtx.Value.PlayerSerial) continue;
                net.Value.Send_CloseStatusBarGump(s);
                net.Value.Send_StatusRequest(s);
            }
            return;
        }

        // Drop serials whose entity left view so a returning mobile gets a
        // fresh request; present-but-unanswered serials stay (no retry spam).
        if (time.Value.Total >= st.NextPrune)
        {
            st.NextPrune = time.Value.Total + PruneIntervalMs;
            var live = new System.Collections.Generic.HashSet<uint>();
            foreach (var (_, serial, _) in mobilesQ)
                live.Add(serial.Ref.Value);
            st.Sent.IntersectWith(live);
        }

        int budget = RequestsPerFrame;
        foreach (var (_, serial, hits) in mobilesQ)
        {
            var s = serial.Ref.Value;
            if (!SerialHelper.IsMobile(s) || s == gameCtx.Value.PlayerSerial) continue;
            if (hits.IsValid() && hits.Ref.MaxValue > 0) continue;
            if (!st.Sent.Add(s)) continue;

            net.Value.Send_StatusRequest(s);
            if (--budget == 0) break;
        }
    }
}

// Bundles the world-overlay inputs GuiRenderingPlugin.Render needs (the
// system sits at the 16-param cap).
internal sealed class WorldOverlayParams : CompositeSystemParam
{
    public readonly Res<TextOverHeadManager> Overhead;
    public readonly Res<Profile> Profile;
    public readonly Res<MouseContext> Mouse;
    public readonly Res<SelectedEntity> Selected;
    public readonly Res<LightRenderTarget> Light;
    public readonly Res<TargetingState> Targeting;
    public readonly Query<Data<WorldPosition, ScreenPositionOffset>> OverheadAnchors;
    public readonly Query<Data<NetworkSerial, WorldPosition, ScreenPositionOffset, Graphic, Hits, Notoriety, ServerFlags, EquipmentSlots>,
        Filter<Optional<ScreenPositionOffset>, Optional<Hits>, Optional<Notoriety>, Optional<ServerFlags>, Optional<EquipmentSlots>,
            Without<ContainedInto>>> Mobiles;
    public readonly Query<Data<WorldPosition, Graphic, StaticNameLabel>> StaticLabels;
    // Serial -> on-screen rect for items shown inside a gump. Feeds
    // TextOverHeadManager.RenderGumpAnchored so a single-clicked container slot
    // / paperdoll item's name floats over the slot (it has no WorldPosition, so
    // the world overhead pass skips it).
    public readonly Query<Data<ContainerItemUI, ComputedNode>> ContainerItemAnchors;
    public readonly Query<Data<PaperdollEquipUI, ComputedNode>> PaperdollEquipAnchors;
    public readonly Query<Data<PaperdollBackpackUI, ComputedNode>> PaperdollBackpackAnchors;
    public readonly Res<GuiRenderingPlugin.GumpItemNameAnchors> GumpAnchors;
    public readonly Res<GumpNameClickPos> ClickPos;
    public readonly Query<Data<ComputedNode>> NodeById;

    public WorldOverlayParams()
    {
        Overhead = Add(new Res<TextOverHeadManager>());
        Profile = Add(new Res<Profile>());
        Mouse = Add(new Res<MouseContext>());
        Selected = Add(new Res<SelectedEntity>());
        Light = Add(new Res<LightRenderTarget>());
        Targeting = Add(new Res<TargetingState>());
        OverheadAnchors = Add(new Query<Data<WorldPosition, ScreenPositionOffset>>());
        Mobiles = Add(new Query<Data<NetworkSerial, WorldPosition, ScreenPositionOffset, Graphic, Hits, Notoriety, ServerFlags, EquipmentSlots>,
            Filter<Optional<ScreenPositionOffset>, Optional<Hits>, Optional<Notoriety>, Optional<ServerFlags>, Optional<EquipmentSlots>,
                Without<ContainedInto>>>());
        StaticLabels = Add(new Query<Data<WorldPosition, Graphic, StaticNameLabel>>());
        ContainerItemAnchors = Add(new Query<Data<ContainerItemUI, ComputedNode>>());
        PaperdollEquipAnchors = Add(new Query<Data<PaperdollEquipUI, ComputedNode>>());
        PaperdollBackpackAnchors = Add(new Query<Data<PaperdollBackpackUI, ComputedNode>>());
        GumpAnchors = Add(new Res<GuiRenderingPlugin.GumpItemNameAnchors>());
        ClickPos = Add(new Res<GumpNameClickPos>());
        NodeById = Add(new Query<Data<ComputedNode>>());
    }
}
