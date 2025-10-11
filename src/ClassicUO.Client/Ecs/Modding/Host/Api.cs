using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Ecs.Modding.Guest;
using ClassicUO.Ecs.Modding.UI;
using ClassicUO.Network;
using Extism.Sdk;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs.Modding;

internal static class Api
{
    public static HostFunction[] Functions
    (
        WeakReference<Mod> weakRef,
        World world
    )
    {
        var tuple = (weakRef, world);
        HostFunction[] functions = [
            HostFunction.FromMethod("cuo_get_packet_size", tuple, static (CurrentPlugin p, long offset) =>
            {
                (_, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var network = world.GetResource<NetClient>();
                var span = p.ReadBytes(offset);
                var size = network.PacketsTable.GetPacketLength(span[0]);
                return p.WriteBytes(size.AsBytes());
            }),

            HostFunction.FromMethod("cuo_send_to_server", tuple, static (CurrentPlugin p, long offset) =>
            {
                (_, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var network = world.GetResource<NetClient>();
                var packet = p.ReadBytes(offset);
                network.Send(packet, true);
            }),

            HostFunction.FromMethod("cuo_set_packet_handler", tuple, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var packetMap = world.GetResource<PacketsMap>();
                var handlerInfo = p.ReadString(offset).FromJson<PacketHandlerInfo>();

                if (modRef.TryGetTarget(out var mod) && mod.Plugin.FunctionExists(handlerInfo.FuncName))
                {
                    packetMap[handlerInfo.PacketId] = buffer
                        => mod.Plugin.Call(handlerInfo.FuncName, buffer);
                }
            }),

            HostFunction.FromMethod("cuo_add_packet_handler", tuple, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var packetMap = world.GetResource<PacketsMap>();

                var handlerInfo = p.ReadString(offset).FromJson<PacketHandlerInfo>();

                if (modRef.TryGetTarget(out var mod) && mod.Plugin.FunctionExists(handlerInfo.FuncName))
                {
                    if (!packetMap.TryGetValue(handlerInfo.PacketId, out var fn))
                    {
                        return;
                    }

                    packetMap[handlerInfo.PacketId] = buffer => {
                        mod.Plugin.Call(handlerInfo.FuncName, buffer);
                        fn(buffer);
                    };
                }
            }),

            HostFunction.FromMethod("cuo_set_sprite", tuple, static (CurrentPlugin p, long offset) =>
            {
                (_, var world) = p.GetUserData<(WeakReference<Mod>, World)>();

                var assets = world.GetResource<AssetsServer>();
                var spriteDesc = p.ReadString(offset).FromJson<SpriteDescription>();
                var data = Convert.FromBase64String(spriteDesc.Base64Data);

                if (spriteDesc.Compression == CompressionType.Zlib)
                {
                    data = Uncompress(data);
                }

                var pixels = MemoryMarshal.Cast<byte, uint>(data);

                switch (spriteDesc.AssetType)
                {
                    case AssetType.Gump:
                        assets.Gumps.SetGump(spriteDesc.Idx, pixels, spriteDesc.Width, spriteDesc.Height);
                        break;

                    case AssetType.Arts:
                        assets.Arts.SetArt(spriteDesc.Idx, pixels, spriteDesc.Width, spriteDesc.Height);
                        break;

                    default:
                        Console.WriteLine("'cuo_set_sprite' for {0} not implemented yet", spriteDesc.AssetType);
                        break;
                }
            }),

            HostFunction.FromMethod("cuo_get_sprite", tuple, static (CurrentPlugin p, long offset) =>
            {
                (_, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var fileManager = world.GetResource<UOFileManager>();
                var spriteDesc = p.ReadString(offset).FromJson<SpriteDescription>();

                switch (spriteDesc.AssetType)
                {
                    case AssetType.Gump:
                        var gumpInfo = fileManager.Gumps.GetGump(spriteDesc.Idx);
                        if (gumpInfo.Pixels.IsEmpty)
                            return p.WriteBytes([]);

                        var json = createSpriteDesc(spriteDesc, gumpInfo.Pixels.AsBytes(), (gumpInfo.Width, gumpInfo.Height), CompressionType.Zlib)
                            .ToJson();
                        return p.WriteString(json);

                    case AssetType.Arts:
                        var artInfo = fileManager.Arts.GetArt(spriteDesc.Idx);
                        if (artInfo.Pixels.IsEmpty)
                            return p.WriteBytes([]);

                        json = createSpriteDesc(spriteDesc, artInfo.Pixels.AsBytes(), (artInfo.Width, artInfo.Height), CompressionType.Zlib)
                            .ToJson();
                        return p.WriteString(json);

                    default:
                        Console.WriteLine("'cuo_get_sprite' for {0} not implemented yet", spriteDesc.AssetType);
                        break;

                }

                return p.WriteBytes([]);

                static SpriteDescription createSpriteDesc(SpriteDescription input, ReadOnlySpan<byte> data, (int w, int h) imgSize, CompressionType compression)
                {
                    data = compression == CompressionType.Zlib ? Compress(data) : data;
                    var base64Data = Convert.ToBase64String(data);
                    return new SpriteDescription(input.AssetType, input.Idx, imgSize.w, imgSize.h, base64Data, compression);
                }
            }),

            HostFunction.FromMethod("cuo_send_events", tuple, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var str = p.ReadString(offset);
                var events = str.FromJson<PluginMessages>();

                modRef.TryGetTarget(out var mod);
                foreach (var ev in events.Messages)
                    world.SendEvent((mod, ev));
            }),

            HostFunction.FromMethod("cuo_get_player_serial", tuple, static p =>
            {
                (_, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                ref var gameCtx = ref world.GetResourceRef<GameContext>();
                var span = gameCtx.PlayerSerial.AsBytes();
                var addr = p.WriteBytes(span);
                return addr;
            }),

            HostFunction.FromMethod("cuo_ecs_spawn_entity", tuple, static (CurrentPlugin p) =>
            {
                (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                modRef.TryGetTarget(out var mod);
                var ent = world.Entity().Set(new PluginEntity(mod));
                return (long)ent.ID;
            }),

            HostFunction.FromMethod("cuo_ecs_delete_entity", tuple, static (CurrentPlugin p, long id) =>
            {
                (_, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                if (world.Exists((ulong)id))
                    world.Delete((ulong)id);
            }),

            HostFunction.FromMethod("cuo_ui_node", tuple, static (CurrentPlugin p, long offset) =>
            {
                // (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                // var nodes = p.ReadString(offset).FromJson<UINodes>();
                //
                // modRef.TryGetTarget(out var mod);
                //
                // foreach (var node in nodes.Nodes)
                // {
                //     var ent = world.Entity(node.Id)
                //         .Set(new PluginEntity(mod))
                //         .CreateUINode(new UINode() {
                //             // TODO: missing some config
                //             Config = {
                //                 layout = node.Config.Layout ?? default,
                //                 backgroundColor = node.Config.BackgroundColor ?? default,
                //                 cornerRadius = node.Config.CornerRadius ?? default,
                //                 floating = node.Config.Floating ?? default,
                //                 clip = node.Config.Clip ?? default,
                //                 border = node.Config.Border ?? default,
                //                 image = {
                //                     // imageData = node.Config.Image.Base64Data
                //                 }
                //             },
                //             UOConfig = node.UOConfig ?? default
                //         });
                //
                //     if (node.TextConfig is {} textCfg)
                //     {
                //         var config = new Text() {
                //             Value = textCfg.Value,
                //             TextConfig = {
                //                 fontId = textCfg.TextConfig.FontId,
                //                 fontSize = textCfg.TextConfig.FontSize,
                //                 letterSpacing = textCfg.TextConfig.LetterSpacing,
                //                 lineHeight = textCfg.TextConfig.LineHeight,
                //                 textAlignment = textCfg.TextConfig.TextAlignment,
                //                 textColor = textCfg.TextConfig.TextColor,
                //                 wrapMode = textCfg.TextConfig.WrapMode
                //             }
                //         };
                //
                //         ent.Set(config);
                //     }
                //
                //     if (node.Movable)
                //         ent.Add<UIMovable>();
                //
                //     // if (node.AcceptInputs)
                //     // ent.Set(new UIMouseAction());
                //
                //     if (node.WidgetType == ClayWidgetType.TextInput)
                //         ent.Add<TextInput>();
                //     else if (node.WidgetType == ClayWidgetType.TextFragment)
                //         ent.Add<TextFragment>();
                //     else if (node.WidgetType == ClayWidgetType.Button)
                //     {
                //         if (node.UOButton is {} button)
                //         {
                //             ent.Set(new UOButton()
                //             {
                //                 Normal = button.Normal,
                //                 Over = button.Over,
                //                 Pressed = button.Pressed
                //             });
                //         }
                //     }
                // }
                //
                // foreach (var (child, parent) in nodes.Relations)
                // {
                //     if (!world.Exists(child))
                //         continue;
                //
                //     if (!world.Exists(parent))
                //         continue;
                //
                //     world.Entity(parent).AddChild(child);
                // }
            }),

            HostFunction.FromMethod("cuo_add_entity_to_parent", tuple, static (CurrentPlugin p, long entityId, long parentId, long index) =>
            {
                (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                if (!world.Exists((ulong)entityId) || !world.Exists((ulong)parentId))
                    return;

                Console.WriteLine("cuo_add_entity_to_parent {0} {1} {2}", entityId, parentId, index);

                var parent = world.Entity((ulong)parentId);
                var entity = world.Entity((ulong)entityId);
                parent.AddChild(entity, (int)index);
            }),

            HostFunction.FromMethod("cuo_ui_add_event_listener", tuple, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var addEvent = p.ReadString(offset).FromJson<UIEvent>();

                if (!world.Exists(addEvent.EntityId))
                    return 0ul;

                var entity = world.Entity(addEvent.EntityId);
                var ev = world.Entity().Set(addEvent);

                // when the entity will be deleted, the event will be deleted too.
                // this needs to be handled by the plugin too (?)
                entity.AddChild(ev);

                return ev.ID;
            }),

            HostFunction.FromMethod("cuo_ui_remove_event_listener", tuple, static (CurrentPlugin p, long offset) =>
            {
                (var modRef, var world) = p.GetUserData<(WeakReference<Mod>, World)>();
                var removeEvent = p.ReadString(offset).FromJson<UIEvent>();

                if (!world.Exists(removeEvent.EntityId))
                    return 0ul;

                if (removeEvent.EventId is null)
                    return 0ul;

                if (!world.Exists(removeEvent.EventId.Value))
                    return 0ul;

                var ev = world.Entity(removeEvent.EventId.Value);
                ev.Delete();

                return ev.ID;
            }),
        ];

        return functions;
    }

    private static unsafe byte[] Uncompress(ReadOnlySpan<byte> data)
    {
        fixed (byte* dataPtr = data)
        {
            using var ms = new UnmanagedMemoryStream(dataPtr, data.Length);
            using var deflateStream = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress);
            using var msOut = new MemoryStream();
            deflateStream.CopyTo(msOut);
            return msOut.ToArray();
        }
    }

    private static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var ms = new MemoryStream();
        {
            using var deflateStream = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, leaveOpen: true);
            deflateStream.Write(data);
        }
        return ms.ToArray();
    }
}
