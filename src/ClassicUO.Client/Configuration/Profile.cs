#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration.Json;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace ClassicUO.Configuration
{
    //[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
    [JsonSerializable(typeof(Profile), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ActionBarSlotData))]
    sealed partial class ProfileJsonContext : JsonSerializerContext
    {
        sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

            public override string ConvertName(string name)
            {
                // Conversion to other naming convention goes here. Like SnakeCase, KebabCase etc.
                return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
            }
        }

        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance;
            return options;
        });

        public static ProfileJsonContext DefaultToUse { get; } = new ProfileJsonContext(_jsonOptions.Value);
    }



    public sealed class Profile
    {
        [JsonIgnore] public string Username { get; set; }
        [JsonIgnore] public string ServerName { get; set; }
        [JsonIgnore] public string CharacterName { get; set; }

        // sounds
        public bool EnableSound { get; set; } = true;
        public int SoundVolume { get; set; } = 100;
        public bool EnableMusic { get; set; } = true;
        public int MusicVolume { get; set; } = 100;
        public bool EnableFootstepsSound { get; set; } = true;
        public bool EnableCombatMusic { get; set; } = true;
        public bool ReproduceSoundsInBackground { get; set; }

        // fonts and speech
        public byte ChatFont { get; set; } = 1;
        public int SpeechDelay { get; set; } = 100;
        public bool ScaleSpeechDelay { get; set; } = true;
        public bool SaveJournalToFile { get; set; } = false;
        public bool ForceUnicodeJournal { get; set; }
        public bool IgnoreAllianceMessages { get; set; }
        public bool IgnoreGuildMessages { get; set; }

        // hues
        public ushort SpeechHue { get; set; } = 0x02B2;
        public ushort WhisperHue { get; set; } = 0x0033;
        public ushort EmoteHue { get; set; } = 0x0021;
        public ushort YellHue { get; set; } = 0x0021;
        public ushort PartyMessageHue { get; set; } = 0x0044;
        public ushort GuildMessageHue { get; set; } = 0x0044;
        public ushort AllyMessageHue { get; set; } = 0x0057;
        public ushort ChatMessageHue { get; set; } = 0x0256;
        public ushort InnocentHue { get; set; } = 0x005A;
        public ushort PartyAuraHue { get; set; } = 0x0044;
        public ushort FriendHue { get; set; } = 0x0044;
        public ushort CriminalHue { get; set; } = 0x03B2;
        public ushort CanAttackHue { get; set; } = 0x03B2;
        public ushort EnemyHue { get; set; } = 0x0031;
        public ushort MurdererHue { get; set; } = 0x0023;
        public ushort BeneficHue { get; set; } = 0x0059;
        public ushort HarmfulHue { get; set; } = 0x0020;
        public ushort NeutralHue { get; set; } = 0x03B1;
        public bool EnabledSpellHue { get; set; }
        public bool EnabledSpellFormat { get; set; }
        public string SpellDisplayFormat { get; set; } = "{power} [{spell}]";
        public ushort PoisonHue { get; set; } = 0x0044;
        public ushort ParalyzedHue { get; set; } = 0x014C;
        public ushort InvulnerableHue { get; set; } = 0x0030;
        public ushort AltJournalBackgroundHue { get; set; } = 0x0000;
        public ushort AltGridContainerBackgroundHue { get; set; } = 0x0000;
        public bool OverridePartyAndGuildHue { get; set; } = false;

        // visual
        public bool EnabledCriminalActionQuery { get; set; } = true;
        public bool EnabledBeneficialCriminalActionQuery { get; set; } = false;
        public bool EnableStatReport { get; set; } = true;
        public bool EnableSkillReport { get; set; } = true;
        public bool UseOldStatusGump { get; set; }
        public int BackpackStyle { get; set; }
        public bool HighlightGameObjects { get; set; }
        public bool HighlightMobilesByParalize { get; set; } = true;
        public bool HighlightMobilesByPoisoned { get; set; } = true;
        public bool HighlightMobilesByInvul { get; set; } = true;
        public bool ShowMobilesHP { get; set; }
        public bool ShowTargetIndicator { get; set; }
        public bool AutoAvoidObstacules { get; set; } = true;
        public int MobileHPType { get; set; }     // 0 = %, 1 = line, 2 = both
        public int MobileHPShowWhen { get; set; } // 0 = Always, 1 - <100%
        public bool DrawRoofs { get; set; } = true;

        public bool SetTargetOut { get; set; }  = false;
        // ## BEGIN - END ## // ART / HUE CHANGES
        public bool TreeToStumps { get; set; }
        // ## BEGIN - END ## // ART / HUE CHANGES
        public bool EnableCaveBorder { get; set; }
        public bool HideVegetation { get; set; }
        public int FieldsType { get; set; } // 0 = normal, 1 = static, 2 = tile
        public bool NoColorObjectsOutOfRange { get; set; }
        public bool UseCircleOfTransparency { get; set; }
        public int CircleOfTransparencyRadius { get; set; } = Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS / 2;
        public int CircleOfTransparencyType { get; set; } // 0 = normal, 1 = like original client
        public int VendorGumpHeight { get; set; } = 60;   //original vendor gump size
        public float DefaultScale { get; set; } = 1.0f;
        public bool EnableMousewheelScaleZoom { get; set; }
        public bool SaveScaleAfterClose { get; set; }
        public bool RestoreScaleAfterUnpressCtrl { get; set; }
        public bool BandageSelfOld { get; set; } = true;
        public bool EnableDeathScreen { get; set; } = true;
        public bool EnableBlackWhiteEffect { get; set; } = true;
        public ushort HiddenBodyHue { get; set; } = 0x038E;
        public byte HiddenBodyAlpha { get; set; } = 40;
        public int PlayerConstantAlpha { get; set; } = 100;

        // tooltip
        public bool UseTooltip { get; set; } = true;
        public bool EnableTooltipOverride { get; set; } = false;
        public ushort TooltipTextHue { get; set; } = 0xFFFF;
        public int TooltipDelayBeforeDisplay { get; set; } = 250;
        public int TooltipDisplayZoom { get; set; } = 100;
        public int TooltipBackgroundOpacity { get; set; } = 70;
        public byte TooltipFont { get; set; } = 1;

        // movements
        public bool EnablePathfind { get; set; } = true;
        public bool UseShiftToPathfind { get; set; }
        public bool PathfindSingleClick { get; set; }
        public bool AlwaysRun { get; set; } = true;
        public bool AlwaysRunUnlessHidden { get; set; } = true;
        public bool SmoothMovements { get; set; } = true;
        public bool HoldDownKeyTab { get; set; } = true;
        public bool HoldShiftForContext { get; set; } = false;
        public bool HoldShiftToSplitStack { get; set; } = false;

        // general
        [JsonConverter(typeof(Point2Converter))] public Point WindowClientBounds { get; set; } = new Point(1024, 768);
        [JsonConverter(typeof(Point2Converter))] public Point ContainerDefaultPosition { get; set; } = new Point(24, 24);
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowPosition { get; set; } = new Point(10, 10);
        public bool GameWindowLock { get; set; }
        public bool GameWindowFullSize { get; set; }
        public bool WindowBorderless { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowSize { get; set; } = new Point(1024, 768);
        [JsonConverter(typeof(Point2Converter))] public Point TopbarGumpPosition { get; set; } = new Point(0, 0);
        public bool TopbarGumpIsMinimized { get; set; }
        public bool TopbarGumpIsDisabled { get; set; }
        public bool UseAlternativeLights { get; set; }
        public bool UseCustomLightLevel { get; set; }
        public byte LightLevel { get; set; }
        public int LightLevelType { get; set; } // 0 = absolute, 1 = minimum
        public bool UseColoredLights { get; set; } = true;
        public bool UseDarkNights { get; set; }
        public int CloseHealthBarType { get; set; } // 0 = none, 1 == not exists, 2 == is dead
        public bool ActivateChatAfterEnter { get; set; }
        public bool ActivateChatAdditionalButtons { get; set; } = true;
        public bool ActivateChatShiftEnterSupport { get; set; } = true;
        public bool UseObjectsFading { get; set; } = true;
        public bool HoldDownKeyAltToCloseAnchored { get; set; } = true;
        public bool CloseAllAnchoredGumpsInGroupWithRightClick { get; set; } = false;
        public bool HoldAltToMoveGumps { get; set; }
        public byte JournalOpacity { get; set; } = 50;
        public int JournalStyle { get; set; } = 0;
        public bool HideScreenshotStoredInMessage { get; set; }
        // Experimental
        public bool CastSpellsByOneClick { get; set; }
        public bool BuffBarTime { get; set; }
        public bool FastSpellsAssign { get; set; }
        public bool AutoOpenDoors { get; set; } = true;
        public bool SmoothDoors { get; set; } = true;
        public bool AutoOpenCorpses { get; set; } = true;
        public int AutoOpenCorpseRange { get; set; } = 2;
        public int CorpseOpenOptions { get; set; } = 3;
        public bool SkipEmptyCorpse { get; set; }
        public bool DisableDefaultHotkeys { get; set; }
        public bool DisableArrowBtn { get; set; }
        public bool DisableTabBtn { get; set; }
        public bool DisableCtrlQWBtn { get; set; }
        public bool DisableAutoMove { get; set; }
        public bool EnableDragSelect { get; set; }
        public int DragSelectModifierKey { get; set; } // 0 = none, 1 = control, 2 = shift, 3 = alt
        public int DragSelect_PlayersModifier { get; set; } = 0;
        public int DragSelect_MonstersModifier { get; set; } = 0;
        public int DragSelect_NameplateModifier { get; set; } = 0;
        public bool OverrideContainerLocation { get; set; }

        public int OverrideContainerLocationSetting { get; set; } // 0 = container position, 1 = top right of screen, 2 = last dragged position, 3 = remember every container

        [JsonConverter(typeof(Point2Converter))] public Point OverrideContainerLocationPosition { get; set; } = new Point(200, 200);
        public bool HueContainerGumps { get; set; } = true;
        public bool DragSelectHumanoidsOnly { get; set; }
        public int DragSelectStartX { get; set; } = 100;
        public int DragSelectStartY { get; set; } = 100;
        public bool DragSelectAsAnchor { get; set; } = false;
        // ## BEGIN - END ## // NAMEOVERHEAD
        //public NameOverheadTypeAllowed NameOverheadTypeAllowed { get; set; } = NameOverheadTypeAllowed.All;
        // ## BEGIN - END ## // NAMEOVERHEAD
        public string LastActiveNameOverheadOption { get; set; } = "All";
        // ## BEGIN - END ## // NAMEOVERHEAD
        public bool NameOverheadToggled { get; set; } = false;
        public bool ShowTargetRangeIndicator { get; set; }
        public bool PartyInviteGump { get; set; }
        // ## BEGIN - END ## // NAMEOVERHEAD
        public bool ShowHPLineInNOH { get; set; } = false;
        public bool NameOverheadPinnedToggled { get; set; } = false;
        public bool NameOverheadBackgroundToggled { get; set; } = false;
        // ## BEGIN - END ## // NAMEOVERHEAD
        // ## BEGIN - END ## // UI/GUMPS
        public bool UOClassicCombatLTBar { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point UOClassicCombatLTBarLocation { get; set; } = new Point(25, 25);
        public bool UOClassicCombatLTBar_Locked { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point BandageGumpOffset { get; set; } = new Point(0, 0);
        public bool BandageGump { get; set; }
        public bool BandageGumpUpDownToggle { get; set; } = false;
        // ## BEGIN - END ## // UI/GUMPS
        public bool CustomBarsToggled { get; set; }
        public bool CBBlackBGToggled { get; set; }

        public bool ShowInfoBar { get; set; }
        public int InfoBarHighlightType { get; set; } // 0 = text colour changes, 1 = underline

        public bool CounterBarEnabled { get; set; }
        public bool CounterBarHighlightOnUse { get; set; }
        public bool CounterBarHighlightOnAmount { get; set; }
        public bool CounterBarDisplayAbbreviatedAmount { get; set; }
        public int CounterBarAbbreviatedAmount { get; set; } = 1000;
        public int CounterBarHighlightAmount { get; set; } = 5;
        public int CounterBarCellSize { get; set; } = 40;
        public int CounterBarRows { get; set; } = 1;
        public int CounterBarColumns { get; set; } = 1;

        public bool ActionBarEnabled { get; set; }
        [JsonConverter(typeof(Point2Converter))] public Point ActionBarPosition { get; set; } = new Point(300, 400);
        public List<ActionBarSlotData> ActionBarSlots { get; set; } = new List<ActionBarSlotData>();

        public bool ShowSkillsChangedMessage { get; set; } = true;
        public int ShowSkillsChangedDeltaValue { get; set; } = 1;
        public bool ShowStatsChangedMessage { get; set; } = true;


        public bool ShadowsEnabled { get; set; } = true;
        public bool ShadowsStatics { get; set; } = true;
        public int TerrainShadowsLevel { get; set; } = 15;
        public int AuraUnderFeetType { get; set; } // 0 = NO, 1 = in warmode, 2 = ctrl+shift, 3 = always
        public bool AuraOnMouse { get; set; } = true;
        public bool AnimatedWaterEffect { get; set; } = false;

        public bool PartyAura { get; set; }

        public bool UseXBR { get; set; } = true;

        public int VisualStyle { get; set; } = 0;

        public bool HideChatGradient { get; set; } = false;

        public bool StandardSkillsGump { get; set; } = true;

        public bool ShowNewMobileNameIncoming { get; set; } = true;
        public bool ShowNewCorpseNameIncoming { get; set; } = true;

        public uint GrabBagSerial { get; set; }

        public int GridLootType { get; set; } // 0 = none, 1 = only grid, 2 = both

        public bool ReduceFPSWhenInactive { get; set; } = true;

        public bool OverrideAllFonts { get; set; }
        public bool OverrideAllFontsIsUnicode { get; set; } = true;
        public bool SallosEasyGrab { get; set; }

        public bool JournalDarkMode { get; set; }

        public byte ContainersScale { get; set; } = 100;

        public byte ContainerOpacity { get; set; } = 50;

        public bool ScaleItemsInsideContainers { get; set; }

        public bool DoubleClickToLootInsideContainers { get; set; }

        public bool UseLargeContainerGumps { get; set; } = false;

        public bool RelativeDragAndDropItems { get; set; }

        public bool HighlightContainerWhenSelected { get; set; }

        public bool ShowHouseContent { get; set; }
        public bool SaveHealthbars { get; set; }
        public bool TextFading { get; set; } = true;

        public bool UseSmoothBoatMovement { get; set; } = false;

        public bool IgnoreStaminaCheck { get; set; } = false;

        // ## BEGIN - END ## // BASICSETUP

        // ## BEGIN - END ## // ART / HUE CHANGES
        public bool ColorStealth { get; set; }
        public ushort StealthHue { get; set; } = 0x0044;
        public int StealthNeonType { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire
        public int GoldType { get; set; } = 0; // 0 = normal, 1 = skillball, 2 = prevcoin
        public bool ColorGold { get; set; }
        public ushort GoldHue { get; set; } = 0x0044;
        public bool ColorEnergyBolt { get; set; }
        public ushort EnergyBoltHue { get; set; } = 0x0044;
        public int EnergyBoltNeonType { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire
        public int EnergyBoltArtType { get; set; } = 0; // 0 = normal, 1 = explo ball, 2 = small bag ball
        public int BlockerType { get; set; } = 0; // 0 = off, 1 = stump, 2 = tile
        public bool ColorBlockerTile { get; set; }
        public ushort BlockerTileHue { get; set; } = 0x0044;
        public int TreeType { get; set; } = 0; // 0 = off, 1 = stump, 2 = tile
        public bool ColorTreeTile { get; set; }
        public ushort TreeTileHue { get; set; } = 0x0044;
        // ## BEGIN - END ## // ART / HUE CHANGES
        // ## BEGIN - END ## // VISUAL HELPERS
        public bool HighlightTileAtRange { get; set; }
        public int HighlightTileAtRangeRange { get; set; }
        public ushort HighlightTileRangeHue { get; set; } = 0x0044;
        public bool HighlightTileAtRangeSpell { get; set; }
        public int HighlightTileAtRangeRangeSpell { get; set; }
        public ushort HighlightTileRangeHueSpell { get; set; } = 0x0044;
        public int GlowingWeaponsType { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire, 5 = custom
        public ushort HighlightGlowingWeaponsTypeHue { get; set; } = 0x0044;
        public bool PreviewFields { get; set; }
        public bool PreviewTeleportTiles { get; set; }
        public ushort PreviewTeleportTilesHue { get; set; } = 0x0044;
        public bool OwnAuraByHP { get; set; }
        public int HighlightLastTargetType { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire, 5 = custom

        public int HighlighFriendsGuildType { get; set; } = 0;

        public ushort HighlightLastTargetTypeHue { get; set; } = 0x0044;

        public ushort HighlighFriendsGuildTypeHue { get; set; } = 0x0044;
        public int HighlightLastTargetTypePoison { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire, 5 = special, 6 = custom
        public ushort HighlightLastTargetTypePoisonHue { get; set; } = 0x0044;
        public int HighlightLastTargetTypePara { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire, 5 = special, 6 = custom
        public ushort HighlightLastTargetTypeParaHue { get; set; } = 0x0044;
        // ## BEGIN - END ## // VISUAL HELPERS
        // ## BEGIN - END ## // HEALTHBAR
        public bool HighlightHealthBarByState { get; set; } //## Highlights mobiles healthbars if they're poisoned or para
        public bool HighlightLastTargetHealthBarOutline { get; set; } //## Highlights last target healthbar if they're poisoned or invul
        public bool FlashingHealthbarOutlineSelf { get; set; } = false;
        public bool FlashingHealthbarOutlineParty { get; set; } = false;
        public bool FlashingHealthbarOutlineGreen { get; set; } = false;
        public bool FlashingHealthbarOutlineOrange { get; set; } = false;
        public bool FlashingHealthbarOutlineAll { get; set; } = false;
        public bool FlashingHealthbarNegativeOnly { get; set; } = false;
        public int FlashingHealthbarTreshold { get; set; } = 10;
        public bool OffscreenTargeting { get; set; } = true;
        // ## BEGIN - END ## // HEALTHBAR
        // ## BEGIN - END ## // CURSOR
        [JsonConverter(typeof(Point2Converter))] public Point SpellOnCursorOffset { get; set; } = new Point(25, 30);
        public bool SpellOnCursor { get; set; }
        public bool ColorGameCursor { get; set; }
        // ## BEGIN - END ## // CURSOR
        // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
        public bool OverheadRange { get; set; }
        // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
        // ## BEGIN - END ## // OLDHEALTHLINES
        public bool MultipleUnderlinesSelfParty { get; set; }
        public bool MultipleUnderlinesSelfPartyBigBars { get; set; }
        public int MultipleUnderlinesSelfPartyTransparency { get; set; } = 10;
        public bool UseOldHealthBars { get; set; } = false;
        // ## BEGIN - END ## // OLDHEALTHLINES
        // ## BEGIN - END ## // MISC
        public bool SpecialSetLastTargetCliloc { get; set; } = false;
        public string SpecialSetLastTargetClilocText { get; set; } = "- Target -";
        public bool BlockWoS { get; set; } = false;
        public bool BlockWoSFelOnly { get; set; } = false;
        public uint BlockWoSArt { get; set; } = 1872;
        public bool BlockWoSArtForceAoS { get; set; } = false;
        public bool BlockEnergyF { get; set; } = false;
        public bool BlockEnergyFFelOnly { get; set; } = false;
        public uint BlockEnergyFArt { get; set; } = 1872;
        public bool BlockEnergyFArtForceAoS { get; set; } = false;
        public bool BlackOutlineStatics { get; set; } = false;
        public bool ScaleMonstersEnabled { get; set; } = false;
        public Dictionary<int, int> MonsterScaleByGraphic { get; set; } = new Dictionary<int, int>();
        // ## BEGIN - END ## // MISC
        // ## BEGIN - END ## // PvM/PvP
        public bool PvM_DamageCounterOnLastTarget { get; set; } = false;
        public bool PvM_DamageCounterAsOverhead { get; set; } = false;
        public bool PvM_AggroIndicatorOnHealthBar { get; set; } = false;
        public bool PvM_CorpseFilterByNotoriety { get; set; } = false;
        public int PvM_CorpseFilterMode { get; set; } = 0;
        public bool PvM_LowHpAlertOnLastTarget { get; set; } = false;
        public bool PvM_KillCountMarkerPerSession { get; set; } = false;
        public bool PvM_LootHighlightOnCorpse { get; set; } = false;
        public bool PvP_CriminalAttackableAlert { get; set; } = false;
        public bool PvP_WarModeIndicator { get; set; } = false;
        public bool PvP_GreyCriminalTimer { get; set; } = false;
        public bool PvP_LastAttackerHighlight { get; set; } = false;
        public bool PvP_SpellRangeOnCursor { get; set; } = true;
        public bool PvP_QuickTargetEnemyList { get; set; } = false;
        public bool PvP_OptimizedMode { get; set; } = true;
        public int PvP_OptimizedRenderDistance { get; set; } = 18;
        public bool PvX_NameOverheadProfilesByContext { get; set; } = false;
        public int PvM_NameOverheadProfileFlags { get; set; } = (int)NameOverheadOptions.MobilesAndCorpses;
        public int PvP_NameOverheadProfileFlags { get; set; } = (int)(NameOverheadOptions.Criminal | NameOverheadOptions.Gray | NameOverheadOptions.Enemy | NameOverheadOptions.Murderer);
        public bool PvX_ConfigurableSoundsPerEvent { get; set; } = false;
        public int PvX_SoundCriminalAlert { get; set; } = 0;
        public bool PvX_BlockBeneficialOnEnemies { get; set; } = false;
        public bool PvX_LastTargetDirectionIndicator { get; set; } = false;
        public bool PvX_LockLastTarget { get; set; } = false;
        // ## BEGIN - END ## // PvM/PvP
        // ## BEGIN - END ## // MISC2
        public bool WireFrameView { get; set; } = false;
        public bool HueImpassableView { get; set; } = false;
        public ushort HueImpassableViewHue { get; set; } = 0x0044;
        public bool TransparentHousesEnabled { get; set; } = false;
        public int TransparentHousesZ { get; set; }
        public int TransparentHousesTransparency { get; set; }
        public bool InvisibleHousesEnabled { get; set; } = false;
        public int InvisibleHousesZ { get; set; }
        public int DontRemoveHouseBelowZ { get; set; } = 6;
        public bool DrawMobilesWithSurfaceOverhead { get; set; } = false;
        public bool IgnoreCoTEnabled { get; set; } = false;
        public bool ShowDeathOnWorldmap { get; set; } = false;

        public bool ShowMapCloseFriend { get; set; } = true;
        public bool AutoAvoidMobiles { get; set; } = false;
        // ## BEGIN - END ## // MISC2
        // ## BEGIN - END ## // MACROS
        public int LastTargetRange { get; set; }
        // ## BEGIN - END ## // MACROS
        // ## BEGIN - END ## // TEXTUREMANAGER
        public bool TextureManagerEnabled { get; set; } = false;
        public bool TextureManagerHalos { get; set; } // Halos
        public bool TextureManagerHumansOnly { get; set; } = false;
        public bool TextureManagerPurple { get; set; } = true;
        public bool TextureManagerGreen { get; set; } = true;
        public bool TextureManagerRed { get; set; } = true;
        public bool TextureManagerOrange { get; set; } = true;
        public bool TextureManagerBlue { get; set; } = true;
        public bool TextureManagerArrows { get; set; } // Arrows
        public bool TextureManagerHumansOnlyArrows { get; set; } = false;
        public bool TextureManagerPurpleArrows { get; set; } = true;
        public bool TextureManagerGreenArrows { get; set; } = true;
        public bool TextureManagerRedArrows { get; set; } = true;
        public bool TextureManagerOrangeArrows { get; set; } = true;
        public bool TextureManagerBlueArrows { get; set; } = true;
        // ## BEGIN - END ## // TEXTUREMANAGER
        // ## BEGIN - END ## // LINES
        public bool UOClassicCombatLines { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point UOClassicCombatLinesLocation { get; set; } = new Point(25, 25);
        public bool UOClassicCombatLines_ToggleLastTarget { get; set; } = false;
        public bool UOClassicCombatLines_ToggleHuntingMmode { get; set; } = false;
        public bool UOClassicCombatLines_ToggleHMBlue { get; set; } = false;
        
        // ## BEGIN - END ## // PERFORMANCE SETTINGS
        public int GraphicsQuality { get; set; } = 2; // 0 = Low, 1 = Medium, 2 = High
        public bool EnableFrustumCulling { get; set; } = true;
        public bool EnableTextureCaching { get; set; } = true;
        public bool EnableChunkPreload { get; set; } = true;
        public int MaxRenderDistance { get; set; } = 24; // Max view range
        public bool UseRenderTarget { get; set; } = true;
        public float RenderTargetScale { get; set; } = 1f;
        public bool EnableLOD { get; set; } = true;
        public int LODDistanceTiles { get; set; } = 24;
        public int ImageRenderingMode { get; set; } = 0;
        public bool OptimizeBackgroundRendering { get; set; } = true;
        public bool ReduceParticleEffects { get; set; } = false;
        public bool EnableVSync { get; set; } = false;
        public bool DisableFrameLimiting { get; set; } = false;
        public int SpriteBatchSize { get; set; } = 8192; // 0x2000
        public bool PerformanceDisableCombatLinesOverlay { get; set; } = false;
        public bool PerformanceDisableHealthLinesOverlay { get; set; } = false;
        public bool PerformanceDisableLightsRenderTarget { get; set; } = false;
        // ## BEGIN - END ## // PERFORMANCE SETTINGS
        public bool UOClassicCombatLines_ToggleHMRed { get; set; } = false;
        public bool UOClassicCombatLines_ToggleHMOrange { get; set; } = false;
        public bool UOClassicCombatLines_ToggleHMCriminal { get; set; } = false;
        // ## BEGIN - END ## // LINES
        // ## BEGIN - END ## // AUTOLOOT
        public bool UOClassicCombatAL { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point UOClassicCombatALLocation { get; set; } = new Point(25, 25);
        public bool UOClassicCombatAL_EnableAL { get; set; } = false;
        public bool UOClassicCombatAL_EnableSL { get; set; } = false;
        public bool UOClassicCombatAL_EnableALLow { get; set; } = false;
        public bool UOClassicCombatAL_EnableSLLow { get; set; } = false;
        public uint UOClassicCombatAL_LootDelay { get; set; } = 500;
        public uint UOClassicCombatAL_PurgeDelay { get; set; } = 10000;
        public uint UOClassicCombatAL_QueueSpeed { get; set; } = 100;
        public bool UOClassicCombatAL_EnableGridLootColoring { get; set; } = false;
        public bool UOClassicCombatAL_EnableLootAboveID { get; set; } = false;
        public uint UOClassicCombatAL_LootAboveID { get; set; } = 22400;
        public uint UOClassicCombatAL_SL_Gray { get; set; } = 946;
        public uint UOClassicCombatAL_SL_Blue { get; set; } = 89;
        public uint UOClassicCombatAL_SL_Green { get; set; } = 63;
        public uint UOClassicCombatAL_SL_Red { get; set; } = 34;
        // ## BEGIN - END ## // AUTOLOOT
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        public bool UOClassicCombatBuffbar { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point UOClassicCombatBuffbarLocation { get; set; } = new Point(25, 25);
        public bool UOClassicCombatBuffbar_SwingEnabled { get; set; } = false;
        public bool UOClassicCombatBuffbar_DoDEnabled { get; set; } = false;
        public bool UOClassicCombatBuffbar_GotDEnabled { get; set; } = false;
        public bool UOClassicCombatBuffbar_Locked { get; set; } = true;
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        public uint UOClassicCombatSelf_DisarmedCooldown { get; set; } = 5000;
        public uint UOClassicCombatSelf_DisarmStrikeCooldown { get; set; } = 30000;
        public uint UOClassicCombatSelf_DisarmAttemptCooldown { get; set; } = 15000;
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        // ## BEGIN - END ## // SELF
        public bool UOClassicCombatSelf { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point UOClassicCombatSelfLocation { get; set; } = new Point(25, 25);
        public bool UOClassicCombatSelf_ColoredPouches { get; set; } = false;
        public ushort UOClassicCombatSelf_ColoredPouchesColor { get; set; } = 38;
        public bool UOClassicCombatSelf_AutoEApple { get; set; } = true;
        public uint UOClassicCombatSelf_EAppleCooldown { get; set; } = 31000;
        public bool UOClassicCombatSelf_AutoBandage { get; set; } = true;
        public bool UOClassicCombatSelf_AutoPouche { get; set; } = true;
        public bool UOClassicCombatSelf_AutoCurepot { get; set; } = true;
        public bool UOClassicCombatSelf_AutoHealpot { get; set; } = true;
        public bool UOClassicCombatSelf_AutoRefreshpot { get; set; } = true;
        public uint UOClassicCombatSelf_ActionCooldown { get; set; } = 700;
        public uint UOClassicCombatSelf_PoucheCooldown { get; set; } = 0;
        public uint UOClassicCombatSelf_CurepotCooldown { get; set; } = 0;
        public uint UOClassicCombatSelf_HealpotCooldown { get; set; } = 10000;
        public uint UOClassicCombatSelf_RefreshpotCooldown { get; set; } = 0;
        public uint UOClassicCombatSelf_WaitForTarget { get; set; } = 1000;
        public bool UOClassicCombatSelf_RearmAfterPot { get; set; } = true;
        public bool UOClassicCombatSelf_IsDuelingOrTankMage { get; set; } = true;
        public bool UOClassicCombatSelf_AutoRearmAfterDisarmed { get; set; } = true;
        public uint UOClassicCombatSelf_AutoRearmAfterDisarmedCooldown { get; set; } = 5000;
        public uint UOClassicCombatSelf_BandiesHPTreshold { get; set; } = 1;
        public bool UOClassicCombatSelf_BandiesPoison { get; set; } = true;
        public uint UOClassicCombatSelf_CurepotHPTreshold { get; set; } = 10;
        public uint UOClassicCombatSelf_HealpotHPTreshold { get; set; } = 20;
        public uint UOClassicCombatSelf_RefreshpotStamTreshold { get; set; } = 15;
        public bool UOClassicCombatSelf_DisarmStrike { get; set; } = true;
        public bool UOClassicCombatSelf_ConsiderHidden { get; set; } = true;
        public bool UOClassicCombatSelf_ConsiderSpells { get; set; } = true;
        public uint UOClassicCombatSelf_StrengthPotCooldown { get; set; } = 120000;
        public uint UOClassicCombatSelf_DexPotCooldown { get; set; } = 120000;
        public int UOClassicCombatSelf_MinRNG { get; set; } = 50;
        public int UOClassicCombatSelf_MaxRNG { get; set; } = 150;
        public bool UOClassicCombatSelf_ClilocTriggers { get; set; } = false;
        public bool UOClassicCombatSelf_MacroTriggers { get; set; } = false;
        public bool UOClassicCombatSelf_ConsiderBalanced { get; set; } = true;
        // ## BEGIN - END ## // SELF
        // ## BEGIN - END ## // ADVMACROS
        public Point PullEnemyBars { get; set; } = new Point(1630, 214);
        public Point PullEnemyBarsFinalLocation { get; set; } = new Point(1790, 0); // X difference needs to be 120 to get bars next to one another
        public Point PullFriendlyBars { get; set; } = new Point(1550, 214);
        public Point PullFriendlyBarsFinalLocation { get; set; } = new Point(1670, 0); // X difference needs to be 120 to get bars next to one another
        public Point PullPartyAllyBars { get; set; } = new Point(1470, 214);
        public Point PullPartyAllyBarsFinalLocation { get; set; } = new Point(1550, 0); // X difference needs to be 120 to get bars next to one another
        public uint CustomSerial { get; set; }
        public uint Mimic_PlayerSerial { get; set; }
        // ## BEGIN - END ## // ADVMACROS
        // ## BEGIN - END ## // AUTOMATIONS
        public bool AutoWorldmapMarker { get; set; }
        public bool AutoRangeDisplayAlways { get; set; } = false;
        public bool AutoRangeDisplayActive { get; set; } = false;
        public int AutoRangeDisplayActiveRange { get; set; } = 10;
        public ushort AutoRangeDisplayHue { get; set; } = 0x0074;
        // ## BEGIN - END ## // AUTOMATIONS
        // ## BEGIN - END ## // OUTLANDS
        /*
        public bool InfernoBridge { get; set; } = true;
        public bool OverheadSummonTime { get; set; }
        public bool OverheadPeaceTime { get; set; }
        public bool MobileHamstrungTime { get; set; }
        public uint MobileHamstrungTimeCooldown { get; set; } = 3000;
        //UCC SELF
        public uint UOClassicCombatSelf_HamstringStrikeCooldown { get; set; } = 30000;
        public uint UOClassicCombatSelf_HamstringAttemptCooldown { get; set; } = 15000;
        public uint UOClassicCombatSelf_HamstrungCooldown { get; set; } = 5000;
        public bool UOClassicCombatSelf_NoRefreshPotAfterHamstrung { get; set; } = true;
        public uint UOClassicCombatSelf_NoRefreshPotAfterHamstrungCooldown { get; set; } = 5000;
        // BUFFBAR
        public bool UOClassicCombatBuffbar_DoHEnabled { get; set; } = false;
        public bool UOClassicCombatBuffbar_GotHEnabled { get; set; } = false;
        */
        // ## BEGIN - END ## // OUTLANDS
        // ## BEGIN - END ## // LOBBY
        public string LobbyIP { get; set; } = "127.0.0.1";
        public string LobbyPort { get; set; } = "2596";
        // ## BEGIN - END ## // LOBBY
        // ## BEGIN - END ## // STATUSGUMP
        public bool UseRazorEnhStatusGump { get; set; } = false;
        // ## BEGIN - END ## // STATUSGUMP
        // ## BEGIN - END ## // MODERNCOOLDOWNBAR
        public bool ModernCooldwonBar_locked { get; set; } = false;
        // ## BEGIN - END ## // MODERNCOOLDOWNBAR
        // ## BEGIN - END ## // ONCASTINGGUMP
        public bool OnCastingGump { get; set; }
        public bool OnCastingGump_hidden { get; set; } = false;
        // ## BEGIN - END ## // ONCASTINGGUMP
        // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
        public bool ShowAllLayers { get; set; }
        public bool ShowAllLayersPaperdoll { get; set; }
        public int ShowAllLayersPaperdoll_X { get; set; } = 166;
        public bool ColorPaperdollByDurability { get; set; }
        public bool UseModernDurabilityGump { get; set; } = true;
        // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
        // ## BEGIN - END ## // MISC3 THIEFSUPREME
        public bool OverrideContainerOpenRange { get; set; }
        // ## BEGIN - END ## // MISC3 THIEFSUPREME
        // ## BEGIN - END ## // VISUALRESPONSEMANAGER
        public bool VisualResponseManager { get; set; } = false;
        
        // Performance Optimizations
        public bool PerformanceOptimizations { get; set; } = true;
        public bool PerformanceFrustumCulling { get; set; } = true;
        public bool PerformanceBatchOptimization { get; set; } = true;
        public bool PerformanceLODSystem { get; set; } = true;
        public bool PerformanceTextureStreaming { get; set; } = true;
        public bool PerformanceOcclusionCulling { get; set; } = false;
        public int PerformanceQualityLevel { get; set; } = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        public bool PerformanceShowStats { get; set; } = false;
        // ## BEGIN - END ## // VISUALRESPONSEMANAGER
        // ## BEGIN - END ## // TABGRID // PKRION
        public bool TabGridGumpEnabled { get; set; } = false;
        public int GridTabs { get; set; } = 1;
        public int GridRows { get; set; } = 1;
        public string TabList { get; set; } = "tab1:tab2:tab3";
        // ## BEGIN - END ## // TABGRID // PKRION
        // ## BEGIN - END ## // BASICSETUP

        public bool ShowJournalClient { get; set; } = true;
        public bool ShowJournalObjects { get; set; } = true;
        public bool ShowJournalSystem { get; set; } = true;
        public bool ShowJournalGuildAlly { get; set; } = true;

        public int WorldMapWidth { get; set; } = 400;
        public int WorldMapHeight { get; set; } = 400;
        public int WorldMapFont { get; set; } = 3;
        public bool WorldMapFlipMap { get; set; } = true;
        public bool WorldMapTopMost { get; set; }
        public bool WorldMapFreeView { get; set; }
        public bool WorldMapShowParty { get; set; } = true;
        public int WorldMapZoomIndex { get; set; } = 4;
        public bool WorldMapShowCoordinates { get; set; } = true;
        public bool WorldMapShowMouseCoordinates { get; set; } = true;
        public bool WorldMapShowCorpse { get; set; } = true;
        public bool WorldMapShowMobiles { get; set; } = true;
        public bool WorldMapShowPlayerName { get; set; } = true;
        public bool WorldMapShowPlayerBar { get; set; } = true;
        public bool WorldMapShowGroupName { get; set; } = true;
        public bool WorldMapShowGroupBar { get; set; } = true;
        public bool WorldMapShowMarkers { get; set; } = true;
        public bool WorldMapShowMarkersNames { get; set; } = true;
        public bool WorldMapShowMultis { get; set; } = true;
        public string WorldMapHiddenMarkerFiles { get; set; } = string.Empty;
        public string WorldMapHiddenZoneFiles { get; set; } = string.Empty;
        public bool WorldMapShowGridIfZoomed { get; set; } = true;
        public bool WorldMapAllowPositionalTarget { get; set; } = true;

        [JsonConverter(typeof(Point2Converter))] public Point ResizeJournalSize { get; set; } = new Point(410, 350);
        public bool FollowingMode { get; set; } = false;
        public uint FollowingTarget { get; set; }
        public bool NamePlateHealthBar { get; set; } = true;
        public byte NamePlateOpacity { get; set; } = 75;
        public byte NamePlateHealthBarOpacity { get; set; } = 50;
        public bool NamePlateHideAtFullHealth { get; set; } = true;
        public bool NamePlateHideAtFullHealthInWarmode { get; set; } = true;
        public byte NamePlateBorderOpacity { get; set; } = 50;

        public bool LeftAlignToolTips { get; set; } = false;
        public bool ForceCenterAlignTooltipMobiles { get; set; } = false;

        public bool CorpseSingleClickLoot { get; set; } = false;

        public bool DisableSystemChat { get; set; } = false;
        public bool JournalMessagesOnlyInJournalBox { get; set; } = false;

        #region GRID CONTAINER
        public bool UseGridLayoutContainerGumps { get; set; } = true;
        public int GridContainerSearchMode { get; set; } = 1;
        public bool EnableGridContainerAnchor { get; set; } = false;
        public byte GridBorderAlpha { get; set; } = 75;
        public ushort GridBorderHue { get; set; } = 0;
        public byte GridContainersScale { get; set; } = 100;
        public bool GridContainerScaleItems { get; set; } = true;
        public bool GridEnableContPreview { get; set; } = true;
        public int Grid_BorderStyle { get; set; } = 0;
        public int Grid_DefaultColumns { get; set; } = 4;
        public int Grid_DefaultRows { get; set; } = 4;
        public bool Grid_UseContainerHue { get; set; } = false;
        public bool Grid_HideBorder { get; set; } = false;
        #endregion

        #region COOLDOWNS
        public int CoolDownX { get; set; } = 50;
        public int CoolDownY { get; set; } = 50;

        public List<ushort> Condition_Hue { get; set; } = new List<ushort>();
        public List<string> Condition_Label { get; set; } = new List<string>();
        public List<int> Condition_Duration { get; set; } = new List<int>();
        public List<string> Condition_Trigger { get; set; } = new List<string>();
        public List<int> Condition_Type { get; set; } = new List<int>();
        public List<bool> Condition_ReplaceIfExists { get; set; } = new List<bool>();
        public int CoolDownConditionCount
        {
            get
            {
                return Condition_Hue.Count;
            }
            set { }
        }
        #endregion

        #region IMPROVED BUFF BAR
        public bool UseImprovedBuffBar { get; set; } = true;
        public ushort ImprovedBuffBarHue { get; set; } = 905;
        #endregion

        #region DAMAGE NUMBER HUES
        public ushort DamageHueSelf { get; set; } = 0x0034;
        public ushort DamageHuePet { get; set; } = 0x0033;
        public ushort DamageHueAlly { get; set; } = 0x0030;
        public ushort DamageHueLastAttck { get; set; } = 0x1F;
        public ushort DamageHueOther { get; set; } = 0x0021;
        #endregion

        #region GridHighlightingProps
        public List<string> GridHighlight_Name { get; set; } = new List<string>();
        public List<ushort> GridHighlight_Hue { get; set; } = new List<ushort>();
        public List<List<string>> GridHighlight_PropNames { get; set; } = new List<List<string>>();
        public List<List<int>> GridHighlight_PropMinVal { get; set; } = new List<List<int>>();
        public bool GridHighlight_CorpseOnly { get; set; } = false;
        public int GridHightlightSize { get; set; } = 1;
        #endregion

        #region Health indicator
        public float ShowHealthIndicatorBelow { get; set; } = 0.9f;
        public bool EnableHealthIndicator { get; set; } = true;
        public int HealthIndicatorWidth { get; set; } = 10;
        #endregion

        public ushort MainWindowBackgroundHue { get; set; } = 1;

        public int MoveMultiObjectDelay { get; set; } = 1000;

        public bool SpellIcon_DisplayHotkey { get; set; } = true;
        public ushort SpellIcon_HotkeyHue { get; set; } = 1;

        public int SpellIconScale { get; set; } = 100;

        public bool EnableAlphaScrollingOnGumps { get; set; } = true;

        [JsonConverter(typeof(Point2Converter))] public Point WorldMapPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point PaperdollPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point JournalPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point StatusGumpPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridSize { get; set; } = new Point(300, 300);
        public bool WorldMapLocked { get; set; } = false;
        public bool PaperdollLocked { get; set; } = false;
        public bool JournalLocked { get; set; } = false;
        public bool StatusGumpLocked { get; set; } = false;
        public bool BackPackLocked { get; set; } = false;

        public bool DisplayPartyChatOverhead { get; set; } = true;

        [JsonConverter(typeof(Json.UOFontIndexConverter))]
        public byte SelectedJournalFont { get; set; } = 1;
        public int SelectedJournalFontSize { get; set; } = 20;

        [JsonConverter(typeof(Json.UOFontIndexConverter))]
        public byte SelectedToolTipFont { get; set; } = 1;
        public int SelectedToolTipFontSize { get; set; } = 20;

        [JsonConverter(typeof(Json.UOFontIndexConverter))]
        public byte GameWindowSideChatFont { get; set; } = 1;
        public int GameWindowSideChatFontSize { get; set; } = 20;

        [JsonConverter(typeof(Json.UOFontIndexConverter))]
        public byte OverheadChatFont { get; set; } = 1;
        public int OverheadChatFontSize { get; set; } = 20;
        public int OverheadChatWidth { get; set; } = 200;

        [JsonConverter(typeof(Json.UOFontIndexConverter))]
        public byte NamePlateFont { get; set; } = 1;
        public int NamePlateFontSize { get; set; } = 20;

        public string DefaultTTFFont { get; set; } = "Roboto-Regular";
        public int TextBorderSize { get; set; } = 1;

        public bool UseModernShopGump { get; set; } = false;

        public int MaxJournalEntries { get; set; } = 750;
        public bool HideJournalBorder { get; set; } = false;
        public bool HideJournalTimestamp { get; set; } = false;

        public int HealthLineSizeMultiplier { get; set; } = 1;

        public bool OpenHealthBarForLastAttack { get; set; } = true;
        [JsonConverter(typeof(Point2Converter))]
        public Point LastTargetHealthBarPos { get; set; } = Point.Zero;
        public ushort ToolTipBGHue { get; set; } = 0;

        public string LastVersionHistoryShown { get; set; }

        public int AdvancedSkillsGumpHeight { get; set; } = 310;

        #region ToolTip Overrides
        public List<string> ToolTipOverride_SearchText { get; set; } = new List<string>() { "Physical Res", "Fire Resist", "Cold Resist", "Poison Resist", "Energy Resist" };
        public List<string> ToolTipOverride_NewFormat { get; set; } = new List<string>() { "/c[#5f423c]Physical Resist {1}%", "/c[red]Fire Resist {1}%", "/c[blue]Cold Resist {1}%", "/c[green]Poison Resist {1}%", "/c[purple]Energy Resist {1}%" };
        public List<int> ToolTipOverride_MinVal1 { get; set; } = new List<int>() { -1, -1, -1, -1, -1 };
        public List<int> ToolTipOverride_MinVal2 { get; set; } = new List<int>() { -1, -1, -1, -1, -1 };
        public List<int> ToolTipOverride_MaxVal1 { get; set; } = new List<int>() { 100, 100, 100, 100, 100 };
        public List<int> ToolTipOverride_MaxVal2 { get; set; } = new List<int>() { 100, 100, 100, 100, 100 };
        public List<byte> ToolTipOverride_Layer { get; set; } = new List<byte>() { (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any };
        #endregion

        public string TooltipHeaderFormat { get; set; } = "/c[yellow]{0}";

        public bool DisplaySkillBarOnChange { get; set; } = true;
        public string SkillBarFormat { get; set; } = "{0}: {1} / {2}";

        public bool DisplayRadius { get; set; } = false;
        public int DisplayRadiusDistance { get; set; } = 10;
        public ushort DisplayRadiusHue { get; set; } = 22;

        public bool EnableSpellIndicators { get; set; } = false;

        public bool EnableAutoLoot { get; set; } = false;
        public bool HueCorpseAfterAutoloot { get; set; } = false;

        public static uint GumpsVersion { get; private set; }

        [JsonConverter(typeof(Point2Converter))]
        public Point InfoBarSize { get; set; } = new Point(400, 20);
        public bool InfoBarLocked { get; set; } = false;
        [JsonConverter(typeof(Json.UOFontIndexConverter))]
        public byte InfoBarFont { get; set; } = 1;
        public int InfoBarFontSize { get; set; } = 18;

        public int LastJournalTab { get; set; } = 0;
        public Dictionary<string, MessageType[]> JournalTabs { get; set; } = new Dictionary<string, MessageType[]>()
        {
            { "All", new MessageType[] {
                MessageType.Alliance, MessageType.Command, MessageType.Emote,
                MessageType.Encoded, MessageType.Focus, MessageType.Guild,
                MessageType.Label, MessageType.Limit3Spell, MessageType.Party,
                MessageType.Regular, MessageType.Spell, MessageType.System,
                MessageType.Whisper, MessageType.Yell, MessageType.ChatSystem }
            },
            { "Chat", new MessageType[] {
                MessageType.Regular,
                MessageType.Guild,
                MessageType.Alliance,
                MessageType.Emote,
                MessageType.Party,
                MessageType.Whisper,
                MessageType.Yell,
                MessageType.ChatSystem }
            },
            {
                "Guild|Party", new MessageType[] {
                    MessageType.Guild,
                    MessageType.Alliance,
                    MessageType.Party }
            },
            {
                "System", new MessageType[] {
                    MessageType.System }
            }
        };

        public bool UseLastMovedCooldownPosition { get; set; } = false;
        public bool CoolDownBarLocked { get; set; } = false;
        public bool CloseHealthBarIfAnchored { get; set; } = false;

        [JsonConverter(typeof(Point2Converter))]
        public Point SkillProgressBarPosition { get; set; } = Point.Zero;

        public bool ForceResyncOnHang { get; set; } = false;

        public bool UseOneHPBarForLastAttack { get; set; } = false;

        public bool DisableMouseInteractionOverheadText { get; set; } = false;

        public List<int> HiddenLayers { get; set; } = new List<int>();
        public bool HideLayersForSelf { get; set; } = true;

        public List<string> AutoOpenXmlGumps { get; set; } = new List<string>();

        public int ControllerMouseSensativity { get => Input.Mouse.ControllerSensativity; set => Input.Mouse.ControllerSensativity = value; }

        [JsonConverter(typeof(Point2Converter))]
        public Point PlayerOffset { get; set; } = new Point(0, 0);

        public bool UseLandTextures { get; set; } = false;

        public double PaperdollScale { get; set; } = 1f;

        public uint SOSGumpID { get; set; } = 1915258020;

        public bool JournalAnchorEnabled { get; set; } = false;
        public bool EnableGumpCloseAnimation { get; set; } = true;

        public bool EnableAutoLootProgressBar { get; set; } = true;
        public bool EnableNearbyItemGump { get; set; } = false;


        public void Save(string path, bool saveGumps = true)
        {
            Log.Trace($"Saving path:\t\t{path}");

            // Save profile settings
            ConfigurationResolver.Save(this, Path.Combine(path, "profile.json"), ProfileJsonContext.DefaultToUse);

            // Save opened gumps
            if (saveGumps)
                SaveGumps(path);

            Log.Trace("Saving done!");
        }

        private void SaveGumps(string path)
        {
            string gumpsXmlPath = Path.Combine(path, "gumps.xml");

            using (XmlTextWriter xml = new XmlTextWriter(gumpsXmlPath, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("gumps");

                UIManager.AnchorManager.Save(xml);

                LinkedList<Gump> gumps = new LinkedList<Gump>();

                foreach (Gump gump in UIManager.Gumps)
                {
                    if (!gump.IsDisposed && gump.CanBeSaved && !(gump is AnchorableGump anchored && UIManager.AnchorManager[anchored] != null))
                    {
                        gumps.AddLast(gump);
                    }
                }

                LinkedListNode<Gump> first = gumps.First;

                while (first != null)
                {
                    Gump gump = first.Value;

                    if (gump.LocalSerial != 0)
                    {
                        Item item = World.Items.Get(gump.LocalSerial);

                        if (item != null && !item.IsDestroyed && item.Opened)
                        {
                            while (SerialHelper.IsItem(item.Container))
                            {
                                item = World.Items.Get(item.Container);
                            }

                            SaveItemsGumpRecursive(item, xml, gumps);

                            if (first.List != null)
                            {
                                gumps.Remove(first);
                            }

                            first = gumps.First;

                            continue;
                        }
                    }

                    xml.WriteStartElement("gump");
                    gump.Save(xml);
                    xml.WriteEndElement();

                    if (first.List != null)
                    {
                        gumps.Remove(first);
                    }

                    first = gumps.First;
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }


            SkillsGroupManager.Save();
        }

        private static void SaveItemsGumpRecursive(Item parent, XmlTextWriter xml, LinkedList<Gump> list)
        {
            if (parent != null && !parent.IsDestroyed && parent.Opened)
            {
                SaveItemsGump(parent, xml, list);

                Item first = (Item)parent.Items;

                while (first != null)
                {
                    Item next = (Item)first.Next;

                    SaveItemsGumpRecursive(first, xml, list);

                    first = next;
                }
            }
        }

        private static void SaveItemsGump(Item item, XmlTextWriter xml, LinkedList<Gump> list)
        {
            if (item != null && !item.IsDestroyed && item.Opened)
            {
                LinkedListNode<Gump> first = list.First;

                while (first != null)
                {
                    LinkedListNode<Gump> next = first.Next;

                    if (first.Value.LocalSerial == item.Serial && !first.Value.IsDisposed)
                    {
                        xml.WriteStartElement("gump");
                        first.Value.Save(xml);
                        xml.WriteEndElement();

                        list.Remove(first);

                        break;
                    }

                    first = next;
                }
            }
        }


        public List<Gump> ReadGumps(string path)
        {
            List<Gump> gumps = new List<Gump>();

            // load skillsgroup
            SkillsGroupManager.Load();

            // load gumps
            string gumpsXmlPath = Path.Combine(path, "gumps.xml");

            if (File.Exists(gumpsXmlPath))
            {
                XmlDocument doc = new XmlDocument();

                try
                {
                    doc.Load(gumpsXmlPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());

                    return gumps;
                }

                XmlElement root = doc["gumps"];

                if (root != null)
                {
                    int pdolc = 0;

                    foreach (XmlElement xml in root.ChildNodes /*.GetElementsByTagName("gump")*/)
                    {
                        if (xml.Name != "gump")
                        {
                            continue;
                        }

                        try
                        {
                            GumpType type = (GumpType)int.Parse(xml.GetAttribute(nameof(type)));
                            int x = int.Parse(xml.GetAttribute(nameof(x)));
                            int y = int.Parse(xml.GetAttribute(nameof(y)));
                            uint serial = uint.Parse(xml.GetAttribute(nameof(serial)));

                            if (uint.TryParse(xml.GetAttribute("serverSerial"), out uint serverSerial))
                            {
                                UIManager.SavePosition(serverSerial, new Point(x, y));
                            }

                            Gump gump = null;

                            switch (type)
                            {
                                case GumpType.Buff:
                                    if (ProfileManager.CurrentProfile.UseImprovedBuffBar)
                                        gump = new ImprovedBuffGump();
                                    else
                                        gump = new BuffGump(100, 100);

                                    break;

                                case GumpType.Container:
                                    gump = new ContainerGump();

                                    break;

                                case GumpType.CounterBar:
                                    gump = new CounterBarGump();

                                    break;

                                case GumpType.HealthBar:
                                    if (CustomBarsToggled)
                                    {
                                        gump = new HealthBarGumpCustom();
                                    }
                                    else
                                    {
                                        gump = new HealthBarGump();
                                    }

                                    break;

                                case GumpType.InfoBar:
                                    gump = new InfoBarGump();

                                    break;

                                case GumpType.ActionBar:
                                    gump = new ActionBarGump();
                                    break;

                                case GumpType.Journal:
                                    gump = new ResizableJournal();
                                    //x = ProfileManager.CurrentProfile.JournalPosition.X;
                                    //y = ProfileManager.CurrentProfile.JournalPosition.Y;
                                    break;

                                case GumpType.MacroButton:
                                    gump = new MacroButtonGump();

                                    break;
                                case GumpType.MacroButtonEditor:
                                    gump = new MacroButtonEditorGump();

                                    break;

                                case GumpType.MiniMap:
                                    gump = new MiniMapGump();

                                    break;

                                case GumpType.PaperDoll:
                                    if (pdolc > 0)
                                    {
                                        break;
                                    }

                                    gump = new PaperDollGump(serial, serial == World.Player.Serial);
                                    x = ProfileManager.CurrentProfile.PaperdollPosition.X;
                                    y = ProfileManager.CurrentProfile.PaperdollPosition.Y;
                                    pdolc++;

                                    break;

                                case GumpType.SkillMenu:
                                    if (StandardSkillsGump)
                                    {
                                        gump = new StandardSkillsGump();
                                    }
                                    else
                                    {
                                        gump = new SkillGumpAdvanced();
                                    }

                                    break;

                                case GumpType.SpellBook:
                                    gump = new SpellbookGump();

                                    break;

                                case GumpType.StatusGump:
                                    gump = StatusGumpBase.AddStatusGump(0, 0);
                                    x = ProfileManager.CurrentProfile.StatusGumpPosition.X;
                                    y = ProfileManager.CurrentProfile.StatusGumpPosition.Y;
                                    break;

                                //case GumpType.TipNotice:
                                //    gump = new TipNoticeGump();
                                //    break;
                                case GumpType.AbilityButton:
                                    gump = new UseAbilityButtonGump();

                                    break;

                                case GumpType.SpellButton:
                                    gump = new UseSpellButtonGump();

                                    break;

                                case GumpType.SkillButton:
                                    gump = new SkillButtonGump();

                                    break;

                                case GumpType.RacialButton:
                                    gump = new RacialAbilityButton();

                                    break;

                                case GumpType.WorldMap:
                                    gump = new WorldMapGump();

                                    break;

                                case GumpType.Debug:
                                    gump = new DebugGump(100, 100);

                                    break;

                                case GumpType.NetStats:
                                    gump = new NetworkStatsGump(100, 100);

                                    break;

                                case GumpType.NameOverHeadHandler:
                                    NameOverHeadHandlerGump.LastPosition = new Point(x, y);
                                    // Gump gets opened by NameOverHeadManager, we just want to save the last position from profile
                                    break;
                                case GumpType.GridContainer:
                                    ushort ogContainer = ushort.Parse(xml.GetAttribute("ogContainer"));
                                    gump = new GridContainer(serial, ogContainer);
                                    if (((GridContainer)gump).IsPlayerBackpack)
                                    {
                                        x = ProfileManager.CurrentProfile.BackpackGridPosition.X;
                                        y = ProfileManager.CurrentProfile.BackpackGridPosition.Y;
                                    }
                                    break;
                                case GumpType.DurabilityGump:
                                    gump = new DurabilitysGump();
                                    break;
                                case GumpType.ScriptManager:
                                    gump = new LegionScripting.ScriptManagerGump();
                                    break;
                                case GumpType.LegionScriptStudio:
                                    gump = new LegionScripting.LegionScriptStudioGump();
                                    break;
                                case GumpType.RunningScripts:
                                    gump = new LegionScripting.RunningScriptsGump();
                                    break;
                            }

                            if (gump == null)
                            {
                                continue;
                            }

                            gump.LocalSerial = serial;
                            gump.Restore(xml);
                            gump.X = x;
                            gump.Y = y;

                            if (gump.LocalSerial != 0)
                            {
                                UIManager.SavePosition(gump.LocalSerial, new Point(x, y));
                            }

                            if (!gump.IsDisposed)
                            {
                                gumps.Add(gump);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }

                    foreach (XmlElement group in root.GetElementsByTagName("anchored_group_gump"))
                    {
                        int matrix_width = int.Parse(group.GetAttribute("matrix_w"));
                        int matrix_height = int.Parse(group.GetAttribute("matrix_h"));

                        AnchorManager.AnchorGroup ancoGroup = new AnchorManager.AnchorGroup();
                        ancoGroup.ResizeMatrix(matrix_width, matrix_height, 0, 0);

                        foreach (XmlElement xml in group.GetElementsByTagName("gump"))
                        {
                            try
                            {
                                GumpType type = (GumpType)int.Parse(xml.GetAttribute("type"));
                                int x = int.Parse(xml.GetAttribute("x"));
                                int y = int.Parse(xml.GetAttribute("y"));
                                uint serial = uint.Parse(xml.GetAttribute("serial"));

                                int matrix_x = int.Parse(xml.GetAttribute("matrix_x"));
                                int matrix_y = int.Parse(xml.GetAttribute("matrix_y"));

                                AnchorableGump gump = null;

                                switch (type)
                                {
                                    case GumpType.SpellButton:
                                        gump = new UseSpellButtonGump();

                                        break;

                                    case GumpType.SkillButton:
                                        gump = new SkillButtonGump();

                                        break;

                                    case GumpType.HealthBar:
                                        if (CustomBarsToggled)
                                        {
                                            gump = new HealthBarGumpCustom();
                                        }
                                        else
                                        {
                                            gump = new HealthBarGump();
                                        }

                                        break;

                                    case GumpType.AbilityButton:
                                        gump = new UseAbilityButtonGump();

                                        break;

                                    case GumpType.MacroButton:
                                        gump = new MacroButtonGump();

                                        break;
                                    case GumpType.GridContainer:
                                        ushort ogContainer = ushort.Parse(xml.GetAttribute("ogContainer"));
                                        gump = new GridContainer(serial, ogContainer);
                                        break;
                                    case GumpType.Journal:
                                        gump = new ResizableJournal();
                                        break;
                                    case GumpType.WorldMap:
                                        gump = new WorldMapGump();
                                        break;
                                    case GumpType.InfoBar:
                                        gump = new InfoBarGump();
                                        break;
                                    case GumpType.ActionBar:
                                        gump = new ActionBarGump();
                                        break;
                                    case GumpType.PaperDoll:
                                        break;
                                }

                                if (gump != null)
                                {
                                    gump.LocalSerial = serial;
                                    gump.Restore(xml);
                                    gump.X = x;
                                    gump.Y = y;

                                    if (!gump.IsDisposed)
                                    {
                                        if (UIManager.AnchorManager[gump] == null && ancoGroup.IsEmptyDirection(matrix_x, matrix_y))
                                        {
                                            gumps.Add(gump);
                                            UIManager.AnchorManager[gump] = ancoGroup;
                                            ancoGroup.AddControlToMatrix(matrix_x, matrix_y, gump);
                                        }
                                        else
                                        {
                                            gump.Dispose();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        }
                    }
                }
            }

            return gumps;
        }
    }
}