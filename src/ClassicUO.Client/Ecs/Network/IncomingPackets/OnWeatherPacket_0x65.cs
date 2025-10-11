using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnWeatherPacket_0x65 : IPacket
{
    public byte Id => 0x65;

    public WeatherType WeatherType { get; private set; }
    public byte Count { get; private set; }
    public byte Temperature { get; private set; }

    public void Fill(StackDataReader reader)
    {
        WeatherType = (WeatherType)reader.ReadUInt8();
        Count = reader.ReadUInt8();
        Temperature = reader.ReadUInt8();
    }
}
