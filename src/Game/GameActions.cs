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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network;

using Microsoft.Xna.Framework;

using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game
{
    internal static class GameActions
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

	    public static void UpdateWarMode(bool state)
	    {
		    Socket.Send(new PChangeWarMode(state));
	    }

		public static void OpenPaperdoll(Serial serial)
            => DoubleClick(serial | 0x80000000);

        public static void Attack(Serial serial)
        {
            if (Engine.Profile.Current.EnabledCriminalActionQuery)
            {
                Mobile m = World.Mobiles.Get(serial);

                if (m != null && World.Player.NotorietyFlag == NotorietyFlag.Innocent && m.NotorietyFlag == NotorietyFlag.Innocent && m != World.Player)
                {

                    QuestionGump messageBox = new QuestionGump("This may flag\nyou criminal!",
                                                                   s =>
                                                                   {
                                                                       if (s)
                                                                           Socket.Send(new PAttackRequest(serial));
                                                                   });

                    Engine.UI.Add(messageBox);
                    return;
                }
            }

            World.LastAttack = serial;
            Socket.Send(new PAttackRequest(serial));
        } 

        public static void DoubleClick(Serial serial)
        {
            Socket.Send(new PDoubleClickRequest(serial));
        }

        public static void SingleClick(Serial serial)
        {
            // add  request context menu
            Socket.Send(new PClickRequest(serial));
        }

        public static void Say(string message, ushort hue = 0x17, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal)
        {
            Socket.Send(new PUnicodeSpeechRequest(message, type, font, hue, "ENU"));
        }

        public static void SayParty(string message)
        {
            Socket.Send(new PPartyMessage(message, 0));
        }

        public static void RequestPartyAccept(Serial serial)
        {
            Socket.Send(new PPartyAccept(serial));
        }

        public static void RequestPartyRemoveMember(Serial serial)
        {
            Socket.Send(new PPartyRemoveRequest(serial));
        }

        public static void RequestPartyQuit()
        {
            Socket.Send(new PPartyRemoveRequest(World.Player));
        }

        public static void RequestPartyInviteByTarget()
        {
            Socket.Send(new PPartyInviteRequest());
        }

        public static void RequestPartyLootState(bool isLootable)
        {
            Socket.Send(new PPartyChangeLootTypeRequest(isLootable));
        }

        public static void PickUp(Serial item, Point point, int? amount = null)
        {
            PickUp(item, point.X, point.Y, amount);
        }

        public static void PickUp(Serial item, int x, int y, int? amount = null)
        {
            _pickUpAction(World.Items.Get(item), x, y, amount);
        }

        public static void PickUp(Serial item, int? amount = null)
        {
            _pickUpAction(World.Items.Get(item), 0, 0, amount);
        }

        public static void DropItem(Serial serial, int x, int y, int z, Serial container)
        {

            if (FileManager.ClientVersion >= ClientVersions.CV_6017)
            {
                Socket.Send(new PDropRequestNew(serial, (ushort)x, (ushort)y, (sbyte)z, 0, container));
            }
            else
            {
                Socket.Send(new PDropRequestOld(serial, (ushort)x, (ushort)y, (sbyte)z, container));

            }
        }

        public static void DropItem(Serial serial, Position position, Serial container)
        {
            DropItem(serial, position.X, position.Y, position.Z, container);
        }

        public static void Equip(Serial serial, Layer layer, Serial target)
        {
            Socket.Send(new PEquipRequest(serial, layer, target));
        }

        public static void ReplyGump(Serial local, Serial server, int button, Serial[] switches = null, Tuple<ushort, string>[] entries = null)
        {
            Socket.Send(new PGumpResponse(local, server, button, switches, entries));
        }

        public static void RequestHelp()
        {
            Socket.Send(new PHelpRequest());
        }

        public static void RequestQuestMenu()
        {
            Socket.Send(new PQuestMenuRequest());
        }

	    public static void RequestProfile(Serial serial)
	    {
		    Socket.Send(new PProfileRequest(serial));
	    }

        public static void ChangeSkillLockStatus(ushort skillindex, byte lockstate)
        {
            Socket.Send(new PSkillsStatusChangeRequest(skillindex, lockstate));
        }

        public static void RequestMobileStatus(Serial serial)
        {
            Socket.Send(new PStatusRequest(serial));
        }

        public static void TargetCancel(Serial cursorID, byte cursorType)
        {
            Socket.Send(new PTargetCancel(cursorID, cursorType));
        }

        public static void TargetObject(Serial entity, Serial cursorID, byte cursorType)
        {
            Socket.Send(new PTargetObject(entity, cursorID, cursorType));
        }

        public static void TargetXYZ(ushort x, ushort y, short z, ushort modelNumber, Serial cursorID, byte targetType)
        {
            Socket.Send(new PTargetXYZ(x, y, z, modelNumber, cursorID, targetType));
        }

        public static void CastSpellFromBook(int index, Serial bookSerial)
        {
            Socket.Send(new PCastSpellFromBook(index, bookSerial));
        }

        public static void CastSpell(int index)
        {
            Socket.Send(new PCastSpell(index));
        }

        public static void OpenGuildGump()
        {
            Socket.Send(new PGuildMenuRequest());
        }

        public static void ChangeStatLock(byte stat, Lock state)
        {
            Socket.Send(new PChangeStatLockStateRequest(stat, state));
        }

        public static void Rename(Serial serial, string name)
        {
            Socket.Send(new PRenameRequest(serial, name));
        }

        public static void UseSkill(int index)
        {
            Socket.Send(new PUseSkill(index));
        }

        public static void OpenPopupMenu(Serial serial)
        {
            Socket.Send(new PRequestPopupMenu(serial));
        }

        public static void ResponsePopupMenu(Serial serial, ushort index)
        {
            Socket.Send(new PPopupMenuSelection(serial, index));
        }

        public static void SystemMessage(string message) => Chat.Print(message);

        public static void SystemMessage(string message, ushort hue) => Chat.Print(message, hue);

        public static void MessageOverhead(string message, Serial entity) => World.Get(entity)?.Print(message);

        public static void MessageOverhead(string message, ushort hue, Serial entity) => World.Get(entity)?.Print(message, hue);

        public static void AcceptTrade(Serial serial, bool accepted)
            => Socket.Send(new PTradeResponse(serial, 2, accepted));

        public static void CancelTrade(Serial serial)
            => Socket.Send(new PTradeResponse(serial, 1, false));

        public static void AllNames()
        {
            foreach (Mobile mobile in World.Mobiles)
            {
                if (mobile != World.Player)
                {
                    Socket.Send(new PClickRequest(mobile));
                }
            }

            foreach (Item item in World.Items.Where(s => s.IsCorpse))
            {
                Socket.Send(new PClickRequest(item));
            }
        }

        public static void OpenDoor()
            => Socket.Send(new POpenDoor());

        public static void EmoteAction(string action)
            => Socket.Send(new PEmoteAction(action));

        public static void OpenAbilitiesBook()
        {
            if (Engine.UI.GetByLocalSerial<CombatBookGump>() == null)
            {
                Engine.UI.Add(new CombatBookGump(100, 100));
            }
        }

        public static void UseAbility(byte index)
            => Socket.Send(new PUseCombatAbility(index));

	    public static void QuestArrow(bool rightClick) => Socket.Send(new PClickQuestArrow(rightClick));
    }
}