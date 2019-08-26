#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

using Newtonsoft.Json;

using SDL2;

namespace ClassicUO.Game.Managers
{
    internal class MacroManager
    {
        private readonly uint[] _itemsInHand = new uint[2];

        private readonly byte[] _skillTable =
        {
            1, 2, 35, 4, 6, 12,
            14, 15, 16, 19, 21, 0xFF /*imbuing*/,
            23, 3, 46, 9, 30, 22,
            48, 32, 33, 47, 36, 38
        };

        private readonly int[] _spellsCountTable =
        {
            Constants.SPELLBOOK_1_SPELLS_COUNT,
            Constants.SPELLBOOK_2_SPELLS_COUNT,
            Constants.SPELLBOOK_3_SPELLS_COUNT,
            Constants.SPELLBOOK_4_SPELLS_COUNT,
            Constants.SPELLBOOK_5_SPELLS_COUNT,
            Constants.SPELLBOOK_6_SPELLS_COUNT,
            Constants.SPELLBOOK_7_SPELLS_COUNT
        };
        private Macro _firstNode;
        private MacroObject _lastMacro;
        private long _nextTimer;

        public MacroManager(Macro[] macros)
        {
            if (macros != null)
                for (int i = 0; i < macros.Length; i++)
                    AppendMacro(macros[i]);
        }

        public long WaitForTargetTimer { get; set; }

        public bool WaitingBandageTarget { get; set; }

        public void InitMacro(Macro first)
        {
            _firstNode = first;
        }

        public void AppendMacro(Macro macro)
        {
            if (_firstNode == null)
                _firstNode = macro;
            else
            {
                Macro o = _firstNode;

                while (o.Right != null)
                    o = o.Right;

                o.Right = macro;
                macro.Left = o;
                macro.Right = null;
            }
        }

        public void RemoveMacro(Macro macro)
        {
            if (_firstNode == null || macro == null)
                return;

            if (_firstNode == macro)
                _firstNode = macro.Right;

            if (macro.Right != null)
                macro.Right.Left = macro.Left;

            if (macro.Left != null)
                macro.Left.Right = macro.Right;

            macro.Left = null;
            macro.Right = null;
        }

        public List<Macro> GetAllMacros()
        {
            Macro m = _firstNode;

            while (m?.Left != null)
                m = m.Left;

            List<Macro> macros = new List<Macro>();

            while (true)
            {
                if (m != null)
                    macros.Add(m);
                else
                    break;

                m = m.Right;
            }

            return macros;
        }


        public Macro FindMacro(SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift)
        {
            Macro obj = _firstNode;

            while (obj != null)
            {
                if (obj.Key == key && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                    break;

                obj = obj.Right;
            }

            return obj;
        }

        public void SetMacroToExecute(MacroObject macro)
        {
            _lastMacro = macro;
        }

        public void Update()
        {
            while (_lastMacro != null)
            {
                switch (Process())
                {
                    case 2:
                        _lastMacro = null;

                        break;

                    case 1:

                        return;

                    case 0:
                        _lastMacro = _lastMacro?.Right;

                        break;
                }
            }
        }

        private int Process()
        {
            int result;

            if (_lastMacro == null) // MRC_STOP
                result = 2;
            else if (_nextTimer <= Engine.Ticks)
                result = Process(_lastMacro);
            else // MRC_BREAK_PARSER
                result = 1;

            return result;
        }

        private int Process(MacroObject macro)
        {
            if (macro == null)
                return 0;

            int result = 0;

            switch (macro.Code)
            {
                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:

                    MacroObjectString mos = (MacroObjectString) macro;

                    if (!string.IsNullOrEmpty(mos.Text))
                    {
                        MessageType type = MessageType.Regular;
                        ushort hue = Engine.Profile.Current.SpeechHue;

                        switch (macro.Code)
                        {
                            case MacroType.Emote:
                                type = MessageType.Emote;
                                hue = Engine.Profile.Current.EmoteHue;

                                break;

                            case MacroType.Whisper:
                                type = MessageType.Whisper;
                                hue = Engine.Profile.Current.WhisperHue;

                                break;

                            case MacroType.Yell:
                                type = MessageType.Yell;

                                break;
                        }

                        GameActions.Say(mos.Text, hue, type);
                    }

                    break;

                case MacroType.Walk:
                    byte dt = (byte) Direction.Up;

                    if (macro.SubCode != MacroSubType.NW)
                    {
                        dt = (byte) (macro.SubCode - 2);

                        if (dt > 7)
                            dt = 0;
                    }

                    if (!Pathfinder.AutoWalking)
                        World.Player.Walk((Direction) dt, false);

                    break;

                case MacroType.WarPeace:
                    GameActions.ChangeWarMode();

                    break;

                case MacroType.Paste:

                    if (SDL.SDL_HasClipboardText() != SDL.SDL_bool.SDL_FALSE)
                    {
                        string s = SDL.SDL_GetClipboardText();

                        if (!string.IsNullOrEmpty(s))
                            Engine.UI.SystemChat.textBox.Text += s;
                    }

                    break;

                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:

                    switch (macro.Code)
                    {
                        case MacroType.Open:

                            switch (macro.SubCode)
                            {
                                case MacroSubType.Configuration:
                                    OptionsGump opt = Engine.UI.GetGump<OptionsGump>();

                                    if (opt == null)
                                    {
                                        Engine.UI.Add(opt = new OptionsGump());
                                        opt.SetInScreen();
                                    }
                                    else
                                    {
                                        opt.SetInScreen();
                                        opt.BringOnTop();
                                    }

                                    break;

                                case MacroSubType.Paperdoll:
                                    GameActions.OpenPaperdoll(World.Player);

                                    break;

                                case MacroSubType.Status:

                                    if (StatusGumpBase.GetStatusGump() == null)
                                        StatusGumpBase.AddStatusGump(100, 100);

                                    break;

                                case MacroSubType.Journal:
                                    JournalGump journalGump = Engine.UI.GetGump<JournalGump>();

                                    if (journalGump == null)
                                    {
                                        Engine.UI.Add(new JournalGump
                                                          {X = 64, Y = 64});
                                    }
                                    else
                                    {
                                        journalGump.SetInScreen();
                                        journalGump.BringOnTop();
                                    }

                                    break;

                                case MacroSubType.Skills:
                                    World.SkillsRequested = true;
                                    NetClient.Socket.Send(new PSkillsRequest(World.Player));

                                    break;

                                case MacroSubType.MageSpellbook:
                                case MacroSubType.NecroSpellbook:
                                case MacroSubType.PaladinSpellbook:
                                case MacroSubType.BushidoSpellbook:
                                case MacroSubType.NinjitsuSpellbook:
                                case MacroSubType.SpellWeavingSpellbook:
                                case MacroSubType.MysticismSpellbook:

                                    SpellBookType type = SpellBookType.Magery;

                                    switch (macro.SubCode)
                                    {
                                        case MacroSubType.NecroSpellbook:
                                            type = SpellBookType.Necromancy;

                                            break;

                                        case MacroSubType.PaladinSpellbook:
                                            type = SpellBookType.Chivalry;

                                            break;

                                        case MacroSubType.BushidoSpellbook:
                                            type = SpellBookType.Bushido;

                                            break;

                                        case MacroSubType.NinjitsuSpellbook:
                                            type = SpellBookType.Ninjitsu;

                                            break;

                                        case MacroSubType.SpellWeavingSpellbook:
                                            type = SpellBookType.Spellweaving;

                                            break;

                                        case MacroSubType.MysticismSpellbook:
                                            type = SpellBookType.Mysticism;

                                            break;

                                        case MacroSubType.BardSpellbook:
                                            type = SpellBookType.Bardic;

                                            break;
                                    }

                                    NetClient.Socket.Send(new POpenSpellBook((byte) type));

                                    break;

                                case MacroSubType.Chat:
                                    Log.Message(LogTypes.Warning, $"Macro '{macro.SubCode}' not implemented");

                                    break;

                                case MacroSubType.Backpack:
                                    Item backpack = World.Player.Equipment[(int) Layer.Backpack];

                                    if (backpack != null)
                                        GameActions.DoubleClick(backpack);

                                    break;

                                case MacroSubType.Overview:
                                    MiniMapGump miniMapGump = Engine.UI.GetGump<MiniMapGump>();

                                    if (miniMapGump == null)
                                        Engine.UI.Add(new MiniMapGump());
                                    else
                                    {
                                        miniMapGump.ToggleSize();
                                        miniMapGump.SetInScreen();
                                        miniMapGump.BringOnTop();
                                    }

                                    break;

                                case MacroSubType.WorldMap:
                                    Log.Message(LogTypes.Warning, $"Macro '{macro.SubCode}' not implemented");

                                    break;

                                case MacroSubType.Mail:
                                case MacroSubType.PartyManifest:
                                    var party = Engine.UI.GetGump<PartyGumpAdvanced>();

                                    if (party == null)
                                        Engine.UI.Add(new PartyGumpAdvanced());
                                    else
                                        party.BringOnTop();

                                    break;

                                case MacroSubType.Guild:
                                    GameActions.OpenGuildGump();

                                    break;

                                case MacroSubType.QuestLog:
                                    GameActions.RequestQuestMenu();

                                    break;

                                case MacroSubType.PartyChat:
                                case MacroSubType.CombatBook:
                                case MacroSubType.RacialAbilitiesBook:
                                case MacroSubType.BardSpellbook:
                                    Log.Message(LogTypes.Warning, $"Macro '{macro.SubCode}' not implemented");

                                    break;
                            }

                            break;

                        case MacroType.Close:
                        case MacroType.Minimize: // TODO: miniminze/maximize
                        case MacroType.Maximize:

                            switch (macro.SubCode)
                            {
                                case MacroSubType.Configuration:

                                    if (macro.Code == MacroType.Close)
                                        Engine.UI.GetGump<OptionsGump>()?.Dispose();

                                    break;

                                case MacroSubType.Paperdoll:

                                    if (macro.Code == MacroType.Close)
                                        Engine.UI.GetGump<PaperDollGump>()?.Dispose();

                                    break;

                                case MacroSubType.Status:

                                    if (macro.Code == MacroType.Close)
                                        StatusGumpBase.GetStatusGump()?.Dispose();

                                    break;

                                case MacroSubType.Journal:

                                    if (macro.Code == MacroType.Close)
                                        Engine.UI.GetGump<JournalGump>()?.Dispose();

                                    break;

                                case MacroSubType.Skills:

                                    if (macro.Code == MacroType.Close)
                                        Engine.UI.GetGump<SkillGumpAdvanced>()?.Dispose();

                                    break;

                                case MacroSubType.MageSpellbook:
                                case MacroSubType.NecroSpellbook:
                                case MacroSubType.PaladinSpellbook:
                                case MacroSubType.BushidoSpellbook:
                                case MacroSubType.NinjitsuSpellbook:
                                case MacroSubType.SpellWeavingSpellbook:
                                case MacroSubType.MysticismSpellbook:

                                    if (macro.Code == MacroType.Close)
                                        Engine.UI.GetGump<SpellbookGump>()?.Dispose();

                                    break;

                                case MacroSubType.Chat:
                                    Log.Message(LogTypes.Warning, $"Macro '{macro.SubCode}' not implemented");

                                    break;

                                case MacroSubType.Overview:

                                    if (macro.Code == MacroType.Close)
                                        Engine.UI.GetGump<MiniMapGump>()?.Dispose();

                                    break;

                                case MacroSubType.Mail:
                                    Log.Message(LogTypes.Warning, $"Macro '{macro.SubCode}' not implemented");

                                    break;

                                case MacroSubType.PartyManifest:

                                    if (macro.Code == MacroType.Close)
                                        Engine.UI.GetGump<PartyGumpAdvanced>()?.Dispose();

                                    break;

                                case MacroSubType.PartyChat:
                                case MacroSubType.CombatBook:
                                case MacroSubType.RacialAbilitiesBook:
                                case MacroSubType.BardSpellbook:
                                    Log.Message(LogTypes.Warning, $"Macro '{macro.SubCode}' not implemented");

                                    break;
                            }

                            break;
                    }

                    break;

                case MacroType.OpenDoor:
                    GameActions.OpenDoor();

                    break;

                case MacroType.UseSkill:
                    int skill = macro.SubCode - MacroSubType.Anatomy;

                    if (skill >= 0 && skill < 24)
                    {
                        skill = _skillTable[skill];

                        if (skill != 0xFF)
                            GameActions.UseSkill(skill);
                    }

                    break;

                case MacroType.LastSkill:
                    GameActions.UseSkill(GameActions.LastSkillIndex);

                    break;

                case MacroType.CastSpell:
                    int spell = macro.SubCode - MacroSubType.Clumsy + 1;

                    if (spell > 0 && spell <= 151)
                    {
                        int totalCount = 0;
                        int spellType = 0;

                        for (spellType = 0; spellType < 7; spellType++)
                        {
                            totalCount += _spellsCountTable[spellType];

                            if (spell < totalCount)
                                break;
                        }

                        if (spellType < 7)
                        {
                            spell += spellType * 100;

                            if (spellType > 2)
                                spell += 100;

                            GameActions.CastSpell(spell);
                        }
                    }

                    break;

                case MacroType.LastSpell:
                    GameActions.CastSpell(GameActions.LastSpellIndex);

                    break;

                case MacroType.Bow:
                case MacroType.Salute:
                    int index = macro.Code - MacroType.Bow;

                    const string BOW = "bow";
                    const string SALUTE = "salute";

                    GameActions.EmoteAction(index == 0 ? BOW : SALUTE);

                    break;

                case MacroType.QuitGame:
                    Engine.SceneManager.GetScene<GameScene>()?.RequestQuitGame();

                    break;

                case MacroType.AllNames:
                    GameActions.AllNames();

                    break;

                case MacroType.LastObject:

                    if (World.Get(GameActions.LastObject) != null)
                        GameActions.DoubleClick(GameActions.LastObject);

                    break;

                case MacroType.UseItemInHand:
                    Item itemInLeftHand = World.Player.Equipment[(int)Layer.OneHanded];
                    if (itemInLeftHand != null)
                    {
                        GameActions.DoubleClick(itemInLeftHand.Serial);
                    } else
                    {
                        Item itemInRightHand = World.Player.Equipment[(int)Layer.TwoHanded];
                        if (itemInRightHand != null)
                        {
                            GameActions.DoubleClick(itemInRightHand.Serial);
                        }
                    }

                    break;

                case MacroType.LastTarget:

                    //if (WaitForTargetTimer == 0)
                    //    WaitForTargetTimer = Engine.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (TargetManager.IsTargeting)
                    {
                        //if (TargetManager.TargetingState != TargetType.Object)
                        //{
                        //    TargetManager.TargetGameObject(TargetManager.LastGameObject);
                        //}
                        //else 
                        TargetManager.TargetGameObject(World.Get(TargetManager.LastTarget));

                        WaitForTargetTimer = 0;
                    }
                    else if (WaitForTargetTimer < Engine.Ticks)
                        WaitForTargetTimer = 0;
                    else result = 1;

                    break;

                case MacroType.TargetSelf:

                    //if (WaitForTargetTimer == 0)
                    //    WaitForTargetTimer = Engine.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (TargetManager.IsTargeting)
                    {
                        TargetManager.TargetGameObject(World.Player);
                        WaitForTargetTimer = 0;
                    }
                    else if (WaitForTargetTimer < Engine.Ticks)
                        WaitForTargetTimer = 0;
                    else
                        result = 1;

                    break;

                case MacroType.ArmDisarm:
                    int handIndex = 1 - (macro.SubCode - MacroSubType.LeftHand);
                    GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                    if (handIndex < 0 || handIndex > 1 || gs.IsHoldingItem)
                        break;

                    if (_itemsInHand[handIndex] != 0)
                    {
                        Item item = World.Items.Get(_itemsInHand[handIndex]);

                        if (item != null)
                        {
                            GameActions.PickUp(item, 1);
                            gs.WearHeldItem(World.Player);
                        }

                        _itemsInHand[handIndex] = 0;
                    }
                    else
                    {
                        Item backpack = World.Player.Equipment[(int) Layer.Backpack];

                        if (backpack == null)
                            break;

                        Item item = World.Player.Equipment[(int) Layer.OneHanded + handIndex];

                        if (item != null)
                        {
                            _itemsInHand[handIndex] = item.Serial;

                            GameActions.PickUp(item, 1);
                            GameActions.DropItem(item, Position.INVALID, backpack);
                        }
                    }

                    break;

                case MacroType.WaitForTarget:

                    if (WaitForTargetTimer == 0)
                        WaitForTargetTimer = Engine.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (TargetManager.IsTargeting || WaitForTargetTimer < Engine.Ticks)
                        WaitForTargetTimer = 0;
                    else
                        result = 1;

                    break;

                case MacroType.TargetNext:

                    if (TargetManager.LastTarget.IsMobile)
                    {
                        Mobile mob = World.Mobiles.Get(TargetManager.LastTarget);

                        if (mob == null)
                            break;

                        if (mob.HitsMax == 0)
                            NetClient.Socket.Send(new PStatusRequest(mob));

                        TargetManager.LastAttack = mob.Serial;
                    }

                    break;

                case MacroType.AttackLast:
                    GameActions.Attack(TargetManager.LastTarget);

                    break;

                case MacroType.Delay:
                    MacroObjectString mosss = (MacroObjectString) macro;
                    string str = mosss.Text;

                    if (!string.IsNullOrEmpty(str) && int.TryParse(str, out int rr))
                        _nextTimer = Engine.Ticks + rr;

                    break;

                case MacroType.CircleTrans:
                    Engine.Profile.Current.UseCircleOfTransparency = !Engine.Profile.Current.UseCircleOfTransparency;

                    break;

                case MacroType.CloseGump:

                    Engine.UI.Gumps
                          .Where(s => !(s is TopBarGump) && !(s is BuffGump) && !(s is WorldViewportGump))
                          .ToList()
                          .ForEach(s => s.Dispose());

                    break;

                case MacroType.AlwaysRun:
                    Engine.Profile.Current.AlwaysRun = !Engine.Profile.Current.AlwaysRun;
                    GameActions.Print($"Always run is now {(Engine.Profile.Current.AlwaysRun ? "on" : "off")}.");

                    break;

                case MacroType.SaveDesktop:
                    Engine.Profile.Current?.Save(Engine.UI.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());

                    break;

                case MacroType.EnableRangeColor:
                    Engine.Profile.Current.NoColorObjectsOutOfRange = true;

                    break;

                case MacroType.DisableRangeColor:
                    Engine.Profile.Current.NoColorObjectsOutOfRange = false;

                    break;

                case MacroType.ToggleRangeColor:
                    Engine.Profile.Current.NoColorObjectsOutOfRange = !Engine.Profile.Current.NoColorObjectsOutOfRange;

                    break;

                case MacroType.AttackSelectedTarget:

                    // TODO:
                    break;

                case MacroType.UseSelectedTarget:

                    // TODO:
                    break;

                case MacroType.CurrentTarget:

                    // TODO:
                    break;

                case MacroType.TargetSystemOnOff:

                    // TODO:
                    break;

                case MacroType.BandageSelf:
                case MacroType.BandageTarget:

                    if (FileManager.ClientVersion < ClientVersions.CV_5020 || Engine.Profile.Current.BandageSelfOld)
                    {
                        if (WaitingBandageTarget)
                        {
                            if (WaitForTargetTimer == 0)
                                WaitForTargetTimer = Engine.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                            if (TargetManager.IsTargeting)
                                TargetManager.TargetGameObject(macro.Code == MacroType.BandageSelf ? World.Player : World.Mobiles.Get(TargetManager.LastTarget));
                            else
                                result = 1;

                            WaitingBandageTarget = false;
                            WaitForTargetTimer = 0;
                        }
                        else
                        {
                            var bandage = World.Player.FindBandage();

                            if (bandage != null)
                            {
                                WaitingBandageTarget = true;
                                GameActions.DoubleClick(bandage);
                                result = 1;
                            }
                        }
                    }
                    else
                    {
                        var bandage = World.Player.FindBandage();

                        if (bandage != null)
                        {
                            if (macro.Code == MacroType.BandageSelf)
                                NetClient.Socket.Send(new PTargetSelectedObject(bandage.Serial, World.Player.Serial));
                            else
                            {
                                // TODO: NewTargetSystem
                                Log.Message(LogTypes.Warning, "BandageTarget (NewTargetSystem) not implemented yet.");
                            }
                        }
                    }

                    break;

                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:

                    if (macro is MacroObjectString moss && !string.IsNullOrEmpty(moss.Text) && byte.TryParse(moss.Text, out byte res))
                    {
                        if (res < Constants.MIN_VIEW_RANGE)
                            res = Constants.MIN_VIEW_RANGE;
                        else if (res > Constants.MAX_VIEW_RANGE)
                            res = Constants.MAX_VIEW_RANGE;

                        World.ClientViewRange = res;

                        GameActions.Print($"ClientViewRange is now {res}.");
                    }

                    break;

                case MacroType.IncreaseUpdateRange:
                    World.ClientViewRange++;

                    if (World.ClientViewRange > Constants.MAX_VIEW_RANGE)
                        World.ClientViewRange = Constants.MAX_VIEW_RANGE;

                    GameActions.Print($"ClientViewRange is now {World.ClientViewRange}.");

                    break;

                case MacroType.DecreaseUpdateRange:
                    World.ClientViewRange--;

                    if (World.ClientViewRange < Constants.MIN_VIEW_RANGE)
                        World.ClientViewRange = Constants.MIN_VIEW_RANGE;
                    GameActions.Print($"ClientViewRange is now {World.ClientViewRange}.");

                    break;

                case MacroType.MaxUpdateRange:
                    World.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    GameActions.Print($"ClientViewRange is now {World.ClientViewRange}.");

                    break;

                case MacroType.MinUpdateRange:
                    World.ClientViewRange = Constants.MIN_VIEW_RANGE;
                    GameActions.Print($"ClientViewRange is now {World.ClientViewRange}.");

                    break;

                case MacroType.DefaultUpdateRange:
                    World.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    GameActions.Print($"ClientViewRange is now {World.ClientViewRange}.");

                    break;

                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                    // TODO:
                    int scantype = macro.SubCode - MacroSubType.Hostile;
                    int scanRange = macro.Code - MacroType.SelectNext;


                    switch (scanRange)
                    {
                        case 0:

                            break;

                        case 1:

                            break;

                        case 2:

                            break;
                    }


                    break;

                case MacroType.ToggleBuffIconGump:
                    BuffGump buff = Engine.UI.GetGump<BuffGump>();

                    if (buff != null)
                        buff.Dispose();
                    else
                        Engine.UI.Add(new BuffGump(100, 100));

                    break;

                case MacroType.InvokeVirtue:
                    byte id = (byte) (macro.SubCode - MacroSubType.Honor + 31);
                    NetClient.Socket.Send(new PInvokeVirtueRequest(id));

                    break;

                case MacroType.PrimaryAbility:
                    GameActions.UsePrimaryAbility();

                    break;

                case MacroType.SecondaryAbility:
                    GameActions.UseSecondaryAbility();

                    break;

                case MacroType.ToggleGargoyleFly:

                    if (World.Player.Race == RaceType.GARGOYLE)
                        NetClient.Socket.Send(new PToggleGargoyleFlying());

                    break;

                case MacroType.EquipLastWeapon:
                    NetClient.Socket.Send(new PEquipLastWeapon());

                    break;

                case MacroType.KillGumpOpen:
                    // TODO:

                    break;

                case MacroType.DefaultScale:
                    Engine.SceneManager.GetScene<GameScene>().Scale = 1;

                    break;

                case MacroType.ToggleChatVisibility:
                    Engine.UI.SystemChat?.ToggleChatVisibility();

                    break;

                case MacroType.MovePlayer:
                    switch (macro.SubCode)
                    {
                        case MacroSubType.Top:
                            break;

                        case MacroSubType.Right:
                            break;

                        case MacroSubType.Down:
                            break;

                        case MacroSubType.Left:
                            break;
                    }

                    break;

                case MacroType.Aura:
                    // hold to draw
                    break;

                case MacroType.AuraOnOff:
                    Engine.AuraManager.ToggleVisibility();

                    break;

                case MacroType.Grab:
                    GameActions.Print("Target an Item to grab it.");
                    TargetManager.SetTargeting(CursorTarget.Grab, Serial.INVALID, TargetType.Neutral);

                    break;

                case MacroType.SetGrabBag:
                    GameActions.Print("Target the container to Grab items into.");
                    TargetManager.SetTargeting(CursorTarget.SetGrabBag, Serial.INVALID, TargetType.Neutral);

                    break;

                case MacroType.NamesOnOff:
                    NameOverHeadManager.ToggleOverheads();

                    break;

                case MacroType.UsePotion:
                    scantype = macro.SubCode - MacroSubType.ConfusionBlastPotion;

                    ushort start = (ushort) (0x0F06 + scantype);

                    Item potion = World.Player.FindItemByGraphic(start);
                    if (potion != null)
                        GameActions.DoubleClick(potion);

                    break;
                    
                 case MacroType.CloseAllHealthBars:

                    var healthBarGumps = Engine.UI.Gumps.OfType<HealthBarGump>();

                    foreach (var healthbar in healthBarGumps)
                    {
                        if (Engine.UI.AnchorManager[healthbar] == null && (healthbar.LocalSerial != World.Player))
                        {
                            healthbar.Dispose();
                        }
                    }
                    break;
            }


            return result;
        }
    }

    [JsonObject]
    internal class Macro : IEquatable<Macro>, INode<Macro>
    {
        public Macro(string name, SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift)
        {
            Name = name;

            Key = key;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public string Name { get; }

        public SDL.SDL_Keycode Key { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }

        public MacroObject FirstNode { get; set; }

        public bool Equals(Macro other)
        {
            if (other == null)
                return false;

            return Key == other.Key && Alt == other.Alt && Ctrl == other.Ctrl && Shift == other.Shift;
        }

        [JsonIgnore] public Macro Left { get; set; }
        [JsonIgnore] public Macro Right { get; set; }

        public static MacroObject Create(MacroType code)
        {
            MacroObject obj;

            switch (code)
            {
                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:
                case MacroType.Delay:
                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:
                    obj = new MacroObjectString(code, MacroSubType.MSC_NONE);

                    break;

                default:
                    obj = new MacroObject(code, MacroSubType.MSC_NONE);

                    break;
            }

            return obj;
        }

        public static Macro CreateEmptyMacro(string name)
        {
            Macro macro = new Macro(name, 0, false, false, false);
            MacroObject item = new MacroObject(MacroType.None, MacroSubType.MSC_NONE);


            if (macro.FirstNode == null)
                macro.FirstNode = item;
            else
            {
                MacroObject o = macro.FirstNode;

                while (o.Right != null)
                    o = o.Right;

                o.Right = item;
                item.Left = o;
            }

            return macro;
        }

        public static void GetBoundByCode(MacroType code, ref int count, ref int offset)
        {
            switch (code)
            {
                case MacroType.Walk:
                    offset = (int) MacroSubType.NW;
                    count = MacroSubType.Configuration - MacroSubType.NW;

                    break;

                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                    offset = (int) MacroSubType.Configuration;
                    count = MacroSubType.Anatomy - MacroSubType.Configuration;

                    break;

                case MacroType.UseSkill:
                    offset = (int) MacroSubType.Anatomy;
                    count = MacroSubType.LeftHand - MacroSubType.Anatomy;

                    break;

                case MacroType.ArmDisarm:
                    offset = (int) MacroSubType.LeftHand;
                    count = MacroSubType.Honor - MacroSubType.LeftHand;

                    break;

                case MacroType.InvokeVirtue:
                    offset = (int) MacroSubType.Honor;
                    count = MacroSubType.Clumsy - MacroSubType.Honor;

                    break;

                case MacroType.CastSpell:
                    offset = (int) MacroSubType.Clumsy;
                    count = MacroSubType.Hostile - MacroSubType.Clumsy;

                    break;

                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                    offset = (int) MacroSubType.Hostile;
                    count = MacroSubType.MscTotalCount - MacroSubType.Hostile;

                    break;

                case MacroType.MovePlayer:
                    offset = (int) MacroSubType.Top;
                    count = 4;

                    break;

                case MacroType.UsePotion:
                    offset = (int) MacroSubType.ConfusionBlastPotion;
                    count = MacroSubType.ExplosionPotion - MacroSubType.ConfusionBlastPotion;
                    break;
            }
        }
    }

    [JsonObject]
    internal class MacroObject : INode<MacroObject>
    {
        [JsonConstructor]
        public MacroObject(MacroType code, MacroSubType sub)
        {
            Code = code;
            SubCode = sub;

            switch (code)
            {
                case MacroType.Walk:
                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                case MacroType.UseSkill:
                case MacroType.ArmDisarm:
                case MacroType.InvokeVirtue:
                case MacroType.CastSpell:
                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                case MacroType.MovePlayer:
                case MacroType.UsePotion:

                    if (sub == MacroSubType.MSC_NONE)
                    {
                        int count = 0;
                        int offset = 0;
                        Macro.GetBoundByCode(code, ref count, ref offset);
                        SubCode = (MacroSubType) offset;
                    }

                    HasSubMenu = 1;

                    break;

                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:
                case MacroType.Delay:
                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:
                    HasSubMenu = 2;

                    break;

                default:
                    HasSubMenu = 0;

                    break;
            }
        }

        [JsonProperty] public MacroType Code { get; set; }
        [JsonProperty] public MacroSubType SubCode { get; set; }
        [JsonProperty] public sbyte HasSubMenu { get; set; }

        [JsonIgnore] public MacroObject Left { get; set; }
        [JsonProperty] public MacroObject Right { get; set; }

        public virtual bool HasString()
        {
            return false;
        }
    }

    [JsonObject]
    internal class MacroObjectString : MacroObject
    {
        [JsonConstructor]
        public MacroObjectString(MacroType code, MacroSubType sub, string str = "") : base(code, sub)
        {
            Text = str;
        }

        [JsonProperty] public string Text { get; set; }

        public override bool HasString()
        {
            return true;
        }
    }

    internal enum MacroType
    {
        None = 0,
        Say,
        Emote,
        Whisper,
        Yell,
        Walk,
        WarPeace,
        Paste,
        Open,
        Close,
        Minimize,
        Maximize,
        OpenDoor,
        UseSkill,
        LastSkill,
        CastSpell,
        LastSpell,
        LastObject,
        Bow,
        Salute,
        QuitGame,
        AllNames,
        LastTarget,
        TargetSelf,
        ArmDisarm,
        WaitForTarget,
        TargetNext,
        AttackLast,
        Delay,
        CircleTrans,
        CloseGump,
        AlwaysRun,
        SaveDesktop,
        KillGumpOpen,
        PrimaryAbility,
        SecondaryAbility,
        EquipLastWeapon,
        SetUpdateRange,
        ModifyUpdateRange,
        IncreaseUpdateRange,
        DecreaseUpdateRange,
        MaxUpdateRange,
        MinUpdateRange,
        DefaultUpdateRange,
        EnableRangeColor,
        DisableRangeColor,
        ToggleRangeColor,
        InvokeVirtue,
        SelectNext,
        SelectPrevious,
        SelectNearest,
        AttackSelectedTarget,
        UseSelectedTarget,
        CurrentTarget,
        TargetSystemOnOff,
        ToggleBuffIconGump,
        BandageSelf,
        BandageTarget,
        ToggleGargoyleFly,
        DefaultScale,
        ToggleChatVisibility,
        MovePlayer,
        Aura,
        AuraOnOff,
        Grab,
        SetGrabBag,
        NamesOnOff,
        UseItemInHand,
        UsePotion,
        CloseAllHealthBars,

    }

    internal enum MacroSubType
    {
        MSC_NONE = 0,
        NW, //Walk group
        N,
        NE,
        E,
        SE,
        S,
        SW,
        W,
        Configuration, //Open/Close/Minimize/Maximize group
        Paperdoll,
        Status,
        Journal,
        Skills,
        MageSpellbook,
        Chat,
        Backpack,
        Overview,
        WorldMap,
        Mail,
        PartyManifest,
        PartyChat,
        NecroSpellbook,
        PaladinSpellbook,
        CombatBook,
        BushidoSpellbook,
        NinjitsuSpellbook,
        Guild,
        SpellWeavingSpellbook,
        QuestLog,
        MysticismSpellbook,
        RacialAbilitiesBook,
        BardSpellbook,
        Anatomy, //Skills group
        AnimalLore,
        AnimalTaming,
        ArmsLore,
        Begging,
        Cartography,
        DetectingHidden,
        Discordance,
        EvaluatingIntelligence,
        ForensicEvaluation,
        Hiding,
        Imbuing,
        Inscription,
        ItemIdentification,
        Meditation,
        Peacemaking,
        Poisoning,
        Provocation,
        RemoveTrap,
        SpiritSpeak,
        Stealing,
        Stealth,
        TasteIdentification,
        Tracking,
        LeftHand,
        ///Arm/Disarm group
        RightHand,
        Honor, //Invoke Virture group
        Sacrifice,
        Valor,
        Clumsy, //Cast Spell group
        CreateFood,
        Feeblemind,
        Heal,
        MagicArrow,
        NightSight,
        ReactiveArmor,
        Weaken,
        Agility,
        Cunning,
        Cure,
        Harm,
        MagicTrap,
        MagicUntrap,
        Protection,
        Strength,
        Bless,
        Fireball,
        MagicLock,
        Poison,
        Telekinesis,
        Teleport,
        Unlock,
        WallOfStone,
        ArchCure,
        ArchProtection,
        Curse,
        FireField,
        GreaterHeal,
        Lightning,
        ManaDrain,
        Recall,
        BladeSpirits,
        DispellField,
        Incognito,
        MagicReflection,
        MindBlast,
        Paralyze,
        PoisonField,
        SummonCreature,
        Dispel,
        EnergyBolt,
        Explosion,
        Invisibility,
        Mark,
        MassCurse,
        ParalyzeField,
        Reveal,
        ChainLightning,
        EnergyField,
        FlameStrike,
        GateTravel,
        ManaVampire,
        MassDispel,
        MeteorSwarm,
        Polymorph,
        Earthquake,
        EnergyVortex,
        Resurrection,
        AirElemental,
        SummonDaemon,
        EarthElemental,
        FireElemental,
        WaterElemental,
        AnimateDead,
        BloodOath,
        CorpseSkin,
        CurseWeapon,
        EvilOmen,
        HorrificBeast,
        LichForm,
        MindRot,
        PainSpike,
        PoisonStrike,
        Strangle,
        SummonFamilar,
        VampiricEmbrace,
        VengefulSpirit,
        Wither,
        WraithForm,
        Exorcism,
        CleanceByFire,
        CloseWounds,
        ConsecrateWeapon,
        DispelEvil,
        DivineFury,
        EnemyOfOne,
        HolyLight,
        NobleSacrifice,
        RemoveCurse,
        SacredJourney,
        HonorableExecution,
        Confidence,
        Evasion,
        CounterAttack,
        LightingStrike,
        MomentumStrike,
        FocusAttack,
        DeathStrike,
        AnimalForm,
        KiAttack,
        SurpriceAttack,
        Backstab,
        Shadowjump,
        MirrorImage,
        ArcaneCircle,
        GiftOfRenewal,
        ImmolatingWeapon,
        Attunement,
        Thunderstorm,
        NaturesFury,
        SummonFey,
        SummonFiend,
        ReaperForm,
        Wildfire,
        EssenceOfWind,
        DryadAllure,
        EtherealVoyage,
        WordOfDeath,
        GiftOfLife,
        ArcaneEmpowermen,
        NetherBolt,
        HealingStone,
        PurgeMagic,
        Enchant,
        Sleep,
        EagleStrike,
        AnimatedWeapon,
        StoneForm,
        SpellTrigger,
        MassSleep,
        CleansingWinds,
        Bombard,
        SpellPlague,
        HailStorm,
        NetherCyclone,
        RisingColossus,
        Inspire,
        Invigorate,
        Resilience,
        Perseverance,
        Tribulation,
        Despair,
        Hostile, //Select Next/Preveous/Nearest group
        Party,
        Follower,
        Object,
        Mobile,
        MscTotalCount,
        Top,
        Right,
        Down,
        Left,


        ConfusionBlastPotion,
        CurePotion,
        AgilityPotion,
        StrengthPotion,
        PoisonPotion,
        RefreshPotion,
        HealPotion,
        ExplosionPotion,
    }
}
