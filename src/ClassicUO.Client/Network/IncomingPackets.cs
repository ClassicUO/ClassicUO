using ClassicUO.Sdk.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Services;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Sdk.IO;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Sdk;
using ClassicUO.Platforms;


namespace ClassicUO.Network;

delegate void OnPacketBufferReader(ref StackDataReader p);

internal class IncomingPackets
{
    private readonly List<uint> _customHouseRequests = new List<uint>();
    private uint _requestedGridLoot;

    private readonly TextFileParser _parser = new TextFileParser(
        string.Empty,
        new[] { ' ' },
        new char[] { },
        new[] { '{', '}' }
    );
    private readonly TextFileParser _cmdparser = new TextFileParser(
        string.Empty,
        new[] { ' ', ',' },
        new char[] { },
        new[] { '@', '@' }
    );



    public void TargetCursor(ref StackDataReader p)
    {
        var managersService = ServiceProvider.Get<ManagersService>();

        managersService.TargetManager.SetTargeting(
            (CursorTarget)p.ReadUInt8(),
            p.ReadUInt32BE(),
            (TargetType)p.ReadUInt8()
        );

        if (managersService.Party.PartyHealTimer < Time.Ticks && managersService.Party.PartyHealTarget != 0)
        {
            managersService.TargetManager.Target(managersService.Party.PartyHealTarget);
            managersService.Party.PartyHealTimer = 0;
            managersService.Party.PartyHealTarget = 0;
        }
    }

    public void SecureTrading(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        byte type = p.ReadUInt8();
        uint serial = p.ReadUInt32BE();

        if (type == 0)
        {
            uint id1 = p.ReadUInt32BE();
            uint id2 = p.ReadUInt32BE();

            // standard client doesn't allow the trading system if one of the traders is invisible (=not sent by server)
            if (world.Get(id1) == null ||world.Get(id2) == null)
            {
                return;
            }

            bool hasName = p.ReadBool();
            string name = string.Empty;

            if (hasName && p.Position < p.Length)
            {
                name = p.ReadASCII();
            }

            ServiceProvider.Get<GuiService>().Add(new TradingGump(world, serial, name, id1, id2));
        }
        else if (type == 1)
        {
            ServiceProvider.Get<GuiService>().GetTradingGump(serial)?.Dispose();
        }
        else if (type == 2)
        {
            uint id1 = p.ReadUInt32BE();
            uint id2 = p.ReadUInt32BE();

            var trading = ServiceProvider.Get<GuiService>().GetTradingGump(serial);

            if (trading != null)
            {
                trading.ImAccepting = id1 != 0;
                trading.HeIsAccepting = id2 != 0;

                trading.RequestUpdateContents();
            }
        }
        else if (type == 3 || type == 4)
        {
            var trading = ServiceProvider.Get<GuiService>().GetTradingGump(serial);

            if (trading != null)
            {
                if (type == 4)
                {
                    trading.Gold = p.ReadUInt32BE();
                    trading.Platinum = p.ReadUInt32BE();
                }
                else
                {
                    trading.HisGold = p.ReadUInt32BE();
                    trading.HisPlatinum = p.ReadUInt32BE();
                }
            }
        }
    }

    public void ClientTalk(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        switch (p.ReadUInt8())
        {
            case 0x78:
                break;

            case 0x3C:
                break;

            case 0x25:
                break;

            case 0x2E:
                break;
        }
    }

    public void Damage(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        var entity = world.Get(p.ReadUInt32BE());

        if (entity != null)
        {
            ushort damage = p.ReadUInt16BE();

            if (damage > 0)
            {
                world.WorldTextManager.AddDamage(entity, damage);
            }
        }
    }

    public void CharacterStatus(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        var entity = world.Get(serial);

        if (entity == null)
        {
            return;
        }

        string oldName = entity.Name;
        entity.Name = p.ReadASCII(30);
        entity.Hits = p.ReadUInt16BE();
        entity.HitsMax = p.ReadUInt16BE();

        if (entity.HitsRequest == HitsRequestStatus.Pending)
        {
            entity.HitsRequest = HitsRequestStatus.Received;
        }

        if (SerialHelper.IsMobile(serial))
        {
            var mobile = entity as Mobile;

            if (mobile == null)
            {
                return;
            }

            mobile.IsRenamable = p.ReadBool();
            byte type = p.ReadUInt8();

            if (type > 0 && p.Position + 1 <= p.Length)
            {
                mobile.IsFemale = p.ReadBool();

                if (mobile == world.Player)
                {
                    if (
                        !string.IsNullOrEmpty(world.Player.Name) && oldName != world.Player.Name
                    )
                    {
                        ServiceProvider.Get<EngineService>().SetWindowTitle(world.Player.Name);
                    }

                    ushort str = p.ReadUInt16BE();
                    ushort dex = p.ReadUInt16BE();
                    ushort intell = p.ReadUInt16BE();
                    world.Player.Stamina = p.ReadUInt16BE();
                    world.Player.StaminaMax = p.ReadUInt16BE();
                    world.Player.Mana = p.ReadUInt16BE();
                    world.Player.ManaMax = p.ReadUInt16BE();
                    world.Player.Gold = p.ReadUInt32BE();
                    world.Player.PhysicalResistance = (short)p.ReadUInt16BE();
                    world.Player.Weight = p.ReadUInt16BE();

                    if (
                        world.Player.Strength != 0
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.ShowStatsChangedMessage
                    )
                    {
                        ushort currentStr = world.Player.Strength;
                        ushort currentDex = world.Player.Dexterity;
                        ushort currentInt = world.Player.Intelligence;

                        int deltaStr = str - currentStr;
                        int deltaDex = dex - currentDex;
                        int deltaInt = intell - currentInt;

                        if (deltaStr != 0)
                        {
                            GameActions.Print(
                                world,
                                string.Format(
                                    ResGeneral.Your0HasChangedBy1ItIsNow2,
                                    ResGeneral.Strength,
                                    deltaStr,
                                    str
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );
                        }

                        if (deltaDex != 0)
                        {
                            GameActions.Print(
                                world,
                                string.Format(
                                    ResGeneral.Your0HasChangedBy1ItIsNow2,
                                    ResGeneral.Dexterity,
                                    deltaDex,
                                    dex
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );
                        }

                        if (deltaInt != 0)
                        {
                            GameActions.Print(
                                world,
                                string.Format(
                                    ResGeneral.Your0HasChangedBy1ItIsNow2,
                                    ResGeneral.Intelligence,
                                    deltaInt,
                                    intell
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );
                        }
                    }

                    world.Player.Strength = str;
                    world.Player.Dexterity = dex;
                    world.Player.Intelligence = intell;

                    if (type >= 5) //ML
                    {
                        world.Player.WeightMax = p.ReadUInt16BE();
                        byte race = p.ReadUInt8();

                        if (race == 0)
                        {
                            race = 1;
                        }

                        world.Player.Race = (RaceType)race;
                    }
                    else
                    {
                        if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_500A)
                        {
                            world.Player.WeightMax = (ushort)(
                                7 * (world.Player.Strength >> 1) + 40
                            );
                        }
                        else
                        {
                            world.Player.WeightMax = (ushort)(world.Player.Strength * 4 + 25);
                        }
                    }

                    if (type >= 3) //Renaissance
                    {
                        world.Player.StatsCap = (short)p.ReadUInt16BE();
                        world.Player.Followers = p.ReadUInt8();
                        world.Player.FollowersMax = p.ReadUInt8();
                    }

                    if (type >= 4) //AOS
                    {
                        world.Player.FireResistance = (short)p.ReadUInt16BE();
                        world.Player.ColdResistance = (short)p.ReadUInt16BE();
                        world.Player.PoisonResistance = (short)p.ReadUInt16BE();
                        world.Player.EnergyResistance = (short)p.ReadUInt16BE();
                        world.Player.Luck = p.ReadUInt16BE();
                        world.Player.DamageMin = (short)p.ReadUInt16BE();
                        world.Player.DamageMax = (short)p.ReadUInt16BE();
                        world.Player.TithingPoints = p.ReadUInt32BE();
                    }

                    if (type >= 6)
                    {
                        world.Player.MaxPhysicResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxFireResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxColdResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxPoisonResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxEnergyResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.DefenseChanceIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxDefenseChanceIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.HitChanceIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.SwingSpeedIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.DamageIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.LowerReagentCost =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.SpellDamageIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.FasterCastRecovery =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.FasterCasting =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.LowerManaCost =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                    }
                }
            }

            if (mobile == world.Player)
            {
                var managersService = ServiceProvider.Get<ManagersService>();
                managersService.UoAssist.SignalHits();
                managersService.UoAssist.SignalStamina();
                managersService.UoAssist.SignalMana();
            }
        }
    }

    public void FollowR(ref StackDataReader p)
    {
        uint tofollow = p.ReadUInt32BE();
        uint isfollowing = p.ReadUInt32BE();
    }

    public void NewHealthbarUpdate(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        if (p[0] == 0x16 && ServiceProvider.Get<UOService>().Version < ClassicUO.Sdk.ClientVersion.CV_500A)
        {
            return;
        }

        var mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
        {
            return;
        }

        ushort count = p.ReadUInt16BE();

        for (int i = 0; i < count; i++)
        {
            ushort type = p.ReadUInt16BE();
            bool enabled = p.ReadBool();

            if (type == 1)
            {
                if (enabled)
                {
                    if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_7000)
                    {
                        mobile.SetSAPoison(true);
                    }
                    else
                    {
                        mobile.Flags |= Flags.Poisoned;
                    }
                }
                else
                {
                    if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_7000)
                    {
                        mobile.SetSAPoison(false);
                    }
                    else
                    {
                        mobile.Flags &= ~Flags.Poisoned;
                    }
                }
            }
            else if (type == 2)
            {
                if (enabled)
                {
                    mobile.Flags |= Flags.YellowBar;
                }
                else
                {
                    mobile.Flags &= ~Flags.YellowBar;
                }
            }
            else if (type == 3)
            {
                // ???
            }
        }
    }

    public void UpdateItem(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort count = 0;
        byte graphicInc = 0;
        byte direction = 0;
        ushort hue = 0;
        byte flags = 0;
        byte type = 0;

        if ((serial & 0x80000000) != 0)
        {
            serial &= 0x7FFFFFFF;
            count = 1;
        }

        ushort graphic = p.ReadUInt16BE();

        if ((graphic & 0x8000) != 0)
        {
            graphic &= 0x7FFF;
            graphicInc = p.ReadUInt8();
        }

        if (count > 0)
        {
            count = p.ReadUInt16BE();
        }
        else
        {
            count++;
        }

        ushort x = p.ReadUInt16BE();

        if ((x & 0x8000) != 0)
        {
            x &= 0x7FFF;
            direction = 1;
        }

        ushort y = p.ReadUInt16BE();

        if ((y & 0x8000) != 0)
        {
            y &= 0x7FFF;
            hue = 1;
        }

        if ((y & 0x4000) != 0)
        {
            y &= 0x3FFF;
            flags = 1;
        }

        if (direction != 0)
        {
            direction = p.ReadUInt8();
        }

        sbyte z = p.ReadInt8();

        if (hue != 0)
        {
            hue = p.ReadUInt16BE();
        }

        if (flags != 0)
        {
            flags = p.ReadUInt8();
        }

        //if (graphic != 0x2006)
        //    graphic += graphicInc;

        if (graphic >= 0x4000)
        {
            //graphic -= 0x4000;
            type = 2;
        }

        UpdateGameObject(
            world,
            serial,
            graphic,
            graphicInc,
            count,
            x,
            y,
            z,
            (Direction)direction,
            hue,
            (Flags)flags,
            count,
            type,
            1
        );
    }

    public void EnterWorld(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();

        world.CreatePlayer(serial);

        p.Skip(4);
        world.Player.Graphic = p.ReadUInt16BE();
        world.Player.CheckGraphicChange();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = (sbyte)p.ReadUInt16BE();

        if (world.Map == null)
        {
            world.MapIndex = 0;
        }

        world.Player.SetInWorldTile(x, y, z);
        world.Player.Direction = (Direction)(p.ReadUInt8() & 0x7);
        world.RangeSize.X = x;
        world.RangeSize.Y = y;

        if (
            ProfileManager.CurrentProfile != null
            && ProfileManager.CurrentProfile.UseCustomLightLevel
        )
        {
            world.Light.Overall =
                ProfileManager.CurrentProfile.LightLevelType == 1
                    ? Math.Min(world.Light.Overall, ProfileManager.CurrentProfile.LightLevel)
                    : ProfileManager.CurrentProfile.LightLevel;
        }

        ServiceProvider.Get<AudioService>().UpdateCurrentMusicVolume();

        if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_200)
        {
            if (ProfileManager.CurrentProfile != null)
            {
                ServiceProvider.Get<PacketHandlerService>().Out.Send_GameWindowSize(
                    (uint)ServiceProvider.Get<SceneService>().Bounds.Width,
                    (uint)ServiceProvider.Get<SceneService>().Bounds.Height
                );
            }

            ServiceProvider.Get<PacketHandlerService>().Out.Send_Language(Settings.GlobalSettings.Language);
        }

        ServiceProvider.Get<PacketHandlerService>().Out.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);

        GameActions.SingleClick(world, world.Player);
        ServiceProvider.Get<PacketHandlerService>().Out.Send_SkillsRequest(world.Player.Serial);

        if (world.Player.IsDead)
        {
            world.ChangeSeason(Game.Managers.Season.Desolation, 42);
        }

        if (
            ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_70796
            && ProfileManager.CurrentProfile != null
        )
        {
            ServiceProvider.Get<PacketHandlerService>().Out.Send_ShowPublicHouseContent(
                ProfileManager.CurrentProfile.ShowHouseContent
            );
        }

        ServiceProvider.Get<PacketHandlerService>().Out.Send_ToPlugins_AllSkills();
        ServiceProvider.Get<PacketHandlerService>().Out.Send_ToPlugins_AllSpells();
    }

    public void Talk(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();
        var entity = world.Get(serial);
        ushort graphic = p.ReadUInt16BE();
        MessageType type = (MessageType)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        ushort font = p.ReadUInt16BE();
        string name = p.ReadASCII(30);
        string text;

        if (p.Length > 44)
        {
            p.Seek(44);
            text = p.ReadASCII();
        }
        else
        {
            text = string.Empty;
        }

        if (
            serial == 0
            && graphic == 0
            && type == MessageType.Regular
            && font == 0xFFFF
            && hue == 0xFFFF
            && name.StartsWith("SYSTEM")
        )
        {
            ServiceProvider.Get<PacketHandlerService>().Out.Send_ACKTalk();

            return;
        }

        TextType text_type = TextType.SYSTEM;

        if (
            type == MessageType.System
            || serial == 0xFFFF_FFFF
            || serial == 0
            || name.ToLower() == "system" && entity == null
        )
        {
            // do nothing
        }
        else if (entity != null)
        {
            text_type = TextType.OBJECT;

            if (string.IsNullOrEmpty(entity.Name))
            {
                entity.Name = string.IsNullOrEmpty(name) ? text : name;
            }
        }

        ServiceProvider.Get<ManagersService>().MessageManager.HandleMessage(entity, text, name, hue, type, (byte)font, text_type);
    }

    public void DeleteObject(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();

        if (world.Player == serial)
        {
            return;
        }

        var entity = world.Get(serial);

        if (entity == null)
        {
            return;
        }

        bool updateAbilities = false;

        if (entity is Item it)
        {
            uint cont = it.Container & 0x7FFFFFFF;

            if (SerialHelper.IsValid(it.Container))
            {
                var top = world.Get(it.RootContainer);

                if (top != null)
                {
                    if (top == world.Player)
                    {
                        updateAbilities =
                            it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                        var tradeBoxItem = world.Player.GetSecureTradeBox();

                        if (tradeBoxItem != null)
                        {
                            ServiceProvider.Get<GuiService>().GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                        }
                    }
                }

                if (cont == world.Player && it.Layer == Layer.Invalid)
                {
                    ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Enabled = false;
                }

                if (it.Layer != Layer.Invalid)
                {
                    ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                }

                ServiceProvider.Get<GuiService>().GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                if (
                    top != null
                    && top.Graphic == 0x2006
                    && (
                        ProfileManager.CurrentProfile.GridLootType == 1
                        || ProfileManager.CurrentProfile.GridLootType == 2
                    )
                )
                {
                    ServiceProvider.Get<GuiService>().GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                }

                if (it.Graphic == 0x0EB0)
                {
                    ServiceProvider.Get<GuiService>().GetGump<BulletinBoardItem>(serial)?.Dispose();

                    var bbgump = ServiceProvider.Get<GuiService>().GetGump<BulletinBoardGump>();

                    if (bbgump != null)
                    {
                        bbgump.RemoveBulletinObject(serial);
                    }
                }
            }
        }

        if (ServiceProvider.Get<ManagersService>().CorpseManager.Exists(0, serial))
        {
            return;
        }

        if (entity is Mobile m)
        {
            if (ServiceProvider.Get<ManagersService>().Party.Contains(serial))
            {
                // m.RemoveFromTile();
            }

            // else
            {
                //BaseHealthBarGump bar = ServiceProvider.Get<UIService>().GetGump<BaseHealthBarGump>(serial);

                //if (bar == null)
                //{
                //    NetClient.Socket.Send(new PCloseStatusBarGump(serial));
                //}

                world.RemoveMobile(serial, true);
            }
        }
        else
        {
            Item item = (Item)entity;

            if (item.IsMulti)
            {
                ServiceProvider.Get<ManagersService>().HouseManager.Remove(serial);
            }

            var cont =world.Get(item.Container);

            if (cont != null)
            {
                cont.Remove(item);

                if (item.Layer != Layer.Invalid)
                {
                    ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                }
            }
            else if (item.IsMulti)
            {
                ServiceProvider.Get<GuiService>().GetGump<MiniMapGump>()?.RequestUpdateContents();
            }

            world.RemoveItem(serial, true);

            if (updateAbilities)
            {
                world.Player.UpdateAbilities();
            }
        }
    }

    public void UpdatePlayer(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();
        byte graphic_inc = p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        Flags flags = (Flags)p.ReadUInt8();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        ushort serverID = p.ReadUInt16BE();
        Direction direction = (Direction)p.ReadUInt8();
        sbyte z = p.ReadInt8();

        UpdatePlayer(world, serial, graphic, graphic_inc, hue, flags, x, y, z, serverID, direction);
    }

    public void DenyWalk(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        byte seq = p.ReadUInt8();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        Direction direction = (Direction)p.ReadUInt8();
        direction &= Direction.Up;
        sbyte z = p.ReadInt8();

        world.Player.Walker.DenyWalk(seq, x, y, z);
        world.Player.Direction = direction;

        ServiceProvider.Get<ManagersService>().Weather.Reset();
    }

    public void ConfirmWalk(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        byte seq = p.ReadUInt8();
        byte noto = (byte)(p.ReadUInt8() & ~0x40);

        if (noto == 0 || noto >= 8)
        {
            noto = 0x01;
        }

        world.Player.NotorietyFlag = (NotorietyFlag)noto;
        world.Player.Walker.ConfirmWalk(seq);

        world.Player.AddToTile();
    }

    public void DragAnimation(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        ushort graphic = p.ReadUInt16BE();
        graphic += p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        ushort count = p.ReadUInt16BE();
        uint source = p.ReadUInt32BE();
        ushort sourceX = p.ReadUInt16BE();
        ushort sourceY = p.ReadUInt16BE();
        sbyte sourceZ = p.ReadInt8();
        uint dest = p.ReadUInt32BE();
        ushort destX = p.ReadUInt16BE();
        ushort destY = p.ReadUInt16BE();
        sbyte destZ = p.ReadInt8();

        if (graphic == 0x0EED)
        {
            graphic = 0x0EEF;
        }
        else if (graphic == 0x0EEA)
        {
            graphic = 0x0EEC;
        }
        else if (graphic == 0x0EF0)
        {
            graphic = 0x0EF2;
        }

        var entity = world.Mobiles.Get(source);

        if (entity == null)
        {
            source = 0;
        }
        else
        {
            sourceX = entity.X;
            sourceY = entity.Y;
            sourceZ = entity.Z;
        }

        var destEntity = world.Mobiles.Get(dest);

        if (destEntity == null)
        {
            dest = 0;
        }
        else
        {
            destX = destEntity.X;
            destY = destEntity.Y;
            destZ = destEntity.Z;
        }

        world.SpawnEffect(
            !SerialHelper.IsValid(source) || !SerialHelper.IsValid(dest)
                ? GraphicEffectType.Moving
                : GraphicEffectType.DragEffect,
            source,
            dest,
            graphic,
            hue,
            sourceX,
            sourceY,
            sourceZ,
            destX,
            destY,
            destZ,
            5,
            5000,
            true,
            false,
            false,
            GraphicEffectBlendMode.Normal
        );

        //if (effect.AnimDataFrame.FrameCount != 0)
        //{
        //    effect.IntervalInMs = (uint) (effect.AnimDataFrame.FrameInterval * 45);
        //}
        //else
        //{
        //    effect.IntervalInMs = 13;
        //}
    }

    public void OpenContainer(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();

        if (graphic == 0xFFFF)
        {
            var spellBookItem = world.Items.Get(serial);

            if (spellBookItem == null)
            {
                return;
            }

            ServiceProvider.Get<GuiService>().GetGump<SpellbookGump>(serial)?.Dispose();

            SpellbookGump spellbookGump = new SpellbookGump(world, spellBookItem);

            if (!ServiceProvider.Get<GuiService>().GetGumpCachePosition(spellBookItem, out Point location))
            {
                location = new Point(64, 64);
            }

            spellbookGump.Location = location;
            ServiceProvider.Get<GuiService>().Add(spellbookGump);

            ServiceProvider.Get<AudioService>().PlaySound(0x0055);
        }
        else if (graphic == 0x0030)
        {
            var vendor = world.Mobiles.Get(serial);

            if (vendor == null)
            {
                return;
            }

            ServiceProvider.Get<GuiService>().GetGump<ShopGump>(serial)?.Dispose();

            ShopGump gump = new ShopGump(world, serial, true, 150, 5);
            ServiceProvider.Get<GuiService>().Add(gump);

            for (Layer layer = Layer.ShopBuyRestock; layer < Layer.ShopBuy + 1; layer++)
            {
                var item = vendor.FindItemByLayer(layer);
                if (item == null)
                    continue;

                var first = item.Items;

                if (first == null)
                {
                    //Log.Warn("buy item not found");
                    continue;
                }

                bool reverse = item.Graphic != 0x2AF8; //hardcoded logic in original client that we must match

                if (reverse)
                {
                    while (first?.Next != null)
                    {
                        first = first.Next;
                    }
                }

                while (first != null)
                {
                    Item it = (Item)first;

                    gump.AddItem(
                        it.Serial,
                        it.Graphic,
                        it.Hue,
                        it.Amount,
                        it.Price,
                        it.Name,
                        false
                    );

                    if (reverse)
                    {
                        first = first.Previous;
                    }
                    else
                    {
                        first = first.Next;
                    }
                }
            }
        }
        else
        {
            var item = world.Items.Get(serial);

            if (item != null)
            {
                if (
                    item.IsCorpse
                    && (
                        ProfileManager.CurrentProfile.GridLootType == 1
                        || ProfileManager.CurrentProfile.GridLootType == 2
                    )
                )
                {
                    //ServiceProvider.Get<UIService>().GetGump<GridLootGump>(serial)?.Dispose();
                    //ServiceProvider.Get<UIService>().Add(new GridLootGump(serial));
                    _requestedGridLoot = serial;

                    if (ProfileManager.CurrentProfile.GridLootType == 1)
                    {
                        return;
                    }
                }

                var container = ServiceProvider.Get<GuiService>().GetGump<ContainerGump>(serial);
                bool playsound = false;
                int x,
                    y;

                // TODO: check client version ?
                if (
                    ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_706000
                    && ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.UseLargeContainerGumps
                )
                {
                    var gumps = ServiceProvider.Get<UOService>().Self.Gumps;

                    switch (graphic)
                    {
                        case 0x0048:
                            if (gumps.GetGump(0x06E8).Texture != null)
                            {
                                graphic = 0x06E8;
                            }

                            break;

                        case 0x0049:
                            if (gumps.GetGump(0x9CDF).Texture != null)
                            {
                                graphic = 0x9CDF;
                            }

                            break;

                        case 0x0051:
                            if (gumps.GetGump(0x06E7).Texture != null)
                            {
                                graphic = 0x06E7;
                            }

                            break;

                        case 0x003E:
                            if (gumps.GetGump(0x06E9).Texture != null)
                            {
                                graphic = 0x06E9;
                            }

                            break;

                        case 0x004D:
                            if (gumps.GetGump(0x06EA).Texture != null)
                            {
                                graphic = 0x06EA;
                            }

                            break;

                        case 0x004E:
                            if (gumps.GetGump(0x06E6).Texture != null)
                            {
                                graphic = 0x06E6;
                            }

                            break;

                        case 0x004F:
                            if (gumps.GetGump(0x06E5).Texture != null)
                            {
                                graphic = 0x06E5;
                            }

                            break;

                        case 0x004A:
                            if (gumps.GetGump(0x9CDD).Texture != null)
                            {
                                graphic = 0x9CDD;
                            }

                            break;

                        case 0x0044:
                            if (gumps.GetGump(0x9CE3).Texture != null)
                            {
                                graphic = 0x9CE3;
                            }

                            break;
                    }
                }

                if (container != null)
                {
                    x = container.ScreenCoordinateX;
                    y = container.ScreenCoordinateY;
                    container.Dispose();
                }
                else
                {
                    ServiceProvider.Get<ManagersService>().ContainerManager.CalculateContainerPosition(serial, graphic);
                    x = ServiceProvider.Get<ManagersService>().ContainerManager.X;
                    y = ServiceProvider.Get<ManagersService>().ContainerManager.Y;
                    playsound = true;
                }

                ServiceProvider.Get<GuiService>().Add(
                    new ContainerGump(world, item, graphic, playsound)
                    {
                        X = x,
                        Y = y,
                        InvalidateContents = true
                    }
                );

                ServiceProvider.Get<GuiService>().RemovePosition(serial);
            }
            else
            {
                Log.Error("[OpenContainer]: item not found");
            }
        }

        if (graphic != 0x0030)
        {
            var it = world.Items.Get(serial);

            if (it != null)
            {
                it.Opened = true;

                if (!it.IsCorpse && graphic != 0xFFFF)
                {
                    ClearContainerAndRemoveItems(world, it);
                }
            }
        }
    }

    public void UpdateContainedItem(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort graphic = (ushort)(p.ReadUInt16BE() + p.ReadUInt8());
        ushort amount = Math.Max((ushort)1, p.ReadUInt16BE());
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();

        var uoService = ServiceProvider.Get<UOService>();
        if (uoService.Version >= ClassicUO.Sdk.ClientVersion.CV_6017)
        {
            p.Skip(1);
        }

        uint containerSerial = p.ReadUInt32BE();
        ushort hue = p.ReadUInt16BE();

        AddItemToContainer(world, serial, graphic, amount, x, y, hue, containerSerial);
    }

    public void DenyMoveItem(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        var uoService = ServiceProvider.Get<UOService>();
        var cursorService = ServiceProvider.Get<GameCursorService>();

        var firstItem = world.Items.Get(cursorService.GameCursor.ItemHold.Serial);

        if (
            cursorService.GameCursor.ItemHold.Enabled
            || cursorService.GameCursor.ItemHold.Dropped
                && (firstItem == null || !firstItem.AllowedToDraw)
        )
        {
            if (world.ObjectToRemove == cursorService.GameCursor.ItemHold.Serial)
            {
                world.ObjectToRemove = 0;
            }

            if (
                SerialHelper.IsValid(cursorService.GameCursor.ItemHold.Serial)
                && cursorService.GameCursor.ItemHold.Graphic != 0xFFFF
            )
            {
                if (!cursorService.GameCursor.ItemHold.UpdatedInWorld)
                {
                    if (
                        cursorService.GameCursor.ItemHold.Layer == Layer.Invalid
                        && SerialHelper.IsValid(cursorService.GameCursor.ItemHold.Container)
                    )
                    {
                        // Server should send an UpdateContainedItem after this packet.
                        Console.WriteLine("=== DENY === ADD TO CONTAINER");

                        AddItemToContainer(
                            world,
                            cursorService.GameCursor.ItemHold.Serial,
                            cursorService.GameCursor.ItemHold.Graphic,
                            cursorService.GameCursor.ItemHold.TotalAmount,
                            cursorService.GameCursor.ItemHold.X,
                            cursorService.GameCursor.ItemHold.Y,
                            cursorService.GameCursor.ItemHold.Hue,
                            cursorService.GameCursor.ItemHold.Container
                        );

                        ServiceProvider.Get<GuiService>()
                            .GetGump<ContainerGump>(cursorService.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                    else
                    {
                        Item item = world.GetOrCreateItem(
                            cursorService.GameCursor.ItemHold.Serial
                        );

                        item.Graphic = cursorService.GameCursor.ItemHold.Graphic;
                        item.Hue = cursorService.GameCursor.ItemHold.Hue;
                        item.Amount = cursorService.GameCursor.ItemHold.TotalAmount;
                        item.Flags = cursorService.GameCursor.ItemHold.Flags;
                        item.Layer = cursorService.GameCursor.ItemHold.Layer;
                        item.X = cursorService.GameCursor.ItemHold.X;
                        item.Y = cursorService.GameCursor.ItemHold.Y;
                        item.Z = cursorService.GameCursor.ItemHold.Z;
                        item.CheckGraphicChange();

                        var container = world.Get(cursorService.GameCursor.ItemHold.Container);

                        if (container != null)
                        {
                            if (SerialHelper.IsMobile(container.Serial))
                            {
                                Console.WriteLine("=== DENY === ADD TO PAPERDOLL");

                                world.RemoveItemFromContainer(item);
                                container.PushToBack(item);
                                item.Container = container.Serial;

                                ServiceProvider.Get<GuiService>()
                                    .GetGump<PaperDollGump>(item.Container)
                                    ?.RequestUpdateContents();
                            }
                            else
                            {
                                Console.WriteLine("=== DENY === SOMETHING WRONG");

                                world.RemoveItem(item, true);
                            }
                        }
                        else
                        {
                            Console.WriteLine("=== DENY === ADD TO TERRAIN");

                            world.RemoveItemFromContainer(item);

                            item.SetInWorldTile(item.X, item.Y, item.Z);
                        }
                    }
                }
            }
            else
            {
                Log.Error(
                    $"Wrong data: serial = {cursorService.GameCursor.ItemHold.Serial:X8}  -  graphic = {cursorService.GameCursor.ItemHold.Graphic:X4}"
                );
            }

            ServiceProvider.Get<GuiService>().GetGump<SplitMenuGump>(cursorService.GameCursor.ItemHold.Serial)?.Dispose();

            cursorService.GameCursor.ItemHold.Clear();
        }
        else
        {
            Log.Warn("There was a problem with ItemHold object. It was cleared before :|");
        }

        //var result = world.Items.Get(ItemHold.Serial);

        //if (result != null && !result.IsDestroyed)
        //    result.AllowedToDraw = true;

        byte code = p.ReadUInt8();

        if (code < 5)
        {
            ServiceProvider.Get<ManagersService>().MessageManager.HandleMessage(
                null,
                ServerErrorMessages.GetError(p[0], code),
                string.Empty,
                0x03b2,
                MessageType.System,
                3,
                TextType.SYSTEM
            );
        }
    }

    public void EndDraggingItem(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        var cursorService = ServiceProvider.Get<GameCursorService>();
        cursorService.GameCursor.ItemHold.Enabled = false;
        cursorService.GameCursor.ItemHold.Dropped = false;
    }

    public void DropItemAccepted(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        var cursorService = ServiceProvider.Get<GameCursorService>();
        cursorService.GameCursor.ItemHold.Enabled = false;
        cursorService.GameCursor.ItemHold.Dropped = false;

        Console.WriteLine("PACKET - ITEM DROP OK!");
    }

    public void DeathScreen(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        // todo
        byte action = p.ReadUInt8();

        if (action != 1)
        {
            ServiceProvider.Get<ManagersService>().Weather.Reset();

            var audioService = ServiceProvider.Get<AudioService>();
            audioService.PlayMusic(audioService.DeathMusicIndex, true);

            if (ProfileManager.CurrentProfile.EnableDeathScreen)
            {
                world.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;
            }

            GameActions.RequestWarMode(world.Player, false);
        }
    }

    public void MobileAttributes(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();

        var entity =world.Get(serial);

        if (entity == null)
        {
            return;
        }

        entity.HitsMax = p.ReadUInt16BE();
        entity.Hits = p.ReadUInt16BE();

        if (entity.HitsRequest == HitsRequestStatus.Pending)
        {
            entity.HitsRequest = HitsRequestStatus.Received;
        }

        if (SerialHelper.IsMobile(serial))
        {
            var mobile = entity as Mobile;

            if (mobile == null)
            {
                return;
            }

            mobile.ManaMax = p.ReadUInt16BE();
            mobile.Mana = p.ReadUInt16BE();
            mobile.StaminaMax = p.ReadUInt16BE();
            mobile.Stamina = p.ReadUInt16BE();

            if (mobile == world.Player)
            {
                ServiceProvider.Get<ManagersService>().UoAssist.SignalHits();
                ServiceProvider.Get<ManagersService>().UoAssist.SignalStamina();
                ServiceProvider.Get<ManagersService>().UoAssist.SignalMana();
            }
        }
    }

    public void EquipItem(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();

        Item item = world.GetOrCreateItem(serial);

        if (item.Graphic != 0 && item.Layer != Layer.Backpack)
        {
            //ClearContainerAndRemoveItems(item);
            world.RemoveItemFromContainer(item);
        }

        if (SerialHelper.IsValid(item.Container))
        {
            ServiceProvider.Get<GuiService>().GetGump<ContainerGump>(item.Container)?.RequestUpdateContents();

            ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
        }

        item.Graphic = (ushort)(p.ReadUInt16BE() + p.ReadInt8());
        item.Layer = (Layer)p.ReadUInt8();
        item.Container = p.ReadUInt32BE();
        item.FixHue(p.ReadUInt16BE());
        item.Amount = 1;

        var entity =world.Get(item.Container);

        entity?.PushToBack(item);

        if (item.Layer >= Layer.ShopBuyRestock && item.Layer <= Layer.ShopSell)
        {
            //item.Clear();
        }
        else if (SerialHelper.IsValid(item.Container) && item.Layer < Layer.Mount)
        {
            ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
        }

        if (
            entity == world.Player
            && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded)
        )
        {
            world.Player?.UpdateAbilities();
        }

        //if (ItemHold.Serial == item.Serial)
        //{
        //    Console.WriteLine("PACKET - ITEM EQUIP");
        //    ItemHold.Enabled = false;
        //    ItemHold.Dropped = true;
        //}
    }

    public void Swing(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        p.Skip(1);

        uint attackers = p.ReadUInt32BE();

        if (attackers != world.Player)
        {
            return;
        }

        uint defenders = p.ReadUInt32BE();

        const int TIME_TURN_TO_LASTTARGET = 2000;

        if (
            ServiceProvider.Get<ManagersService>().TargetManager.LastAttack == defenders
            && world.Player.InWarMode
            && world.Player.Walker.LastStepRequestTime + TIME_TURN_TO_LASTTARGET < Time.Ticks
            && world.Player.Steps.Count == 0
        )
        {
            var enemy = world.Mobiles.Get(defenders);

            if (enemy != null)
            {
                Direction pdir = DirectionHelper.GetDirectionAB(
                    world.Player.X,
                    world.Player.Y,
                    enemy.X,
                    enemy.Y
                );

                int x = world.Player.X;
                int y = world.Player.Y;
                sbyte z = world.Player.Z;

                if (
                    world.Player.Pathfinder.CanWalk(ref pdir, ref x, ref y, ref z)
                    && world.Player.Direction != pdir
                )
                {
                    world.Player.Walk(pdir, false);
                }
            }
        }
    }

    public void Unknown_0x32(ref StackDataReader p) { }

    public void UpdateSkills(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        byte type = p.ReadUInt8();
        bool haveCap = type != 0u && type <= 0x03 || type == 0xDF;
        bool isSingleUpdate = type == 0xFF || type == 0xDF;

        if (type == 0xFE)
        {
            int count = p.ReadUInt16BE();

            var assetsService = ServiceProvider.Get<AssetsService>();
            assetsService.Skills.Skills.Clear();
            assetsService.Skills.SortedSkills.Clear();

            for (int i = 0; i < count; i++)
            {
                bool haveButton = p.ReadBool();
                int nameLength = p.ReadUInt8();

                assetsService.Skills.Skills.Add(
                    new SkillEntry(i, p.ReadASCII(nameLength), haveButton)
                );
            }

            assetsService.Skills.SortedSkills.AddRange(assetsService.Skills.Skills);

            assetsService.Skills.SortedSkills.Sort(
                (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture)
            );
        }
        else
        {
            StandardSkillsGump? standard = null;
            SkillGumpAdvanced? advanced = null;

            if (ProfileManager.CurrentProfile.StandardSkillsGump)
            {
                standard = ServiceProvider.Get<GuiService>().GetGump<StandardSkillsGump>();
            }
            else
            {
                advanced = ServiceProvider.Get<GuiService>().GetGump<SkillGumpAdvanced>();
            }

            if (!isSingleUpdate && (type == 1 || type == 3 || world.SkillsRequested))
            {
                world.SkillsRequested = false;

                // TODO: make a base class for this gump
                if (ProfileManager.CurrentProfile.StandardSkillsGump)
                {
                    if (standard == null)
                    {
                        ServiceProvider.Get<GuiService>().Add(standard = new StandardSkillsGump(world) { X = 100, Y = 100 });
                    }
                }
                else
                {
                    if (advanced == null)
                    {
                        ServiceProvider.Get<GuiService>().Add(advanced = new SkillGumpAdvanced(world) { X = 100, Y = 100 });
                    }
                }
            }

            while (p.Position < p.Length)
            {
                ushort id = p.ReadUInt16BE();

                if (p.Position >= p.Length)
                {
                    break;
                }

                if (id == 0 && type == 0)
                {
                    break;
                }

                if (type == 0 || type == 0x02)
                {
                    id--;
                }

                ushort realVal = p.ReadUInt16BE();
                ushort baseVal = p.ReadUInt16BE();
                Lock locked = (Lock)p.ReadUInt8();
                ushort cap = 1000;

                if (haveCap)
                {
                    cap = p.ReadUInt16BE();
                }

                if (id < world.Player.Skills.Length)
                {
                    Skill skill = world.Player.Skills[id];

                    if (skill != null)
                    {
                        if (isSingleUpdate)
                        {
                            float change = realVal / 10.0f - skill.Value;

                            if (
                                change != 0.0f
                                && !float.IsNaN(change)
                                && ProfileManager.CurrentProfile != null
                                && ProfileManager.CurrentProfile.ShowSkillsChangedMessage
                                && Math.Abs(change * 10)
                                    >= ProfileManager.CurrentProfile.ShowSkillsChangedDeltaValue
                            )
                            {
                                GameActions.Print(
                                    world,
                                    string.Format(
                                        ResGeneral.YourSkillIn0Has1By2ItIsNow3,
                                        skill.Name,
                                        change < 0
                                            ? ResGeneral.Decreased
                                            : ResGeneral.Increased,
                                        Math.Abs(change),
                                        skill.Value + change
                                    ),
                                    0x58,
                                    MessageType.System,
                                    3,
                                    false
                                );
                            }
                        }

                        skill.BaseFixed = baseVal;
                        skill.ValueFixed = realVal;
                        skill.CapFixed = cap;
                        skill.Lock = locked;

                        standard?.Update(id);
                        advanced?.ForceUpdate();
                    }
                }

                if (isSingleUpdate)
                {
                    break;
                }
            }
        }
    }

    public void Pathfinding(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        ushort z = p.ReadUInt16BE();

        world.Player.Pathfinder.WalkTo(x, y, z, 0);
    }

    public void UpdateContainedItems(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        var uoService = ServiceProvider.Get<UOService>();
        ushort count = p.ReadUInt16BE();

        for (int i = 0; i < count; i++)
        {
            uint serial = p.ReadUInt32BE();
            ushort graphic = (ushort)(p.ReadUInt16BE() + p.ReadUInt8());
            ushort amount = Math.Max(p.ReadUInt16BE(), (ushort)1);
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();

            if (uoService.Version >= ClassicUO.Sdk.ClientVersion.CV_6017)
            {
                p.Skip(1);
            }

            uint containerSerial = p.ReadUInt32BE();
            ushort hue = p.ReadUInt16BE();

            AddItemToContainer(
                world,
                serial,
                graphic,
                amount,
                x,
                y,
                hue,
                containerSerial
            );
        }
    }

    public void CloseVendorInterface(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();

        ServiceProvider.Get<GuiService>().GetGump<ShopGump>(serial)?.Dispose();
    }

    public void PersonalLightLevel(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        if (world.Player == p.ReadUInt32BE())
        {
            byte level = p.ReadUInt8();

            if (level > 0x1E)
            {
                level = 0x1E;
            }

            world.Light.RealPersonal = level;

            if (!ProfileManager.CurrentProfile.UseCustomLightLevel)
            {
                world.Light.Personal = level;
            }
        }
    }

    public void LightLevel(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        byte level = p.ReadUInt8();

        if (level > 0x1E)
        {
            level = 0x1E;
        }

        world.Light.RealOverall = level;

        if (
            !ProfileManager.CurrentProfile.UseCustomLightLevel
            || ProfileManager.CurrentProfile.LightLevelType == 1
        )
        {
            world.Light.Overall =
                ProfileManager.CurrentProfile.LightLevelType == 1
                    ? Math.Min(level, ProfileManager.CurrentProfile.LightLevel)
                    : level;
        }
    }

    public void PlaySoundEffect(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        p.Skip(1);

        ushort index = p.ReadUInt16BE();
        ushort audio = p.ReadUInt16BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        short z = (short)p.ReadUInt16BE();

        ServiceProvider.Get<AudioService>().PlaySoundWithDistance(world, index, x, y);
    }

    public void PlayMusic(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (p.Length == 3) // Play Midi Music packet (0x6D, 0x10, index)
        {
            byte cmd = p.ReadUInt8();
            byte index = p.ReadUInt8();

            // Check for stop music packet (6D 1F FF)
            if (cmd == 0x1F && index == 0xFF)
            {
                ServiceProvider.Get<AudioService>().StopMusic();
            }
            else
            {
                ServiceProvider.Get<AudioService>().PlayMusic(index);
            }
        }
        else
        {
            ushort index = p.ReadUInt16BE();
            ServiceProvider.Get<AudioService>().PlayMusic(index);
        }
    }

    public void LoginComplete(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player != null && ServiceProvider.Get<SceneService>().Scene is LoginScene)
        {
            var scene = new GameScene(world);
            ServiceProvider.Get<SceneService>().SetScene(scene);

            //GameActions.OpenPaperdoll(world.Player);
            GameActions.RequestMobileStatus(world,world.Player);
            ServiceProvider.Get<PacketHandlerService>().Out.Send_OpenChat("");

            ServiceProvider.Get<PacketHandlerService>().Out.Send_SkillsRequest(world.Player);
            scene.DoubleClickDelayed(world.Player);

            var uoService = ServiceProvider.Get<UOService>();
            if (uoService.Version >= ClassicUO.Sdk.ClientVersion.CV_306E)
            {
                ServiceProvider.Get<PacketHandlerService>().Out.Send_ClientType();
            }

            if (uoService.Version >= ClassicUO.Sdk.ClientVersion.CV_305D)
            {
                ServiceProvider.Get<PacketHandlerService>().Out.Send_ClientViewRange(world.ClientViewRange);
            }

            List<Gump> gumps = ProfileManager.CurrentProfile.ReadGumps(
                world,
                ProfileManager.CurrentProfile.ProfilePath
            );

            if (gumps != null)
            {
                foreach (Gump gump in gumps)
                {
                    ServiceProvider.Get<GuiService>().Add(gump);
                }
            }
        }
    }

    public void MapData(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();

        var gump = ServiceProvider.Get<GuiService>().GetGump<MapGump>(serial);

        if (gump != null)
        {
            switch ((MapMessageType)p.ReadUInt8())
            {
                case MapMessageType.Add:
                    p.Skip(1);

                    ushort x = p.ReadUInt16BE();
                    ushort y = p.ReadUInt16BE();

                    gump.AddPin(x, y);

                    break;

                case MapMessageType.Insert:
                    break;
                case MapMessageType.Move:
                    break;
                case MapMessageType.Remove:
                    break;

                case MapMessageType.Clear:
                    gump.ClearContainer();

                    break;

                case MapMessageType.Edit:
                    break;

                case MapMessageType.EditResponse:
                    gump.SetPlotState(p.ReadUInt8());

                    break;
            }
        }
    }

    public void SetTime(ref StackDataReader p) { }

    public void SetWeather(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        var scene = ServiceProvider.Get<SceneService>().GetScene<GameScene>();

        if (scene == null)
        {
            return;
        }

        WeatherType type = (WeatherType)p.ReadUInt8();

        if (ServiceProvider.Get<ManagersService>().Weather.CurrentWeather != type)
        {
            byte count = p.ReadUInt8();
            byte temp = p.ReadUInt8();

            ServiceProvider.Get<ManagersService>().Weather.Generate(type, count, temp);
        }
    }

    public void BookData(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort pageCnt = p.ReadUInt16BE();

        var gump = ServiceProvider.Get<GuiService>().GetGump<ModernBookGump>(serial);

        if (gump == null || gump.IsDisposed)
        {
            return;
        }

        for (int i = 0; i < pageCnt; i++)
        {
            int pageNum = p.ReadUInt16BE() - 1;
            gump.KnownPages.Add(pageNum);

            if (pageNum < gump.BookPageCount && pageNum >= 0)
            {
                ushort lineCnt = p.ReadUInt16BE();

                for (int line = 0; line < lineCnt; line++)
                {
                    int index = pageNum * ModernBookGump.MAX_BOOK_LINES + line;

                    if (index < gump.BookLines.Length)
                    {
                        gump.BookLines[index] = ModernBookGump.IsNewBook
                            ? p.ReadUTF8(true)
                            : p.ReadASCII();
                    }
                    else
                    {
                        Log.Error(
                            "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!"
                        );
                    }
                }

                if (lineCnt < ModernBookGump.MAX_BOOK_LINES)
                {
                    for (int line = lineCnt; line < ModernBookGump.MAX_BOOK_LINES; line++)
                    {
                        gump.BookLines[pageNum * ModernBookGump.MAX_BOOK_LINES + line] =
                            string.Empty;
                    }
                }
            }
            else
            {
                Log.Error(
                    "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!"
                );
            }
        }

        gump.ServerSetBookText();
    }

    public void CharacterAnimation(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        var mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
        {
            return;
        }

        ushort action = p.ReadUInt16BE();
        ushort frame_count = p.ReadUInt16BE();
        ushort repeat_count = p.ReadUInt16BE();
        bool forward = !p.ReadBool();
        bool repeat = p.ReadBool();
        byte delay = p.ReadUInt8();

        mobile.SetAnimation(
            Mobile.GetReplacedObjectAnimation(mobile.Graphic, action),
            delay,
            (byte)frame_count,
            (byte)repeat_count,
            repeat,
            forward,
            true
        );
    }

    public void GraphicEffect(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        GraphicEffectType type = (GraphicEffectType)p.ReadUInt8();

        if (type > GraphicEffectType.FixedFrom)
        {
            if (type == GraphicEffectType.ScreenFade && p[0] == 0x70)
            {
                p.Skip(8);
                ushort val = p.ReadUInt16BE();

                if (val > 4)
                {
                    val = 4;
                }

                Log.Warn("Effect not implemented");
            }

            return;
        }

        uint source = p.ReadUInt32BE();
        uint target = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();
        ushort srcX = p.ReadUInt16BE();
        ushort srcY = p.ReadUInt16BE();
        sbyte srcZ = p.ReadInt8();
        ushort targetX = p.ReadUInt16BE();
        ushort targetY = p.ReadUInt16BE();
        sbyte targetZ = p.ReadInt8();
        byte speed = p.ReadUInt8();
        byte duration = p.ReadUInt8();
        ushort unk = p.ReadUInt16BE();
        bool fixedDirection = p.ReadBool();
        bool doesExplode = p.ReadBool();
        uint hue = 0;
        GraphicEffectBlendMode blendmode = 0;

        if (p[0] == 0x70) { }
        else
        {
            hue = p.ReadUInt32BE();
            blendmode = (GraphicEffectBlendMode)(p.ReadUInt32BE() % 7);

            if (p[0] == 0xC7)
            {
                var tileID = p.ReadUInt16BE();
                var explodeEffect = p.ReadUInt16BE();
                var explodeSound = p.ReadUInt16BE();
                var serial = p.ReadUInt32BE();
                var layer = p.ReadUInt8();
                p.Skip(2);
            }
        }

        world.SpawnEffect(
            type,
            source,
            target,
            graphic,
            (ushort)hue,
            srcX,
            srcY,
            srcZ,
            targetX,
            targetY,
            targetZ,
            speed,
            duration,
            fixedDirection,
            doesExplode,
            false,
            blendmode
        );
    }

    public void ClientViewRange(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        world.ClientViewRange = p.ReadUInt8();
    }

    public void BulletinBoardData(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        switch (p.ReadUInt8())
        {
            case 0: // open

                {
                    uint serial = p.ReadUInt32BE();
                    var item = world.Items.Get(serial);

                    if (item != null)
                    {
                        var bulletinBoard = ServiceProvider.Get<GuiService>().GetGump<BulletinBoardGump>(
                            serial
                        );
                        bulletinBoard?.Dispose();

                        int x = (ServiceProvider.Get<WindowService>().ClientBounds.Width >> 1) - 245;
                        int y = (ServiceProvider.Get<WindowService>().ClientBounds.Height >> 1) - 205;

                        bulletinBoard = new BulletinBoardGump(world, item, x, y, p.ReadUTF8(22, true)); //p.ReadASCII(22));
                        ServiceProvider.Get<GuiService>().Add(bulletinBoard);

                        item.Opened = true;
                    }
                }

                break;

            case 1: // summary msg

                {
                    uint boardSerial = p.ReadUInt32BE();
                    var bulletinBoard = ServiceProvider.Get<GuiService>().GetGump<BulletinBoardGump>(
                        boardSerial
                    );

                    if (bulletinBoard != null)
                    {
                        uint serial = p.ReadUInt32BE();
                        uint parendID = p.ReadUInt32BE();

                        // poster
                        int len = p.ReadUInt8();
                        string text = (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                        // subject
                        len = p.ReadUInt8();
                        text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                        // datetime
                        len = p.ReadUInt8();
                        text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true));

                        bulletinBoard.AddBulletinObject(serial, text);
                    }
                }

                break;

            case 2: // message

                {
                    uint boardSerial = p.ReadUInt32BE();
                    var bulletinBoard = ServiceProvider.Get<GuiService>().GetGump<BulletinBoardGump>(
                        boardSerial
                    );

                    if (bulletinBoard != null)
                    {
                        uint serial = p.ReadUInt32BE();

                        int len = p.ReadUInt8();
                        string poster = len > 0 ? p.ReadASCII(len) : string.Empty;

                        len = p.ReadUInt8();
                        string subject = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                        len = p.ReadUInt8();
                        string dataTime = len > 0 ? p.ReadASCII(len) : string.Empty;

                        p.Skip(4);

                        byte unk = p.ReadUInt8();

                        if (unk > 0)
                        {
                            p.Skip(unk * 4);
                        }

                        byte lines = p.ReadUInt8();

                        Span<char> span = stackalloc char[256];
                        ValueStringBuilder sb = new ValueStringBuilder(span);

                        for (int i = 0; i < lines; i++)
                        {
                            byte lineLen = p.ReadUInt8();

                            if (lineLen > 0)
                            {
                                string putta = p.ReadUTF8(lineLen, true);
                                sb.Append(putta);
                                sb.Append('\n');
                            }
                        }

                        string msg = sb.ToString();
                        byte variant = (byte)(1 + (poster == world.Player.Name ? 1 : 0));

                        ServiceProvider.Get<GuiService>().Add(
                            new BulletinBoardItem(
                                world,
                                boardSerial,
                                serial,
                                poster,
                                subject,
                                dataTime,
                                msg.TrimStart(),
                                variant
                            )
                            {
                                X = 40,
                                Y = 40
                            }
                        );

                        sb.Dispose();
                    }
                }

                break;
        }
    }

    public void Warmode(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        world.Player.InWarMode = p.ReadBool();
    }

    public void Ping(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        ServiceProvider.Get<NetClientService>().Statistics.PingReceived(p.ReadUInt8());
    }

    public void BuyList(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        var container = world.Items.Get(p.ReadUInt32BE());

        if (container == null)
        {
            return;
        }

        var vendor = world.Mobiles.Get(container.Container);

        if (vendor == null)
        {
            return;
        }

        var gump = ServiceProvider.Get<GuiService>().GetGump<ShopGump>();

        if (gump != null && (gump.LocalSerial != vendor || !gump.IsBuyGump))
        {
            gump.Dispose();
            gump = null;
        }

        if (gump == null)
        {
            gump = new ShopGump(world, vendor, true, 150, 5);
            ServiceProvider.Get<GuiService>().Add(gump);
        }

        if (container.Layer == Layer.ShopBuyRestock || container.Layer == Layer.ShopBuy)
        {
            byte count = p.ReadUInt8();

            var first = container.Items;

            if (first == null)
            {
                return;
            }

            bool reverse = false;

            if (container.Graphic == 0x2AF8) //hardcoded logic in original client that we must match
            {
                //sort the contents
                first = container.SortContents<Item>((x, y) => x.X - y.X);
            }
            else
            {
                //skip to last item and read in reverse later
                reverse = true;

                while (first?.Next != null)
                {
                    first = first.Next;
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (first == null)
                {
                    break;
                }

                Item it = (Item)first;

                it.Price = p.ReadUInt32BE();
                byte nameLen = p.ReadUInt8();
                string name = p.ReadASCII(nameLen);

                if (ServiceProvider.Get<ManagersService>().OPL.TryGetNameAndData(it.Serial, out var s, out _))
                {
                    it.Name = s ?? "";
                }
                else if (int.TryParse(name, out int cliloc))
                {
                    it.Name = ServiceProvider.Get<AssetsService>().Clilocs.Translate(
                        cliloc,
                        $"\t{it.ItemData.Name}: \t{it.Amount}",
                        true
                    );
                }
                else if (string.IsNullOrEmpty(name))
                {
                    it.Name = it.ItemData.Name;
                }
                else
                {
                    it.Name = name;
                }

                if (reverse)
                {
                    first = first.Previous;
                }
                else
                {
                    first = first.Next;
                }
            }
        }
    }

    public void UpdateCharacter(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        var mobile = world.Mobiles.Get(serial);

        if (mobile == null)
        {
            return;
        }

        ushort graphic = p.ReadUInt16BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = p.ReadInt8();
        Direction direction = (Direction)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        Flags flags = (Flags)p.ReadUInt8();
        NotorietyFlag notoriety = (NotorietyFlag)p.ReadUInt8();

        mobile.NotorietyFlag = notoriety;

        if (serial == world.Player)
        {
            mobile.Flags = flags;
            mobile.Graphic = graphic;
            mobile.CheckGraphicChange();
            mobile.FixHue(hue);
            // TODO: x,y,z, direction cause elastic effect, ignore 'em for the moment
        }
        else
        {
            UpdateGameObject(world, serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 1, 1);
        }
    }

    public void UpdateObject(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = p.ReadInt8();
        Direction direction = (Direction)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        Flags flags = (Flags)p.ReadUInt8();
        NotorietyFlag notoriety = (NotorietyFlag)p.ReadUInt8();
        bool oldDead = false;
        //bool alreadyExists =world.Get(serial) != null;

        if (serial == world.Player)
        {
            oldDead = world.Player.IsDead;
            world.Player.Graphic = graphic;
            world.Player.CheckGraphicChange();
            world.Player.FixHue(hue);
            world.Player.Flags = flags;
        }
        else
        {
            UpdateGameObject(world, serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 0, 1);
        }

        var obj = world.Get(serial);

        if (obj == null)
        {
            return;
        }

        if (!obj.IsEmpty)
        {
            var o = obj.Items;

            while (o != null)
            {
                var next = o.Next;
                Item it = (Item)o;

                if (!it.Opened && it.Layer != Layer.Backpack)
                {
                    world.RemoveItem(it.Serial, true);
                }

                o = next;
            }
        }

        if (SerialHelper.IsMobile(serial) && obj is Mobile mob)
        {
            mob.NotorietyFlag = notoriety;

            ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(serial)?.RequestUpdateContents();
        }

        if (p[0] != 0x78)
        {
            p.Skip(6);
        }

        uint itemSerial = p.ReadUInt32BE();

        while (itemSerial != 0 && p.Position < p.Length)
        {
            //if (!SerialHelper.IsItem(itemSerial))
            //    break;

            ushort itemGraphic = p.ReadUInt16BE();
            byte layer = p.ReadUInt8();
            ushort item_hue = 0;

            if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_70331)
            {
                item_hue = p.ReadUInt16BE();
            }
            else if ((itemGraphic & 0x8000) != 0)
            {
                itemGraphic &= 0x7FFF;
                item_hue = p.ReadUInt16BE();
            }

            Item item = world.GetOrCreateItem(itemSerial);
            item.Graphic = itemGraphic;
            item.FixHue(item_hue);
            item.Amount = 1;
            world.RemoveItemFromContainer(item);
            item.Container = serial;
            item.Layer = (Layer)layer;

            item.CheckGraphicChange();

            obj.PushToBack(item);

            itemSerial = p.ReadUInt32BE();
        }

        if (serial == world.Player)
        {
            if (oldDead != world.Player.IsDead)
            {
                if (world.Player.IsDead)
                {
                    // NOTE: This packet causes some weird issue on sphere servers.
                    //       When the character dies, this packet trigger a "reset" and
                    //       somehow it messes up the packet reading server side
                    //ServiceProvider.Get<PacketHandlerService>().Out.Send_DeathScreen();
                    world.ChangeSeason(Game.Managers.Season.Desolation, 42);
                }
                else
                {
                    world.ChangeSeason(world.OldSeason, world.OldMusicIndex);
                }
            }

            ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(serial)?.RequestUpdateContents();

            world.Player.UpdateAbilities();
        }
    }

    public void OpenMenu(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort id = p.ReadUInt16BE();
        string name = p.ReadASCII(p.ReadUInt8());
        int count = p.ReadUInt8();

        ushort menuid = p.ReadUInt16BE();
        p.Seek(p.Position - 2);

        if (menuid != 0)
        {
            MenuGump gump = new MenuGump(world, serial, id, name) { X = 100, Y = 100 };

            int posX = 0;

            for (int i = 0; i < count; i++)
            {
                ushort graphic = p.ReadUInt16BE();
                ushort hue = p.ReadUInt16BE();
                name = p.ReadASCII(p.ReadUInt8());

                ref readonly var artInfo = ref ServiceProvider.Get<UOService>().Self.Arts.GetArt(graphic);

                if (artInfo.UV.Width != 0 && artInfo.UV.Height != 0)
                {
                    int posY = artInfo.UV.Height;

                    if (posY >= 47)
                    {
                        posY = 0;
                    }
                    else
                    {
                        posY = (47 - posY) >> 1;
                    }

                    gump.AddItem(graphic, hue, name, posX, posY, i + 1);

                    posX += artInfo.UV.Width;
                }
            }

            ServiceProvider.Get<GuiService>().Add(gump);
        }
        else
        {
            GrayMenuGump gump = new GrayMenuGump(world, serial, id, name)
            {
                X = (ServiceProvider.Get<WindowService>().ClientBounds.Width >> 1) - 200,
                Y = (ServiceProvider.Get<WindowService>().ClientBounds.Height >> 1) - ((121 + count * 21) >> 1)
            };

            int offsetY = 35 + gump.Height;
            int gumpHeight = 70 + offsetY;

            for (int i = 0; i < count; i++)
            {
                p.Skip(4);
                name = p.ReadASCII(p.ReadUInt8());

                int addHeight = gump.AddItem(name, offsetY);

                if (addHeight < 21)
                {
                    addHeight = 21;
                }

                offsetY += addHeight - 1;
                gumpHeight += addHeight;
            }

            offsetY += 5;

            gump.Add(
                new Button(0, 0x1450, 0x1451, 0x1450)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = 70,
                    Y = offsetY
                }
            );

            gump.Add(
                new Button(1, 0x13B2, 0x13B3, 0x13B2)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = 200,
                    Y = offsetY
                }
            );

            gump.SetHeight(gumpHeight);
            gump.WantUpdateSize = false;
            ServiceProvider.Get<GuiService>().Add(gump);
        }
    }

    public void OpenPaperdoll(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        var mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
        {
            return;
        }

        string text = p.ReadASCII(60);
        byte flags = p.ReadUInt8();

        mobile.Title = text;

        var paperdoll = ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(mobile);

        if (paperdoll == null)
        {
            if (!ServiceProvider.Get<GuiService>().GetGumpCachePosition(mobile, out Point location))
            {
                location = new Point(100, 100);
            }

            ServiceProvider.Get<GuiService>().Add(
                new PaperDollGump(world, mobile, (flags & 0x02) != 0) { Location = location }
            );
        }
        else
        {
            bool old = paperdoll.CanLift;
            bool newLift = (flags & 0x02) != 0;

            paperdoll.CanLift = newLift;
            paperdoll.UpdateTitle(text);

            if (old != newLift)
            {
                paperdoll.RequestUpdateContents();
            }

            paperdoll.SetInScreen();
            paperdoll.BringOnTop();
        }
    }

    public void CorpseEquipment(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        var corpse = world.Get(serial);

        if (corpse == null)
        {
            return;
        }

        // if it's not a corpse we should skip this [?]
        if (corpse.Graphic != 0x2006)
        {
            return;
        }

        Layer layer = (Layer)p.ReadUInt8();

        while (layer != Layer.Invalid && p.Position < p.Length)
        {
            uint item_serial = p.ReadUInt32BE();

            if (layer - 1 != Layer.Backpack)
            {
                Item item = world.GetOrCreateItem(item_serial);

                world.RemoveItemFromContainer(item);
                item.Container = serial;
                item.Layer = layer - 1;
                corpse.PushToBack(item);
            }

            layer = (Layer)p.ReadUInt8();
        }
    }

    public void DisplayMap(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();
        ushort gumpid = p.ReadUInt16BE();
        ushort startX = p.ReadUInt16BE();
        ushort startY = p.ReadUInt16BE();
        ushort endX = p.ReadUInt16BE();
        ushort endY = p.ReadUInt16BE();
        ushort width = p.ReadUInt16BE();
        ushort height = p.ReadUInt16BE();

        MapGump gump = new MapGump(world, serial, gumpid, width, height);
        SpriteInfo multiMapInfo;

        if (p[0] == 0xF5 || ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_308Z)
        {
            ushort facet = 0;

            if (p[0] == 0xF5)
            {
                facet = p.ReadUInt16BE();
            }

            multiMapInfo = ServiceProvider.Get<UOService>().Self.MultiMaps.GetMap(facet, width, height, startX, startY, endX, endY);            }
        else
        {
            multiMapInfo = ServiceProvider.Get<UOService>().Self.MultiMaps.GetMap(null, width, height, startX, startY, endX, endY);
        }

        if (multiMapInfo.Texture != null)
            gump.SetMapTexture(multiMapInfo.Texture);

        ServiceProvider.Get<GuiService>().Add(gump);

        var it = world.Items.Get(serial);

        if (it != null)
        {
            it.Opened = true;
        }
    }

    public void OpenBook(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();
        bool oldpacket = p[0] == 0x93;
        bool editable = p.ReadBool();

        if (!oldpacket)
        {
            editable = p.ReadBool();
        }
        else
        {
            p.Skip(1);
        }

        var bgump = ServiceProvider.Get<GuiService>().GetGump<ModernBookGump>(serial);

        if (bgump == null || bgump.IsDisposed)
        {
            ushort page_count = p.ReadUInt16BE();
            string title = oldpacket
                ? p.ReadUTF8(60, true)
                : p.ReadUTF8(p.ReadUInt16BE(), true);
            string author = oldpacket
                ? p.ReadUTF8(30, true)
                : p.ReadUTF8(p.ReadUInt16BE(), true);

            ServiceProvider.Get<GuiService>().Add(
                new ModernBookGump(world, serial, page_count, title, author, editable, oldpacket)
                {
                    X = 100,
                    Y = 100
                }
            );

            ServiceProvider.Get<PacketHandlerService>().Out.Send_BookPageDataRequest(serial, 1);
        }
        else
        {
            p.Skip(2);
            bgump.IsEditable = editable;
            bgump.SetTile(
                oldpacket ? p.ReadUTF8(60, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                editable
            );
            bgump.SetAuthor(
                oldpacket ? p.ReadUTF8(30, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                editable
            );
            bgump.UseNewHeader = !oldpacket;
            bgump.SetInScreen();
            bgump.BringOnTop();
        }
    }

    public void DyeData(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();
        p.Skip(2);
        ushort graphic = p.ReadUInt16BE();

        ref readonly var gumpInfo = ref ServiceProvider.Get<UOService>().Self.Gumps.GetGump(0x0906);

        int x = (ServiceProvider.Get<WindowService>().ClientBounds.Width >> 1) - (gumpInfo.UV.Width >> 1);
        int y = (ServiceProvider.Get<WindowService>().ClientBounds.Height >> 1) - (gumpInfo.UV.Height >> 1);

        var gump = ServiceProvider.Get<GuiService>().GetGump<ColorPickerGump>(serial);

        if (gump == null || gump.IsDisposed || gump.Graphic != graphic)
        {
            gump?.Dispose();

            gump = new ColorPickerGump(world, serial, graphic, x, y, null);

            ServiceProvider.Get<GuiService>().Add(gump);
        }
    }

    public void MovePlayer(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        Direction direction = (Direction)p.ReadUInt8();
        world.Player.Walk(direction & Direction.Mask, (direction & Direction.Running) != 0);
    }

    public void UpdateName(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        string name = p.ReadASCII();

        var wme = ServiceProvider.Get<ManagersService>().WMapManager.GetEntity(serial);

        if (wme != null && !string.IsNullOrEmpty(name))
        {
            wme.Name = name;
        }

        var entity = world.Get(serial);

        if (entity != null)
        {
            entity.Name = name;

            if (
                serial == world.Player.Serial
                && !string.IsNullOrEmpty(name)
                && name != world.Player.Name
            )
            {
                ServiceProvider.Get<EngineService>().SetWindowTitle(name);
            }

            ServiceProvider.Get<GuiService>().GetGump<NameOverheadGump>(serial)?.SetName();
        }
    }

    public void MultiPlacement(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        bool allowGround = p.ReadBool();
        uint targID = p.ReadUInt32BE();
        byte flags = p.ReadUInt8();
        p.Seek(18);
        ushort multiID = p.ReadUInt16BE();
        ushort xOff = p.ReadUInt16BE();
        ushort yOff = p.ReadUInt16BE();
        ushort zOff = p.ReadUInt16BE();
        ushort hue = p.ReadUInt16BE();

        ServiceProvider.Get<ManagersService>().TargetManager.SetTargetingMulti(targID, multiID, xOff, yOff, zOff, hue);
    }

    public void ASCIIPrompt(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        ServiceProvider.Get<ManagersService>().MessageManager.PromptData = new PromptData
        {
            Prompt = ConsolePrompt.ASCII,
            Data = p.ReadUInt64BE()
        };
    }

    public void SellList(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        var vendor = world.Mobiles.Get(p.ReadUInt32BE());

        if (vendor == null)
        {
            return;
        }

        ushort countItems = p.ReadUInt16BE();

        if (countItems <= 0)
        {
            return;
        }

        var gump = ServiceProvider.Get<GuiService>().GetGump<ShopGump>(vendor);
        gump?.Dispose();
        gump = new ShopGump(world, vendor, false, 100, 0);

        for (int i = 0; i < countItems; i++)
        {
            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            ushort hue = p.ReadUInt16BE();
            ushort amount = p.ReadUInt16BE();
            ushort price = p.ReadUInt16BE();
            var name = p.ReadASCII(p.ReadUInt16BE());
            bool fromcliloc = false;

            if (int.TryParse(name, out int clilocnum))
            {
                name = ServiceProvider.Get<AssetsService>().Clilocs.GetString(clilocnum);
                fromcliloc = true;
            }
            else if (string.IsNullOrEmpty(name))
            {
                bool success = ServiceProvider.Get<ManagersService>().OPL.TryGetNameAndData(serial, out name, out _);

                if (!success)
                {
                    name = ServiceProvider.Get<AssetsService>().TileData.StaticData[graphic].Name;
                }
            }

            //if (string.IsNullOrEmpty(item.Name))
            //    item.Name = name;

            gump.AddItem(serial, graphic, hue, amount, price, name, fromcliloc);
        }

        ServiceProvider.Get<GuiService>().Add(gump);
    }

    public void UpdateHitpoints(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        var entity = world.Get(p.ReadUInt32BE());

        if (entity == null)
        {
            return;
        }

        entity.HitsMax = p.ReadUInt16BE();
        entity.Hits = p.ReadUInt16BE();

        if (entity.HitsRequest == HitsRequestStatus.Pending)
        {
            entity.HitsRequest = HitsRequestStatus.Received;
        }

        if (entity == world.Player)
        {
            ServiceProvider.Get<ManagersService>().UoAssist.SignalHits();
        }
    }

    public void UpdateMana(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        var mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
        {
            return;
        }

        mobile.ManaMax = p.ReadUInt16BE();
        mobile.Mana = p.ReadUInt16BE();

        if (mobile == world.Player)
        {
            ServiceProvider.Get<ManagersService>().UoAssist.SignalMana();
        }
    }

    public void UpdateStamina(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        var mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
        {
            return;
        }

        mobile.StaminaMax = p.ReadUInt16BE();
        mobile.Stamina = p.ReadUInt16BE();

        if (mobile == world.Player)
        {
            ServiceProvider.Get<ManagersService>().UoAssist.SignalStamina();
        }
    }

    public void OpenUrl(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        string url = p.ReadASCII();

        if (!string.IsNullOrEmpty(url))
        {
            PlatformHelper.LaunchBrowser(url);
        }
    }

    public void TipWindow(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        byte flag = p.ReadUInt8();

        if (flag == 1)
        {
            return;
        }

        uint tip = p.ReadUInt32BE();
        var str = p.ReadASCII(p.ReadUInt16BE())?.Replace('\r', '\n');

        int x = 20;
        int y = 20;

        if (flag == 0)
        {
            x = 200;
            y = 100;
        }

        ServiceProvider.Get<GuiService>().Add(new TipNoticeGump(world, tip, flag, str) { X = x, Y = y });
    }

    public void AttackCharacter(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();

        //if (TargetManager.LastAttack != serial && world.InGame)
        //{



        //}

        GameActions.SendCloseStatus(world, ServiceProvider.Get<ManagersService>().TargetManager.LastAttack);
        ServiceProvider.Get<ManagersService>().TargetManager.LastAttack = serial;
        GameActions.RequestMobileStatus(world, serial);
    }

    public void TextEntryDialog(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        byte parentID = p.ReadUInt8();
        byte buttonID = p.ReadUInt8();

        ushort textLen = p.ReadUInt16BE();
        string text = p.ReadASCII(textLen);

        bool haveCancel = p.ReadBool();
        byte variant = p.ReadUInt8();
        uint maxLength = p.ReadUInt32BE();

        ushort descLen = p.ReadUInt16BE();
        string desc = p.ReadASCII(descLen);

        TextEntryDialogGump gump = new TextEntryDialogGump(
            world,
            serial,
            143,
            172,
            variant,
            (int)maxLength,
            text,
            desc,
            buttonID,
            parentID
        )
        {
            CanCloseWithRightClick = haveCancel
        };

        ServiceProvider.Get<GuiService>().Add(gump);
    }

    public void UnicodeTalk(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            var scene = ServiceProvider.Get<SceneService>().GetScene<LoginScene>();

            if (scene != null)
            {
                //Serial serial = p.ReadUInt32BE();
                //ushort graphic = p.ReadUInt16BE();
                //MessageType type = (MessageType)p.ReadUInt8();
                //Hue hue = p.ReadUInt16BE();
                //MessageFont font = (MessageFont)p.ReadUInt16BE();
                //string lang = p.ReadASCII(4);
                //string name = p.ReadASCII(30);
                Log.Warn("UnicodeTalk received during LoginScene");

                if (p.Length > 48)
                {
                    p.Seek(48);
                    Log.Warn("Handled UnicodeTalk in LoginScene");
                }
            }

            return;
        }

        uint serial = p.ReadUInt32BE();
        var entity = world.Get(serial);
        ushort graphic = p.ReadUInt16BE();
        MessageType type = (MessageType)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        ushort font = p.ReadUInt16BE();
        string lang = p.ReadASCII(4);
        string name = p.ReadASCII();

        if (
            serial == 0
            && graphic == 0
            && type == MessageType.Regular
            && font == 0xFFFF
            && hue == 0xFFFF
            && name.ToLower() == "system"
        )
        {
            Span<byte> buffer =
                stackalloc byte[] {
                    0x03,
                    0x00,
                    0x28,
                    0x20,
                    0x00,
                    0x34,
                    0x00,
                    0x03,
                    0xdb,
                    0x13,
                    0x14,
                    0x3f,
                    0x45,
                    0x2c,
                    0x58,
                    0x0f,
                    0x5d,
                    0x44,
                    0x2e,
                    0x50,
                    0x11,
                    0xdf,
                    0x75,
                    0x5c,
                    0xe0,
                    0x3e,
                    0x71,
                    0x4f,
                    0x31,
                    0x34,
                    0x05,
                    0x4e,
                    0x18,
                    0x1e,
                    0x72,
                    0x0f,
                    0x59,
                    0xad,
                    0xf5,
                    0x00
                };

            ServiceProvider.Get<NetClientService>().Socket.Send(buffer);

            return;
        }

        string text = string.Empty;

        if (p.Length > 48)
        {
            p.Seek(48);
            text = p.ReadUnicodeBE();
        }

        TextType text_type = TextType.SYSTEM;

        if (type == MessageType.Alliance || type == MessageType.Guild)
        {
            text_type = TextType.GUILD_ALLY;
        }
        else if (
            type == MessageType.System
            || serial == 0xFFFF_FFFF
            || serial == 0
            || name.ToLower() == "system" && entity == null
        )
        {
            // do nothing
        }
        else if (entity != null)
        {
            text_type = TextType.OBJECT;

            if (string.IsNullOrEmpty(entity.Name))
            {
                entity.Name = string.IsNullOrEmpty(name) ? text : name;
            }
        }

        ServiceProvider.Get<ManagersService>().MessageManager.HandleMessage(
            entity,
            text,
            name,
            hue,
            type,
            ProfileManager.CurrentProfile.ChatFont,
            text_type,
            true,
            lang
        );
    }

    public void DisplayDeath(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        uint corpseSerial = p.ReadUInt32BE();
        uint running = p.ReadUInt32BE();

        var owner = world.Mobiles.Get(serial);

        if (owner == null || serial == world.Player)
        {
            return;
        }

        serial |= 0x80000000;

        if (world.Mobiles.Remove(owner.Serial))
        {
            for (var i = owner.Items; i != null; i = i.Next)
            {
                Item it = (Item)i;
                it.Container = serial;
            }

            world.Mobiles[serial] = owner;
            owner.Serial = serial;
        }

        if (SerialHelper.IsValid(corpseSerial))
        {
            ServiceProvider.Get<ManagersService>().CorpseManager.Add(corpseSerial, serial, owner.Direction, running != 0);
        }

        var animations = ServiceProvider.Get<UOService>().Self.Animations;
        var gfx = owner.Graphic;
        animations.ConvertBodyIfNeeded(ref gfx);
        var animGroup = animations.GetAnimType(gfx);
        var animFlags = animations.GetAnimFlags(gfx);
        byte group = ServiceProvider.Get<AssetsService>().Animations.GetDeathAction(
            gfx,
            animFlags,
            animGroup,
            running != 0,
            true
        );
        owner.SetAnimation(group, 0, 5, 1);
        owner.AnimIndex = 0;

        if (ProfileManager.CurrentProfile.AutoOpenCorpses)
        {
            world.Player.TryOpenCorpses();
        }
    }

    public void OpenGump(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint sender = p.ReadUInt32BE();
        uint gumpID = p.ReadUInt32BE();
        int x = (int)p.ReadUInt32BE();
        int y = (int)p.ReadUInt32BE();

        ushort cmdLen = p.ReadUInt16BE();
        string cmd = p.ReadASCII(cmdLen);

        ushort textLinesCount = p.ReadUInt16BE();

        string[] lines = new string[textLinesCount];

        for (int i = 0; i < textLinesCount; ++i)
        {
            int length = p.ReadUInt16BE();

            if (length > 0)
            {
                lines[i] = p.ReadUnicodeBE(length);
            }
            else
            {
                lines[i] = string.Empty;
            }
        }

        //for (int i = 0, index = p.Position; i < textLinesCount; i++)
        //{
        //    int length = ((p[index++] << 8) | p[index++]) << 1;
        //    int true_length = 0;

        //    while (true_length < length)
        //    {
        //        if (((p[index + true_length++] << 8) | p[index + true_length++]) << 1 == '\0')
        //        {
        //            break;
        //        }
        //    }

        //    unsafe
        //    {

        //        fixed (byte* ptr = &p.Buffer[index])
        //        {
        //            lines[i] = Encoding.BigEndianUnicode.GetString(ptr, true_length);
        //        }
        //    }
        //    index += length;
        //}

        CreateGump(world, sender, gumpID, x, y, cmd, lines);
    }

    public void ChatMessage(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        ushort cmd = p.ReadUInt16BE();

        switch (cmd)
        {
            case 0x03E8: // create conference
                p.Skip(4);
                string channelName = p.ReadUnicodeBE();
                bool hasPassword = p.ReadUInt16BE() == 0x31;
                world.ChatManager.CurrentChannelName = channelName;
                world.ChatManager.AddChannel(channelName, hasPassword);

                ServiceProvider.Get<GuiService>().GetGump<ChatGump>()?.RequestUpdateContents();

                break;

            case 0x03E9: // destroy conference
                p.Skip(4);
                channelName = p.ReadUnicodeBE();
                world.ChatManager.RemoveChannel(channelName);

                ServiceProvider.Get<GuiService>().GetGump<ChatGump>()?.RequestUpdateContents();

                break;

            case 0x03EB: // display enter username window
                world.ChatManager.ChatIsEnabled = ChatStatus.EnabledUserRequest;

                break;

            case 0x03EC: // close chat
                world.ChatManager.Clear();
                world.ChatManager.ChatIsEnabled = ChatStatus.Disabled;

                ServiceProvider.Get<GuiService>().GetGump<ChatGump>()?.Dispose();

                break;

            case 0x03ED: // username accepted, display chat
                p.Skip(4);
                string username = p.ReadUnicodeBE();
                world.ChatManager.ChatIsEnabled = ChatStatus.Enabled;
                ServiceProvider.Get<PacketHandlerService>().Out.Send_ChatJoinCommand("General");

                break;

            case 0x03EE: // add user
                p.Skip(4);
                ushort userType = p.ReadUInt16BE();
                username = p.ReadUnicodeBE();

                break;

            case 0x03EF: // remove user
                p.Skip(4);
                username = p.ReadUnicodeBE();

                break;

            case 0x03F0: // clear all players
                break;

            case 0x03F1: // you have joined a conference
                p.Skip(4);
                channelName = p.ReadUnicodeBE();
                world.ChatManager.CurrentChannelName = channelName;

                ServiceProvider.Get<GuiService>().GetGump<ChatGump>()?.UpdateConference();

                GameActions.Print(
                    world,
                    string.Format(ResGeneral.YouHaveJoinedThe0Channel, channelName),
                    ProfileManager.CurrentProfile.ChatMessageHue,
                    MessageType.Regular,
                    1
                );

                break;

            case 0x03F4:
                p.Skip(4);
                channelName = p.ReadUnicodeBE();

                GameActions.Print(
                    world,
                    string.Format(ResGeneral.YouHaveLeftThe0Channel, channelName),
                    ProfileManager.CurrentProfile.ChatMessageHue,
                    MessageType.Regular,
                    1
                );

                break;

            case 0x0025:
            case 0x0026:
            case 0x0027:
                p.Skip(4);
                ushort msgType = p.ReadUInt16BE();
                username = p.ReadUnicodeBE();
                string msgSent = p.ReadUnicodeBE();

                if (!string.IsNullOrEmpty(msgSent))
                {
                    int idx = msgSent.IndexOf('{');
                    int idxLast = msgSent.IndexOf('}') + 1;

                    if (idxLast > idx && idx > -1)
                    {
                        msgSent = msgSent.Remove(idx, idxLast - idx);
                    }
                }

                //Color c = new Color(49, 82, 156, 0);
                GameActions.Print(
                    world,
                    $"{username}: {msgSent}",
                    ProfileManager.CurrentProfile.ChatMessageHue,
                    MessageType.Regular,
                    1
                );

                break;

            default:
                if (cmd >= 0x0001 && cmd <= 0x0024 || cmd >= 0x0028 && cmd <= 0x002C)
                {
                    // TODO: read Chat.enu ?
                    // http://docs.polserver.com/packets/index.php?Packet=0xB2

                    string msg = ChatManager.GetMessage(cmd - 1);

                    if (string.IsNullOrEmpty(msg))
                    {
                        return;
                    }

                    p.Skip(4);
                    string text = p.ReadUnicodeBE();

                    if (!string.IsNullOrEmpty(text))
                    {
                        int idx = msg.IndexOf("%1");

                        if (idx >= 0)
                        {
                            msg = msg.Replace("%1", text);
                        }

                        if (cmd - 1 == 0x000A || cmd - 1 == 0x0017)
                        {
                            idx = msg.IndexOf("%2");

                            if (idx >= 0)
                            {
                                msg = msg.Replace("%2", text);
                            }
                        }
                    }

                    GameActions.Print(
                        world,
                        msg,
                        ProfileManager.CurrentProfile.ChatMessageHue,
                        MessageType.Regular,
                        1
                    );
                }

                break;
        }
    }

    public void Help(ref StackDataReader p) { }

    public void CharacterProfile(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        string header = p.ReadASCII();
        string footer = p.ReadUnicodeBE();

        string body = p.ReadUnicodeBE();

        ServiceProvider.Get<GuiService>().GetGump<ProfileGump>(serial)?.Dispose();

        ServiceProvider.Get<GuiService>().Add(
            new ProfileGump(world, serial, header, footer, body, serial == world.Player.Serial)
        );
    }

    public void EnableLockedFeatures(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        LockedFeatureFlags flags = 0;

        if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_60142)
        {
            flags = (LockedFeatureFlags)p.ReadUInt32BE();
        }
        else
        {
            flags = (LockedFeatureFlags)p.ReadUInt16BE();
        }

        world.ClientLockedFeatures.SetFlags(flags);

        world.ChatManager.ChatIsEnabled = world.ClientLockedFeatures.Flags.HasFlag(
            LockedFeatureFlags.T2A
        )
            ? ChatStatus.Enabled
            : 0;

        BodyConvFlags bcFlags = 0;
        if (flags.HasFlag(LockedFeatureFlags.UOR))
            bcFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.LBR))
            bcFlags |= BodyConvFlags.Anim1;
        if (flags.HasFlag(LockedFeatureFlags.AOS))
            bcFlags |= BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.SE))
            bcFlags |= BodyConvFlags.Anim3;
        if (flags.HasFlag(LockedFeatureFlags.ML))
            bcFlags |= BodyConvFlags.Anim4;

        ServiceProvider.Get<UOService>().Self.Animations.UpdateAnimationTable(bcFlags);
    }

    public void DisplayQuestArrow(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        bool display = p.ReadBool();
        ushort mx = p.ReadUInt16BE();
        ushort my = p.ReadUInt16BE();

        uint serial = 0;

        if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_7090)
        {
            serial = p.ReadUInt32BE();
        }

        var arrow = ServiceProvider.Get<GuiService>().GetGump<QuestArrowGump>(serial);

        if (display)
        {
            if (arrow == null)
            {
                ServiceProvider.Get<GuiService>().Add(new QuestArrowGump(world, serial, mx, my));
            }
            else
            {
                arrow.SetRelativePosition(mx, my);
            }
        }
        else
        {
            if (arrow != null)
            {
                arrow.Dispose();
            }
        }
    }

    public void UltimaMessengerR(ref StackDataReader p) { }

    public void Season(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        byte season = p.ReadUInt8();
        byte music = p.ReadUInt8();

        if (season > 4)
        {
            season = 0;
        }

        if (world.Player.IsDead && season == 4)
        {
            return;
        }

        world.OldSeason = (Season)season;
        world.OldMusicIndex = music;

        if (world.Season == Game.Managers.Season.Desolation)
        {
            world.OldMusicIndex = 42;
        }

        world.ChangeSeason((Season)season, music);
    }

    public void ClientVersion(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        ServiceProvider.Get<PacketHandlerService>().Out.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);
    }

    public void AssistVersion(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        //uint version = p.ReadUInt32BE();

        //string[] parts = Service.GetByLocalSerial<Settings>().ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        //byte[] clientVersionBuffer =
        //    {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

        //NetClient.Socket.Send(new PAssistVersion(clientVersionBuffer, version));
    }

    public void ExtendedCommand(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        ushort cmd = p.ReadUInt16BE();

        switch (cmd)
        {
            case 0:
                break;

            //===========================================================================================
            //===========================================================================================
            case 1: // fast walk prevention
                for (int i = 0; i < 6; i++)
                {
                    world.Player.Walker.FastWalkStack.SetValue(i, p.ReadUInt32BE());
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 2: // add key to fast walk stack
                world.Player.Walker.FastWalkStack.AddValue(p.ReadUInt32BE());

                break;

            //===========================================================================================
            //===========================================================================================
            case 4: // close generic gump
                uint ser = p.ReadUInt32BE();
                int button = (int)p.ReadUInt32BE();

                var first = ServiceProvider.Get<GuiService>().Gumps.First;

                while (first != null)
                {
                    var nextGump = first.Next;

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
                                ServiceProvider.Get<GuiService>().SavePosition(ser, first.Value.Location);
                            }
                            else
                            {
                                ServiceProvider.Get<GuiService>().RemovePosition(ser);
                            }
                        }

                        first.Value.Dispose();
                    }

                    first = nextGump;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 6: //party
                ServiceProvider.Get<ManagersService>().Party.ParsePacket(ref p);

                break;

            //===========================================================================================
            //===========================================================================================
            case 8: // map change
                world.MapIndex = p.ReadUInt8();

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x0C: // close statusbar gump
                ServiceProvider.Get<GuiService>().GetGump<HealthBarGump>(p.ReadUInt32BE())?.Dispose();

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x10: // display equip info
                var item = world.Items.Get(p.ReadUInt32BE());

                if (item == null)
                {
                    return;
                }

                uint cliloc = p.ReadUInt32BE();
                string str = string.Empty;

                if (cliloc > 0)
                {
                    str = ServiceProvider.Get<AssetsService>().Clilocs.GetString((int)cliloc, true);

                    if (!string.IsNullOrEmpty(str))
                    {
                        item.Name = str;
                    }

                    ServiceProvider.Get<ManagersService>().MessageManager.HandleMessage(
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

                str = string.Empty;
                ushort crafterNameLen = 0;
                uint next = p.ReadUInt32BE();

                Span<char> span = stackalloc char[256];
                ValueStringBuilder strBuffer = new ValueStringBuilder(span);
                if (next == 0xFFFFFFFD)
                {
                    crafterNameLen = p.ReadUInt16BE();

                    if (crafterNameLen > 0)
                    {
                        strBuffer.Append(ResGeneral.CraftedBy);
                        strBuffer.Append(p.ReadASCII(crafterNameLen));
                    }
                }

                if (crafterNameLen != 0)
                {
                    next = p.ReadUInt32BE();
                }

                if (next == 0xFFFFFFFC)
                {
                    strBuffer.Append("[Unidentified");
                }

                byte count = 0;

                while (p.Position < p.Length - 4)
                {
                    if (count != 0 || next == 0xFFFFFFFD || next == 0xFFFFFFFC)
                    {
                        next = p.ReadUInt32BE();
                    }

                    short charges = (short)p.ReadUInt16BE();
                    string attr = ServiceProvider.Get<AssetsService>().Clilocs.GetString((int)next);

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

                if (count < 20 && count > 0 || next == 0xFFFFFFFC && count == 0)
                {
                    strBuffer.Append(']');
                }

                if (strBuffer.Length != 0)
                {
                    ServiceProvider.Get<ManagersService>().MessageManager.HandleMessage(
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

                ServiceProvider.Get<PacketHandlerService>().Out.Send_MegaClilocRequest_Old(item);

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x11:
                break;

            //===========================================================================================
            //===========================================================================================
            case 0x14: // display popup/context menu
                ServiceProvider.Get<GuiService>().ShowGamePopup(
                    new PopupMenuGump(world, PopupMenuData.Parse(ref p))
                    {
                        X = ServiceProvider.Get<ManagersService>().DelayedObjectClickManager.LastMouseX,
                        Y = ServiceProvider.Get<ManagersService>().DelayedObjectClickManager.LastMouseY
                    }
                );

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x16: // close user interface windows
                uint id = p.ReadUInt32BE();
                uint serial = p.ReadUInt32BE();

                switch (id)
                {
                    case 1: // paperdoll
                        ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(serial)?.Dispose();

                        break;

                    case 2: //statusbar
                        ServiceProvider.Get<GuiService>().GetGump<HealthBarGump>(serial)?.Dispose();

                        if (serial == world.Player.Serial)
                        {
                            StatusGumpBase.GetStatusGump()?.Dispose();
                        }

                        break;

                    case 8: // char profile
                        ServiceProvider.Get<GuiService>().GetGump<ProfileGump>()?.Dispose();

                        break;

                    case 0x0C: //container
                        ServiceProvider.Get<GuiService>().GetGump<ContainerGump>(serial)?.Dispose();

                        break;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x18: // enable map patches

                if (ServiceProvider.Get<AssetsService>().Maps.ApplyPatches(ref p))
                {
                    //List<GameObject> list = new List<GameObject>();

                    //foreach (int i in world.Map.GetUsedChunks())
                    //{
                    //    Chunk chunk = world.Map.Chunks[i];

                    //    for (int xx = 0; xx < 8; xx++)
                    //    {
                    //        for (int yy = 0; yy < 8; yy++)
                    //        {
                    //            Tile tile = chunk.Tiles[xx, yy];

                    //            for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                    //            {
                    //                if (!(obj is Static) && !(obj is Land))
                    //                {
                    //                    list.Add(obj);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}


                    int map = world.MapIndex;
                    world.MapIndex = -1;
                    world.MapIndex = map;

                    Log.Trace("Map Patches applied.");
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x19: //extened stats
                byte version = p.ReadUInt8();
                serial = p.ReadUInt32BE();

                switch (version)
                {
                    case 0:
                        var bonded = world.Mobiles.Get(serial);

                        if (bonded == null)
                        {
                            break;
                        }

                        bool dead = p.ReadBool();
                        bonded.IsDead = dead;

                        break;

                    case 2:

                        if (serial == world.Player)
                        {
                            byte updategump = p.ReadUInt8();
                            byte state = p.ReadUInt8();

                            world.Player.StrLock = (Lock)((state >> 4) & 3);
                            world.Player.DexLock = (Lock)((state >> 2) & 3);
                            world.Player.IntLock = (Lock)(state & 3);

                            StatusGumpBase.GetStatusGump()?.RequestUpdateContents();
                        }

                        break;

                    case 5:

                        int pos = p.Position;
                        byte zero = p.ReadUInt8();
                        byte type2 = p.ReadUInt8();

                        if (type2 == 0xFF)
                        {
                            byte status = p.ReadUInt8();
                            ushort animation = p.ReadUInt16BE();
                            ushort frame = p.ReadUInt16BE();

                            if (status == 0 && animation == 0 && frame == 0)
                            {
                                p.Seek(pos);
                                goto case 0;
                            }

                            var mobile = world.Mobiles.Get(serial);

                            if (mobile != null)
                            {
                                mobile.SetAnimation(
                                    Mobile.GetReplacedObjectAnimation(mobile.Graphic, animation)
                                );
                                mobile.ExecuteAnimation = false;
                                mobile.AnimIndex = (byte)frame;
                            }
                        }
                        else if (world.Player != null && serial == world.Player)
                        {
                            p.Seek(pos);
                            goto case 2;
                        }

                        break;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x1B: // new spellbook content
                p.Skip(2);
                Item spellbook = world.GetOrCreateItem(p.ReadUInt32BE());
                spellbook.Graphic = p.ReadUInt16BE();
                spellbook.Clear();
                ushort type = p.ReadUInt16BE();

                for (int j = 0; j < 2; j++)
                {
                    uint spells = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        spells |= (uint)(p.ReadUInt8() << (i * 8));
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        if ((spells & (1 << i)) != 0)
                        {
                            ushort cc = (ushort)(j * 32 + i + 1);
                            // FIXME: should i call Item.Create ?
                            Item spellItem = Item.Create(world, cc); // new Item()
                            spellItem.Serial = cc;
                            spellItem.Graphic = 0x1F2E;
                            spellItem.Amount = cc;
                            spellItem.Container = spellbook;
                            spellbook.PushToBack(spellItem);
                        }
                    }
                }

                ServiceProvider.Get<GuiService>().GetGump<SpellbookGump>(spellbook)?.RequestUpdateContents();

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x1D: // house revision state
                serial = p.ReadUInt32BE();
                uint revision = p.ReadUInt32BE();

                var multi = world.Items.Get(serial);

                if (multi == null)
                {
                    ServiceProvider.Get<ManagersService>().HouseManager.Remove(serial);
                }

                if (
                    !ServiceProvider.Get<ManagersService>().HouseManager.TryGetHouse(serial, out var house)
                    || !house.IsCustom
                    || house.Revision != revision
                )
                {
                    _customHouseRequests.Add(serial);
                }
                else
                {
                    house.Generate();
                    ServiceProvider.Get<ManagersService>().BoatMovingManager.ClearSteps(serial);

                    ServiceProvider.Get<GuiService>().GetGump<MiniMapGump>()?.RequestUpdateContents();

                    if (ServiceProvider.Get<ManagersService>().HouseManager.EntityIntoHouse(serial, world.Player))
                    {
                        ServiceProvider.Get<SceneService>().GetScene<GameScene>()?.UpdateMaxDrawZ(true);
                    }
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x20:
                serial = p.ReadUInt32BE();
                type = p.ReadUInt8();
                ushort graphic = p.ReadUInt16BE();
                ushort x = p.ReadUInt16BE();
                ushort y = p.ReadUInt16BE();
                sbyte z = p.ReadInt8();

                switch (type)
                {
                    case 1: // update
                        break;

                    case 2: // remove
                        break;

                    case 3: // update multi pos
                        break;

                    case 4: // begin
                        var gump = ServiceProvider.Get<GuiService>().GetGump<HouseCustomizationGump>();

                        if (gump != null)
                        {
                            break;
                        }

                        gump = new HouseCustomizationGump(world, serial, 50, 50);
                        ServiceProvider.Get<GuiService>().Add(gump);

                        break;

                    case 5: // end
                        ServiceProvider.Get<GuiService>().GetGump<HouseCustomizationGump>(serial)?.Dispose();

                        break;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x21:

                for (int i = 0; i < 2; i++)
                {
                    world.Player.Abilities[i] &= (Ability)0x7F;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x22:
                p.Skip(1);

                var en = world.Get(p.ReadUInt32BE());

                if (en != null)
                {
                    byte damage = p.ReadUInt8();

                    if (damage > 0)
                    {
                        world.WorldTextManager.AddDamage(en, damage);
                    }
                }

                break;

            case 0x25:

                ushort spell = p.ReadUInt16BE();
                bool active = p.ReadBool();

                foreach (Gump g in ServiceProvider.Get<GuiService>().Gumps)
                {
                    if (!g.IsDisposed && g.IsVisible)
                    {
                        if (g is UseSpellButtonGump spellButton && spellButton.SpellID == spell)
                        {
                            if (active)
                            {
                                spellButton.Hue = 38;
                                ServiceProvider.Get<ManagersService>().ActiveSpellIcons.Add(spell);
                            }
                            else
                            {
                                spellButton.Hue = 0;
                                ServiceProvider.Get<ManagersService>().ActiveSpellIcons.Remove(spell);
                            }

                            break;
                        }
                    }
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x26:
                byte val = p.ReadUInt8();

                if (val > (int)CharacterSpeedType.FastUnmountAndCantRun)
                {
                    val = 0;
                }

                world.Player.SpeedMode = (CharacterSpeedType)val;

                break;

            case 0x2A:
                bool isfemale = p.ReadBool();
                byte race = p.ReadUInt8();

                ServiceProvider.Get<GuiService>().GetGump<RaceChangeGump>()?.Dispose();
                ServiceProvider.Get<GuiService>().Add(new RaceChangeGump(world, isfemale, race));
                break;

            case 0x2B:
                serial = p.ReadUInt16BE();
                byte animID = p.ReadUInt8();
                byte frameCount = p.ReadUInt8();

                foreach (Mobile m in world.Mobiles.Values)
                {
                    if ((m.Serial & 0xFFFF) == serial)
                    {
                        m.SetAnimation(animID);
                        m.AnimIndex = frameCount;
                        m.ExecuteAnimation = false;

                        break;
                    }
                }

                break;

            case 0xBEEF: // ClassicUO commands

                type = p.ReadUInt16BE();

                break;

            default:
                Log.Warn($"Unhandled 0xBF - sub: 0x{cmd:X2}");

                break;
        }
    }

    public void DisplayClilocString(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        var entity = world.Get(serial);
        ushort graphic = p.ReadUInt16BE();
        MessageType type = (MessageType)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        ushort font = p.ReadUInt16BE();
        uint cliloc = p.ReadUInt32BE();
        AffixType flags = p[0] == 0xCC ? (AffixType)p.ReadUInt8() : 0x00;
        string name = p.ReadASCII(30);
        string affix = p[0] == 0xCC ? p.ReadASCII() : string.Empty;

        string? arguments = null;

        if (cliloc == 1008092 || cliloc == 1005445) // value for "You notify them you don't want to join the party" || "You have been added to the party"
        {
            for (var g = ServiceProvider.Get<GuiService>().Gumps.Last; g != null; g = g.Previous)
            {
                if (g.Value is PartyInviteGump pg)
                {
                    pg.Dispose();
                }
            }
        }

        int remains = p.Remaining;

        if (remains > 0)
        {
            if (p[0] == 0xCC)
            {
                arguments = p.ReadUnicodeBE(remains);
            }
            else
            {
                arguments = p.ReadUnicodeLE(remains / 2);
            }
        }

        string text = ServiceProvider.Get<AssetsService>().Clilocs.Translate((int)cliloc, arguments);

        if (text == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(affix))
        {
            if ((flags & AffixType.Prepend) != 0)
            {
                text = $"{affix}{text}";
            }
            else
            {
                text = $"{text}{affix}";
            }
        }

        if ((flags & AffixType.System) != 0)
        {
            type = MessageType.System;
        }

        if (!ServiceProvider.Get<AssetsService>().Fonts.UnicodeFontExists((byte)font))
        {
            font = 0;
        }

        TextType text_type = TextType.SYSTEM;

        if (
            serial == 0xFFFF_FFFF
            || serial == 0
            || !string.IsNullOrEmpty(name)
                && string.Equals(name, "system", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            // do nothing
        }
        else if (entity != null)
        {
            //entity.Graphic = graphic;
            text_type = TextType.OBJECT;

            if (string.IsNullOrEmpty(entity.Name))
            {
                entity.Name = name;
            }
        }
        else
        {
            if (type == MessageType.Label)
                return;
        }

        ServiceProvider.Get<ManagersService>().MessageManager.HandleMessage(
            entity,
            text,
            name,
            hue,
            type,
            (byte)font,
            text_type,
            true
        );
    }

    public void UnicodePrompt(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        ServiceProvider.Get<ManagersService>().MessageManager.PromptData = new PromptData
        {
            Prompt = ConsolePrompt.Unicode,
            Data = p.ReadUInt64BE()
        };
    }

    public void Semivisible(ref StackDataReader p) { }

    public void InvalidMapEnable(ref StackDataReader p) { }

    public void ParticleEffect3D(ref StackDataReader p) { }

    public void GetUserServerPingGodClientR(ref StackDataReader p) { }

    public void GlobalQueCount(ref StackDataReader p) { }

    public void ConfigurationFileR(ref StackDataReader p) { }

    public void Logout(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        // http://docs.polserver.com/packets/index.php?Packet=0xD1

        if (
            ServiceProvider.Get<SceneService>().GetScene<GameScene>().DisconnectionRequested
            && (
                world.ClientFeatures.Flags
                & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON
            ) != 0
        )
        {
            if (p.ReadBool())
            {
                // client can disconnect
                ServiceProvider.Get<NetClientService>().Disconnect();
                ServiceProvider.Get<SceneService>().SetScene(new LoginScene(world));
            }
            else
            {
                Log.Warn("0x1D - client asked to disconnect but server answered 'NO!'");
            }
        }
    }

    public void MegaCliloc(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        ushort unknown = p.ReadUInt16BE();

        if (unknown > 1)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();

        p.Skip(2);

        uint revision = p.ReadUInt32BE();

        Entity? entity = world.Mobiles.Get(serial);

        if (entity == null)
        {
            if (SerialHelper.IsMobile(serial))
            {
                Log.Warn("Searching a mobile into world.Items from MegaCliloc packet");
            }

            entity = world.Items.Get(serial);
        }

        List<(int, string, int)> list = new List<(int, string, int)>();
        int totalLength = 0;

        while (p.Position < p.Length)
        {
            int cliloc = (int)p.ReadUInt32BE();

            if (cliloc == 0)
            {
                break;
            }

            ushort length = p.ReadUInt16BE();

            string argument = string.Empty;

            if (length != 0)
            {
                argument = p.ReadUnicodeLE(length / 2);
            }

            string str = ServiceProvider.Get<AssetsService>().Clilocs.Translate(cliloc, argument, true);

            if (str == null)
            {
                continue;
            }

            int argcliloc = 0;

            string[] argcheck = argument.Split(
                new[] { '#' },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (argcheck.Length == 2)
            {
                int.TryParse(argcheck[1], out argcliloc);
            }

            // hardcoded colors lol
            switch (cliloc)
            {
                case 1080418:
                    if (ServiceProvider.Get<UOService>().Version >= ClassicUO.Sdk.ClientVersion.CV_60143)
                        str = "<basefont color=#40a4fe>" + str + "</basefont>";
                    break;
                case 1061170:
                    if (int.TryParse(argument, out var strength) && world.Player.Strength < strength)
                        str = "<basefont color=#FF0000>" + str + "</basefont>";
                    break;
                case 1062613:
                    str = "<basefont color=#FFCC33>" + str + "</basefont>";
                    break;
                case 1159561:
                    str = "<basefont color=#b66dff>" + str + "</basefont>";
                    break;
            }


            for (int i = 0; i < list.Count; i++)
            {
                if (
                    list[i].Item1 == cliloc
                    && string.Equals(list[i].Item2, str, StringComparison.Ordinal)
                )
                {
                    list.RemoveAt(i);

                    break;
                }
            }

            list.Add((cliloc, str, argcliloc));

            totalLength += str.Length;
        }

        Item? container = null;

        if (entity is Item it && SerialHelper.IsValid(it.Container))
        {
            container = world.Items.Get(it.Container);
        }

        bool inBuyList = false;

        if (container != null)
        {
            inBuyList =
                container.Layer == Layer.ShopBuy
                || container.Layer == Layer.ShopBuyRestock
                || container.Layer == Layer.ShopSell;
        }

        bool first = true;

        string name = string.Empty;
        string data = string.Empty;
        int namecliloc = 0;

        if (list.Count != 0)
        {
            Span<char> span = stackalloc char[totalLength];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            foreach (var s in list)
            {
                string str = s.Item2;

                if (first)
                {
                    name = str;

                    if (entity != null && !SerialHelper.IsMobile(serial))
                    {
                        entity.Name = str;
                        namecliloc = s.Item3 > 0 ? s.Item3 : s.Item1;
                    }

                    first = false;
                }
                else
                {
                    if (sb.Length != 0)
                    {
                        sb.Append('\n');
                    }

                    sb.Append(str);
                }
            }

            data = sb.ToString();

            sb.Dispose();
        }

        ServiceProvider.Get<ManagersService>().OPL.Add(serial, revision, name, data, namecliloc);

        if (inBuyList && container != null && SerialHelper.IsValid(container.Serial) && entity is Item itt)
        {
            ServiceProvider.Get<GuiService>().GetGump<ShopGump>(container.RootContainer)?.SetNameTo(itt, name);
        }
    }

    public void GenericAOSCommandsR(ref StackDataReader p) { }

    public unsafe void ReadUnsafeCustomHouseData(
        ReadOnlySpan<byte> source,
        int sourcePosition,
        int dlen,
        int clen,
        int planeZ,
        int planeMode,
        short minX,
        short minY,
        short maxY,
        Item item,
        House house
    )
    {
        //byte* decompressedBytes = stackalloc byte[dlen];
        bool ismovable = item.ItemData.IsMultiMovable;

        byte[]? buffer = null;
        Span<byte> span =
            dlen <= 1024
                ? stackalloc byte[dlen]
                : (buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen));

        try
        {
            Decompressor.Zlib(source.Slice(sourcePosition, clen), span.Slice(0, dlen));
            var reader = new StackDataReader(span.Slice(0, dlen));

            ushort id = 0;
            sbyte x = 0,
                y = 0,
                z = 0;

            switch (planeMode)
            {
                case 0:
                    int c = dlen / 5;

                    for (uint i = 0; i < c; i++)
                    {
                        id = reader.ReadUInt16BE();
                        x = reader.ReadInt8();
                        y = reader.ReadInt8();
                        z = reader.ReadInt8();

                        if (id != 0)
                        {
                            house.Add(
                                id,
                                0,
                                (ushort)(item.X + x),
                                (ushort)(item.Y + y),
                                (sbyte)(item.Z + z),
                                true,
                                ismovable
                            );
                        }
                    }

                    break;

                case 1:

                    if (planeZ > 0)
                    {
                        z = (sbyte)((planeZ - 1) % 4 * 20 + 7);
                    }
                    else
                    {
                        z = 0;
                    }

                    c = dlen >> 2;

                    for (uint i = 0; i < c; i++)
                    {
                        id = reader.ReadUInt16BE();
                        x = reader.ReadInt8();
                        y = reader.ReadInt8();

                        if (id != 0)
                        {
                            house.Add(
                                id,
                                0,
                                (ushort)(item.X + x),
                                (ushort)(item.Y + y),
                                (sbyte)(item.Z + z),
                                true,
                                ismovable
                            );
                        }
                    }

                    break;

                case 2:
                    short offX = 0,
                        offY = 0;
                    short multiHeight = 0;

                    if (planeZ > 0)
                    {
                        z = (sbyte)((planeZ - 1) % 4 * 20 + 7);
                    }
                    else
                    {
                        z = 0;
                    }

                    if (planeZ <= 0)
                    {
                        offX = minX;
                        offY = minY;
                        multiHeight = (short)(maxY - minY + 2);
                    }
                    else if (planeZ <= 4)
                    {
                        offX = (short)(minX + 1);
                        offY = (short)(minY + 1);
                        multiHeight = (short)(maxY - minY);
                    }
                    else
                    {
                        offX = minX;
                        offY = minY;
                        multiHeight = (short)(maxY - minY + 1);
                    }

                    c = dlen >> 1;

                    for (uint i = 0; i < c; i++)
                    {
                        id = reader.ReadUInt16BE();
                        x = (sbyte)(i / multiHeight + offX);
                        y = (sbyte)(i % multiHeight + offY);

                        if (id != 0)
                        {
                            house.Add(
                                id,
                                0,
                                (ushort)(item.X + x),
                                (ushort)(item.Y + y),
                                (sbyte)(item.Z + z),
                                true,
                                ismovable
                            );
                        }
                    }

                    break;
            }

            reader.Release();
        }
        finally
        {
            if (buffer != null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    public void CustomHouse(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        bool compressed = p.ReadUInt8() == 0x03;
        bool enableReponse = p.ReadBool();
        uint serial = p.ReadUInt32BE();
        var foundation = world.Items.Get(serial);
        uint revision = p.ReadUInt32BE();

        if (foundation == null)
        {
            return;
        }

        Rectangle? multi = foundation.MultiInfo;

        if (!foundation.IsMulti || multi == null)
        {
            return;
        }

        p.Skip(4);

        if (!ServiceProvider.Get<ManagersService>().HouseManager.TryGetHouse(foundation, out var house))
        {
            house = new House(world, foundation, revision, true);
            ServiceProvider.Get<ManagersService>().HouseManager.Add(foundation, house);
        }
        else
        {
            house.ClearComponents(true);
            house.Revision = revision;
            house.IsCustom = true;
        }

        short minX = (short)multi.Value.X;
        short minY = (short)multi.Value.Y;
        short maxY = (short)multi.Value.Height;

        if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
        {
            Log.Warn(
                "[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files"
            );

            return;
        }

        byte planes = p.ReadUInt8();

        house.ClearCustomHouseComponents(0);

        for (int plane = 0; plane < planes; plane++)
        {
            uint header = p.ReadUInt32BE();
            int dlen = (int)(((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
            int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
            int planeZ = (int)((header & 0x0F000000) >> 24);
            int planeMode = (int)((header & 0xF0000000) >> 28);

            if (clen <= 0)
            {
                continue;
            }

            ReadUnsafeCustomHouseData(
                p.Buffer,
                p.Position,
                dlen,
                clen,
                planeZ,
                planeMode,
                minX,
                minY,
                maxY,
                foundation,
                house
            );

            p.Skip(clen);
        }

        if (world.CustomHouseManager != null)
        {
            world.CustomHouseManager.GenerateFloorPlace();

            ServiceProvider.Get<GuiService>().GetGump<HouseCustomizationGump>(house.Serial)?.Update();
        }

        ServiceProvider.Get<GuiService>().GetGump<MiniMapGump>()?.RequestUpdateContents();

        if (ServiceProvider.Get<ManagersService>().HouseManager.EntityIntoHouse(serial, world.Player))
        {
            ServiceProvider.Get<SceneService>().GetScene<GameScene>()?.UpdateMaxDrawZ(true);
        }

        ServiceProvider.Get<ManagersService>().BoatMovingManager.ClearSteps(serial);
    }

    public void CharacterTransferLog(ref StackDataReader p) { }

    public void OPLInfo(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.ClientFeatures.TooltipsEnabled)
        {
            uint serial = p.ReadUInt32BE();
            uint revision = p.ReadUInt32BE();

            if (!ServiceProvider.Get<ManagersService>().OPL.IsRevisionEquals(serial, revision))
            {
                ServiceProvider.Get<MegaClilocRequestsService>().AddMegaClilocRequest(serial);
            }
        }
    }

    public void OpenCompressedGump(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint sender = p.ReadUInt32BE();
        uint gumpID = p.ReadUInt32BE();
        uint x = p.ReadUInt32BE();
        uint y = p.ReadUInt32BE();
        uint clen = p.ReadUInt32BE() - 4;
        int dlen = (int)p.ReadUInt32BE();
        byte[] decData = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen);
        string layout;

        try
        {
            Decompressor.Zlib(p.Buffer.Slice(p.Position, (int)clen), decData.AsSpan(0, dlen));
            layout = Encoding.UTF8.GetString(decData.AsSpan(0, dlen));
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(decData);
        }

        p.Skip((int)clen);

        uint linesNum = p.ReadUInt32BE();
        string[] lines = new string[linesNum];

        try
        {
            if (linesNum != 0)
            {
                clen = p.ReadUInt32BE() - 4;
                dlen = (int)p.ReadUInt32BE();
                decData = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen);

                try
                {
                    Decompressor.Zlib(p.Buffer.Slice(p.Position, (int)clen), decData.AsSpan(0, dlen));
                    p.Skip((int)clen);

                    var reader = new StackDataReader(decData.AsSpan(0, dlen));

                    for (int i = 0; i < linesNum; ++i)
                    {
                        int remaining = reader.Remaining;

                        if (remaining >= 2)
                        {
                            int length = reader.ReadUInt16BE();

                            if (length > 0)
                            {
                                lines[i] = reader.ReadUnicodeBE(length);
                            }
                            else
                            {
                                lines[i] = string.Empty;
                            }
                        }
                        else
                        {
                            lines[i] = string.Empty;
                        }
                    }

                    reader.Release();

                    //for (int i = 0, index = 0; i < linesNum && index < dlen; i++)
                    //{
                    //    int length = ((decData[index++] << 8) | decData[index++]) << 1;
                    //    int true_length = 0;

                    //    for (int k = 0; k < length && true_length < length && index + true_length < dlen; ++k, true_length += 2)
                    //    {
                    //        ushort c = (ushort)(((decData[index + true_length] << 8) | decData[index + true_length + 1]) << 1);

                    //        if (c == '\0')
                    //        {
                    //            break;
                    //        }
                    //    }

                    //    lines[i] = Encoding.BigEndianUnicode.GetString(decData, index, true_length);

                    //    index += length;
                    //}
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(decData);
                }
            }

            CreateGump(world, sender, gumpID, (int)x, (int)y, layout, lines);
        }
        finally
        {
            //System.Buffers.ArrayPool<string>.Shared.Return(lines);
        }
    }

    public void UpdateMobileStatus(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();
        byte status = p.ReadUInt8();

        if (status == 1)
        {
            uint attackerSerial = p.ReadUInt32BE();
        }
    }

    public void BuffDebuff(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        const ushort BUFF_ICON_START = 0x03E9;
        const ushort BUFF_ICON_START_NEW = 0x466;

        uint serial = p.ReadUInt32BE();
        BuffIconType ic = (BuffIconType)p.ReadUInt16BE();

        ushort iconID =
            (ushort)ic >= BUFF_ICON_START_NEW
                ? (ushort)(ic - (BUFF_ICON_START_NEW - 125))
                : (ushort)((ushort)ic - BUFF_ICON_START);

        if (iconID < BuffTable.Table.Length)
        {
            var gump = ServiceProvider.Get<GuiService>().GetGump<BuffGump>();
            ushort count = p.ReadUInt16BE();

            if (count == 0)
            {
                world.Player.RemoveBuff(ic);
                gump?.RequestUpdateContents();
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    ushort source_type = p.ReadUInt16BE();
                    p.Skip(2);
                    ushort icon = p.ReadUInt16BE();
                    ushort queue_index = p.ReadUInt16BE();
                    p.Skip(4);
                    ushort timer = p.ReadUInt16BE();
                    p.Skip(3);

                    uint titleCliloc = p.ReadUInt32BE();
                    uint descriptionCliloc = p.ReadUInt32BE();
                    uint wtfCliloc = p.ReadUInt32BE();

                    ushort arg_length = p.ReadUInt16BE();
                    var str = p.ReadUnicodeLE(2);
                    var args = str + p.ReadUnicodeLE();
                    string title = ServiceProvider.Get<AssetsService>().Clilocs.Translate(
                        (int)titleCliloc,
                        args,
                        true
                    );

                    arg_length = p.ReadUInt16BE();
                    string args_2 = p.ReadUnicodeLE();
                    string description = string.Empty;

                    if (descriptionCliloc != 0)
                    {
                        description =
                            "\n"
                            + ServiceProvider.Get<AssetsService>().Clilocs.Translate(
                                (int)descriptionCliloc,
                                String.IsNullOrEmpty(args_2) ? args : args_2,
                                true
                            );

                        if (description.Length < 2)
                        {
                            description = string.Empty;
                        }
                    }

                    arg_length = p.ReadUInt16BE();
                    string args_3 = p.ReadUnicodeLE();
                    string wtf = string.Empty;

                    if (wtfCliloc != 0)
                    {
                        wtf = ServiceProvider.Get<AssetsService>().Clilocs.Translate(
                            (int)wtfCliloc,
                            String.IsNullOrEmpty(args_3) ? args : args_3,
                            true
                        );

                        if (!string.IsNullOrWhiteSpace(wtf))
                        {
                            wtf = $"\n{wtf}";
                        }
                    }

                    string text = $"<left>{title}{description}{wtf}</left>";
                    bool alreadyExists = world.Player.IsBuffIconExists(ic);
                    world.Player.AddBuff(ic, BuffTable.Table[iconID], timer, text);

                    if (!alreadyExists)
                    {
                        gump?.RequestUpdateContents();
                    }
                }
            }
        }
    }

    public void NewCharacterAnimation(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        var mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
        {
            return;
        }

        ushort type = p.ReadUInt16BE();
        ushort action = p.ReadUInt16BE();
        byte mode = p.ReadUInt8();
        byte group = Mobile.GetObjectNewAnimation(mobile, type, action, mode);

        mobile.SetAnimation(
            group,
            repeatCount: 1,
            repeat: (type == 1 || type == 2) && mobile.Graphic == 0x0015,
            forward: true,
            fromServer: true
        );
    }

    public void KREncryptionResponse(ref StackDataReader p) { }

    public void DisplayWaypoint(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = p.ReadInt8();
        byte map = p.ReadUInt8();
        WaypointsType type = (WaypointsType)p.ReadUInt16BE();
        bool ignoreobject = p.ReadUInt16BE() != 0;
        uint cliloc = p.ReadUInt32BE();
        string name = p.ReadUnicodeLE();
    }

    public void RemoveWaypoint(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        uint serial = p.ReadUInt32BE();
    }

    public void KrriosClientSpecial(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        byte type = p.ReadUInt8();

        switch (type)
        {
            case 0x00: // accepted
                Log.Trace("Krrios special packet accepted");
                ServiceProvider.Get<ManagersService>().WMapManager.SetACKReceived();
                ServiceProvider.Get<ManagersService>().WMapManager.SetEnable(true);

                break;

            case 0x01: // custom party info
            case 0x02: // guild track info
                bool locations = type == 0x01 || p.ReadBool();

                uint serial;

                while ((serial = p.ReadUInt32BE()) != 0)
                {
                    if (locations)
                    {
                        ushort x = p.ReadUInt16BE();
                        ushort y = p.ReadUInt16BE();
                        byte map = p.ReadUInt8();
                        int hits = type == 1 ? 0 : p.ReadUInt8();

                        ServiceProvider.Get<ManagersService>().WMapManager.AddOrUpdate(
                            serial,
                            x,
                            y,
                            hits,
                            map,
                            type == 0x02,
                            null,
                            true
                        );
                    }
                }

                ServiceProvider.Get<ManagersService>().WMapManager.RemoveUnupdatedWEntity();

                break;

            case 0x03: // runebook contents
                break;

            case 0x04: // guardline data
                break;

            case 0xF0:
                break;

            case 0xFE:

                ServiceProvider.Get<EngineService>().EnqueueAction(5000, () =>
                {
                    Log.Info("Razor ACK sent");
                    ServiceProvider.Get<PacketHandlerService>().Out.Send_RazorACK();
                });

                break;
        }
    }

    public void FreeshardListR(ref StackDataReader p) { }

    public void UpdateItemSA(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        p.Skip(2);
        byte type = p.ReadUInt8();
        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();
        byte graphicInc = p.ReadUInt8();
        ushort amount = p.ReadUInt16BE();
        ushort unk = p.ReadUInt16BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = p.ReadInt8();
        Direction dir = (Direction)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        Flags flags = (Flags)p.ReadUInt8();
        ushort unk2 = p.ReadUInt16BE();

        if (serial != world.Player)
        {
            UpdateGameObject(
                world,
                serial,
                graphic,
                graphicInc,
                amount,
                x,
                y,
                z,
                dir,
                hue,
                flags,
                unk,
                type,
                unk2
            );

            if (graphic == 0x2006 && ProfileManager.CurrentProfile.AutoOpenCorpses)
            {
                world.Player.TryOpenCorpses();
            }
        }
        else if (p[0] == 0xF7)
        {
            UpdatePlayer(world, serial, graphic, graphicInc, hue, flags, x, y, z, 0, dir);
        }
    }

    public void BoatMoving(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (!world.InGame)
        {
            return;
        }

        uint serial = p.ReadUInt32BE();
        byte boatSpeed = p.ReadUInt8();
        Direction movingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
        Direction facingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        ushort z = p.ReadUInt16BE();

        var multi = world.Items.Get(serial);

        if (multi == null)
        {
            return;
        }

        //multi.LastX = x;
        //multi.LastY = y;

        //if (ServiceProvider.Get<ManagersService>().HouseManager.TryGetHouse(serial, out var house))
        //{
        //    foreach (Multi component in house.Components)
        //    {
        //        component.LastX = (ushort) (x + component.MultiOffsetX);
        //        component.LastY = (ushort) (y + component.MultiOffsetY);
        //    }
        //}

        bool smooth =
            ProfileManager.CurrentProfile != null
            && ProfileManager.CurrentProfile.UseSmoothBoatMovement;

        if (smooth)
        {
            ServiceProvider.Get<ManagersService>().BoatMovingManager.AddStep(
                serial,
                boatSpeed,
                movingDirection,
                facingDirection,
                x,
                y,
                (sbyte)z
            );
        }
        else
        {
            //UpdateGameObject(serial,
            //                 multi.Graphic,
            //                 0,
            //                 multi.Amount,
            //                 x,
            //                 y,
            //                 (sbyte) z,
            //                 facingDirection,
            //                 multi.Hue,
            //                 multi.Flags,
            //                 0,
            //                 2,
            //                 1);

            multi.SetInWorldTile(x, y, (sbyte)z);

            if (ServiceProvider.Get<ManagersService>().HouseManager.TryGetHouse(serial, out var house))
            {
                house.Generate(true, true, true);
            }
        }

        int count = p.ReadUInt16BE();

        for (int i = 0; i < count; i++)
        {
            uint cSerial = p.ReadUInt32BE();
            ushort cx = p.ReadUInt16BE();
            ushort cy = p.ReadUInt16BE();
            ushort cz = p.ReadUInt16BE();

            if (cSerial == world.Player)
            {
                world.RangeSize.X = cx;
                world.RangeSize.Y = cy;
            }

            var ent = world.Get(cSerial);

            if (ent == null)
            {
                continue;
            }

            //if (SerialHelper.IsMobile(cSerial))
            //{
            //    Mobile m = (Mobile) ent;

            //    if (m.Steps.Count != 0)
            //    {
            //        ref var step = ref m.Steps.Back();

            //        step.X = cx;
            //        step.Y = cy;
            //    }
            //}

            //ent.LastX = cx;
            //ent.LastY = cy;

            if (smooth)
            {
                ServiceProvider.Get<ManagersService>().BoatMovingManager.PushItemToList(
                    serial,
                    cSerial,
                    x - cx,
                    y - cy,
                    (sbyte)(z - cz)
                );
            }
            else
            {
                if (cSerial == world.Player)
                {
                    UpdatePlayer(
                        world,
                        cSerial,
                        ent.Graphic,
                        0,
                        ent.Hue,
                        ent.Flags,
                        cx,
                        cy,
                        (sbyte)cz,
                        0,
                        world.Player.Direction
                    );
                }
                else
                {
                    UpdateGameObject(
                        world,
                        cSerial,
                        ent.Graphic,
                        0,
                        (ushort)(ent.Graphic == 0x2006 ? ((Item)ent).Amount : 0),
                        cx,
                        cy,
                        (sbyte)cz,
                        SerialHelper.IsMobile(ent) ? ent.Direction : 0,
                        ent.Hue,
                        ent.Flags,
                        0,
                        0,
                        1
                    );
                }
            }
        }
    }

    public void PacketList(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.Player == null)
        {
            return;
        }

        int count = p.ReadUInt16BE();

        for (int i = 0; i < count; i++)
        {
            byte id = p.ReadUInt8();

            if (id == 0xF3)
            {
                UpdateItemSA(ref p);
            }
            else
            {
                Log.Warn($"Unknown packet ID: [0x{id:X2}] in 0xF7");

                break;
            }
        }
    }

    public void ServerListReceived(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.InGame)
        {
            return;
        }

        var scene = ServiceProvider.Get<SceneService>().GetScene<LoginScene>();

        if (scene != null)
        {
            scene.ServerListReceived(ref p);
        }
    }

    public void ReceiveServerRelay(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.InGame)
        {
            return;
        }

        var scene = ServiceProvider.Get<SceneService>().GetScene<LoginScene>();

        if (scene != null)
        {
            scene.HandleRelayServerPacket(ref p);
        }
    }

    public void UpdateCharacterList(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.InGame)
        {
            return;
        }

        var scene = ServiceProvider.Get<SceneService>().GetScene<LoginScene>();

        if (scene != null)
        {
            scene.UpdateCharacterList(ref p);
        }
    }

    public void ReceiveCharacterList(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.InGame)
        {
            return;
        }

        var scene = ServiceProvider.Get<SceneService>().GetScene<LoginScene>();

        if (scene != null)
        {
            scene.ReceiveCharacterList(ref p);
        }
    }

    public void LoginDelay(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.InGame)
        {
            return;
        }

        var scene = ServiceProvider.Get<SceneService>().GetScene<LoginScene>();

        if (scene != null)
        {
            scene.HandleLoginDelayPacket(ref p);
        }
    }

    public void ReceiveLoginRejection(ref StackDataReader p)
    {
        var world = ServiceProvider.Get<WorldService>().World;
        if (world.InGame)
        {
            return;
        }

        var scene = ServiceProvider.Get<SceneService>().GetScene<LoginScene>();

        if (scene != null)
        {
            scene.HandleErrorCode(ref p);
        }
    }

    public void AddItemToContainer(
        World world,
        uint serial,
        ushort graphic,
        ushort amount,
        ushort x,
        ushort y,
        ushort hue,
        uint containerSerial
    )
    {
        var cursorService = ServiceProvider.Get<GameCursorService>();

        if (cursorService.GameCursor.ItemHold.Serial == serial)
        {
            if (cursorService.GameCursor.ItemHold.Dropped)
            {
                Console.WriteLine("ADD ITEM TO CONTAINER -- CLEAR HOLD");
                cursorService.GameCursor.ItemHold.Clear();
            }

            //else if (ItemHold.Graphic == graphic && ItemHold.Amount == amount &&
            //         ItemHold.Container == containerSerial)
            //{
            //    ItemHold.Enabled = false;
            //    ItemHold.Dropped = false;
            //}
        }

        var container = world.Get(containerSerial);

        if (container == null)
        {
            Log.Warn($"No container ({containerSerial}) found");

            //container = world.GetOrCreateItem(containerSerial);
            return;
        }

        var item = world.Items.Get(serial);

        if (SerialHelper.IsMobile(serial))
        {
            world.RemoveMobile(serial, true);
            Log.Warn("AddItemToContainer function adds mobile as Item");
        }

        if (item != null && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid))
        {
            world.RemoveItem(item, true);
        }

        item = world.GetOrCreateItem(serial);
        item.Graphic = graphic;
        item.CheckGraphicChange();
        item.Amount = amount;
        item.FixHue(hue);
        item.X = x;
        item.Y = y;
        item.Z = 0;

        world.RemoveItemFromContainer(item);
        item.Container = containerSerial;
        container.PushToBack(item);

        if (SerialHelper.IsMobile(containerSerial))
        {
            var m = world.Mobiles.Get(containerSerial);
            var secureBox = m?.GetSecureTradeBox();

            if (secureBox != null)
            {
                ServiceProvider.Get<GuiService>().GetTradingGump(secureBox)?.RequestUpdateContents();
            }
            else
            {
                ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
            }
        }
        else if (SerialHelper.IsItem(containerSerial))
        {
            Gump? gump = ServiceProvider.Get<GuiService>().GetGump<BulletinBoardGump>(containerSerial);

            if (gump != null)
            {
                ServiceProvider.Get<PacketHandlerService>().Out.Send_BulletinBoardRequestMessageSummary(
                    containerSerial,
                    serial
                );
            }
            else
            {
                gump = ServiceProvider.Get<GuiService>().GetGump<SpellbookGump>(containerSerial);

                if (gump == null)
                {
                    gump = ServiceProvider.Get<GuiService>().GetGump<ContainerGump>(containerSerial);

                    if (gump != null)
                    {
                        ((ContainerGump)gump).CheckItemControlPosition(item);
                    }

                    if (ProfileManager.CurrentProfile.GridLootType > 0)
                    {
                        var grid_gump = ServiceProvider.Get<GuiService>().GetGump<GridLootGump>(
                            containerSerial
                        );

                        if (
                            grid_gump == null
                            && SerialHelper.IsValid(_requestedGridLoot)
                            && _requestedGridLoot == containerSerial
                        )
                        {
                            grid_gump = new GridLootGump(world, _requestedGridLoot);
                            ServiceProvider.Get<GuiService>().Add(grid_gump);
                            _requestedGridLoot = 0;
                        }

                        grid_gump?.RequestUpdateContents();
                    }
                }

                if (gump != null)
                {
                    if (SerialHelper.IsItem(containerSerial))
                    {
                        ((Item)container).Opened = true;
                    }

                    gump.RequestUpdateContents();
                }
            }
        }

        ServiceProvider.Get<GuiService>().GetTradingGump(containerSerial)?.RequestUpdateContents();
    }

    public void UpdateGameObject(
        World world,
        uint serial,
        ushort graphic,
        byte graphic_inc,
        ushort count,
        ushort x,
        ushort y,
        sbyte z,
        Direction direction,
        ushort hue,
        Flags flagss,
        int UNK,
        byte type,
        ushort UNK_2
    )
    {
        Mobile? mobile = null;
        Item? item = null;
        Entity? obj = world.Get(serial);
        var cursorService = ServiceProvider.Get<GameCursorService>();

        if (
            cursorService.GameCursor.ItemHold.Enabled
            && cursorService.GameCursor.ItemHold.Serial == serial
        )
        {
            if (SerialHelper.IsValid(cursorService.GameCursor.ItemHold.Container))
            {
                if (cursorService.GameCursor.ItemHold.Layer == 0)
                {
                    ServiceProvider.Get<GuiService>()
                        .GetGump<ContainerGump>(cursorService.GameCursor.ItemHold.Container)
                        ?.RequestUpdateContents();
                }
                else
                {
                    ServiceProvider.Get<GuiService>()
                        .GetGump<PaperDollGump>(cursorService.GameCursor.ItemHold.Container)
                        ?.RequestUpdateContents();
                }
            }

            cursorService.GameCursor.ItemHold.UpdatedInWorld = true;
        }

        bool created = false;

        if (obj == null || obj.IsDestroyed)
        {
            created = true;

            if (SerialHelper.IsMobile(serial) && type != 3)
            {
                mobile = world.GetOrCreateMobile(serial);

                if (mobile == null)
                {
                    return;
                }

                obj = mobile;
                mobile.Graphic = (ushort)(graphic + graphic_inc);
                mobile.CheckGraphicChange();
                mobile.Direction = direction & Direction.Up;
                mobile.FixHue(hue);
                mobile.X = x;
                mobile.Y = y;
                mobile.Z = z;
                mobile.Flags = flagss;
            }
            else
            {
                item = world.GetOrCreateItem(serial);

                if (item == null)
                {
                    return;
                }

                obj = item;
            }
        }
        else
        {
            if (obj is Item item1)
            {
                item = item1;

                if (SerialHelper.IsValid(item.Container))
                {
                    world.RemoveItemFromContainer(item);
                }
            }
            else
            {
                mobile = (Mobile)obj;
            }
        }

        if (obj == null)
        {
            return;
        }

        if (item != null)
        {
            if (graphic != 0x2006)
            {
                graphic += graphic_inc;
            }

            if (type == 2)
            {
                item.IsMulti = true;
                item.WantUpdateMulti =
                    (graphic & 0x3FFF) != item.Graphic
                    || item.X != x
                    || item.Y != y
                    || item.Z != z
                    || item.Hue != hue;
                item.Graphic = (ushort)(graphic & 0x3FFF);
            }
            else
            {
                item.IsDamageable = type == 3;
                item.IsMulti = false;
                item.Graphic = graphic;
            }

            item.X = x;
            item.Y = y;
            item.Z = z;
            item.LightID = (byte)direction;

            if (graphic == 0x2006)
            {
                item.Layer = (Layer)direction;
            }

            item.FixHue(hue);

            if (count == 0)
            {
                count = 1;
            }

            item.Amount = count;
            item.Flags = flagss;
            item.Direction = direction;
            item.CheckGraphicChange(item.AnimIndex);
        }
        else if (mobile != null)
        {
            graphic += graphic_inc;

            if (serial != world.Player)
            {
                Direction cleaned_dir = direction & Direction.Up;
                bool isrun = (direction & Direction.Running) != 0;

                if (world.Get(mobile) == null || mobile.X == 0xFFFF && mobile.Y == 0xFFFF)
                {
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.Direction = cleaned_dir;
                    mobile.IsRunning = isrun;
                    mobile.ClearSteps();
                }

                if (!mobile.EnqueueStep(x, y, z, cleaned_dir, isrun))
                {
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.Direction = cleaned_dir;
                    mobile.IsRunning = isrun;
                    mobile.ClearSteps();
                }
            }

            mobile.Graphic = (ushort)(graphic & 0x3FFF);
            mobile.FixHue(hue);
            mobile.Flags = flagss;
        }

        if (created && !obj.IsClicked)
        {
            if (mobile != null)
            {
                if (ProfileManager.CurrentProfile.ShowNewMobileNameIncoming)
                {
                    GameActions.SingleClick(world, serial);
                }
            }
            else if (graphic == 0x2006)
            {
                if (ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming)
                {
                    GameActions.SingleClick(world, serial);
                }
            }
        }

        if (mobile != null)
        {
            mobile.SetInWorldTile(mobile.X, mobile.Y, mobile.Z);

            if (created)
            {
                // This is actually a way to get all Hp from all new mobiles.
                // Real UO client does it only when LastAttack == serial.
                // We force to close suddenly.
                GameActions.RequestMobileStatus(world, serial);

                //if (TargetManager.LastAttack != serial)
                //{
                //    GameActions.SendCloseStatus(serial);
                //}
            }
        }
        else
        {
            if (
                cursorService.GameCursor.ItemHold.Serial == serial
                && cursorService.GameCursor.ItemHold.Dropped
            )
            {
                // we want maintain the item data due to the denymoveitem packet
                //ItemHold.Clear();
                cursorService.GameCursor.ItemHold.Enabled = false;
                cursorService.GameCursor.ItemHold.Dropped = false;
            }

            if (item?.OnGround ?? false)
            {
                item.SetInWorldTile(item.X, item.Y, item.Z);

                if (graphic == 0x2006 && ProfileManager.CurrentProfile.AutoOpenCorpses)
                {
                    world.Player.TryOpenCorpses();
                }
            }
        }
    }

    public void UpdatePlayer(
        World world,
        uint serial,
        ushort graphic,
        byte graph_inc,
        ushort hue,
        Flags flags,
        ushort x,
        ushort y,
        sbyte z,
        ushort serverID,
        Direction direction
    )
    {
        if (serial == world.Player)
        {
            world.RangeSize.X = x;
            world.RangeSize.Y = y;

            bool olddead = world.Player.IsDead;
            ushort old_graphic = world.Player.Graphic;

            world.Player.CloseBank();
            world.Player.Walker.WalkingFailed = false;
            world.Player.Graphic = graphic;
            world.Player.Direction = direction & Direction.Mask;
            world.Player.FixHue(hue);
            world.Player.Flags = flags;
            world.Player.Walker.DenyWalk(0xFF, -1, -1, -1);

            var gs = ServiceProvider.Get<SceneService>().GetScene<GameScene>();

            if (gs != null)
            {
                ServiceProvider.Get<ManagersService>().Weather.Reset();
                gs.UpdateDrawPosition = true;
            }

            // std client keeps the target open!
            /*if (old_graphic != 0 && old_graphic != world.Player.Graphic)
            {
                if (world.Player.IsDead)
                {
                    TargetManager.Reset();
                }
            }*/

            if (olddead != world.Player.IsDead)
            {
                if (world.Player.IsDead)
                {
                    world.ChangeSeason(Game.Managers.Season.Desolation, 42);
                }
                else
                {
                    world.ChangeSeason(world.OldSeason, world.OldMusicIndex);
                }
            }

            world.Player.Walker.ResendPacketResync = false;
            world.Player.CloseRangedGumps();
            world.Player.SetInWorldTile(x, y, z);
            world.Player.UpdateAbilities();
        }
    }

    public void ClearContainerAndRemoveItems(
        World world,
        Entity container,
        bool remove_unequipped = false
    )
    {
        if (container == null || container.IsEmpty)
        {
            return;
        }

        var first = container.Items;
        LinkedObject? new_first = null;

        while (first != null)
        {
            var next = first.Next;
            Item it = (Item)first;

            if (remove_unequipped && it.Layer != 0)
            {
                if (new_first == null)
                {
                    new_first = first;
                }
            }
            else
            {
                world.RemoveItem(it, true);
            }

            first = next;
        }

        container.Items = remove_unequipped ? new_first : null;
    }

    public Gump? CreateGump(
        World world,
        uint sender,
        uint gumpID,
        int x,
        int y,
        string layout,
        string[] lines
    )
    {
        List<string> cmdlist = _parser.GetTokens(layout);
        int cmdlen = cmdlist.Count;

        if (cmdlen <= 0)
        {
            return null;
        }

        Gump? gump = null;
        bool mustBeAdded = true;

        if (ServiceProvider.Get<GuiService>().GetGumpCachePosition(gumpID, out Point pos))
        {
            x = pos.X;
            y = pos.Y;

            for (
                var last = ServiceProvider.Get<GuiService>().Gumps.Last;
                last != null;
                last = last.Previous
            )
            {
                Control g = last.Value;

                if (!g.IsDisposed && g.LocalSerial == sender && g.ServerSerial == gumpID)
                {
                    g.Clear();
                    gump = g as Gump;
                    mustBeAdded = false;

                    break;
                }
            }
        }
        else
        {
            ServiceProvider.Get<GuiService>().SavePosition(gumpID, new Point(x, y));
        }

        if (gump == null)
        {
            gump = new Gump(world, sender, gumpID)
            {
                X = x,
                Y = y,
                CanMove = true,
                CanCloseWithRightClick = true,
                CanCloseWithEsc = true,
                InvalidateContents = false,
                IsFromServer = true
            };
        }

        int group = 0;
        int page = 0;

        bool textBoxFocused = false;

        for (int cnt = 0; cnt < cmdlen; cnt++)
        {
            List<string> gparams = _cmdparser.GetTokens(cmdlist[cnt], false);

            if (gparams.Count == 0)
            {
                continue;
            }

            string entry = gparams[0];

            if (string.Equals(entry, "button", StringComparison.InvariantCultureIgnoreCase))
            {
                gump.Add(new Button(gparams), page);
            }
            else if (
                string.Equals(
                    entry,
                    "buttontileart",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                gump.Add(new ButtonTileArt(gparams), page);
            }
            else if (
                string.Equals(
                    entry,
                    "checkertrans",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                var checkerTrans = new CheckerTrans(gparams);
                gump.Add(checkerTrans, page);
                ApplyTrans(
                    gump,
                    page,
                    checkerTrans.X,
                    checkerTrans.Y,
                    checkerTrans.Width,
                    checkerTrans.Height
                );
            }
            else if (
                string.Equals(entry, "croppedtext", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.Add(new CroppedText(gparams, lines), page);
            }
            else if (
                string.Equals(entry, "gumppic", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                GumpPic pic;
                var isVirtue = gparams.Count >= 6
                    && gparams[5].IndexOf(
                        "virtuegumpitem",
                        StringComparison.InvariantCultureIgnoreCase
                    ) >= 0;

                if (isVirtue)
                {
                    pic = new VirtueGumpPic(world, gparams);
                    pic.ContainsByBounds = true;

                    string s,
                        lvl;

                    switch (pic.Hue)
                    {
                        case 2403:
                            lvl = "";

                            break;

                        case 1154:
                        case 1547:
                        case 2213:
                        case 235:
                        case 18:
                        case 2210:
                        case 1348:
                            lvl = "Seeker of ";

                            break;

                        case 2404:
                        case 1552:
                        case 2216:
                        case 2302:
                        case 2118:
                        case 618:
                        case 2212:
                        case 1352:
                            lvl = "Follower of ";

                            break;

                        case 43:
                        case 53:
                        case 1153:
                        case 33:
                        case 318:
                        case 67:
                        case 98:
                            lvl = "Knight of ";

                            break;

                        case 2406:
                            if (pic.Graphic == 0x6F)
                            {
                                lvl = "Seeker of ";
                            }
                            else
                            {
                                lvl = "Knight of ";
                            }

                            break;

                        default:
                            lvl = "";

                            break;
                    }

                    switch (pic.Graphic)
                    {
                        case 0x69:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000 + 2);

                            break;

                        case 0x6A:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000 + 7);

                            break;

                        case 0x6B:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000 + 5);

                            break;

                        case 0x6D:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000 + 6);

                            break;

                        case 0x6E:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000 + 1);

                            break;

                        case 0x6F:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000 + 3);

                            break;

                        case 0x70:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000 + 4);

                            break;

                        case 0x6C:
                        default:
                            s = ServiceProvider.Get<AssetsService>().Clilocs.GetString(1051000);

                            break;
                    }

                    if (string.IsNullOrEmpty(s))
                    {
                        s = "Unknown virtue";
                    }

                    pic.SetTooltip(lvl + s, 100);
                }
                else
                {
                    pic = new GumpPic(gparams);
                }

                gump.Add(pic, page);
            }
            else if (
                string.Equals(
                    entry,
                    "gumppictiled",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                gump.Add(new GumpPicTiled(gparams), page);
            }
            else if (
                string.Equals(entry, "htmlgump", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.Add(new HtmlControl(gparams, lines), page);
            }
            else if (
                string.Equals(entry, "xmfhtmlgump", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.Add(
                    new HtmlControl(
                        int.Parse(gparams[1]),
                        int.Parse(gparams[2]),
                        int.Parse(gparams[3]),
                        int.Parse(gparams[4]),
                        int.Parse(gparams[6]) == 1,
                        int.Parse(gparams[7]) != 0,
                        gparams[6] != "0" && gparams[7] == "2",
                        ServiceProvider.Get<AssetsService>().Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                        0,
                        true
                    )
                    {
                        IsFromServer = true
                    },
                    page
                );
            }
            else if (
                string.Equals(
                    entry,
                    "xmfhtmlgumpcolor",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                int color = int.Parse(gparams[8]);

                if (color == 0x7FFF)
                {
                    color = 0x00FFFFFF;
                }

                gump.Add(
                    new HtmlControl(
                        int.Parse(gparams[1]),
                        int.Parse(gparams[2]),
                        int.Parse(gparams[3]),
                        int.Parse(gparams[4]),
                        int.Parse(gparams[6]) == 1,
                        int.Parse(gparams[7]) != 0,
                        gparams[6] != "0" && gparams[7] == "2",
                        ServiceProvider.Get<AssetsService>().Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                        color,
                        true
                    )
                    {
                        IsFromServer = true
                    },
                    page
                );
            }
            else if (
                string.Equals(entry, "xmfhtmltok", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                int color = int.Parse(gparams[7]);

                if (color == 0x7FFF)
                {
                    color = 0x00FFFFFF;
                }

                StringBuilder? sb = null;

                if (gparams.Count >= 9)
                {
                    sb = new StringBuilder();

                    for (int i = 9; i < gparams.Count; i++)
                    {
                        sb.Append('\t');
                        sb.Append(gparams[i]);
                    }
                }

                gump.Add(
                    new HtmlControl(
                        int.Parse(gparams[1]),
                        int.Parse(gparams[2]),
                        int.Parse(gparams[3]),
                        int.Parse(gparams[4]),
                        int.Parse(gparams[5]) == 1,
                        int.Parse(gparams[6]) != 0,
                        gparams[5] != "0" && gparams[6] == "2",
                        sb == null
                            ? ServiceProvider.Get<AssetsService>().Clilocs.GetString(
                                int.Parse(gparams[8].Replace("#", ""))
                            )
                            : ServiceProvider.Get<AssetsService>().Clilocs.Translate(
                                int.Parse(gparams[8].Replace("#", "")),
                                sb.ToString().Trim('@').Replace('@', '\t')
                            ),
                        color,
                        true
                    )
                    {
                        IsFromServer = true
                    },
                    page
                );
            }
            else if (string.Equals(entry, "page", StringComparison.InvariantCultureIgnoreCase))
            {
                if (gparams.Count >= 2)
                {
                    page = int.Parse(gparams[1]);
                }
            }
            else if (
                string.Equals(entry, "resizepic", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.Add(new ResizePic(gparams), page);
            }
            else if (string.Equals(entry, "text", StringComparison.InvariantCultureIgnoreCase))
            {
                if (gparams.Count >= 5)
                {
                    gump.Add(new Label(gparams, lines), page);
                }
            }
            else if (
                string.Equals(
                    entry,
                    "textentrylimited",
                    StringComparison.InvariantCultureIgnoreCase
                )
                || string.Equals(
                    entry,
                    "textentry",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                StbTextBox textBox = new StbTextBox(gparams, lines);

                if (!textBoxFocused)
                {
                    textBox.SetKeyboardFocus();
                    textBoxFocused = true;
                }

                gump.Add(textBox, page);
            }
            else if (
                string.Equals(entry, "tilepichue", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(entry, "tilepic", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.Add(new StaticPic(gparams), page);
            }
            else if (
                string.Equals(entry, "noclose", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.CanCloseWithRightClick = false;
            }
            else if (
                string.Equals(entry, "nodispose", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.CanCloseWithEsc = false;
            }
            else if (
                string.Equals(entry, "nomove", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.CanMove = false;
            }
            else if (
                string.Equals(entry, "group", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(entry, "endgroup", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                group++;
            }
            else if (string.Equals(entry, "radio", StringComparison.InvariantCultureIgnoreCase))
            {
                gump.Add(new RadioButton(group, gparams, lines), page);
            }
            else if (
                string.Equals(entry, "checkbox", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.Add(new Checkbox(gparams, lines), page);
            }
            else if (
                string.Equals(entry, "tooltip", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                string? text;

                if (gparams.Count > 2 && gparams[2].Length != 0)
                {
                    string args = gparams[2];

                    for (int i = 3; i < gparams.Count; i++)
                    {
                        args += '\t' + gparams[i];
                    }

                    if (args.Length == 0)
                    {
                        text = ServiceProvider.Get<AssetsService>().Clilocs.GetString(int.Parse(gparams[1]));
                        Log.Error(
                            $"String '{args}' too short, something wrong with gump tooltip: {text}"
                        );
                    }
                    else
                    {
                        text = ServiceProvider.Get<AssetsService>().Clilocs.Translate(
                            int.Parse(gparams[1]),
                            args,
                            false
                        );
                    }
                }
                else
                {
                    text = ServiceProvider.Get<AssetsService>().Clilocs.GetString(int.Parse(gparams[1]));
                }

                var last =
                    gump.Children.Count != 0 ? gump.Children[gump.Children.Count - 1] : null;

                if (last != null)
                {
                    if (last.HasTooltip)
                    {
                        if (last.Tooltip is string s)
                        {
                            s += '\n' + text;
                            last.SetTooltip(s);
                        }
                    }
                    else
                    {
                        last.SetTooltip(text);
                    }

                    last.Priority = ClickPriority.High;
                    last.AcceptMouseInput = true;
                }
            }
            else if (
                string.Equals(
                    entry,
                    "itemproperty",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                if (world.ClientFeatures.TooltipsEnabled && gump.Children.Count != 0)
                {
                    gump.Children[gump.Children.Count - 1].SetTooltip(
                        SerialHelper.Parse(gparams[1])
                    );

                    if (
                        uint.TryParse(gparams[1], out uint s)
                        && (!ServiceProvider.Get<ManagersService>().OPL.TryGetRevision(s, out uint rev) || rev == 0)
                    )
                    {
                        ServiceProvider.Get<MegaClilocRequestsService>().AddMegaClilocRequest(s);
                    }
                }
            }
            else if (
                string.Equals(entry, "noresize", StringComparison.InvariantCultureIgnoreCase)
            ) { }
            else if (
                string.Equals(entry, "mastergump", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                gump.MasterGumpSerial = gparams.Count > 0 ? SerialHelper.Parse(gparams[1]) : 0;
            }
            else if (
                string.Equals(entry, "picinpic", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                if (gparams.Count > 7)
                {
                    gump.Add(new GumpPicInPic(gparams), page);
                }
            }
            else if (string.Equals(entry, "\0", StringComparison.InvariantCultureIgnoreCase))
            {
                //This gump is null terminated: Breaking
                break;
            }
            else if (string.Equals(entry, "gumppichued", StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(entry, "gumppicphued", StringComparison.InvariantCultureIgnoreCase))
            {
                if (gparams.Count >= 3)
                    gump.Add(new GumpPic(gparams));
            }
            else if (string.Equals(entry, "togglelimitgumpscale", StringComparison.InvariantCultureIgnoreCase))
            {
                // ??
            }
            else
            {
                Log.Warn($"Invalid Gump Command: \"{gparams[0]}\"");
            }
        }

        if (mustBeAdded)
        {
            ServiceProvider.Get<GuiService>().Add(gump);
        }

        gump.Update();
        gump.SetInScreen();

        return gump;
    }

    public void ApplyTrans(
        Gump gump,
        int current_page,
        int x,
        int y,
        int width,
        int height
    )
    {
        int x2 = x + width;
        int y2 = y + height;
        for (int i = 0; i < gump.Children.Count; i++)
        {
            Control child = gump.Children[i];
            bool canDraw = child.Page == 0 || current_page == child.Page;

            bool overlap =
                (x < child.X + child.Width)
                && (child.X < x2)
                && (y < child.Y + child.Height)
                && (child.Y < y2);

            if (canDraw && child.IsVisible && overlap)
            {
                child.Alpha = 0.5f;
            }
        }
    }

    [Flags]
    private enum AffixType
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02
    }
}