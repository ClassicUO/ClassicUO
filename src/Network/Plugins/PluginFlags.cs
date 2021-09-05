using System;


namespace ClassicUO.Network.Plugins
{
    [Flags]
    enum PluginFlags : ulong
    {
        None = 0,
        CanDraw,
        CanSendPackets,
        CanRecvPackets,


        All = ~None
    }
}
