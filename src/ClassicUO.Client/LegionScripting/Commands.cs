using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility;
using LScript;

namespace ClassicUO.LegionScripting
{
    internal static class Commands
    {
        public static bool TargetLandRel(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: targetlandrel 'x' 'y'");

            if (!TargetManager.IsTargeting)
                return true;

            ushort x = (ushort)(World.Player.X + args[0].AsInt());
            ushort y = (ushort)(World.Player.Y + args[1].AsInt());

            World.Map.GetMapZ(x, y, out sbyte gZ, out sbyte sZ);
            TargetManager.Target(0, x, y, gZ);
            return true;
        }
        public static bool TargetTileRel(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: targettilerel 'x' 'y' ['graphic']");

            if (!TargetManager.IsTargeting)
                return true;

            ushort x = (ushort)(World.Player.X + args[0].AsInt());
            ushort y = (ushort)(World.Player.Y + args[1].AsInt());

            GameObject g = World.Map.GetTile(x, y);

            if (args.Length > 3)
            {
                ushort gfx = args[4].AsUShort();

                if (g.Graphic != gfx)
                    return true;
            }

            TargetManager.Target(g.Graphic, x, y, g.Z);
            return true;
        }
        public static bool CommandFly(string command, Argument[] args, bool quiet, bool force)
        {
            if (World.Player.Race == RaceType.GARGOYLE)
            {
                NetClient.Socket.Send_ToggleGargoyleFlying();
                return true;
            }

            if (!quiet)
                LegionScripting.LScriptError("Player is not a gargoyle, cannot fly.");

            return true;
        }
        public static bool UseSecondaryAbility(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.UsePrimaryAbility();
            return true;
        }
        public static bool UsePrimaryAbility(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.UseSecondaryAbility();
            return true;
        }
        public static bool BandageSelf(string command, Argument[] args, bool quiet, bool force)
        {
            if (Client.Version < ClientVersion.CV_5020 || ProfileManager.CurrentProfile.BandageSelfOld)
            {
                Item band = World.Player.FindBandage();

                if (band != null)
                {
                    GameActions.DoubleClickQueued(band);
                }
            }
            else
            {
                GameActions.BandageSelf();
            }

            return true;
        }
        public static bool ClickObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: clickobject 'serial'");

            GameActions.SingleClick(args[0].AsSerial());
            return true;
        }
        public static bool CommandAttack(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: attack 'serial'");

            GameActions.Attack(args[0].AsSerial());
            return true;
        }
        public static bool WaitForJournal(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: waitforjournal 'search text' 'duration'");

            if (Interpreter.ActiveScript.SearchJournalEntries(args[0].AsString()))
                return true;

            Interpreter.Timeout(args.Length >= 2 ? args[1].AsInt() : 10000, LegionScripting.ReturnTrue);

            return false;
        }
        public static bool CastSpell(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: cast 'spell name'");

            GameActions.CastSpellByName(args[0].AsString());

            return true;
        }
        public static bool MoveItemOffset(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 5)
                throw new RunTimeError(null, "Usage: moveitemoffset 'item' 'amt' 'x' 'y' 'z'");

            uint item = args[0].AsSerial();
            int amt = args[1].AsInt();
            int x = args[2].AsInt();
            int y = args[3].AsInt();
            int z = args[4].AsInt();


            GameActions.PickUp(item, 0, 0, amt);
            GameActions.DropItem
            (
                item,
                World.Player.X + x,
                World.Player.Y + y,
                World.Player.Z + z,
                0
            );

            return true;
        }
        public static bool MoveItem(string command, Argument[] args, bool quiet, bool force)
        {

            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: moveitem 'item' 'container'");

            uint item = args[0].AsSerial();

            uint bag = args[1].AsSerial();

            ushort amt = 0;
            if (args.Length > 2)
                amt = args[2].AsUShort();

            GameActions.GrabItem(item, amt, bag, !force);
            return true;
        }
        public static bool SystemMessage(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: sysmsg 'message text' 'hue'");

            string msg = args[0].AsString();

            ushort hue = 946;

            if (args.Length > 1)
                hue = args[1].AsUShort();


            GameActions.Print(msg, hue);
            return true;
        }
        public static bool CancelTarget(string command, Argument[] args, bool quiet, bool force)
        {
            if (TargetManager.IsTargeting)
                TargetManager.CancelTarget();

            return true;
        }
        public static bool CommandRun(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: run 'direction'");

            string dir = args[0].AsString().ToLower();

            World.Player.Walk(Utility.GetDirection(dir), true);

            return true;
        }
        public static bool CommandWalk(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: walk 'direction'");

            string dir = args[0].AsString().ToLower();

            World.Player.Walk(Utility.GetDirection(dir), false);

            return true;
        }
        public static bool UseSkill(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: useskill 'skill name'");

            string skill = args[0].AsString().Trim().ToLower();

            if (skill.Length > 0)
            {
                for (int i = 0; i < World.Player.Skills.Length; i++)
                {
                    if (World.Player.Skills[i].Name.ToLower().Contains(skill))
                    {
                        GameActions.UseSkill(World.Player.Skills[i].Index);
                        break;
                    }
                }
            }

            return true;
        }
        public static bool PauseCommand(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: pause 'duration'");

            int ms = args[0].AsInt();

            Interpreter.Pause(ms);
            return true;
        }
        public static bool UseType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: usetype 'container' 'graphic' 'hue'");

            uint source = args[0].AsSerial();
            uint gfx = args[1].AsUInt();
            ushort hue = args.Length > 2 ? args[2].AsUShort() : ushort.MaxValue;

            if (gfx == Constants.MAX_SERIAL) gfx = uint.MaxValue;

            var items = Utility.FindItems(gfx, parOrRootContainer: source, hue: hue);

            if (items.Count > 0)
            {
                GameActions.DoubleClick(items[0]);
            }

            return true;
        }
        public static bool WaitForTarget(string command, Argument[] args, bool quiet, bool force)
        {
            TargetType type = TargetType.Neutral;

            if (args.Length >= 1)
            {
                type = (TargetType)args[0].AsInt();
            }

            Interpreter.Timeout(args.Length >= 2 ? args[1].AsInt() : 10000, LegionScripting.ReturnTrue);

            if (TargetManager.IsTargeting)
            {
                if (type == TargetType.Neutral)
                    return true;

                if (TargetManager.TargetingType == type)
                    return true;
            }

            return false;
        }
        public static bool TargetSerial(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: target 'serial'");

            TargetManager.Target(args[0].AsSerial());

            return true;
        }
        public static bool UseObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: useobject 'serial' 'true/false'");

            bool useQueue = true;

            if (args.Length >= 2)
                if (args[1].AsBool())
                    useQueue = true;
                else
                    useQueue = false;

            if (useQueue)
                GameActions.DoubleClickQueued(args[0].AsSerial());
            else
                GameActions.DoubleClick(args[0].AsSerial());

            return true;
        }
        public static bool MoveType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 3)
                throw new RunTimeError(null, "Usage: movetype 'graphic' 'source' 'destination'  [amount] [color] [range]");

            uint gfx = args[0].AsUInt();
            uint source = args[1].AsSerial();
            uint target = args[2].AsSerial();

            int amount = args.Length >= 4 ? args[3].AsInt() : -1;
            ushort hue = args.Length >= 5 ? args[4].AsUShort() : ushort.MaxValue;
            int range = args.Length >= 6 ? args[5].AsInt() : 2;

            foreach (Item item in World.Items.Values)
            {
                if (source == Constants.MAX_SERIAL || item.Container == source || item.RootContainer == source)
                {
                    if (item.Graphic != gfx || item.Container == target || item.RootContainer == target)
                        continue;

                    if (source == Constants.MAX_SERIAL && item.Distance > range)
                        continue;

                    if (!Interpreter.InIgnoreList(item))
                        continue;

                    if (hue != ushort.MaxValue)
                    {
                        if (item.Hue == hue)
                        {
                            if (GameActions.PickUp(item, 0, 0, amount < 1 ? item.Amount : amount))
                            {
                                GameActions.DropItem(item, 0xFFFF, 0xFFFF, 0, target);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (GameActions.PickUp(item, 0, 0, amount < 1 ? item.Amount : amount))
                            GameActions.DropItem(item, 0xFFFF, 0xFFFF, 0, target);
                        return true;
                    }
                }
            }

            return true;
        }
        public static bool UnsetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: unsetalias 'name'");

            Interpreter.RemoveAlias(args[0].AsString());

            return true;
        }
        public static bool SetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: setalias 'name' 'serial'");

            string name = args[0].AsString();
            uint val = args[1].AsSerial();

            Interpreter.SetAlias(name, val);

            return true;
        }
        public static bool SetTimer(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: settimer 'timer name' 'duration'");

            Interpreter.SetTimer(args[0].AsString(), args[1].AsInt());

            return true;
        }
        public static bool RemoveTimer(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: removetimer 'timer name'");

            Interpreter.RemoveTimer(args[0].AsString());

            return true;
        }
        public static bool ToggleAutoLoot(string command, Argument[] args, bool quiet, bool force)
        {
            ProfileManager.CurrentProfile.EnableAutoLoot = !ProfileManager.CurrentProfile.EnableAutoLoot;
            return true;
        }
        public static bool InfoGump(string command, Argument[] args, bool quiet, bool force)
        {
            TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
            return true;
        }
        public static bool SetSkillLock(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: setskill 'skill' 'up/down/locked'");

            Lock status = Lock.Up;

            switch (args[1].AsString())
            {
                case "up":
                default:
                    status = Lock.Up;
                    break;
                case "down":
                    status = Lock.Down;
                    break;
                case "locked":
                    status = Lock.Locked;
                    break;
            }

            for (int i = 0; i < World.Player.Skills.Length; i++)
            {
                if (World.Player.Skills[i].Name.ToLower().Contains(args[0].AsString()))
                {
                    World.Player.Skills[i].Lock = status;
                    break;
                }
            }

            return true;
        }
        public static bool GetProperties(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: getproperties 'serial'");

            bool hasProps = World.OPL.Contains(args[0].AsSerial()); //This will request properties if we don't already have them

            if (force)
                return true;

            return hasProps;
        }
        public static bool Logout(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.Logout();
            return true;
        }
        public static bool RenamePet(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: rename 'serial' 'name'");

            GameActions.Rename(args[0].AsSerial(), args[1].AsString());

            return true;
        }
        public static bool PushList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: pushlist 'name' 'value' [front]");

            bool front = false;

            if (args.Length > 2 && args[2].AsString().ToLower() == "front")
                front = true;

            if (args[1].IsSerial())                     
                Interpreter.PushList(args[0].AsString(), new Argument(Interpreter.ActiveScript, new ASTNode(ASTNodeType.SERIAL, args[1].AsSerial().ToString(), null, 0)), front, force);
            else
                Interpreter.PushList(args[0].AsString(), args[1], front, force);

            return true;
        }
        public static bool CreateList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: createlist 'name'");

            Interpreter.CreateList(args[0].AsString());

            return true;
        }
        public static bool TurnCommand(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: turn 'direction'");

            Direction d = Utility.GetDirection(args[0].AsString());

            if (d != Direction.NONE && World.Player.Direction != d)
                World.Player.Walk(d, false);

            return true;
        }
        public static bool RemoveList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: removelist 'name'");

            Interpreter.DestroyList(args[0].AsString());

            return true;
        }
        public static bool ClearList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: clearlist 'name'");

            Interpreter.ClearList(args[0].AsString());

            return true;
        }
        public static bool ShowNames(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.AllNames();
            return true;
        }
        public static bool PopList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: poplist 'name' 'value'");

            Interpreter.PopList(args[0].AsString(), args[1]);

            return true;
        }
        public static bool ClearJournal(string command, Argument[] args, bool quiet, bool force)
        {
            Interpreter.ClearJournal();
            return true;
        }
        public static bool CloseGump(string command, Argument[] args, bool quiet, bool force)
        {
            uint gump = args.Length > 0 ? args[0].AsUInt() : World.Player.LastGumpID;

            UIManager.GetGumpServer(gump)?.Dispose();

            return true;
        }
        public static bool ReplyGump(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: replygump 'buttonid' 'gumpid'");

            int buttonID = args[0].AsInt();
            uint gumpID = args.Length > 1 ? args[1].AsUInt() : World.Player.LastGumpID;

            Gump g = UIManager.GetGumpServer(gumpID);

            if (g != null)
                GameActions.ReplyGump(g.LocalSerial, gumpID, buttonID);

            return true;
        }
        public static bool WaitForGump(string command, Argument[] args, bool quiet, bool force)
        {
            uint gumpID = uint.MaxValue;
            int timeout = 5000;

            if (args.Length > 0) gumpID = args[0].AsUInt();
            if (args.Length > 1) timeout = args[1].AsInt();

            Interpreter.Timeout(timeout, LegionScripting.ReturnTrue);

            if (World.Player.HasGump && (World.Player.LastGumpID == gumpID || gumpID == uint.MaxValue))
            {
                Interpreter.ClearTimeout();
                Interpreter.SetAlias("lastgump", World.Player.LastGumpID);
                return true;
            }

            return false;
        }
        public static bool PromptAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: promptalias 'name'");

            if (Interpreter.IsTargetRequested())
            {
                if (TargetManager.IsTargeting && TargetManager.TargetingState == CursorTarget.Internal)
                    return false;

                if (TargetManager.LastTargetInfo.IsEntity)
                {
                    Interpreter.SetAlias(args[0].AsString(), TargetManager.LastTargetInfo.Serial);
                    Interpreter.SetTargetRequested(false);
                    return true;
                }

                LegionScripting.LScriptWarning("Warning: Targeted object only supports items and mobiles.");
                Interpreter.SetTargetRequested(false);
                return true;
            }

            TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral);
            Interpreter.SetTargetRequested(true);

            return false;
        }
        public static bool ToggleMounted(string command, Argument[] args, bool quiet, bool force)
        {
            //No params
            Item mount = World.Player.FindItemByLayer(Layer.Mount);

            if (mount != null)
            {
                Interpreter.SetAlias(Constants.LASTMOUNT + World.Player.Serial.ToString(), mount);
                GameActions.DoubleClick(World.Player.Serial);
                return true;
            }
            else
            {
                uint serial = Interpreter.GetAlias(Constants.LASTMOUNT + World.Player.Serial.ToString());
                if (serial != uint.MaxValue)
                {
                    GameActions.DoubleClick(serial);
                    return true;
                }
            }

            return true;
        }
        public static bool EquipItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: equipitem 'serial'");

            uint serial = args[0].AsSerial();

            if (SerialHelper.IsItem(serial))
            {
                GameActions.PickUp(serial, 0, 0, 1);
                GameActions.Equip(serial);
            }

            return true;
        }
        public static bool ToggleHands(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: togglehands 'left/right'");

            Layer hand = args[0].AsString() switch
            {
                "left" => Layer.OneHanded,
                "right" => Layer.TwoHanded,
                _ => Layer.Invalid
            };

            if (hand != Layer.Invalid)
            {
                Item i = World.Player.FindItemByLayer(hand);
                if (i != null) //Item is in hand, lets unequip and save it
                {
                    GameActions.GrabItem(i, 0, World.Player.FindItemByLayer(Layer.Backpack));
                    Interpreter.SetAlias(Constants.LASTITEMINHAND + hand.ToString(), i);
                    return true;
                }
                else //No item in hand, lets see if we have a saved item for this slot
                {
                    uint serial = Interpreter.GetAlias(Constants.LASTITEMINHAND + hand.ToString());
                    if (SerialHelper.IsItem(serial))
                    {
                        GameActions.PickUp(serial, 0, 0, 1);
                        GameActions.Equip();
                        return true;
                    }
                }
            }

            return true;
        }
        public static bool Virtue(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: virtue 'honor|sacrifice|valor'");

            switch (args[0].AsString())
            {
                case "honor":
                    NetClient.Socket.Send_InvokeVirtueRequest(0x01);
                    break;
                case "sacrifice":
                    NetClient.Socket.Send_InvokeVirtueRequest(0x02);
                    break;
                case "valor":
                    NetClient.Socket.Send_InvokeVirtueRequest(0x03);
                    break;
            }

            return true;
        }
        public static bool MsgCommand(string command, Argument[] args, bool quiet, bool force)
        {
            string msg = "";

            foreach (Argument arg in args)
            {
                msg += " " + arg.AsString();
            }

            GameActions.Say(msg, ProfileManager.CurrentProfile.SpeechHue);

            return true;
        }
        public static bool PlayMacro(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: playmacro 'macroname'");

            var mm = MacroManager.TryGetMacroManager();

            if (mm != null)
            {
                var macro = mm.FindMacro(args[0].AsString());
                if (macro != null)
                    mm.SetMacroToExecute(macro.Items as MacroObject);
            }

            return true;
        }
        public static bool HeadMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: headmsg 'serial' 'msg'");

            Entity e = World.Get(args[0].AsSerial());

            MessageManager.HandleMessage(e, args[1].AsString(), "", ProfileManager.CurrentProfile.SpeechHue, MessageType.Label, 3, TextType.OBJECT);

            return true;
        }
        public static bool PartyMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: partymsg 'msg'");

            GameActions.SayParty(args[0].AsString());

            return true;
        }
        public static bool GuildMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: guildmsg 'msg'");

            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile.GuildMessageHue, MessageType.Guild);

            return true;
        }
        public static bool AllyMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: allymsg 'msg'");

            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile.AllyMessageHue, MessageType.Alliance);

            return true;
        }
        public static bool WhisperMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: whispermsg 'msg'");

            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile.WhisperHue, MessageType.Whisper);

            return true;
        }
        public static bool YellMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: yellmsg 'msg'");

            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile.YellHue, MessageType.Yell);

            return true;
        }
        public static bool EmoteMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: emotemsg 'msg'");

            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile.EmoteHue, MessageType.Emote);

            return true;
        }
        public static bool WaitForPrompt(string command, Argument[] args, bool quiet, bool force)
        {
            //timeout optional
            long duration = args.Length > 0 ? args[0].AsInt() : 10000;

            Interpreter.Timeout(duration, LegionScripting.ReturnTrue);

            return MessageManager.PromptData.Prompt != ConsolePrompt.None;
        }
        public static bool CancelPrompt(string command, Argument[] args, bool quiet, bool force)
        {
            if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
            {
                NetClient.Socket.Send_ASCIIPromptResponse(string.Empty, true);
            }
            else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
            {
                NetClient.Socket.Send_UnicodePromptResponse(string.Empty, Settings.GlobalSettings.Language, true);
            }

            MessageManager.PromptData = default;
            return true;
        }
        public static bool PromptResponse(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: promptresponse 'msg'");

            string text = args[0].AsString();

            if (MessageManager.PromptData.Prompt != ConsolePrompt.None)
            {
                if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                {
                    NetClient.Socket.Send_ASCIIPromptResponse(text, text.Length < 1);
                }
                else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                {
                    NetClient.Socket.Send_UnicodePromptResponse(text, Settings.GlobalSettings.Language, text.Length < 1);
                }

                MessageManager.PromptData = default;
            }

            return true;
        }
        public static bool ContextMenu(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: contextmenu 'serial' 'option'");

            uint serial = args[0].AsSerial();

            PopupMenuGump.CloseNext = serial;
            NetClient.Socket.Send_RequestPopupMenu(serial);
            NetClient.Socket.Send_PopupMenuSelection(serial, args[1].AsUShort());

            return true;
        }
        public static bool ClearIgnoreList(string command, Argument[] args, bool quiet, bool force)
        {
            Interpreter.ClearIgnoreList();
            return true;
        }
        public static bool IgnoreObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: ignoreobject 'serial'");

            Interpreter.IgnoreSerial(args[0].AsSerial());
            return true;
        }
        public static bool Goto(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: goto 'linenum'");

            return !Interpreter.GotoLine(args[0].AsInt());
        }
        public static bool Return(string command, Argument[] args, bool quiet, bool force)
        {
            Interpreter.ReturnFromGoto();

            return true;
        }
        public static bool Follow(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: follow 'serial'");

            ProfileManager.CurrentProfile.FollowingMode = true;
            ProfileManager.CurrentProfile.FollowingTarget = args[0].AsSerial();

            return true;
        }
        public static bool Pathfind(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 3)
                throw new RunTimeError(null, "Usage: pathfind 'x' 'y' 'z'");

            Pathfinder.WalkTo(args[0].AsInt(), args[1].AsInt(), args[2].AsInt(), 0);

            return true;
        }
        public static bool CancelPathfind(string command, Argument[] args, bool quiet, bool force)
        {
            Pathfinder.StopAutoWalk();
            return true;
        }
    }
}
