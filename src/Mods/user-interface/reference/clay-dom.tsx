// DOM shim for the mod's `~/react` primitives.
//
// The design-system components (Tooltip, IconButton, …) are written against
// these primitives + the Clay prop vocabulary. Here we re-implement View / Gump /
// Text / Art / Button as react-dom elements that translate the SAME Clay layout
// props into CSS flexbox. That lets the UNCHANGED components run in a real
// browser through react-dom — a ground-truth for the intended behaviour, to diff
// against the cuo-ecs mod (custom reconciler -> Bevy.UI -> Clay.NET).
//
// Fidelity goal: layout + event/hover semantics, NOT pixel-exact UO gump art.
// A Gump/Art renders as a representative box (UO sprites aren't available here).

import * as React from 'react';
import {
  FloatingAttachToElement,
  LayoutAlignment,
  LayoutDirection,
  SizingType,
  type ClayChildAlignment,
  type ClayColor,
  type ClayFloatingElementConfig,
  type ClayLayoutConfig,
  type ClayPadding,
  type ClaySize,
  type ClaySizing,
  type ClayText,
} from '~/host';

export type Hue = { hue?: number; hueMode?: number; alpha?: number };
export const defaultHue = { x: 0, y: 0, z: 1.0 };
export const hueToVector3 = (hue: Hue = {}) => ({ x: hue.hue ?? 0, y: hue.hueMode ?? 0, z: hue.alpha ?? 1.0 });

const cssColor = (c?: ClayColor): string | undefined =>
  c ? `rgba(${Math.round(c.r * 255)}, ${Math.round(c.g * 255)}, ${Math.round(c.b * 255)}, ${c.a})` : undefined;

const alignToFlex = (a?: LayoutAlignment): string => {
  switch (a) {
    case LayoutAlignment.Center: // 2
      return 'center';
    case LayoutAlignment.Right: // 1 (== Bottom)
      return 'flex-end';
    default: // Left/Top (0) or undefined
      return 'flex-start';
  }
};

const axisSize = (s?: ClaySizing): string | undefined => {
  if (!s) return undefined;
  switch (s.type) {
    case SizingType.Fixed:
      return `${s.size.minMax?.min ?? 0}px`;
    case SizingType.Percent:
      return `${s.size.percent ?? 0}%`;
    case SizingType.Grow:
      return '100%';
    case SizingType.Fit:
    default:
      return 'fit-content';
  }
};

const pad = (p?: ClayPadding): string | undefined =>
  p ? `${p.top ?? 0}px ${p.right ?? 0}px ${p.bottom ?? 0}px ${p.left ?? 0}px` : undefined;

// Clay childAlignment is axis-relative (x,y); CSS flex splits main(justify)/cross(align).
function layoutStyle(layout?: ClayLayoutConfig, floating?: ClayFloatingElementConfig): React.CSSProperties {
  const isCol = layout?.layoutDirection === LayoutDirection.TopToBottom;
  const s: React.CSSProperties = {
    display: 'flex',
    boxSizing: 'border-box',
    flexDirection: isCol ? 'column' : 'row',
    // Clay AttachTo.Parent => attach to the parent box; CSS absolute needs a
    // positioned ancestor, so every box is `relative` unless it itself floats.
    position: floating ? 'absolute' : 'relative',
  };

  const w = axisSize(layout?.sizing?.width);
  const h = axisSize(layout?.sizing?.height);
  if (w) s.width = w;
  if (h) s.height = h;

  if (layout?.childAlignment) {
    const a: ClayChildAlignment = layout.childAlignment;
    s.justifyContent = alignToFlex(isCol ? a.y : a.x);
    s.alignItems = alignToFlex(isCol ? a.x : a.y);
  }
  if (layout?.childGap != null) s.gap = `${layout.childGap}px`;

  if (floating) {
    s.left = floating.offset?.x ?? 0;
    s.top = floating.offset?.y ?? 0;
    if (floating.attachTo === FloatingAttachToElement.Root) {
      s.position = 'fixed';
    }
    s.zIndex = floating.zIndex ?? 1000;
  }
  return s;
}

type Handlers = {
  onMouseEnter?: () => void;
  onMouseLeave?: () => void;
  onClick?: () => void;
  onMouseDown?: () => void;
  onMouseUp?: () => void;
};

const handlers = (p: Handlers) => ({
  onMouseEnter: p.onMouseEnter,
  onMouseLeave: p.onMouseLeave,
  onClick: p.onClick,
  onMouseDown: p.onMouseDown,
  onMouseUp: p.onMouseUp,
});

// ---- primitives ----

export const View: React.FC<any> = (props) => {
  const style: React.CSSProperties = {
    ...layoutStyle(props.layout, props.floating),
    backgroundColor: cssColor(props.backgroundColor),
    padding: pad(props.padding ?? props.layout?.padding),
  };
  if (props.cornerRadius) style.borderRadius = props.cornerRadius.topLeft;
  if (props.border) {
    style.borderStyle = 'solid';
    style.borderColor = cssColor(props.border.color);
    const w = props.border.width;
    style.borderWidth = `${w.top}px ${w.right}px ${w.bottom}px ${w.left}px`;
  }
  return (
    <div data-clay="view" style={style} {...handlers(props)}>
      {props.children}
    </div>
  );
};

// A gump: a representative nine-patch-ish box. ninePatch => rounded bordered
// panel; plain => flat box. hue.alpha drives opacity (tooltips pass alpha 0.9).
export const Gump: React.FC<any> = (props) => {
  const layout: ClayLayoutConfig =
    props.layout ?? {
      layoutDirection: props.direction ?? LayoutDirection.TopToBottom,
      childAlignment: props.childAlignment,
      padding: props.padding,
    };
  const size: ClaySize | undefined = props.size ?? (props.layout ? undefined : { width: 50, height: 25 });
  const style: React.CSSProperties = {
    ...layoutStyle(layout, props.floating),
    padding: pad(props.padding ?? layout.padding),
    background: props.ninePatch ? 'rgba(20,18,14,0.92)' : 'rgba(60,55,45,0.92)',
    border: props.ninePatch ? '2px solid #7a6a48' : '1px solid #555',
    borderRadius: props.ninePatch ? 4 : 0,
    opacity: props.hue?.alpha ?? 1,
    color: '#e8e0c8',
  };
  if (size) {
    style.width = size.width;
    style.height = size.height;
  }
  return (
    <div data-clay="gump" data-gump-id={props.id} style={style} {...handlers(props)}>
      {props.children}
    </div>
  );
};

// Text: a flowing text box. Width follows the parent (grow) unless `size` pins
// it, and white-space:normal => the browser wraps to that width. This is the
// ground truth the Clay.NET text-wrap fix is chasing.
export const Text: React.FC<any> = (props) => {
  const st: ClayText | undefined = props.style;
  const style: React.CSSProperties = {
    ...layoutStyle(undefined, props.floating),
    display: 'block',
    width: props.size ? `${props.size.width}px` : '100%',
    padding: pad(props.padding),
    color: cssColor(st?.textColor) ?? '#e8e0c8',
    fontSize: st?.fontSize ?? 14,
    lineHeight: st?.lineHeight ? `${st.lineHeight}px` : 1.2,
    whiteSpace: 'normal',
    overflowWrap: 'anywhere',
  };
  return (
    <span data-clay="text" style={style} {...handlers(props)}>
      {props.children}
    </span>
  );
};

export const Label = Text;

export const Art: React.FC<any> = (props) => {
  const size: ClaySize = props.size ?? { width: 24, height: 24 };
  const style: React.CSSProperties = {
    ...layoutStyle(undefined, props.floating),
    width: size.width,
    height: size.height,
    background: 'repeating-conic-gradient(#9c8 0 25%, #687 0 50%) 0 0 / 8px 8px',
    border: '1px solid #353',
    opacity: props.hue?.alpha ?? 1,
  };
  return <div data-clay="art" data-art-id={props.id} style={style} {...handlers(props)} />;
};

// Button: a sized box that swaps shade on hover/press (mirrors UOButton states).
export const Button: React.FC<any> = (props) => {
  const [state, setState] = React.useState<'normal' | 'over' | 'pressed'>('normal');
  const size: ClaySize = props.size ?? { width: 50, height: 25 };
  const bg = state === 'pressed' ? '#3a4a6a' : state === 'over' ? '#4a5a7a' : '#39435a';
  const style: React.CSSProperties = {
    ...layoutStyle(
      { layoutDirection: LayoutDirection.TopToBottom, childAlignment: { x: LayoutAlignment.Center, y: LayoutAlignment.Center }, padding: props.padding },
      props.floating,
    ),
    width: size.width,
    height: size.height,
    background: bg,
    border: '1px solid #223',
    cursor: 'pointer',
    opacity: props.hue?.alpha ?? 1,
  };
  return (
    <div
      data-clay="button"
      style={style}
      onMouseEnter={() => { setState('over'); props.onMouseEnter?.(); }}
      onMouseLeave={() => { setState('normal'); props.onMouseLeave?.(); }}
      onMouseDown={() => { setState('pressed'); props.onMouseDown?.(); }}
      onMouseUp={() => { setState('over'); props.onMouseUp?.(); }}
      onClick={() => props.onClick?.()}
    >
      {props.children}
    </div>
  );
};

export const Checkbox: React.FC<any> = (props) => (
  <div data-clay="checkbox" style={{ width: 16, height: 16, border: '1px solid #888' }} {...handlers(props)} />
);
export const TextInput: React.FC<any> = (props) => (
  <input data-clay="textinput" placeholder={props.placeholder} style={{ width: props.size?.width ?? 100 }} />
);
export const HSliderBar: React.FC<any> = (props) => (
  <div data-clay="hsliderbar" style={{ width: props.size?.width ?? 100, height: 8, background: '#555' }} {...handlers(props)} />
);
