// Network-log mod: a movable window that LIVE-lists incoming packets (ID + size
// + hex). Capture is driven by the cuo:net/incoming custom event. Rows append as
// packets arrive. Buttons:
//   - STOP/RUN  freezes / resumes capture (the list keeps what it has).
//   - CLEAR     drops stored packets and the rows.
//
// An observer callback gets no Commands, so on-packet only appends to a guest-side
// buffer; mod-netlog-tick (which owns Commands) appends the new rows each frame.

wit_bindgen::generate!({
    world: "netlog-mod",
    path: "wit",
    with: { "tinyecs:modding/app@0.1.0": generate },
});

use crate::tinyecs::modding::app::{
    Color, Component, ComponentValue, Entity, EntityCommands, NameRec, NodeRec, ObserverEvent,
    QueryFor, Schedule, System, TextFontRec, TextRec, UiRect, Val, ZRec,
};
use base64::Engine as _;
use std::sync::atomic::{AtomicBool, AtomicUsize, Ordering::Relaxed};
use std::sync::Mutex;

// ── tunables ──────────────────────────────────────────────────────────────────
const WIN_W: f32 = 440.0;
const WIN_H: f32 = 300.0;
const ROW_H: f32 = 16.0;
const BYTES_PER_ROW: usize = 16; // "Payload formatted in 16 HEX"
const MAX_PACKETS: usize = 256; // capture stops accepting past this (CLEAR resets)
// Unicode UI font (NO 0x80 AsciiFlag) — crisp/antialiased, unlike the bitmap
// ASCII fonts. TextColor on the unicode path is an RGB tint (not a packed hue).
// ponytail: font_id + TEXT colour are the two knobs to tune the look.
const UI_FONT: u16 = 1;

struct Packet {
    id: u8,
    bytes: Vec<u8>,
}

// Guest-side state (wasm instance is single-threaded; Mutex is uncontended).
static PACKETS: Mutex<Vec<Packet>> = Mutex::new(Vec::new());
static CAPTURING: AtomicBool = AtomicBool::new(true);
static SPAWNED: AtomicBool = AtomicBool::new(false);
// How many of PACKETS are already rendered as rows. PACKETS never evicts (it caps
// instead), so this index stays valid until CLEAR resets both.
static RENDERED: AtomicUsize = AtomicUsize::new(0);
// Autoscroll: pin the list to the newest row as packets arrive (checkbox toggles
// it). Default on for a live log.
static AUTOSCROLL: AtomicBool = AtomicBool::new(true);

struct NetlogMod;

impl Guest for NetlogMod {
    fn setup(app: App) {
        let init = System::new("mod-netlog-init");
        init.add_commands();
        app.add_systems(&Schedule::Update, &[&init]);

        let tick = System::new("mod-netlog-tick");
        tick.add_commands();
        tick.add_query(&[
            QueryFor::With("cuo:ui/clicked".into()),
            QueryFor::Ref("cuo:ui/name".into()),
        ]);
        // Named entities — used to find the list container + stop label. Rows are
        // deliberately UNnamed so they don't bloat this scan as the log grows.
        tick.add_query(&[QueryFor::Ref("cuo:ui/name".into())]);
        app.add_systems(&Schedule::Update, &[&tick]);

        app.add_observer("on-packet", &ObserverEvent::Custom("cuo:net/incoming".into()));
        println!("[netlog] setup");
    }

    fn mod_netlog_init(commands: Commands) {
        if SPAWNED.swap(true, Relaxed) {
            return; // one-shot (right-click-close won't reopen — v1)
        }
        build_window(&commands);
        println!("[netlog] window spawned");
    }

    fn mod_netlog_tick(commands: Commands, clicked: Query, named: Query) {
        // 1) our control clicks (cheap: only clicked entities).
        let mut toggle = false;
        let mut clear = false;
        let mut toggle_auto = false;
        while let Some(row) = clicked.iter() {
            // Match the control AND its label/mark child (each is independently
            // interactive — a click can land on either).
            match name_of(&row.component(0)).as_str() {
                "netlog.btn.stop" | "netlog.btn.stop.label" => toggle = true,
                "netlog.btn.clear" | "netlog.btn.clear.label" => clear = true,
                "netlog.chk.autoscroll" | "netlog.chk.mark" => toggle_auto = true,
                _ => {}
            }
        }
        if toggle {
            CAPTURING.store(!CAPTURING.load(Relaxed), Relaxed);
        }
        if clear {
            PACKETS.lock().unwrap().clear();
            RENDERED.store(0, Relaxed);
        }
        if toggle_auto {
            AUTOSCROLL.store(!AUTOSCROLL.load(Relaxed), Relaxed);
        }

        let pending = PACKETS.lock().unwrap().len() > RENDERED.load(Relaxed);
        if !toggle && !clear && !toggle_auto && !pending {
            return; // idle → no scan
        }

        // 2) find the list container + the stop-button label + the checkbox mark
        // (rows are unnamed, so they don't bloat this scan).
        let mut list: Option<Entity> = None;
        let mut label: Option<Entity> = None;
        let mut mark: Option<Entity> = None;
        while let Some(row) = named.iter() {
            match name_of(&row.component(0)).as_str() {
                "netlog.list" => list = Some(row.entity()),
                "netlog.btn.stop.label" => label = Some(row.entity()),
                "netlog.chk.mark" => mark = Some(row.entity()),
                _ => {}
            }
        }

        if toggle {
            if let Some(lbl) = &label {
                let txt = if CAPTURING.load(Relaxed) { "STOP" } else { "RUN" };
                commands.entity(lbl).insert_typed(&[ComponentValue::Text(TextRec { value: txt.into() })]);
            }
        }
        if toggle_auto {
            if let Some(m) = &mark {
                let txt = if AUTOSCROLL.load(Relaxed) { "X" } else { "" };
                commands.entity(m).insert_typed(&[ComponentValue::Text(TextRec { value: txt.into() })]);
            }
        }

        let Some(list) = list else {
            return;
        };

        if clear {
            for child in list.children() {
                commands.entity(&child).despawn();
            }
            return; // PACKETS + RENDERED already reset
        }

        // 3) append rows for packets captured since the last append (live).
        let appended = {
            let listc = commands.entity(&list);
            let pkts = PACKETS.lock().unwrap();
            let start = RENDERED.load(Relaxed);
            for p in &pkts[start..] {
                for line in hex_lines(p.id, &p.bytes) {
                    let r = spawn_row(&commands, line);
                    listc.add_child(&r.id(), u32::MAX);
                }
            }
            let n = pkts.len();
            RENDERED.store(n, Relaxed);
            n > start
        };

        // 4) autoscroll: pin to the bottom when new rows arrived (or the box was
        // just ticked). A huge OffsetY is clamped to maxY by the layout pass.
        if AUTOSCROLL.load(Relaxed) && (appended || toggle_auto) {
            commands
                .entity(&list)
                .insert(&[("cuo:ui/scroll".into(), "{\"OffsetX\":0,\"OffsetY\":1000000}".into())]);
        }
    }

    fn on_packet(_entity: u64, json: String) {
        if !CAPTURING.load(Relaxed) {
            return;
        }
        let v: serde_json::Value = serde_json::from_str(&json).unwrap_or(serde_json::Value::Null);
        let id = v["Id"].as_u64().unwrap_or(0) as u8;
        let bytes = base64::engine::general_purpose::STANDARD
            .decode(v["Payload"].as_str().unwrap_or(""))
            .unwrap_or_default();
        let mut p = PACKETS.lock().unwrap();
        if p.len() < MAX_PACKETS {
            p.push(Packet { id, bytes });
        }
    }
}

// ── window construction ───────────────────────────────────────────────────────
fn build_window(commands: &Commands) {
    let root = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(30.0, 120.0, WIN_W, WIN_H)),
        ComponentValue::BgColor(color(18.0, 18.0, 26.0, 235.0)),
        ComponentValue::Movable,
        ComponentValue::ContainsByBounds,
        ComponentValue::GlobalZ(ZRec { value: 100 }),
        ComponentValue::UiName(NameRec { value: "netlog.root".into() }),
    ]);

    let title = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(10.0, 6.0, WIN_W - 20.0, 18.0)),
        ComponentValue::Text(TextRec { value: "NET LOG  (incoming packets)".into() }),
        ComponentValue::TextFont(TextFontRec { font_id: UI_FONT, size: 15 }),
        ComponentValue::TextColor(text_color()),
    ]);
    root.add_child(&title.id(), u32::MAX);

    let stop = button(commands, 10.0, 28.0, 150.0, "netlog.btn.stop");
    // The label is an ABSOLUTE child → Clay-floating → it captures the pointer and
    // hides the parent button from the hit list. So a click on the text never
    // reaches the button. Give the label its own Interaction (+NoWindowDrag so the
    // window drag doesn't steal the press) and route its name to the same action.
    let stop_lbl = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(10.0, 3.0, 140.0, 16.0)),
        ComponentValue::Text(TextRec { value: "STOP".into() }),
        ComponentValue::TextFont(TextFontRec { font_id: UI_FONT, size: 13 }),
        ComponentValue::TextColor(text_color()),
        ComponentValue::Interaction(0),
        ComponentValue::NoWindowDrag,
        ComponentValue::UiName(NameRec { value: "netlog.btn.stop.label".into() }),
    ]);
    stop.add_child(&stop_lbl.id(), u32::MAX);
    root.add_child(&stop.id(), u32::MAX);

    let clear = button(commands, 170.0, 28.0, 100.0, "netlog.btn.clear");
    let clear_lbl = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(10.0, 3.0, 90.0, 16.0)),
        ComponentValue::Text(TextRec { value: "CLEAR".into() }),
        ComponentValue::TextFont(TextFontRec { font_id: UI_FONT, size: 13 }),
        ComponentValue::TextColor(text_color()),
        ComponentValue::Interaction(0),
        ComponentValue::NoWindowDrag,
        ComponentValue::UiName(NameRec { value: "netlog.btn.clear.label".into() }),
    ]);
    clear.add_child(&clear_lbl.id(), u32::MAX);
    root.add_child(&clear.id(), u32::MAX);

    // Autoscroll checkbox: a small interactive box + an "X" mark child (shown when
    // on) + a plain label. Clicking the box toggles AUTOSCROLL.
    let chk = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(290.0, 30.0, 18.0, 18.0)),
        ComponentValue::BgColor(color(40.0, 40.0, 58.0, 255.0)),
        ComponentValue::Interaction(0),
        ComponentValue::NoWindowDrag,
        ComponentValue::UiName(NameRec { value: "netlog.chk.autoscroll".into() }),
    ]);
    let mark = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(4.0, 0.0, 12.0, 16.0)),
        ComponentValue::Text(TextRec { value: "X".into() }), // default on
        ComponentValue::TextFont(TextFontRec { font_id: UI_FONT, size: 13 }),
        ComponentValue::TextColor(text_color()),
        ComponentValue::Interaction(0), // own handler (absolute child captures the pointer)
        ComponentValue::NoWindowDrag,
        ComponentValue::UiName(NameRec { value: "netlog.chk.mark".into() }),
    ]);
    chk.add_child(&mark.id(), u32::MAX);
    root.add_child(&chk.id(), u32::MAX);

    let chk_lbl = commands.spawn_typed(&[
        ComponentValue::Node(abs_node(312.0, 31.0, 120.0, 16.0)),
        ComponentValue::Text(TextRec { value: "autoscroll".into() }),
        ComponentValue::TextFont(TextFontRec { font_id: UI_FONT, size: 13 }),
        ComponentValue::TextColor(text_color()),
    ]);
    root.add_child(&chk_lbl.id(), u32::MAX);

    // Scrollable list: flex column, Overflow.Scroll, ScrollPosition → wheel scrolls.
    let list = commands.spawn_typed(&[
        ComponentValue::Node(scroll_node(10.0, 54.0, WIN_W - 20.0, WIN_H - 64.0)),
        ComponentValue::UiName(NameRec { value: "netlog.list".into() }),
    ]);
    list.insert(&[("cuo:ui/scroll".into(), "{}".into())]);
    root.add_child(&list.id(), u32::MAX);
}

fn button(commands: &Commands, left: f32, top: f32, w: f32, name: &str) -> EntityCommands {
    commands.spawn_typed(&[
        ComponentValue::Node(abs_node(left, top, w, 22.0)),
        ComponentValue::BgColor(color(58.0, 58.0, 92.0, 255.0)),
        ComponentValue::Interaction(0),
        ComponentValue::NoWindowDrag,
        ComponentValue::UiName(NameRec { value: name.into() }),
    ])
}

fn spawn_row(commands: &Commands, line: String) -> EntityCommands {
    commands.spawn_typed(&[
        ComponentValue::Node(row_node()),
        ComponentValue::Text(TextRec { value: line }),
        ComponentValue::TextFont(TextFontRec { font_id: UI_FONT, size: 13 }),
        ComponentValue::TextColor(text_color()),
    ])
}

// ── packet → hex lines ────────────────────────────────────────────────────────
fn hex_lines(id: u8, bytes: &[u8]) -> Vec<String> {
    let mut out = vec![format!("0x{:02X}   size={}", id, bytes.len())];
    for chunk in bytes.chunks(BYTES_PER_ROW) {
        let hex: Vec<String> = chunk.iter().map(|b| format!("{:02X}", b)).collect();
        out.push(format!("     {}", hex.join(" ")));
    }
    out
}

// ── node / color helpers (mirror ecs-status) ─────────────────────────────────
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
        display: 0,
        position_type: 0,
        overflow: 0,
        flex_direction: 0,
        justify_content: 0,
        align_items: 0,
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
fn abs_node(left: f32, top: f32, w: f32, h: f32) -> NodeRec {
    NodeRec { position_type: 1, left: px(left), top: px(top), width: px(w), height: px(h), ..base_node() }
}
/// Absolute, scrollable flex column (Overflow.Scroll=2, FlexDirection.Column=1).
fn scroll_node(left: f32, top: f32, w: f32, h: f32) -> NodeRec {
    NodeRec {
        position_type: 1,
        display: 0,
        flex_direction: 1,
        overflow: 2,
        left: px(left),
        top: px(top),
        width: px(w),
        height: px(h),
        ..base_node()
    }
}
/// In-flow row inside the scroll column.
fn row_node() -> NodeRec {
    NodeRec { position_type: 0, width: px(WIN_W - 30.0), height: px(ROW_H), ..base_node() }
}
fn color(r: f32, g: f32, b: f32, a: f32) -> Color {
    Color { r, g, b, a }
}
/// Unicode-font text is tinted by an RGB ClayColor (0-255). White = untinted.
fn text_color() -> Color {
    color(235.0, 235.0, 245.0, 255.0)
}

fn name_of(c: &Component) -> String {
    serde_json::from_str::<serde_json::Value>(&c.get())
        .ok()
        .and_then(|v| v["Value"].as_str().map(|s| s.to_string()))
        .unwrap_or_default()
}

export!(NetlogMod);
