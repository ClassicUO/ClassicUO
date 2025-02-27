// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using SDL2;

namespace ClassicUO.Game.Managers
{
    internal sealed class MacroManager : LinkedObject
    {
        public static readonly string[] MacroNames = Enum.GetNames(typeof(MacroType));
        private readonly uint[] _itemsInHand = new uint[2];
        private MacroObject _lastMacro;
        private long _nextTimer;
        private readonly World _world;

        private readonly byte[] _skillTable =
        {
            1, 2, 35, 4, 6, 12,
            14, 15, 16, 19, 21, 56 /*imbuing*/,
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


        public MacroManager(World world) { _world = world; }

        public long WaitForTargetTimer { get; set; }

        public bool WaitingBandageTarget { get; set; }


        public void Load()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "macros.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No macros.xml file. Creating a default file.");

                Clear();
                CreateDefaultMacros();
                Save();

                return;
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());

                return;
            }


            Clear();

            XmlElement root = doc["macros"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("macro"))
                {
                    Macro macro = new Macro(xml.GetAttribute("name"));
                    macro.Load(xml);
                    PushToBack(macro);
                }
            }
        }

        public void Save()
        {
            List<Macro> list = GetAllMacros();

            string path = Path.Combine(ProfileManager.ProfilePath, "macros.xml");

            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("macros");

                foreach (Macro macro in list)
                {
                    macro.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        private void CreateDefaultMacros()
        {
            PushToBack
            (
                new Macro
                (
                    ResGeneral.Paperdoll,
                    (SDL.SDL_Keycode) 112,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 10)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    ResGeneral.Options,
                    (SDL.SDL_Keycode) 111,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 9)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    ResGeneral.Journal,
                    (SDL.SDL_Keycode) 106,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 12)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    ResGeneral.Backpack,
                    (SDL.SDL_Keycode) 105,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 16)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    ResGeneral.Radar,
                    (SDL.SDL_Keycode) 114,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 17)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    ResGeneral.Bow,
                    (SDL.SDL_Keycode) 98,
                    false,
                    true,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 18, 0)
                    {
                        SubMenuType = 0
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    ResGeneral.Salute,
                    (SDL.SDL_Keycode) 115,
                    false,
                    true,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 19, 0)
                    {
                        SubMenuType = 0
                    }
                }
            );
        }


        public List<Macro> GetAllMacros()
        {
            Macro m = (Macro) Items;

            while (m?.Previous != null)
            {
                m = (Macro) m.Previous;
            }

            List<Macro> macros = new List<Macro>();

            while (true)
            {
                if (m != null)
                {
                    macros.Add(m);
                }
                else
                {
                    break;
                }

                m = (Macro) m.Next;
            }

            return macros;
        }


        public Macro FindMacro(SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift)
        {
            Macro obj = (Macro) Items;

            while (obj != null)
            {
                if (obj.Key == key && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro) obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(MouseButtonType button, bool alt, bool ctrl, bool shift)
        {
            Macro obj = (Macro) Items;

            while (obj != null)
            {
                if (obj.MouseButton == button && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro) obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(bool wheelUp, bool alt, bool ctrl, bool shift)
        {
            Macro obj = (Macro) Items;

            while (obj != null)
            {
                if (obj.WheelScroll == true && obj.WheelUp == wheelUp && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro) obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(string name)
        {
            Macro obj = (Macro) Items;

            while (obj != null)
            {
                if (obj.Name == name)
                {
                    break;
                }

                obj = (Macro) obj.Next;
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

                    case 1: return;

                    case 0:
                        _lastMacro = (MacroObject) _lastMacro?.Next;

                        break;
                }
            }
        }

        private int Process()
        {
            int result;

            if (_lastMacro == null) // MRC_STOP
            {
                result = 2;
            }
            else if (_nextTimer <= Time.Ticks)
            {
                result = Process(_lastMacro);
            }
            else // MRC_BREAK_PARSER
            {
                result = 1;
            }

            return result;
        }

        private int Process(MacroObject macro)
        {
            if (macro == null)
            {
                return 0;
            }

            int result = 0;

            switch (macro.Code)
            {
                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:
                case MacroType.RazorMacro:

                    string text = ((MacroObjectString) macro).Text;

                    if (!string.IsNullOrEmpty(text))
                    {
                        MessageType type = MessageType.Regular;
                        ushort hue = ProfileManager.CurrentProfile.SpeechHue;

                        switch (macro.Code)
                        {
                            case MacroType.Emote:
                                text = ResGeneral.EmoteChar + text + ResGeneral.EmoteChar;
                                type = MessageType.Emote;
                                hue = ProfileManager.CurrentProfile.EmoteHue;

                                break;

                            case MacroType.Whisper:
                                type = MessageType.Whisper;
                                hue = ProfileManager.CurrentProfile.WhisperHue;

                                break;

                            case MacroType.Yell:
                                type = MessageType.Yell;

                                break;

                            case MacroType.RazorMacro:
                                text = ">macro " + text;

                                break;
                        }

                        GameActions.Say(text, hue, type);
                    }

                    break;

                case MacroType.Walk:
                    byte dt = (byte) Direction.Up;

                    if (macro.SubCode != MacroSubType.NW)
                    {
                        dt = (byte) (macro.SubCode - 2);

                        if (dt > 7)
                        {
                            dt = 0;
                        }
                    }

                    if (!_world.Player.Pathfinder.AutoWalking)
                    {
                        _world.Player.Walk((Direction) dt, false);
                    }

                    break;

                case MacroType.WarPeace:
                    GameActions.ToggleWarMode(_world.Player);

                    break;

                case MacroType.Paste:
                    string txt = StringHelper.GetClipboardText(true);

                    if (txt != null)
                    {
                        UIManager.SystemChat.TextBoxControl.AppendText(txt);
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
                                    GameActions.OpenSettings(_world);

                                    break;

                                case MacroSubType.Paperdoll:
                                    GameActions.OpenPaperdoll(_world, _world.Player);

                                    break;

                                case MacroSubType.Status:
                                    GameActions.OpenStatusBar(_world);

                                    break;

                                case MacroSubType.Journal:
                                    GameActions.OpenJournal(_world);

                                    break;

                                case MacroSubType.Skills:
                                    GameActions.OpenSkills(_world);

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
                                            type = SpellBookType.Mastery;

                                            break;
                                    }

                                    NetClient.Socket.Send_OpenSpellBook((byte)type);

                                    break;

                                case MacroSubType.Chat:
                                    GameActions.OpenChat(_world);

                                    break;

                                case MacroSubType.Backpack:
                                    GameActions.OpenBackpack(_world);

                                    break;

                                case MacroSubType.Overview:
                                    GameActions.OpenMiniMap(_world);

                                    break;

                                case MacroSubType.WorldMap:
                                    GameActions.OpenWorldMap(_world);

                                    break;

                                case MacroSubType.Mail:
                                case MacroSubType.PartyManifest:
                                    PartyGump party = UIManager.GetGump<PartyGump>();

                                    if (party == null)
                                    {
                                        int x = Client.Game.Window.ClientBounds.Width / 2 - 272;
                                        int y = Client.Game.Window.ClientBounds.Height / 2 - 240;
                                        UIManager.Add(new PartyGump(_world, x, y, _world.Party.CanLoot));
                                    }
                                    else
                                    {
                                        party.BringOnTop();
                                    }

                                    break;

                                case MacroSubType.Guild:
                                    GameActions.OpenGuildGump(_world);

                                    break;

                                case MacroSubType.QuestLog:
                                    GameActions.RequestQuestMenu(_world);

                                    break;

                                case MacroSubType.PartyChat:
                                case MacroSubType.CombatBook:
                                case MacroSubType.RacialAbilitiesBook:
                                case MacroSubType.BardSpellbook:
                                    Log.Warn($"Macro '{macro.SubCode}' not implemented");

                                    break;
                            }

                            break;

                        case MacroType.Close:
                        case MacroType.Minimize:
                        case MacroType.Maximize:

                            switch (macro.SubCode)
                            {
                                case MacroSubType.WorldMap:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.Dispose();
                                    }

                                    break;

                                case MacroSubType.Configuration:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<OptionsGump>()?.Dispose();
                                    }

                                    break;

                                case MacroSubType.Paperdoll:

                                    PaperDollGump paperdoll = UIManager.GetGump<PaperDollGump>(_world.Player.Serial);

                                    if (paperdoll != null)
                                    {
                                        if (macro.Code == MacroType.Close)
                                        {
                                            paperdoll.Dispose();
                                        }
                                        else if (macro.Code == MacroType.Minimize)
                                        {
                                            paperdoll.IsMinimized = true;
                                        }
                                        else if (macro.Code == MacroType.Maximize)
                                        {
                                            paperdoll.IsMinimized = false;
                                        }
                                    }

                                    break;

                                case MacroSubType.Status:

                                    StatusGumpBase status = StatusGumpBase.GetStatusGump();

                                    if (macro.Code == MacroType.Close)
                                    {
                                        if (status != null)
                                        {
                                            status.Dispose();
                                        }
                                        else
                                        {
                                            UIManager.GetGump<BaseHealthBarGump>(_world.Player)?.Dispose();
                                        }
                                    }
                                    else if (macro.Code == MacroType.Minimize)
                                    {
                                        if (status != null)
                                        {
                                            if (ProfileManager.CurrentProfile.StatusGumpBarMutuallyExclusive)
                                                status.Dispose();

                                            if (ProfileManager.CurrentProfile.CustomBarsToggled)
                                            {
                                                UIManager.Add(new HealthBarGumpCustom(_world, _world.Player) { X = status.ScreenCoordinateX, Y = status.ScreenCoordinateY });
                                            }
                                            else
                                            {
                                                UIManager.Add(new HealthBarGump(_world, _world.Player) { X = status.ScreenCoordinateX, Y = status.ScreenCoordinateY });
                                            }
                                        }
                                        else
                                        {
                                            UIManager.GetGump<BaseHealthBarGump>(_world.Player)?.BringOnTop();
                                        }
                                    }
                                    else if (macro.Code == MacroType.Maximize)
                                    {
                                        if (status != null)
                                        {
                                            status.BringOnTop();
                                        }
                                        else
                                        {
                                            BaseHealthBarGump healthbar = UIManager.GetGump<BaseHealthBarGump>(_world.Player);

                                            if (healthbar != null)
                                            {
                                                UIManager.Add(StatusGumpBase.AddStatusGump(_world, healthbar.ScreenCoordinateX, healthbar.ScreenCoordinateY));
                                            }
                                        }
                                    }

                                    break;

                                case MacroSubType.Journal:
                                    if(ProfileManager.CurrentProfile.UseAlternateJournal)
                                    {
                                        ResizableJournal rjournal = UIManager.GetGump<ResizableJournal>();
                                        if (macro.Code == MacroType.Close)
                                        {
                                            rjournal?.Dispose();
                                        }
                                        break;
                                    }

                                    JournalGump journal = UIManager.GetGump<JournalGump>();

                                    if (journal != null)
                                    {
                                        if (macro.Code == MacroType.Close)
                                        {
                                            journal.Dispose();
                                        }
                                        else if (macro.Code == MacroType.Minimize)
                                        {
                                            journal.IsMinimized = true;
                                        }
                                        else if (macro.Code == MacroType.Maximize)
                                        {
                                            journal.IsMinimized = false;
                                        }
                                    }

                                    break;

                                case MacroSubType.Skills:

                                    if (ProfileManager.CurrentProfile.StandardSkillsGump)
                                    {
                                        StandardSkillsGump skillgump = UIManager.GetGump<StandardSkillsGump>();

                                        if (skillgump != null)
                                        {
                                            if (macro.Code == MacroType.Close)
                                            {
                                                skillgump.Dispose();
                                            }
                                            else if (macro.Code == MacroType.Minimize)
                                            {
                                                skillgump.IsMinimized = true;
                                            }
                                            else if (macro.Code == MacroType.Maximize)
                                            {
                                                skillgump.IsMinimized = false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (macro.Code == MacroType.Close)
                                        {
                                            UIManager.GetGump<SkillGumpAdvanced>()?.Dispose();
                                        }
                                    }

                                    break;

                                case MacroSubType.MageSpellbook:
                                case MacroSubType.NecroSpellbook:
                                case MacroSubType.PaladinSpellbook:
                                case MacroSubType.BushidoSpellbook:
                                case MacroSubType.NinjitsuSpellbook:
                                case MacroSubType.SpellWeavingSpellbook:
                                case MacroSubType.MysticismSpellbook:

                                    SpellbookGump spellbook = UIManager.GetGump<SpellbookGump>();

                                    if (spellbook != null)
                                    {
                                        if (macro.Code == MacroType.Close)
                                        {
                                            spellbook.Dispose();
                                        }
                                        else if (macro.Code == MacroType.Minimize)
                                        {
                                            spellbook.IsMinimized = true;
                                        }
                                        else if (macro.Code == MacroType.Maximize)
                                        {
                                            spellbook.IsMinimized = false;
                                        }
                                    }

                                    break;

                                case MacroSubType.Overview:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.Dispose();
                                    }
                                    else if (macro.Code == MacroType.Minimize)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.ToggleSize(false);
                                    }
                                    else if (macro.Code == MacroType.Maximize)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.ToggleSize(true);
                                    }

                                    break;

                                case MacroSubType.Backpack:

                                    Item backpack = _world.Player.FindItemByLayer(Layer.Backpack);

                                    if (backpack != null)
                                    {
                                        ContainerGump backpackGump = UIManager.GetGump<ContainerGump>(backpack.Serial);

                                        if (backpackGump != null)
                                        {
                                            if (macro.Code == MacroType.Close)
                                            {
                                                backpackGump.Dispose();
                                            }
                                            else if (macro.Code == MacroType.Minimize)
                                            {
                                                backpackGump.IsMinimized = true;
                                            }
                                            else if (macro.Code == MacroType.Maximize)
                                            {
                                                backpackGump.IsMinimized = false;
                                            }
                                        }
                                    }

                                    break;

                                case MacroSubType.Mail:
                                    Log.Warn($"Macro '{macro.SubCode}' not implemented");

                                    break;

                                case MacroSubType.PartyManifest:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<PartyGump>()?.Dispose();
                                    }

                                    break;

                                case MacroSubType.PartyChat:
                                case MacroSubType.CombatBook:
                                case MacroSubType.RacialAbilitiesBook:
                                case MacroSubType.BardSpellbook:
                                    Log.Warn($"Macro '{macro.SubCode}' not implemented");

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
                        {
                            GameActions.UseSkill(skill);
                        }
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
                        int spellType;

                        for (spellType = 0; spellType < 7; spellType++)
                        {
                            totalCount += _spellsCountTable[spellType];

                            if (spell <= totalCount)
                            {
                                break;
                            }
                        }

                        if (spellType < 7)
                        {
                            spell -= totalCount - _spellsCountTable[spellType];
                            spell += spellType * 100;

                            if (spellType > 2)
                            {
                                spell += 100;

                                // fix offset for mysticism
                                if (spellType == 6)
                                {
                                    spell -= 23;
                                }
                            }

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
                    Client.Game.GetScene<GameScene>()?.RequestQuitGame();

                    break;

                case MacroType.AllNames:
                    GameActions.AllNames(_world);

                    break;

                case MacroType.LastObject:

                    if (_world.Get(_world.LastObject) != null)
                    {
                        GameActions.DoubleClick(_world, _world.LastObject);
                    }

                    break;

                case MacroType.UseItemInHand:
                    Item itemInLeftHand = _world.Player.FindItemByLayer(Layer.OneHanded);

                    if (itemInLeftHand != null)
                    {
                        GameActions.DoubleClick(_world, itemInLeftHand.Serial);
                    }
                    else
                    {
                        Item itemInRightHand = _world.Player.FindItemByLayer(Layer.TwoHanded);

                        if (itemInRightHand != null)
                        {
                            GameActions.DoubleClick(_world, itemInRightHand.Serial);
                        }
                    }

                    break;

                case MacroType.LastTarget:

                    //if (WaitForTargetTimer == 0)
                    //    WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (_world.TargetManager.IsTargeting)
                    {
                        //if (TargetManager.TargetingState != TargetType.Object)
                        //{
                        //    TargetManager.TargetGameObject(TargetManager.LastGameObject);
                        //}
                        //else

                        if (_world.TargetManager.TargetingState != CursorTarget.Object && !_world.TargetManager.LastTargetInfo.IsEntity)
                        {
                            _world.TargetManager.TargetLast();
                        }
                        else if (_world.TargetManager.LastTargetInfo.IsEntity)
                        {
                            _world.TargetManager.Target(_world.TargetManager.LastTargetInfo.Serial);
                        }
                        else
                        {
                            _world.TargetManager.Target(_world.TargetManager.LastTargetInfo.Graphic, _world.TargetManager.LastTargetInfo.X, _world.TargetManager.LastTargetInfo.Y, _world.TargetManager.LastTargetInfo.Z);
                        }

                        WaitForTargetTimer = 0;
                    }
                    else if (WaitForTargetTimer < Time.Ticks)
                    {
                        WaitForTargetTimer = 0;
                    }
                    else
                    {
                        result = 1;
                    }

                    break;

                case MacroType.TargetSelf:

                    //if (WaitForTargetTimer == 0)
                    //    WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (_world.TargetManager.IsTargeting)
                    {
                        _world.TargetManager.Target(_world.Player);
                        WaitForTargetTimer = 0;
                    }
                    else if (WaitForTargetTimer < Time.Ticks)
                    {
                        WaitForTargetTimer = 0;
                    }
                    else
                    {
                        result = 1;
                    }

                    break;

                case MacroType.ArmDisarm:
                    int handIndex = 1 - (macro.SubCode - MacroSubType.LeftHand);
                    GameScene gs = Client.Game.GetScene<GameScene>();

                    if (handIndex < 0 || handIndex > 1 || Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        break;
                    }

                    if (_itemsInHand[handIndex] != 0)
                    {
                        GameActions.PickUp(_world, _itemsInHand[handIndex], 0, 0, 1);
                        GameActions.Equip(_world);

                        _itemsInHand[handIndex] = 0;
                        _nextTimer = Time.Ticks + 1000;
                    }
                    else
                    {
                        Item backpack = _world.Player.FindItemByLayer(Layer.Backpack);

                        if (backpack == null)
                        {
                            break;
                        }

                        Item item = _world.Player.FindItemByLayer(Layer.OneHanded + (byte) handIndex);

                        if (item != null)
                        {
                            _itemsInHand[handIndex] = item.Serial;

                            GameActions.PickUp(_world, item, 0, 0, 1);

                            GameActions.DropItem
                            (
                                Client.Game.UO.GameCursor.ItemHold.Serial,
                                0xFFFF,
                                0xFFFF,
                                0,
                                backpack.Serial
                            );

                            _nextTimer = Time.Ticks + 1000;
                        }
                    }

                    break;

                case MacroType.WaitForTarget:

                    if (WaitForTargetTimer == 0)
                    {
                        WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;
                    }

                    if (_world.TargetManager.IsTargeting || WaitForTargetTimer < Time.Ticks)
                    {
                        WaitForTargetTimer = 0;
                    }
                    else
                    {
                        result = 1;
                    }

                    break;

                case MacroType.TargetNext:

                    uint sel_obj = _world.FindNext(ScanTypeObject.Mobiles, _world.TargetManager.LastTargetInfo.Serial, false);

                    if (SerialHelper.IsValid(sel_obj))
                    {
                        _world.TargetManager.LastTargetInfo.SetEntity(sel_obj);
                        _world.TargetManager.LastAttack = sel_obj;
                    }

                    break;

                case MacroType.AttackLast:
                    if (_world.TargetManager.LastTargetInfo.IsEntity)
                    {
                        GameActions.Attack(_world, _world.TargetManager.LastTargetInfo.Serial);
                    }

                    break;

                case MacroType.Delay:
                    MacroObjectString mosss = (MacroObjectString) macro;
                    string str = mosss.Text;

                    if (!string.IsNullOrEmpty(str) && int.TryParse(str, out int rr))
                    {
                        _nextTimer = Time.Ticks + rr;
                    }

                    break;

                case MacroType.CircleTrans:
                    ProfileManager.CurrentProfile.UseCircleOfTransparency = !ProfileManager.CurrentProfile.UseCircleOfTransparency;

                    break;

                case MacroType.CloseGump:

                    UIManager.Gumps.Where(s => !(s is TopBarGump) && !(s is BuffGump) && !(s is WorldViewportGump)).ToList().ForEach(s => s.Dispose());

                    break;

                case MacroType.AlwaysRun:
                    ProfileManager.CurrentProfile.AlwaysRun = !ProfileManager.CurrentProfile.AlwaysRun;

                    GameActions.Print(_world, ProfileManager.CurrentProfile.AlwaysRun ? ResGeneral.AlwaysRunIsNowOn : ResGeneral.AlwaysRunIsNowOff);

                    break;

                case MacroType.SaveDesktop:
                    ProfileManager.CurrentProfile?.Save(_world, ProfileManager.ProfilePath);

                    break;

                case MacroType.EnableRangeColor:
                    ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = true;

                    break;

                case MacroType.DisableRangeColor:
                    ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = false;

                    break;

                case MacroType.ToggleRangeColor:
                    ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = !ProfileManager.CurrentProfile.NoColorObjectsOutOfRange;

                    break;

                case MacroType.AttackSelectedTarget:

                    if (SerialHelper.IsMobile(_world.TargetManager.SelectedTarget))
                    {
                        GameActions.Attack(_world, _world.TargetManager.SelectedTarget);
                    }

                    break;

                case MacroType.UseSelectedTarget:
                    if (SerialHelper.IsValid(_world.TargetManager.SelectedTarget))
                    {
                        GameActions.DoubleClick(_world, _world.TargetManager.SelectedTarget);
                    }

                    break;

                case MacroType.CurrentTarget:

                    if (_world.TargetManager.SelectedTarget != 0)
                    {
                        if (WaitForTargetTimer == 0)
                        {
                            WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;
                        }

                        if (_world.TargetManager.IsTargeting)
                        {
                            _world.TargetManager.Target(_world.TargetManager.SelectedTarget);
                            WaitForTargetTimer = 0;
                        }
                        else if (WaitForTargetTimer < Time.Ticks)
                        {
                            WaitForTargetTimer = 0;
                        }
                        else
                        {
                            result = 1;
                        }
                    }

                    break;

                case MacroType.TargetSystemOnOff:

                    GameActions.Print(_world, ResGeneral.TargetSystemNotImplemented);

                    break;

                case MacroType.BandageSelf:
                case MacroType.BandageTarget:

                    if (Client.Game.UO.Version < ClientVersion.CV_5020 || ProfileManager.CurrentProfile.BandageSelfOld)
                    {
                        if (WaitingBandageTarget)
                        {
                            if (WaitForTargetTimer == 0)
                            {
                                WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;
                            }

                            if (_world.TargetManager.IsTargeting)
                            {
                                if (macro.Code == MacroType.BandageSelf)
                                {
                                    _world.TargetManager.Target(_world.Player);
                                }
                                else if (_world.TargetManager.LastTargetInfo.IsEntity)
                                {
                                    _world.TargetManager.Target(_world.TargetManager.LastTargetInfo.Serial);
                                }

                                WaitingBandageTarget = false;
                                WaitForTargetTimer = 0;
                            }
                            else if (WaitForTargetTimer < Time.Ticks)
                            {
                                WaitingBandageTarget = false;
                                WaitForTargetTimer = 0;
                            }
                            else
                            {
                                result = 1;
                            }
                        }
                        else
                        {
                            Item bandage = _world.Player.FindBandage();

                            if (bandage != null)
                            {
                                WaitingBandageTarget = true;
                                GameActions.DoubleClick(_world,bandage);
                                result = 1;
                            }
                        }
                    }
                    else
                    {
                        Item bandage = _world.Player.FindBandage();

                        if (bandage != null)
                        {
                            if (macro.Code == MacroType.BandageSelf)
                            {
                                NetClient.Socket.Send_TargetSelectedObject(bandage.Serial, _world.Player.Serial);
                            }
                            else if (SerialHelper.IsMobile(_world.TargetManager.SelectedTarget))
                            {
                                NetClient.Socket.Send_TargetSelectedObject(bandage.Serial, _world.TargetManager.SelectedTarget);
                            }
                        }
                    }

                    break;

                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:

                    if (macro is MacroObjectString moss && !string.IsNullOrEmpty(moss.Text) && byte.TryParse(moss.Text, out byte res))
                    {
                        if (res < Constants.MIN_VIEW_RANGE)
                        {
                            res = Constants.MIN_VIEW_RANGE;
                        }
                        else if (res > Constants.MAX_VIEW_RANGE)
                        {
                            res = Constants.MAX_VIEW_RANGE;
                        }

                        _world.ClientViewRange = res;

                        GameActions.Print(_world, string.Format(ResGeneral.ClientViewRangeIsNow0, res));
                    }

                    break;

                case MacroType.IncreaseUpdateRange:
                    _world.ClientViewRange++;

                    if (_world.ClientViewRange > Constants.MAX_VIEW_RANGE)
                    {
                        _world.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    }

                    GameActions.Print(_world, string.Format(ResGeneral.ClientViewRangeIsNow0, _world.ClientViewRange));

                    break;

                case MacroType.DecreaseUpdateRange:
                    _world.ClientViewRange--;

                    if (_world.ClientViewRange < Constants.MIN_VIEW_RANGE)
                    {
                        _world.ClientViewRange = Constants.MIN_VIEW_RANGE;
                    }

                    GameActions.Print(_world, string.Format(ResGeneral.ClientViewRangeIsNow0, _world.ClientViewRange));

                    break;

                case MacroType.MaxUpdateRange:
                    _world.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    GameActions.Print(_world, string.Format(ResGeneral.ClientViewRangeIsNow0, _world.ClientViewRange));

                    break;

                case MacroType.MinUpdateRange:
                    _world.ClientViewRange = Constants.MIN_VIEW_RANGE;
                    GameActions.Print(_world, string.Format(ResGeneral.ClientViewRangeIsNow0, _world.ClientViewRange));

                    break;

                case MacroType.DefaultUpdateRange:
                    _world.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    GameActions.Print(_world, string.Format(ResGeneral.ClientViewRangeIsNow0, _world.ClientViewRange));

                    break;

                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                    // scanRange:
                    // 0 - SelectNext
                    // 1 - SelectPrevious
                    // 2 - SelectNearest
                    ScanModeObject scanRange = (ScanModeObject)(macro.Code - MacroType.SelectNext);

                    // scantype:
                    // 0 - Hostile (only hostile mobiles: gray, criminal, enemy, murderer)
                    // 1 - Party (only party members)
                    // 2 - Follower (only your followers)
                    // 3 - Object (???)
                    // 4 - Mobile (any mobiles)
                    ScanTypeObject scantype = (ScanTypeObject)(macro.SubCode - MacroSubType.Hostile);

                    if (scanRange == ScanModeObject.Nearest)
                    {
                        SetLastTarget(_world.FindNearest(scantype));
                    }
                    else
                    {
                        SetLastTarget(_world.FindNext(scantype, _world.TargetManager.SelectedTarget, scanRange == ScanModeObject.Previous));
                    }

                    break;

                case MacroType.ToggleBuffIconGump:
                    BuffGump buff = UIManager.GetGump<BuffGump>();

                    if (buff != null)
                    {
                        buff.Dispose();
                    }
                    else
                    {
                        UIManager.Add(new BuffGump(_world, 100, 100));
                    }

                    break;

                case MacroType.InvokeVirtue:
                    byte id = (byte) (macro.SubCode - MacroSubType.Honor + 1);
                    NetClient.Socket.Send_InvokeVirtueRequest(id);

                    break;

                case MacroType.PrimaryAbility:
                    GameActions.UsePrimaryAbility(_world);

                    break;

                case MacroType.SecondaryAbility:
                    GameActions.UseSecondaryAbility(_world);

                    break;

                case MacroType.ToggleGargoyleFly:

                    if (_world.Player.Race == RaceType.GARGOYLE)
                    {
                        NetClient.Socket.Send_ToggleGargoyleFlying();
                    }

                    break;

                case MacroType.EquipLastWeapon:
                    NetClient.Socket.Send_EquipLastWeapon(_world);

                    break;

                case MacroType.KillGumpOpen:
                    // TODO:
                    break;

                case MacroType.Zoom:

                    switch (macro.SubCode)
                    {
                        case MacroSubType.MSC_NONE:
                        case MacroSubType.DefaultZoom:
                            Client.Game.Scene.Camera.Zoom = ProfileManager.CurrentProfile.DefaultScale;

                            break;

                        case MacroSubType.ZoomIn:
                            Client.Game.Scene.Camera.ZoomIn();

                            break;

                        case MacroSubType.ZoomOut:
                            Client.Game.Scene.Camera.ZoomOut();

                            break;
                    }

                    break;

                case MacroType.ToggleChatVisibility:
                    UIManager.SystemChat?.ToggleChatVisibility();

                    break;

                case MacroType.Aura:
                    // hold to draw
                    break;

                case MacroType.AuraOnOff:
                    _world.AuraManager.ToggleVisibility();

                    break;

                case MacroType.Grab:
                    GameActions.Print(_world, ResGeneral.TargetAnItemToGrabIt);
                    _world.TargetManager.SetTargeting(CursorTarget.Grab, 0, TargetType.Neutral);

                    break;

                case MacroType.SetGrabBag:
                    GameActions.Print(_world, ResGumps.TargetContainerToGrabItemsInto);
                    _world.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);

                    break;

                case MacroType.NamesOnOff:
                    _world.NameOverHeadManager.ToggleOverheads();

                    break;

                case MacroType.UsePotion:
                    scantype = (ScanTypeObject)(macro.SubCode - MacroSubType.ConfusionBlastPotion);

                    ushort start = (ushort) (0x0F06 + scantype);

                    Item potion = _world.Player.FindItemByGraphic(start);

                    if (potion != null)
                    {
                        GameActions.DoubleClick(_world, potion);
                    }

                    break;

                case MacroType.UseObject:
                    Item obj;

                    switch (macro.SubCode)
                    {
                        case MacroSubType.BestHealPotion:
                            Span<int> healpotion_clilocs = stackalloc int[3] { 1041330, 1041329, 1041328 };

                            obj = _world.Player.FindPreferredItemByCliloc(healpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world,obj);
                            }

                            break;

                        case MacroSubType.BestCurePotion:
                            Span<int> curepotion_clilocs = stackalloc int[3] { 1041317, 1041316, 1041315 };

                            obj = _world.Player.FindPreferredItemByCliloc(curepotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world,obj);
                            }

                            break;

                        case MacroSubType.BestRefreshPotion:
                            Span<int> refreshpotion_clilocs = stackalloc int[2] { 1041327, 1041326 };

                            obj = _world.Player.FindPreferredItemByCliloc(refreshpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestStrengthPotion:
                            Span<int> strpotion_clilocs = stackalloc int[2] { 1041321, 1041320 };

                            obj = _world.Player.FindPreferredItemByCliloc(strpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world,obj);
                            }

                            break;

                        case MacroSubType.BestAgiPotion:
                            Span<int> agipotion_clilocs = stackalloc int[2] { 1041319, 1041318 };

                            obj = _world.Player.FindPreferredItemByCliloc(agipotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world,obj);
                            }

                            break;

                        case MacroSubType.BestExplosionPotion:
                            Span<int> explopotion_clilocs = stackalloc int[3] { 1041333, 1041332, 1041331 };

                            obj = _world.Player.FindPreferredItemByCliloc(explopotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestConflagPotion:
                            Span<int> conflagpotion_clilocs = stackalloc int[2] { 1072098, 1072095 };

                            obj = _world.Player.FindPreferredItemByCliloc(conflagpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.HealStone:
                            obj = _world.Player.FindItemByCliloc(1095376);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.SpellStone:
                            obj = _world.Player.FindItemByCliloc(1095377);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.EnchantedApple:
                            obj = _world.Player.FindItemByCliloc(1032248);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.PetalsOfTrinsic:
                            obj = _world.Player.FindItemByCliloc(1062926);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.OrangePetals:
                            obj = _world.Player.FindItemByCliloc(1053122);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.SmokeBomb:
                            obj = _world.Player.FindItemByGraphic(0x2808);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.TrappedBox:
                            Span<int> trapbox_clilocs = stackalloc int[7] { 1015093, 1022473, 1044309, 1022474, 1023709, 1027808, 1027809 };

                            obj = _world.Player.FindPreferredItemByCliloc(trapbox_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;
                    }

                    break;

                case MacroType.CloseAllHealthBars:

                    //Includes HealthBarGump/HealthBarGumpCustom
                    IEnumerable<BaseHealthBarGump> healthBarGumps = UIManager.Gumps.OfType<BaseHealthBarGump>();

                    foreach (BaseHealthBarGump healthbar in healthBarGumps)
                    {
                        if (UIManager.AnchorManager[healthbar] == null && healthbar.LocalSerial != _world.Player)
                        {
                            healthbar.Dispose();
                        }
                    }

                    break;

                case MacroType.CloseInactiveHealthBars:
                    IEnumerable<BaseHealthBarGump> inactiveHealthBarGumps = UIManager.Gumps.OfType<BaseHealthBarGump>().Where(hb => hb.IsInactive);

                    foreach (var healthbar in inactiveHealthBarGumps)
                    {
                        if (healthbar.LocalSerial == _world.Player) continue;

                        if (UIManager.AnchorManager[healthbar] != null)
                        {
                            UIManager.AnchorManager[healthbar].DetachControl(healthbar);
                        }

                        healthbar.Dispose();
                    }
                    break;

                case MacroType.CloseCorpses:
                    var gridLootType = ProfileManager.CurrentProfile?.GridLootType; // 0 = none, 1 = only grid, 2 = both
                    if (gridLootType == 0 || gridLootType == 2)
                    {
                        IEnumerable<ContainerGump> containerGumps = UIManager.Gumps.OfType<ContainerGump>().Where(cg => cg.Graphic == ContainerGump.CORPSES_GUMP);

                        foreach (var containerGump in containerGumps)
                        {
                            containerGump.Dispose();
                        }
                    }
                    if (gridLootType == 1 || gridLootType == 2)
                    {
                        IEnumerable<GridLootGump> gridLootGumps = UIManager.Gumps.OfType<GridLootGump>();

                        foreach (var gridLootGump in gridLootGumps)
                        {
                            gridLootGump.Dispose();
                        }
                    }
                    break;

                case MacroType.ToggleDrawRoofs:
                    ProfileManager.CurrentProfile.DrawRoofs = !ProfileManager.CurrentProfile.DrawRoofs;

                    break;

                case MacroType.ToggleTreeStumps:
                    StaticFilters.CleanTreeTextures();
                    ProfileManager.CurrentProfile.TreeToStumps = !ProfileManager.CurrentProfile.TreeToStumps;

                    break;

                case MacroType.ToggleVegetation:
                    ProfileManager.CurrentProfile.HideVegetation = !ProfileManager.CurrentProfile.HideVegetation;

                    break;

                case MacroType.ToggleCaveTiles:
                    StaticFilters.CleanCaveTextures();
                    ProfileManager.CurrentProfile.EnableCaveBorder = !ProfileManager.CurrentProfile.EnableCaveBorder;

                    break;

                case MacroType.LookAtMouse:
                    // handle in gamesceneinput
                    break;
            }


            return result;
        }

        private void SetLastTarget(uint serial)
        {
            if (SerialHelper.IsValid(serial))
            {
                Entity ent = _world.Get(serial);

                if (SerialHelper.IsMobile(serial))
                {
                    if (ent != null)
                    {
                        GameActions.MessageOverhead(_world, string.Format(ResGeneral.Target0, ent.Name), Notoriety.GetHue(((Mobile) ent).NotorietyFlag), _world.Player);

                        _world.TargetManager.NewTargetSystemSerial = serial;
                        _world.TargetManager.SelectedTarget = serial;
                        _world.TargetManager.LastTargetInfo.SetEntity(serial);

                        return;
                    }
                }
                else
                {
                    if (ent != null)
                    {
                        GameActions.MessageOverhead(_world, string.Format(ResGeneral.Target0, ent.Name), 992, _world.Player);
                        _world.TargetManager.SelectedTarget = serial;
                        _world.TargetManager.LastTargetInfo.SetEntity(serial);

                        return;
                    }
                }
            }

            GameActions.Print(_world, ResGeneral.EntityNotFound);
        }
    }


    internal class Macro : LinkedObject, IEquatable<Macro>
    {
        public Macro(string name, SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift) : this(name)
        {
            Key = key;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public Macro(string name, MouseButtonType button, bool alt, bool ctrl, bool shift) : this(name)
        {
            MouseButton = button;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public Macro(string name, bool wheelUp, bool alt, bool ctrl, bool shift) : this(name)
        {
            WheelScroll = true;
            WheelUp = wheelUp;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public Macro(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public SDL.SDL_Keycode Key { get; set; }
        public MouseButtonType MouseButton { get; set; }
        public bool WheelScroll { get; set; }
        public bool WheelUp { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }

        public bool Equals(Macro other)
        {
            if (other == null)
            {
                return false;
            }

            return Key == other.Key && Alt == other.Alt && Ctrl == other.Ctrl && Shift == other.Shift && Name == other.Name;
        }

        //public Macro Left { get; set; }
        //public Macro Right { get; set; }

        //private void AppendMacro(MacroObject item)
        //{
        //    if (FirstNode == null)
        //    {
        //        FirstNode = item;
        //    }
        //    else
        //    {
        //        MacroObject o = FirstNode;

        //        while (o.Right != null)
        //        {
        //            o = o.Right;
        //        }

        //        o.Right = item;
        //        item.Left = o;
        //    }
        //}


        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("macro");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("key", ((int) Key).ToString());
            writer.WriteAttributeString("mousebutton", ((int) MouseButton).ToString());
            writer.WriteAttributeString("wheelscroll", WheelScroll.ToString());
            writer.WriteAttributeString("wheelup", WheelUp.ToString());
            writer.WriteAttributeString("alt", Alt.ToString());
            writer.WriteAttributeString("ctrl", Ctrl.ToString());
            writer.WriteAttributeString("shift", Shift.ToString());

            writer.WriteStartElement("actions");

            for (MacroObject action = (MacroObject) Items; action != null; action = (MacroObject) action.Next)
            {
                writer.WriteStartElement("action");
                writer.WriteAttributeString("code", ((int) action.Code).ToString());
                writer.WriteAttributeString("subcode", ((int) action.SubCode).ToString());
                writer.WriteAttributeString("submenutype", action.SubMenuType.ToString());

                if (action.HasString())
                {
                    writer.WriteAttributeString("text", ((MacroObjectString) action).Text);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public void Load(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }

            Key = (SDL.SDL_Keycode) int.Parse(xml.GetAttribute("key"));
            Alt = bool.Parse(xml.GetAttribute("alt"));
            Ctrl = bool.Parse(xml.GetAttribute("ctrl"));
            Shift = bool.Parse(xml.GetAttribute("shift"));

            if (xml.HasAttribute("mousebutton"))
            {
                MouseButton = (MouseButtonType) int.Parse(xml.GetAttribute("mousebutton"));
            }

            if (xml.HasAttribute("wheelscroll"))
            {
                WheelScroll = bool.Parse(xml.GetAttribute("wheelscroll"));
            }

            if (xml.HasAttribute("wheelup"))
            {
                WheelUp = bool.Parse(xml.GetAttribute("wheelup"));
            }

            XmlElement actions = xml["actions"];

            if (actions != null)
            {
                foreach (XmlElement xmlAction in actions.GetElementsByTagName("action"))
                {
                    MacroType code = (MacroType) int.Parse(xmlAction.GetAttribute("code"));
                    MacroSubType sub = (MacroSubType) int.Parse(xmlAction.GetAttribute("subcode"));

                    // ########### PATCH ###########
                    // FIXME: path to remove the MovePlayer macro. This macro is not needed. We have Walk.
                    if ((int) code == 61 /*MacroType.MovePlayer*/)
                    {
                        code = MacroType.Walk;

                        switch ((int) sub)
                        {
                            case 211: // top
                                sub = MacroSubType.NW;

                                break;

                            case 214: // left
                                sub = MacroSubType.SW;

                                break;

                            case 213: // down
                                sub = MacroSubType.SE;

                                break;

                            case 212: // right
                                sub = MacroSubType.NE;

                                break;
                        }
                    }
                    // ########### END PATCH ###########

                    sbyte subMenuType = sbyte.Parse(xmlAction.GetAttribute("submenutype"));

                    MacroObject m;

                    if (xmlAction.HasAttribute("text"))
                    {
                        m = new MacroObjectString(code, sub, xmlAction.GetAttribute("text"));
                    }
                    else
                    {
                        m = new MacroObject(code, sub);
                    }

                    m.SubMenuType = subMenuType;

                    PushToBack(m);
                }
            }
        }


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
                case MacroType.RazorMacro:
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
            Macro macro = new Macro
            (
                name,
                (SDL.SDL_Keycode) 0,
                false,
                false,
                false
            );

            MacroObject item = new MacroObject(MacroType.None, MacroSubType.MSC_NONE);

            macro.PushToBack(item);

            return macro;
        }

        public static Macro CreateFastMacro(string name, MacroType type, MacroSubType sub)
        {
            Macro macro = new Macro
              (
                  name,
                  (SDL.SDL_Keycode) 0,
                  false,
                  false,
                  false
              );

            MacroObject item = new MacroObject(type, sub);

            macro.PushToBack(item);

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

                case MacroType.UsePotion:
                    offset = (int) MacroSubType.ConfusionBlastPotion;
                    count = MacroSubType.DefaultZoom - MacroSubType.ConfusionBlastPotion;

                    break;

                case MacroType.Zoom:
                    offset = (int) MacroSubType.DefaultZoom;
                    count = 1 + MacroSubType.ZoomOut - MacroSubType.DefaultZoom;

                    break;

                case MacroType.UseObject:
                    offset = (int) MacroSubType.BestHealPotion;
                    count = 1 + MacroSubType.SpellStone - MacroSubType.BestHealPotion;

                    break;

                case MacroType.LookAtMouse:
                    offset = (int) MacroSubType.LookForwards;
                    count = 1 + MacroSubType.LookBackwards - MacroSubType.LookForwards;

                    break;
            }
        }
    }


    internal class MacroObject : LinkedObject
    {
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
                case MacroType.UsePotion:
                case MacroType.Zoom:
                case MacroType.UseObject:
                case MacroType.LookAtMouse:

                    if (sub == MacroSubType.MSC_NONE)
                    {
                        int count = 0;
                        int offset = 0;
                        Macro.GetBoundByCode(code, ref count, ref offset);
                        SubCode = (MacroSubType) offset;
                    }

                    SubMenuType = 1;

                    break;

                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:
                case MacroType.Delay:
                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:
                case MacroType.RazorMacro:
                    SubMenuType = 2;

                    break;

                default:
                    SubMenuType = 0;

                    break;
            }
        }

        public MacroType Code { get; set; }
        public MacroSubType SubCode { get; set; }
        public sbyte SubMenuType { get; set; }

        public virtual bool HasString()
        {
            return false;
        }
    }

    internal class MacroObjectString : MacroObject
    {
        public MacroObjectString(MacroType code, MacroSubType sub, string str = "") : base(code, sub)
        {
            Text = str;
        }

        public string Text { get; set; }

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
        Zoom,
        ToggleChatVisibility,
        INVALID,
        Aura = 62,
        AuraOnOff,
        Grab,
        SetGrabBag,
        NamesOnOff,
        UseItemInHand,
        UsePotion,
        CloseAllHealthBars,
        RazorMacro,
        ToggleDrawRoofs,
        ToggleTreeStumps,
        ToggleVegetation,
        ToggleCaveTiles,
        CloseInactiveHealthBars,
        CloseCorpses,
        UseObject,
        LookAtMouse
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

        INVALID_0,
        INVALID_1,
        INVALID_2,
        INVALID_3,


        ConfusionBlastPotion = 215,
        CurePotion,
        AgilityPotion,
        StrengthPotion,
        PoisonPotion,
        RefreshPotion,
        HealPotion,
        ExplosionPotion,

        DefaultZoom,
        ZoomIn,
        ZoomOut,

        BestHealPotion,
        BestCurePotion,
        BestRefreshPotion,
        BestStrengthPotion,
        BestAgiPotion,
        BestExplosionPotion,
        BestConflagPotion,
        EnchantedApple,
        PetalsOfTrinsic,
        OrangePetals,
        TrappedBox,
        SmokeBomb,
        HealStone,
        SpellStone,

        LookForwards,
        LookBackwards
    }
}
