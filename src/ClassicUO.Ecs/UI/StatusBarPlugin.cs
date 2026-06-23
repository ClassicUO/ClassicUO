// Player status bar — ECS port of StatusGump (Game/UI/Gumps/StatusGump.cs).
// Picks the layout by client version: the classic StatusGumpOld (bg 0x0802) for
// pre-AOS clients, the modern StatusGumpModern (bg 0x2A6C) for >= CV_308Z.
// Opened from the paperdoll Status button (single instance), draggable +
// right-click-close via the UOGump/UiMovable infra; refreshed each frame from
// the player entity.
//
// v1 scope vs legacy: no stat-lock togglers (no Lock data in ECS), no buff-icon
// button, no cliloc tooltips; values are left-aligned (no per-label centering).
// The modern layout uses the UOP-client field positions.

using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// MinimizeX/Y: the gump-local origin of the minimize region (legacy
// StatusGumpBase._point). A left-click released between here and the gump's
// bottom-right corner minimizes the bar into the player healthbar
// (HealthBarPlugin.WindowInteractions); clicking elsewhere — stat-lock
// togglers, the buff button, the body — does not. Stamped by both the host
// gump and the ecs-status mod root so the shared host gesture gates on it.
internal struct StatusBarWindow { public float MinimizeX, MinimizeY; }

internal enum StatusField : byte
{
    Name, Sex,
    Strength, Dexterity, Intelligence,
    Hits, HitsMax, Mana, ManaMax, Stamina, StaminaMax,
    HitsCombined, ManaCombined, StaminaCombined, WeightCombined, ArmorOld, // old-layout combined values
    Weight, WeightMax, StatCap, Luck, Gold,
    Damage, Followers,
    HitChanceInc, DefenseChanceInc, SwingSpeedInc, DamageInc,
    LowerManaCost, LowerReagentCost, SpellDamageInc, FasterCasting, FasterCastRecovery,
    Armor, FireRes, ColdRes, PoisonRes, EnergyRes,
}

// BoxLeft/CenterW: when CenterW > 0 the value is centered inside [BoxLeft,
// BoxLeft+CenterW] (legacy TS_CENTER columns); else left-aligned at BoxLeft.
internal struct StatusLabel { public StatusField Field; public int BoxLeft; public int CenterW; }

// Marks status fields whose UiTooltip text hasn't been resolved yet, so Refresh
// fills the cliloc string once (and we don't re-resolve every frame).
internal struct StatusTooltipPending { public int Cliloc; }

// A stat-lock toggle (Str/Dex/Int). Stat 0/1/2. The lock STATE lives on the
// player (StatLocks) so it survives reopening; the button just identifies which.
internal struct StatLockButton { public byte Stat; }

internal readonly struct StatusBarPlugin : IPlugin
{
    private const ushort OldBackground = 0x0802;
    private const ushort ModernBackground = 0x2A6C;
    private const ushort LabelHue = 0x0386; // legacy status Label hue
    private const ushort BuffIconNormal = 0x7538, BuffIconPressed = 0x7539;
    private const ushort LockUp = 0x0984, LockDown = 0x0986, LockLocked = 0x082C;

    private static ushort LockGraphic(byte state) => state switch
    {
        1 => LockDown, 2 => LockLocked, _ => LockUp,
    };

    public void Build(App app)
    {
        var refreshFn = Refresh;
        var despawnFn = Despawn;
        app.AddSystem(refreshFn).InStage(Stage.Update).Build();
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
        // Opened from the paperdoll Status button via OpenOrFocus.

#if AGENT_BUILD
        app.AddResource(new DebugStatusQueue());
        var drainStatusFn = DrainDebugStatus;
        app.AddSystem(drainStatusFn).InStage(Stage.First).Build();
#endif
    }

#if AGENT_BUILD
    // debug.openStatus — open the status bar without driving the paperdoll UI.
    private static void DrainDebugStatus(
        Commands commands,
        ResMut<DebugStatusQueue> q,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Res<ClassicUO.Configuration.Profile> profile,
        Res<UiZCounter> zCounter,
        Query<Data<StatusBarWindow>> existing)
    {
        if (!q.Value.OpenRequested) return;
        q.Value.OpenRequested = false;
        OpenOrFocus(commands, builder.Value, assets.Value, gameCtx.Value, profile.Value, zCounter.Value, existing);
    }
#endif

    // (x, y, field, centerWidth) — centerWidth>0 centers the value in that box.
    // Classic layout (bg 0x0802) — two columns of plain left-aligned values.
    private static readonly (int x, int y, StatusField f, int cw)[] s_old =
    {
        (86, 42, StatusField.Name, 0),
        (86, 62, StatusField.Strength, 0), (86, 74, StatusField.Dexterity, 0), (86, 86, StatusField.Intelligence, 0),
        (86, 98, StatusField.Sex, 0), (86, 110, StatusField.ArmorOld, 0),
        (171, 62, StatusField.HitsCombined, 0), (171, 74, StatusField.ManaCombined, 0), (171, 86, StatusField.StaminaCombined, 0),
        (171, 98, StatusField.Gold, 0), (171, 110, StatusField.WeightCombined, 0),
    };

    // Modern AOS layout (bg 0x2A6C), UOP-client field positions. Hits/Stam/Mana
    // and Weight are centered in 40px columns; the name is centered over 320px.
    private static readonly (int x, int y, StatusField f, int cw)[] s_modern =
    {
        (90, 50, StatusField.Name, 320),
        (80, 77, StatusField.Strength, 0), (80, 105, StatusField.Dexterity, 0), (80, 133, StatusField.Intelligence, 0),
        (80, 161, StatusField.HitChanceInc, 0),
        (145, 70, StatusField.Hits, 40), (145, 83, StatusField.HitsMax, 40),
        (145, 98, StatusField.Stamina, 40), (145, 111, StatusField.StaminaMax, 40),
        (145, 126, StatusField.Mana, 40), (145, 139, StatusField.ManaMax, 40),
        (150, 161, StatusField.DefenseChanceInc, 0),
        (240, 77, StatusField.StatCap, 0), (240, 105, StatusField.Luck, 0),
        (230, 126, StatusField.Weight, 40), (230, 139, StatusField.WeightMax, 40),
        (240, 162, StatusField.LowerManaCost, 0),
        (320, 77, StatusField.Damage, 0), (320, 105, StatusField.DamageInc, 0),
        (320, 133, StatusField.Followers, 0), (320, 161, StatusField.SwingSpeedInc, 0),
        (400, 77, StatusField.LowerReagentCost, 0), (400, 105, StatusField.SpellDamageInc, 0),
        (400, 133, StatusField.FasterCasting, 0), (400, 161, StatusField.FasterCastRecovery, 0),
        (480, 161, StatusField.Gold, 0),
        (475, 74, StatusField.Armor, 0), (475, 92, StatusField.FireRes, 0), (475, 106, StatusField.ColdRes, 0),
        (475, 120, StatusField.PoisonRes, 0), (475, 134, StatusField.EnergyRes, 0),
    };

    // Open the status bar, or focus it (bump z) if already open. One instance.
    public static void OpenOrFocus(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        GameContext gameCtx,
        ClassicUO.Configuration.Profile profile,
        UiZCounter zCounter,
        Query<Data<StatusBarWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        // UseOldStatusGump forces the classic 0x0802 layout on modern clients
        // (legacy StatusGumpBase.GetStatusGump). Takes effect on next open.
        bool modern = gameCtx.ClientVersion >= ClientVersion.CV_308Z && !profile.UseOldStatusGump;
        var bg = modern ? ModernBackground : OldBackground;
        var layout = modern ? s_modern : s_old;
        // Exact legacy parity: ASCII font 1 + hue 0x0386 (Label(text, isunicode:
        // false, 0x0386, font:1)). The ASCII path bakes the UO per-pixel hue
        // gradient; the "color" is the packed hue, not an RGB tint.
        var color = UoFontRuntime.AsciiHue(LabelHue);
        ushort fontId = 1 | UoFontRuntime.AsciiFlag;

        // Clicking the status bar minimizes it into the player healthbar — see
        // HealthBarPlugin.WindowInteractions (UiPick over the raw mouse, so it
        // works over the bar's transparent cutouts and isn't stolen by other
        // centered interactive surfaces). UiContainsByBounds makes the whole
        // panel a hit target (legacy ContainsByBounds).
        // Minimize-region origin (legacy StatusGumpBase._point): UOP modern at
        // (540,180), classic at (244,112). The shared host gesture only minimizes
        // when the click lands between here and the gump's bottom-right corner.
        var minimize = modern ? new Vector2(540, 180) : new Vector2(244, 112);

        var root = builder.SpawnUOGump(commands, bg, Vector3.UnitZ, new Vector2(20, 150), zCounter)
            .Insert(new StatusBarWindow { MinimizeX = minimize.X, MinimizeY = minimize.Y })
            .Insert<UiContainsByBounds>();

        foreach (var (x, y, field, cw) in layout)
        {
            var lbl = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(x), Top = Val.Px(y),
                    Width = Val.Px(cw > 0 ? cw : 60), Height = Val.Px(15),
                })
                .Insert(new Text(string.Empty))
                .Insert(new TextFont { FontId = fontId, Size = 12 })
                .Insert(new TextColor(color))
                .Insert(new StatusLabel { Field = field, BoxLeft = x, CenterW = cw });

            // Cliloc tooltip per stat (legacy StatusGump SetTooltip). The label's
            // box is the hover area: UiContainsByBounds makes the child a UiPick
            // bounds-hit that wins over the window root by paint order; the text
            // is resolved once in Refresh (needs the cliloc loader).
            int cliloc = FieldCliloc(field);
            if (cliloc != 0)
            {
                lbl.Insert<UiContainsByBounds>()
                   .Insert(new UiTooltip())
                   .Insert(new StatusTooltipPending { Cliloc = cliloc });
            }

            commands.AddChild(root.Id, lbl.Id);
        }

        if (modern)
        {
            var rootId = root.Id;

            // Buff-icon button (opens the buff bar). 0x7538 normal / 0x7539 over.
            var buff = builder.AddButton(commands, (BuffIconNormal, BuffIconPressed, BuffIconPressed), Vector3.UnitZ, new Vector2(40, 50));
            buff.Observe((On<UiClick> _,
                          Commands cmd,
                          Res<GumpBuilder> gb,
                          Res<UiZCounter> z,
                          Res<PlayerBuffs> buffs,
                          Query<Data<Node>, Filter<With<BuffGumpUI>>> rootQ) =>
                BuffGumpPlugin.OpenOrFocus(cmd, gb.Value, z.Value, buffs.Value, rootQ));
            commands.AddChild(rootId, buff.Id);

            // Stat-lock toggles for Str/Dex/Int (UOP xOffset 28; y 76/102/132).
            // Click cycles up->down->locked on the PLAYER's StatLocks (persisted),
            // and sends 0xBF/0x1A. The graphic is refreshed from StatLocks below.
            foreach (var (stat, ly) in new[] { ((byte)0, 76), ((byte)1, 102), ((byte)2, 132) })
            {
                var lockPic = builder.AddGump(commands, LockUp, Vector3.UnitZ, new Vector2(28, ly))
                    .Insert(Interaction.None)
                    .Insert(new StatLockButton { Stat = stat });
                var capStat = stat;
                lockPic.Observe((On<UiClick> _,
                                 Res<NetClient> net,
                                 Commands cmd,
                                 Query<Data<StatLocks>, Filter<With<Player>>> locksQ,
                                 Query<Data<WorldPosition>, Filter<With<Player>>> pQ) =>
                {
                    ulong p = 0;
                    foreach (var (e, _) in pQ) { p = e.Ref; break; }
                    if (p == 0) return;

                    StatLocks locks = default;
                    if (locksQ.TryGet(p, out var locksRow)) { var (_, l) = locksRow; locks = l.Ref; }
                    byte cur = capStat == 0 ? locks.Str : capStat == 1 ? locks.Dex : locks.Int;
                    byte next = (byte)((cur + 1) % 3);
                    if (capStat == 0) locks.Str = next; else if (capStat == 1) locks.Dex = next; else locks.Int = next;
                    cmd.Entity(p).Insert(locks);
                    net.Value.Send_StatLockStateRequest(capStat, next);
                });
                commands.AddChild(rootId, lockPic.Id);
            }
        }

        // Dark divider lines under each current value, separating it from its max
        // (legacy Line(x, y, w, 1, 0xFF383838)) — hits/stam/mana + weight.
        if (modern)
        {
            var lineColor = new ClayColor(0x38, 0x38, 0x38, 255);
            foreach (var (lx, ly, lw) in new[] { (150, 82, 35), (150, 110, 35), (150, 138, 35), (236, 138, 34) })
            {
                var line = commands.Spawn()
                    .Insert(new Node
                    {
                        PositionType = PositionType.Absolute,
                        Left = Val.Px(lx), Top = Val.Px(ly),
                        Width = Val.Px(lw), Height = Val.Px(1),
                    })
                    .Insert(new BackgroundColor(lineColor));
                commands.AddChild(root.Id, line.Id);
            }
        }
    }

    private static void Refresh(
        Res<GameContext> gameCtx,
        Res<UOFileManager> files,
        Query<Data<WorldPosition>, Filter<With<Player>>> playerQ,
        Query<Data<Hits>> hitsQ,
        Query<Data<Mana>> manaQ,
        Query<Data<Stamina>> stamQ,
        Query<Data<PlayerData>> dataQ,
        Query<Data<StatLocks>> locksQ,
        Query<Data<UiCustom, StatLockButton>> lockBtnsQ,
        Query<Data<UiTooltip, StatusTooltipPending>> tipsQ,
        Query<Data<Text, Node, StatusLabel>> labelsQ)
    {
        // Resolve each field's cliloc tooltip once (lazy: the cliloc loader isn't
        // available at spawn). Idempotent — skipped once the text is filled.
        foreach (var (_, tip, pending) in tipsQ)
        {
            if (string.IsNullOrEmpty(tip.Ref.Text) && pending.Ref.Cliloc != 0)
                tip.Ref.Text = files.Value.Clilocs.GetString(pending.Ref.Cliloc, true);
        }

        ulong pid = 0;
        foreach (var (ent, _) in playerQ) { pid = ent.Ref; break; }
        if (pid == 0) return;

        // Lock button graphics reflect the player's persisted StatLocks.
        StatLocks locks = default;
        if (locksQ.TryGet(pid, out var locksRow)) { var (_, l) = locksRow; locks = l.Ref; }
        foreach (var (_, custom, btn) in lockBtnsQ)
        {
            byte st = btn.Ref.Stat == 0 ? locks.Str : btn.Ref.Stat == 1 ? locks.Dex : locks.Int;
            var r = custom.Ref.Render();
            if (r != null) r.AssetId = LockGraphic(st);
        }

        Hits hits = default; Mana mana = default; Stamina stam = default; PlayerData d = default;
        if (hitsQ.TryGet(pid, out var hitsRow)) { var (_, h) = hitsRow; hits = h.Ref; }
        if (manaQ.TryGet(pid, out var manaRow)) { var (_, m) = manaRow; mana = m.Ref; }
        if (stamQ.TryGet(pid, out var stamRow)) { var (_, s) = stamRow; stam = s.Ref; }
        if (dataQ.TryGet(pid, out var dataRow)) { var (_, pd) = dataRow; d = pd.Ref; }

        foreach (var (_, text, node, label) in labelsQ)
        {
            text.Ref.Value = FormatField(label.Ref.Field, in d, in hits, in mana, in stam, gameCtx.Value.PlayerName ?? string.Empty);

            // Center the value inside its box (legacy TS_CENTER columns).
            if (label.Ref.CenterW > 0 && UoFontRuntime.Fonts != null)
            {
                int w = UoFontRuntime.Fonts.GetWidthASCII(1, text.Ref.Value);
                node.Ref.Left = Val.Px(label.Ref.BoxLeft + (label.Ref.CenterW - w) / 2f);
            }
        }
    }

    // Tooltip cliloc per status field (legacy StatusGump SetTooltip ids). 0 = no
    // tooltip (the Name field). Combined cur/max fields reuse the base stat's id.
    internal static int FieldCliloc(StatusField f) => f switch
    {
        StatusField.Strength => 1061146,
        StatusField.Dexterity => 1061147,
        StatusField.Intelligence => 1061148,
        StatusField.Hits or StatusField.HitsMax or StatusField.HitsCombined => 1061149,
        StatusField.Mana or StatusField.ManaMax or StatusField.ManaCombined => 1061151,
        StatusField.Stamina or StatusField.StaminaMax or StatusField.StaminaCombined => 1061150,
        StatusField.Weight or StatusField.WeightMax or StatusField.WeightCombined => 1061154,
        StatusField.StatCap => 1061152,
        StatusField.Luck => 1061153,
        StatusField.Gold => 1061156,
        StatusField.Damage => 1061155,
        StatusField.Followers => 1061157,
        StatusField.HitChanceInc => 1075616,
        StatusField.DefenseChanceInc => 1075620,
        StatusField.SwingSpeedInc => 1075629,
        StatusField.DamageInc => 1075619,
        StatusField.LowerManaCost => 1075621,
        StatusField.LowerReagentCost => 1075625,
        StatusField.SpellDamageInc => 1075628,
        StatusField.FasterCasting => 1075617,
        StatusField.FasterCastRecovery => 1075618,
        StatusField.Armor => 1061158,
        StatusField.FireRes => 1061159,
        StatusField.ColdRes => 1061160,
        StatusField.PoisonRes => 1061161,
        StatusField.EnergyRes => 1061162,
        StatusField.Sex => 3000076,
        StatusField.ArmorOld => 1062760,
        _ => 0, // Name → no tooltip
    };

    // Map a status field to its display string from the player's live stats.
    // Pure (no ECS / render) so it is unit-testable; the Refresh system just
    // pipes the resolved player components in. Mirrors legacy StatusGump label
    // text (combined "cur/max" vs single value per field).
    internal static string FormatField(StatusField field, in PlayerData d, in Hits hits, in Mana mana, in Stamina stam, string playerName)
        => field switch
        {
            StatusField.Name => playerName ?? string.Empty,
            StatusField.Sex => d.IsFemale ? "Female" : "Male",
            StatusField.Strength => d.Str.ToString(),
            StatusField.Dexterity => d.Dex.ToString(),
            StatusField.Intelligence => d.Int.ToString(),
            StatusField.Hits => hits.Value.ToString(),
            StatusField.HitsMax => hits.MaxValue.ToString(),
            StatusField.Mana => mana.Value.ToString(),
            StatusField.ManaMax => mana.MaxValue.ToString(),
            StatusField.Stamina => stam.Value.ToString(),
            StatusField.StaminaMax => stam.MaxValue.ToString(),
            StatusField.HitsCombined => $"{hits.Value}/{hits.MaxValue}",
            StatusField.ManaCombined => $"{mana.Value}/{mana.MaxValue}",
            StatusField.StaminaCombined => $"{stam.Value}/{stam.MaxValue}",
            StatusField.WeightCombined => $"{d.Weight}/{d.WeightMax}",
            StatusField.ArmorOld => d.PhysicalRes.ToString(),
            StatusField.Weight => d.Weight.ToString(),
            StatusField.WeightMax => d.WeightMax.ToString(),
            StatusField.StatCap => d.StatsCap.ToString(),
            StatusField.Luck => d.Luck.ToString(),
            StatusField.Gold => d.Gold.ToString(),
            StatusField.Damage => $"{d.DamageMin}-{d.DamageMax}",
            StatusField.Followers => $"{d.Followers}/{d.FollowersMax}",
            StatusField.HitChanceInc => d.HitChanceInc.ToString(),
            StatusField.DefenseChanceInc => $"{d.DefenseChanceInc}/{d.MaxDefenseChanceInc}",
            StatusField.SwingSpeedInc => d.SwingSpeedInc.ToString(),
            StatusField.DamageInc => d.DamageInc.ToString(),
            StatusField.LowerManaCost => d.LowerManaCost.ToString(),
            StatusField.LowerReagentCost => d.LowerReagentCost.ToString(),
            StatusField.SpellDamageInc => d.SpellDamageInc.ToString(),
            StatusField.FasterCasting => d.FasterCasting.ToString(),
            StatusField.FasterCastRecovery => d.FasterCastRecovery.ToString(),
            StatusField.Armor => $"{d.PhysicalRes}/{d.MaxPhysicalRes}",
            StatusField.FireRes => $"{d.FireRes}/{d.MaxFireRes}",
            StatusField.ColdRes => $"{d.ColdRes}/{d.MaxColdRes}",
            StatusField.PoisonRes => $"{d.PoisonRes}/{d.MaxPoisonRes}",
            StatusField.EnergyRes => $"{d.EnergyRes}/{d.MaxEnergyRes}",
            _ => string.Empty,
        };

    private static void Despawn(
        Commands commands,
        Query<Data<StatusBarWindow>> windowsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
    }
}

#if AGENT_BUILD
internal sealed class DebugStatusQueue { public bool OpenRequested; }
#endif
