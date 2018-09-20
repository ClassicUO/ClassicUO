using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game
{
    public static class GameActions
    {
        private static Action<Item, int, int, int?> _pickUpAction;

        internal static void Initialize(Action<Item, int, int, int?> onPickUpAction)
        {
            _pickUpAction = onPickUpAction;
        }




        public static void DoubleClick(Serial serial)     
            => Socket.Send(new PDoubleClickRequest(serial));


        public static void SingleClick(Serial serial)
        {
            // add  request context menu
            Socket.Send(new PClickRequest(serial));
        }

        public static void Say(string message, ushort hue = 0x17, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal)
            => Socket.Send(new PUnicodeSpeechRequest(message, type, font, hue, "ENU"));



        public static void SayParty(string message)
            => Socket.Send(new PPartyMessage(message, World.Player));

        public static void PickUp(Item item, Point point, int? amount = null)     
            => PickUp(item, point.X, point.Y, amount);
     
        public static void PickUp(Item item, int x, int y, int? amount = null)
            => _pickUpAction(item, x, y, amount);

        public static void DropDown(Serial serial, int x, int y, int z, Serial container)
            => Socket.Send(new PDropRequestNew(serial, (ushort)x, (ushort)y, (sbyte)z, 0, container));

        public static void DropDown(Serial serial, Position position, Serial container)
            => DropDown(serial, position.X, position.Y, position.Z, container);

        public static void Equip(Serial serial, Layer layer)
            => Socket.Send(new PEquipRequest(serial, layer, World.Player));

        public static void ReplyGump(Serial local, Serial server, int button, Serial[] switches = null, Tuple<ushort, string>[] entries = null)  
            => Socket.Send(new PGumpResponse(local, server, button, switches, entries));

        
    }
}
