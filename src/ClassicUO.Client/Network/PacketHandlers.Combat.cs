// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Resources;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterCombatHandlers(PacketHandlers h)
        {
            h.Add(0x0B, Damage);
            h.Add(0x11, CharacterStatus);
            h.Add(0x16, NewHealthbarUpdate);
            h.Add(0x17, NewHealthbarUpdate);
            h.Add(0x2C, DeathScreen);
            h.Add(0x2D, MobileAttributes);
            h.Add(0x2F, Swing);
            h.Add(0x72, Warmode);
            h.Add(0xA1, UpdateHitpoints);
            h.Add(0xA2, UpdateMana);
            h.Add(0xA3, UpdateStamina);
            h.Add(0xAA, AttackCharacter);
            h.Add(0xAF, DisplayDeath);
            h.Add(0xDE, UpdateMobileStatus);
            h.Add(0xDF, BuffDebuff);
        }

        private static void Damage(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            Entity entity = world.Get(p.ReadUInt32BE());

            if (entity != null)
            {
                ushort damage = p.ReadUInt16BE();

                if (damage > 0)
                {
                    world.WorldTextManager.AddDamage(entity, damage);
                }
            }
        }

        private static void CharacterStatus(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity entity = world.Get(serial);

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
                Mobile mobile = entity as Mobile;

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
                            Client.Game.SetWindowTitle(world.Player.Name);
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
                            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_500A)
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
                    world.UoAssist.SignalHits();
                    world.UoAssist.SignalStamina();
                    world.UoAssist.SignalMana();
                }
            }
        }

        private static void NewHealthbarUpdate(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            if (p[0] == 0x16 && Client.Game.UO.Version < Utility.ClientVersion.CV_500A)
            {
                return;
            }

            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

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
                        if (Client.Game.UO.Version >= Utility.ClientVersion.CV_7000)
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
                        if (Client.Game.UO.Version >= Utility.ClientVersion.CV_7000)
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

        private static void DeathScreen(World world, ref StackDataReader p)
        {
            // todo
            byte action = p.ReadUInt8();

            if (action != 1)
            {
                world.Weather.Reset();

                Client.Game.Audio.PlayMusic(Client.Game.Audio.DeathMusicIndex, true);

                if (ProfileManager.CurrentProfile.EnableDeathScreen)
                {
                    world.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;
                }

                GameActions.RequestWarMode(world.Player, false);
            }
        }

        private static void MobileAttributes(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            Entity entity = world.Get(serial);

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
                Mobile mobile = entity as Mobile;

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
                    world.UoAssist.SignalHits();
                    world.UoAssist.SignalStamina();
                    world.UoAssist.SignalMana();
                }
            }
        }

        private static void Swing(World world, ref StackDataReader p)
        {
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
                world.TargetManager.LastAttack == defenders
                && world.Player.InWarMode
                && world.Player.Walker.LastStepRequestTime + TIME_TURN_TO_LASTTARGET < Time.Ticks
                && world.Player.Steps.Count == 0
            )
            {
                Mobile enemy = world.Mobiles.Get(defenders);

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

        private static void Warmode(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            world.Player.InWarMode = p.ReadBool();
        }

        private static void UpdateHitpoints(World world, ref StackDataReader p)
        {
            Entity entity = world.Get(p.ReadUInt32BE());

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
                world.UoAssist.SignalHits();
            }
        }

        private static void UpdateMana(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            mobile.ManaMax = p.ReadUInt16BE();
            mobile.Mana = p.ReadUInt16BE();

            if (mobile == world.Player)
            {
                world.UoAssist.SignalMana();
            }
        }

        private static void UpdateStamina(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            mobile.StaminaMax = p.ReadUInt16BE();
            mobile.Stamina = p.ReadUInt16BE();

            if (mobile == world.Player)
            {
                world.UoAssist.SignalStamina();
            }
        }

        private static void AttackCharacter(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            //if (TargetManager.LastAttack != serial && World.InGame)
            //{



            //}

            GameActions.SendCloseStatus(world, world.TargetManager.LastAttack);
            world.TargetManager.LastAttack = serial;
            GameActions.RequestMobileStatus(world, serial);
        }

        private static void DisplayDeath(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            uint corpseSerial = p.ReadUInt32BE();
            uint running = p.ReadUInt32BE();

            Mobile owner = world.Mobiles.Get(serial);

            if (owner == null || serial == world.Player)
            {
                return;
            }

            serial |= 0x80000000;

            if (world.Mobiles.Remove(owner.Serial))
            {
                for (LinkedObject i = owner.Items; i != null; i = i.Next)
                {
                    Item it = (Item)i;
                    it.Container = serial;
                }

                world.Mobiles[serial] = owner;
                owner.Serial = serial;
            }

            if (SerialHelper.IsValid(corpseSerial))
            {
                world.CorpseManager.Add(corpseSerial, serial, owner.Direction, running != 0);
            }

            var animations = Client.Game.UO.Animations;
            var gfx = owner.Graphic;
            animations.ConvertBodyIfNeeded(ref gfx);
            var animGroup = animations.GetAnimType(gfx);
            var animFlags = animations.GetAnimFlags(gfx);
            byte group = Client.Game.UO.FileManager.Animations.GetDeathAction(
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

        private static void UpdateMobileStatus(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            byte status = p.ReadUInt8();

            if (status == 1)
            {
                uint attackerSerial = p.ReadUInt32BE();
            }
        }

        private static void BuffDebuff(World world, ref StackDataReader p)
        {
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
                BuffGump gump = UIManager.GetGump<BuffGump>();
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
                        string title = Client.Game.UO.FileManager.Clilocs.Translate(
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
                                + Client.Game.UO.FileManager.Clilocs.Translate(
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
                            wtf = Client.Game.UO.FileManager.Clilocs.Translate(
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
    }
}
