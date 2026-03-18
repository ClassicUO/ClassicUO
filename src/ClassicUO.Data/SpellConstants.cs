// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    /// <summary>
    /// Named constants for spell school ID ranges. Each school's spells use a
    /// contiguous block of IDs; these constants make range checks self-documenting.
    /// </summary>
    public static class SpellSchoolConstants
    {
        // ── Magery ──────────────────────────────────────────────────────
        public const int MageryStart = 1;
        public const int MageryCount = 64;
        public const int MageryEnd = 64;

        // ── Necromancy ──────────────────────────────────────────────────
        public const int NecromancyStart = 101;
        public const int NecromancyCount = 17;
        public const int NecromancyEnd = 117;

        // ── Chivalry ────────────────────────────────────────────────────
        public const int ChivalryStart = 201;
        public const int ChivalryCount = 10;
        public const int ChivalryEnd = 210;

        // ── Bushido ─────────────────────────────────────────────────────
        public const int BushidoStart = 401;
        public const int BushidoCount = 6;
        public const int BushidoEnd = 406;

        // ── Ninjitsu ────────────────────────────────────────────────────
        public const int NinjitsuStart = 501;
        public const int NinjitsuCount = 8;
        public const int NinjitsuEnd = 508;

        // ── Spellweaving ────────────────────────────────────────────────
        public const int SpellweavingStart = 601;
        public const int SpellweavingCount = 16;
        public const int SpellweavingEnd = 616;

        // ── Mysticism ───────────────────────────────────────────────────
        public const int MysticismStart = 678;
        public const int MysticismCount = 16;
        public const int MysticismEnd = 693;

        // ── Mastery ─────────────────────────────────────────────────────
        public const int MasteryStart = 701;
        public const int MasteryCount = 45;
        public const int MasteryEnd = 745;

        // ── Magery: Spell IDs ───────────────────────────────────────────
        // First Circle
        public const int Clumsy = 1;
        public const int CreateFood = 2;
        public const int Feeblemind = 3;
        public const int Heal = 4;
        public const int MagicArrow = 5;
        public const int NightSight = 6;
        public const int ReactiveArmor = 7;
        public const int Weaken = 8;

        // Second Circle
        public const int Agility = 9;
        public const int Cunning = 10;
        public const int Cure = 11;
        public const int Harm = 12;
        public const int MagicTrap = 13;
        public const int MagicUntrap = 14;
        public const int Protection = 15;
        public const int Strength = 16;

        // Third Circle
        public const int Bless = 17;
        public const int Fireball = 18;
        public const int MagicLock = 19;
        public const int Poison = 20;
        public const int Telekinesis = 21;
        public const int Teleport = 22;
        public const int Unlock = 23;
        public const int WallOfStone = 24;

        // Fourth Circle
        public const int ArchCure = 25;
        public const int ArchProtection = 26;
        public const int Curse = 27;
        public const int FireField = 28;
        public const int GreaterHeal = 29;
        public const int Lightning = 30;
        public const int ManaDrain = 31;
        public const int Recall = 32;

        // Fifth Circle
        public const int BladeSpirits = 33;
        public const int DispelField = 34;
        public const int Incognito = 35;
        public const int MagicReflection = 36;
        public const int MindBlast = 37;
        public const int Paralyze = 38;
        public const int PoisonField = 39;
        public const int SummonCreature = 40;

        // Sixth Circle
        public const int Dispel = 41;
        public const int EnergyBolt = 42;
        public const int Explosion = 43;
        public const int Invisibility = 44;
        public const int Mark = 45;
        public const int MassCurse = 46;
        public const int ParalyzeField = 47;
        public const int Reveal = 48;

        // Seventh Circle
        public const int ChainLightning = 49;
        public const int EnergyField = 50;
        public const int Flamestrike = 51;
        public const int GateTravel = 52;
        public const int ManaVampire = 53;
        public const int MassDispel = 54;
        public const int MeteorSwarm = 55;
        public const int Polymorph = 56;

        // Eighth Circle
        public const int Earthquake = 57;
        public const int EnergyVortex = 58;
        public const int Resurrection = 59;
        public const int AirElemental = 60;
        public const int SummonDaemon = 61;
        public const int EarthElemental = 62;
        public const int FireElemental = 63;
        public const int WaterElemental = 64;

        // ── Necromancy: Spell IDs ───────────────────────────────────────
        public const int AnimateDead = 101;
        public const int BloodOath = 102;
        public const int CorpseSkin = 103;
        public const int CurseWeapon = 104;
        public const int EvilOmen = 105;
        public const int HorrificBeast = 106;
        public const int LichForm = 107;
        public const int MindRot = 108;
        public const int PainSpike = 109;
        public const int PoisonStrike = 110;
        public const int Strangle = 111;
        public const int SummonFamiliar = 112;
        public const int VampiricEmbrace = 113;
        public const int VengefulSpirit = 114;
        public const int Wither = 115;
        public const int WraithForm = 116;
        public const int Exorcism = 117;

        // ── Chivalry: Spell IDs ─────────────────────────────────────────
        public const int CleanseByFire = 201;
        public const int CloseWounds = 202;
        public const int ConsecrateWeapon = 203;
        public const int DispelEvil = 204;
        public const int DivineFury = 205;
        public const int EnemyOfOne = 206;
        public const int HolyLight = 207;
        public const int NobleSacrifice = 208;
        public const int RemoveCurse = 209;
        public const int SacredJourney = 210;

        // ── Bushido: Spell IDs ──────────────────────────────────────────
        public const int HonorableExecution = 401;
        public const int Confidence = 402;
        public const int Evasion = 403;
        public const int CounterAttack = 404;
        public const int LightningStrike = 405;
        public const int MomentumStrike = 406;

        // ── Ninjitsu: Spell IDs ─────────────────────────────────────────
        public const int FocusAttack = 501;
        public const int DeathStrike = 502;
        public const int AnimalForm = 503;
        public const int KiAttack = 504;
        public const int SurpriseAttack = 505;
        public const int Backstab = 506;
        public const int Shadowjump = 507;
        public const int MirrorImage = 508;

        // ── Spellweaving: Spell IDs ─────────────────────────────────────
        public const int ArcaneCircle = 601;
        public const int GiftOfRenewal = 602;
        public const int ImmolatingWeapon = 603;
        public const int AttuneWeapon = 604;
        public const int Thunderstorm = 605;
        public const int NaturesFury = 606;
        public const int SummonFey = 607;
        public const int SummonFiend = 608;
        public const int ReaperForm = 609;
        public const int Wildfire = 610;
        public const int EssenceOfWind = 611;
        public const int DryadAllure = 612;
        public const int EtherealVoyage = 613;
        public const int WordOfDeath = 614;
        public const int GiftOfLife = 615;
        public const int ArcaneEmpowerment = 616;

        // ── Mysticism: Spell IDs ────────────────────────────────────────
        public const int NetherBolt = 678;
        public const int HealingStone = 679;
        public const int PurgeMagic = 680;
        public const int Enchant = 681;
        public const int Sleep = 682;
        public const int EagleStrike = 683;
        public const int AnimatedWeapon = 684;
        public const int StoneForm = 685;
        public const int SpellTrigger = 686;
        public const int MassSleep = 687;
        public const int CleansingWinds = 688;
        public const int Bombard = 689;
        public const int SpellPlague = 690;
        public const int HailStorm = 691;
        public const int NetherCyclone = 692;
        public const int RisingColossus = 693;

        // ── Mastery: Spell IDs (selected) ───────────────────────────────
        public const int Inspire = 701;
        public const int Invigorate = 702;
        public const int Resilience = 703;
        public const int Perseverance = 704;
        public const int Tribulation = 705;
        public const int Despair = 706;
        public const int DeathRay = 707;
        public const int EtherealBurst = 708;
        public const int NetherBlast = 709;
        public const int MysticWeapon = 710;
        public const int CommandUndead = 711;
        public const int Conduit = 712;
        public const int ManaShield = 713;
        public const int SummonReaper = 714;
        public const int EnchantedSummoning = 715;
        public const int AnticipateHit = 716;
        public const int Warcry = 717;
        public const int Intuition = 718;
        public const int Rejuvenate = 719;
        public const int HolyFist = 720;
        public const int Shadow = 721;
        public const int WhiteTigerForm = 722;
        public const int FlamingShot = 723;
        public const int PlayingTheOdds = 724;
        public const int Thrust = 725;
        public const int Pierce = 726;
        public const int Stagger = 727;
        public const int Toughness = 728;
        public const int Onslaught = 729;
        public const int FocusedEye = 730;
        public const int ElementalFury = 731;
        public const int CalledShot = 732;
        public const int WarriorsGifts = 733;
        public const int ShieldBash = 734;
        public const int Bodyguard = 735;
        public const int HeightenSenses = 736;
        public const int Tolerance = 737;
        public const int InjectedStrike = 738;
        public const int Potency = 739;
        public const int Rampage = 740;
        public const int FistsOfFury = 741;
        public const int Knockout = 742;
        public const int Whispering = 743;
        public const int CombatTraining = 744;
        public const int Boarding = 745;
    }
}
