// Demo mod: enlarges the topbar and appends a "[MOD]" button flush at the end,
// then counts clicks on it — all through the tinyecs:modding API.
//
// Flow: once the topbar exists, read the bar width from the group node, widen
// the root + group + marble background by the button width, spawn the button
// at the old right end, and parent it on top. Each frame, if any mod entity is
// clicked (cuo:ui/clicked), bump a counter.

wit_bindgen::generate!({
    world: "topbar-mod",
    path: "wit",
    with: {
        "tinyecs:modding/app@0.1.0": generate,
    },
});

// App/Commands/Query are already in scope from the generated bindings.
use crate::tinyecs::modding::app::{
    Color, Component, ComponentValue, Entity, NodeRec, QueryFor, Schedule, System, TextFontRec,
    TextRec, UiRect, Val,
};
use std::sync::atomic::{AtomicBool, Ordering};

static ADDED: AtomicBool = AtomicBool::new(false);

const GAP: f64 = 2.0;

struct TopbarMod;

impl Guest for TopbarMod {
    fn setup(app: App) {
        let init = System::new("mod-ui-init");
        init.add_commands();
        // Node handles to widen: group, root, marble bg. Plus a sample button
        // (ref) to copy its size so ours matches the others.
        init.add_query(&[QueryFor::Mut("cuo:ui/node".into()), QueryFor::With("cuo:ui/topbar-full".into())]);
        init.add_query(&[QueryFor::Mut("cuo:ui/node".into()), QueryFor::With("cuo:ui/topbar-root".into())]);
        init.add_query(&[QueryFor::Mut("cuo:ui/node".into()), QueryFor::With("cuo:ui/topbar-bg".into())]);
        init.add_query(&[QueryFor::Ref("cuo:ui/node".into()), QueryFor::With("cuo:ui/topbar-button".into())]);
        app.add_systems(&Schedule::Update, &[&init]);

        let tick = System::new("mod-ui-tick");
        tick.add_query(&[QueryFor::With("cuo:ui/clicked".into())]);
        tick.add_query(&[QueryFor::Mut("cuo:test/counter".into())]);
        app.add_systems(&Schedule::Update, &[&tick]);

        // Pure-mod gump editing: find the options window + inject a section.
        let opts = System::new("mod-options");
        opts.add_commands();
        opts.add_query(&[QueryFor::With("cuo:ui/options-window".into())]);
        app.add_systems(&Schedule::Update, &[&opts]);

        // Hover/press feedback on the mod button, matching host topbar buttons.
        // Scoped to our button by its unique cuo:test/counter marker.
        let feedback = System::new("mod-button-feedback");
        feedback.add_commands();
        feedback.add_query(&[
            QueryFor::Ref("cuo:ui/interaction".into()),
            QueryFor::With("cuo:test/counter".into()),
        ]);
        app.add_systems(&Schedule::Update, &[&feedback]);

        println!("[topbar-mod] setup");
    }

    fn mod_ui_init(commands: Commands, group: Query, root: Query, bg: Query, sample: Query) {
        if ADDED.load(Ordering::Relaxed) {
            return;
        }
        // Group must exist (topbar spawns on entering the game).
        let Some(grow) = group.iter() else {
            return;
        };
        let group_ent = grow.entity();
        let group_node = grow.component(0);

        let old_w = node_dim(&group_node, "Width");
        if old_w <= 0.0 {
            return;
        }

        // Copy a real topbar button's size so ours matches exactly.
        let (bw, bh) = match sample.iter() {
            Some(s) => {
                let n = s.component(0);
                (node_dim(&n, "Width"), node_dim(&n, "Height"))
            }
            None => (46.0, 26.0),
        };
        let new_w = old_w + GAP + bw;

        // Enlarge the bar: group + root + marble bg all stretch to the new width.
        set_node_width(&group_node, new_w);
        if let Some(r) = root.iter() {
            set_node_width(&r.component(0), new_w);
        }
        while let Some(b) = bg.iter() {
            set_node_width(&b.component(0), new_w);
        }

        // The button: same marble gump (0x098B = 2443) the stock buttons use.
        // It carries its own click counter (cuo:test/counter) that mod-ui-tick
        // bumps each frame a click lands on a mod entity.
        let btn = commands.spawn(&[
            ("cuo:ui/node".into(), abs_node(old_w + GAP, 1.0, bw, bh)),
            ("cuo:ui/custom".into(), "{\"Kind\":0,\"AssetId\":2443,\"HueX\":0,\"HueY\":0,\"HueZ\":1}".into()),
            ("cuo:ui/interaction".into(), "0".into()),
            ("cuo:test/counter".into(), "{\"Value\":0}".into()),
        ]);
        let btn_ent = btn.id();
        commands.entity(&group_ent).add_child(&btn_ent, u32::MAX);

        // Black caption, centered, child of the button (so clicks bubble up) —
        // same font/colour as the stock captions.
        // Typed component-values (no JSON) — every caption component has a
        // case in the host's component-value variant.
        let cap = commands.spawn_typed(&[
            abs_node_cv((bw / 2.0 - 13.0).max(0.0), 5.0, 28.0, bh),
            ComponentValue::Text(TextRec { value: "MOD".into() }),
            ComponentValue::TextFont(TextFontRec { font_id: 1, size: 12 }),
            ComponentValue::TextColor(Color { r: 0.0, g: 0.0, b: 0.0, a: 255.0 }),
            // Interaction so the text captures hover/press over its own area
            // (Text blocks the pointer regardless) and clicks bubble to the
            // parent button — matching host caption behaviour.
            ComponentValue::Interaction(0),
        ]);
        btn.add_child(&cap.id(), u32::MAX);

        ADDED.store(true, Ordering::Relaxed);
        println!("[topbar-mod] bar widened {} -> {}, marble button appended", old_w, new_w);
    }

    // Per-frame hover/press feedback for the mod button, mirroring the host
    // topbar buttons (PressOffsetCaptions): the CAPTION text yellows on
    // hover/press and nudges 1px down on press; the marble never changes.
    // Reads BOTH the button's and the caption's Interaction (None=0/Hovered=1/
    // Pressed=2, written by the host) and recolours/nudges the caption child
    // via Commands. A Text element blocks the pointer over its own area, so the
    // caption owns the text region (and clicks on the text bubble to the parent
    // button) while the marble owns the rest — together the whole button reacts.
    fn mod_button_feedback(commands: Commands, buttons: Query) {
        while let Some(row) = buttons.iter() {
            let btn_inter = parse_value(&row.component(0).get());

            // The caption is the button's only child.
            let children = row.entity().children();
            let Some(cap) = children.into_iter().next() else { continue };

            // Honour whichever of the button (marble) or caption (text) the
            // pointer is over — Text blocks the pointer for its own area, the
            // marble owns the rest. None=0 < Hovered=1 < Pressed=2, so max wins.
            let cap_inter = parse_value(&cap.get("cuo:ui/interaction"));
            let interaction = btn_inter.max(cap_inter);
            let over = interaction >= 1;
            let pressed = interaction == 2;

            // Caption colour, typed: hover = UO hue 0x36 polygone (≈239,239,49), rest black.
            let color = if over {
                Color { r: 239.0, g: 239.0, b: 49.0, a: 255.0 }
            } else {
                Color { r: 0.0, g: 0.0, b: 0.0, a: 255.0 }
            };
            commands.entity(&cap).insert_typed(&[ComponentValue::TextColor(color)]);
            // Node Top stays JSON: a 1px partial patch beats rebuilding the full node-rec.
            let node = patch_top(&cap.get("cuo:ui/node"), if pressed { 6.0 } else { 5.0 });
            commands.entity(&cap).insert(&[("cuo:ui/node".into(), node)]);
        }
    }

    fn mod_ui_tick(clicked: Query, counters: Query) {
        let mut any_click = false;
        while clicked.iter().is_some() {
            any_click = true;
        }
        if !any_click {
            return;
        }
        while let Some(row) = counters.iter() {
            let c = row.component(0);
            let n = parse_value(&c.get());
            c.set(&format!("{{\"Value\":{}}}", n + 1));
            println!("[topbar-mod] click -> {}", n + 1);
        }
    }

    fn mod_options(commands: Commands, windows: Query) {
        // Find the options window (its marker already exists on the gump root).
        let Some(wr) = windows.iter() else {
            return; // options gump not open
        };
        let root = wr.entity();

        // Walk the tree to the scroll viewport (a descendant with ScrollPosition) —
        // pure traversal + component inspection, no host cooperation.
        let Some(viewport) = find_descendant(&root, "cuo:ui/scroll") else {
            return;
        };

        // Idempotent: bail if our section is already a child.
        for child in viewport.children() {
            if entity_name(&child) == "mod.options.section" {
                return;
            }
        }

        // Inject a section into the existing options list (additive → survives rebuilds).
        let sec = commands.spawn(&[
            ("cuo:ui/node".into(), "{\"PositionType\":0,\"Width\":{\"Type\":1,\"Value\":300},\"Height\":{\"Type\":1,\"Value\":40},\"JustifyContent\":1,\"AlignItems\":1}".into()),
            ("cuo:ui/name".into(), "{\"Value\":\"mod.options.section\"}".into()),
            ("cuo:ui/bg-color".into(), "{\"Value\":{\"R\":40,\"G\":90,\"B\":160,\"A\":255}}".into()),
            ("cuo:ui/text".into(), "{\"Value\":\"== INJECTED BY MOD ==\"}".into()),
            ("cuo:ui/text-font".into(), "{\"FontId\":1,\"Size\":13}".into()),
            ("cuo:ui/text-color".into(), "{\"Value\":{\"R\":255,\"G\":255,\"B\":255,\"A\":255}}".into()),
            ("cuo:ui/interaction".into(), "0".into()),
        ]);
        commands.entity(&viewport).add_child(&sec.id(), 0); // top of the list
        println!("[topbar-mod] injected options section via traversal");
    }
}

/// Depth-first: first descendant of `e` that has component `type_path`.
fn find_descendant(e: &Entity, type_path: &str) -> Option<Entity> {
    for child in e.children() {
        if child.get(type_path) != "null" {
            return Some(child);
        }
        if let Some(found) = find_descendant(&child, type_path) {
            return Some(found);
        }
    }
    None
}

/// This entity's UiName Value, or "" if none.
fn entity_name(e: &Entity) -> String {
    let v: serde_json::Value = serde_json::from_str(&e.get("cuo:ui/name")).unwrap_or(serde_json::Value::Null);
    v["Value"].as_str().unwrap_or("").to_string()
}

/// Read Node.<field>.Value (a Val) from the serialized Node JSON.
fn node_dim(c: &Component, field: &str) -> f64 {
    let v: serde_json::Value = serde_json::from_str(&c.get()).unwrap_or(serde_json::Value::Null);
    v[field]["Value"].as_f64().unwrap_or(0.0)
}

/// Build an absolute-positioned Node JSON.
fn abs_node(left: f64, top: f64, w: f64, h: f64) -> String {
    format!(
        "{{\"PositionType\":1,\"Left\":{{\"Type\":1,\"Value\":{}}},\"Top\":{{\"Type\":1,\"Value\":{}}},\"Width\":{{\"Type\":1,\"Value\":{}}},\"Height\":{{\"Type\":1,\"Value\":{}}}}}",
        left, top, w, h
    )
}

// val kinds: 0=auto, 1=px, 2=percent.
fn px(v: f64) -> Val { Val { kind: 1, value: v as f32 } }
fn auto() -> Val { Val { kind: 0, value: 0.0 } }
fn rect_auto() -> UiRect { UiRect { left: auto(), right: auto(), top: auto(), bottom: auto() } }

// Typed equivalent of abs_node: an absolute node with only left/top/width/height
// set in px; every other field defaults (auto / 0), matching the JSON path where
// omitted fields take the host's defaults.
fn abs_node_cv(left: f64, top: f64, w: f64, h: f64) -> ComponentValue {
    ComponentValue::Node(NodeRec {
        display: 0,
        position_type: 1, // absolute
        overflow: 0,
        flex_direction: 0,
        justify_content: 0,
        align_items: 0,
        width: px(w),
        height: px(h),
        min_width: auto(),
        min_height: auto(),
        max_width: auto(),
        max_height: auto(),
        left: px(left),
        top: px(top),
        right: auto(),
        bottom: auto(),
        padding: rect_auto(),
        border: rect_auto(),
        gap: auto(),
        aspect_ratio: 0.0,
    })
}

/// Rewrite Node.Width.Value, preserving every other field.
fn set_node_width(c: &Component, w: f64) {
    let mut v: serde_json::Value = match serde_json::from_str(&c.get()) {
        Ok(v) => v,
        Err(_) => return,
    };
    v["Width"]["Value"] = serde_json::json!(w);
    c.set(&v.to_string());
}

// Return `node_json` with Node.Top.Value set to `top`, preserving every other
// field. Used to nudge the caption on press (it's written back via Commands,
// not a query handle, so this works on the raw string).
fn patch_top(node_json: &str, top: f64) -> String {
    match serde_json::from_str::<serde_json::Value>(node_json) {
        Ok(mut v) => {
            v["Top"]["Value"] = serde_json::json!(top);
            v.to_string()
        }
        Err(_) => node_json.to_string(),
    }
}

fn parse_value(s: &str) -> i64 {
    s.chars()
        .filter(|c| c.is_ascii_digit() || *c == '-')
        .collect::<String>()
        .parse()
        .unwrap_or(0)
}

export!(TopbarMod);
