using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Network;

using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game
{
    public static class GameActions
    {
        public static void DoubleClick(Serial serial)     
            => Socket.Send(new PDoubleClickRequest(serial));
        

        public static void SingleClick(Serial serial)
            => Socket.Send(new PClickRequest(serial));

    }
}
