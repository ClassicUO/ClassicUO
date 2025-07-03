// Enums
export enum Keys {
  None = 0,
  Back = 8,
  Tab = 9,
  Enter = 13,
  CapsLock = 20,
  Escape = 27,
  Space = 32,
  PageUp = 33,
  PageDown = 34,
  End = 35,
  Home = 36,
  Left = 37,
  Up = 38,
  Right = 39,
  Down = 40,
  Select = 41,
  Print = 42,
  Execute = 43,
  PrintScreen = 44,
  Insert = 45,
  Delete = 46,
  Help = 47,
  D0 = 48,
  D1 = 49,
  D2 = 50,
  D3 = 51,
  D4 = 52,
  D5 = 53,
  D6 = 54,
  D7 = 55,
  D8 = 56,
  D9 = 57,
  A = 65,
  B = 66,
  C = 67,
  D = 68,
  E = 69,
  F = 70,
  G = 71,
  H = 72,
  I = 73,
  J = 74,
  K = 75,
  L = 76,
  M = 77,
  N = 78,
  O = 79,
  P = 80,
  Q = 81,
  R = 82,
  S = 83,
  T = 84,
  U = 85,
  V = 86,
  W = 87,
  X = 88,
  Y = 89,
  Z = 90,
  LeftWindows = 91,
  RightWindows = 92,
  Apps = 93,
  Sleep = 95,
  NumPad0 = 96,
  NumPad1 = 97,
  NumPad2 = 98,
  NumPad3 = 99,
  NumPad4 = 100,
  NumPad5 = 101,
  NumPad6 = 102,
  NumPad7 = 103,
  NumPad8 = 104,
  NumPad9 = 105,
  Multiply = 106,
  Add = 107,
  Separator = 108,
  Subtract = 109,
  Decimal = 110,
  Divide = 111,
  F1 = 112,
  F2 = 113,
  F3 = 114,
  F4 = 115,
  F5 = 116,
  F6 = 117,
  F7 = 118,
  F8 = 119,
  F9 = 120,
  F10 = 121,
  F11 = 122,
  F12 = 123,
  F13 = 124,
  F14 = 125,
  F15 = 126,
  F16 = 127,
  F17 = 128,
  F18 = 129,
  F19 = 130,
  F20 = 131,
  F21 = 132,
  F22 = 133,
  F23 = 134,
  F24 = 135,
  NumLock = 144,
  Scroll = 145,
  LeftShift = 160,
  RightShift = 161,
  LeftControl = 162,
  RightControl = 163,
  LeftAlt = 164,
  RightAlt = 165,
  BrowserBack = 166,
  BrowserForward = 167,
  BrowserRefresh = 168,
  BrowserStop = 169,
  BrowserSearch = 170,
  BrowserFavorites = 171,
  BrowserHome = 172,
  VolumeMute = 173,
  VolumeDown = 174,
  VolumeUp = 175,
  MediaNextTrack = 176,
  MediaPreviousTrack = 177,
  MediaStop = 178,
  MediaPlayPause = 179,
  LaunchMail = 180,
  SelectMedia = 181,
  LaunchApplication1 = 182,
  LaunchApplication2 = 183,
  OemSemicolon = 186,
  OemPlus = 187,
  OemComma = 188,
  OemMinus = 189,
  OemPeriod = 190,
  OemQuestion = 191,
  OemTilde = 192,
  OemOpenBrackets = 219,
  OemPipe = 220,
  OemCloseBrackets = 221,
  OemQuotes = 222,
  Oem8 = 223,
  OemBackslash = 226,
  ProcessKey = 229,
  Attn = 246,
  Crsel = 247,
  Exsel = 248,
  EraseEof = 249,
  Play = 250,
  Zoom = 251,
  Pa1 = 253,
  OemClear = 254,
  ChatPadGreen = 0xca,
  ChatPadOrange = 0xcb,
  Pause = 0x13,
  ImeConvert = 0x1c,
  ImeNoConvert = 0x1d,
  Kana = 0x15,
  Kanji = 0x19,
  OemAuto = 0xf3,
  OemCopy = 0xf2,
  OemEnlW = 0xf4,
}

export enum AssetType {
  Gump = 0,
  Arts = 1,
  Animation = 2,
}

export enum CompressionType {
  None = 0,
  Zlib = 1,
}

export enum ClayWidgetType {
  None = 0,
  Button = 1,
  TextInput = 2,
}

export enum ClayUOCommandType {
  None = 0,
  Text = 1,
  Gump = 2,
  GumpNinePatch = 3,
  Art = 4,
  Land = 5,
  Animation = 6,
}

export enum TermOp {
  With = 0,
  Without = 1,
  Optional = 2,
}

export enum UIInteractionState {
  None = 0,
  Over = 1,
  Pressed = 2,
  Released = 3,
}

export enum ClayLayoutDirection {
  LeftToRight = 0,
  TopToBottom = 1,
}

export enum ClayLayoutAlignmentX {
  Left = 0,
  Right = 1,
  Center = 2,
}

export enum ClayLayoutAlignmentY {
  Top = 0,
  Bottom = 1,
  Center = 2,
}

export enum ClaySizingType {
  Fit = 0,
  Grow = 1,
  Percent = 2,
  Fixed = 3,
}

export enum ClayFloatingAttachToElement {
  None = 0,
  Parent = 1,
  ElementWithId = 2,
  Root = 3,
}

export enum ClayFloatingClipToElement {
  None = 0,
  AttachedParent = 1,
}

export enum ClayTextAlignment {
  Left = 0,
  Center = 1,
  Right = 2,
}

// Data structures
export interface Vector2 {
  x: number;
  y: number;
}

export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

export interface ClayColor {
  r: number;
  g: number;
  b: number;
  a: number;
}

export interface ClaySizingAxis {
  type: ClaySizingType;
  minMax?: { min: number; max: number };
  percent?: number;
}

export interface ClaySizing {
  width: ClaySizingAxis;
  height: ClaySizingAxis;
}

export interface ClayChildAlignment {
  x: ClayLayoutAlignmentX;
  y: ClayLayoutAlignmentY;
}

export interface ClayLayoutConfig {
  sizing?: ClaySizing;
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

export interface ClayElementDeclProxy {
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
  hue: Vector3;
}

export interface ClayTextProxy {
  textColor: ClayColor;
  fontId: number;
  fontSize: number;
  letterSpacing?: number;
  lineHeight?: number;
  wrapMode?: number;
  textAlignment?: ClayTextAlignment;
}

export interface UITextProxy {
  value: string;
  replacedChar?: string;
  textConfig: ClayTextProxy;
}

export interface UOButtonWidgetProxy {
  normal: number;
  pressed: number;
  over: number;
}

export interface UINodeProxy {
  id: number;
  config: ClayElementDeclProxy;
  uoConfig?: ClayUOCommandData;
  textConfig?: UITextProxy;
  uoButton?: UOButtonWidgetProxy;
  widgetType?: ClayWidgetType;
  movable?: boolean;
  acceptInputs?: boolean;
}

export interface UINodes {
  nodes: UINodeProxy[];
  relations: Record<number, number>;
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

export interface ComponentInfoProxy {
  id: number;
  size: number;
  name: string;
}

export interface ArchetypeProxy {
  components: ComponentInfoProxy[];
  entities: number[];
}

export interface QueryRequest {
  terms: Array<{ ids: number; op: TermOp }>;
}

export interface QueryResponse {
  results: ArchetypeProxy[];
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
