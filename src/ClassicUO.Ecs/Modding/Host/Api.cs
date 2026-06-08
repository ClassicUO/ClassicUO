// Extism host-function bindings exposed to WASM mods.
//
// ABI note: signatures here match what the `user-interface` mod imports
// (src/Mods/user-interface/src/index.d.ts) — e.g. entity serials are passed as
// a raw i64, NOT a byte-offset. Other mods (my-plugin) used a different ABI.
//
// This module is the ONE place host code is allowed to touch TinyEcs.World
// directly (see CLAUDE.md ECS rule 1): Extism callbacks fire synchronously
// inside Plugin.Call(...) mid-system, so they can't defer through Commands.
// State is reached through the captured App (resources + world live on App).
//
// P1 implements the data + message-loop functions. The UI bridge
// (cuo_ui_node / event listeners / parenting) is bound as minimal stubs so the
// mod instantiates; bodies land in P2 (UI bridge) and P3 (event routing).

using System;
using System.IO;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Ecs.Modding.Guest;
using ClassicUO.Ecs.Modding.UI;
using ClassicUO.Network;
using Extism.Sdk;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs.Modding;

internal static class Api
{
    public static HostFunction[] Functions(WeakReference<Mod> weakRef, App app)
    {
        var userData = (weakRef, app);

        HostFunction[] functions =
        [
            HostFunction.FromMethod("cuo_send_to_server", userData, static (CurrentPlugin p, long offset) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var packet = p.ReadBytes(offset);
                app.GetResource<NetClient>().Send(packet, true);
            }),

            HostFunction.FromMethod("cuo_get_player_serial", userData, static (CurrentPlugin p) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var serial = app.GetResource<GameContext>().PlayerSerial;
                return p.WriteBytes(serial.AsBytes());
            }),

            HostFunction.FromMethod("cuo_get_entity_graphic", userData, static (CurrentPlugin p, long serial) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var map = app.GetResource<NetworkEntitiesMap>();
                var world = app.GetWorld();
                if (map.TryGet((uint)serial, out var id) && world.Has<Graphic>(id))
                    return p.WriteString(world.Get<Graphic>(id).ToJson());
                return p.WriteString(new Graphic().ToJson());
            }),

            HostFunction.FromMethod("cuo_set_entity_graphic", userData, static (CurrentPlugin p, long serial, long valueOffset) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var map = app.GetResource<NetworkEntitiesMap>();
                var world = app.GetWorld();
                var val = p.ReadString(valueOffset).FromJson<Graphic>();
                if (map.TryGet((uint)serial, out var id) && world.Exists(id))
                    world.Set(id, val);
            }),

            HostFunction.FromMethod("cuo_get_sprite", userData, static (CurrentPlugin p, long offset) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var fileManager = app.GetResource<UOFileManager>();
                var desc = p.ReadString(offset).FromJson<SpriteDescription>();

                switch (desc.AssetType)
                {
                    case AssetType.Gump:
                        var gump = fileManager.Gumps.GetGump(desc.Idx);
                        if (gump.Pixels.IsEmpty)
                            return p.WriteBytes([]);
                        return p.WriteString(MakeSpriteDesc(desc, gump.Pixels.AsBytes(), gump.Width, gump.Height).ToJson());

                    case AssetType.Arts:
                        var art = fileManager.Arts.GetArt(desc.Idx);
                        if (art.Pixels.IsEmpty)
                            return p.WriteBytes([]);
                        return p.WriteString(MakeSpriteDesc(desc, art.Pixels.AsBytes(), art.Width, art.Height).ToJson());

                    default:
                        Console.WriteLine("[mod] cuo_get_sprite: {0} not implemented", desc.AssetType);
                        return p.WriteBytes([]);
                }
            }),

            // Overriding the renderer's sprite cache is gone in the ECS build
            // (Gump/Art no longer expose setters — they hold GPU textures, not
            // pixels). Bound so the mod links; revisit as a P3 nice-to-have.
            HostFunction.FromMethod("cuo_set_sprite", userData, static (CurrentPlugin p, long offset) =>
            {
                Console.WriteLine("[mod] cuo_set_sprite: asset override not supported in ECS build");
            }),

            HostFunction.FromMethod("cuo_send_events", userData, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var events = p.ReadString(offset).FromJson<PluginMessages>();
                modRef.TryGetTarget(out var mod);
                foreach (var ev in events.Messages)
                    app.SendEvent((mod, ev));
            }),

            // ---- UI bridge: P2/P3 stubs (bound so the mod instantiates) ----

            HostFunction.FromMethod("cuo_ecs_spawn_entity", userData, static (CurrentPlugin p) =>
            {
                (var modRef, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                modRef.TryGetTarget(out var mod);
                // Mark mod-owned so teardown can find every entity this mod created.
                var ent = app.GetWorld().Entity().Set(new PluginEntity(mod));
                return (long)ent.ID;
            }),

            HostFunction.FromMethod("cuo_ecs_delete_entity", userData, static (CurrentPlugin p, long id) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var world = app.GetWorld();
                // Drop the node's event listeners so they don't leak across the
                // mod's show/hide churn (tooltips re-spawn entities constantly).
                app.GetResource<ModListeners>().RemoveNode((ulong)id);
                if (world.Exists((ulong)id))
                    world.Delete((ulong)id);
            }),

            HostFunction.FromMethod("cuo_add_entity_to_parent", userData, static (CurrentPlugin p, long entityId, long parentId, long index) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var world = app.GetWorld();
                if (world.Exists((ulong)entityId) && world.Exists((ulong)parentId))
                    world.AddChild((ulong)parentId, (ulong)entityId, (int)index);
            }),

            HostFunction.FromMethod("cuo_ui_node", userData, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var world = app.GetWorld();
                modRef.TryGetTarget(out var mod);

                var tree = p.ReadString(offset).FromJson<UINodes>();

                foreach (var n in tree.Nodes)
                {
                    // Id 0 = let the host allocate (used when a mod builds a node
                    // without a prior cuo_ecs_spawn_entity round-trip).
                    var ent = n.Id == 0 ? world.Entity() : world.Entity(n.Id);
                    ent.Set(ToNode(n.Node));
                    ent.Set(new PluginEntity(mod));

                    if (n.BackgroundColor is { } bg)
                        ent.Set(new BackgroundColor(ToColor(bg)));
                    if (n.BorderColor is { } bc)
                        ent.Set(new BorderColor(ToColor(bc)));
                    if (n.BorderRadius is { } br)
                        ent.Set(BorderRadius.All(br));

                    if (n.Text is { } t)
                    {
                        if (t.Editable)
                        {
                            // Tag the node as the shared editor's glyph: TextInput
                            // (I-beam cursor + "a field is focused"), EditableText
                            // (opt into the stb_textedit-backed editor), TextFieldGeom
                            // (mouse caret origin — the node is its own frame, no
                            // offset) and Interaction so it's clickable to focus.
                            // UIEventsPlugin adds the focus-on-click + caret/selection
                            // overlays + OnTextChanged reporting off the ModEditable tag.
                            ent.Set(new ModEditable { Masked = t.Masked });
                            ent.Set<TextInput>();
                            ent.Set<EditableText>();
                            ent.Set(new TextFieldGeom { Frame = ent.ID, OffsetX = 0 });
                            ent.Set(Interaction.None);

                            // Apply the guest's value only when it differs from the
                            // last value host and guest agreed on (a controlled
                            // update). Equal means the guest is echoing back what the
                            // user typed — leave the live buffer alone so the edit
                            // isn't clobbered each re-render.
                            var agreed = app.GetResource<ModTextValues>();
                            var incoming = t.Value ?? string.Empty;
                            if (!agreed.TryGetValue(ent.ID, out var cur) || !string.Equals(cur, incoming, StringComparison.Ordinal))
                            {
                                agreed[ent.ID] = incoming;
                                if (t.Masked)
                                    ent.Set(new MaskedText { Value = incoming, MaskChar = '*' });
                                else
                                    ent.Set(new Text(incoming));
                            }
                        }
                        else
                        {
                            ent.Set(new Text(t.Value));
                        }
                        ent.Set(new TextFont { FontId = t.FontId, Size = t.FontSize });
                        ent.Set(new TextColor(ToColor(t.Color)));
                    }

                    if (n.Uo is { } uo)
                        ent.Set(new UiCustom
                        {
                            Data = new UOCustomRender
                            {
                                Kind = (UOCustomKind)uo.Kind,
                                AssetId = uo.AssetId,
                                Hue = new Vector3(uo.HueX, uo.HueY, uo.HueZ),
                                AnimAction = uo.AnimAction,
                                AnimDir = uo.AnimDir,
                                AnimFrame = uo.AnimFrame,
                            }
                        });

                    if (n.Z is { } z)
                        ent.Set(new GlobalZIndex(z));

                    if (n.Button is { } btn)
                    {
                        ent.Set(new UOButton { Normal = btn.Normal, Over = btn.Over, Pressed = btn.Pressed });
                        // A button inside a movable mod window must reach its own
                        // UiClick, not latch a window drag on press (WindowDragPlugin
                        // .Drag yields the gesture on this marker).
                        ent.Set<UINoWindowDrag>();
                    }

                    if (n.Interactive)
                        ent.Set(Interaction.None);
                    if (n.Movable)
                        ent.Set<UIMovable>();
                }

                foreach (var rel in tree.Relations)
                    if (world.Exists(rel.Child) && world.Exists(rel.Parent))
                        world.AddChild(rel.Parent, rel.Child, rel.Index);
            }),

            HostFunction.FromMethod("cuo_ui_add_event_listener", userData, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var ev = p.ReadString(offset).FromJson<UIEvent>();
                modRef.TryGetTarget(out var mod);
                return (long)app.GetResource<ModListeners>().Add(ev.EntityId, ev.EventType, mod);
            }),

            HostFunction.FromMethod("cuo_ui_remove_event_listener", userData, static (CurrentPlugin p, long offset) =>
            {
                (_, var app) = p.GetUserData<(WeakReference<Mod>, App)>();
                var ev = p.ReadString(offset).FromJson<UIEvent>();
                if (ev.EventId is { } id)
                    app.GetResource<ModListeners>().Remove(id);
                return 0L;
            }),

            HostFunction.FromMethod("cuo_ecs_query", userData, static (CurrentPlugin p, long offset) =>
            {
                // TODO(P2): rebuild query proxy (QueryRequest/Response need JSON ctx).
                return p.WriteString("{}");
            }),
        ];

        return functions;
    }

    private static ClayColor ToColor(ColorProxy c) => new(c.R, c.G, c.B, c.A);

    private static Val ToVal(ValProxy v) => new((ValType)v.Type, v.Value);

    private static UiRect ToRect(RectProxy r) => new(ToVal(r.Left), ToVal(r.Right), ToVal(r.Top), ToVal(r.Bottom));

    private static Node ToNode(NodeProxy n) => new()
    {
        Display = (Display)n.Display,
        PositionType = (PositionType)n.PositionType,
        Overflow = (Overflow)n.Overflow,
        FlexDirection = (FlexDirection)n.FlexDirection,
        JustifyContent = (JustifyContent)n.JustifyContent,
        AlignItems = (AlignItems)n.AlignItems,
        Width = ToVal(n.Width),
        Height = ToVal(n.Height),
        MinWidth = ToVal(n.MinWidth),
        MinHeight = ToVal(n.MinHeight),
        MaxWidth = ToVal(n.MaxWidth),
        MaxHeight = ToVal(n.MaxHeight),
        Left = ToVal(n.Left),
        Top = ToVal(n.Top),
        Right = ToVal(n.Right),
        Bottom = ToVal(n.Bottom),
        Padding = ToRect(n.Padding),
        Border = ToRect(n.Border),
        Gap = ToVal(n.Gap),
        AspectRatio = n.AspectRatio,
    };

    private static SpriteDescription MakeSpriteDesc(SpriteDescription input, ReadOnlySpan<byte> data, int width, int height)
    {
        var compressed = Compress(data);
        return new SpriteDescription(input.AssetType, input.Idx, width, height, Convert.ToBase64String(compressed), CompressionType.Zlib);
    }

    private static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var ms = new MemoryStream();
        using (var deflate = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, leaveOpen: true))
            deflate.Write(data);
        return ms.ToArray();
    }
}
