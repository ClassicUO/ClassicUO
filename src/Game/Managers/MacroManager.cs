using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    class MacroManager
    {
        private readonly List<Macro> _macros = new List<Macro>();

        private MacroObject _lastMacro;

        private readonly byte[] _skillTable = { 1,  2,  35, 4,  6,  12,
            14, 15, 16, 19, 21, 0xFF /*imbuing*/,
            23, 3,  46, 9,  30, 22,
            48, 32, 33, 47, 36, 38 };

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

        private readonly uint[] _itemsInHand = new uint[2];

        public long WaitForTargetTimer { get; set; }

        public Macro FindMacro(ushort key, bool alt, bool ctrl, bool shift)
        {
            return _macros.FirstOrDefault(s => s.Key == key && s.Alt == alt && s.Shift == shift && s.Ctrl == ctrl);
        }

        public void Update()
        {

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

                        switch (macro.Code)
                        {
                            case MacroType.Emote:
                                type = MessageType.Emote;

                                break;
                            case MacroType.Whisper:
                                type = MessageType.Whisper;

                                break;
                            case MacroType.Yell:
                                type = MessageType.Yell;

                                break;
                        }

                        Chat.Say(mos.Text, type: type);
                    }

                    break;

                case MacroType.Walk:
                    byte dt = (byte) Direction.Up;

                    if (macro.SubCode != MacroSubType.MSC_G1_NW)
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
                    // TODO:
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
                    int skill = macro.SubCode - MacroSubType.MSC_G3_ANATOMY;

                    if (skill >= 0 && skill < 24)
                    {
                        skill = _skillTable[skill];

                        if (skill != 0xFF)
                            GameActions.UseSkill(skill);
                    }
                    break;
                case MacroType.LastSkill:
                    // TODO:
                    break;
                case MacroType.CastSpell:
                    int spell = macro.SubCode - MacroSubType.MSC_G6_CLUMSY + 1;

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
                    // TODO:
                    break;
                case MacroType.Bow:
                case MacroType.Salute:
                    int index = macro.Code - MacroType.Bow;

                    const string BOW = "bow";
                    const string SALUTE = "salute";

                    GameActions.EmoteAction(index == 0 ? BOW : SALUTE);
                    break;
                case MacroType.QuitGame:
                    // TODO:
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
                    int handIndex = 1 - (macro.SubCode - MacroSubType.MSC_G4_LEFT_HAND);
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

                    break;
                case MacroType.UseSelectedTarget:

                    break;

                case MacroType.CurrentTarget:

                    break;

                case MacroType.TargetSystemOnOff:

                    break;

                case MacroType.BandageSelf:
                case MacroType.BandageTarget:

                    if (FileManager.ClientVersion < ClientVersions.CV_5020)
                    {
                        
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

                    break;

                case MacroType.ToggleBuiconWindow:

                    break;
                case MacroType.InvokeVirtue:
                    byte id = (byte) ( macro.SubCode - MacroSubType.MSC_G5_HONOR + 31);
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

                    break;
                case MacroType.KillGumpOpen:

                    break;
            }


            return result;
        }
    }

    class Macro : IEquatable<Macro>
    {
        public Macro(ushort key, bool alt, bool ctrl, bool shift)
        {
            Key = key;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public ushort Key { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }

        public List<MacroObject> Objects { get; } = new List<MacroObject>();

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

        public static Macro CreateEmptyMacro()
        {
            Macro macro = new Macro(0, false, false, false);
            macro.Objects.Add(new MacroObject(MacroType.None, MacroSubType.MSC_NONE));

            return macro;
        }

        public static void GetBoundByCode(MacroType code, ref int count, ref int offset)
        {
            switch (code)
            {
                case MacroType.Walk:
                    offset = (int) MacroSubType.MSC_G1_NW;
                    count = (int) MacroSubType.MSC_G2_CONFIGURATION - (int)MacroSubType.MSC_G1_NW;

                    break;
                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                    offset = (int)MacroSubType.MSC_G2_CONFIGURATION;
                    count = (int)MacroSubType.MSC_G3_ANATOMY - (int)MacroSubType.MSC_G2_CONFIGURATION;

                    break;
                case MacroType.UseSkill:
                    offset = (int)MacroSubType.MSC_G4_LEFT_HAND;
                    count = (int)MacroSubType.MSC_G5_HONOR - (int)MacroSubType.MSC_G4_LEFT_HAND;

                    break;
                case MacroType.InvokeVirtue:
                    offset = (int)MacroSubType.MSC_G5_HONOR;
                    count = (int)MacroSubType.MSC_G6_CLUMSY - (int)MacroSubType.MSC_G5_HONOR;

                    break;
                case MacroType.CastSpell:
                    offset = (int)MacroSubType.MSC_G6_CLUMSY;
                    count = (int)MacroSubType.MSC_G7_HOSTILE - (int)MacroSubType.MSC_G6_CLUMSY;

                    break;
                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                    offset = (int)MacroSubType.MSC_G7_HOSTILE;
                    count = (int)MacroSubType.MSC_TOTAL_COUNT - (int)MacroSubType.MSC_G7_HOSTILE;

                    break;
                default:

                    break;
            }
        }

        public bool Equals(Macro other)
        {
            if (other == null)
                return false;

            return Key == other.Key && Alt == other.Alt && Ctrl == other.Ctrl && Shift == other.Shift && Equals(Objects, other.Objects);
        }
    }

    class MacroObject
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

        public MacroType Code { get; set; }
        public MacroSubType SubCode { get; set; }
        public sbyte HasSubMenu { get; set; }

        public virtual bool HasString() => false;
    }

    class MacroObjectString : MacroObject
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

    enum MacroSubType
    {
        MSC_NONE = 0,
        MSC_G1_NW, //Walk group
        MSC_G1_N,
        MSC_G1_NE,
        MSC_G1_E,
        MSC_G1_SE,
        MSC_G1_S,
        MSC_G1_SW,
        MSC_G1_W,
        MSC_G2_CONFIGURATION, //Open/Close/Minimize/Maximize group
        MSC_G2_PAPERDOLL,
        MSC_G2_STATUS,
        MSC_G2_JOURNAL,
        MSC_G2_SKILLS,
        MSC_G2_MAGE_SPELLBOOK,
        MSC_G2_CHAT,
        MSC_G2_BACKPACK,
        MSC_G2_OWERVIEW,
        MSC_G2_WORLD_MAP,
        MSC_G2_MAIL,
        MSC_G2_PARTY_MANIFEST,
        MSC_G2_PARTY_CHAT,
        MSC_G2_NECRO_SPELLBOOK,
        MSC_G2_PALADIN_SPELLBOOK,
        MSC_G2_COMBAT_BOOK,
        MSC_G2_BUSHIDO_SPELLBOOK,
        MSC_G2_NINJITSU_SPELLBOOK,
        MSC_G2_GUILD,
        MSC_G2_SPELL_WEAVING_SPELLBOOK,
        MSC_G2_QUEST_LOG,
        MSC_G2_MYSTICISM_SPELLBOOK,
        MSC_G2_RACIAL_ABILITIES_BOOK,
        MSC_G2_BARD_SPELLBOOK,
        MSC_G3_ANATOMY, //Skills group
        MSC_G3_ANIMAL_LORE,
        MSC_G3_ANIMAL_TAMING,
        MSC_G3_ARMS_LORE,
        MSC_G3_BEGGING,
        MSC_G3_CARTOGRAPHY,
        MSC_G3_DETECTING_HIDDEN,
        MSC_G3_ENTICEMENT,
        MSC_G3_EVALUATING_INTELLIGENCE,
        MSC_G3_FORENSIC_EVALUATION,
        MSC_G3_HIDING,
        MSC_G3_IMBUING,
        MSC_G3_INSCRIPTION,
        MSC_G3_ITEM_IDENTIFICATION,
        MSC_G3_MEDITATION,
        MSC_G3_PEACEMAKING,
        MSC_G3_POISONING,
        MSC_G3_PROVOCATION,
        MSC_G3_REMOVE_TRAP,
        MSC_G3_SPIRIT_SPEAK,
        MSC_G3_STEALING,
        MSC_G3_STEALTH,
        MSC_G3_TASTE_IDENTIFICATION,
        MSC_G3_TRACKING,
        MSC_G4_LEFT_HAND, ///Arm/Disarm group
        MSC_G4_RIGHT_HAND,
        MSC_G5_HONOR, //Invoke Virture group
        MSC_G5_SACRIFICE,
        MSC_G5_VALOR,
        MSC_G6_CLUMSY, //Cast Spell group
        MSC_G6_CREATE_FOOD,
        MSC_G6_FEEBLEMIND,
        MSC_G6_HEAL,
        MSC_G6_MAGIC_ARROW,
        MSC_G6_NIGHT_SIGHT,
        MSC_G6_REACTIVE_ARMOR,
        MSC_G6_WEAKEN,
        MSC_G6_AGILITY,
        MSC_G6_CUNNING,
        MSC_G6_CURE,
        MSC_G6_HARM,
        MSC_G6_MAGIC_TRAP,
        MSC_G6_MAGIC_UNTRAP,
        MSC_G6_PROTECTION,
        MSC_G6_STRENGTH,
        MSC_G6_BLESS,
        MSC_G6_FIREBALL,
        MSC_G6_MAGIC_LOCK,
        MSC_G6_POISON,
        MSC_G6_TELEKINESIS,
        MSC_G6_TELEPORT,
        MSC_G6_UNLOCK,
        MSC_G6_WALL_OF_STONE,
        MSC_G6_ARCH_CURE,
        MSC_G6_ARCH_PROTECTION,
        MSC_G6_CURSE,
        MSC_G6_FIRE_FIELD,
        MSC_G6_GREATER_HEAL,
        MSC_G6_LIGHTNING,
        MSC_G6_MANA_DRAIN,
        MSC_G6_RECALL,
        MSC_G6_BLADE_SPIRITS,
        MSC_G6_DISPELL_FIELD,
        MSC_G6_INCOGNITO,
        MSC_G6_MAGIC_REFLECTION,
        MSC_G6_MIND_BLAST,
        MSC_G6_PARALYZE,
        MSC_G6_POISON_FIELD,
        MSC_G6_SUMMON_CREATURE,
        MSC_G6_DISPEL,
        MSC_G6_ENERGY_BOLT,
        MSC_G6_EXPLOSION,
        MSC_G6_INVISIBILITY,
        MSC_G6_MARK,
        MSC_G6_MASS_CURSE,
        MSC_G6_PARALYZE_FIELD,
        MSC_G6_REVEAL,
        MSC_G6_CHAIN_LIGHTNING,
        MSC_G6_ENERGY_FIELD,
        MSC_G6_FLAME_STRIKE,
        MSC_G6_GATE_TRAVEL,
        MSC_G6_MANA_VAMPIRE,
        MSC_G6_MASS_DISPEL,
        MSC_G6_METEOR_SWARM,
        MSC_G6_POLYMORPH,
        MSC_G6_EARTHQUAKE,
        MSC_G6_ENERGY_VORTEX,
        MSC_G6_RESURRECTION,
        MSC_G6_AIR_ELEMENTAL,
        MSC_G6_SUMMON_DAEMON,
        MSC_G6_EARTH_ELEMENTAL,
        MSC_G6_FIRE_ELEMENTAL,
        MSC_G6_WATER_ELEMENTAL,
        MSC_G6_ANIMATE_DEAD,
        MSC_G6_BLOOD_OATH,
        MSC_G6_CORPSE_SKIN,
        MSC_G6_CURSE_WEAPON,
        MSC_G6_EVIL_OMEN,
        MSC_G6_HORRIFIC_BEAST,
        MSC_G6_LICH_FORM,
        MSC_G6_MIND_ROT,
        MSC_G6_PAIN_SPIKE,
        MSC_G6_POISON_STRIKE,
        MSC_G6_STRANGLE,
        MSC_G6_SUMMON_FAMILAR,
        MSC_G6_VAMPIRIC_EMBRACE,
        MSC_G6_VENGEFUL_SPIRIT,
        MSC_G6_WITHER,
        MSC_G6_WRAITH_FORM,
        MSC_G6_EXORCISM,
        MSC_G6_CLEANCE_BY_FIRE,
        MSC_G6_CLOSE_WOUNDS,
        MSC_G6_CONSECRATE_WEAPON,
        MSC_G6_DISPEL_EVIL,
        MSC_G6_DIVINE_FURY,
        MSC_G6_ENEMY_OF_ONE,
        MSC_G6_HOLY_LIGHT,
        MSC_G6_NOBLE_SACRIFICE,
        MSC_G6_REMOVE_CURSE,
        MSC_G6_SACRED_JOURNEY,
        MSC_G6_HONORABLE_EXECUTION,
        MSC_G6_CONFIDENCE,
        MSC_G6_EVASION,
        MSC_G6_COUNTER_ATTACK,
        MSC_G6_LIGHTING_STRIKE,
        MSC_G6_MOMENTUM_STRIKE,
        MSC_G6_FOCUS_ATTACK,
        MSC_G6_DEATH_STRIKE,
        MSC_G6_ANIMAL_FORM,
        MSC_G6_KI_ATTACK,
        MSC_G6_SURPRICE_ATTACK,
        MSC_G6_BACKSTAB,
        MSC_G6_SHADOWJUMP,
        MSC_G6_MIRROR_IMAGE,
        MSC_G6_ARCANE_CIRCLE,
        MSC_G6_GIFT_OF_RENEWAL,
        MSC_G6_IMMOLATING_WEAPON,
        MSC_G6_ATTUNEMENT,
        MSC_G6_THUNDERSTORM,
        MSC_G6_NATURES_FURY,
        MSC_G6_SUMMON_FEY,
        MSC_G6_SUMMON_FIEND,
        MSC_G6_REAPER_FORM,
        MSC_G6_WILDFIRE,
        MSC_G6_ESSENCE_OF_WIND,
        MSC_G6_DRYAD_ALLURE,
        MSC_G6_ETHEREAL_VOYAGE,
        MSC_G6_WORD_OF_DEATH,
        MSC_G6_GIFT_OF_LIFE,
        MSC_G6_ARCANE_EMPOWERMEN,
        MSC_G6_NETHER_BOLT,
        MSC_G6_HEALING_STONE,
        MSC_G6_PURGE_MAGIC,
        MSC_G6_ENCHANT,
        MSC_G6_SLEEP,
        MSC_G6_EAGLE_STRIKE,
        MSC_G6_ANIMATED_WEAPON,
        MSC_G6_STONE_FORM,
        MSC_G6_SPELL_TRIGGER,
        MSC_G6_MASS_SLEEP,
        MSC_G6_CLEANSING_WINDS,
        MSC_G6_BOMBARD,
        MSC_G6_SPELL_PLAGUE,
        MSC_G6_HAIL_STORM,
        MSC_G6_NETHER_CYCLONE,
        MSC_G6_RISING_COLOSSUS,
        MSC_G6_INSPIRE,
        MSC_G6_INVIGORATE,
        MSC_G6_RESILIENCE,
        MSC_G6_PERSEVERANCE,
        MSC_G6_TRIBULATION,
        MSC_G6_DESPAIR,
        MSC_G7_HOSTILE, //Select Next/Preveous/Nearest group
        MSC_G7_PARTY,
        MSC_G7_FOLLOWER,
        MSC_G7_OBJECT,
        MSC_G7_MOBILE,
        MSC_TOTAL_COUNT
    }
}
