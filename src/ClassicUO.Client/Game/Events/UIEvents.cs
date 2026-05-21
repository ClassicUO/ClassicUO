// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    internal readonly record struct GumpOpenedArgs(uint Sender, uint GumpId, int X, int Y);

    internal readonly record struct GumpClosedArgs(uint Sender, uint GumpId, int ButtonId);

    internal readonly record struct CompressedGumpOpenedArgs(uint Sender, uint GumpId, int X, int Y);

    internal readonly record struct ContextMenuOpenedArgs(uint Serial, ushort MenuId, string Name);

    internal readonly record struct PaperdollOpenedArgs(uint Serial, string Title, byte Flags);

    internal readonly record struct MapDisplayedArgs(
        uint Serial,
        ushort GumpId,
        ushort StartX,
        ushort StartY,
        ushort EndX,
        ushort EndY,
        ushort Width,
        ushort Height,
        ushort? Facet);

    internal readonly record struct BookOpenedArgs(
        uint Serial,
        bool Editable,
        ushort PageCount,
        bool OldPacket,
        byte[] Data,
        int Offset);

    internal readonly record struct BookDataReceivedArgs(
        uint Serial,
        ushort PageCount,
        byte[] Data,
        int Offset);

    internal readonly record struct TextEntryDialogArgs(
        uint Serial,
        byte ParentId,
        byte ButtonId,
        uint MaxLength,
        string Text,
        string Description);

    internal readonly record struct TipWindowDisplayedArgs(uint TipId, byte Flag, string Text);

    internal readonly record struct BulletinBoardDataReceivedArgs(
        byte Action,
        uint Serial,
        byte[] Data,
        int Offset);

    internal readonly record struct OpenUrlRequestedArgs(string Url);

    internal readonly record struct CharacterProfileOpenedArgs(uint Serial, string Header, string Footer, string Body);

    internal readonly record struct VendorWindowClosedArgs(uint VendorSerial);

    internal readonly record struct QuestArrowDisplayedArgs(bool Display, ushort X, ushort Y, uint Serial);

    internal readonly record struct WaypointDisplayedArgs(
        uint Serial,
        ushort X,
        ushort Y,
        sbyte Z,
        byte Map,
        ushort Type,
        bool IgnoreObject,
        uint Cliloc);

    internal readonly record struct WaypointRemovedArgs(uint Serial);
}
