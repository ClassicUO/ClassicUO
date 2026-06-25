using System;

namespace ClassicUO.Ecs;

// Per-incoming-packet filter hook: (id, full framed bytes incl header) -> block?
//   true  => drop the packet before host dispatch (host handlers skip it).
//   false => host handlers run normally.
internal delegate bool IncomingPacketFilter(byte id, ReadOnlySpan<byte> data);

// Neutral host↔mod network filter, written by the modding layer and read by
// NetworkPlugin.PacketReader. Kept in the ClassicUO.Ecs namespace (not Modding) so
// the host network code takes no dependency on the modding layer — the modding
// layer installs Filter; PacketReader only invokes the delegate. Filter is null
// when no loaded mod exports `on-incoming-packet`, so a mod-less (or filter-less)
// client pays nothing per packet.
internal sealed class ModNetTap
{
    public IncomingPacketFilter? Filter;
}
