using System.Collections.Generic;
using System;
using ClassicUO.Game;
using LScript;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;

namespace ClassicUO.LegionScripting
{
    internal static class Expressions
    {
        public static int GetPlayerMaxStam(string expression, Argument[] args, bool quiet) => World.Player.StaminaMax;
        public static int GetPlayerStam(string expression, Argument[] args, bool quiet) => World.Player.Stamina;
        public static int GetPlayerMaxHits(string expression, Argument[] args, bool quiet)
        {
            uint serial = World.Player.Serial;

            if (args.Length > 0)
                serial = args[0].AsSerial();

            if (World.Mobiles.TryGetValue(serial, out var m))
                return m.HitsMax;

            return 0;
        }
        public static int GetPlayerHits(string expression, Argument[] args, bool quiet)
        {
            uint serial = World.Player.Serial;

            if (args.Length > 0)
                serial = args[0].AsSerial();

            if (World.Mobiles.TryGetValue(serial, out var m))
                return m.Hits;

            return 0;
        }
        public static int GetPlayerMaxMana(string expression, Argument[] args, bool quiet) => World.Player.ManaMax;
        public static int GetPlayerMana(string expression, Argument[] args, bool quiet) => World.Player.Mana;
        public static int GetPosX(string expression, Argument[] args, bool quiet) => World.Player.X;
        public static int GetPosY(string expression, Argument[] args, bool quiet) => World.Player.Y;
        public static int GetPosZ(string expression, Argument[] args, bool quiet) => World.Player.Z;
        public static string GetPlayerName(string expression, Argument[] args, bool quiet) => World.Player.Name;
        public static bool GetFalse(string expression, Argument[] args, bool quiet) => false;
        public static bool GetTrue(string expression, Argument[] args, bool quiet) => true;
        public static bool TimerExpired(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: timerexpired 'timer name'");

            return Interpreter.TimerExpired(args[0].AsString());
        }
        public static bool TimerExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: timerexists 'timer name'");

            return Interpreter.TimerExists(args[0].AsString());
        }
        public static bool PoisonedStatus(string expression, Argument[] args, bool quiet)
        {
            uint serial = args.Length > 0 ? args[0].AsSerial() : World.Player;

            if (World.Mobiles.TryGetValue(serial, out var m))
            {
                return m.IsPoisoned;
            }

            return false;
        }
        public static double SkillValue(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: skill 'name' [true/false]");

            bool force = args.Length > 1 ? args[1].AsBool() : false;

            for (int i = 0; i < World.Player.Skills.Length; i++)
            {
                if (World.Player.Skills[i].Name.ToLower().Contains(args[0].AsString()))
                {
                    return force ? World.Player.Skills[World.Player.Skills[i].Index].Base : World.Player.Skills[World.Player.Skills[i].Index].Value;
                }
            }

            if (!quiet) LegionScripting.LScriptError($"Skill {args[0].AsString()} not found!");
            return 0;
        }
        public static bool FindAlias(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: findalias 'name'");

            uint foundVal = Interpreter.GetAlias(args[0].GetLexeme());

            if (foundVal == uint.MaxValue)
                foundVal = args[0].AsSerial();

            Entity e = World.Get(foundVal);
            if (e != null)
            {
                Interpreter.SetAlias(Constants.FOUND, e);
                return true;
            }

            return false;
        }
        public static uint FindType(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: findtype 'graphic' 'source' [color] [range]");

            uint gfx = args[0].AsUInt();
            uint source = args[1].AsSerial();

            if (source == Constants.MAX_SERIAL) source = uint.MaxValue;
            if (gfx == 0) gfx = uint.MaxValue;

            ushort hue = args.Length >= 3 ? args[2].AsUShort() : ushort.MaxValue;
            int range = args.Length >= 4 ? args[3].AsInt() : int.MaxValue;


            List<Item> items = Utility.FindItems(gfx, parOrRootContainer: source, hue: hue, groundRange: range);

            if (items.Count > 0)
            {
                foreach (Item item in items)
                {
                    Interpreter.SetAlias(Constants.FOUND, item);
                    break;
                }
            }

            return (uint)items.Count;
        }
        public static bool PropertySearch(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: property 'serial' 'text'");

            if (World.Items.TryGetValue(args[0].AsSerial(), out var item))
            {
                return Utility.SearchItemNameAndProps(args[1].AsString(), item);
            }

            return false;
        }
        public static bool InParty(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: inparty 'serial'");

            uint serial = args[0].AsSerial();

            foreach (var mem in World.Party.Members)
            {
                if (mem.Serial == serial)
                    return true;
            }

            return false;
        }
        public static bool InJournal(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: injournal 'search text'");

            return Interpreter.ActiveScript.SearchJournalEntries(args[0].AsString());
        }
        public static uint DistanceCheck(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: distance 'serial'");

            uint serial = args[0].AsSerial();

            if (SerialHelper.IsValid(serial))
            {
                if (SerialHelper.IsItem(serial))
                {
                    if (World.Items.TryGetValue(serial, out var item))
                        return (uint)item.Distance;
                }
                else if (SerialHelper.IsMobile(serial))
                {
                    if (World.Mobiles.TryGetValue(serial, out var mobile))
                        return (uint)mobile.Distance;
                }
            }

            return uint.MaxValue;
        }
        public static bool FindObject(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: findobject 'serial' [container]");



            if (World.Items.TryGetValue(args[0].AsSerial(), out var obj))
            {
                if (args.Length > 1)
                {
                    uint source = args[1].AsSerial();

                    if (obj.Container == source || obj.RootContainer == source)
                        return true;
                }
                else
                    return true;
            }
            else
            if (World.Mobiles.TryGetValue(args[0].AsSerial(), out var m))
            {
                return true;
            }

            return false;
        }
        public static uint CountContents(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: contents 'container'");

            if (World.Items.TryGetValue(args[0].AsSerial(), out var item))
            {
                return Utility.ContentsCount(item);
            }

            return 0;
        }
        public static bool CheckWar(string expression, Argument[] args, bool quiet)
        {
            return World.Player.InWarMode;
        }
        public static bool InList(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: inlist 'name' 'value'");

            return Interpreter.ListContains(args[0].AsString(), args[1]);
        }
        public static bool ListExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: listexists 'name'");

            return Interpreter.ListExists(args[0].AsString());
        }
        public static int ListCount(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: listcount 'name'");

            return Interpreter.ListLength(args[0].AsString());
        }
        public static bool GumpExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: gumpexists 'gumpid'");

            uint gumpid = args[0].AsUInt();

            for (LinkedListNode<Gump> last = UIManager.Gumps.Last; last != null; last = last.Previous)
            {
                Control c = last.Value;
                if (last.Value != null && (last.Value.ServerSerial == gumpid || last.Value.LocalSerial == gumpid))
                    return true;
            }

            return false;
        }
        public static bool FindLayer(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: findlayer 'layer'");

            string layer = args[0].AsString();

            Layer finalLayer = Utility.GetItemLayer(layer);

            if (finalLayer != Layer.Invalid)
            {
                Item item = World.Player.FindItemByLayer(finalLayer);
                if (item != null)
                {
                    Interpreter.SetAlias(Constants.FOUND, item);
                    return true;
                }
            }

            return false;
        }
        public static bool BuffExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: buffexists 'name'");

            string bufftext = args[0].AsString().ToLower();

            foreach (BuffIcon buff in World.Player.BuffIcons.Values)
            {
                if (buff.Title.ToLower().Contains(bufftext))
                    return true;
            }

            return false;
        }
        public static bool IsDead(string expression, Argument[] args, bool quiet)
        {
            Mobile m = World.Player;

            if (args.Length > 0)
                if (World.Mobiles.TryGetValue(args[0].AsSerial(), out m))
                    return m.IsDead;
                else
                    return true;

            return m.IsDead;
        }
        public static int CountType(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: counttype 'graphic' 'source' 'hue', 'ground range'");

            uint graphic = args[0].AsUInt();
            uint source = args[1].AsSerial();
            if (source == Constants.MAX_SERIAL) source = uint.MaxValue;

            ushort hue = args.Length > 2 ? args[2].AsUShort() : ushort.MaxValue;

            int ground = args.Length > 3 ? args[3].AsInt() : int.MaxValue;

            var items = Utility.FindItems(graphic, parOrRootContainer: source, hue: hue, groundRange: ground);

            int count = 0;

            foreach (var item in items)
            {
                count += item.Amount == 0 ? 1 : item.Amount;
            }

            return count;
        }
        public static bool NearestHostile(string expression, Argument[] args, bool quiet)
        {
            // nearesthostile 'distance'
            int maxDist = 10;

            if (args.Length > 0)
            {
                maxDist = args[0].AsInt();
            }

            uint m = World.FindNearest(ScanTypeObject.Hostile);

            if (SerialHelper.IsMobile(m) && World.Mobiles.TryGetValue(m, out var mobile))
            {
                if (mobile.Distance <= maxDist && !Interpreter.InIgnoreList(m))
                {
                    Interpreter.SetAlias(Constants.FOUND, m);
                    return true;
                }
            }

            return false;
        }
        public static bool IsMounted(string expression, Argument[] args, bool quiet)
        {
            return World.Player.FindItemByLayer(Layer.Mount) != null;
        }
        public static bool IsParalyzed(string expression, Argument[] args, bool quiet)
        {
            uint serial = World.Player;

            if (args.Length > 0) serial = args[0].AsSerial();

            if (World.Mobiles.TryGetValue(serial, out var m)) return m.IsParalyzed;

            return false;
        }
        public static int GetPlayerWeight(string expression, Argument[] args, bool quiet) => World.Player.Weight;
        public static int GetPlayerMaxWeight(string expression, Argument[] args, bool quiet) => World.Player.WeightMax;
        public static bool SecondaryAbilityActive(string expression, Argument[] args, bool quiet)
        {
            return ((byte)World.Player.SecondaryAbility & 0x80) != 0;
        }
        public static bool PrimaryAbilityActive(string expression, Argument[] args, bool quiet)
        {
            return ((byte)World.Player.PrimaryAbility & 0x80) != 0;
        }
        public static bool IsHidden(string expression, Argument[] args, bool quiet) => World.Player.IsHidden;
        public static int GetGold(string expression, Argument[] args, bool quiet) => (int)World.Player.Gold;
        public static int GetMaxFollowers(string expression, Argument[] args, bool quiet) => World.Player.FollowersMax;
        public static int GetFollowers(string expression, Argument[] args, bool quiet) => World.Player.Followers;
        public static int GetInt(string expression, Argument[] args, bool quiet) => World.Player.Intelligence;
        public static int GetDex(string expression, Argument[] args, bool quiet) => World.Player.Dexterity;
        public static int GetStr(string expression, Argument[] args, bool quiet) => World.Player.Strength;
        public static int DiffHits(string expression, Argument[] args, bool quiet)
        {
            uint serial = World.Player.Serial;

            if (args.Length > 0)
                serial = args[0].AsSerial();

            if (World.Mobiles.TryGetValue(serial, out var m))
                return m.HitsMax - m.Hits;

            return 0;
        }
        public static bool FindTypeList(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: findtypelist 'listname' 'source' [color] [range]");

            List<Argument> list = Interpreter.GetList(args[0].AsString());

            if (list == null)
                throw new RunTimeError(null, $"List '{args[0].AsString()}' does not exist!");

            uint source = args[1].AsSerial();

            if (source == Constants.MAX_SERIAL) source = uint.MaxValue;

            ushort hue = args.Length >= 3 ? args[2].AsUShort() : ushort.MaxValue;
            int range = args.Length >= 4 ? args[3].AsInt() : int.MaxValue;

            foreach (Argument arg in list)
            {
                List<Item> items = Utility.FindItems(arg.AsUInt(), parOrRootContainer: source, hue: hue, groundRange: range);
                if (items.Count > 0)
                {
                    foreach (Item item in items)
                    {
                        Interpreter.SetAlias(Constants.FOUND, item);
                        return true;
                    }
                }
            }

            return false;
        }
        public static ushort ItemAmt(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: itemamt 'serial'");

            if (World.Items.TryGetValue(args[0].AsSerial(), out var item))
                return item.Amount < 1 ? (ushort)1 : item.Amount;

            return 0;
        }
        public static uint Ping(string expression, Argument[] args, bool quiet)
        {
            return NetClient.Socket.Statistics.Ping;
        }
        public static bool IsPathfinding(string expression, Argument[] args, bool quiet) => Pathfinder.AutoWalking;
    }
}
