import * as P from 'ts-pattern';
import {
  ClayChildAlignment,
  ClayColor,
  ClayFloatingElementConfig,
  ClayLayoutConfig,
  ClayPadding,
  ClaySize,
  ClaySizing,
  ClayText,
  ClayUOCommandType,
  ColorProxy,
  LayoutAlignment,
  LayoutDirection,
  NodeProxy,
  RectProxy,
  SizingType,
  TextProxy,
  UINode,
  UOCustomKind,
  ValProxy,
} from '~/host';
import { TextStyle } from '~/ui';
import { ClayElementPropTypes, defaultHue, hueToVector3 } from '../elements';
import { hasEventProp } from '../events';

// ---- Clay-prop -> Bevy.UI Node mapping helpers ----

const auto = (): ValProxy => ({ type: 0, value: 0 });
const px = (v: number): ValProxy => ({ type: 1, value: v });
const pct = (v: number): ValProxy => ({ type: 2, value: v });
const grow = (): ValProxy => pct(100);

const zeroRect = (): RectProxy => ({ left: px(0), right: px(0), top: px(0), bottom: px(0) });

function padRect(p?: ClayPadding): RectProxy {
  if (!p) return zeroRect();
  return { left: px(p.left ?? 0), right: px(p.right ?? 0), top: px(p.top ?? 0), bottom: px(p.bottom ?? 0) };
}

// ClayColor is 0-1 float; host Clay.Color is 0-255.
function col(c?: ClayColor): ColorProxy | undefined {
  if (!c) return undefined;
  return { r: c.r * 255, g: c.g * 255, b: c.b * 255, a: c.a * 255 };
}

function sizingToVal(s?: ClaySizing): ValProxy {
  if (!s) return auto();
  switch (s.type) {
    case SizingType.Fixed:
      return px(s.size.minMax?.min ?? 0);
    case SizingType.Percent:
      return pct(s.size.percent ?? 0);
    case SizingType.Grow:
      return pct(100);
    case SizingType.Fit:
    default:
      return auto();
  }
}

// LayoutDirection.TopToBottom(1) -> Column(1); LeftToRight(0) -> Row(0).
const dirToFlex = (d?: LayoutDirection): number => (d === LayoutDirection.TopToBottom ? 1 : 0);

// LayoutAlignment Left/Top(0)->Start(0), Right/Bottom(1)->End(2), Center(2)->Center(1).
const alignMap = (a: number): number => (a === 2 ? 1 : a === 1 ? 2 : 0);

function defaultNode(): NodeProxy {
  return {
    display: 0,
    positionType: 0,
    overflow: 0,
    flexDirection: 0,
    justifyContent: 0,
    alignItems: 0,
    width: auto(),
    height: auto(),
    minWidth: auto(),
    minHeight: auto(),
    maxWidth: auto(),
    maxHeight: auto(),
    left: auto(),
    top: auto(),
    right: auto(),
    bottom: auto(),
    padding: zeroRect(),
    border: zeroRect(),
    gap: px(0),
    aspectRatio: 0,
  };
}

function buildNode(
  layout?: ClayLayoutConfig,
  floating?: ClayFloatingElementConfig,
  sizeOverride?: ClaySize,
): NodeProxy {
  const n = defaultNode();

  if (layout?.sizing) {
    n.width = sizingToVal(layout.sizing.width);
    n.height = sizingToVal(layout.sizing.height);
  }
  if (sizeOverride) {
    n.width = px(sizeOverride.width);
    n.height = px(sizeOverride.height);
  }

  n.padding = padRect(layout?.padding);
  if (layout?.childGap != null) n.gap = px(layout.childGap);
  n.flexDirection = dirToFlex(layout?.layoutDirection);

  if (layout?.childAlignment) {
    const isCol = n.flexDirection === 1;
    const a: ClayChildAlignment = layout.childAlignment;
    // Clay alignment is axis-relative; Bevy splits main (Justify) / cross (Align).
    n.justifyContent = alignMap(isCol ? a.y : a.x);
    n.alignItems = alignMap(isCol ? a.x : a.y);
  }

  if (floating) {
    n.positionType = 1; // Absolute
    n.left = px(floating.offset?.x ?? 0);
    n.top = px(floating.offset?.y ?? 0);
  }

  return n;
}

// ClayUOCommandType -> UOCustomKind (different numbering).
function uoKind(t: ClayUOCommandType): UOCustomKind {
  switch (t) {
    case ClayUOCommandType.Gump:
      return UOCustomKind.Gump;
    case ClayUOCommandType.GumpNinePatch:
      return UOCustomKind.GumpNinePatch;
    case ClayUOCommandType.Art:
      return UOCustomKind.Art;
    case ClayUOCommandType.Land:
      return UOCustomKind.Land;
    case ClayUOCommandType.Animation:
      return UOCustomKind.Animation;
    case ClayUOCommandType.Text:
      return UOCustomKind.WrappedText;
    default:
      return UOCustomKind.None;
  }
}

function text(value: string, style?: ClayText): TextProxy {
  const s = style ?? TextStyle.default;
  return {
    value,
    fontId: s.fontId ?? 0,
    fontSize: s.fontSize,
    color: col(s.textColor) ?? { r: 255, g: 255, b: 255, a: 255 },
  };
}

const zIndexOf = (floating?: ClayFloatingElementConfig): number | undefined => floating?.zIndex;

function spreadHue(hue?: Parameters<typeof hueToVector3>[0]): { hueX: number; hueY: number; hueZ: number } {
  const v = hue ? hueToVector3(hue) : defaultHue;
  return { hueX: v.x, hueY: v.y, hueZ: v.z };
}

export function createElement(type: string, props: unknown, id: number): UINode {
  return P.match<ClayElementPropTypes, UINode>({
    type: type.toLowerCase(),
    props,
  } as ClayElementPropTypes)
    .with({ type: 'view' }, (data) => ({
      id,
      node: buildNode(data.props.layout, data.props.floating),
      backgroundColor: col(data.props.backgroundColor),
      borderColor: col(data.props.border?.color),
      borderRadius: data.props.cornerRadius?.topLeft,
      interactive: hasEventProp(data.props),
      movable: data.props.movable ?? false,
      z: zIndexOf(data.props.floating),
    }))
    .with({ type: 'gump' }, (data) => {
      // Prefer an explicit `layout` (lets a gump be content-sized, e.g. a
      // ninePatch tooltip that fits its text). Fall back to the fixed `size`,
      // and only to the 50x25 default when neither is given. A gump defaults to
      // column + centered children (matches the original behaviour; a row
      // default jumbles stacked card content and pushes children off-box).
      const layout: ClayLayoutConfig = data.props.layout ?? {
        layoutDirection: data.props.direction ?? LayoutDirection.TopToBottom,
        childAlignment: data.props.childAlignment ?? { x: LayoutAlignment.Center, y: LayoutAlignment.Center },
        padding: data.props.padding,
      };
      const sizeOverride = data.props.size ?? (data.props.layout ? undefined : { width: 50, height: 25 });
      return {
        id,
        node: buildNode(layout, data.props.floating, sizeOverride),
        uo: {
          kind: uoKind(data.props.ninePatch ? ClayUOCommandType.GumpNinePatch : ClayUOCommandType.Gump),
          assetId: data.props.id,
          ...spreadHue(data.props.hue),
        },
        interactive: hasEventProp(data.props),
        movable: data.props.movable ?? false,
        z: zIndexOf(data.props.floating),
      };
    })
    .with({ type: 'button' }, (data) => ({
      id,
      node: buildNode({ padding: data.props.padding }, data.props.floating, data.props.size ?? { width: 50, height: 25 }),
      uo: {
        kind: UOCustomKind.Gump,
        assetId: data.props.gumpIds.normal,
        ...spreadHue(data.props.hue),
      },
      button: {
        normal: data.props.gumpIds.normal,
        over: data.props.gumpIds.over,
        pressed: data.props.gumpIds.pressed,
      },
      interactive: true,
      movable: data.props.movable ?? false,
      z: zIndexOf(data.props.floating),
    }))
    .with({ type: 'textinput' }, (data) => ({
      id,
      node: buildNode({ padding: data.props.padding }, data.props.floating, data.props.size ?? { width: 50, height: 25 }),
      text: text(data.props.placeholder ?? '', data.props.textStyle),
      interactive: true,
      movable: false,
      z: zIndexOf(data.props.floating),
    }))
    .with({ type: 'text' }, (data) => {
      const node = buildNode({ padding: data.props.padding }, data.props.floating, data.props.size);
      // Fill the parent's width so Clay wraps the text to it (a Fit/Auto text
      // element shrink-wraps to a single line and overflows). The parent
      // constrains the actual wrap width.
      if (!data.props.size) node.width = grow();
      return {
        id,
        node,
        text: text('', data.props.style),
        interactive: hasEventProp(data.props),
        movable: data.props.movable ?? false,
        z: zIndexOf(data.props.floating),
      };
    })
    .with({ type: 'label' }, (data) => {
      const node = buildNode({ padding: data.props.padding }, data.props.floating, data.props.size);
      if (!data.props.size) node.width = grow();
      return {
        id,
        node,
        text: text('', data.props.style),
        interactive: false,
        movable: false,
        z: zIndexOf(data.props.floating),
      };
    })
    .with({ type: 'checkbox' }, (data) => ({
      id,
      node: buildNode({ padding: data.props.padding }, data.props.floating, data.props.size),
      text: text('[ ]'),
      interactive: hasEventProp(data.props),
      movable: false,
      z: zIndexOf(data.props.floating),
    }))
    .with({ type: 'hsliderbar' }, (data) => ({
      id,
      node: buildNode({ padding: data.props.padding }, data.props.floating, data.props.size),
      text: text('====='),
      interactive: hasEventProp(data.props),
      movable: false,
      z: zIndexOf(data.props.floating),
    }))
    .with({ type: 'art' }, (data) => ({
      id,
      node: buildNode({ padding: data.props.padding }, data.props.floating, data.props.size),
      uo: {
        kind: UOCustomKind.Art,
        assetId: data.props.id,
        ...spreadHue(data.props.hue),
      },
      interactive: hasEventProp(data.props),
      movable: false,
      z: zIndexOf(data.props.floating),
    }))
    .exhaustive();
}

// Raw text node (React text children become these). Carries a native Bevy.UI
// Text component via the `text` proxy.
export function createTextFragment(id: number, value: string): UINode {
  const node = defaultNode();
  node.width = grow(); // fill the parent text element so the line wraps to it
  return { id, node, text: text(value), interactive: false, movable: false };
}
