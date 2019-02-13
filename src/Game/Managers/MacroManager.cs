using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.Network;

using Newtonsoft.Json;
using System.Diagnostics;

using SDL2;

namespace ClassicUO.Game.Managers
{
    internal class MacroManager
    {
        private readonly uint[] _itemsInHand = new uint[2];
        private MacroObject _lastMacro;
        private Macro _firstNode;
        private long _nextTimer;

        private readonly byte[] _skillTable = 
        {
            1,  2,  35, 4,  6,  12,
            14, 15, 16, 19, 21, 0xFF /*imbuing*/,
            23, 3,  46, 9,  30, 22,
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


        public MacroManager(Macro[] macros)
        {
            if (macros != null)
            {
                foreach (Macro macro in macros)
                {
                    AppendMacro(macro);
                }
            }
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
                        _lastMacro = _lastMacro.Right;
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

                        Chat.Say(mos.Text, hue, type);
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
                    GameActions.ToggleWarMode();

                    break;
                case MacroType.Paste:
                    if (SDL.SDL_HasClipboardText() != SDL.SDL_bool.SDL_FALSE)
                    {
                        string s = SDL.SDL_GetClipboardText();
                        if (!string.IsNullOrEmpty(s))
                        {
                            WorldViewportGump viewport = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                            if (viewport != null)
                            {
                                SystemChatControl chat = viewport.FindControls<SystemChatControl>().SingleOrDefault();
                                if (chat != null)
                                    chat.textBox.Text += s;
                            }
                        }
                    }

                    break;
                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                    // TODO:
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
                case MacroType.LastTarget:

                    if (WaitForTargetTimer == 0)
                        WaitForTargetTimer = Engine.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (TargetManager.IsTargeting)
                    {
                        //if (TargetManager.TargetingState != TargetType.Object)
                        //{
                        //    TargetManager.TargetGameObject(TargetManager.LastGameObject);
                        //}
                        //else 
                            TargetManager.TargetGameObject(TargetManager.LastGameObject);

                        WaitForTargetTimer = 0;
                    }
                    else if (WaitForTargetTimer < Engine.Ticks)
                        WaitForTargetTimer = 0;
                    else result = 1;

                    break;
                case MacroType.TargetSelf:
                    if (WaitForTargetTimer == 0)
                        WaitForTargetTimer = Engine.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

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

                        Item item = World.Player.Equipment[(int)Layer.OneHanded + handIndex];

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

                    if (TargetManager.LastGameObject is Mobile mob)
                    {
                        if (mob.HitsMax == 0)
                            NetClient.Socket.Send(new PStatusRequest(mob));

                        TargetManager.LastGameObject = mob;
                        World.LastAttack = mob.Serial;
                    }

                    break;
                case MacroType.AttackLast:
                    GameActions.Attack(World.LastAttack);
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
                          .Where( s=> !(s is TopBarGump) && !(s is BuffGump) && !(s is WorldViewportGump))
                          .ToList()
                          .ForEach(s => s.Dispose());

                    break;

                case MacroType.AlwaysRun:
                    Engine.Profile.Current.AlwaysRun = !Engine.Profile.Current.AlwaysRun;

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
                    if (FileManager.ClientVersion < ClientVersions.CV_5020)
                    {
                        if (WaitingBandageTarget)
                        {
                            if (WaitForTargetTimer == 0)
                                WaitForTargetTimer = Engine.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                            if (TargetManager.IsTargeting)
                            {
                                TargetManager.TargetGameObject(macro.Code == MacroType.BandageSelf ? World.Player : TargetManager.LastGameObject);
                                WaitingBandageTarget = false;
                                WaitForTargetTimer = 0;
                            }
                            else if (WaitForTargetTimer < Engine.Ticks)
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
                            {
                                NetClient.Socket.Send(new PTargetSelectedObject(bandage.Serial, World.Player.Serial));
                            }
                            else // TODO: NewTargetSystem
                            {
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

                        World.ViewRange = res;
                    }
                    break;
                case MacroType.IncreaseUpdateRange:
                    World.ViewRange++;
                    if (World.ViewRange > Constants.MAX_VIEW_RANGE)
                        World.ViewRange = Constants.MAX_VIEW_RANGE;
                    break;
                case MacroType.DecreaseUpdateRange:
                    World.ViewRange--;
                    if (World.ViewRange < Constants.MIN_VIEW_RANGE)
                        World.ViewRange = Constants.MIN_VIEW_RANGE;

                    break;

                case MacroType.MaxUpdateRange:
                    World.ViewRange = Constants.MAX_VIEW_RANGE;

                    break;
                case MacroType.MinUpdateRange:
                    World.ViewRange = Constants.MIN_VIEW_RANGE;

                    break;

                case MacroType.DefaultUpdateRange:
                    World.ViewRange = Constants.MAX_VIEW_RANGE;

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

                case MacroType.ToggleBuiconWindow:
                    // TODO:
                    break;
                case MacroType.InvokeVirtue:
                    byte id = (byte) ( macro.SubCode - MacroSubType.Honor + 31);
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

                    break;
            }


            return result;
        }
    }

    [JsonObject]
    class Macro : IEquatable<Macro>, INode<Macro>
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

        [JsonIgnore] public Macro Left { get; set; }
        [JsonIgnore] public Macro Right { get; set; }

        public SDL.SDL_Keycode Key { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }

        public MacroObject FirstNode { get; set; }

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
            {
                macro.FirstNode = item;
            }
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
                    count = (int) MacroSubType.Configuration - (int)MacroSubType.NW;

                    break;
                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                    offset = (int)MacroSubType.Configuration;
                    count = (int)MacroSubType.Anatomy - (int)MacroSubType.Configuration;

                    break;
                case MacroType.UseSkill:
                    offset = (int)MacroSubType.Anatomy;
                    count = (int)MacroSubType.LeftHand - (int)MacroSubType.Anatomy;

                    break;
                case MacroType.ArmDisarm:
                    offset = (int)MacroSubType.LeftHand;
                    count = (int)MacroSubType.Honor - (int)MacroSubType.LeftHand;

                    break;
                case MacroType.InvokeVirtue:
                    offset = (int)MacroSubType.Honor;
                    count = (int)MacroSubType.Clumsy - (int)MacroSubType.Honor;

                    break;
                case MacroType.CastSpell:
                    offset = (int)MacroSubType.Clumsy;
                    count = (int)MacroSubType.Hostile - (int)MacroSubType.Clumsy;

                    break;
                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                    offset = (int)MacroSubType.Hostile;
                    count = (int)MacroSubType.MscTotalCount - (int)MacroSubType.Hostile;

                    break;
            }
        }

        public bool Equals(Macro other)
        {
            if (other == null)
                return false;

            return Key == other.Key && Alt == other.Alt && Ctrl == other.Ctrl && Shift == other.Shift;
        }
    }

    [JsonObject]
    class MacroObject : INode<MacroObject>
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

        public virtual bool HasString() => false;

        [JsonIgnore] public MacroObject Left { get; set; }
        [JsonProperty] public MacroObject Right { get; set; }
    }

    [JsonObject]
    class MacroObjectString : MacroObject
    {
        [JsonConstructor]
        public MacroObjectString(MacroType code, MacroSubType sub, string str = "") : base(code, sub)
        {
            Text = str;
        }

        [JsonProperty]
        public string Text { get; set; }

        public override bool HasString()
        {
            return true;
        }
    }

    enum MacroType
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
        ToggleBuiconWindow,
        BandageSelf,
        BandageTarget,
        ToggleGargoyleFly
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
        Owerview,
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
        Enticement,
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
        MscTotalCount
    }
}
