// ClassicUO's composition of the generic component-model modding runtime
// (TinyEcs.Bevy.Modding). The reusable library owns mod loading + per-stage
// dispatch + the click bridge; this plugin supplies the cuo-specific pieces:
//   - the cuo component/resource registry (CuoModdingRegistry),
//   - per-mod linker hooks (cuo:modding/net + /ui imports, input-consume wiring),
//   - the incoming-packet filter hook (ModNetTap.Filter → mods' on-incoming-packet).

using System;
using ClassicUO.Input;
using Microsoft.Xna.Framework.Input;
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
        // Mods folder from settings.json (tests inject their own ModFolder via a
        // pre-registered config, so don't clobber that path).
        if (!hadConfig)
        {
#if AGENT_BUILD
            // Agent-harness builds load NO mods by default: deterministic
            // screenshots / RPC scenarios shouldn't carry the full ecs-mods/ set
            // (mod-spawned windows + per-mod noise). Point discovery at a
            // dedicated agent folder that is empty/absent by default — drop a
            // single mod into ecs-mods-agent/<mod>/ when a scenario needs it.
            // (modding infra still registers: ModControl drives the chat -mods
            // command + topbar regardless.)
            config.ModFolder = "ecs-mods-agent";
#else
            var modsPath = ClassicUO.Configuration.Settings.GlobalSettings?.ModsPath;
            if (!string.IsNullOrWhiteSpace(modsPath))
                config.ModFolder = modsPath;
#endif
        }
        config.Registry = CuoModdingRegistry.Build();
        config.PerMod.Add((linker, ctx) =>
        {
            // cuo-specific imports (UO networking + UI helpers). Defined
            // unconditionally — harmless if a mod doesn't import cuo:modding,
            // required if it does.
            linker.Define(new CuoNetBridge(ctx));
            // Route the generic input-consume capability to the cuo MouseContext.
            CuoModBridge.WireInput(ctx);
            // Install the incoming-packet filter (idempotent — every mod's hook sets
            // the same delegate). PacketReader calls ModNetTap.Filter per packet; it
            // routes to the loaded mods' `on-incoming-packet` export (OR of their
            // bool returns), resolved live so it covers every mod loaded by then.
            // Set here (Startup, after all Build()) so it lands on the final ModNetTap
            // instance — NetworkPlugin re-adds the resource during its own Build.
            var app = ctx.App!;
            app.GetResource<ModNetTap>().Filter = (byte id, System.ReadOnlySpan<byte> data) =>
                app.HasResource<ModRuntimes>()
                && app.GetResource<ModRuntimes>().AnyReturnsTrue("on-incoming-packet", id, data);
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

        // Scene info events (server/character/error lists) are EventWriter/EventReader,
        // not triggers, so a mod's On<T> observer (cuo:scene/server-list etc., wired by
        // ModEvent) wouldn't see them. Re-emit each as a trigger. The EventReader cursor
        // here is independent of the scene plugins' own readers (multi-reader), so it
        // doesn't consume their events. Cheap: these fire only during the login flow.
        var fwdServer = (EventReader<ServerSelectionInfoEvent> r, Commands c)
            => { foreach (var e in r.Read()) c.EmitTrigger(e); };
        var fwdChar = (EventReader<CharacterSelectionInfoEvent> r, Commands c)
            => { foreach (var e in r.Read()) c.EmitTrigger(e); };
        var fwdError = (EventReader<LoginErrorsInfoEvent> r, Commands c)
            => { foreach (var e in r.Read()) c.EmitTrigger(e); };
        app.AddSystem(fwdServer).InStage(Stage.First).Build();
        app.AddSystem(fwdChar).InStage(Stage.First).Build();
        app.AddSystem(fwdError).InStage(Stage.First).Build();
    }

}

// cuo host-capability wiring for one mod context. Shared by the plugin's per-mod
// hook and the bridge tests so both exercise the same code (the lib owns no input
// device of its own; the host routes the generic input-consume capability here).
internal static class CuoModBridge
{
    public static void WireInput(ModHostContext ctx)
    {
        ctx.ConsumeMouse = button =>
        {
            if (ctx.App != null && ctx.App.HasResource<MouseContext>())
                ctx.App.GetResource<MouseContext>().Consume((MouseButtonType)button);
        };
        ctx.ConsumeKeyboard = key =>
        {
            if (ctx.App?.HasResource<KeyboardContext>() == true)
                ctx.App.GetResource<KeyboardContext>().Consume((Keys)key);
        };
    }
}
