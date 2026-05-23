// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Xml;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Macros
{
    /// <summary>
    /// Facade over the Macros collaborators. Keeps the existing public
    /// surface (<c>_world.Macros.X</c>) and the <see cref="LinkedObject"/>
    /// inheritance so external callers can still poke
    /// <see cref="LinkedObject.Items"/>, <see cref="LinkedObject.PushToBack"/>,
    /// <see cref="LinkedObject.Remove"/> and <see cref="LinkedObject.Clear"/>.
    /// XML persistence + defaults live in <see cref="IMacroStore"/>, hotkey
    /// and name lookup in <see cref="IMacroLookup"/>, and the runtime macro
    /// VM (per-frame opcode dispatch, target / bandage wait flags) in
    /// <see cref="IMacroExecutor"/>.
    /// </summary>
    internal sealed class MacroManager : LinkedObject
    {
        public static readonly string[] MacroNames = Enum.GetNames(typeof(MacroType));

        private readonly IMacroStore _store;
        private readonly IMacroLookup _lookup;
        private readonly IMacroExecutor _executor;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public MacroManager(World world)
            : this(new MacroStore(), new MacroLookup(), new MacroExecutor(world))
        {
        }

        /// <summary>Full DI seam — inject all collaborators.</summary>
        internal MacroManager(IMacroStore store, IMacroLookup lookup, IMacroExecutor executor)
        {
            _store = store;
            _lookup = lookup;
            _executor = executor;
        }

        // ---- Executor facade ----
        public long WaitForTargetTimer
        {
            get => _executor.WaitForTargetTimer;
            set => _executor.WaitForTargetTimer = value;
        }

        public bool WaitingBandageTarget
        {
            get => _executor.WaitingBandageTarget;
            set => _executor.WaitingBandageTarget = value;
        }

        public void SetMacroToExecute(MacroObject macro) => _executor.SetMacroToExecute(macro);
        public void Update() => _executor.Update();

        // ---- Store facade ----
        public void Load() => _store.Load(this);
        public void Save() => _store.Save(this);
        public List<Macro> GetAllMacros() => _store.GetAllMacros(this);

        // ---- Lookup facade ----
        public Macro FindMacro(SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift) => _lookup.FindMacro(this, key, alt, ctrl, shift);
        public Macro FindMacro(MouseButtonType button, bool alt, bool ctrl, bool shift) => _lookup.FindMacro(this, button, alt, ctrl, shift);
        public Macro FindMacro(bool wheelUp, bool alt, bool ctrl, bool shift) => _lookup.FindMacro(this, wheelUp, alt, ctrl, shift);
        public Macro FindMacro(string name) => _lookup.FindMacro(this, name);
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
                case MacroType.UseCounterBarSlot:
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
                case MacroType.UseCounterBarSlot:
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
        LookAtMouse,
        UseCounterBarSlot
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
