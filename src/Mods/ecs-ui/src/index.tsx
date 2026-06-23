import './support/init'; // FIRST: install timer polyfills before react-reconciler loads
import { System } from 'tinyecs:modding/app@0.1.0';
import { EventType } from '~/host';
import { StorybookScreen } from '~/components/storybook';
import { HostWrapper, setCommands } from '~/host';
import { ClayReactRenderer } from '~/react';
import { pumpTimers } from './support/init';

let renderer: ClayReactRenderer | null = null;
let hoveredPrev = new Set<number>();
// Engine-clock fallback: the bare modding app has no Time resource, so estimate
// ~16ms/frame for the tooltip delay timers. The full game reads cuo:engine/time.
let clockMs = 0;

export function setup(app: any) {
  const startup = new System('ui-startup');
  startup.addCommands();
  app.addSystems({ tag: 'mod-startup' }, [startup]);

  const onTick = new System('ui-tick');
  onTick.addCommands();
  // clicked: entities the host flagged this frame.
  onTick.addQuery([
    { tag: 'with', val: 'cuo:ui/clicked' },
    { tag: 'ref', val: 'cuo:ui/name' },
  ]);
  // hovered: every interactive element's Interaction state (0=None,1=Hovered,2=Pressed).
  onTick.addQuery([
    { tag: 'ref', val: 'cuo:ui/interaction' },
    { tag: 'ref', val: 'cuo:ui/name' },
  ]);
  app.addSystems({ tag: 'update' }, [onTick]);

  console.log('[ecs-ui] setup: storybook mod');
}

export function uiStartup(commands: any) {
  setCommands(commands);
  try {
    renderer = new ClayReactRenderer();
    renderer.render(<StorybookScreen onBack={() => console.log('[ecs-ui] back')} />);
  } catch (e: any) {
    console.error('[ecs-ui] ui-startup threw:', e, e && e.stack);
  }
}

function idOf(nameJson: string): number {
  try {
    return parseInt(JSON.parse(nameJson)?.Value, 10);
  } catch {
    return NaN;
  }
}

export function uiTick(commands: any, clicked: any, hovered: any) {
  setCommands(commands);
  if (!renderer) return;

  // Clicks the host flagged (cuo:ui/clicked) -> React onClick.
  for (let row = clicked.iter(); row; row = clicked.iter()) {
    const id = idOf(row.component(0).get());
    if (!Number.isNaN(id)) {
      const ev = HostWrapper.eventFor(id, EventType.OnMouseReleased);
      if (ev) renderer.handleUIEvent(ev);
    }
  }

  // Hover: build the currently-hovered set, diff vs last frame -> enter/leave.
  const now = new Set<number>();
  for (let row = hovered.iter(); row; row = hovered.iter()) {
    const state = parseInt(row.component(0).get(), 10); // Interaction value
    const id = idOf(row.component(1).get());
    // Hovered or Pressed = over. Mark the element AND its ancestors hovered so a
    // listener on a wrapper around an interactive child (Tooltip over IconButton)
    // fires — the host only flags the topmost interactive element itself.
    if (!Number.isNaN(id) && state !== 0) HostWrapper.withAncestors(id, now);
  }
  for (const id of now) {
    if (!hoveredPrev.has(id)) {
      const ev = HostWrapper.eventFor(id, EventType.OnMouseEnter);
      if (ev) renderer.handleUIEvent(ev);
    }
  }
  for (const id of hoveredPrev) {
    if (!now.has(id)) {
      const ev = HostWrapper.eventFor(id, EventType.OnMouseLeave);
      if (ev) renderer.handleUIEvent(ev);
    }
  }
  hoveredPrev = now;

  // Advance React's scheduler + the tooltip delay timers on the engine clock.
  let ms = clockMs += 16;
  try {
    const t = JSON.parse(commands.resourceGet('cuo:engine/time'));
    if (t && typeof t.Total === 'number') ms = t.Total;
  } catch {
    // no Time resource (bare app) -> keep the ~16ms/frame estimate
  }
  pumpTimers(ms);
}
