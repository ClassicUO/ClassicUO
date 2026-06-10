// UO ASSET IDE — WASM mod (JS / extism-js).
//
// A movable, multi-tab inspector for the client's art assets, built entirely
// from the modding UI bridge (cuo_ui_node + listeners). NO host gump code.
//
//   Tabs:  Animation | Static (item art) | Land (terrain tiles).
//   Each tab: a framed viewport that renders the live asset + an inspector
//   panel with an editable ID field, +/- steppers, and tab-specific controls
//   (action/direction/play-pause/speed for animation).
//
// Open: on GameStateChanged -> LoginScreen. Close: the title-bar X (or
// right-click the window).
//
//   host->mod : HostMessages (GameStateChanged / CustomEvent) in on_event.
//   mod->host : control clicks -> UI listeners in on_ui_event; CustomAction
//               pings logged host-side.
//
// Needs host UOCustomKind.Animation/Art/Land render + the cuo_ui_node `noDrag`
// flag + editable-text OnTextChanged events.

// ---------------------------------------------------------------- ABI helpers

const T_AUTO = 0, T_PX = 1, T_PCT = 2;
const AUTO = { type: T_AUTO, value: 0 };
const PX = (v) => ({ type: T_PX, value: v });
const PCT = (v) => ({ type: T_PCT, value: v });
const REL0 = { left: PX(0), right: PX(0), top: PX(0), bottom: PX(0) };
const pad = (l, t, r, b) => ({ left: PX(l), top: PX(t), right: PX(r), bottom: PX(b) });
const padAll = (v) => pad(v, v, v, v);
const rgba = (r, g, b, a) => ({ r, g, b, a: a === undefined ? 255 : a });

// FlexDirection / JustifyContent / AlignItems / Display.
const ROW = 0, COLUMN = 1;
const JC_START = 0, JC_CENTER = 1, JC_END = 2, JC_BETWEEN = 3;
const AI_START = 0, AI_CENTER = 1, AI_END = 2;
const D_FLEX = 0, D_NONE = 1;

// InputEventType.
const EV_RELEASED = 4, EV_TEXT_CHANGED = 11;

// UOCustomKind.
const K_GUMP = 0, K_NINEPATCH = 1, K_ART = 3, K_LAND = 4, K_ANIM = 5;

// Gump button graphic sets ({normal, over, pressed}).
const BTN_LEFT = { n: 1150, o: 1151, p: 1152 };   // 0x047E  «
const BTN_RIGHT = { n: 1153, o: 1154, p: 1155 };   // 0x0481  »
const BTN_X = { n: 1838, o: 1839, p: 1840 };       // 0x072E  X

// ----------------------------------------------------------------- IDE theme

const COL = {
  window: rgba(24, 26, 33, 248),
  windowBorder: rgba(70, 78, 96),
  titleBar: rgba(38, 42, 54),
  titleText: rgba(232, 236, 245),
  tabIdle: rgba(34, 38, 48),
  tabActive: rgba(58, 110, 190),
  tabText: rgba(210, 216, 228),
  panelBorder: rgba(60, 66, 82),
  viewport: rgba(12, 13, 17),
  label: rgba(150, 158, 176),
  value: rgba(236, 240, 248),
  field: rgba(30, 33, 42),
  fieldBorder: rgba(80, 120, 200),
  accent: rgba(120, 200, 140),
  closeBtn: rgba(170, 60, 64),
};

// Layout metrics.
const W = 360, H = 300, X = 200, Y = 90;
const VP = 150;            // viewport square
const TITLE_H = 26, TAB_H = 24, ROW_H = 24, BTN = 22;

// --------------------------------------------------------------- tab catalog

const TAB_ANIM = 0, TAB_STATIC = 1, TAB_LAND = 2;
const TABS = [
  { key: TAB_ANIM, label: 'Animation' },
  { key: TAB_STATIC, label: 'Static' },
  { key: TAB_LAND, label: 'Land' },
];

// Per-tab default asset.
const DEFAULT_ID = { [TAB_ANIM]: 400, [TAB_STATIC]: 3712, [TAB_LAND]: 3 };

const FRAME_MS_MIN = 60, FRAME_MS_MAX = 400, FRAME_MS_STEP = 30;

// -------------------------------------------------------------------- engine

function fns() { return Host.getFunctions(); }
function call(name, obj) { return fns()[name](Memory.fromString(JSON.stringify(obj)).offset); }
function spawn() { return fns().cuo_ecs_spawn_entity(); }
function addChild(c, p, i) { fns().cuo_add_entity_to_parent(c, p, i); }
function listen(id, type) { return call('cuo_ui_add_event_listener', { eventType: type, entityId: id }); }

// One UINode -> host. `o` overrides; sensible flex defaults otherwise.
function setNode(o) {
  o = o || {};
  const n = {
    display: o.display || D_FLEX,
    positionType: o.absolute ? 1 : 0,
    overflow: 0,
    flexDirection: o.dir || ROW,
    justifyContent: o.justify || JC_START,
    alignItems: o.align || AI_START,
    width: o.width || AUTO, height: o.height || AUTO,
    minWidth: AUTO, minHeight: AUTO, maxWidth: AUTO, maxHeight: AUTO,
    left: o.left || PX(0), top: o.top || PX(0), right: AUTO, bottom: AUTO,
    padding: o.padding || REL0, border: o.border || REL0,
    gap: o.gap || PX(0), aspectRatio: 0,
  };
  const node = {
    id: o.id,
    node: n,
    interactive: !!o.interactive,
    movable: !!o.movable,
    noDrag: !!o.noDrag,
  };
  if (o.bg) node.backgroundColor = o.bg;
  if (o.bdColor) node.borderColor = o.bdColor;
  if (o.radius !== undefined) node.borderRadius = o.radius;
  if (o.z !== undefined) node.z = o.z;
  if (o.text !== undefined) {
    node.text = {
      value: '' + o.text, fontId: o.font === undefined ? 1 : o.font, fontSize: 0,
      color: o.color || COL.value, editable: !!o.editable, masked: false,
    };
  }
  if (o.uo) node.uo = Object.assign({ hueX: 0, hueY: 0, hueZ: 1, animAction: 0, animDir: 0, animFrame: 0 }, o.uo);
  if (o.button) node.button = { normal: o.button.n, over: o.button.o, pressed: o.button.p };
  call('cuo_ui_node', { nodes: [node], relations: [] });
}

// -------------------------------------------------------------------- state

let ui = null;   // built skeleton (entity ids) or null while closed.

function newState() {
  return {
    tab: TAB_ANIM,
    ids: { [TAB_ANIM]: DEFAULT_ID[TAB_ANIM], [TAB_STATIC]: DEFAULT_ID[TAB_STATIC], [TAB_LAND]: DEFAULT_ID[TAB_LAND] },
    idText: '' + DEFAULT_ID[TAB_ANIM],
    action: 0, dir: 1, frame: 0,
    playing: true, frameMs: 130, lastT: null,
    e: {},        // named entity ids
    tabEnts: [],  // tab panel ids by index
    listeners: [],
  };
}

function curId() { return ui.ids[ui.tab]; }
function setCurId(v) { ui.ids[ui.tab] = v; }

// --------------------------------------------------------------- build tree

function build() {
  if (ui) return;
  ui = newState();
  const e = ui.e;

  // Allocate every entity up front.
  e.window = spawn();
  e.title = spawn(); e.titleText = spawn(); e.close = spawn();
  e.tabStrip = spawn();
  ui.tabEnts = TABS.map(() => spawn());
  const tabLabels = TABS.map(() => spawn());
  e.body = spawn();
  e.vpPanel = spawn(); e.asset = spawn();
  e.inspector = spawn();
  e.idRow = spawn(); e.idLabel = spawn(); e.idField = spawn(); e.idMinus = spawn(); e.idPlus = spawn();
  e.actionRow = spawn(); e.actionLabel = spawn(); e.actionVal = spawn(); e.actionMinus = spawn(); e.actionPlus = spawn();
  e.dirRow = spawn(); e.dirLabel = spawn(); e.dirVal = spawn(); e.dirMinus = spawn(); e.dirPlus = spawn();
  e.playRow = spawn(); e.playBtn = spawn(); e.speedLabel = spawn(); e.speedMinus = spawn(); e.speedPlus = spawn();
  e.status = spawn();

  // Window root.
  setNode({
    id: e.window, absolute: true, left: PX(X), top: PX(Y), width: PX(W), height: PX(H),
    dir: COLUMN, bg: COL.window, bdColor: COL.windowBorder, radius: 6, z: 1000,
    movable: true, padding: padAll(0),
  });

  // Title bar (JustifyContent has no space-between — close is absolutely
  // pinned top-right of the window instead).
  setNode({
    id: e.title, width: PCT(100), height: PX(TITLE_H), dir: ROW, align: AI_CENTER,
    bg: COL.titleBar, padding: pad(10, 0, 6, 0),
  });
  addChild(e.title, e.window, 0);
  setNode({ id: e.titleText, text: 'UO Asset IDE', color: COL.titleText });
  addChild(e.titleText, e.title, 0);
  // Text "X" close button — a styled interactive node (same pattern as tabs),
  // absolute top-right. A gump-art button is unreliable when its graphic is
  // absent from the dataset (0x072E blank in NEW LEGACY).
  setNode({
    id: e.close, absolute: true, left: PX(W - 24), top: PX(4), width: PX(18), height: PX(18),
    justify: JC_CENTER, align: AI_CENTER, bg: COL.closeBtn, radius: 3,
    interactive: true, noDrag: true, text: 'X', color: COL.value,
  });
  ui.listeners.push(listen(e.close, EV_RELEASED));
  addChild(e.close, e.window, 99);

  // Tab strip.
  setNode({ id: e.tabStrip, width: PCT(100), height: PX(TAB_H), dir: ROW, gap: PX(2), padding: pad(4, 2, 4, 0) });
  addChild(e.tabStrip, e.window, 1);
  TABS.forEach((t, i) => {
    setNode({
      id: ui.tabEnts[i], height: PX(TAB_H - 2), dir: ROW, justify: JC_CENTER, align: AI_CENTER,
      padding: pad(12, 0, 12, 0), bg: COL.tabIdle, radius: 4, interactive: true, noDrag: true,
    });
    ui.listeners.push(listen(ui.tabEnts[i], EV_RELEASED));
    addChild(ui.tabEnts[i], e.tabStrip, i);
    setNode({ id: tabLabels[i], text: t.label, color: COL.tabText });
    addChild(tabLabels[i], ui.tabEnts[i], 0);
  });

  // Body: viewport + inspector.
  setNode({ id: e.body, width: PCT(100), dir: ROW, gap: PX(8), padding: pad(8, 6, 8, 6) });
  addChild(e.body, e.window, 2);

  setNode({
    id: e.vpPanel, width: PX(VP), height: PX(VP), justify: JC_CENTER, align: AI_CENTER,
    bg: COL.viewport, bdColor: COL.panelBorder, radius: 4,
  });
  addChild(e.vpPanel, e.body, 0);
  setNode({ id: e.asset, width: PX(VP - 8), height: PX(VP - 8), uo: { kind: K_ANIM, assetId: 400 } });
  addChild(e.asset, e.vpPanel, 0);

  // Inspector column.
  setNode({ id: e.inspector, dir: COLUMN, gap: PX(6), padding: pad(2, 2, 2, 2) });
  addChild(e.inspector, e.body, 1);

  buildStepperRow(e.idRow, e.idLabel, 'ID', e.idMinus, e.idPlus, 0, { field: e.idField });
  buildStepperRow(e.actionRow, e.actionLabel, 'Action', e.actionMinus, e.actionPlus, 1, { valEnt: e.actionVal });
  buildStepperRow(e.dirRow, e.dirLabel, 'Dir', e.dirMinus, e.dirPlus, 2, { valEnt: e.dirVal });

  // Play / speed row.
  setNode({ id: e.playRow, width: PCT(100), height: PX(ROW_H), dir: ROW, align: AI_CENTER, gap: PX(6) });
  addChild(e.playRow, e.inspector, 3);
  setNode({
    id: e.playBtn, height: PX(BTN), padding: pad(10, 0, 10, 0), justify: JC_CENTER, align: AI_CENTER,
    bg: COL.tabActive, radius: 4, interactive: true, noDrag: true, text: 'Pause', color: COL.value,
  });
  ui.listeners.push(listen(e.playBtn, EV_RELEASED));
  addChild(e.playBtn, e.playRow, 0);
  setNode({ id: e.speedLabel, text: 'Speed', color: COL.label });
  addChild(e.speedLabel, e.playRow, 1);
  setNode({ id: e.speedMinus, width: PX(BTN), height: PX(BTN), uo: { kind: K_GUMP, assetId: BTN_LEFT.n }, button: BTN_LEFT, interactive: true });
  ui.listeners.push(listen(e.speedMinus, EV_RELEASED));
  addChild(e.speedMinus, e.playRow, 2);
  setNode({ id: e.speedPlus, width: PX(BTN), height: PX(BTN), uo: { kind: K_GUMP, assetId: BTN_RIGHT.n }, button: BTN_RIGHT, interactive: true });
  ui.listeners.push(listen(e.speedPlus, EV_RELEASED));
  addChild(e.speedPlus, e.playRow, 3);

  // Status bar.
  setNode({ id: e.status, width: PCT(100), height: PX(20), padding: pad(10, 0, 6, 0), align: AI_CENTER, bg: COL.titleBar, text: '', color: COL.accent });
  addChild(e.status, e.window, 3);

  render();
  call('cuo_send_events', { messages: [{ $type: 'CustomAction', topic: 'ide.opened', json: '{}' }] });
  console.log('UO Asset IDE opened');
}

// A "label  value  [-] [+]" row. `opt.field` => editable text instead of a
// readonly value label; `opt.valEnt` => a readonly value label.
function buildStepperRow(rowEnt, labelEnt, label, minusEnt, plusEnt, slot, opt) {
  const e = ui.e;
  setNode({ id: rowEnt, width: PCT(100), height: PX(ROW_H), dir: ROW, align: AI_CENTER, gap: PX(6) });
  addChild(rowEnt, e.inspector, slot);

  setNode({ id: labelEnt, width: PX(54), text: label, color: COL.label });
  addChild(labelEnt, rowEnt, 0);

  if (opt.field) {
    setNode({
      id: opt.field, width: PX(58), height: PX(BTN), padding: pad(6, 0, 6, 0), align: AI_CENTER,
      bg: COL.field, bdColor: COL.fieldBorder, radius: 3, interactive: true, text: '', editable: true, color: COL.value,
    });
    ui.listeners.push(listen(opt.field, EV_TEXT_CHANGED));
    addChild(opt.field, rowEnt, 1);
  } else {
    setNode({ id: opt.valEnt, width: PX(58), text: '', color: COL.value });
    addChild(opt.valEnt, rowEnt, 1);
  }

  setNode({ id: minusEnt, width: PX(BTN), height: PX(BTN), uo: { kind: K_GUMP, assetId: BTN_LEFT.n }, button: BTN_LEFT, interactive: true });
  ui.listeners.push(listen(minusEnt, EV_RELEASED));
  addChild(minusEnt, rowEnt, 2);

  setNode({ id: plusEnt, width: PX(BTN), height: PX(BTN), uo: { kind: K_GUMP, assetId: BTN_RIGHT.n }, button: BTN_RIGHT, interactive: true });
  ui.listeners.push(listen(plusEnt, EV_RELEASED));
  addChild(plusEnt, rowEnt, 3);
}

function destroy() {
  if (!ui) return;
  const f = fns();
  ui.listeners.forEach((id) => { try { call('cuo_ui_remove_event_listener', { eventId: id }); } catch (_) {} });
  // Despawn every node we spawned (host removes children with the root too).
  Object.values(ui.e).forEach((id) => { try { f.cuo_ecs_delete_entity(id); } catch (_) {} });
  ui.tabEnts.forEach((id) => { try { f.cuo_ecs_delete_entity(id); } catch (_) {} });
  ui = null;
  console.log('UO Asset IDE closed');
}

// ------------------------------------------------------------------- render

function hex(v) { return (v >>> 0).toString(16).toUpperCase(); }

function paintAsset() {
  const e = ui.e, id = curId();
  let uo;
  if (ui.tab === TAB_ANIM) uo = { kind: K_ANIM, assetId: id, animAction: ui.action, animDir: ui.dir, animFrame: ui.frame };
  else if (ui.tab === TAB_STATIC) uo = { kind: K_ART, assetId: id };
  else uo = { kind: K_LAND, assetId: id };
  setNode({ id: e.asset, width: PX(VP - 8), height: PX(VP - 8), uo });
}

function paintStatus() {
  const e = ui.e, id = curId();
  let s;
  if (ui.tab === TAB_ANIM) s = `body 0x${hex(id)} (${id})  act ${ui.action} dir ${ui.dir} frame ${ui.frame}`;
  else if (ui.tab === TAB_STATIC) s = `art 0x${hex(id)} (${id})`;
  else s = `land 0x${hex(id)} (${id})`;
  setNode({ id: e.status, width: PCT(100), height: PX(20), padding: pad(10, 0, 6, 0), align: AI_CENTER, bg: COL.titleBar, text: s, color: COL.accent });
}

// Toggle a stepper row's visibility (re-emit its Node with display flipped).
function showRow(rowEnt, display) {
  setNode({ id: rowEnt, display, width: PCT(100), height: PX(ROW_H), dir: ROW, align: AI_CENTER, gap: PX(6) });
}

// Repaint everything that depends on state.
function render() {
  const e = ui.e;

  // Tabs: highlight the active one.
  TABS.forEach((t, i) => {
    setNode({
      id: ui.tabEnts[i], height: PX(TAB_H - 2), dir: ROW, justify: JC_CENTER, align: AI_CENTER,
      padding: pad(12, 0, 12, 0), bg: ui.tab === t.key ? COL.tabActive : COL.tabIdle, radius: 4,
      interactive: true, noDrag: true,
    });
  });

  // ID field mirrors the typed text (controlled; equal value is a host-side no-op,
  // so the live edit isn't clobbered).
  setNode({
    id: e.idField, width: PX(58), height: PX(BTN), padding: pad(6, 0, 6, 0), align: AI_CENTER,
    bg: COL.field, bdColor: COL.fieldBorder, radius: 3, interactive: true, text: ui.idText, editable: true, color: COL.value,
  });

  // Animation-only rows: shown on the Animation tab, hidden otherwise.
  const animVis = ui.tab === TAB_ANIM ? D_FLEX : D_NONE;
  showRow(e.actionRow, animVis);
  showRow(e.dirRow, animVis);
  showRow(e.playRow, animVis);

  if (ui.tab === TAB_ANIM) {
    setNode({ id: e.actionVal, width: PX(58), text: '' + ui.action, color: COL.value });
    setNode({ id: e.dirVal, width: PX(58), text: '' + ui.dir, color: COL.value });
    setNode({
      id: e.playBtn, height: PX(BTN), padding: pad(10, 0, 10, 0), justify: JC_CENTER, align: AI_CENTER,
      bg: COL.tabActive, radius: 4, interactive: true, noDrag: true, text: ui.playing ? 'Pause' : 'Play', color: COL.value,
    });
  }

  paintAsset();
  paintStatus();
}

// ------------------------------------------------------------------ exports

export function on_init() {
  console.log('UO Asset IDE init (opens on LoginScreen)');
  return 1;
}

export function on_update() {
  if (!ui) return 1;
  if (ui.tab !== TAB_ANIM || !ui.playing) return 1;
  const time = JSON.parse(Host.inputString());
  if (ui.lastT === null) ui.lastT = time.total;
  if (time.total - ui.lastT >= ui.frameMs) {
    ui.lastT = time.total;
    ui.frame = (ui.frame + 1) | 0;
    paintAsset();
    paintStatus();
  }
  return 1;
}

export function on_event() {
  const list = JSON.parse(Host.inputString());
  for (const ev of list.messages || []) {
    if (ev.$type === 'GameStateChanged' && ev.state === 1 /* LoginScreen */) build();
    else if (ev.$type === 'CustomEvent' && ev.topic === 'ide.toggle') { if (ui) destroy(); else build(); }
    // Host closed the window out from under us (right-click) — entities are
    // already despawned, just drop local state (destroy()'s deletes no-op).
    else if (ev.$type === 'CustomEvent' && ev.topic === 'ui.window.closed') {
      if (ui && JSON.parse(ev.json).id === ui.e.window) destroy();
    }
  }
  return 1;
}

export function on_ui_event() {
  if (!ui) return 1;
  const ev = JSON.parse(Host.inputString());
  const e = ui.e;

  if (ev.eventType === EV_TEXT_CHANGED && ev.entityId === e.idField) {
    ui.idText = ev.value || '';
    const n = parseInt(ui.idText, 10);
    if (Number.isFinite(n) && n >= 0) { setCurId(n); ui.frame = 0; paintAsset(); paintStatus(); }
    return 1;
  }

  if (ev.eventType !== EV_RELEASED) return 1;
  const id = ev.entityId;

  // Tab switch.
  const tabIdx = ui.tabEnts.indexOf(id);
  if (tabIdx >= 0) {
    ui.tab = TABS[tabIdx].key;
    ui.idText = '' + curId();
    ui.frame = 0;
    render();
    return 1;
  }

  switch (id) {
    case e.close: destroy(); return 1;
    case e.idMinus: setCurId(Math.max(0, curId() - 1)); ui.idText = '' + curId(); ui.frame = 0; break;
    case e.idPlus:  setCurId(curId() + 1); ui.idText = '' + curId(); ui.frame = 0; break;
    case e.actionMinus: ui.action = Math.max(0, ui.action - 1); ui.frame = 0; break;
    case e.actionPlus:  ui.action = ui.action + 1; ui.frame = 0; break;
    case e.dirMinus: ui.dir = (ui.dir + 4) % 5; ui.frame = 0; break;
    case e.dirPlus:  ui.dir = (ui.dir + 1) % 5; ui.frame = 0; break;
    case e.playBtn: ui.playing = !ui.playing; break;
    case e.speedMinus: ui.frameMs = Math.min(FRAME_MS_MAX, ui.frameMs + FRAME_MS_STEP); break; // slower
    case e.speedPlus:  ui.frameMs = Math.max(FRAME_MS_MIN, ui.frameMs - FRAME_MS_STEP); break; // faster
    default: return 1;
  }
  render();
  return 1;
}
