using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using ClassicUO.Utility;
using UOScript;

namespace ClassicUO.LegionScripting
{
    internal static class UOSBridge
    {
        private static bool _registered;

        public static void Register()
        {
            if (_registered) return;

            UOScript.Interpreter.RegisterAliasHandler("backpack", alias => World.InGame ? World.Player.FindItemByLayer(Layer.Backpack) : 0);
            UOScript.Interpreter.RegisterAliasHandler("bank", alias => World.InGame ? World.Player.FindItemByLayer(Layer.Bank) : 0);
            UOScript.Interpreter.RegisterAliasHandler("lastobject", alias => World.LastObject);
            UOScript.Interpreter.RegisterAliasHandler("lasttarget", alias => TargetManager.LastTargetInfo.Serial);
            UOScript.Interpreter.RegisterAliasHandler("lefthand", alias => World.InGame ? World.Player.FindItemByLayer(Layer.OneHanded) : 0);
            UOScript.Interpreter.RegisterAliasHandler("righthand", alias => World.InGame ? World.Player.FindItemByLayer(Layer.TwoHanded) : 0);
            UOScript.Interpreter.RegisterAliasHandler("self", alias => World.InGame ? World.Player.Serial : 0);
            UOScript.Interpreter.RegisterAliasHandler("mount", alias => World.InGame ? World.Player.FindItemByLayer(Layer.Mount) : 0);
            UOScript.Interpreter.RegisterAliasHandler("bandage", alias => World.InGame ? World.Player.FindBandage() : 0);
            UOScript.Interpreter.RegisterAliasHandler("ground", alias => 0);
            UOScript.Interpreter.RegisterAliasHandler("last", alias => World.LastObject);

            UOScript.Interpreter.RegisterCommandHandler("msg", UOS_Msg);
            UOScript.Interpreter.RegisterCommandHandler("sysmsg", UOS_SysMsg);
            UOScript.Interpreter.RegisterCommandHandler("pause", UOS_Pause);
            UOScript.Interpreter.RegisterCommandHandler("clickobject", UOS_ClickObject);
            UOScript.Interpreter.RegisterCommandHandler("attack", UOS_Attack);
            UOScript.Interpreter.RegisterCommandHandler("useobject", UOS_UseObject);
            UOScript.Interpreter.RegisterCommandHandler("usetype", UOS_UseType);
            UOScript.Interpreter.RegisterCommandHandler("moveitem", UOS_MoveItem);
            UOScript.Interpreter.RegisterCommandHandler("walk", UOS_Walk);
            UOScript.Interpreter.RegisterCommandHandler("run", UOS_Run);
            UOScript.Interpreter.RegisterCommandHandler("canceltarget", UOS_CancelTarget);
            UOScript.Interpreter.RegisterCommandHandler("setalias", UOS_SetAlias);
            UOScript.Interpreter.RegisterCommandHandler("unsetalias", UOS_UnsetAlias);
            UOScript.Interpreter.RegisterCommandHandler("createlist", UOS_CreateList);
            UOScript.Interpreter.RegisterCommandHandler("pushlist", UOS_PushList);
            UOScript.Interpreter.RegisterCommandHandler("clearlist", UOS_ClearList);
            UOScript.Interpreter.RegisterCommandHandler("removelist", UOS_RemoveList);
            UOScript.Interpreter.RegisterCommandHandler("poplist", UOS_PopList);
            UOScript.Interpreter.RegisterCommandHandler("bandageself", UOS_BandageSelf);
            UOScript.Interpreter.RegisterCommandHandler("useskill", UOS_UseSkill);
            UOScript.Interpreter.RegisterCommandHandler("cast", UOS_Cast);
            UOScript.Interpreter.RegisterCommandHandler("settimer", UOS_SetTimer);
            UOScript.Interpreter.RegisterCommandHandler("removetimer", UOS_RemoveTimer);
            UOScript.Interpreter.RegisterCommandHandler("toggleautoloot", UOS_ToggleAutoLoot);
            UOScript.Interpreter.RegisterCommandHandler("logout", UOS_Logout);
            UOScript.Interpreter.RegisterCommandHandler("shownames", UOS_ShowNames);
            UOScript.Interpreter.RegisterCommandHandler("clearjournal", UOS_ClearJournal);
            UOScript.Interpreter.RegisterCommandHandler("target", UOS_Target);
            UOScript.Interpreter.RegisterCommandHandler("turn", UOS_Turn);
            UOScript.Interpreter.RegisterCommandHandler("togglehands", UOS_ToggleHands);
            UOScript.Interpreter.RegisterCommandHandler("equipitem", UOS_EquipItem);
            UOScript.Interpreter.RegisterCommandHandler("togglemounted", UOS_ToggleMounted);
            UOScript.Interpreter.RegisterCommandHandler("rename", UOS_Rename);
            UOScript.Interpreter.RegisterCommandHandler("headmsg", UOS_HeadMsg);
            UOScript.Interpreter.RegisterCommandHandler("partymsg", UOS_PartyMsg);
            UOScript.Interpreter.RegisterCommandHandler("guildmsg", UOS_GuildMsg);
            UOScript.Interpreter.RegisterCommandHandler("allymsg", UOS_AllyMsg);
            UOScript.Interpreter.RegisterCommandHandler("whispermsg", UOS_WhisperMsg);
            UOScript.Interpreter.RegisterCommandHandler("yellmsg", UOS_YellMsg);
            UOScript.Interpreter.RegisterCommandHandler("emotemsg", UOS_EmoteMsg);
            UOScript.Interpreter.RegisterCommandHandler("info", UOS_Info);
            UOScript.Interpreter.RegisterCommandHandler("playmacro", UOS_PlayMacro);
            UOScript.Interpreter.RegisterCommandHandler("virtue", UOS_Virtue);
            UOScript.Interpreter.RegisterCommandHandler("moveitemoffset", UOS_MoveItemOffset);
            UOScript.Interpreter.RegisterCommandHandler("movetype", UOS_MoveType);
            UOScript.Interpreter.RegisterCommandHandler("waitfortarget", UOS_WaitForTarget);
            UOScript.Interpreter.RegisterCommandHandler("waitforjournal", UOS_WaitForJournal);
            UOScript.Interpreter.RegisterCommandHandler("waitforgump", UOS_WaitForGump);
            UOScript.Interpreter.RegisterCommandHandler("replygump", UOS_ReplyGump);
            UOScript.Interpreter.RegisterCommandHandler("closegump", UOS_CloseGump);
            UOScript.Interpreter.RegisterCommandHandler("promptalias", UOS_PromptAlias);
            UOScript.Interpreter.RegisterCommandHandler("promptresponse", UOS_PromptResponse);
            UOScript.Interpreter.RegisterCommandHandler("cancelprompt", UOS_CancelPrompt);
            UOScript.Interpreter.RegisterCommandHandler("waitforprompt", UOS_WaitForPrompt);

            UOScript.Interpreter.RegisterExpressionHandler("findtype", UOS_Expr_FindType);
            UOScript.Interpreter.RegisterExpressionHandler("findalias", UOS_Expr_FindAlias);
            UOScript.Interpreter.RegisterExpressionHandler("findobject", UOS_Expr_FindObject);
            UOScript.Interpreter.RegisterExpressionHandler("skill", UOS_Expr_Skill);
            UOScript.Interpreter.RegisterExpressionHandler("contents", UOS_Expr_Contents);
            UOScript.Interpreter.RegisterExpressionHandler("distance", UOS_Expr_Distance);
            UOScript.Interpreter.RegisterExpressionHandler("injournal", UOS_Expr_InJournal);
            UOScript.Interpreter.RegisterExpressionHandler("property", UOS_Expr_Property);
            UOScript.Interpreter.RegisterExpressionHandler("listexists", UOS_Expr_ListExists);
            UOScript.Interpreter.RegisterExpressionHandler("list", UOS_Expr_ListCount);
            UOScript.Interpreter.RegisterExpressionHandler("timerexists", UOS_Expr_TimerExists);
            UOScript.Interpreter.RegisterExpressionHandler("war", UOS_Expr_War);
            UOScript.Interpreter.RegisterExpressionHandler("gumpexists", UOS_Expr_GumpExists);
            UOScript.Interpreter.RegisterExpressionHandler("counttype", UOS_Expr_CountType);
            UOScript.Interpreter.RegisterExpressionHandler("findlayer", UOS_Expr_FindLayer);
            UOScript.Interpreter.RegisterExpressionHandler("buffexists", UOS_Expr_BuffExists);
            UOScript.Interpreter.RegisterExpressionHandler("inlist", UOS_Expr_InList);
            UOScript.Interpreter.RegisterExpressionHandler("inparty", UOS_Expr_InParty);
            UOScript.Interpreter.RegisterExpressionHandler("mana", UOS_Expr_Mana);
            UOScript.Interpreter.RegisterExpressionHandler("x", UOS_Expr_X);
            UOScript.Interpreter.RegisterExpressionHandler("y", UOS_Expr_Y);
            UOScript.Interpreter.RegisterExpressionHandler("z", UOS_Expr_Z);
            UOScript.Interpreter.RegisterExpressionHandler("name", UOS_Expr_Name);

            _registered = true;
        }

        private static bool UOS_Msg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            string msg = "";
            foreach (var a in args) msg += " " + a.AsString();
            GameActions.Say(msg.TrimStart(), ProfileManager.CurrentProfile?.SpeechHue ?? 0);
            return true;
        }

        private static bool UOS_SysMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            ushort hue = args.Length > 1 ? args[1].AsUShort() : (ushort)946;
            GameActions.Print(args[0].AsString(), hue);
            return true;
        }

        private static bool UOS_Pause(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            UOScript.Interpreter.Pause(args[0].AsInt());
            return true;
        }

        private static bool UOS_ClickObject(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.SingleClick(args[0].AsSerial());
            return true;
        }

        private static bool UOS_Attack(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.Attack(args[0].AsSerial());
            return true;
        }

        private static bool UOS_UseObject(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.DoubleClickQueued(World.Get(args[0].AsSerial()));
            return true;
        }

        private static bool UOS_UseType(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            uint gfx = args[0].AsUInt();
            uint container = args[1].AsSerial();
            ushort hue = args.Length > 2 ? args[2].AsUShort() : ushort.MaxValue;
            var items = Utility.FindItems(gfx, parOrRootContainer: container, hue: hue);
            if (items.Count > 0)
                GameActions.DoubleClickQueued(items[0]);
            return true;
        }

        private static bool UOS_MoveItem(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            ushort amt = args.Length > 2 ? args[2].AsUShort() : (ushort)0;
            GameActions.GrabItem(args[0].AsSerial(), amt, args[1].AsSerial(), !force);
            return true;
        }

        private static bool UOS_Walk(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            World.Player.Walk(GetDirection(args[0].AsString()), false);
            return true;
        }

        private static bool UOS_Run(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            World.Player.Walk(GetDirection(args[0].AsString()), true);
            return true;
        }

        private static bool UOS_CancelTarget(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (TargetManager.IsTargeting) TargetManager.CancelTarget();
            return true;
        }

        private static bool UOS_SetAlias(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            UOScript.Interpreter.SetAlias(args[0].AsString(), args[1].AsSerial());
            return true;
        }

        private static bool UOS_UnsetAlias(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            UOScript.Interpreter.SetAlias(args[0].AsString(), uint.MaxValue);
            return true;
        }

        private static bool UOS_CreateList(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            UOScript.Interpreter.CreateList(args[0].AsString());
            return true;
        }

        private static bool UOS_PushList(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            UOScript.Interpreter.PushList(args[0].AsString(), args[1], false, force);
            return true;
        }

        private static bool UOS_ClearList(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            UOScript.Interpreter.ClearList(args[0].AsString());
            return true;
        }

        private static bool UOS_RemoveList(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            UOScript.Interpreter.DestroyList(args[0].AsString());
            return true;
        }

        private static bool UOS_PopList(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            UOScript.Interpreter.PopList(args[0].AsString(), args[1]);
            return true;
        }

        private static bool UOS_BandageSelf(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            GameActions.BandageSelf();
            return true;
        }

        private static bool UOS_UseSkill(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            string skill = args[0].AsString().ToLower();
            for (int i = 0; i < World.Player.Skills.Length; i++)
            {
                if (World.Player.Skills[i].Name.ToLower().Contains(skill))
                {
                    GameActions.UseSkill(World.Player.Skills[i].Index);
                    break;
                }
            }
            return true;
        }

        private static bool UOS_Cast(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.CastSpellByName(args[0].AsString());
            return true;
        }

        private static bool UOS_SetTimer(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            UOScript.Interpreter.CreateTimer(args[0].AsString());
            UOScript.Interpreter.SetTimer(args[0].AsString(), args[1].AsInt());
            return true;
        }

        private static bool UOS_RemoveTimer(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            UOScript.Interpreter.RemoveTimer(args[0].AsString());
            return true;
        }

        private static bool UOS_ToggleAutoLoot(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            return true;
        }

        private static bool UOS_Logout(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            GameActions.Logout();
            return true;
        }

        private static bool UOS_ShowNames(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            NameOverHeadManager.ToggleOverheads();
            return true;
        }

        private static bool UOS_ClearJournal(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            World.Journal.Clear();
            return true;
        }

        private static bool UOS_Target(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            TargetManager.Target(args[0].AsSerial());
            return true;
        }

        private static bool UOS_Turn(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            World.Player.Direction = GetDirection(args[0].AsString());
            return true;
        }

        private static bool UOS_ToggleHands(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            bool left = args[0].AsString().ToLower().Contains("left");
            Layer hand = left ? Layer.OneHanded : Layer.TwoHanded;
            Item i = World.Player.FindItemByLayer(hand);
            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
            if (i != null && backpack != null)
                GameActions.GrabItem(i.Serial, 0, backpack.Serial, true);
            return true;
        }

        private static bool UOS_EquipItem(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            uint serial = args[0].AsSerial();
            if (SerialHelper.IsItem(serial))
            {
                GameActions.PickUp(serial, 0, 0, 1);
                GameActions.Equip();
            }
            return true;
        }

        private static bool UOS_ToggleMounted(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            Item mount = World.Player.FindItemByLayer(Layer.Mount);
            if (mount != null)
                GameActions.DoubleClick(World.Player.Serial);
            return true;
        }

        private static bool UOS_Rename(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            NetClient.Socket.Send_RenameRequest(args[0].AsSerial(), args[1].AsString());
            return true;
        }

        private static bool UOS_HeadMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2) return true;
            MessageManager.HandleMessage(World.Get(args[0].AsSerial()), args[1].AsString(), "", ProfileManager.CurrentProfile?.SpeechHue ?? 0, MessageType.Label, 3, TextType.OBJECT);
            return true;
        }

        private static bool UOS_PartyMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.SayParty(args[0].AsString());
            return true;
        }

        private static bool UOS_GuildMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile?.GuildMessageHue ?? 0, MessageType.Guild);
            return true;
        }

        private static bool UOS_AllyMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile?.AllyMessageHue ?? 0, MessageType.Alliance);
            return true;
        }

        private static bool UOS_WhisperMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile?.WhisperHue ?? 0, MessageType.Whisper);
            return true;
        }

        private static bool UOS_YellMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile?.YellHue ?? 0, MessageType.Yell);
            return true;
        }

        private static bool UOS_EmoteMsg(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            GameActions.Say(args[0].AsString(), ProfileManager.CurrentProfile?.EmoteHue ?? 0, MessageType.Emote);
            return true;
        }

        private static bool UOS_Info(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
            return true;
        }

        private static bool UOS_PlayMacro(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            string name = args[0].AsString();
            if (string.IsNullOrWhiteSpace(name)) return true;
            var scriptFile = LegionScripting.FindScriptByName(name);
            if (scriptFile != null && scriptFile.ScriptType == ScriptType.UOScript)
            {
                LegionScripting.PlayScript(scriptFile);
                return true;
            }
            var mm = MacroManager.TryGetMacroManager();
            if (mm != null)
            {
                var macro = mm.FindMacro(name);
                if (macro != null)
                    mm.SetMacroToExecute(macro.Items as MacroObject);
            }
            return true;
        }

        private static bool UOS_Virtue(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            string v = args[0].AsString().ToLower();
            if (v.Contains("honor")) NetClient.Socket.Send_InvokeVirtueRequest(0x01);
            else if (v.Contains("sacrifice")) NetClient.Socket.Send_InvokeVirtueRequest(0x02);
            else if (v.Contains("valor")) NetClient.Socket.Send_InvokeVirtueRequest(0x03);
            return true;
        }

        private static bool UOS_MoveItemOffset(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 5) return true;
            GameActions.PickUp(args[0].AsSerial(), 0, 0, args[1].AsInt());
            GameActions.DropItem(args[0].AsSerial(), World.Player.X + args[2].AsInt(), World.Player.Y + args[3].AsInt(), World.Player.Z + args[4].AsInt(), 0);
            return true;
        }

        private static bool UOS_MoveType(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 3) return true;
            uint gfx = args[0].AsUInt();
            uint src = args[1].AsSerial();
            uint dst = args[2].AsSerial();
            ushort hue = args.Length > 3 ? args[3].AsUShort() : ushort.MaxValue;
            int amt = args.Length > 4 ? args[4].AsInt() : 1;
            var items = Utility.FindItems(gfx, parOrRootContainer: src, hue: hue);
            foreach (var item in items)
            {
                if (amt-- <= 0) break;
                GameActions.GrabItem(item.Serial, 0, dst, !force);
            }
            return true;
        }

        private static bool UOS_WaitForTarget(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            int type = args.Length > 0 ? args[0].AsInt() : 0;
            int timeout = args.Length > 1 ? args[1].AsInt() : 10000;
            UOScript.Interpreter.Timeout(timeout, () => { TargetManager.LastTargetInfo.Serial = 0; return TargetManager.LastTargetInfo.Serial != 0; });
            return true;
        }

        private static bool UOS_WaitForJournal(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            string text = args[0].AsString();
            int timeout = args.Length > 1 ? args[1].AsInt() : 10000;
            UOScript.Interpreter.Timeout(timeout, () => JournalManager.Entries.Any(e => e.Text != null && e.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0));
            return true;
        }

        private static bool UOS_WaitForGump(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            int timeout = args.Length > 0 ? args[0].AsInt() : 5000;
            UOScript.Interpreter.Timeout(timeout, () => true);
            return true;
        }

        private static bool UOS_ReplyGump(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            int buttonId = args[0].AsInt();
            uint gumpId = args.Length > 1 ? args[1].AsUInt() : World.Player.LastGumpID;
            var g = UIManager.GetGumpServer(gumpId);
            if (g != null)
                GameActions.ReplyGump(g.LocalSerial, gumpId, buttonId);
            return true;
        }

        private static bool UOS_CloseGump(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            uint gumpId = args.Length > 0 ? args[0].AsUInt() : World.Player.LastGumpID;
            UIManager.GetGumpServer(gumpId)?.Dispose();
            return true;
        }

        private static bool UOS_PromptAlias(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            string name = args[0].AsString();
            TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral);
            UOScript.Interpreter.Timeout(args.Length > 1 ? args[1].AsInt() : 10000, () =>
            {
                if (!TargetManager.IsTargeting && TargetManager.LastTargetInfo.Serial != 0)
                {
                    UOScript.Interpreter.SetAlias(name, TargetManager.LastTargetInfo.Serial);
                    return true;
                }
                return false;
            });
            return false;
        }

        private static bool UOS_PromptResponse(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1) return true;
            if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                NetClient.Socket.Send_ASCIIPromptResponse(args[0].AsString(), false);
            else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                NetClient.Socket.Send_UnicodePromptResponse(args[0].AsString(), Settings.GlobalSettings.Language, false);
            MessageManager.PromptData = default;
            return true;
        }

        private static bool UOS_CancelPrompt(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                NetClient.Socket.Send_ASCIIPromptResponse("", true);
            else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                NetClient.Socket.Send_UnicodePromptResponse("", Settings.GlobalSettings.Language, true);
            MessageManager.PromptData = default;
            return true;
        }

        private static bool UOS_WaitForPrompt(string cmd, UOScript.Argument[] args, bool quiet, bool force)
        {
            int timeout = args.Length > 0 ? args[0].AsInt() : 10000;
            UOScript.Interpreter.Timeout(timeout, () => MessageManager.PromptData.Prompt != ConsolePrompt.None);
            return true;
        }

        private static IComparable UOS_Expr_FindType(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 2) return 0;
            uint gfx = args[0].AsUInt();
            uint src = args[1].AsSerial();
            ushort hue = args.Length >= 3 ? args[2].AsUShort() : ushort.MaxValue;
            int range = args.Length >= 4 ? args[3].AsInt() : int.MaxValue;
            var items = Utility.FindItems(gfx, parOrRootContainer: src, hue: hue, groundRange: range);
            if (items.Count > 0) UOScript.Interpreter.SetAlias("found", items[0].Serial);
            return items.Count;
        }

        private static IComparable UOS_Expr_FindAlias(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            uint s = UOScript.Interpreter.GetAlias(args[0].AsString());
            return s != uint.MaxValue && World.Get(s) != null;
        }

        private static IComparable UOS_Expr_FindObject(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            return World.Get(args[0].AsSerial()) != null;
        }

        private static IComparable UOS_Expr_Skill(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return 0.0;
            for (int i = 0; i < World.Player.Skills.Length; i++)
                if (World.Player.Skills[i].Name.ToLower().Contains(args[0].AsString().ToLower()))
                    return (double)World.Player.Skills[World.Player.Skills[i].Index].Value;
            return 0.0;
        }

        private static IComparable UOS_Expr_Contents(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return 0;
            var c = World.Get(args[0].AsSerial()) as Item;
            return c?.Amount ?? 0;
        }

        private static IComparable UOS_Expr_Distance(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return int.MaxValue;
            var e = World.Get(args[0].AsSerial());
            return e != null ? (int)Math.Sqrt((e.X - World.Player.X) * (e.X - World.Player.X) + (e.Y - World.Player.Y) * (e.Y - World.Player.Y)) : int.MaxValue;
        }

        private static IComparable UOS_Expr_InJournal(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            return JournalManager.Entries.Any(e => e.Text != null && e.Text.IndexOf(args[0].AsString(), StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static IComparable UOS_Expr_Property(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 2) return false;
            var item = World.Get(args[0].AsSerial()) as Item;
            return item != null && Utility.SearchItemNameAndProps(args[1].AsString(), item);
        }

        private static IComparable UOS_Expr_ListExists(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            return UOScript.Interpreter.ListExists(args[0].AsString());
        }

        private static IComparable UOS_Expr_ListCount(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return 0;
            return UOScript.Interpreter.ListLength(args[0].AsString());
        }

        private static IComparable UOS_Expr_TimerExists(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            return UOScript.Interpreter.TimerExists(args[0].AsString());
        }

        private static IComparable UOS_Expr_War(string expr, UOScript.Argument[] args, bool quiet)
        {
            return World.Player.InWarMode;
        }

        private static IComparable UOS_Expr_GumpExists(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            return UIManager.IsModalOpen || UIManager.Gumps.Any(g => g.LocalSerial == args[0].AsSerial());
        }

        private static IComparable UOS_Expr_CountType(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 2) return 0;
            var items = Utility.FindItems(args[0].AsUInt(), parOrRootContainer: args[1].AsSerial());
            return items.Count;
        }

        private static IComparable UOS_Expr_FindLayer(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            Layer layer;
            if (!Enum.TryParse(args[0].AsString(), true, out layer)) return false;
            var item = World.Player.FindItemByLayer(layer);
            if (item != null) UOScript.Interpreter.SetAlias("found", item.Serial);
            return item != null;
        }

        private static IComparable UOS_Expr_BuffExists(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            if (World.Player?.BuffIcons == null) return false;
            string s = args[0].AsString();
            foreach (var buff in World.Player.BuffIcons.Values)
                if ((buff.Title != null && buff.Title.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) || (buff.Text != null && buff.Text.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
            return false;
        }

        private static IComparable UOS_Expr_InList(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 2) return false;
            return UOScript.Interpreter.ListContains(args[0].AsString(), args[1]);
        }

        private static IComparable UOS_Expr_InParty(string expr, UOScript.Argument[] args, bool quiet)
        {
            if (args.Length < 1) return false;
            return World.Party.Contains(args[0].AsSerial());
        }

        private static IComparable UOS_Expr_Mana(string expr, UOScript.Argument[] args, bool quiet)
        {
            return World.Player.Mana;
        }

        private static IComparable UOS_Expr_X(string expr, UOScript.Argument[] args, bool quiet)
        {
            return World.Player.X;
        }

        private static IComparable UOS_Expr_Y(string expr, UOScript.Argument[] args, bool quiet)
        {
            return World.Player.Y;
        }

        private static IComparable UOS_Expr_Z(string expr, UOScript.Argument[] args, bool quiet)
        {
            return World.Player.Z;
        }

        private static IComparable UOS_Expr_Name(string expr, UOScript.Argument[] args, bool quiet)
        {
            return World.Player.Name ?? "";
        }

        private static Direction GetDirection(string dir)
        {
            dir = dir.ToLower();
            if (dir == "north" || dir == "n") return Direction.North;
            if (dir == "south" || dir == "s") return Direction.South;
            if (dir == "east" || dir == "e") return Direction.East;
            if (dir == "west" || dir == "w") return Direction.West;
            if (dir == "northeast" || dir == "ne" || dir == "right") return Direction.Right;
            if (dir == "northwest" || dir == "nw" || dir == "up") return Direction.Up;
            if (dir == "southeast" || dir == "se" || dir == "down") return Direction.Down;
            if (dir == "southwest" || dir == "sw" || dir == "left") return Direction.Left;
            return Direction.North;
        }
    }
}
