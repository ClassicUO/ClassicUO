// TODO: migrate to Bevy.UI
// The old Clay_cs-based proxy types are gone. This file is kept so the JSON
// source generator and modding host stubs still compile. The plugin/UI bridge
// for WASM mods will be rebuilt on top of TinyEcs.Bevy.UI later.

using System.Collections.Generic;

namespace ClassicUO.Ecs.Modding.UI;

internal enum ClayWidgetType
{
    None,
    Button,
    TextInput,
    TextFragment,
}

internal record struct UINodeProxy(ulong Id, ClayWidgetType WidgetType = ClayWidgetType.None, bool Movable = false);

internal record struct UINodes(
    List<UINodeProxy> Nodes,
    Dictionary<ulong, ulong> Relations
);
