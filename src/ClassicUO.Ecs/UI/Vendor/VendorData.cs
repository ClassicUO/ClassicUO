// Vendor (buy/sell) shop store + components. The buy gump opens from 0x24
// graphic 0x0030 (vendor serial); its items live in the vendor's shop-layer
// containers (filled by 0x3C) and get priced/named by 0x74 (stamped onto the
// item entities as VendorListing). The sell gump opens from 0x9E, which carries
// the full item list inline (cached here in SellByVendor). VendorGumpPlugin
// renders either from these.

using System.Collections.Generic;

namespace ClassicUO.Ecs;

// One sellable/buyable line as the gump needs it.
internal struct VendorEntry
{
    public uint Serial;
    public ushort Graphic;
    public ushort Hue;
    public ushort Amount;   // stock available
    public uint Price;
    public string RawName;  // name as sent in the packet (may be blank or a cliloc number)
    public string Name;     // resolved display name (OPL → cliloc → tiledata → raw)
}

// Stamped on a buy item entity by the 0x74 BuyList observer (price + raw packet
// name). The buy gump reads it off each shop-container child and resolves the
// display name live (OPL → cliloc → tiledata → raw) at rebuild — OPL/tooltip
// data usually arrives after the gump opens, like legacy SetNameTo.
internal struct VendorListing
{
    public uint Price;
    public string RawName;
}

// Sell lists arrive whole in 0x9E; buy lists are read live from the entity
// graph, so only sell entries are cached. Revision bumps on any vendor packet
// so an open window rebuilds.
internal sealed class VendorStore
{
    public readonly Dictionary<uint, List<VendorEntry>> SellByVendor = new();
    public int Revision;
}

internal record struct VendorOpenedEvent(uint Vendor, bool IsBuy);

// Window root. Items/Txn are per-window mutable maps (managed refs in the
// component are fine — this is per-entity state, not closure-captured).
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct VendorWindow
{
    public uint Vendor;
    public bool IsBuy;
    public int BuiltRevision;
    public bool Dirty;
    public float BodyHeight;     // resizable middle-panel height
    public float MinBodyHeight;
    public ulong ShopList;       // left scroll column (shop items)
    public ulong TxnList;        // right scroll column (transaction)
    public ulong TotalLabel;
    public ulong GoldLabel;
    // Chrome panels + bottom-anchored controls repositioned on resize (legacy Update()).
    public ulong LeftMid, LeftBottom, RightMid, RightBottom, Handle, Accept, Clear;
    public ulong LeftDownArrow, RightDownArrow;
    public bool IsBuyGold; // total/gold X differs buy vs sell
    public float RightMidExtra; // rightMiddle height = body + this (legacy diff)
    public float RightYBase;    // rightY, for recomputing bottom anchors on resize
    public Dictionary<uint, VendorEntry> Items;  // serial -> shop item
    public Dictionary<uint, int> Txn;            // serial -> chosen amount
}

// Left-pane shop row: double-click adds one (Shift = all remaining) to the txn.
internal struct VendorShopRow { public ulong Window; public uint Serial; }

// Right-pane transaction +/- buttons.
internal struct VendorTxnButton { public ulong Window; public uint Serial; public int Dir; }

// Accept (0) / Clear (1) action buttons.
internal struct VendorActionButton { public ulong Window; public int Action; }

// Scroll arrow (over the baked up/down arrows). Dir -1 up, +1 down; scrolls the
// referenced list while held (legacy ScrollArea up/down hitboxes).
internal struct VendorScrollButton { public ulong List; public int Dir; }

// Bottom-center drag handle that resizes the body height.
internal struct VendorResizeHandle { public ulong Window; }

#if AGENT_BUILD
// Harness-only: debug.openVendor {isBuy:true} sets Pending; the drain system in
// VendorGumpPlugin seeds the buy entity graph and fires the open event.
internal sealed class DebugVendorBuyQueue
{
    public bool Pending;
}
#endif
