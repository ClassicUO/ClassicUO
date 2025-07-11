import {
  ClayLayoutAlignment,
  ClayLayoutDirection,
  ClaySizingType,
  ClayTextAlignment,
  ClayTextWrapMode,
  ClayUOCommandType,
  ClayWidgetType,
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
  AssetType,
  CompressionType,
  TermOp,
  UIInteractionState,
  EventType,
  Keys,
  MouseButtonType,
} from "./enums";

export interface Vector2 {
  x: number;
  y: number;
}

export interface Vector3 {
  x: number; // Game hue, e.g. 0x44
  y: number; // Color mode, e.g. 1
  z: number; // Alpha 0-255 e.g. 1
}

export interface ClayColor {
  r: number; // 0.0 - 1.0
  g: number; // 0.0 - 1.0
  b: number; // 0.0 - 1.0
  a: number; // 0.0 - 1.0
}

export interface ClaySizingAxis {
  minMax?: { min: number; max: number };
  percent?: number;
}

export interface ClaySizing {
  size: ClaySizingAxis;
  type: ClaySizingType;
}

export interface ClayChildAlignment {
  x: ClayLayoutAlignment;
  y: ClayLayoutAlignment;
}

export interface ClayLayoutConfig {
  sizing?: {
    width: ClaySizing;
    height: ClaySizing;
  };
  padding?: { left: number; right: number; top: number; bottom: number };
  childGap?: number;
  childAlignment?: ClayChildAlignment;
  layoutDirection?: ClayLayoutDirection;
}

export interface ClayFloatingElementConfig {
  offset: Vector2;
  expand?: { width: number; height: number };
  parentId?: number;
  zIndex?: number;
  attachPoints?: { element: number; parent: number };
  pointerCaptureMode?: number;
  attachTo: ClayFloatingAttachToElement;
  clipTo: ClayFloatingClipToElement;
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
  fontId: number;
  fontSize: number;
  letterSpacing?: number;
  lineHeight?: number;
  wrapMode?: ClayTextWrapMode;
  textAlignment?: ClayTextAlignment;
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

export interface UINode {
  id: number;
  config: ClayElementDecl;
  uoConfig?: ClayUOCommandData;
  textConfig?: UIText;
  uoButton?: UOButtonWidgetProxy;
  widgetType?: ClayWidgetType;
  movable?: boolean;
  acceptInputs?: boolean;
}

export interface UINodes {
  nodes: UINode[];
  relations: Record<number, number>;
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
