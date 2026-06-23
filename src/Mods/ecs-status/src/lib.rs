// Status-gump mod: a 100%-compatible reproduction of the host StatusGump
// (src/ClassicUO.Ecs/UI/StatusBarPlugin.cs), built entirely through the modding
// API — the host gump files are NOT edited.
//
// Compatibility strategy:
//   - The panel ROOT is tagged `cuo:ui/statusbar-window` + `cuo:ui/movable`, so
//     the host's generic systems treat it exactly like the native gump: drag,
//     right-click-close, z-bump-on-reopen, and click-minimize-to-healthbar
//     (HealthBarPlugin) all work with no extra code.
//   - Stat-lock buttons carry `cuo:ui/stat-lock-button` and the buff button
//     `cuo:ui/button`; both markers make the control minimize-latch-exempt in
//     HealthBarPlugin, and the host StatusBarPlugin.Refresh keeps the lock
//     graphic in sync with the player's StatLocks for free.
//   - Labels are generic Node+Text the mod fills + recenters each frame (the
//     same ASCII font 1 / hue 0x0386 the host uses). Tooltips resolve their
//     cliloc via cuo:modding/ui. Layout, positions, divider lines, buff/lock
//     buttons mirror the host s_old / s_modern tables 1:1.
//
// Flow:
//   - mod-status-init     paperdoll "Status" spawns the host StatusBarWindow;
//                         despawn it (suppress) and open OUR panel unless open.
//   - mod-status-refresh  pump live stats into the labels + recenter columns.
//   - mod-status-click    stat-lock click cycles StatLocks + sends 0xBF/0x1A;
//                         buff button opens the buff bar.

wit_bindgen::generate!({
    world: "status-mod",
    path: "wit",
    with: {
        "tinyecs:modding/app@0.1.0": generate,
        "cuo:modding/net@0.1.0": generate,
        "cuo:modding/ui@0.1.0": generate,
    },
});

use crate::cuo::modding::net::Net;
use crate::cuo::modding::ui::Ui;
use crate::tinyecs::modding::app::{
    AlignItems, Color, Component, ComponentValue, Display, Entity, FlexDirection, JustifyContent,
    NameRec, NodeRec, Overflow, PositionType, QueryFor, Schedule, System, TextFontRec, TextRec,
    UiRect, Val, ZRec,
};

// ── host constants (StatusBarPlugin.cs) ─────────────────────────────────────
const MODERN_BG: u16 = 0x2A6C;
const OLD_BG: u16 = 0x0802;
const LOCK_UP: u16 = 0x0984;
// FontId = 1 | UoFontRuntime.AsciiFlag(0x80); ASCII font 1, the legacy status font.
const ASCII_FONT: u16 = 1 | 0x80;
// modern = ClientVersion >= CV_308Z && !UseOldStatusGump.
const CV_308Z: u32 = 0x0300_087A;

// StatusField ids — mirror the host enum order verbatim.
mod field {
    pub const NAME: u8 = 0;
    pub const SEX: u8 = 1;
    pub const STR: u8 = 2;
    pub const DEX: u8 = 3;
    pub const INT: u8 = 4;
    pub const HITS: u8 = 5;
    pub const HITS_MAX: u8 = 6;
    pub const MANA: u8 = 7;
    pub const MANA_MAX: u8 = 8;
    pub const STAM: u8 = 9;
    pub const STAM_MAX: u8 = 10;
    pub const HITS_COMBINED: u8 = 11;
    pub const MANA_COMBINED: u8 = 12;
    pub const STAM_COMBINED: u8 = 13;
    pub const WEIGHT_COMBINED: u8 = 14;
    pub const ARMOR_OLD: u8 = 15;
    pub const WEIGHT: u8 = 16;
    pub const WEIGHT_MAX: u8 = 17;
    pub const STAT_CAP: u8 = 18;
    pub const LUCK: u8 = 19;
    pub const GOLD: u8 = 20;
    pub const DAMAGE: u8 = 21;
    pub const FOLLOWERS: u8 = 22;
    pub const HIT_CHANCE_INC: u8 = 23;
    pub const DEFENSE_CHANCE_INC: u8 = 24;
    pub const SWING_SPEED_INC: u8 = 25;
    pub const DAMAGE_INC: u8 = 26;
    pub const LOWER_MANA_COST: u8 = 27;
    pub const LOWER_REAGENT_COST: u8 = 28;
    pub const SPELL_DAMAGE_INC: u8 = 29;
    pub const FASTER_CASTING: u8 = 30;
    pub const FASTER_CAST_RECOVERY: u8 = 31;
    pub const ARMOR: u8 = 32;
    pub const FIRE_RES: u8 = 33;
    pub const COLD_RES: u8 = 34;
    pub const POISON_RES: u8 = 35;
    pub const ENERGY_RES: u8 = 36;
}

// (x, y, field, center-width) — center-width > 0 centers the value in that box.
const OLD: &[(i32, i32, u8, i32)] = &[
    (86, 42, field::NAME, 0),
    (86, 62, field::STR, 0),
    (86, 74, field::DEX, 0),
    (86, 86, field::INT, 0),
    (86, 98, field::SEX, 0),
    (86, 110, field::ARMOR_OLD, 0),
    (171, 62, field::HITS_COMBINED, 0),
    (171, 74, field::MANA_COMBINED, 0),
    (171, 86, field::STAM_COMBINED, 0),
    (171, 98, field::GOLD, 0),
    (171, 110, field::WEIGHT_COMBINED, 0),
];

const MODERN: &[(i32, i32, u8, i32)] = &[
    (90, 50, field::NAME, 320),
    (80, 77, field::STR, 0),
    (80, 105, field::DEX, 0),
    (80, 133, field::INT, 0),
    (80, 161, field::HIT_CHANCE_INC, 0),
    (145, 70, field::HITS, 40),
    (145, 83, field::HITS_MAX, 40),
    (145, 98, field::STAM, 40),
    (145, 111, field::STAM_MAX, 40),
    (145, 126, field::MANA, 40),
    (145, 139, field::MANA_MAX, 40),
    (150, 161, field::DEFENSE_CHANCE_INC, 0),
    (240, 77, field::STAT_CAP, 0),
    (240, 105, field::LUCK, 0),
    (230, 126, field::WEIGHT, 40),
    (230, 139, field::WEIGHT_MAX, 40),
    (240, 162, field::LOWER_MANA_COST, 0),
    (320, 77, field::DAMAGE, 0),
    (320, 105, field::DAMAGE_INC, 0),
    (320, 133, field::FOLLOWERS, 0),
    (320, 161, field::SWING_SPEED_INC, 0),
    (400, 77, field::LOWER_REAGENT_COST, 0),
    (400, 105, field::SPELL_DAMAGE_INC, 0),
    (400, 133, field::FASTER_CASTING, 0),
    (400, 161, field::FASTER_CAST_RECOVERY, 0),
    (480, 161, field::GOLD, 0),
    (475, 74, field::ARMOR, 0),
    (475, 92, field::FIRE_RES, 0),
    (475, 106, field::COLD_RES, 0),
    (475, 120, field::POISON_RES, 0),
    (475, 134, field::ENERGY_RES, 0),
];

struct StatusMod;

impl Guest for StatusMod {
    fn setup(app: App) {
        let init = System::new("mod-status-init");
        init.add_commands();
        init.add_query(&[QueryFor::With("cuo:ui/statusbar-window".into())]);
        init.add_query(&[QueryFor::With("cuo:player/player".into())]);
        app.add_systems(&Schedule::Update, &[&init]);

        let refresh = System::new("mod-status-refresh");
        refresh.add_commands();
        refresh.add_query(&[QueryFor::With("cuo:player/player".into())]);
        refresh.add_query(&[
            QueryFor::Mut("cuo:ui/node".into()),
            QueryFor::Mut("cuo:ui/text".into()),
            QueryFor::Ref("cuo:ui/name".into()),
        ]);
        app.add_systems(&Schedule::Update, &[&refresh]);

        let click = System::new("mod-status-click");
        click.add_commands();
        click.add_query(&[
            QueryFor::With("cuo:ui/clicked".into()),
            QueryFor::Ref("cuo:ui/name".into()),
        ]);
        click.add_query(&[QueryFor::With("cuo:player/player".into())]);
        app.add_systems(&Schedule::Update, &[&click]);

        println!("[status-mod] setup");
    }

    fn mod_status_init(commands: Commands, host_bar: Query, player: Query) {
        // The host StatusBarWindow appearing (paperdoll Status / debug.openStatus)
        // is the open request. Our own root ALSO carries statusbar-window, so skip
        // it by its UiName — a closed panel can then reopen.
        let mut mod_present = false;
        let mut open_requested = false;
        while let Some(row) = host_bar.iter() {
            let e = row.entity();
            if entity_name(&e) == "mod.status.root" {
                mod_present = true;
            } else {
                commands.entity(&e).despawn();
                open_requested = true;
            }
        }
        if mod_present || !open_requested {
            return;
        }
        if player.iter().is_none() {
            return;
        }

        // Layout choice — exactly the host formula.
        let ctx = parse(&commands.resource_get("cuo:game/context"));
        let cv = ctx["ClientVersion"].as_u64().unwrap_or(0) as u32;
        let use_old = ctx["UseOldStatusGump"].as_bool().unwrap_or(false);
        let modern = cv >= CV_308Z && !use_old;

        build_panel(&commands, modern);
    }

    fn mod_status_refresh(commands: Commands, player: Query, labels: Query) {
        let Some(prow) = player.iter() else {
            return;
        };
        let p = prow.entity();

        let d = parse(&p.get("cuo:player/data"));
        let hits = get_pair(&p, "cuo:player/hits");
        let mana = get_pair(&p, "cuo:player/mana");
        let stam = get_pair(&p, "cuo:player/stamina");
        let name = parse(&commands.resource_get("cuo:game/context"))["PlayerName"]
            .as_str()
            .unwrap_or("")
            .to_string();

        let ui = Ui::new();
        while let Some(row) = labels.iter() {
            let node = row.component(0); // mut cuo:ui/node
            let text = row.component(1); // mut cuo:ui/text
            let Some((f, x, y, cw)) = parse_label(&name_of(&row.component(2))) else {
                continue;
            };
            let value = format_field(f, &d, hits, mana, stam, &name);
            text.set_typed(&ComponentValue::Text(TextRec { value: value.clone() }));

            // Center the value inside its box (legacy TS_CENTER columns).
            if cw > 0 {
                let w = ui.measure_text(ASCII_FONT, &value) as i32;
                let left = x as f32 + (cw - w) as f32 / 2.0;
                node.set_typed(&ComponentValue::Node(abs_node(left, y as f32, cw as f32, 15.0)));
            }
        }
    }

    fn mod_status_click(commands: Commands, clicked: Query, player: Query) {
        let mut lock_cycle: Vec<u8> = Vec::new();
        while let Some(row) = clicked.iter() {
            let nm = name_of(&row.component(0)); // ref cuo:ui/name
            if let Some(s) = nm.strip_prefix("mod.status.lock.") {
                if let Ok(stat) = s.parse::<u8>() {
                    lock_cycle.push(stat);
                }
            }
        }

        if lock_cycle.is_empty() {
            return;
        }

        let Some(prow) = player.iter() else {
            return;
        };
        let p = prow.entity();
        let mut locks = get_locks(&p);

        let net = Net::new();
        for stat in lock_cycle {
            let i = stat as usize;
            let next = (locks[i] + 1) % 3;
            locks[i] = next;
            // 0xBF subcommand 0x1A — stat-lock change request.
            net.send(&[0xBF, 0x00, 0x07, 0x00, 0x1A, stat, next]);
        }
        // StatLocks isn't in the typed core set → write it via the JSON path. The
        // host Refresh then re-reads it and updates the lock graphic.
        commands
            .entity(&p)
            .insert(&[("cuo:player/stat-locks".into(), locks_json(&locks))]);
        println!("[status-mod] lock toggled");
    }
}

// ── panel construction ──────────────────────────────────────────────────────
fn build_panel(commands: &Commands, modern: bool) {
    let ui = Ui::new();
    let bg = if modern { MODERN_BG } else { OLD_BG };
    let (bw, bh) = unpack(ui.gump_size(bg));

    // Root: movable window sized to the bg gump + tagged statusbar-window so the
    // host treats it like the native gump (drag / close / minimize / z-focus).
    let root = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(20.0, 150.0, bw as f32, bh as f32)),
        ComponentValue::Movable,
        ComponentValue::ContainsByBounds,
        ComponentValue::GlobalZ(ZRec { value: 100 }),
        ComponentValue::UiName(NameRec { value: "mod.status.root".into() }),
    ]);
    root.insert(&[
        // Background tinted with UO hue 0x38 (mode 1 = HUED).
        ("cuo:ui/custom".into(), gump_json_hue(bg, 0x38, 1, 1)),
        // Stamp the minimize-region origin (legacy StatusGumpBase._point) so the
        // host only minimizes on a click in the bottom-right corner — not anywhere
        // on the panel, which would also eat stat-lock/buff clicks. UOP modern
        // (540,180), classic (244,112). Mirrors the host StatusBarPlugin.
        ("cuo:ui/statusbar-window".into(), {
            let (mx, my) = if modern { (540.0, 180.0) } else { (244.0, 112.0) };
            format!("{{\"MinimizeX\":{mx},\"MinimizeY\":{my}}}")
        }),
    ]);

    let layout = if modern { MODERN } else { OLD };
    for &(x, y, f, cw) in layout {
        let w = if cw > 0 { cw } else { 60 };
        let lbl = commands.spawn_typed(&[
            ComponentValue::Node(abs_node(x as f32, y as f32, w as f32, 15.0)),
            ComponentValue::Text(TextRec { value: String::new() }),
            ComponentValue::TextFont(TextFontRec { font_id: ASCII_FONT, size: 12 }),
            ComponentValue::TextColor(label_color()),
            // encode field + box-left + top + center-width for refresh/centering.
            ComponentValue::UiName(NameRec { value: format!("mod.status.lbl.{f}.{x}.{y}.{cw}") }),
        ]);
        let cliloc = field_cliloc(f);
        if cliloc != 0 {
            lbl.insert_typed(&[ComponentValue::ContainsByBounds]);
            lbl.insert(&[("cuo:ui/tooltip".into(), tooltip_json(&ui.resolve_cliloc(cliloc)))]);
        }
        root.add_child(&lbl.id(), u32::MAX);
    }

    if !modern {
        println!("[status-mod] panel opened (old)");
        return;
    }

    // Stat-lock toggles (Str/Dex/Int) — UOP xOffset 28, y 76/102/132. The marker
    // makes them minimize-exempt; the host Refresh sets the lock graphic.
    let (lw, lh) = unpack(ui.gump_size(LOCK_UP));
    for (stat, ly) in [(0u8, 76.0f32), (1, 102.0), (2, 132.0)] {
        let btn = commands.spawn_typed(&[
            ComponentValue::Node(abs_node(28.0, ly, lw as f32, lh as f32)),
            ComponentValue::Interaction(0),
            ComponentValue::NoWindowDrag,
            ComponentValue::UiName(NameRec { value: format!("mod.status.lock.{stat}") }),
        ]);
        btn.insert(&[
            ("cuo:ui/custom".into(), gump_json(LOCK_UP)),
            ("cuo:ui/stat-lock-button".into(), format!("{{\"Stat\":{stat}}}")),
        ]);
        root.add_child(&btn.id(), u32::MAX);
    }

    // Dark divider lines under each current value (legacy Line(..., 0xFF383838)).
    for (lx, ly, lwid) in [
        (150.0f32, 82.0f32, 35.0f32),
        (150.0, 110.0, 35.0),
        (150.0, 138.0, 35.0),
        (236.0, 138.0, 34.0),
    ] {
        let line = commands.spawn_typed(&[
            ComponentValue::Node(abs_node(lx, ly, lwid, 1.0)),
            ComponentValue::BgColor(color(56.0, 56.0, 56.0, 255.0)),
        ]);
        root.add_child(&line.id(), u32::MAX);
    }

    println!("[status-mod] panel opened (modern)");
}

// ── label text (mirrors StatusBarPlugin.FormatField) ────────────────────────
fn format_field(
    f: u8,
    d: &serde_json::Value,
    hits: (i64, i64),
    mana: (i64, i64),
    stam: (i64, i64),
    name: &str,
) -> String {
    let gi = |k: &str| d[k].as_i64().unwrap_or(0);
    match f {
        field::NAME => name.to_string(),
        field::SEX => {
            if d["IsFemale"].as_bool().unwrap_or(false) { "Female" } else { "Male" }.to_string()
        }
        field::STR => gi("Str").to_string(),
        field::DEX => gi("Dex").to_string(),
        field::INT => gi("Int").to_string(),
        field::HITS => hits.0.to_string(),
        field::HITS_MAX => hits.1.to_string(),
        field::MANA => mana.0.to_string(),
        field::MANA_MAX => mana.1.to_string(),
        field::STAM => stam.0.to_string(),
        field::STAM_MAX => stam.1.to_string(),
        field::HITS_COMBINED => format!("{}/{}", hits.0, hits.1),
        field::MANA_COMBINED => format!("{}/{}", mana.0, mana.1),
        field::STAM_COMBINED => format!("{}/{}", stam.0, stam.1),
        field::WEIGHT_COMBINED => format!("{}/{}", gi("Weight"), gi("WeightMax")),
        field::ARMOR_OLD => gi("PhysicalRes").to_string(),
        field::WEIGHT => gi("Weight").to_string(),
        field::WEIGHT_MAX => gi("WeightMax").to_string(),
        field::STAT_CAP => gi("StatsCap").to_string(),
        field::LUCK => gi("Luck").to_string(),
        field::GOLD => gi("Gold").to_string(),
        field::DAMAGE => format!("{}-{}", gi("DamageMin"), gi("DamageMax")),
        field::FOLLOWERS => format!("{}/{}", gi("Followers"), gi("FollowersMax")),
        field::HIT_CHANCE_INC => gi("HitChanceInc").to_string(),
        field::DEFENSE_CHANCE_INC => {
            format!("{}/{}", gi("DefenseChanceInc"), gi("MaxDefenseChanceInc"))
        }
        field::SWING_SPEED_INC => gi("SwingSpeedInc").to_string(),
        field::DAMAGE_INC => gi("DamageInc").to_string(),
        field::LOWER_MANA_COST => gi("LowerManaCost").to_string(),
        field::LOWER_REAGENT_COST => gi("LowerReagentCost").to_string(),
        field::SPELL_DAMAGE_INC => gi("SpellDamageInc").to_string(),
        field::FASTER_CASTING => gi("FasterCasting").to_string(),
        field::FASTER_CAST_RECOVERY => gi("FasterCastRecovery").to_string(),
        field::ARMOR => format!("{}/{}", gi("PhysicalRes"), gi("MaxPhysicalRes")),
        field::FIRE_RES => format!("{}/{}", gi("FireRes"), gi("MaxFireRes")),
        field::COLD_RES => format!("{}/{}", gi("ColdRes"), gi("MaxColdRes")),
        field::POISON_RES => format!("{}/{}", gi("PoisonRes"), gi("MaxPoisonRes")),
        field::ENERGY_RES => format!("{}/{}", gi("EnergyRes"), gi("MaxEnergyRes")),
        _ => String::new(),
    }
}

// Tooltip cliloc per field (mirrors StatusBarPlugin.FieldCliloc). 0 = none.
fn field_cliloc(f: u8) -> u32 {
    match f {
        field::STR => 1061146,
        field::DEX => 1061147,
        field::INT => 1061148,
        field::HITS | field::HITS_MAX | field::HITS_COMBINED => 1061149,
        field::MANA | field::MANA_MAX | field::MANA_COMBINED => 1061151,
        field::STAM | field::STAM_MAX | field::STAM_COMBINED => 1061150,
        field::WEIGHT | field::WEIGHT_MAX | field::WEIGHT_COMBINED => 1061154,
        field::STAT_CAP => 1061152,
        field::LUCK => 1061153,
        field::GOLD => 1061156,
        field::DAMAGE => 1061155,
        field::FOLLOWERS => 1061157,
        field::HIT_CHANCE_INC => 1075616,
        field::DEFENSE_CHANCE_INC => 1075620,
        field::SWING_SPEED_INC => 1075629,
        field::DAMAGE_INC => 1075619,
        field::LOWER_MANA_COST => 1075621,
        field::LOWER_REAGENT_COST => 1075625,
        field::SPELL_DAMAGE_INC => 1075628,
        field::FASTER_CASTING => 1075617,
        field::FASTER_CAST_RECOVERY => 1075618,
        field::ARMOR => 1061158,
        field::FIRE_RES => 1061159,
        field::COLD_RES => 1061160,
        field::POISON_RES => 1061161,
        field::ENERGY_RES => 1061162,
        field::SEX => 3000076,
        field::ARMOR_OLD => 1062760,
        _ => 0,
    }
}

// ── typed record builders ────────────────────────────────────────────────────
fn auto() -> Val {
    Val { kind: 0, value: 0.0 }
}
fn px(v: f32) -> Val {
    Val { kind: 1, value: v }
}
fn zero_rect() -> UiRect {
    UiRect { left: px(0.0), right: px(0.0), top: px(0.0), bottom: px(0.0) }
}
fn base_node() -> NodeRec {
    NodeRec {
        display: Display::Flex,
        position_type: PositionType::Relative,
        overflow: Overflow::Visible,
        flex_direction: FlexDirection::Row,
        justify_content: JustifyContent::Start,
        align_items: AlignItems::Start,
        width: auto(),
        height: auto(),
        min_width: auto(),
        min_height: auto(),
        max_width: auto(),
        max_height: auto(),
        left: auto(),
        top: auto(),
        right: auto(),
        bottom: auto(),
        padding: zero_rect(),
        border: zero_rect(),
        gap: px(0.0),
        aspect_ratio: 0.0,
    }
}
/// Absolutely-positioned, fixed-size node.
fn abs_node(left: f32, top: f32, w: f32, h: f32) -> NodeRec {
    NodeRec {
        position_type: PositionType::Absolute,
        left: px(left),
        top: px(top),
        width: px(w),
        height: px(h),
        ..base_node()
    }
}
fn color(r: f32, g: f32, b: f32, a: f32) -> Color {
    Color { r, g, b, a }
}
/// Legacy status label hue 0x0386 baked the host way: the ASCII font path reads
/// TextColor as a PACKED UO hue (R=low byte, G=high byte), not an RGB tint.
fn label_color() -> Color {
    color((0x0386 & 0xFF) as f32, (0x0386 >> 8) as f32, 0.0, 255.0)
}

// ── JSON helpers ──────────────────────────────────────────────────────────────
fn parse(s: &str) -> serde_json::Value {
    serde_json::from_str(s).unwrap_or(serde_json::Value::Null)
}
fn name_of(c: &Component) -> String {
    parse(&c.get())["Value"].as_str().unwrap_or("").to_string()
}
fn entity_name(e: &Entity) -> String {
    parse(&e.get("cuo:ui/name"))["Value"].as_str().unwrap_or("").to_string()
}
fn get_pair(e: &Entity, path: &str) -> (i64, i64) {
    let v = parse(&e.get(path));
    (v["Value"].as_i64().unwrap_or(0), v["MaxValue"].as_i64().unwrap_or(0))
}
fn get_locks(e: &Entity) -> [u8; 3] {
    let v = parse(&e.get("cuo:player/stat-locks"));
    [
        v["Str"].as_u64().unwrap_or(0) as u8,
        v["Dex"].as_u64().unwrap_or(0) as u8,
        v["Int"].as_u64().unwrap_or(0) as u8,
    ]
}
fn locks_json(l: &[u8; 3]) -> String {
    format!("{{\"Str\":{},\"Dex\":{},\"Int\":{}}}", l[0], l[1], l[2])
}
fn gump_json(id: u16) -> String {
    // UiCustomDto { Kind=0 (Gump), AssetId, Hue = Vector3.UnitZ (0,0,1) = no hue }.
    gump_json_hue(id, 0, 0, 1)
}
/// Gump with an explicit ShaderHue vector. Matches HealthBarPlugin.ToShaderHue:
/// X = UO hue id, Y = mode (1 = HUED), Z = 1. Hue id 0 means no hue (UnitZ).
fn gump_json_hue(id: u16, hx: i32, hy: i32, hz: i32) -> String {
    format!("{{\"Kind\":0,\"AssetId\":{id},\"HueX\":{hx},\"HueY\":{hy},\"HueZ\":{hz}}}")
}
fn tooltip_json(text: &str) -> String {
    serde_json::json!({ "Text": text, "MaxWidth": 0 }).to_string()
}
/// Parse "mod.status.lbl.{field}.{x}.{y}.{cw}" → (field, x, y, cw).
fn parse_label(name: &str) -> Option<(u8, i32, i32, i32)> {
    let rest = name.strip_prefix("mod.status.lbl.")?;
    let mut it = rest.split('.');
    let f = it.next()?.parse().ok()?;
    let x = it.next()?.parse().ok()?;
    let y = it.next()?.parse().ok()?;
    let cw = it.next()?.parse().ok()?;
    Some((f, x, y, cw))
}
/// Unpack (width << 16) | height from cuo:modding/ui gump-size.
fn unpack(packed: u32) -> (u32, u32) {
    (packed >> 16, packed & 0xFFFF)
}

export!(StatusMod);
