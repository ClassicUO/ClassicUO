// Wire schema for the modding UI bridge (cuo_ui_node).
//
// Option B: the mod's reconciler speaks TinyEcs.Bevy.UI vocabulary directly, so
// these proxies mirror the engine's UI primitives 1:1 and the host stores them
// near-verbatim (see Api.cs cuo_ui_node). Kept JSON-friendly (plain fields,
// camelCase via ModdingJsonContext) and decoupled from the engine structs so a
// renderer/layout change doesn't silently reshape the mod ABI.

using System.Collections.Generic;

namespace ClassicUO.Ecs.Modding.UI;

// ValType: 0 = Auto, 1 = Px, 2 = Percent (matches TinyEcs.Bevy.UI.ValType).
internal record struct ValProxy(byte Type, float Value);

internal record struct RectProxy(ValProxy Left, ValProxy Right, ValProxy Top, ValProxy Bottom);

// RGBA, 0-255 convention (Clay.Color).
internal record struct ColorProxy(float R, float G, float B, float A);

// Mirrors TinyEcs.Bevy.UI.Node. Enum fields are bytes matching the engine enums
// (Display/PositionType/Overflow/FlexDirection/JustifyContent/AlignItems).
internal record struct NodeProxy(
    byte Display,
    byte PositionType,
    byte Overflow,
    byte FlexDirection,
    byte JustifyContent,
    byte AlignItems,
    ValProxy Width,
    ValProxy Height,
    ValProxy MinWidth,
    ValProxy MinHeight,
    ValProxy MaxWidth,
    ValProxy MaxHeight,
    ValProxy Left,
    ValProxy Top,
    ValProxy Right,
    ValProxy Bottom,
    RectProxy Padding,
    RectProxy Border,
    ValProxy Gap,
    float AspectRatio
);

// Editable: turn the node into a focusable text field driven by the shared host
// editor (caret, selection, arrows, Ctrl+A/C/X/V/Z) — see Api.cs cuo_ui_node.
// The guest gets edits via an OnTextChanged UI event (UIEvent.Value). Masked
// renders the value as '*'. Contract for editable nodes: send `Value` only when
// the guest controls it (controlled input); an uncontrolled field must stop
// resending Value after mount or it clobbers what the user types.
internal record struct TextProxy(string Value, ushort FontId, ushort FontSize, ColorProxy Color, bool Editable, bool Masked);

// UO custom render: Kind matches UOCustomKind; Hue is a Vector3 (X/Y/Z).
// AnimAction/AnimDir/AnimFrame drive UOCustomKind.Animation (mobile player):
// group, stored direction (0..4), frame index. Default 0 for non-animation nodes.
internal record struct UORenderProxy(byte Kind, uint AssetId, float HueX, float HueY, float HueZ,
    byte AnimAction, byte AnimDir, int AnimFrame);

// UO button graphics per Interaction state. The host UpdateUOButtonsState system
// swaps UOCustomRender.AssetId between these by hover/press.
internal record struct UOButtonProxy(ushort Normal, ushort Over, ushort Pressed);

internal record struct UINodeProxy(
    ulong Id,
    NodeProxy Node,
    ColorProxy? BackgroundColor,
    ColorProxy? BorderColor,
    float? BorderRadius,
    TextProxy? Text,
    UORenderProxy? Uo,
    bool Interactive,
    bool Movable,
    // GlobalZIndex. Root (parentless) nodes need this to layer over the world
    // scene — see reference_ecs_root_node_layout; LayoutSystem threads it down.
    int? Z,
    UOButtonProxy? Button,
    // Opt this node out of window-drag latching (WindowDragPlugin yields the
    // gesture on UINoWindowDrag) so an interactive control inside a movable mod
    // window reaches its own UiClick. Buttons/editable fields get this implicitly;
    // set it for plain interactive nodes (tabs, list rows, sliders).
    bool NoDrag
);

internal record struct UINodeRelation(ulong Child, ulong Parent, int Index);

internal record struct UINodes(
    List<UINodeProxy> Nodes,
    List<UINodeRelation> Relations
);
