// HostWrapper — component-model reimplementation of the Extism HostWrapper.
// The reconciler + createElement + EventManager are reused VERBATIM: they speak
// numeric entity ids + a UINode JSON + addEventListener. This wrapper maps that
// vocabulary onto TinyEcs `commands`:
//   - spawnEntity()        -> commands.spawnEmpty().id() (held Entity handle, kept
//                             in an id->handle map so updates across frames work)
//   - setNode(UINode)      -> entity(handle).insertTyped(typed) + .insert(strings)
//   - addEntityToParent()  -> entity(parent).addChild(child)
//   - deleteEntity()       -> entity(handle).despawn()
//   - addEventListener()   -> reserves an eventId; the click poll (index.tsx)
//                             resolves a clicked entity's ui-name back to its id,
//                             then to the OnMouseReleased eventId, and dispatches.
//
// The current frame's `commands` is set each system call via setCommands().

import { Ui } from 'cuo:modding/ui@0.1.0';
import { EventType } from './clay';
import type { ColorProxy, NodeProxy, UIEvent, UINode, UINodes } from './clay';

// `commands` for the current system call. render() runs inside a system, so it is
// set for the whole React commit.
let cmds: any = null;
export function setCommands(c: any) {
  cmds = c;
}

const idToHandle = new Map<number, any>();
const childToParent = new Map<number, number>(); // childId -> parentId, for hover ancestry
const entityEvents = new Map<number, Map<number, number>>(); // entityId -> (eventType -> eventId)
const eventToEntity = new Map<number, number>(); // eventId -> entityId
let nextEntityId = 1;
let nextEventId = 1;
const APPEND = 0xffffffff;

// NodeProxy uses ValProxy {type,value} (type = ValType); node-rec uses {kind,value}.
const val = (p: any) => ({ kind: p.type, value: p.value });
const rect = (r: any) => ({ left: val(r.left), right: val(r.right), top: val(r.top), bottom: val(r.bottom) });
function nodeRec(n: NodeProxy) {
  return {
    display: n.display, positionType: n.positionType, overflow: n.overflow,
    flexDirection: n.flexDirection, justifyContent: n.justifyContent, alignItems: n.alignItems,
    width: val(n.width), height: val(n.height),
    minWidth: val(n.minWidth), minHeight: val(n.minHeight), maxWidth: val(n.maxWidth), maxHeight: val(n.maxHeight),
    left: val(n.left), top: val(n.top), right: val(n.right), bottom: val(n.bottom),
    padding: rect(n.padding), border: rect(n.border), gap: val(n.gap), aspectRatio: n.aspectRatio,
  };
}

function applyNode(node: UINode) {
  const handle = idToHandle.get(node.id);
  if (!handle) return;

  const typed: any[] = [{ tag: 'node', val: nodeRec(node.node) }];
  if (node.backgroundColor) typed.push({ tag: 'bg-color', val: node.backgroundColor as ColorProxy });
  if (node.text) {
    typed.push({ tag: 'text', val: { value: String(node.text.value) } });
    typed.push({ tag: 'text-font', val: { fontId: node.text.fontId, size: node.text.fontSize } });
    typed.push({ tag: 'text-color', val: node.text.color });
  }
  if (node.interactive) {
    typed.push({ tag: 'interaction', val: 0 });
    typed.push({ tag: 'no-window-drag' });
  }
  if (node.movable) {
    typed.push({ tag: 'movable' });
    typed.push({ tag: 'contains-by-bounds' });
  }
  if (node.z != null) typed.push({ tag: 'global-z', val: { value: node.z } });
  // ui-name = numeric id so the click poll maps a clicked entity back to its id.
  typed.push({ tag: 'ui-name', val: { value: String(node.id) } });

  const strings: [string, string][] = [];
  if (node.uo) {
    strings.push(['cuo:ui/custom', JSON.stringify({
      Kind: node.uo.kind, AssetId: node.uo.assetId,
      HueX: node.uo.hueX, HueY: node.uo.hueY, HueZ: node.uo.hueZ,
    })]);
  }
  if (node.button) {
    strings.push(['cuo:ui/button', JSON.stringify({
      Normal: node.button.normal, Pressed: node.button.pressed, Over: node.button.over,
    })]);
  }

  const ec = cmds.entity(handle);
  ec.insertTyped(typed);
  if (strings.length) ec.insert(strings);
}

export class HostWrapper {
  static spawnEntity(): number {
    const handle = cmds.spawnEmpty().id();
    const id = nextEntityId++;
    idToHandle.set(id, handle);
    return id;
  }

  static deleteEntity(id: number): void {
    const h = idToHandle.get(id);
    if (h) cmds.entity(h).despawn();
    idToHandle.delete(id);
    childToParent.delete(id);
    const evs = entityEvents.get(id);
    if (evs) {
      for (const eid of evs.values()) eventToEntity.delete(eid);
      entityEvents.delete(id);
    }
  }

  static addEntityToParent(childId: number, parentId: number, index = -1): void {
    childToParent.set(childId, parentId);
    const c = idToHandle.get(childId);
    const p = idToHandle.get(parentId);
    if (c && p) cmds.entity(p).addChild(c, index < 0 ? APPEND : index >>> 0);
  }

  // The host flags only the topmost interactive element Hovered. A tooltip's
  // onMouseEnter sits on a wrapper AROUND an interactive child (IconButton), so
  // walk the parent chain to mark every ancestor hovered too — DOM mouseenter
  // semantics. Without this the wrapper never enters and the tooltip never shows
  // (only a non-interactive child, e.g. a Text, leaves the wrapper itself topmost).
  static withAncestors(id: number, out: Set<number>): void {
    let cur: number | undefined = id;
    for (let guard = 0; cur != null && guard < 64; guard++) {
      if (out.has(cur)) break; // already walked this chain
      out.add(cur);
      cur = childToParent.get(cur);
    }
  }

  static setNode(data: UINode | UINodes): void {
    if ('id' in data) {
      applyNode(data);
      return;
    }
    for (const n of data.nodes) applyNode(n);
    for (const r of data.relations) HostWrapper.addEntityToParent(r.child, r.parent, r.index);
  }

  // Reserve an eventId for (entity, type). The listener fn lives in EventManager
  // keyed by this id; the host can't push events, so the click poll dispatches.
  static addEventListener(event: { entityId: number; eventType: EventType }): number {
    const eid = nextEventId++;
    let m = entityEvents.get(event.entityId);
    if (!m) entityEvents.set(event.entityId, m = new Map());
    m.set(event.eventType, eid);
    eventToEntity.set(eid, event.entityId);
    return eid;
  }

  static removeEventListener(event: { eventId: number; entityId: number; eventType: EventType }): number {
    entityEvents.get(event.entityId)?.delete(event.eventType);
    eventToEntity.delete(event.eventId);
    return 0;
  }

  // Native art dimensions of a gump (cuo:modding/ui gump-size, packed w<<16|h).
  // Lets a gump node be sized to its sprite instead of stretched to an arbitrary
  // box (the host scales a UO gump to fill its node). null when assets aren't
  // loaded (headless test host) so callers fall back to an explicit size.
  static gumpSize(id: number): { width: number; height: number } | null {
    try {
      const packed = new Ui().gumpSize(id) >>> 0;
      const width = packed >>> 16;
      const height = packed & 0xffff;
      return width > 0 && height > 0 ? { width, height } : null;
    } catch {
      return null;
    }
  }

  // index.tsx: resolve an entity (numeric id from its ui-name) + event type to the
  // UIEvent to dispatch, if it has a listener for that type. Used by the click
  // poll (OnMouseReleased) and the hover poll (OnMouseEnter/OnMouseLeave).
  static eventFor(entityId: number, eventType: EventType): UIEvent | null {
    const eid = entityEvents.get(entityId)?.get(eventType);
    if (eid == null) return null;
    return { eventType, entityId, eventId: eid };
  }
}
