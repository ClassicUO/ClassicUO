using ClassicUO.Network;
using System;

namespace ClassicUO.Services
{
    internal class PacketHandlerService : IService
    {
        private readonly PacketHandlers _packetHandler;
        private readonly IncomingPackets _incomingPackets;
        private readonly OutgoingPackets _outgoingPackets;
        private readonly NetClient _netClient;

        public PacketHandlerService(
            PacketHandlers packetHandler,
            IncomingPackets incomingPackets,
            OutgoingPackets outgoingPackets,
            NetClient netClient)
        {
            _packetHandler = packetHandler;
            _incomingPackets = incomingPackets;
            _outgoingPackets = outgoingPackets;
            _netClient = netClient;

            DefaultPacketBidings();
        }

        public OutgoingPackets Out => _outgoingPackets;

        public void AddHandler(byte packetId, OnPacketBufferReader handler)
        {
            _packetHandler.Add(packetId, handler);
        }

        public void Append(Span<byte> data, bool fromPlugin)
        {
            _packetHandler.Append(data, fromPlugin);
        }

        public int ParsePackets(Span<byte> data)
        {
            return _packetHandler.ParsePackets(_netClient, data);
        }

        private void DefaultPacketBidings()
        {
            AddHandler(0x1B, _incomingPackets.EnterWorld);
            AddHandler(0x55, _incomingPackets.LoginComplete);
            AddHandler(0xBD, _incomingPackets.ClientVersion);
            AddHandler(0x03, _incomingPackets.ClientTalk);
            AddHandler(0x0B, _incomingPackets.Damage);
            AddHandler(0x11, _incomingPackets.CharacterStatus);
            AddHandler(0x15, _incomingPackets.FollowR);
            AddHandler(0x16, _incomingPackets.NewHealthbarUpdate);
            AddHandler(0x17, _incomingPackets.NewHealthbarUpdate);
            AddHandler(0x1A, _incomingPackets.UpdateItem);
            AddHandler(0x1C, _incomingPackets.Talk);
            AddHandler(0x1D, _incomingPackets.DeleteObject);
            AddHandler(0x20, _incomingPackets.UpdatePlayer);
            AddHandler(0x21, _incomingPackets.DenyWalk);
            AddHandler(0x22, _incomingPackets.ConfirmWalk);
            AddHandler(0x23, _incomingPackets.DragAnimation);
            AddHandler(0x24, _incomingPackets.OpenContainer);
            AddHandler(0x25, _incomingPackets.UpdateContainedItem);
            AddHandler(0x27, _incomingPackets.DenyMoveItem);
            AddHandler(0x28, _incomingPackets.EndDraggingItem);
            AddHandler(0x29, _incomingPackets.DropItemAccepted);
            AddHandler(0x2C, _incomingPackets.DeathScreen);
            AddHandler(0x2D, _incomingPackets.MobileAttributes);
            AddHandler(0x2E, _incomingPackets.EquipItem);
            AddHandler(0x2F, _incomingPackets.Swing);
            AddHandler(0x32, _incomingPackets.Unknown_0x32);
            AddHandler(0x38, _incomingPackets.Pathfinding);
            AddHandler(0x3A, _incomingPackets.UpdateSkills);
            AddHandler(0x3B, _incomingPackets.CloseVendorInterface);
            AddHandler(0x3C, _incomingPackets.UpdateContainedItems);
            AddHandler(0x4E, _incomingPackets.PersonalLightLevel);
            AddHandler(0x4F, _incomingPackets.LightLevel);
            AddHandler(0x54, _incomingPackets.PlaySoundEffect);
            AddHandler(0x56, _incomingPackets.MapData);
            AddHandler(0x5B, _incomingPackets.SetTime);
            AddHandler(0x65, _incomingPackets.SetWeather);
            AddHandler(0x66, _incomingPackets.BookData);
            AddHandler(0x6C, _incomingPackets.TargetCursor);
            AddHandler(0x6D, _incomingPackets.PlayMusic);
            AddHandler(0x6F, _incomingPackets.SecureTrading);
            AddHandler(0x6E, _incomingPackets.CharacterAnimation);
            AddHandler(0x70, _incomingPackets.GraphicEffect);
            AddHandler(0x71, _incomingPackets.BulletinBoardData);
            AddHandler(0x72, _incomingPackets.Warmode);
            AddHandler(0x73, _incomingPackets.Ping);
            AddHandler(0x74, _incomingPackets.BuyList);
            AddHandler(0x77, _incomingPackets.UpdateCharacter);
            AddHandler(0x78, _incomingPackets.UpdateObject);
            AddHandler(0x7C, _incomingPackets.OpenMenu);
            AddHandler(0x88, _incomingPackets.OpenPaperdoll);
            AddHandler(0x89, _incomingPackets.CorpseEquipment);
            AddHandler(0x90, _incomingPackets.DisplayMap);
            AddHandler(0x93, _incomingPackets.OpenBook);
            AddHandler(0x95, _incomingPackets.DyeData);
            AddHandler(0x97, _incomingPackets.MovePlayer);
            AddHandler(0x98, _incomingPackets.UpdateName);
            AddHandler(0x99, _incomingPackets.MultiPlacement);
            AddHandler(0x9A, _incomingPackets.ASCIIPrompt);
            AddHandler(0x9E, _incomingPackets.SellList);
            AddHandler(0xA1, _incomingPackets.UpdateHitpoints);
            AddHandler(0xA2, _incomingPackets.UpdateMana);
            AddHandler(0xA3, _incomingPackets.UpdateStamina);
            AddHandler(0xA5, _incomingPackets.OpenUrl);
            AddHandler(0xA6, _incomingPackets.TipWindow);
            AddHandler(0xAA, _incomingPackets.AttackCharacter);
            AddHandler(0xAB, _incomingPackets.TextEntryDialog);
            AddHandler(0xAF, _incomingPackets.DisplayDeath);
            AddHandler(0xAE, _incomingPackets.UnicodeTalk);
            AddHandler(0xB0, _incomingPackets.OpenGump);
            AddHandler(0xB2, _incomingPackets.ChatMessage);
            AddHandler(0xB7, _incomingPackets.Help);
            AddHandler(0xB8, _incomingPackets.CharacterProfile);
            AddHandler(0xB9, _incomingPackets.EnableLockedFeatures);
            AddHandler(0xBA, _incomingPackets.DisplayQuestArrow);
            AddHandler(0xBB, _incomingPackets.UltimaMessengerR);
            AddHandler(0xBC, _incomingPackets.Season);
            AddHandler(0xBE, _incomingPackets.AssistVersion);
            AddHandler(0xBF, _incomingPackets.ExtendedCommand);
            AddHandler(0xC0, _incomingPackets.GraphicEffect);
            AddHandler(0xC1, _incomingPackets.DisplayClilocString);
            AddHandler(0xC2, _incomingPackets.UnicodePrompt);
            AddHandler(0xC4, _incomingPackets.Semivisible);
            AddHandler(0xC6, _incomingPackets.InvalidMapEnable);
            AddHandler(0xC7, _incomingPackets.GraphicEffect);
            AddHandler(0xC8, _incomingPackets.ClientViewRange);
            AddHandler(0xCA, _incomingPackets.GetUserServerPingGodClientR);
            AddHandler(0xCB, _incomingPackets.GlobalQueCount);
            AddHandler(0xCC, _incomingPackets.DisplayClilocString);
            AddHandler(0xD0, _incomingPackets.ConfigurationFileR);
            AddHandler(0xD1, _incomingPackets.Logout);
            AddHandler(0xD2, _incomingPackets.UpdateCharacter);
            AddHandler(0xD3, _incomingPackets.UpdateObject);
            AddHandler(0xD4, _incomingPackets.OpenBook);
            AddHandler(0xD6, _incomingPackets.MegaCliloc);
            AddHandler(0xD7, _incomingPackets.GenericAOSCommandsR);
            AddHandler(0xD8, _incomingPackets.CustomHouse);
            AddHandler(0xDB, _incomingPackets.CharacterTransferLog);
            AddHandler(0xDC, _incomingPackets.OPLInfo);
            AddHandler(0xDD, _incomingPackets.OpenCompressedGump);
            AddHandler(0xDE, _incomingPackets.UpdateMobileStatus);
            AddHandler(0xDF, _incomingPackets.BuffDebuff);
            AddHandler(0xE2, _incomingPackets.NewCharacterAnimation);
            AddHandler(0xE3, _incomingPackets.KREncryptionResponse);
            AddHandler(0xE5, _incomingPackets.DisplayWaypoint);
            AddHandler(0xE6, _incomingPackets.RemoveWaypoint);
            AddHandler(0xF0, _incomingPackets.KrriosClientSpecial);
            AddHandler(0xF1, _incomingPackets.FreeshardListR);
            AddHandler(0xF3, _incomingPackets.UpdateItemSA);
            AddHandler(0xF5, _incomingPackets.DisplayMap);
            AddHandler(0xF6, _incomingPackets.BoatMoving);
            AddHandler(0xF7, _incomingPackets.PacketList);

            AddHandler(0xA8, _incomingPackets.ServerListReceived);
            AddHandler(0x8C, _incomingPackets.ReceiveServerRelay);
            AddHandler(0x86, _incomingPackets.UpdateCharacterList);
            AddHandler(0xA9, _incomingPackets.ReceiveCharacterList);
            AddHandler(0x82, _incomingPackets.ReceiveLoginRejection);
            AddHandler(0x85, _incomingPackets.ReceiveLoginRejection);
            AddHandler(0x53, _incomingPackets.ReceiveLoginRejection);
            AddHandler(0xFD, _incomingPackets.LoginDelay);
        }
    }
}