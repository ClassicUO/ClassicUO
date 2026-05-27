// TODO: migrate to Bevy.UI / current TinyEcs.Bevy API.
// This file exposed Extism host functions that read from `world.GetResource<T>()`
// — that API is gone (resources live on App now). It also exposed the
// `cuo_ui_node` bridge which is being replaced by the Bevy.UI Node model.
// The whole module is stubbed to keep ModdingPlugin compilable; the WASM mods
// don't currently exercise these host functions (every callsite in
// ModdingPlugin is commented out).

using System;
using ClassicUO.Ecs.Modding.Guest;
using Extism.Sdk;
using TinyEcs;

namespace ClassicUO.Ecs.Modding;

internal static class Api
{
    public static HostFunction[] Functions(WeakReference<Mod> weakRef, World world)
    {
        // TODO: migrate to Bevy.UI — return the real bindings once the App
        // reference + new UINode flow is available here.
        return Array.Empty<HostFunction>();
    }
}
