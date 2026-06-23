using System.Collections.Generic;

namespace ClassicUO.Ecs;

// Neutral host↔mod network tap, written by NetworkPlugin.PacketReader and read
// by the tinyecs:modding modding layer. Kept in the ClassicUO.Ecs namespace (not
// Modding) so the host network code doesn't take a dependency on the modding
// layer — same spirit as the existing EventWriter<IPacket> "kept around for
// mods". Lives next to the modding code only for discoverability.
internal sealed class ModNetTap
{
    // Packet ids a mod asked the host to DROP before dispatch (phase 4 block).
    public readonly HashSet<byte> Blocked = new();

    // Set true once any mod is loaded. PacketReader emits the per-packet
    // cuo:net/incoming event only when this is set, so a mod-less client pays
    // nothing per packet.
    // ponytail: coarse — opens for ANY mod, not just cuo:net/incoming observers.
    // Narrow to actual tappers if a mod-heavy client shows per-packet ToArray cost.
    public bool Tapped;
}

// One incoming network packet, emitted as the cuo:net/incoming custom event mods
// observe. Neutral (ClassicUO.Ecs namespace) so NetworkPlugin can emit it without
// depending on the modding layer; the Modding registry maps it to the WIT
// event. byte[] Payload serializes to base64 lazily — only when a mod observer
// actually fires (System.Text.Json encodes byte[] as a base64 string).
internal struct ModIncomingPacket { public byte Id; public byte[] Payload; }
