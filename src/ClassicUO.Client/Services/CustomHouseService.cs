using System.Collections.Generic;

namespace ClassicUO.Services;

internal class CustomHouseService : IService
{
    private readonly List<uint> _customHouseRequests = new List<uint>();


    public void AddCustomHouseRequest(uint serial)
    {
        if (!_customHouseRequests.Contains(serial))
        {
            _customHouseRequests.Add(serial);
        }
    }

    public void SendCustomHouseRequests()
    {
        if (_customHouseRequests.Count > 0)
        {
            var outPackets = ServiceProvider.Get<PacketHandlerService>().Out;

            for (int i = 0; i < _customHouseRequests.Count; ++i)
                outPackets.Send_CustomHouseDataRequest(_customHouseRequests[i]);

            _customHouseRequests.Clear();
        }
    }
}