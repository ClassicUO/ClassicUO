// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using SDL3;

namespace ClassicUO.Game.Input.Hotkeys
{
    internal class HotKeyCombination
    {
        public SDL.SDL_Keycode Key { get; set; }
        public SDL.SDL_Keymod Mod { get; set; }
        public HotkeyAction KeyAction { get; set; }
    }

    /// <summary>
    /// Facade over the three Hotkeys collaborators: keeps the existing
    /// public surface (<c>HotkeysManager.Bind / UnBind / TryExecuteIfBinded /
    /// GetValues</c>). The default action catalog, binding list and
    /// dispatcher live in dedicated cohesive classes under
    /// <see cref="Game.Input.Hotkeys"/>. No <c>EventSink</c> subscriptions.
    /// </summary>
    internal class HotkeysManager
    {
        private readonly IHotkeyActionRegistry _registry;
        private readonly IHotkeyBindingStore _bindings;
        private readonly IHotkeyDispatcher _dispatcher;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public HotkeysManager()
            : this(new HotkeyActionRegistry(), new HotkeyBindingStore())
        {
        }

        /// <summary>Test / extension seam — inject a fake registry and store.</summary>
        internal HotkeysManager(IHotkeyActionRegistry registry, IHotkeyBindingStore bindings)
            : this(registry, bindings, new HotkeyDispatcher(bindings, registry))
        {
        }

        /// <summary>Full DI seam — inject all three collaborators.</summary>
        internal HotkeysManager(IHotkeyActionRegistry registry, IHotkeyBindingStore bindings, IHotkeyDispatcher dispatcher)
        {
            _registry = registry;
            _bindings = bindings;
            _dispatcher = dispatcher;
        }

        // ---- Binding facade ----
        public bool Bind(HotkeyAction action, SDL.SDL_Keycode key, SDL.SDL_Keymod mod) => _bindings.Bind(action, key, mod);
        public void UnBind(HotkeyAction action) => _bindings.UnBind(action);

        // ---- Dispatch facade ----
        public bool TryExecuteIfBinded(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, out Action action) => _dispatcher.TryExecuteIfBinded(key, mod, out action);

        // ---- Registry facade ----
        public Dictionary<HotkeyAction, Action> GetValues() => _registry.GetValues();
    }

    internal enum HotkeyAction
    {
        None,

        #region Magery

        CastClumsy,
        CastCreateFood,
        CastFeeblemind,
        CastHeal,
        CastMagicArrow,
        CastNightSight,
        CastReactiveArmor,
        CastWeaken,
        CastAgility,
        CastCunning,
        CastCure,
        CastHarm,
        CastMagicTrap,
        CastMagicUntrap,
        CastProtection,
        CastStrength,
        CastBless,
        CastFireball,
        CastMagicLock,
        CastPosion,
        CastTelekinesis,
        CastTeleport,
        CastUnlock,
        CastWallOfStone,
        CastArchCure,
        CastArchProtection,
        CastCurse,
        CastFireField,
        CastGreaterHeal,
        CastLightning,
        CastManaDrain,
        CastRecall,
        CastBladeSpirits,
        CastDispelField,
        CastIncognito,
        CastMagicReflection,
        CastMindBlast,
        CastParalyze,
        CastPoisonField,
        CastSummonCreature,
        CastDispel,
        CastEnergyBolt,
        CastExplosion,
        CastInvisibility,
        CastMark,
        CastMassCurse,
        CastParalyzeField,
        CastReveal,
        CastChainLightning,
        CastEnergyField,
        CastFlamestrike,
        CastGateTravel,
        CastManaVampire,
        CastMassDispel,
        CastMeteorSwam,
        CastPolymorph,
        CastEarthquake,
        CastEnergyVortex,
        CastResurrection,
        CastAirElemental,
        CastSummonDaemon,
        CastEarthElemental,
        CastFireElemental,
        CastWaterElemental,

        #endregion

        #region Necro

        CastAnimatedDead,
        CastBloodOath,
        CastCorpseSkin,
        CastCurseWeapon,
        CastEvilOmen,
        CastHorrificBeast,
        CastLichForm,
        CastMindRot,
        CastPainSpike,
        CastPoisonStrike,
        CastStrangle,
        CastSummonFamiliar,
        CastVampiricEmbrace,
        CastVangefulSpririt,
        CastWither,
        CastWraithForm,
        CastExorcism,

        #endregion

        #region Chivalry

        CastCleanseByFire,
        CastCloseWounds,
        CastConsecrateWeapon,
        CastDispelEvil,
        CastDivineFury,
        CastEnemyOfOne,
        CastHolyLight,
        CastNobleSacrifice,
        CastRemoveCurse,
        CastSacredJourney,

        #endregion

        #region Bushido

        CastHonorableExecution,
        CastConfidence,
        CastEvasion,
        CastCounterAttack,
        CastLightningStrike,
        CastMomentumStrike,

        #endregion

        #region Ninja

        CastFocusAttack,
        CastDeathStrike,
        CastAnimalForm,
        CastKiAttack,
        CastSurpriseAttack,
        CastBackstab,
        CastShadowjump,
        CastMirrorImage,

        #endregion

        #region Spellweaving

        CastArcaneCircle,
        CastGiftOfRenewal,
        CastImmolatingWeapon,
        CastAttuneWeapon,
        CastThinderstorm,
        CastNaturesFury,
        CastSummonFey,
        CastSummonFiend,
        CastReaperForm,
        CastWildFire,
        CastEssenceOfWind,
        CastDryadAllure,
        CastEtherealVoyage,
        CastWordOfDeath,
        CastGiftOfLife,
        CastArcaneEmpowerment,

        #endregion

        #region Mysticism

        CastNetherBolt,
        CastHealingStone,
        CastPurgeMagic,
        CastEnchant,
        CastSleep,
        CastEagleStrike,
        CastAnimatedWeapon,
        CastStoneForm,
        CastSpellTrigger,
        CastMassSleep,
        CastCleansingWinds,
        CastBombard,
        CastSpellPlague,
        CastHailStorm,
        CastNetherCyclone,
        CastRisingColossus,

        #endregion

        #region Bardic

        CastInspire,
        CastInvigorate,
        CastResilience,
        CastPerseverance,
        CastTribulation,
        CastDespair,

        #endregion

        #region Other mastery spells
        CastDeathRay,
        CastEtherealBurst,
        CastNetherBlast,
        CastMysticWeapon,
        CastCommandUndead,
        CastConduit,
        CastManaShield,
        CastSummonReaper,
        CastEnchantedSummoning,
        CastAnticipateHit,
        CastWarcry,
        CastIntuition,
        CastRejuvenate,
        CastHolyFist,
        CastShadow,
        CastWhiteTigerForm,
        CastFlamingShot,
        CastPlayingTheOdds,
        CastThrust,
        CastPierce,
        CastStagger,
        CastToughness,
        CastOnslaught,
        CastFocusedEye,
        CastElementalFury,
        CastCalledShot,
        CastWarriorsGifts,
        CastShieldBash,
        CastBodyguard,
        CastHeightenSenses,
        CastTolerance,
        CastInjectedStrike,
        CastPotency,
        CastRampage,
        CastFistsofFury,
        CastKnockout,
        CastWhispering,
        CastCombatTraining,
        CastBoarding,
        #endregion

        #region Skills

        UseSkillAnatomy,
        UseSkillAnimalLore,
        UseSkillAnimalTaming,
        UseSkillArmsLore,
        UseSkillBegging,
        UseSkillCartography,
        UseSkillDetectingHidden,
        UseSkillEnticement,
        UseSkillEvaluatingIntelligence,
        UseSkillForensicEvaluation,
        UseSkillHiding,
        UseSkillImbuing,
        UseSkillInscription,
        UseSkillItemIdentificator,
        UseSkillMeditation,
        UseSkillPeacemaking,
        UseSkillPoisoning,
        UseSkillProvocation,
        UseSkillRemoveTrap,
        UseSkillSpiritSpeak,
        UseSkillStealing,
        UseSkillStealth,
        UseSkillTasteIdentification,
        UseSkillTracking,

        #endregion

        #region Virtues

        UseVirtueHonor,
        UseVirtueSacrifice,
        UseVirtueValor,

        #endregion

        #region WalkDir

        WalkToNW,
        WalkToN,
        WalkToNE,
        WalkToE,
        WalkToSE,
        WalkToS,
        WalkToSW,
        WalkToW,

        #endregion

        #region GumpAction

        OpenSettings,
        OpenPaperdoll,
        OpenStatus,
        OpenJournal,
        OpenSkills,
        OpenMageSpellbook,
        OpenNecroSpellbook,
        OpenChivaSpellbook,
        OpenBushidoSpellbook,
        OpenNinjaSpellbook,
        OpenSpellweaverSpellbook,
        OpenMysticSpellbook,
        OpenRacialAbilitiesBook,
        OpenChat,
        OpenBackpack,
        OpenMinimap,
        OpenParty,
        OpenPartyChat,
        OpenGuild,
        OpenQuestLog,
        ToggleBuffGump,
        QuitGame,
        SaveGumps,

        #endregion

        #region Abilities

        UsePrimaryAbility,
        UseSecondaryAbility,
        ClearCurrentAbility,
        ToggleGargoyleFly,

        #endregion

        #region UseItems

        UseSelectedItem,
        UseCurrentTarget,
        BandageSelf,
        BandageTarget,

        #endregion

        #region Speech

        Say,
        Emote,
        Whisper,
        Yell,

        #endregion

        #region Targetting

        TargetNext,
        TargetClosest,
        TargetSelf,

        #endregion

        #region Attack

        AttackLast,
        AttackSelected,

        #endregion

        #region SelectTarget

        SelectNext,
        SelectPrevious,
        SelectNearest,

        #endregion

        #region Misc

        ArmDisarm,
        AllNames,
        Bow,
        Salute,
        AlwaysRun,
        EquipLastWeapon,

        #endregion
    }
}
