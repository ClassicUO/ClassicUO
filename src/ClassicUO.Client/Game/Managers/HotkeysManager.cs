// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using SDL2;

namespace ClassicUO.Game.Managers
{
    internal class HotKeyCombination
    {
        public SDL.SDL_Keycode Key { get; set; }
        public SDL.SDL_Keymod Mod { get; set; }
        public HotkeyAction KeyAction { get; set; }
    }

    internal class HotkeysManager
    {
        private readonly Dictionary<HotkeyAction, Action> _actions = new Dictionary<HotkeyAction, Action>();

        private readonly List<HotKeyCombination> _hotkeys = new List<HotKeyCombination>();

        public HotkeysManager()
        {
            Add(HotkeyAction.CastClumsy, () => GameActions.CastSpell(1));
            Add(HotkeyAction.CastCreateFood, () => GameActions.CastSpell(2));
            Add(HotkeyAction.CastFeeblemind, () => GameActions.CastSpell(3));
            Add(HotkeyAction.CastHeal, () => GameActions.CastSpell(4));
            Add(HotkeyAction.CastMagicArrow, () => GameActions.CastSpell(5));
            Add(HotkeyAction.CastNightSight, () => GameActions.CastSpell(6));
            Add(HotkeyAction.CastReactiveArmor, () => GameActions.CastSpell(7));
            Add(HotkeyAction.CastWeaken, () => GameActions.CastSpell(8));
            Add(HotkeyAction.CastAgility, () => GameActions.CastSpell(9));
            Add(HotkeyAction.CastCunning, () => GameActions.CastSpell(10));
            Add(HotkeyAction.CastCure, () => GameActions.CastSpell(11));
            Add(HotkeyAction.CastHarm, () => GameActions.CastSpell(12));
            Add(HotkeyAction.CastMagicTrap, () => GameActions.CastSpell(13));
            Add(HotkeyAction.CastMagicUntrap, () => GameActions.CastSpell(14));
            Add(HotkeyAction.CastProtection, () => GameActions.CastSpell(15));
            Add(HotkeyAction.CastStrength, () => GameActions.CastSpell(16));
            Add(HotkeyAction.CastBless, () => GameActions.CastSpell(17));
            Add(HotkeyAction.CastFireball, () => GameActions.CastSpell(18));
            Add(HotkeyAction.CastMagicLock, () => GameActions.CastSpell(19));
            Add(HotkeyAction.CastPosion, () => GameActions.CastSpell(20));
            Add(HotkeyAction.CastTelekinesis, () => GameActions.CastSpell(21));
            Add(HotkeyAction.CastTeleport, () => GameActions.CastSpell(22));
            Add(HotkeyAction.CastUnlock, () => GameActions.CastSpell(23));
            Add(HotkeyAction.CastWallOfStone, () => GameActions.CastSpell(24));
            Add(HotkeyAction.CastArchCure, () => GameActions.CastSpell(25));
            Add(HotkeyAction.CastArchProtection, () => GameActions.CastSpell(26));
            Add(HotkeyAction.CastCurse, () => GameActions.CastSpell(27));
            Add(HotkeyAction.CastFireField, () => GameActions.CastSpell(28));
            Add(HotkeyAction.CastGreaterHeal, () => GameActions.CastSpell(29));
            Add(HotkeyAction.CastLightning, () => GameActions.CastSpell(30));
            Add(HotkeyAction.CastManaDrain, () => GameActions.CastSpell(31));
            Add(HotkeyAction.CastRecall, () => GameActions.CastSpell(32));
            Add(HotkeyAction.CastBladeSpirits, () => GameActions.CastSpell(33));
            Add(HotkeyAction.CastDispelField, () => GameActions.CastSpell(34));
            Add(HotkeyAction.CastIncognito, () => GameActions.CastSpell(35));
            Add(HotkeyAction.CastMagicReflection, () => GameActions.CastSpell(36));
            Add(HotkeyAction.CastMindBlast, () => GameActions.CastSpell(37));
            Add(HotkeyAction.CastParalyze, () => GameActions.CastSpell(38));
            Add(HotkeyAction.CastPoisonField, () => GameActions.CastSpell(39));
            Add(HotkeyAction.CastSummonCreature, () => GameActions.CastSpell(40));
            Add(HotkeyAction.CastDispel, () => GameActions.CastSpell(41));
            Add(HotkeyAction.CastEnergyBolt, () => GameActions.CastSpell(42));
            Add(HotkeyAction.CastExplosion, () => GameActions.CastSpell(43));
            Add(HotkeyAction.CastInvisibility, () => GameActions.CastSpell(44));
            Add(HotkeyAction.CastMark, () => GameActions.CastSpell(45));
            Add(HotkeyAction.CastMassCurse, () => GameActions.CastSpell(46));
            Add(HotkeyAction.CastParalyzeField, () => GameActions.CastSpell(47));
            Add(HotkeyAction.CastReveal, () => GameActions.CastSpell(48));
            Add(HotkeyAction.CastChainLightning, () => GameActions.CastSpell(49));
            Add(HotkeyAction.CastEnergyField, () => GameActions.CastSpell(50));
            Add(HotkeyAction.CastFlamestrike, () => GameActions.CastSpell(51));
            Add(HotkeyAction.CastGateTravel, () => GameActions.CastSpell(52));
            Add(HotkeyAction.CastManaVampire, () => GameActions.CastSpell(53));
            Add(HotkeyAction.CastMassDispel, () => GameActions.CastSpell(54));
            Add(HotkeyAction.CastMeteorSwam, () => GameActions.CastSpell(55));
            Add(HotkeyAction.CastPolymorph, () => GameActions.CastSpell(56));
            Add(HotkeyAction.CastEarthquake, () => GameActions.CastSpell(57));
            Add(HotkeyAction.CastEnergyVortex, () => GameActions.CastSpell(58));
            Add(HotkeyAction.CastResurrection, () => GameActions.CastSpell(59));
            Add(HotkeyAction.CastAirElemental, () => GameActions.CastSpell(60));
            Add(HotkeyAction.CastSummonDaemon, () => GameActions.CastSpell(61));
            Add(HotkeyAction.CastEarthElemental, () => GameActions.CastSpell(62));
            Add(HotkeyAction.CastFireElemental, () => GameActions.CastSpell(63));
            Add(HotkeyAction.CastWaterElemental, () => GameActions.CastSpell(64));


            Add(HotkeyAction.CastAnimatedDead, () => GameActions.CastSpell(101));
            Add(HotkeyAction.CastBloodOath, () => GameActions.CastSpell(102));
            Add(HotkeyAction.CastCorpseSkin, () => GameActions.CastSpell(103));
            Add(HotkeyAction.CastCurseWeapon, () => GameActions.CastSpell(104));
            Add(HotkeyAction.CastEvilOmen, () => GameActions.CastSpell(105));
            Add(HotkeyAction.CastHorrificBeast, () => GameActions.CastSpell(106));
            Add(HotkeyAction.CastLichForm, () => GameActions.CastSpell(107));
            Add(HotkeyAction.CastMindRot, () => GameActions.CastSpell(108));
            Add(HotkeyAction.CastPainSpike, () => GameActions.CastSpell(109));
            Add(HotkeyAction.CastPoisonStrike, () => GameActions.CastSpell(110));
            Add(HotkeyAction.CastStrangle, () => GameActions.CastSpell(111));
            Add(HotkeyAction.CastSummonFamiliar, () => GameActions.CastSpell(112));
            Add(HotkeyAction.CastVampiricEmbrace, () => GameActions.CastSpell(113));
            Add(HotkeyAction.CastVangefulSpririt, () => GameActions.CastSpell(114));
            Add(HotkeyAction.CastWither, () => GameActions.CastSpell(115));
            Add(HotkeyAction.CastWraithForm, () => GameActions.CastSpell(116));
            Add(HotkeyAction.CastExorcism, () => GameActions.CastSpell(117));


            Add(HotkeyAction.CastCleanseByFire, () => GameActions.CastSpell(201));
            Add(HotkeyAction.CastCloseWounds, () => GameActions.CastSpell(202));
            Add(HotkeyAction.CastConsecrateWeapon, () => GameActions.CastSpell(203));
            Add(HotkeyAction.CastDispelEvil, () => GameActions.CastSpell(204));
            Add(HotkeyAction.CastDivineFury, () => GameActions.CastSpell(205));
            Add(HotkeyAction.CastEnemyOfOne, () => GameActions.CastSpell(206));
            Add(HotkeyAction.CastHolyLight, () => GameActions.CastSpell(207));
            Add(HotkeyAction.CastNobleSacrifice, () => GameActions.CastSpell(208));
            Add(HotkeyAction.CastRemoveCurse, () => GameActions.CastSpell(209));
            Add(HotkeyAction.CastSacredJourney, () => GameActions.CastSpell(210));


            Add(HotkeyAction.CastHonorableExecution, () => GameActions.CastSpell(401));
            Add(HotkeyAction.CastConfidence, () => GameActions.CastSpell(402));
            Add(HotkeyAction.CastEvasion, () => GameActions.CastSpell(403));
            Add(HotkeyAction.CastCounterAttack, () => GameActions.CastSpell(404));
            Add(HotkeyAction.CastLightningStrike, () => GameActions.CastSpell(405));
            Add(HotkeyAction.CastMomentumStrike, () => GameActions.CastSpell(406));


            Add(HotkeyAction.CastFocusAttack, () => GameActions.CastSpell(501));
            Add(HotkeyAction.CastDeathStrike, () => GameActions.CastSpell(502));
            Add(HotkeyAction.CastAnimalForm, () => GameActions.CastSpell(503));
            Add(HotkeyAction.CastKiAttack, () => GameActions.CastSpell(504));
            Add(HotkeyAction.CastSurpriseAttack, () => GameActions.CastSpell(505));
            Add(HotkeyAction.CastBackstab, () => GameActions.CastSpell(506));
            Add(HotkeyAction.CastShadowjump, () => GameActions.CastSpell(507));
            Add(HotkeyAction.CastMirrorImage, () => GameActions.CastSpell(508));


            Add(HotkeyAction.CastArcaneCircle, () => GameActions.CastSpell(601));
            Add(HotkeyAction.CastGiftOfRenewal, () => GameActions.CastSpell(602));
            Add(HotkeyAction.CastImmolatingWeapon, () => GameActions.CastSpell(603));
            Add(HotkeyAction.CastAttuneWeapon, () => GameActions.CastSpell(604));
            Add(HotkeyAction.CastThinderstorm, () => GameActions.CastSpell(605));
            Add(HotkeyAction.CastNaturesFury, () => GameActions.CastSpell(606));
            Add(HotkeyAction.CastSummonFey, () => GameActions.CastSpell(607));
            Add(HotkeyAction.CastSummonFiend, () => GameActions.CastSpell(608));
            Add(HotkeyAction.CastReaperForm, () => GameActions.CastSpell(609));
            Add(HotkeyAction.CastWildFire, () => GameActions.CastSpell(610));
            Add(HotkeyAction.CastEssenceOfWind, () => GameActions.CastSpell(611));
            Add(HotkeyAction.CastDryadAllure, () => GameActions.CastSpell(612));
            Add(HotkeyAction.CastEtherealVoyage, () => GameActions.CastSpell(613));
            Add(HotkeyAction.CastWordOfDeath, () => GameActions.CastSpell(614));
            Add(HotkeyAction.CastGiftOfLife, () => GameActions.CastSpell(615));


            Add(HotkeyAction.CastNetherBolt, () => GameActions.CastSpell(678));
            Add(HotkeyAction.CastHealingStone, () => GameActions.CastSpell(679));
            Add(HotkeyAction.CastPurgeMagic, () => GameActions.CastSpell(680));
            Add(HotkeyAction.CastEnchant, () => GameActions.CastSpell(681));
            Add(HotkeyAction.CastSleep, () => GameActions.CastSpell(682));
            Add(HotkeyAction.CastEagleStrike, () => GameActions.CastSpell(683));
            Add(HotkeyAction.CastAnimatedWeapon, () => GameActions.CastSpell(684));
            Add(HotkeyAction.CastStoneForm, () => GameActions.CastSpell(685));
            Add(HotkeyAction.CastSpellTrigger, () => GameActions.CastSpell(686));
            Add(HotkeyAction.CastMassSleep, () => GameActions.CastSpell(687));
            Add(HotkeyAction.CastCleansingWinds, () => GameActions.CastSpell(688));
            Add(HotkeyAction.CastBombard, () => GameActions.CastSpell(689));
            Add(HotkeyAction.CastSpellPlague, () => GameActions.CastSpell(690));
            Add(HotkeyAction.CastHailStorm, () => GameActions.CastSpell(691));
            Add(HotkeyAction.CastNetherCyclone, () => GameActions.CastSpell(692));
            Add(HotkeyAction.CastRisingColossus, () => GameActions.CastSpell(693));


            Add(HotkeyAction.CastInspire, () => GameActions.CastSpell(701));
            Add(HotkeyAction.CastInvigorate, () => GameActions.CastSpell(702));
            Add(HotkeyAction.CastResilience, () => GameActions.CastSpell(703));
            Add(HotkeyAction.CastPerseverance, () => GameActions.CastSpell(704));
            Add(HotkeyAction.CastTribulation, () => GameActions.CastSpell(705));
            Add(HotkeyAction.CastDespair, () => GameActions.CastSpell(706));


            Add(HotkeyAction.CastDeathRay, () => GameActions.CastSpell(707));
            Add(HotkeyAction.CastEtherealBurst, () => GameActions.CastSpell(708));
            Add(HotkeyAction.CastNetherBlast, () => GameActions.CastSpell(709));
            Add(HotkeyAction.CastMysticWeapon, () => GameActions.CastSpell(710));
            Add(HotkeyAction.CastCommandUndead, () => GameActions.CastSpell(711));
            Add(HotkeyAction.CastConduit, () => GameActions.CastSpell(712));
            Add(HotkeyAction.CastManaShield, () => GameActions.CastSpell(713));
            Add(HotkeyAction.CastSummonReaper, () => GameActions.CastSpell(714));
            Add(HotkeyAction.CastEnchantedSummoning, () => GameActions.CastSpell(715));
            Add(HotkeyAction.CastAnticipateHit, () => GameActions.CastSpell(716));
            Add(HotkeyAction.CastWarcry, () => GameActions.CastSpell(717));
            Add(HotkeyAction.CastIntuition, () => GameActions.CastSpell(718));
            Add(HotkeyAction.CastRejuvenate, () => GameActions.CastSpell(719));
            Add(HotkeyAction.CastHolyFist, () => GameActions.CastSpell(720));
            Add(HotkeyAction.CastShadow, () => GameActions.CastSpell(721));
            Add(HotkeyAction.CastWhiteTigerForm, () => GameActions.CastSpell(722));
            Add(HotkeyAction.CastFlamingShot, () => GameActions.CastSpell(723));
            Add(HotkeyAction.CastPlayingTheOdds, () => GameActions.CastSpell(724));
            Add(HotkeyAction.CastThrust, () => GameActions.CastSpell(725));
            Add(HotkeyAction.CastPierce, () => GameActions.CastSpell(726));
            Add(HotkeyAction.CastStagger, () => GameActions.CastSpell(727));
            Add(HotkeyAction.CastToughness, () => GameActions.CastSpell(728));
            Add(HotkeyAction.CastOnslaught, () => GameActions.CastSpell(729));
            Add(HotkeyAction.CastFocusedEye, () => GameActions.CastSpell(730));
            Add(HotkeyAction.CastElementalFury, () => GameActions.CastSpell(731));
            Add(HotkeyAction.CastCalledShot, () => GameActions.CastSpell(732));
            Add(HotkeyAction.CastWarriorsGifts, () => GameActions.CastSpell(733));
            Add(HotkeyAction.CastShieldBash, () => GameActions.CastSpell(734));
            Add(HotkeyAction.CastBodyguard, () => GameActions.CastSpell(735));
            Add(HotkeyAction.CastHeightenSenses, () => GameActions.CastSpell(736));
            Add(HotkeyAction.CastTolerance, () => GameActions.CastSpell(737));
            Add(HotkeyAction.CastInjectedStrike, () => GameActions.CastSpell(738));
            Add(HotkeyAction.CastPotency, () => GameActions.CastSpell(739));
            Add(HotkeyAction.CastRampage, () => GameActions.CastSpell(740));
            Add(HotkeyAction.CastFistsofFury, () => GameActions.CastSpell(741));
            Add(HotkeyAction.CastKnockout, () => GameActions.CastSpell(742));
            Add(HotkeyAction.CastWhispering, () => GameActions.CastSpell(743));
            Add(HotkeyAction.CastCombatTraining, () => GameActions.CastSpell(744));
            Add(HotkeyAction.CastBoarding, () => GameActions.CastSpell(745));
        }


        public bool Bind(HotkeyAction action, SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            foreach (HotKeyCombination h in _hotkeys)
            {
                if (h.Key == key && h.Mod == mod)
                {
                    return false;
                }
            }

            _hotkeys.Add
            (
                new HotKeyCombination
                {
                    Key = key,
                    Mod = mod,
                    KeyAction = action
                }
            );

            return true;
        }

        public void UnBind(HotkeyAction action)
        {
            for (int i = 0; i < _hotkeys.Count; i++)
            {
                HotKeyCombination h = _hotkeys[i];

                if (h.KeyAction == action)
                {
                    _hotkeys.RemoveAt(i);

                    break;
                }
            }
        }

        public bool TryExecuteIfBinded(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, out Action action)
        {
            for (int i = 0; i < _hotkeys.Count; i++)
            {
                HotKeyCombination h = _hotkeys[i];

                if (h.Key == key && h.Mod == mod)
                {
                    if (_actions.TryGetValue(h.KeyAction, out action))
                    {
                        return true;
                    }

                    break;
                }
            }

            action = null;

            return false;
        }

        public Dictionary<HotkeyAction, Action> GetValues()
        {
            return _actions;
        }

        private void Add(HotkeyAction action, Action handler)
        {
            _actions.Add(action, handler);
        }
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