// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

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
        string Title,
        string Author);

    /// <summary>
    /// One page of a book payload. <see cref="PageNumber"/> is the 0-based page
    /// index (already decremented from the wire 1-based value). <see cref="Lines"/>
    /// contains the raw line strings as parsed from the packet.
    /// </summary>
    internal readonly record struct BookPage(int PageNumber, IReadOnlyList<string> Lines);

    internal readonly record struct BookDataReceivedArgs(
        uint Serial,
        ushort PageCount,
        IReadOnlyList<BookPage> Pages);

    internal readonly record struct TextEntryDialogArgs(
        uint Serial,
        byte ParentId,
        byte ButtonId,
        uint MaxLength,
        string Text,
        string Description);

    internal readonly record struct TipWindowDisplayedArgs(uint TipId, byte Flag, string Text);

    /// <summary>Bulletin board "open" sub-action (0): server provides board title.</summary>
    internal readonly record struct BulletinBoardOpenedArgs(uint Serial, string Name);

    /// <summary>Bulletin board "summary" sub-action (1): server provides one post header.</summary>
    internal readonly record struct BulletinBoardSummaryArgs(
        uint BoardSerial,
        uint MessageSerial,
        uint ParentSerial,
        string Poster,
        string Subject,
        string DateTime);

    /// <summary>Bulletin board "message" sub-action (2): server provides one full post body.</summary>
    internal readonly record struct BulletinBoardMessageArgs(
        uint BoardSerial,
        uint MessageSerial,
        string Poster,
        string Subject,
        string DateTime,
        string Message);

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
