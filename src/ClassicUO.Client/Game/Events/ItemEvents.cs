// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events
{
    internal readonly record struct ItemSpawnedArgs(
        uint Serial,
        ushort Graphic,
        ushort Amount,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction,
        ushort Hue,
        Flags Flags);

    internal readonly record struct ItemUpdatedArgs(
        uint Serial,
        ushort Graphic,
        byte GraphicInc,
        ushort Amount,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction,
        ushort Hue,
        Flags Flags,
        byte Type = 0,
        int Unk = 0,
        ushort Unk2 = 1,
        bool IsItemSA = false,
        bool IsFromPacketList = false);

    internal readonly record struct ItemRemovedArgs(uint Serial);

    internal readonly record struct ContainerOpenedArgs(uint Serial, ushort Graphic);

    internal readonly record struct ContainerItemAddedArgs(
        uint Serial,
        ushort Graphic,
        ushort Amount,
        ushort X,
        ushort Y,
        uint ContainerSerial,
        ushort Hue);

    internal readonly record struct ContainerItemsReceivedArgs(uint ContainerSerial, ushort Count);

    internal readonly record struct ItemEquippedArgs(
        uint Serial,
        ushort Graphic,
        Layer Layer,
        uint ContainerSerial,
        ushort Hue);

    internal readonly record struct CorpseEquipmentEntry(Layer Layer, uint ItemSerial);

    internal readonly record struct CorpseEquipmentReceivedArgs(
        uint CorpseSerial,
        IReadOnlyList<CorpseEquipmentEntry> Entries);

    internal readonly record struct DyeDataReceivedArgs(uint Serial, ushort Graphic);

    internal readonly record struct OplInfoReceivedArgs(uint Serial, uint Revision);

    internal readonly record struct MegaClilocReceivedArgs(
        uint Serial,
        uint Revision,
        string Name,
        string Properties,
        int NameCliloc);

    internal readonly record struct ItemDragAnimationArgs(
        ushort Graphic,
        ushort Hue,
        ushort Count,
        uint Source,
        ushort SourceX,
        ushort SourceY,
        sbyte SourceZ,
        uint Destination,
        ushort DestinationX,
        ushort DestinationY,
        sbyte DestinationZ);

    internal readonly record struct ItemMoveDeniedArgs(byte Code);

    internal readonly record struct ItemDragEndedArgs;

    internal readonly record struct ItemDropAcceptedArgs;

    internal readonly record struct ShopBuyListReceivedArgs(uint VendorSerial, byte Count);

    internal readonly record struct ShopSellListReceivedArgs(uint VendorSerial, ushort Count);

    internal readonly record struct TradeWindowArgs(byte SubType, uint Serial);

    internal readonly record struct CustomHouseReceivedArgs(
        uint Serial,
        uint Revision,
        byte[] Data,
        int Offset);
}
