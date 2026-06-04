import {
  AssetType,
  ClayUOCommandType,
  ClayWidgetType,
  CompressionType,
  EventType,
  FloatingAttachToElement,
  FloatingClipToElement,
  Keys,
  LayoutAlignment,
  LayoutDirection,
  MouseButtonType,
  SizingType,
  TermOp,
  TextAlignment,
  TextWrapMode,
  UIInteractionState,
} from './enums';

export interface Vector2 {
  x: number;
  y: number;
}

export interface Vector3 {
  // Game hue, e.g. 0x44
  x: number;

  // Color mode, e.g. 1
  y: number;

  // Alpha 0-255 e.g. 1
  z: number;
}

export interface ClayColor {
  // 0.0 - 1.0
  r: number;
  // 0.0 - 1.0
  g: number;
  // 0.0 - 1.0
  b: number;
  // 0.0 - 1.0
  a: number;
}

export interface ClaySizingAxis {
  minMax?: { min: number; max: number };
  percent?: number;
}

export type ClaySizing =
  | {
      size: ClaySizingAxis;
      type: SizingType.Fixed | SizingType.Percent | SizingType.Fit;
    }
  | {
      type: SizingType.Grow;
      size: object;
    };

export interface ClayChildAlignment {
  x: LayoutAlignment;
  y: LayoutAlignment;
}

export interface ClayPadding {
  left?: number;
  right?: number;
  top?: number;
  bottom?: number;
}

export interface ClayLayoutConfig {
  sizing?: {
    width: ClaySizing;
    height: ClaySizing;
  };
  padding?: ClayPadding;
  childGap?: number;
  childAlignment?: ClayChildAlignment;
  layoutDirection?: LayoutDirection;
}

export interface ClayFloatingElementConfig {
  offset: Vector2;
  expand?: { width: number; height: number };
  parentId?: number;
  zIndex?: number;
  attachPoints?: { element: number; parent: number };
  pointerCaptureMode?: number;
  attachTo: FloatingAttachToElement;
  clipTo: FloatingClipToElement;
}

export interface ClayElementDecl {
  id?: { id: number; offset: number; baseId: number; stringId: string };
  layout?: ClayLayoutConfig;
  backgroundColor?: ClayColor;
  cornerRadius?: {
    topLeft: number;
    topRight: number;
    bottomLeft: number;
    bottomRight: number;
  };
  image?: { base64Data: string };
  floating?: ClayFloatingElementConfig;
  clip?: { horizontal: boolean; vertical: boolean; childOffset: Vector2 };
  border?: {
    color: ClayColor;
    width: {
      left: number;
      right: number;
      top: number;
      bottom: number;
      betweenChildren: number;
    };
  };
}

export interface ClayUOCommandData {
  type: ClayUOCommandType;
  id: number;
  hue: Partial<Vector3>;
}

export interface ClayText {
  textColor: ClayColor;
  fontId?: number;
  fontSize: number;
  letterSpacing?: number;
  lineHeight?: number;
  wrapMode?: TextWrapMode;
  textAlignment?: TextAlignment;
}

export interface ClaySize {
  width: number;
  height: number;
}

export interface UIText {
  value: string | string[];
  replacedChar?: string;
  textConfig: ClayText;
}

export interface UOButtonWidgetProxy {
  normal: number;
  pressed: number;
  over: number;
}

// ---- Wire schema for cuo_ui_node (mirrors host ClayProxy.cs) ----
// Option B: the reconciler speaks TinyEcs.Bevy.UI vocabulary. createElement
// maps the component (Clay-ish) props into these shapes.

export interface ValProxy {
  type: number; // ValType
  value: number;
}

export interface RectProxy {
  left: ValProxy;
  right: ValProxy;
  top: ValProxy;
  bottom: ValProxy;
}

// RGBA 0-255 (Clay.Color convention on the host).
export interface ColorProxy {
  r: number;
  g: number;
  b: number;
  a: number;
}

export interface NodeProxy {
  display: number; // Display
  positionType: number; // PositionType
  overflow: number; // Overflow
  flexDirection: number; // FlexDirection
  justifyContent: number; // JustifyContent
  alignItems: number; // AlignItems
  width: ValProxy;
  height: ValProxy;
  minWidth: ValProxy;
  minHeight: ValProxy;
  maxWidth: ValProxy;
  maxHeight: ValProxy;
  left: ValProxy;
  top: ValProxy;
  right: ValProxy;
  bottom: ValProxy;
  padding: RectProxy;
  border: RectProxy;
  gap: ValProxy;
  aspectRatio: number;
}

export interface TextProxy {
  value: string;
  fontId: number;
  fontSize: number;
  color: ColorProxy;
}

export interface UORenderProxy {
  kind: number; // UOCustomKind
  assetId: number;
  hueX: number;
  hueY: number;
  hueZ: number;
}

export interface UOButtonStateProxy {
  normal: number;
  over: number;
  pressed: number;
}

export interface UINode {
  id: number;
  node: NodeProxy;
  backgroundColor?: ColorProxy;
  borderColor?: ColorProxy;
  borderRadius?: number;
  text?: TextProxy;
  uo?: UORenderProxy;
  interactive: boolean;
  movable: boolean;
  z?: number; // GlobalZIndex
  button?: UOButtonStateProxy; // per-state gump graphics (host swaps on hover/press)
}

export interface UINodeRelation {
  child: number;
  parent: number;
  index: number;
}

export interface UINodes {
  nodes: UINode[];
  relations: UINodeRelation[];
}

export interface UIEvent {
  eventType: EventType;
  entityId: number;

  eventId?: number;
  x?: number;
  y?: number;
  wheel?: number;
  mouseButton?: MouseButtonType;
  key?: Keys;
}

export interface Graphic {
  value: number;
}

export interface SpriteDescription {
  assetType: AssetType;
  idx: number;
  width?: number;
  height?: number;
  base64Data?: string;
  compression?: CompressionType;
}

export interface TimeProxy {
  total: number;
  frame: number;
}

export interface ComponentInfo {
  id: number;
  size: number;
  name: string;
}

export interface Archetype {
  components: ComponentInfo[];
  entities: number[];
}

export interface QueryRequest {
  terms: Array<{ ids: number; op: TermOp }>;
}

export interface QueryResponse {
  results: Archetype[];
}

export interface UIMouseEvent {
  id: number;
  button: number;
  x: number;
  y: number;
  state: UIInteractionState;
}

export interface HostMessage {
  $type: string;
  x?: number;
  y?: number;
  delta?: number;
  button?: number;
  key?: Keys;
}

export interface HostMessages {
  messages: HostMessage[];
}


export interface PluginMessage {
  $type: string;
  username?: string;
  password?: string;
}

export interface PluginMessages {
  messages: PluginMessage[];
}
