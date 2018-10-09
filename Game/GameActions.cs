#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
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

        public static void ToggleWarMode()
        {
            Socket.Send(new PChangeWarMode((World.Player.Flags & Flags.WarMode) == 0));
        }


        public static void DoubleClick(Serial serial)
            => Socket.Send(new PDoubleClickRequest(serial));


        public static void SingleClick(Serial serial)
        {
            // add  request context menu
            Socket.Send(new PClickRequest(serial));
        }

        public static void Say(string message, ushort hue = 0x17, MessageType type = MessageType.Regular,
            MessageFont font = MessageFont.Normal)
            => Socket.Send(new PUnicodeSpeechRequest(message, type, font, hue, "ENU"));

        public static void SayParty(string message)
            => Socket.Send(new PPartyMessage(message, World.Player));

        public static void RequestPartyRemoveMember(Serial serial)
            => Socket.Send(new PPartyRemoveRequest(serial));

        public static void RequestPartyLeave()
            => Socket.Send(new PPartyRemoveRequest(World.Player));

        public static void RequestPartyInviteByTarget()
            => Socket.Send(new PPartyInviteRequest());

        public  static void RequestPartyLootState(bool isLootable)
            => Socket.Send(new PPartyChangeLootTypeRequest(isLootable));

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

        public static void ReplyGump(Serial local, Serial server, int button, Serial[] switches = null,
            Tuple<ushort, string>[] entries = null)
            => Socket.Send(new PGumpResponse(local, server, button, switches, entries));

        public static void RequestHelp()
            => Socket.Send(new PHelpRequest());

        public static void RequestQuestMenu()
            => Socket.Send(new PQuestMenuRequest());

        public static void ChangeSkillLockStatus(ushort skillindex, byte lockstate)
            => Socket.Send(new PSkillsStatusChangeRequest(skillindex, lockstate));

        public static void RequestMobileStatus(Serial serial)
            => Socket.Send(new PStatusRequest(serial));

        public static void RequestTargetCancel(int cursorID, byte cursorType)
            => Socket.Send(new PTargetCancelRequest(cursorID, cursorType));

        public static void RequestTargetObject(Entity entity, int cursorID, byte cursorType)
            => Socket.Send(new PTargetObjectRequest(entity, cursorID, cursorType));

        public static void RequestTargetObjectPosition(ushort x, ushort y, ushort z, ushort modelNumber, int cursorID, byte targetType)
            => Socket.Send(new PTargetObjectPositionRequest(x,y,z,modelNumber, cursorID, targetType));

        public static void CastSpellFromBook(int index, Serial bookSerial)
            => Socket.Send(new PCastSpellFromBook(index, bookSerial));

        public static void CastSpell(int index)
            => Socket.Send(new PCastSpell(index));

    }
}