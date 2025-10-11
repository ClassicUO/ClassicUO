using System.Collections.Generic;
using ClassicUO.IO;
using ClassicUO.Utility;

namespace ClassicUO.Ecs;

internal struct OnCustomHousePacket_0xD8 : IPacket
{
    internal struct PlaneData
    {
        public uint Header;
        public int PlaneZ;
        public int PlaneMode;
        public byte[] Data;
    }

    public byte Id => 0xD8;

    public bool IsCompressed { get; private set; }
    public bool Response { get; private set; }
    public uint Serial { get; private set; }
    public uint Revision { get; private set; }
    public List<PlaneData> Planes { get; private set; }

    public void Fill(StackDataReader reader)
    {
        IsCompressed = reader.ReadUInt8() == 0x03;
        Response = reader.ReadBool();
        Serial = reader.ReadUInt32BE();
        Revision = reader.ReadUInt32BE();
        reader.Skip(4);

        Planes ??= new List<PlaneData>();
        Planes.Clear();
        var planesCount = reader.ReadUInt8();
        Planes.Capacity = planesCount;

        for (var i = 0; i < planesCount; ++i)
        {
            var header = reader.ReadUInt32BE();
            var decompressedLength = (int)(((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
            var compressedLength = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
            var planeZ = (int)((header & 0x0F000000) >> 24);
            var planeMode = (int)((header & 0xF0000000) >> 28);

            if (compressedLength <= 0)
            {
                continue;
            }

            var compressed = reader.ReadArray(compressedLength);
            var planeBuffer = new byte[decompressedLength];
            ZLib.Decompress(compressed, 0, planeBuffer, decompressedLength);

            Planes.Add(new PlaneData
            {
                Header = header,
                PlaneZ = planeZ,
                PlaneMode = planeMode,
                Data = planeBuffer
            });
        }
    }
}
