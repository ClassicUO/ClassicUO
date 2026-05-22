// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Handles the misc. extended-gump server events (generic-gump close,
    /// statusbar close, equipment-info, popup menu, close-UI by kind, house
    /// design state, race-change request). Replaces the legacy
    /// <c>World.Subscribers.ExtendedGumps</c> partial.
    /// </summary>
    internal sealed class ExtendedGumpsHandler : IEventListener
    {
        private readonly World _world;

        public ExtendedGumpsHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.GenericGumpClose += OnGenericGumpClose;
            EventSink.CloseStatusbarGump += OnCloseStatusbarGump;
            EventSink.EquipInfoReceived += OnEquipInfoReceived;
            EventSink.PopupMenuReceived += OnPopupMenuReceived;
            EventSink.CloseUserInterface += OnCloseUserInterface;
            EventSink.HouseDesignState += OnHouseDesignState;
            EventSink.RaceChangeRequested += OnRaceChangeRequested;
        }

        public void Unsubscribe()
        {
            EventSink.GenericGumpClose -= OnGenericGumpClose;
            EventSink.CloseStatusbarGump -= OnCloseStatusbarGump;
            EventSink.EquipInfoReceived -= OnEquipInfoReceived;
            EventSink.PopupMenuReceived -= OnPopupMenuReceived;
            EventSink.CloseUserInterface -= OnCloseUserInterface;
            EventSink.HouseDesignState -= OnHouseDesignState;
            EventSink.RaceChangeRequested -= OnRaceChangeRequested;
        }

        private void OnGenericGumpClose(GenericGumpCloseArgs e)
        {
            uint ser = e.Serial;
            int button = e.Button;

            LinkedListNode<Gump> first = UIManager.Gumps.First;

            while (first != null)
            {
                LinkedListNode<Gump> nextGump = first.Next;

                if (first.Value.ServerSerial == ser && first.Value.IsFromServer)
                {
                    if (button != 0)
                    {
                        (first.Value as Gump)?.OnButtonClick(button);
                    }
                    else
                    {
                        if (first.Value.CanMove)
                        {
                            UIManager.SavePosition(ser, first.Value.Location);
                        }
                        else
                        {
                            UIManager.RemovePosition(ser);
                        }
                    }

                    first.Value.Dispose();
                }

                first = nextGump;
            }
        }

        private void OnCloseStatusbarGump(CloseStatusbarGumpArgs e)
        {
            UIManager.GetGump<HealthBarGump>(e.Serial)?.Dispose();
        }

        private void OnEquipInfoReceived(EquipInfoArgs e)
        {
            Item item = _world.Items.Get(e.ItemSerial);
            if (item == null) return;

            uint cliloc = e.Cliloc;
            string str = string.Empty;

            if (cliloc > 0)
            {
                str = Client.Game.UO.FileManager.Clilocs.GetString((int)cliloc, true);

                if (!string.IsNullOrEmpty(str))
                {
                    item.Name = str;
                }

                _world.MessageManager.HandleMessage(
                    item,
                    str,
                    item.Name,
                    0x3B2,
                    MessageType.Regular,
                    3,
                    TextType.OBJECT,
                    true
                );
            }

            Span<char> span = stackalloc char[256];
            ValueStringBuilder strBuffer = new ValueStringBuilder(span);

            if (!string.IsNullOrEmpty(e.CrafterName))
            {
                strBuffer.Append(ResGeneral.CraftedBy);
                strBuffer.Append(e.CrafterName);
            }

            if (e.Unidentified)
            {
                strBuffer.Append("[Unidentified");
            }

            byte count = 0;
            uint lastCliloc = 0;

            var lines = e.Lines;
            int lineCount = lines?.Count ?? 0;

            for (int i = 0; i < lineCount; i++)
            {
                EquipInfoLine line = lines[i];
                lastCliloc = (uint)line.Cliloc;
                short charges = line.Charges;
                string attr = Client.Game.UO.FileManager.Clilocs.GetString(line.Cliloc);

                if (attr != null)
                {
                    if (charges == -1)
                    {
                        if (count > 0)
                        {
                            strBuffer.Append("/");
                            strBuffer.Append(attr);
                        }
                        else
                        {
                            strBuffer.Append(" [");
                            strBuffer.Append(attr);
                        }
                    }
                    else
                    {
                        strBuffer.Append("\n[");
                        strBuffer.Append(attr);
                        strBuffer.Append(" : ");
                        strBuffer.Append(charges.ToString());
                        strBuffer.Append("]");
                        count += 20;
                    }
                }

                count++;
            }

            if (count < 20 && count > 0 || e.Unidentified && count == 0)
            {
                strBuffer.Append(']');
            }

            if (strBuffer.Length != 0)
            {
                _world.MessageManager.HandleMessage(
                    item,
                    strBuffer.ToString(),
                    item.Name,
                    0x3B2,
                    MessageType.Regular,
                    3,
                    TextType.OBJECT,
                    true
                );
            }

            strBuffer.Dispose();

            NetClient.Socket.Send_MegaClilocRequest_Old(item);
        }

        private void OnPopupMenuReceived(PopupMenuArgs e)
        {
            UIManager.ShowGamePopup(
                new PopupMenuGump(_world, e.Data)
                {
                    X = _world.DelayedObjectClickManager.LastMouseX,
                    Y = _world.DelayedObjectClickManager.LastMouseY
                }
            );
        }

        private void OnCloseUserInterface(CloseUserInterfaceArgs e)
        {
            uint id = e.GumpKindId;
            uint serial = e.Serial;

            switch (id)
            {
                case 1: // paperdoll
                    UIManager.GetGump<PaperDollGump>(serial)?.Dispose();
                    break;

                case 2: // statusbar
                    UIManager.GetGump<HealthBarGump>(serial)?.Dispose();
                    if (_world.Player != null && serial == _world.Player.Serial)
                    {
                        StatusGumpBase.GetStatusGump()?.Dispose();
                    }
                    break;

                case 8: // char profile
                    UIManager.GetGump<ProfileGump>()?.Dispose();
                    break;

                case 0x0C: // container
                    UIManager.GetGump<ContainerGump>(serial)?.Dispose();
                    break;
            }
        }

        private void OnHouseDesignState(HouseDesignStateArgs e)
        {
            uint serial = e.Serial;

            switch (e.Action)
            {
                case 1: // update
                    break;

                case 2: // remove
                    break;

                case 3: // update multi pos
                    break;

                case 4: // begin
                {
                    HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>();
                    if (gump != null) break;

                    gump = new HouseCustomizationGump(_world, serial, 50, 50);
                    UIManager.Add(gump);
                    break;
                }

                case 5: // end
                    UIManager.GetGump<HouseCustomizationGump>(serial)?.Dispose();
                    break;
            }
        }

        private void OnRaceChangeRequested(RaceChangeRequestedArgs e)
        {
            UIManager.GetGump<RaceChangeGump>()?.Dispose();
            UIManager.Add(new RaceChangeGump(_world, e.IsFemale, e.Race));
        }
    }
}
