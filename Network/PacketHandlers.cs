using ClassicUO.Assets;
using ClassicUO.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO.Network
{
    public class PacketHandler
    {
        public PacketHandler(Action<Packet> callback)
        {
            Callback = callback;
        }

        public Action<Packet> Callback { get; }
    }

    public class PacketHandlers
    {
        public static PacketHandlers ToClient { get; private set; }
        public static PacketHandlers ToServer { get; private set; }

        static PacketHandlers()
        {
            ToClient = new PacketHandlers();
            ToServer = new PacketHandlers();

            NetClient.PacketReceived += ToClient.OnPacket;
            NetClient.PacketSended += ToServer.OnPacket;
        }

        private readonly List<PacketHandler>[] _handlers = new List<PacketHandler>[0x100];

        private PacketHandlers()
        {
            for (int i = 0; i < _handlers.Length; i++)
                _handlers[i] = new List<PacketHandler>();
        }

        public void Add(byte id, Action<Packet> handler)
        {
            lock (_handlers)
                _handlers[id].Add(new PacketHandler(handler));
        }

        public void Remove(byte id, Action<Packet> handler)
        {
            lock (_handlers)
                _handlers[id].Remove(_handlers[id].FirstOrDefault(s => s.Callback == handler));
        }

        private void OnPacket(object sender, Packet p)
        {
            lock (_handlers)
            {
                for (int i = 0; i < _handlers[p.ID].Count; i++)
                {
                    p.MoveToData();
                    _handlers[p.ID][i].Callback(p);
                }
            }
        }





        public static void Load()
        {
            /*ToServer.Add(0x00, CreateCharacter);
            ToServer.Add(0x01, Disconnect);
            ToServer.Add(0x02, MoveRequest);
            ToServer.Add(0x03, TalkRequest);
            ToServer.Add(0x04, RequestGodMode);
            ToServer.Add(0x05, RequestAttack);
            ToServer.Add(0x06, DoubleClick);
            ToServer.Add(0x07, PickUpItem);
            ToServer.Add(0x08, DropItem);
            ToServer.Add(0x09, SingleClick);
            ToServer.Add(0x0A, EditGodMode);*/
            ToClient.Add(0x0B, Damage);
            ToClient.Add(0x0C, EditTileDataGodClientR); /*ToServer.Add(0x0C, EditTileDataGodClientS);*/
            ToClient.Add(0x11, StatusBarInfo);
           /* ToServer.Add(0x12, RequestSkill);
            ToServer.Add(0x13, WearItem);
            ToServer.Add(0x14, SendElevationGodClient);
            ToServer.Add(0x15 FollowS);*/ ToClient.Add(0x15, FollowR);
            ToClient.Add(0x16, NewHealthBarStatusUpdateSA);
            ToClient.Add(0x17, HealtBarStatusUpdateKR);
            ToClient.Add(0x1A, ObjectInfo);
            ToClient.Add(0x1B, CharLocaleAndBody);
            ToClient.Add(0x1C, SendSpeech);
            ToClient.Add(0x1D, DeleteObject);
            ToClient.Add(0x1F, Explosion);
            ToClient.Add(0x20, DrawGamePlayer);
            ToClient.Add(0x21, CharMoveRejection);
            ToClient.Add(0x22, CharacterMoveACK); /*ToServer.Add(0x22, ResyncRequest);*/
            ToClient.Add(0x23, DraggingOfItem);
            ToClient.Add(0x24, DrawContainer);
            ToClient.Add(0x25, AddItemToContainer);
            ToClient.Add(0x26, KickPlayer);
            ToClient.Add(0x27, RejectMoveItemRequest);
            ToClient.Add(0x28, DropItemFailed);
            ToClient.Add(0x29, DropItemApproved);
            ToClient.Add(0x2A, Blood);
            ToClient.Add(0x2B, GodMode);
            ToClient.Add(0x2D, MobAttributes);
            ToClient.Add(0x2E, WornItem);
            ToClient.Add(0x2F, FightOccuring);
            ToClient.Add(0x30, AttackOK);
            ToClient.Add(0x31, AttackEnded);
            ToClient.Add(0x32, (Packet p) => { }); // unknown
            ToClient.Add(0x33, PauseClient);
            /*ToServer.Add(0x34, GetPlayerStatus);
            ToServer.Add(0x35, AddResourceGodClient);*/
            ToClient.Add(0x36, ResourceTileDataGodClient);
            /*ToServer.Add(0x37, MoveItemGodClient);
            ToServer.Add(0x38, PathfindingInClient);
            ToServer.Add(0x39, RemoveGroupS);*/ ToClient.Add(0x39, RemoveGroupR);
            /*ToServer.Add(0x3A, SendSkills);*/ ToClient.Add(0x3A, ReceiveSkills);
            //ToServer.Add(0x3B, BuyItems);
            ToClient.Add(0x3C, AddMultipleItemsInContainer);
            ToClient.Add(0x3E, VersionGodClient);
            ToClient.Add(0x3F, UpdateStaticsGodClient);
            /*ToServer.Add(0x45, VersionOK);
            ToServer.Add(0x46, NewArtwork);
            ToServer.Add(0x47, NewTerrain);
            ToServer.Add(0x48, NewAnimation);
            ToServer.Add(0x49, NewHues);
            ToServer.Add(0x4A, DeleteArt);
            ToServer.Add(0x4B, CheckClientVersion);
            ToServer.Add(0x4C, ScriptNames);
            ToServer.Add(0x4D, EditScriptFile);*/
            ToClient.Add(0x4F, OverallLightLevel);
            /*ToServer.Add(0x50, BoardHeader);
            ToServer.Add(0x51, BoardMessage);
            ToServer.Add(0x52, BoardPostMessage);*/
            ToClient.Add(0x53, RejectCharacterLogon);
            ToClient.Add(0x54, PlaySoundEffect);
            ToClient.Add(0x55, LoginComplete);
            ToClient.Add(0x56, MapPacketTreauseCartographyR); //ToServer.Add(0x56, MapPacketTreauseCartographyS);
            /*ToServer.Add(0x57, UpdateRegions);
            ToServer.Add(0x58, AddRegion);
            ToServer.Add(0x59, NewContextFX);
            ToServer.Add(0x5A, UpdateContextFX);*/
            ToClient.Add(0x5B, Time);
            /*ToServer.Add(0x5C, RestartVersion);
            ToServer.Add(0x5D, LoginCharacter);
            ToServer.Add(0x5E, ServerListing);
            ToServer.Add(0x5F, ServerListAddEntry);
            ToServer.Add(0x60, ServerListRemoveEntry);
            ToServer.Add(0x61, RemoveStaticObject);
            ToServer.Add(0x62, MoveStaticObject);
            ToServer.Add(0x63, LoadArea);
            ToServer.Add(0x64, LoadAreaRequest);*/
            ToClient.Add(0x65, SetWeather);
            ToClient.Add(0x66, BookPagesR); //ToServer.Add(0x66, BookPagesS);
            //ToServer.Add(0x69, ChangeText);
            ToClient.Add(0x70, GraphicalEffect);
            ToClient.Add(0x71, BulletinBoardMessagesR); //ToServer.Add(0x71, BulletinBoardMessagesS);
            ToClient.Add(0x72, SetWarMode);// ToServer.Add(0x72, RequestWarMode);
            ToClient.Add(0x73, PingR); //ToServer.Add(0x73, PingS);
            ToClient.Add(0x74, OpenByuWindow);
            //ToServer.Add(0x75, RenameCharacter);
            ToClient.Add(0x76, NewSubServer);
            ToClient.Add(0x77, UpdatePlayer);
            ToClient.Add(0x78, DrawObject);
            ToClient.Add(0x7C, OpenDialogBox);
            /*ToServer.Add(0x7D, ResponseToDialogBox);
            ToServer.Add(0x80, LoginRequest);*/
            ToClient.Add(0x82, LoginDenied);
            //ToServer.Add(0x83, DeleteCharacter);
            ToClient.Add(0x86, ResendCharactersAfterDelete);
            ToClient.Add(0x88, OpenPaperdoll);
            ToClient.Add(0x89, CorpseClothing);
            ToClient.Add(0x8C, ConnectToGameServer);
            ToClient.Add(0x90, MapMessage);
            //ToServer.Add(0x91, GameServerLogin);
            ToClient.Add(0x93, BookHeaderOldR); //ToServer.Add(0x93, BookHeaderOldS);
            ToClient.Add(0x95, DyeWindowR); //ToServer.Add(0x95, DyeWindowS);
            ToClient.Add(0x97, MovePlayer);
            ToClient.Add(0x98, AllNames3DGameOnlyR); //ToServer.Add(0x98, AllNames3DGameOnlyS);
            ToClient.Add(0x99, GiveBoatAndHousePlacement); //ToServer.Add(0x99, RequestBoatAndHousePlacement);
            ToClient.Add(0x9A, ConsoleEntryPromptR); //ToServer.Add(0x9A, ConsoleEntryPromptS);
            //ToServer.Add(0x9B, RequestHelp);
            ToClient.Add(0x9C, RequestAssistance);
            ToClient.Add(0x9E, SellList);
            /*ToServer.Add(0x9F, SellListReply);
            ToServer.Add(0xA0, SelectServer);*/
            ToClient.Add(0xA1, UpdateCurrentHealht);
            ToClient.Add(0xA2, UpdateCurrentMana);
            ToClient.Add(0xA3, UpdateCurrentStamina);
            //ToServer.Add(0xA4, ClientSpy);
            ToClient.Add(0xA5, OpenWebBrowser);
            ToClient.Add(0xA6, NoticeWindow);
            //ToServer.Add(0xA7, RequestNoticeWindow);
            ToClient.Add(0xA8, GameServerList);
            ToClient.Add(0xA9, CharactersAndStartingLocations);
            ToClient.Add(0xAA, AllowRefuseAttack);
            ToClient.Add(0xAB, GumpTextEntryDialog);
            /*ToServer.Add(0xAC, GumpTextEntryDialogReply);
            ToServer.Add(0xAD, UnicodeAsciiSpeechRequest);*/
            ToClient.Add(0xAE, UnicodeSpeechMessage);
            ToClient.Add(0xB0, SendGumpMenuDialog);
            //ToServer.Add(0xB1, GumpMenuSelection);
            ToClient.Add(0xB2, ChatMessage);
            /*ToServer.Add(0xB3, ChatText);
            ToServer.Add(0xB5, OpenChatWindow);
            ToServer.Add(0xB6, SendHelpRequest);*/
            ToClient.Add(0xB7, Help);
            ToClient.Add(0xB8, CharProfile); //ToServer.Add(0xB8, RequestCharProfile);
            ToClient.Add(0xBA, QuestArrow);
            ToClient.Add(0xBB, UltimaMessengerR); //ToServer.Add(0xBB, UltimaMessengerS);
            ToClient.Add(0xBC, SeasonalInformation);
            ToClient.Add(0xBD, ClientVersionR); //ToServer.Add(0xBD, ClientVersionS);
            ToClient.Add(0xBE, AssistVersionR);// ToServer.Add(0xBE, AssistVersionS);
            ToClient.Add(0xBF, GeneralInformationPacketR); //ToServer.Add(0xBF, GeneralInformationPacketS);
            ToClient.Add(0xC0, GraphcialEffect);
            ToClient.Add(0xC1, ClilocMessage);
            ToClient.Add(0xC2, UnicodeTextEntryR); //ToServer.Add(0xC2, UnicodeTextEntryS);
            ToClient.Add(0xC4, Semivisible);
            //ToServer.Add(0xC5, InvalidMapRequest);
            ToClient.Add(0xC6, InvalidMapEnable);
            ToClient.Add(0xC7, ParticleEffect3D);
            ToClient.Add(0xCA, GetUserServerPingGodClientR); //ToServer.Add(0xCA, GetUserServerPingGodClientS);
            ToClient.Add(0xCB, GlobalQueCount);
            ToClient.Add(0xCC, ClilocMessageAffix);
            ToClient.Add(0xD0, ConfigurationFileR); //ToServer.Add(0xD0, ConfigurationFileS);
            ToClient.Add(0xD1, LogoutStatusR); //ToServer.Add(0xD1, LogoutStatusS);
            ToClient.Add(0xD2, Extended0x20);
            ToClient.Add(0xD3, Extended0x78);
            ToClient.Add(0xD4, BookHeaderNewR); //ToServer.Add(0xD4, BookHeaderNewS);
            ToClient.Add(0xD6, MegaClilocR); //ToServer.Add(0xD6, MegaClilocS);
            ToClient.Add(0xD7, GenericAOSCommandsR); //ToServer.Add(0xD7, GenericAOSCommandsS);
            ToClient.Add(0xD8, SendCustomHouse);
            //ToServer.Add(0xD9, SpyOnClient);
            ToClient.Add(0xDB, CharacterTransferLog);
            ToClient.Add(0xDC, SEIntroducedRevision);
            ToClient.Add(0xDD, CompressedGump);
            ToClient.Add(0xDE, UpdateMobileStatus);
            ToClient.Add(0xDF, BuffDebuffSystem);
            /*ToServer.Add(0xE0, BugReportKR);
            ToServer.Add(0xE1, ClientTypeKRSA);*/
            ToClient.Add(0xE2, NewCharacterAnimationKR);
            ToClient.Add(0xE3, KREncryptionResponse);
            /*ToServer.Add(0xEC, EquipMacroKR);
            ToServer.Add(0xED, UnequipItemMacroKR);
            ToServer.Add(0xEF, KR2DClientLoginSeed);*/
            ToClient.Add(0xF0, KrriosClientSpecial);
            ToClient.Add(0xF1, FreeshardListR); //ToServer.Add(0xF1, FreeshardListS);
            ToClient.Add(0xF3, ObjectInformationSA);
            ToClient.Add(0xF5, NewMapMessage);
            //ToServer.Add(0xF8, CharacterCreation_7_0_16_0);
        }

        private static void Damage(Packet p)
        {
            
        }

        private static void EditTileDataGodClientR(Packet p)
        {
            
        }

        private static void StatusBarInfo(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null)
                return;

            mobile.Name = p.ReadASCII(30);
            mobile.Hits = p.ReadUShort();
            mobile.HitsMax = p.ReadUShort();
            mobile.Renamable = p.ReadBool();

            byte type = p.ReadByte();
            if (type > 0)
            {

                World.Player.Female = p.ReadBool();
                World.Player.Strength = p.ReadUShort();
                World.Player.Dexterity = p.ReadUShort();
                World.Player.Intelligence = p.ReadUShort();
                World.Player.Stamina = p.ReadUShort();
                //if (Player.IsDead)
                //{
                //    p.Seek(p.Position -2);
                //    p.WriteUShort(Player.StaminaMax);
                //}
                World.Player.StaminaMax = p.ReadUShort();
                World.Player.Mana = p.ReadUShort();
                World.Player.ManaMax = p.ReadUShort();
                World.Player.Gold = p.ReadUInt();
                World.Player.ResistPhysical = p.ReadUShort();
                World.Player.Weight = p.ReadUShort();
            }

            if (type >= 5)//ML
            {
                World.Player.WeightMax = p.ReadUShort();
                p.Skip(1);
            }

            if (type >= 2)//T2A
                p.Skip(2);

            if (type >= 3)//Renaissance
            {
                World.Player.Followers = p.ReadByte();
                World.Player.FollowersMax = p.ReadByte();
            }

            if (type >= 4)//AOS
            {
                World.Player.ResistFire = p.ReadUShort();
                World.Player.ResistCold = p.ReadUShort();
                World.Player.ResistPoison = p.ReadUShort();
                World.Player.ResistEnergy = p.ReadUShort();
                World.Player.Luck = p.ReadUShort();
                World.Player.DamageMin = p.ReadUShort();
                World.Player.DamageMax = p.ReadUShort();
                World.Player.TithingPoints = p.ReadUInt();
            }

            mobile.ProcessDelta();

        }

        private static void FollowR(Packet p)
        {
            
        }

        private static void NewHealthBarStatusUpdateSA(Packet p)
        {
            
        }

        private static void HealtBarStatusUpdateKR(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null)
                return;

            ushort count = p.ReadUShort();
            for (int i = 0; i < count; i++)
            {
                ushort type = p.ReadUShort();
                bool enabled = p.ReadBool();
                byte flags = (byte)mobile.Flags;

                if (type == 1)
                {
                    if (FileManager.ClientVersion >= ClientVersions.CV_7000)
                        mobile.SetSAPoison(true);
                    else
                        flags |= 0x04;
                }
                else if (type == 2)
                {
                    if (FileManager.ClientVersion >= ClientVersions.CV_7000)
                        mobile.SetSAPoison(false);
                    else
                        flags &= 0x04;
                }
                else if (type == 3)
                {
                    if (enabled)
                        flags |= 0x08;
                    else
                        flags &= 0x08;
                }

                mobile.Flags = (Flags)flags;
            }
            mobile.ProcessDelta();

        }

        private static void ObjectInfo(Packet p)
        {
            uint serial = p.ReadUInt();
            Item item = World.GetOrCreateItem(serial & 0x7FFFFFFF);

            ushort graphic = (ushort)(p.ReadUShort() & 0x3FFF);
            item.Amount = (serial & 0x80000000) != 0 ? p.ReadUShort() : (ushort)1;

            if ((graphic & 0x8000) != 0)
                item.Graphic = (ushort)(graphic & 0x7FFF + p.ReadSByte());
            else
                item.Graphic = (ushort)(graphic & 0x7FFF);

            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();

            if ((x & 0x8000) != 0)
                item.Direction = (Direction)p.ReadByte();//wtf???

            item.Position = new Position((ushort)(x & 0x7FFF), (ushort)(y & 0x3FFF), p.ReadSByte());

            if ((y & 0x8000) != 0)
                item.Hue = p.ReadUShort();

            if ((y & 0x4000) != 0)
                item.Flags = (Flags)p.ReadByte();

            item.Container = Serial.Invalid;
            item.ProcessDelta();
            if (World.Items.Add(item))
                World.Items.ProcessDelta();
        }

        private static void CharLocaleAndBody(Packet p)
        {
            World.Mobiles.Add(World.Player = new PlayerMobile(p.ReadUInt()));
            p.Skip(4);
            World.Player.Graphic = p.ReadUShort();
            World.Player.Position = new Position(p.ReadUShort(), p.ReadUShort(), (sbyte)p.ReadUShort());
            World.Player.Direction = (Direction)p.ReadByte();
            World.Player.ProcessDelta();
            World.Mobiles.ProcessDelta();
        }

        private static void SendSpeech(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Mobiles.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType)p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont)p.ReadUShort();
            string name = p.ReadASCII(30);
            string text = p.ReadASCII();

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

        }

        private static void DeleteObject(Packet p)
        {
            Serial serial = p.ReadUInt();
            if (serial.IsItem)
            {
                if (World.RemoveItem(serial))
                    World.Items.ProcessDelta();
            }
            else if (serial.IsMobile && World.RemoveMobile(serial))
            {
                World.Items.ProcessDelta();
                World.Mobiles.ProcessDelta();
            }
        }

        private static void Explosion(Packet p)
        {
            
        }

        private static void DrawGamePlayer(Packet p)
        {
            if (p.ReadUInt() != World.Player)
                throw new Exception("OnMobileStatus");
            //World.MovementsQueue.Clear();
            World.Player.Graphic = (ushort)(p.ReadUShort() + p.ReadSByte());
            World.Player.Hue = p.ReadUShort();
            World.Player.Flags = (Flags)p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            p.Skip(2);
            World.Player.Direction = (Direction)p.ReadByte();
            World.Player.Position = new Position(x, y, p.ReadSByte());
            //OnPlayerMoved();
            World.Player.ProcessDelta();

        }

        private static void CharMoveRejection(Packet p)
        {
            p.Skip(1);
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            World.Player.Direction = (Direction)p.ReadByte();
            World.Player.Position = new Position(x, y, p.ReadSByte());
            World.Player.ProcessDelta();
        }

        private static void CharacterMoveACK(Packet p)
        {
            p.Skip(1);
            World.Player.Notoriety = (Notoriety)p.ReadByte();

            World.Player.ProcessDelta();
        }

        private static void DraggingOfItem(Packet p)
        {
            
        }

        private static void DrawContainer(Packet p)
        {
            
        }

        private static void AddItemToContainer(Packet p)
        {
            if (ReadContainerContent(p))
                World.Items.ProcessDelta();
        }

        private static void KickPlayer(Packet p)
        {
            
        }

        private static void RejectMoveItemRequest(Packet p)
        {
            
        }

        private static void DropItemFailed(Packet p)
        {
            
        }

        private static void DropItemApproved(Packet p)
        {
            
        }

        private static void Blood(Packet p)
        {
            
        }

        private static void GodMode(Packet p)
        {
            
        }

        private static void MobAttributes(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null)
                return;
            mobile.HitsMax = p.ReadUShort();
            mobile.Hits = p.ReadUShort();
            mobile.ManaMax = p.ReadUShort();
            mobile.Mana = p.ReadUShort();
            mobile.StaminaMax = p.ReadUShort();
            mobile.Stamina = p.ReadUShort();
            mobile.ProcessDelta();

        }

        private static void WornItem(Packet p)
        {
            Item item = World.GetOrCreateItem(p.ReadUInt());
            item.Graphic = (ushort)(p.ReadUShort() + p.ReadSByte());
            item.Layer = (Layer)p.ReadByte();
            item.Container = p.ReadUInt();
            item.Hue = p.ReadUShort();
            item.Amount = 1;

            Mobile mobile = World.Mobiles.Get(item.Container);
            mobile?.Items.Add(item);
            item.ProcessDelta();
            if (World.Items.Add(item))
                World.Items.ProcessDelta();
            mobile?.ProcessDelta();

            if (mobile == World.Player)
                World.Player.UpdateAbilities();

        }

        private static void FightOccuring(Packet p)
        {
            
        }

        private static void AttackOK(Packet p)
        {
            
        }

        private static void AttackEnded(Packet p)
        {
            
        }

        private static void ReceiveSkills(Packet p)
        {
            ushort id;
            switch (p.ReadByte())
            {
                case 0:
                    while ((id = p.ReadUShort()) > 0)
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (SkillLock)p.ReadByte(), 100);
                    break;

                case 2:
                    while ((id = p.ReadUShort()) > 0)
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (SkillLock)p.ReadByte(), p.ReadUShort());
                    break;

                case 0xDF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (SkillLock)p.ReadByte(), p.ReadUShort());
                    break;

                case 0xFF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (SkillLock)p.ReadByte(), 100);
                    break;
            }
            World.Player.ProcessDelta();

        }

        private static void RemoveGroupR(Packet p)
        {
            
        }

        private static void PauseClient(Packet p)
        {
            
        }

        private static void ResourceTileDataGodClient(Packet p)
        {
            
        }

        private static void AddMultipleItemsInContainer(Packet p)
        {
            ushort count = p.ReadUShort();
            for (int i = 0; i < count; i++)
                ReadContainerContent(p);
            World.Items.ProcessDelta();
        }

        private static void VersionGodClient(Packet p)
        {
            
        }

        private static void UpdateStaticsGodClient(Packet p)
        {
            
        }

        private static void OverallLightLevel(Packet p)
        {
            
        }

        private static void RejectCharacterLogon(Packet p)
        {
            
        }

        private static void PlaySoundEffect(Packet p)
        {
            
        }

        private static void LoginComplete(Packet p)
        {
            
        }

        private static void MapPacketTreauseCartographyR(Packet p)
        {
            
        }

        private static void Time(Packet p)
        {
            
        }

        private static void SetWeather(Packet p)
        {
            
        }

        private static void BookPagesR(Packet p)
        {
            
        }

        private static void GraphicalEffect(Packet p)
        {
            
        }

        private static void BulletinBoardMessagesR(Packet p)
        {
            
        }

        private static void SetWarMode(Packet p)
        {
            World.Player.WarMode = p.ReadBool();
            World.Player.ProcessDelta();
        }

        private static void PingR(Packet p)
        {
            
        }

        private static void OpenByuWindow(Packet p)
        {
            
        }

        private static void NewSubServer(Packet p)
        {
            
        }

        private static void UpdatePlayer(Packet p)
        {
            Mobile mobile = World.GetOrCreateMobile(p.ReadUInt());
            mobile.Graphic = p.ReadUShort();

            mobile.Position = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            mobile.Direction = (Direction)p.ReadByte();
            mobile.Hue = p.ReadUShort();
            mobile.Flags = (Flags)p.ReadByte();
            mobile.Notoriety = (Notoriety)p.ReadByte();
            mobile.ProcessDelta();
            if (World.Mobiles.Add(mobile))
                World.Mobiles.ProcessDelta();

        }

        private static void DrawObject(Packet p)
        {
            Mobile mobile = World.GetOrCreateMobile(p.ReadUInt());
            mobile.Graphic = p.ReadUShort();
            mobile.Position = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            mobile.Direction = (Direction)p.ReadByte();
            mobile.Hue = p.ReadUShort();
            mobile.Flags = (Flags)p.ReadByte();
            mobile.Notoriety = (Notoriety)p.ReadByte();

            uint itemSerial;
            while ((itemSerial = p.ReadUInt()) != 0)
            {
                Item item = World.GetOrCreateItem(itemSerial);
                ushort graphic = p.ReadUShort();
                item.Layer = (Layer)p.ReadByte();
                if (FileManager.ClientVersion >= ClientVersions.CV_70331 || (graphic & 0x8000) != 0)
                    item.Hue = p.ReadUShort();

                if (FileManager.ClientVersion >= ClientVersions.CV_70331)
                    item.Graphic = graphic;
                else if (FileManager.ClientVersion >= ClientVersions.CV_7000)
                    item.Graphic = (ushort)(graphic & 0x7FFF);
                else
                    item.Graphic = (ushort)(graphic & 0x3FFF);

                item.Amount = 1;
                item.Container = mobile;
                mobile.Items.Add(item);
                item.ProcessDelta();
                World.Items.Add(item);
            }
            mobile.ProcessDelta();
            if (World.Mobiles.Add(mobile))
                World.Mobiles.ProcessDelta();
            World.Items.ProcessDelta();

        }

        private static void OpenDialogBox(Packet p)
        {
            
        }

        private static void LoginDenied(Packet p)
        {
            
        }

        private static void ResendCharactersAfterDelete(Packet p)
        {
            
        }

        private static void OpenPaperdoll(Packet p)
        {
            
        }

        private static void CorpseClothing(Packet p)
        {
            
        }

        private static void ConnectToGameServer(Packet p)
        {
            
        }

        private static void MapMessage(Packet p)
        {
            
        }

        private static void BookHeaderOldR(Packet p)
        {
            
        }

        private static void DyeWindowR(Packet p)
        {
            
        }

        private static void MovePlayer(Packet p)
        {
            Direction direction = (Direction)p.ReadByte();
            World.Player.ProcessDelta();
        }

        private static void AllNames3DGameOnlyR(Packet p)
        {
            
        }

        private static void GiveBoatAndHousePlacement(Packet p)
        {
            
        }

        private static void ConsoleEntryPromptR(Packet p)
        {
            
        }

        private static void RequestAssistance(Packet p)
        {
            
        }

        private static void SellList(Packet p)
        {
            
        }

        private static void UpdateCurrentHealht(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null)
                return;
            mobile.HitsMax = p.ReadUShort();
            mobile.Hits = p.ReadUShort();
            mobile.ProcessDelta();

        }

        private static void UpdateCurrentMana(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null)
                return;
            mobile.ManaMax = p.ReadUShort();
            mobile.Mana = p.ReadUShort();
            mobile.ProcessDelta();

        }

        private static void UpdateCurrentStamina(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null)
                return;
            mobile.StaminaMax = p.ReadUShort();
            mobile.Stamina = p.ReadUShort();
            mobile.ProcessDelta();
        }

        private static void OpenWebBrowser(Packet p)
        {
            
        }

        private static void NoticeWindow(Packet p)
        {
            
        }

        private static void GameServerList(Packet p)
        {
            
        }

        private static void CharactersAndStartingLocations(Packet p)
        {
            
        }

        private static void AllowRefuseAttack(Packet p)
        {
            
        }

        private static void GumpTextEntryDialog(Packet p)
        {
            
        }

        private static void UnicodeSpeechMessage(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Mobiles.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType)p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont)p.ReadUShort();
            string lang = p.ReadASCII(4);
            string name = p.ReadASCII(30);
            string text = p.ReadUnicode();

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

        }

        private static void SendGumpMenuDialog(Packet p)
        {
            
        }

        private static void ChatMessage(Packet p)
        {
            
        }

        private static void Help(Packet p)
        {
            
        }

        private static void CharProfile(Packet p)
        {
            
        }

        private static void QuestArrow(Packet p)
        {
            
        }

        private static void UltimaMessengerR(Packet p)
        {
            
        }

        private static void SeasonalInformation(Packet p)
        {
            
        }

        private static void ClientVersionR(Packet p)
        {
            
        }

        private static void AssistVersionR(Packet p)
        {
            
        }

        private static void GeneralInformationPacketR(Packet p)
        {
            switch (p.ReadUShort())
            {
                case 6: //party
                    break;
                case 8: // map change
                    break;
            }

        }

        private static void GraphcialEffect(Packet p)
        {
            
        }

        private static void ClilocMessage(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Mobiles.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType)p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont)p.ReadUShort();
            uint cliloc = p.ReadUInt();
            string name = p.ReadASCII(30);
            string text = Cliloc.GetString((int)cliloc);

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

        }

        private static void UnicodeTextEntryR(Packet p)
        {
            
        }

        private static void Semivisible(Packet p)
        {
            
        }

        private static void InvalidMapEnable(Packet p)
        {
            
        }

        private static void ParticleEffect3D(Packet p)
        {

        }

        private static void GetUserServerPingGodClientR(Packet p)
        {
            
        }

        private static void GlobalQueCount(Packet p)
        {
            
        }

        private static void ClilocMessageAffix(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Mobiles.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType)p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont)p.ReadUShort();
            uint cliloc = p.ReadUInt();
            AffixType affixType = (AffixType)p.ReadByte();
            string name = p.ReadASCII(30);
            string affix = p.ReadASCII();
            string text = p.ReadUnicode();

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

        }

        private static void ConfigurationFileR(Packet p)
        {
            
        }

        private static void LogoutStatusR(Packet p)
        {
            
        }

        private static void Extended0x20(Packet p)
        {
            
        }

        private static void Extended0x78(Packet p)
        {
            
        }

        private static void BookHeaderNewR(Packet p)
        {
            
        }

        private static void MegaClilocR(Packet p)
        {
            p.Skip(2);
            Entity entity = World.Get(p.ReadUInt());
            if (entity == null)
                return;
            p.Skip(6);
            entity.UpdateProperties(ReadProperties(p));
            entity.ProcessDelta();

        }

        private static void GenericAOSCommandsR(Packet p)
        {
            
        }

        private static void SendCustomHouse(Packet p)
        {
            
        }

        private static void CharacterTransferLog(Packet p)
        {
            
        }

        private static void SEIntroducedRevision(Packet p)
        {
            
        }

        private static void CompressedGump(Packet p)
        {
            Serial sender = p.ReadUInt();
            Serial gumpID = p.ReadUInt();
            uint x = p.ReadUInt();
            uint y = p.ReadUInt();
            uint clen = p.ReadUInt() - 4;
            uint dlen = p.ReadUInt();

            byte[] data = new byte[clen];
            for (int i = 0; i < data.Length; i++)
                data[i] = p.ReadByte();

            byte[] decData = new byte[dlen];

            Zlib.Decompress(data, 0, decData, (int)dlen);

            string layout = Encoding.UTF8.GetString(decData);

            uint linesNum = p.ReadUInt();
            if (linesNum > 0)
            {
                uint clineLength = p.ReadUInt() - 4;
                uint dlineLength = p.ReadUInt();

                byte[] linesdata = new byte[clineLength];
                for (int i = 0; i < linesdata.Length; i++)
                    linesdata[i] = p.ReadByte();

                /*Packet dp = GetDecompressedData(linesdata, (int)dlineLength);
               
                for (int i = 0; i < linesNum; i++)
                {
                    ushort len = dp.ReadUShort();
                    string text = dp.ReadUnicode(len);
                }*/
            }


        }

        private static void UpdateMobileStatus(Packet p)
        {
            
        }

        private static void BuffDebuffSystem(Packet p)
        {
            
        }

        private static void NewCharacterAnimationKR(Packet p)
        {
            
        }

        private static void KREncryptionResponse(Packet p)
        {
            
        }

        private static void KrriosClientSpecial(Packet p)
        {
            
        }

        private static void FreeshardListR(Packet p)
        {
            
        }

        private static void ObjectInformationSA(Packet p)
        {
            p.Skip(2);//unknown

            byte type = p.ReadByte();
            Item item = World.GetOrCreateItem(p.ReadUInt());

            ushort g = p.ReadUShort();
            if (type == 2)
                g |= 0x4000;
            item.Graphic = g;
            item.Direction = (Direction)p.ReadByte();

            item.Amount = p.ReadUShort();
            p.Skip(2);//amount again? wtf???

            item.Position = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            p.Skip(1);//light? wtf?

            item.Hue = p.ReadUShort();
            item.Flags = (Flags)p.ReadByte();

            if (FileManager.ClientVersion >= ClientVersions.CV_7090)
                p.ReadUShort();//unknown

            item.Container = Serial.Invalid;
            item.ProcessDelta();
            if (World.Items.Add(item))
                World.Items.ProcessDelta();

        }

        private static void NewMapMessage(Packet p)
        {
            
        }


        private static bool ReadContainerContent(Packet p)
        {
            Item item = World.GetOrCreateItem(p.ReadUInt());
            item.Graphic = (ushort)(p.ReadUShort() + p.ReadSByte());
            item.Amount = Math.Max(p.ReadUShort(), (ushort)1);
            item.Position = new Position(p.ReadUShort(), p.ReadUShort());
            if (FileManager.ClientVersion >= ClientVersions.CV_6017)
                p.ReadByte(); //gridnumber - useless?

            item.Container = p.ReadUInt();
            item.Hue = p.ReadUShort();

            Item entity = World.Items.Get(item.Container);
            if (entity != null)
            {
                entity.Items.Add(item);
                foreach (Item i in World.ToAdd.Where(i => i.Container == item))
                {
                    item.Items.Add(i);
                    World.Items.Add(i);
                }
                World.ToAdd.ExceptWith(item.Items);
                item.ProcessDelta();
                entity.ProcessDelta();
                return World.Items.Add(item);
            }
            World.ToAdd.Add(item);
            item.ProcessDelta();
            return false;
        }

        private static IEnumerable<Property> ReadProperties(Packet p)
        {
            uint cliloc;
            while ((cliloc = p.ReadUInt()) != 0)
            {
                ushort len = p.ReadUShort();
                string str = p.ReadUnicodeReversed(len);
                yield return new Property(cliloc, str);
            }
        }

    }
}
