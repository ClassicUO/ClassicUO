using System.Collections.Generic;

namespace ClassicUO.Game.Services;

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
            for (int i = 0; i < _customHouseRequests.Count; ++i)
            {
                ServiceProvider.Get<PacketHandlerService>().Out.Send_CustomHouseDataRequest(_customHouseRequests[i]);
            }

            _customHouseRequests.Clear();
        }
    }
}