// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    // ECS per-character profile. Read by gump plugins via Res<Profile>. Field
    // names + defaults mirror the legacy Profile (src/ClassicUO.Client/
    // Configuration/Profile.cs) so a future profile.json load can deserialize
    // straight in. Until that load is wired, every field starts at the legacy
    // default. Settings are exposed through OptionsGumpPlugin's catalog;
    // string/position fields are carried for (de)serialization parity only.
    //
    // NOTE: UseUOPGumps is intentionally absent — in the legacy client it is a
    // property of the Gumps asset loader (FileManager.Gumps.UseUOPGumps), not a
    // profile flag. Status-gump layout reads it from the asset side, not here.
    //
    // NOTE: legacy NameOverheadTypeAllowed is an enum in Game/Managers — kept
    // as int here (System.Text.Json writes enums as numbers in metadata mode).
    internal sealed class Profile
    {
        // --- sounds ---
        public bool EnableSound { get; set; } = true;
        public int SoundVolume { get; set; } = 100;
        public bool EnableMusic { get; set; } = true;
        public int MusicVolume { get; set; } = 100;
        public bool EnableFootstepsSound { get; set; } = true;
        public bool EnableCombatMusic { get; set; } = true;
        public bool ReproduceSoundsInBackground { get; set; }

        // --- fonts and speech ---
        public byte ChatFont { get; set; } = 1;
        public int SpeechDelay { get; set; } = 100;
        public bool ScaleSpeechDelay { get; set; } = true;
        public bool SaveJournalToFile { get; set; } = true;
        public bool ForceUnicodeJournal { get; set; }
        public bool IgnoreAllianceMessages { get; set; }
        public bool IgnoreGuildMessages { get; set; }
        public bool OverrideAllFonts { get; set; }
        public bool OverrideAllFontsIsUnicode { get; set; } = true;

        // --- speech / notoriety hues ---
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

        // --- visual / queries ---
        public bool EnabledCriminalActionQuery { get; set; } = true;
        public bool EnabledBeneficialCriminalActionQuery { get; set; }
        public bool EnableStatReport { get; set; } = true;
        public bool EnableSkillReport { get; set; } = true;
        public bool UseOldStatusGump { get; set; }
        public bool StatusGumpBarMutuallyExclusive { get; set; } = true;
        public int BackpackStyle { get; set; }                            // 0=default, 1=suede, 2=polar bear, 3=ghoul skin
        public bool HighlightGameObjects { get; set; }
        public bool HighlightMobilesByParalize { get; set; } = true;
        public bool HighlightMobilesByPoisoned { get; set; } = true;
        public bool HighlightMobilesByInvul { get; set; } = true;
        public bool ShowMobilesHP { get; set; }
        public int MobileHPType { get; set; }                             // 0 = %, 1 = line, 2 = both
        public int MobileHPShowWhen { get; set; }                         // 0 = always, 1 = <100%, 2 = smart
        // OSI shards normalize other mobiles' hits to a fixed scale and only
        // push updates on change — polling (close status + re-request, 500ms)
        // keeps the overheads fresh there. Off for ModernUO/RunUO-likes.
        public bool PollMobileStatus { get; set; }
        public bool DrawRoofs { get; set; } = true;
        public bool TreeToStumps { get; set; }
        public bool EnableCaveBorder { get; set; }
        public bool HideVegetation { get; set; }
        public int FieldsType { get; set; }                               // 0 = normal, 1 = static, 2 = tile
        public bool NoColorObjectsOutOfRange { get; set; }
        public bool UseCircleOfTransparency { get; set; }
        public int CircleOfTransparencyRadius { get; set; } = 100;        // legacy: MAX_CIRCLE_OF_TRANSPARENCY_RADIUS / 2
        public int CircleOfTransparencyType { get; set; }                 // 0 = full, 1 = gradient
        public int VendorGumpHeight { get; set; } = 60;
        public float DefaultScale { get; set; } = 1.0f;
        public bool EnableMousewheelScaleZoom { get; set; }
        public bool SaveScaleAfterClose { get; set; }
        public bool RestoreScaleAfterUnpressCtrl { get; set; }
        public bool BandageSelfOld { get; set; } = true;
        public bool EnableDeathScreen { get; set; } = true;
        public bool EnableBlackWhiteEffect { get; set; } = true;

        // --- tooltip ---
        public bool UseTooltip { get; set; } = true;
        public ushort TooltipTextHue { get; set; } = 0xFFFF;
        public int TooltipDelayBeforeDisplay { get; set; } = 250;
        public int TooltipDisplayZoom { get; set; } = 100;
        public int TooltipBackgroundOpacity { get; set; } = 70;
        public byte TooltipFont { get; set; } = 1;

        // --- movement ---
        public bool EnablePathfind { get; set; }
        public bool UseShiftToPathfind { get; set; }
        public bool AlwaysRun { get; set; }
        public bool AlwaysRunUnlessHidden { get; set; }
        public bool FastRotation { get; set; }
        public bool SmoothMovements { get; set; } = true;
        public bool HoldDownKeyTab { get; set; } = true;
        public bool HoldShiftForContext { get; set; }
        public bool HoldShiftToSplitStack { get; set; }

        // --- window geometry / general (positions carried for parity, not settings) ---
        public Point WindowClientBounds { get; set; } = new Point(600, 480);
        public Point ContainerDefaultPosition { get; set; } = new Point(24, 24);
        public Point GameWindowPosition { get; set; } = new Point(10, 10);
        public bool GameWindowLock { get; set; }
        public bool GameWindowFullSize { get; set; }
        public bool WindowBorderless { get; set; }
        public Point GameWindowSize { get; set; } = new Point(600, 480);
        public Point TopbarGumpPosition { get; set; } = new Point(0, 0);
        public bool TopbarGumpIsMinimized { get; set; }
        public bool TopbarGumpIsDisabled { get; set; }
        public bool UseAlternativeLights { get; set; }
        public bool UseCustomLightLevel { get; set; }
        public byte LightLevel { get; set; }
        public int LightLevelType { get; set; }                           // 0 = absolute, 1 = minimum
        public bool UseColoredLights { get; set; } = true;
        public bool UseDarkNights { get; set; }
        public int CloseHealthBarType { get; set; }                       // 0 = none, 1 = not exists, 2 = is dead
        public bool ActivateChatAfterEnter { get; set; }
        public bool ActivateChatAdditionalButtons { get; set; } = true;
        public bool ActivateChatShiftEnterSupport { get; set; } = true;
        public bool UseObjectsFading { get; set; } = true;
        public bool HoldDownKeyAltToCloseAnchored { get; set; } = true;
        public bool CloseAllAnchoredGumpsInGroupWithRightClick { get; set; }
        public bool HoldAltToMoveGumps { get; set; }
        public bool HideScreenshotStoredInMessage { get; set; }

        // --- macros / spells ---
        public bool CastSpellsByOneClick { get; set; }
        public bool BuffBarTime { get; set; }
        public bool FastSpellsAssign { get; set; }

        // --- doors / corpses ---
        public bool AutoOpenDoors { get; set; }
        public bool SmoothDoors { get; set; }
        public bool AutoOpenCorpses { get; set; }
        public int AutoOpenCorpseRange { get; set; } = 2;
        public int CorpseOpenOptions { get; set; } = 3;                   // 0=none, 1=not targeting, 2=not hiding, 3=both
        public bool SkipEmptyCorpse { get; set; }

        // --- hotkeys ---
        public bool DisableDefaultHotkeys { get; set; }
        public bool DisableArrowBtn { get; set; }
        public bool DisableTabBtn { get; set; }
        public bool DisableCtrlQWBtn { get; set; }
        public bool DisableAutoMove { get; set; }

        // --- drag select ---
        public bool EnableDragSelect { get; set; }
        public int DragSelectModifierKey { get; set; }                    // 0 = none, 1 = ctrl, 2 = shift
        public bool DragSelectHumanoidsOnly { get; set; }
        public bool DragSelectHostileOnly { get; set; }
        public int DragSelectStartX { get; set; } = 100;
        public int DragSelectStartY { get; set; } = 100;
        public bool DragSelectAsAnchor { get; set; }

        // --- containers ---
        public bool OverrideContainerLocation { get; set; }
        public int OverrideContainerLocationSetting { get; set; }         // 0=near, 1=top right, 2=last dragged, 3=remember each
        public Point OverrideContainerLocationPosition { get; set; } = new Point(200, 200);
        public bool HueContainerGumps { get; set; } = true;
        public byte ContainersScale { get; set; } = 100;
        public bool ScaleItemsInsideContainers { get; set; }
        public bool DoubleClickToLootInsideContainers { get; set; }
        public bool UseLargeContainerGumps { get; set; }
        public bool UseGridContainers { get; set; }
        public bool RelativeDragAndDropItems { get; set; }
        public bool HighlightContainerWhenSelected { get; set; }

        // --- name overhead ---
        public int NameOverheadTypeAllowed { get; set; } = 0;             // legacy enum NameOverheadTypeAllowed.All
        public bool NameOverheadToggled { get; set; }
        public bool NameOverheadShowGump { get; set; } = true;
        public bool NameOverheadShowHpBar { get; set; } = true;
        public bool ShowTargetRangeIndicator { get; set; }

        // --- status / health bars ---
        public bool PartyInviteGump { get; set; } = true;
        public bool CustomBarsToggled { get; set; }
        public bool CBBlackBGToggled { get; set; }
        public bool SaveHealthbars { get; set; }

        // --- info bar ---
        public bool ShowInfoBar { get; set; }
        public int InfoBarHighlightType { get; set; }                     // 0 = text colour, 1 = bars

        // --- counter bar ---
        public bool CounterBarEnabled { get; set; }
        public bool CounterBarHighlightOnChange { get; set; } = true;
        public bool CounterBarHighlightOnAmount { get; set; }
        public bool CounterBarDisplayAbbreviatedAmount { get; set; }
        public int CounterBarAbbreviatedAmount { get; set; } = 1000;
        public int CounterBarHighlightAmount { get; set; } = 5;
        public int CounterBarCellSize { get; set; } = 40;

        // --- messages ---
        public bool ShowSkillsChangedMessage { get; set; } = true;
        public int ShowSkillsChangedDeltaValue { get; set; } = 1;
        public bool ShowStatsChangedMessage { get; set; } = true;

        // --- shadows / aura / effects ---
        public bool ShadowsEnabled { get; set; } = true;
        public bool ShadowsStatics { get; set; } = true;
        public int TerrainShadowsLevel { get; set; } = 15;
        public int AuraUnderFeetType { get; set; }                        // 0 = no, 1 = warmode, 2 = ctrl+shift, 3 = always
        public bool AuraOnMouse { get; set; } = true;
        public bool AnimatedWaterEffect { get; set; }
        public bool PartyAura { get; set; }
        public bool UseXBR { get; set; } = true;

        // --- chat / journal ---
        public bool HideChatGradient { get; set; }
        public bool StandardSkillsGump { get; set; } = true;
        public bool ShowNewMobileNameIncoming { get; set; } = true;
        public bool ShowNewCorpseNameIncoming { get; set; } = true;
        public bool JournalDarkMode { get; set; }
        public bool ShowJournalClient { get; set; } = true;
        public bool ShowJournalObjects { get; set; } = true;
        public bool ShowJournalSystem { get; set; } = true;
        public bool ShowJournalGuildAlly { get; set; } = true;
        public bool UseAlternateJournal { get; set; }
        public bool OverheadPartyMessages { get; set; }

        // --- misc gameplay ---
        public uint GrabBagSerial { get; set; }
        public int GridLootType { get; set; }                             // 0 = none, 1 = grid only, 2 = both
        public bool ReduceFPSWhenInactive { get; set; } = true;
        public bool SallosEasyGrab { get; set; }
        public bool UseNewTargetSystem { get; set; } = true;
        public bool UseKrEquipUnequipPacket { get; set; }
        public bool ShowHouseContent { get; set; }
        public bool TextFading { get; set; } = true;
        public bool UseSmoothBoatMovement { get; set; }
        public bool IgnoreStaminaCheck { get; set; }
        public bool ShowDPSWithDamageNumbers { get; set; } = true;

        // --- world map ---
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
        public bool WorldMapShowSextantCoordinates { get; set; }
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
        public bool WorldMapAllowPositionalTarget { get; set; }
    }
}
