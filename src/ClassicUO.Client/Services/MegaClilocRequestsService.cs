
using System.Collections.Generic;

namespace ClassicUO.Services;

internal class MegaClilocRequestsService : IService
{
    private readonly List<uint> _clilocRequests = new List<uint>();


    public void AddMegaClilocRequest(uint serial)
    {
        if (!_clilocRequests.Contains(serial))
        {
            _clilocRequests.Add(serial);
        }
    }

    public void SendMegaClilocRequests()
    {
        var world = ServiceProvider.Get<UOService>().World;

        if (world.ClientFeatures.TooltipsEnabled && _clilocRequests.Count != 0)
        {
            if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_5090)
            {
                if (_clilocRequests.Count != 0)
                {
                    ServiceProvider.Get<PacketHandlerService>().Out.Send_MegaClilocRequest(_clilocRequests);
                }
            }
            else
            {
                foreach (uint serial in _clilocRequests)
                {
                    ServiceProvider.Get<PacketHandlerService>().Out.Send_MegaClilocRequest_Old(serial);
                }

                _clilocRequests.Clear();
            }
        }
    }
}