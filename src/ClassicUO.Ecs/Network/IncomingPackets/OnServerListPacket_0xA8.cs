using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnServerListPacket_0xA8 : IPacket
{
    public byte Id => 0xA8;

    public byte Flags { get; private set; }
    public List<ServerInfo> Servers { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Flags = reader.ReadUInt8();
        var count = reader.ReadUInt16BE();

        var servers = new List<ServerInfo>(count);
        for (var i = 0; i < count; ++i)
        {
            var index = reader.ReadUInt16BE();
            var name = reader.ReadASCII(32, true);
            var percentFull = reader.ReadUInt8();
            var timeZone = reader.ReadUInt8();
            var address = reader.ReadUInt32BE();

            servers.Add(new ServerInfo(index, name, percentFull, timeZone, address));
        }

        Servers = servers;
    }
}
