using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;
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

        public static void Say(string message, ushort hue = 0x17, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal)
            => Socket.Send(new PUnicodeSpeechRequest(message, type, font, hue, "ENU"));

        public static void SayParty(string message)
            => Socket.Send(new PPartyMessage(message, World.Player));

        public static void PickUp(Serial serial, ushort count)
            => Socket.Send(new PPickUpRequest(serial, count));

        public static void DropDown(Serial serial, int x, int y, int z, Serial container)
            => Socket.Send(new PDropRequestNew(serial, (ushort)x, (ushort)y, (sbyte)z, 0, container));

        public static void DropDown(Serial serial, Position position, Serial container)
            => DropDown(serial, position.X, position.Y, position.Z, container);

        public static void Equip(Serial serial, Layer layer)
            => Socket.Send(new PEquipRequest(serial, layer, World.Player));
    }
}
