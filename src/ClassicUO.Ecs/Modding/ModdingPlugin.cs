// ClassicUO's composition of the generic component-model modding runtime
// (TinyEcs.Bevy.Modding). The reusable library owns mod loading + per-stage
// dispatch + the click bridge; this plugin supplies the cuo-specific pieces:
//   - the cuo component/resource registry (CuoModdingRegistry),
//   - per-mod linker hooks (cuo:modding/net + /ui imports, input-consume wiring),
//   - the UO network tap drain (cuo:net/incoming poll-entities).

using System;
using ClassicUO.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
// Alias: this plugin is also named ModdingPlugin, so an unqualified ModdingPlugin
// in this file resolves to itself; reference the generic component-model runtime
// (TinyEcs.Bevy.Modding.ModdingPlugin) by an unambiguous alias.
using ModdingRuntimePlugin = TinyEcs.Bevy.Modding.ModdingPlugin;

namespace ClassicUO.Ecs.Modding;

internal readonly struct ModdingPlugin : IPlugin
{
    public void Build(App app)
    {
        // cuo registry + per-mod hooks → the generic plugin's config. Reuse a
        // pre-registered config if present (tests inject a ModFolder for true
        // isolation); otherwise create the default. Either way supply the cuo
        // registry + hooks before AddPlugin<ModdingPlugin> so the lib picks it up.
        var hadConfig = app.HasResource<ModdingConfig>();
        var config = hadConfig ? app.GetResource<ModdingConfig>() : new ModdingConfig();
        config.Registry = CuoModdingRegistry.Build();
        config.PerMod.Add((linker, ctx) =>
        {
            // cuo-specific imports (UO networking + UI helpers). Defined
            // unconditionally — harmless if a mod doesn't import cuo:modding,
            // required if it does.
            linker.Define(new CuoNetBridge(ctx));
            // Route the generic input-consume capability to the cuo MouseContext.
            CuoModBridge.WireInput(ctx);
            // A mod is present → open the network tap so PacketReader emits the
            // cuo:net/incoming event. No mods loaded ⇒ never set ⇒ zero per-packet cost.
            ctx.App!.GetResource<ModNetTap>().Tapped = true;
        });
        if (!hadConfig)
            app.AddResource(config);

        // Network tap (observe/block). NetworkPlugin also registers it in the full
        // app; guard so the bare modding app (tests) has it too.
        if (!app.HasResource<ModNetTap>())
            app.AddResource(new ModNetTap());

        // The generic modding runtime (loader + per-stage dispatch + click bridge).
        // Added before the cuo systems below so their Before/After labels resolve.
        app.AddPlugin<ModdingRuntimePlugin>();
    }

}

// cuo host-capability wiring for one mod context. Shared by the plugin's per-mod
// hook and the bridge tests so both exercise the same code (the lib owns no input
// device of its own; the host routes the generic input-consume capability here).
internal static class CuoModBridge
{
    public static void WireInput(ModHostContext ctx)
        => ctx.ConsumeMouse = button =>
        {
            if (ctx.App != null && ctx.App.HasResource<MouseContext>())
                ctx.App.GetResource<MouseContext>().Consume((MouseButtonType)button);
        };
}
