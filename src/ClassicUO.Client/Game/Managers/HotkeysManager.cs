// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using SDL3;

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
        private readonly World _world;

        public HotkeysManager(World world)
        {
            _world = world;
            Add(HotkeyAction.CastClumsy, () => GameActions.CastSpell(_world, 1));
            Add(HotkeyAction.CastCreateFood, () => GameActions.CastSpell(_world,2));
            Add(HotkeyAction.CastFeeblemind, () => GameActions.CastSpell(_world,3));
            Add(HotkeyAction.CastHeal, () => GameActions.CastSpell(_world,4));
            Add(HotkeyAction.CastMagicArrow, () => GameActions.CastSpell(_world,5));
            Add(HotkeyAction.CastNightSight, () => GameActions.CastSpell(_world,6));
            Add(HotkeyAction.CastReactiveArmor, () => GameActions.CastSpell(_world,7));
            Add(HotkeyAction.CastWeaken, () => GameActions.CastSpell(_world,8));
            Add(HotkeyAction.CastAgility, () => GameActions.CastSpell(_world,9));
            Add(HotkeyAction.CastCunning, () => GameActions.CastSpell(_world,10));
            Add(HotkeyAction.CastCure, () => GameActions.CastSpell(_world,11));
            Add(HotkeyAction.CastHarm, () => GameActions.CastSpell(_world,12));
            Add(HotkeyAction.CastMagicTrap, () => GameActions.CastSpell(_world,13));
            Add(HotkeyAction.CastMagicUntrap, () => GameActions.CastSpell(_world,14));
            Add(HotkeyAction.CastProtection, () => GameActions.CastSpell(_world,15));
            Add(HotkeyAction.CastStrength, () => GameActions.CastSpell(_world,16));
            Add(HotkeyAction.CastBless, () => GameActions.CastSpell(_world,17));
            Add(HotkeyAction.CastFireball, () => GameActions.CastSpell(_world,18));
            Add(HotkeyAction.CastMagicLock, () => GameActions.CastSpell(_world,19));
            Add(HotkeyAction.CastPosion, () => GameActions.CastSpell(_world,20));
            Add(HotkeyAction.CastTelekinesis, () => GameActions.CastSpell(_world,21));
            Add(HotkeyAction.CastTeleport, () => GameActions.CastSpell(_world,22));
            Add(HotkeyAction.CastUnlock, () => GameActions.CastSpell(_world,23));
            Add(HotkeyAction.CastWallOfStone, () => GameActions.CastSpell(_world,24));
            Add(HotkeyAction.CastArchCure, () => GameActions.CastSpell(_world,25));
            Add(HotkeyAction.CastArchProtection, () => GameActions.CastSpell(_world,26));
            Add(HotkeyAction.CastCurse, () => GameActions.CastSpell(_world,27));
            Add(HotkeyAction.CastFireField, () => GameActions.CastSpell(_world,28));
            Add(HotkeyAction.CastGreaterHeal, () => GameActions.CastSpell(_world,29));
            Add(HotkeyAction.CastLightning, () => GameActions.CastSpell(_world,30));
            Add(HotkeyAction.CastManaDrain, () => GameActions.CastSpell(_world,31));
            Add(HotkeyAction.CastRecall, () => GameActions.CastSpell(_world,32));
            Add(HotkeyAction.CastBladeSpirits, () => GameActions.CastSpell(_world,33));
            Add(HotkeyAction.CastDispelField, () => GameActions.CastSpell(_world,34));
            Add(HotkeyAction.CastIncognito, () => GameActions.CastSpell(_world,35));
            Add(HotkeyAction.CastMagicReflection, () => GameActions.CastSpell(_world,36));
            Add(HotkeyAction.CastMindBlast, () => GameActions.CastSpell(_world,37));
            Add(HotkeyAction.CastParalyze, () => GameActions.CastSpell(_world,38));
            Add(HotkeyAction.CastPoisonField, () => GameActions.CastSpell(_world,39));
            Add(HotkeyAction.CastSummonCreature, () => GameActions.CastSpell(_world,40));
            Add(HotkeyAction.CastDispel, () => GameActions.CastSpell(_world,41));
            Add(HotkeyAction.CastEnergyBolt, () => GameActions.CastSpell(_world,42));
            Add(HotkeyAction.CastExplosion, () => GameActions.CastSpell(_world,43));
            Add(HotkeyAction.CastInvisibility, () => GameActions.CastSpell(_world,44));
            Add(HotkeyAction.CastMark, () => GameActions.CastSpell(_world,45));
            Add(HotkeyAction.CastMassCurse, () => GameActions.CastSpell(_world,46));
            Add(HotkeyAction.CastParalyzeField, () => GameActions.CastSpell(_world,47));
            Add(HotkeyAction.CastReveal, () => GameActions.CastSpell(_world,48));
            Add(HotkeyAction.CastChainLightning, () => GameActions.CastSpell(_world,49));
            Add(HotkeyAction.CastEnergyField, () => GameActions.CastSpell(_world,50));
            Add(HotkeyAction.CastFlamestrike, () => GameActions.CastSpell(_world,51));
            Add(HotkeyAction.CastGateTravel, () => GameActions.CastSpell(_world,52));
            Add(HotkeyAction.CastManaVampire, () => GameActions.CastSpell(_world,53));
            Add(HotkeyAction.CastMassDispel, () => GameActions.CastSpell(_world,54));
            Add(HotkeyAction.CastMeteorSwam, () => GameActions.CastSpell(_world,55));
            Add(HotkeyAction.CastPolymorph, () => GameActions.CastSpell(_world,56));
            Add(HotkeyAction.CastEarthquake, () => GameActions.CastSpell(_world,57));
            Add(HotkeyAction.CastEnergyVortex, () => GameActions.CastSpell(_world,58));
            Add(HotkeyAction.CastResurrection, () => GameActions.CastSpell(_world,59));
            Add(HotkeyAction.CastAirElemental, () => GameActions.CastSpell(_world,60));
            Add(HotkeyAction.CastSummonDaemon, () => GameActions.CastSpell(_world,61));
            Add(HotkeyAction.CastEarthElemental, () => GameActions.CastSpell(_world,62));
            Add(HotkeyAction.CastFireElemental, () => GameActions.CastSpell(_world,63));
            Add(HotkeyAction.CastWaterElemental, () => GameActions.CastSpell(_world,64));


            Add(HotkeyAction.CastAnimatedDead, () => GameActions.CastSpell(_world,101));
            Add(HotkeyAction.CastBloodOath, () => GameActions.CastSpell(_world,102));
            Add(HotkeyAction.CastCorpseSkin, () => GameActions.CastSpell(_world,103));
            Add(HotkeyAction.CastCurseWeapon, () => GameActions.CastSpell(_world,104));
            Add(HotkeyAction.CastEvilOmen, () => GameActions.CastSpell(_world,105));
            Add(HotkeyAction.CastHorrificBeast, () => GameActions.CastSpell(_world,106));
            Add(HotkeyAction.CastLichForm, () => GameActions.CastSpell(_world,107));
            Add(HotkeyAction.CastMindRot, () => GameActions.CastSpell(_world,108));
            Add(HotkeyAction.CastPainSpike, () => GameActions.CastSpell(_world,109));
            Add(HotkeyAction.CastPoisonStrike, () => GameActions.CastSpell(_world,110));
            Add(HotkeyAction.CastStrangle, () => GameActions.CastSpell(_world,111));
            Add(HotkeyAction.CastSummonFamiliar, () => GameActions.CastSpell(_world,112));
            Add(HotkeyAction.CastVampiricEmbrace, () => GameActions.CastSpell(_world,113));
            Add(HotkeyAction.CastVangefulSpririt, () => GameActions.CastSpell(_world,114));
            Add(HotkeyAction.CastWither, () => GameActions.CastSpell(_world,115));
            Add(HotkeyAction.CastWraithForm, () => GameActions.CastSpell(_world,116));
            Add(HotkeyAction.CastExorcism, () => GameActions.CastSpell(_world,117));


            Add(HotkeyAction.CastCleanseByFire, () => GameActions.CastSpell(_world,201));
            Add(HotkeyAction.CastCloseWounds, () => GameActions.CastSpell(_world,202));
            Add(HotkeyAction.CastConsecrateWeapon, () => GameActions.CastSpell(_world,203));
            Add(HotkeyAction.CastDispelEvil, () => GameActions.CastSpell(_world,204));
            Add(HotkeyAction.CastDivineFury, () => GameActions.CastSpell(_world,205));
            Add(HotkeyAction.CastEnemyOfOne, () => GameActions.CastSpell(_world,206));
            Add(HotkeyAction.CastHolyLight, () => GameActions.CastSpell(_world,207));
            Add(HotkeyAction.CastNobleSacrifice, () => GameActions.CastSpell(_world,208));
            Add(HotkeyAction.CastRemoveCurse, () => GameActions.CastSpell(_world,209));
            Add(HotkeyAction.CastSacredJourney, () => GameActions.CastSpell(_world,210));


            Add(HotkeyAction.CastHonorableExecution, () => GameActions.CastSpell(_world,401));
            Add(HotkeyAction.CastConfidence, () => GameActions.CastSpell(_world,402));
            Add(HotkeyAction.CastEvasion, () => GameActions.CastSpell(_world,403));
            Add(HotkeyAction.CastCounterAttack, () => GameActions.CastSpell(_world,404));
            Add(HotkeyAction.CastLightningStrike, () => GameActions.CastSpell(_world,405));
            Add(HotkeyAction.CastMomentumStrike, () => GameActions.CastSpell(_world,406));


            Add(HotkeyAction.CastFocusAttack, () => GameActions.CastSpell(_world,501));
            Add(HotkeyAction.CastDeathStrike, () => GameActions.CastSpell(_world,502));
            Add(HotkeyAction.CastAnimalForm, () => GameActions.CastSpell(_world,503));
            Add(HotkeyAction.CastKiAttack, () => GameActions.CastSpell(_world,504));
            Add(HotkeyAction.CastSurpriseAttack, () => GameActions.CastSpell(_world,505));
            Add(HotkeyAction.CastBackstab, () => GameActions.CastSpell(_world,506));
            Add(HotkeyAction.CastShadowjump, () => GameActions.CastSpell(_world,507));
            Add(HotkeyAction.CastMirrorImage, () => GameActions.CastSpell(_world,508));


            Add(HotkeyAction.CastArcaneCircle, () => GameActions.CastSpell(_world,601));
            Add(HotkeyAction.CastGiftOfRenewal, () => GameActions.CastSpell(_world,602));
            Add(HotkeyAction.CastImmolatingWeapon, () => GameActions.CastSpell(_world,603));
            Add(HotkeyAction.CastAttuneWeapon, () => GameActions.CastSpell(_world,604));
            Add(HotkeyAction.CastThinderstorm, () => GameActions.CastSpell(_world,605));
            Add(HotkeyAction.CastNaturesFury, () => GameActions.CastSpell(_world,606));
            Add(HotkeyAction.CastSummonFey, () => GameActions.CastSpell(_world,607));
            Add(HotkeyAction.CastSummonFiend, () => GameActions.CastSpell(_world,608));
            Add(HotkeyAction.CastReaperForm, () => GameActions.CastSpell(_world,609));
            Add(HotkeyAction.CastWildFire, () => GameActions.CastSpell(_world,610));
            Add(HotkeyAction.CastEssenceOfWind, () => GameActions.CastSpell(_world,611));
            Add(HotkeyAction.CastDryadAllure, () => GameActions.CastSpell(_world,612));
            Add(HotkeyAction.CastEtherealVoyage, () => GameActions.CastSpell(_world,613));
            Add(HotkeyAction.CastWordOfDeath, () => GameActions.CastSpell(_world,614));
            Add(HotkeyAction.CastGiftOfLife, () => GameActions.CastSpell(_world,615));


            Add(HotkeyAction.CastNetherBolt, () => GameActions.CastSpell(_world,678));
            Add(HotkeyAction.CastHealingStone, () => GameActions.CastSpell(_world,679));
            Add(HotkeyAction.CastPurgeMagic, () => GameActions.CastSpell(_world,680));
            Add(HotkeyAction.CastEnchant, () => GameActions.CastSpell(_world,681));
            Add(HotkeyAction.CastSleep, () => GameActions.CastSpell(_world,682));
            Add(HotkeyAction.CastEagleStrike, () => GameActions.CastSpell(_world,683));
            Add(HotkeyAction.CastAnimatedWeapon, () => GameActions.CastSpell(_world,684));
            Add(HotkeyAction.CastStoneForm, () => GameActions.CastSpell(_world,685));
            Add(HotkeyAction.CastSpellTrigger, () => GameActions.CastSpell(_world,686));
            Add(HotkeyAction.CastMassSleep, () => GameActions.CastSpell(_world,687));
            Add(HotkeyAction.CastCleansingWinds, () => GameActions.CastSpell(_world,688));
            Add(HotkeyAction.CastBombard, () => GameActions.CastSpell(_world,689));
            Add(HotkeyAction.CastSpellPlague, () => GameActions.CastSpell(_world,690));
            Add(HotkeyAction.CastHailStorm, () => GameActions.CastSpell(_world,691));
            Add(HotkeyAction.CastNetherCyclone, () => GameActions.CastSpell(_world,692));
            Add(HotkeyAction.CastRisingColossus, () => GameActions.CastSpell(_world,693));


            Add(HotkeyAction.CastInspire, () => GameActions.CastSpell(_world,701));
            Add(HotkeyAction.CastInvigorate, () => GameActions.CastSpell(_world,702));
            Add(HotkeyAction.CastResilience, () => GameActions.CastSpell(_world,703));
            Add(HotkeyAction.CastPerseverance, () => GameActions.CastSpell(_world,704));
            Add(HotkeyAction.CastTribulation, () => GameActions.CastSpell(_world,705));
            Add(HotkeyAction.CastDespair, () => GameActions.CastSpell(_world,706));


            Add(HotkeyAction.CastDeathRay, () => GameActions.CastSpell(_world,707));
            Add(HotkeyAction.CastEtherealBurst, () => GameActions.CastSpell(_world,708));
            Add(HotkeyAction.CastNetherBlast, () => GameActions.CastSpell(_world,709));
            Add(HotkeyAction.CastMysticWeapon, () => GameActions.CastSpell(_world,710));
            Add(HotkeyAction.CastCommandUndead, () => GameActions.CastSpell(_world,711));
            Add(HotkeyAction.CastConduit, () => GameActions.CastSpell(_world,712));
            Add(HotkeyAction.CastManaShield, () => GameActions.CastSpell(_world,713));
            Add(HotkeyAction.CastSummonReaper, () => GameActions.CastSpell(_world,714));
            Add(HotkeyAction.CastEnchantedSummoning, () => GameActions.CastSpell(_world,715));
            Add(HotkeyAction.CastAnticipateHit, () => GameActions.CastSpell(_world,716));
            Add(HotkeyAction.CastWarcry, () => GameActions.CastSpell(_world,717));
            Add(HotkeyAction.CastIntuition, () => GameActions.CastSpell(_world,718));
            Add(HotkeyAction.CastRejuvenate, () => GameActions.CastSpell(_world,719));
            Add(HotkeyAction.CastHolyFist, () => GameActions.CastSpell(_world,720));
            Add(HotkeyAction.CastShadow, () => GameActions.CastSpell(_world,721));
            Add(HotkeyAction.CastWhiteTigerForm, () => GameActions.CastSpell(_world,722));
            Add(HotkeyAction.CastFlamingShot, () => GameActions.CastSpell(_world,723));
            Add(HotkeyAction.CastPlayingTheOdds, () => GameActions.CastSpell(_world,724));
            Add(HotkeyAction.CastThrust, () => GameActions.CastSpell(_world,725));
            Add(HotkeyAction.CastPierce, () => GameActions.CastSpell(_world,726));
            Add(HotkeyAction.CastStagger, () => GameActions.CastSpell(_world,727));
            Add(HotkeyAction.CastToughness, () => GameActions.CastSpell(_world,728));
            Add(HotkeyAction.CastOnslaught, () => GameActions.CastSpell(_world,729));
            Add(HotkeyAction.CastFocusedEye, () => GameActions.CastSpell(_world,730));
            Add(HotkeyAction.CastElementalFury, () => GameActions.CastSpell(_world,731));
            Add(HotkeyAction.CastCalledShot, () => GameActions.CastSpell(_world,732));
            Add(HotkeyAction.CastWarriorsGifts, () => GameActions.CastSpell(_world,733));
            Add(HotkeyAction.CastShieldBash, () => GameActions.CastSpell(_world,734));
            Add(HotkeyAction.CastBodyguard, () => GameActions.CastSpell(_world,735));
            Add(HotkeyAction.CastHeightenSenses, () => GameActions.CastSpell(_world,736));
            Add(HotkeyAction.CastTolerance, () => GameActions.CastSpell(_world,737));
            Add(HotkeyAction.CastInjectedStrike, () => GameActions.CastSpell(_world,738));
            Add(HotkeyAction.CastPotency, () => GameActions.CastSpell(_world,739));
            Add(HotkeyAction.CastRampage, () => GameActions.CastSpell(_world,740));
            Add(HotkeyAction.CastFistsofFury, () => GameActions.CastSpell(_world,741));
            Add(HotkeyAction.CastKnockout, () => GameActions.CastSpell(_world,742));
            Add(HotkeyAction.CastWhispering, () => GameActions.CastSpell(_world,743));
            Add(HotkeyAction.CastCombatTraining, () => GameActions.CastSpell(_world,744));
            Add(HotkeyAction.CastBoarding, () => GameActions.CastSpell(_world,745));
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