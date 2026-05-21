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

    internal readonly record struct ShopBuyListEntry(uint ItemSerial, uint Price, string Name);

    internal readonly record struct ShopBuyListReceivedArgs(
        uint VendorSerial,
        IReadOnlyList<ShopBuyListEntry> Entries);

    internal readonly record struct ShopSellListEntry(
        uint Serial,
        ushort Graphic,
        ushort Hue,
        ushort Amount,
        ushort Price,
        string Name);

    internal readonly record struct ShopSellListReceivedArgs(
        uint VendorSerial,
        IReadOnlyList<ShopSellListEntry> Entries);

    // TradeWindow design: split per sub-type into discrete events.
    // The original 0x6F packet multiplexes five very different payloads on a
    // single byte sub-type; collapsing them into one record forced every
    // subscriber to switch on SubType and re-interpret generic fields. By
    // splitting we make the contract self-describing and let subscribers
    // ignore sub-types they don't care about.
    internal readonly record struct TradeWindowOpenArgs(
        uint Serial,
        uint Id1,
        uint Id2,
        string Name);

    internal readonly record struct TradeWindowClosedArgs(uint Serial);

    internal readonly record struct TradeWindowAcceptUpdatedArgs(
        uint Serial,
        bool ImAccepting,
        bool HeIsAccepting);

    internal readonly record struct TradeWindowCurrencyUpdatedArgs(
        uint Serial,
        bool IsMine,
        uint Gold,
        uint Platinum);

    internal readonly record struct CustomHouseComponent(
        ushort Graphic,
        sbyte OffsetX,
        sbyte OffsetY,
        sbyte OffsetZ);

    internal readonly record struct CustomHouseReceivedArgs(
        uint Serial,
        uint Revision,
        IReadOnlyList<CustomHouseComponent> Components);
}
