using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Configuration
{
    public class Language
    {
        public ModernOptionsGumpLanguage GetModernOptionsGumpLanguage { get; set; } = new ModernOptionsGumpLanguage();
        public ErrorsLanguage ErrorsLanguage { get; set; } = new ErrorsLanguage();
        public MapLanguage MapLanguage { get; set; } = new MapLanguage();
        public TopBarGumpLanguage TopBarGump { get; set; } = new TopBarGumpLanguage();

        public string TazuoVersionHistory { get; set; } = "TazUO Version History";
        public string CurrentVersion { get; set; } = "Current Version: ";
        public string TazUOWiki { get; set; } = "TazUO Wiki";
        public string TazUODiscord { get; set; } = "TazUO Discord";
        public string CommandGump { get; set; } = "Available Client Commands";

        [JsonIgnore]
        public static Language Instance { get; private set; } = new Language();

        public static void Load()
        {
            if (File.Exists(languageFilePath))
            {
                Language f = JsonSerializer.Deserialize<Language>(File.ReadAllText(languageFilePath));
                Instance = f;
                Save(); //To update language file with new additions as needed
            }
            else
            {
                CreateNewLanguageFile();
            }
        }

        private static void CreateNewLanguageFile()
        {
            Directory.CreateDirectory(Path.Combine(CUOEnviroment.ExecutablePath, "Data"));

            string defaultLanguage = JsonSerializer.Serialize<Language>(Instance, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(languageFilePath, defaultLanguage);
        }

        private static void Save()
        {
            string language = JsonSerializer.Serialize<Language>(Instance, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(languageFilePath, language);
        }

        private static string languageFilePath { get { return Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Language.json"); } }
    }

    public class ModernOptionsGumpLanguage
    {
        public string OptionsTitle { get; set; } = "Options";
        public string Search { get; set; } = "Search";

        public string ButtonGeneral { get; set; } = "General";
        public string ButtonSound { get; set; } = "Sound";
        public string ButtonVideo { get; set; } = "Video";
        public string ButtonMacros { get; set; } = "Macros";
        public string ButtonTooltips { get; set; } = "Tooltips";
        public string ButtonSpeech { get; set; } = "Speech";
        public string ButtonCombatSpells { get; set; } = "Combat & Spells";
        public string ButtonCounters { get; set; } = "Counters";
        public string ButtonInfobar { get; set; } = "Infobar";
        public string ButtonContainers { get; set; } = "Containers";
        public string ButtonExperimental { get; set; } = "Experimental";
        public string ButtonIgnoreList { get; set; } = "Ignore List";
        public string ButtonNameplates { get; set; } = "Nameplate Options";
        public string ButtonCooldowns { get; set; } = "Cooldown bars";
        public string ButtonTazUO { get; set; } = "TazUO Specific";
        public string ButtonMobiles { get; set; } = "Mobiles";
        public string ButtonGumpContext { get; set; } = "Gumps & Context";
        public string ButtonMisc { get; set; } = "Misc";
        public string ButtonTerrainStatics { get; set; } = "Terrain & Statics";
        public string ButtonGameWindow { get; set; } = "Game window";
        public string ButtonZoom { get; set; } = "Zoom";
        public string ButtonLighting { get; set; } = "Lighting";
        public string ButtonShadows { get; set; } = "Shadows";

        public General GetGeneral { get; set; } = new General();
        public Video GetVideo { get; set; } = new Video();
        public Sound GetSound { get; set; } = new Sound();
        public Macros GetMacros { get; set; } = new Macros();
        public ToolTips GetToolTips { get; set; } = new ToolTips();
        public Speech GetSpeech { get; set; } = new Speech();
        public CombatSpells GetCombatSpells { get; set; } = new CombatSpells();
        public Counters GetCounters { get; set; } = new Counters();
        public InfoBars GetInfoBars { get; set; } = new InfoBars();
        public Containers GetContainers { get; set; } = new Containers();
        public Experimental GetExperimental { get; set; } = new Experimental();
        public NamePlates GetNamePlates { get; set; } = new NamePlates();
        public Cooldowns GetCooldowns { get; set; } = new Cooldowns();
        public TazUO GetTazUO { get; set; } = new TazUO();

        public class General
        {
            public string SharedNone { get; set; } = "None";
            public string SharedShift { get; set; } = "Shift";
            public string SharedCtrl { get; set; } = "Ctrl";
            public string SharedAlt { get; set; } = "Alt";

            #region General->General
            public string HighlightObjects { get; set; } = "Highlight objects under cursor";
            public string Pathfinding { get; set; } = "Enable pathfinding";
            public string ShiftPathfinding { get; set; } = "Use shift for pathfinding";
            public string SingleClickPathfind { get; set; } = "Single click for pathfinding";
            public string AlwaysRun { get; set; } = "Always run";
            public string RunUnlessHidden { get; set; } = "Unless hidden";
            public string AutoOpenDoors { get; set; } = "Automatically open doors";
            public string AutoOpenPathfinding { get; set; } = "Open doors while pathfinding";
            public string AutoOpenCorpse { get; set; } = "Automatically open corpses";
            public string CorpseOpenDistance { get; set; } = "Corpse open distance";
            public string CorpseSkipEmpty { get; set; } = "Skip empty corpses";
            public string CorpseOpenOptions { get; set; } = "Corpse open options";
            public string CorpseOptNone { get; set; } = "None";
            public string CorpseOptNotTarg { get; set; } = "Not targeting";
            public string CorpseOptNotHiding { get; set; } = "Not hiding";
            public string CorpseOptBoth { get; set; } = "Both";
            public string OutRangeColor { get; set; } = "No color for out of range objects";
            public string SallosEasyGrab { get; set; } = "Enable sallos easy grab";
            public string SallosTooltip { get; set; } = "Sallos easy grab is not recommended with grid containers enabled.";
            public string ShowHouseContent { get; set; } = "Show house content";
            public string SmoothBoat { get; set; } = "Smooth boat movements";
            #endregion

            #region General->Mobiles
            public string ShowMobileHP { get; set; } = "Show mobile's HP";
            public string ShowTargetIndicator { get; set; } = "Show Target Indicator";
            public string MobileHPType { get; set; } = "Type";
            public string HPTypePerc { get; set; } = "Percentage";
            public string HPTypeBar { get; set; } = "Bar";
            public string HPTypeNBoth { get; set; } = "Both";
            public string HPShowWhen { get; set; } = "Show when";
            public string HPShowWhen_Always { get; set; } = "Always";
            public string HPShowWhen_Less100 { get; set; } = "Less than 100%";
            public string HPShowWhen_Smart { get; set; } = "Smart";
            public string HighlightPoisoned { get; set; } = "Highlight poisoned mobiles";
            public string PoisonHighlightColor { get; set; } = "Highlight color";
            public string HighlightPara { get; set; } = "Highlight paralyzed mobiles";
            public string ParaHighlightColor { get; set; } = "Highlight color";
            public string HighlightInvul { get; set; } = "Highlight invulnerable mobiles";
            public string InvulHighlightColor { get; set; } = "Highlight color";
            public string IncomingMobiles { get; set; } = "Show incoming mobile names";
            public string IncomingCorpses { get; set; } = "Show incoming corpse names";
            public string AuraUnderFeet { get; set; } = "Show aura under feet";
            public string AuraOptDisabled { get; set; } = "Disabled";
            public string AuroOptWarmode { get; set; } = "Warmode";
            public string AuraOptCtrlShift { get; set; } = "Ctrl + Shift";
            public string AuraOptAlways { get; set; } = "Always";
            public string AuraForParty { get; set; } = "Use a custom color for party members";
            public string AuraPartyColor { get; set; } = "Party aura color";
            #endregion

            #region General->Gumps
            public string DisableTopMenu { get; set; } = "Disable top menu bar";
            public string AltForAnchorsGumps { get; set; } = "Require alt to close anchored gumps";
            public string AltToMoveGumps { get; set; } = "Require alt to move gumps";
            public string CloseEntireAnchorWithRClick { get; set; } = "Close entire group of anchored gumps with right click";
            public string OriginalSkillsGump { get; set; } = "Use original skills gump";
            public string OldStatusGump { get; set; } = "Use old status gump";
            public string PartyInviteGump { get; set; } = "Show party invite gump";
            public string ModernHealthBars { get; set; } = "Use modern health bar gumps";
            public string ModernHPBlackBG { get; set; } = "Use black background";
            public string SaveHPBars { get; set; } = "Save health bars on logout";
            public string CloseHPGumpsWhen { get; set; } = "Close health bars when";
            public string CloseHPOptDisable { get; set; } = "Disabled";
            public string CloseHPOptOOR { get; set; } = "Out of range";
            public string CloseHPOptDead { get; set; } = "Dead";
            public string CloseHPOptBoth { get; set; } = "Both";
            public string GridLoot { get; set; } = "Grid Loot";
            public string GridLootOptDisable { get; set; } = "Disabled";
            public string GridLootOptOnly { get; set; } = "Grid loot only";
            public string GridLootOptBoth { get; set; } = "Grid loot and normal container";
            public string GridLootTooltip { get; set; } = "This is not the same as Grid Containers, this is a simple grid gump used for looting corpses.";
            public string ShiftContext { get; set; } = "Require shift to open context menus";
            public string ShiftSplit { get; set; } = "Require shift to split stacks of items";

            #endregion

            #region General->Misc
            public string EnableCOT { get; set; } = "Enable circle of transparency";
            public string COTDistance { get; set; } = "Distance";
            public string COTType { get; set; } = "Type";
            public string COTTypeOptFull { get; set; } = "Full";
            public string COTTypeOptGrad { get; set; } = "Gradient";
            public string COTTypeOptModern { get; set; } = "Modern";
            public string HideScreenshotMessage { get; set; } = "Hide 'screenshot stored in' message";
            public string ObjFade { get; set; } = "Enable object fading";
            public string TextFade { get; set; } = "Enable text fading";
            public string CursorRange { get; set; } = "Show target range indicator";
            public string DragSelectHP { get; set; } = "Enable drag select for health bars";
            public string DragKeyMod { get; set; } = "Key modifier";
            public string DragPlayersOnly { get; set; } = "Players only";
            public string DragMobsOnly { get; set; } = "Monsters only";
            public string DragNameplatesOnly { get; set; } = "Visible nameplates only";
            public string DragX { get; set; } = "X Position of healthbars";
            public string DragY { get; set; } = "Y Position of healthbars";
            public string DragAnchored { get; set; } = "Anchor opened health bars together";
            public string ShowStatsChangedMsg { get; set; } = "Show stats changed messages";
            public string ShowSkillsChangedMsg { get; set; } = "Show skills changed messages";
            public string ChangeVolume { get; set; } = "Changed by";
            #endregion

            #region General->TerrainStatics
            public string HideRoof { get; set; } = "Hide roof tiles";
            public string TreesToStump { get; set; } = "Change trees to stumps";
            public string HideVegetation { get; set; } = "Hide vegetation";
            public string MagicFieldType { get; set; } = "Field types";
            public string MagicFieldOpt_Normal { get; set; } = "Normal";
            public string MagicFieldOpt_Static { get; set; } = "Static";
            public string MagicFieldOpt_Tile { get; set; } = "Tile";
            #endregion
        }

        public class Sound
        {
            public string SharedVolume { get; set; } = "Volume";

            public string EnableSound { get; set; } = "Enable sound";
            public string EnableMusic { get; set; } = "Enable music";
            public string LoginMusic { get; set; } = "Enable login page music";
            public string PlayFootsteps { get; set; } = "Play footsteps";
            public string CombatMusic { get; set; } = "Combat music";
            public string BackgroundMusic { get; set; } = "Play sound when UO is not in focus";
        }

        public class Video
        {
            #region GameWindow
            public string FPSCap { get; set; } = "FPS Cap";
            public string BackgroundFPS { get; set; } = "Reduce FPS when game is not in focus";
            public string FullsizeViewport { get; set; } = "Always use fullsize game world viewport";
            public string FullScreen { get; set; } = "Fullscreen window";
            public string LockViewport { get; set; } = "Lock game world viewport position/size";
            public string ViewportX { get; set; } = "Viewport position X";
            public string ViewportY { get; set; } = "Viewport position Y";
            public string ViewportW { get; set; } = "Viewport width";
            public string ViewportH { get; set; } = "Viewport height";
            #endregion

            #region Zoom
            public string DefaultZoom { get; set; } = "Default zoom";
            public string ZoomWheel { get; set; } = "Enable zooming with ctrl + mousewheel";
            public string ReturnDefaultZoom { get; set; } = "Return to default zoom after ctrl is released";
            #endregion

            #region Lighting
            public string AltLights { get; set; } = "Alternative lights";
            public string CustomLLevel { get; set; } = "Custom light level";
            public string Level { get; set; } = "Light level";
            public string LightType { get; set; } = "Light level type";
            public string LightType_Absolute { get; set; } = "Absolute";
            public string LightType_Minimum { get; set; } = "Minimum";
            public string DarkNight { get; set; } = "Dark nights";
            public string ColoredLight { get; set; } = "Colored lighting";
            #endregion

            #region Misc
            public string EnableDeathScreen { get; set; } = "Enable death screen";
            public string BWDead { get; set; } = "Black and white mode while dead";
            public string MouseThread { get; set; } = "Run mouse in seperate thread";
            public string TargetAura { get; set; } = "Aura on mouse target";
            public string AnimWater { get; set; } = "Animated water effect";
            #endregion

            #region Shadows
            public string EnableShadows { get; set; } = "Enable shadows";
            public string RockTreeShadows { get; set; } = "Rock and tree shadows";
            public string TerrainShadowLevel { get; set; } = "Terrain shadow level";
            #endregion
        }

        public class Macros
        {
            public string NewMacro { get; set; } = "New Macro";
            public string DelMacro { get; set; } = "Delete Macro";
        }

        public class ToolTips
        {
            public string EnableToolTips { get; set; } = "Enable tooltips";
            public string ToolTipDelay { get; set; } = "Tooltip delay";
            public string ToolTipBG { get; set; } = "Tooltip background opacity";
            public string ToolTipFont { get; set; } = "Default tooltip font color";
        }

        public class Speech
        {
            public string ScaleSpeechDelay { get; set; } = "Scale speech delay";
            public string SpeechDelay { get; set; } = "Delay";
            public string SaveJournalE { get; set; } = "Save journal entries to file";
            public string ChatEnterActivation { get; set; } = "Activate chat by pressing Enter";
            public string ChatEnterSpecial { get; set; } = "Also activate with common keys( ! ; : / \\ \\ , . [ | ~ )";
            public string ShiftEnterChat { get; set; } = "Use Shift + Enter to send message without closing chat";
            public string ChatGradient { get; set; } = "Hide chat gradient";
            public string HideGuildChat { get; set; } = "Hide guild chat";
            public string HideAllianceChat { get; set; } = "Hide alliance chat";
            public string SpeechColor { get; set; } = "Speech color";
            public string YellColor { get; set; } = "Yell color";
            public string PartyColor { get; set; } = "Party color";
            public string AllianceColor { get; set; } = "Alliance color";
            public string EmoteColor { get; set; } = "Emote color";
            public string WhisperColor { get; set; } = "Whisper color";
            public string GuildColor { get; set; } = "Guild color";
            public string CharColor { get; set; } = "Chat color";
        }

        public class CombatSpells
        {
            public string HoldTabForCombat { get; set; } = "Hold tab for combat";
            public string QueryBeforeAttack { get; set; } = "Query before attack";
            public string QueryBeforeBeneficial { get; set; } = "Query before beneficial acts on murderers/criminals/gray";
            public string EnableOverheadSpellFormat { get; set; } = "Enable overhead spell format";
            public string EnableOverheadSpellHue { get; set; } = "Enable overhead spell hue";
            public string SingleClickForSpellIcons { get; set; } = "Single click for spell icons";
            public string ShowBuffDurationOnOldStyleBuffBar { get; set; } = "Show buff duration on old style buff bar";
            public string EnableFastSpellHotkeyAssigning { get; set; } = "Enable fast spell hotkey assigning";
            public string TooltipFastSpellAssign { get; set; } = "Ctrl + Alt + Click a spell icon the open a gump to set a hotkey";
            public string InnocentColor { get; set; } = "Innocent color";
            public string BeneficialSpell { get; set; } = "Beneficial spell";
            public string FriendColor { get; set; } = "Friend color";
            public string HarmfulSpell { get; set; } = "Harmful spell";
            public string Criminal { get; set; } = "Criminal";
            public string NeutralSpell { get; set; } = "Neutral spell";
            public string CanBeAttackedHue { get; set; } = "Can be attacked hue";
            public string Murderer { get; set; } = "Murderer";
            public string Enemy { get; set; } = "Enemy";
            public string SpellOverheadFormat { get; set; } = "Spell overhead format";
            public string TooltipSpellFormat { get; set; } = "{power} for powerword, {spell} for spell name";
        }

        public class Counters
        {
            public string EnableCounters { get; set; } = "Enable counters";
            public string HighlightItemsOnUse { get; set; } = "Highlight items on use";
            public string AbbreviatedValues { get; set; } = "Abbreviated values";
            public string AbbreviateIfAmountExceeds { get; set; } = "Abbreviate if amount exceeds";
            public string HighlightRedWhenAmountIsLow { get; set; } = "Highlight red when amount is low";
            public string HighlightRedIfAmountIsBelow { get; set; } = "Highlight red if amount is below";
            public string CounterLayout { get; set; } = "Counter layout";
            public string GridSize { get; set; } = "Grid size";
            public string Rows { get; set; } = "Rows";
            public string Columns { get; set; } = "Columns";
        }

        public class InfoBars
        {
            public string ShowInfoBar { get; set; } = "Show info bar";
            public string HighlightType { get; set; } = "Highlight type";
            public string HighLightOpt_TextColor { get; set; } = "Text color";
            public string HighLightOpt_ColoredBars { get; set; } = "Colored bars";
            public string AddItem { get; set; } = "+ Add item";
            public string Hp { get; set; } = "HP";
            public string Label { get; set; } = "Label";
            public string Color { get; set; } = "Color";
            public string Data { get; set; } = "Data";
        }

        public class Containers
        {
            public string Description { get; set; } = "These settings are for original container gumps, for grid container settings visit the TazUO section";
            public string CharacterBackpackStyle { get; set; } = "Character backpack style";
            public string BackpackOpt_Default { get; set; } = "Default";
            public string BackpackOpt_Suede { get; set; } = "Suede";
            public string BackpackOpt_PolarBear { get; set; } = "Polar bear";
            public string BackpackOpt_GhoulSkin { get; set; } = "Ghoul skin";
            public string ContainerScale { get; set; } = "Container scale";
            public string AlsoScaleItems { get; set; } = "Also scale items";
            public string UseLargeContainerGumps { get; set; } = "Use large container gumps";
            public string DoubleClickToLootItemsInsideContainers { get; set; } = "Double click to loot items inside containers";
            public string RelativeDragAndDropItemsInContainers { get; set; } = "Relative drag and drop items in containers";
            public string HighlightContainerOnGroundWhenMouseIsOverAContainerGump { get; set; } = "Highlight container on ground when mouse is over a container gump";
            public string RecolorContainerGumpByWithContainerHue { get; set; } = "Recolor container gump by with container hue";
            public string OverrideContainerGumpLocations { get; set; } = "Override container gump locations";
            public string OverridePosition { get; set; } = "Override position";
            public string PositionOpt_NearContainer { get; set; } = "Near container";
            public string PositionOpt_TopRight { get; set; } = "Top right";
            public string PositionOpt_LastDraggedPosition { get; set; } = "Last dragged position";
            public string RememberEachContainer { get; set; } = "Remember each container";
            public string RebuildContainersTxt { get; set; } = "Rebuild containers.txt";
        }

        public class Experimental
        {
            public string DisableDefaultUoHotkeys { get; set; } = "Disable default UO hotkeys";
            public string DisableArrowsNumlockArrowsPlayerMovement { get; set; } = "Disable arrows & numlock arrows(player movement)";
            public string DisableTabToggleWarmode { get; set; } = "Disable tab (toggle warmode)";
            public string DisableCtrlQWMessageHistory { get; set; } = "Disable Ctrl + Q/W (message history)";
            public string DisableRightLeftClickAutoMove { get; set; } = "Disable right + left click auto move";
        }

        public class NamePlates
        {
            public string NewEntry { get; set; } = "New entry";
            public string NameOverheadEntryName { get; set; } = "Name overhead entry name";
            public string DeleteEntry { get; set; } = "Delete entry";
        }

        public class Cooldowns
        {
            public string CustomCooldownBars { get; set; } = "Custom cooldown bars";
            public string PositionX { get; set; } = "Position X";
            public string PositionY { get; set; } = "Position Y";
            public string UseLastMovedBarPosition { get; set; } = "Use last moved bar position";
            public string Conditions { get; set; } = "Conditions";
            public string AddCondition { get; set; } = "+ Add condition";
        }

        public class TazUO
        {
            #region General
            public string GridContainers { get; set; } = "Grid containers";
            public string EnableGridContainers { get; set; } = "Enable grid containers";
            public string GridContainerScale { get; set; } = "Grid container scale";
            public string AlsoScaleItems { get; set; } = "Also scale items";
            public string GridItemBorderOpacity { get; set; } = "Grid item border opacity";
            public string BorderColor { get; set; } = "Border color";
            public string ContainerOpacity { get; set; } = "Container opacity";
            public string BackgroundColor { get; set; } = "Background color";
            public string UseContainersHue { get; set; } = "Use container's hue";
            public string SearchStyle { get; set; } = "Search style";
            public string OnlyShow { get; set; } = "Only show";
            public string Highlight { get; set; } = "Highlight";
            public string EnableContainerPreview { get; set; } = "Enable container preview";
            public string TooltipPreview { get; set; } = "This only works on containers that you have opened, otherwise the client does not have that information yet.";
            public string MakeAnchorable { get; set; } = "Make anchorable";
            public string TooltipGridAnchor { get; set; } = "This will allow grid containers to be anchored to other containers/world map/journal";
            public string ContainerStyle { get; set; } = "Container style";
            public string HideBorders { get; set; } = "Hide borders";
            public string DefaultGridRows { get; set; } = "Default grid rows";
            public string DefaultGridColumns { get; set; } = "Default grid columns";
            public string GridHighlightSettings { get; set; } = "Grid highlight settings";
            public string GridHighlightSize { get; set; } = "Grid highlight size";
            #endregion

            #region Journal
            public string Journal { get; set; } = "Journal";
            public string MaxJournalEntries { get; set; } = "Max journal entries";
            public string JournalOpacity { get; set; } = "Journal opacity";
            public string JournalBackgroundColor { get; set; } = "Background color";
            public string JournalStyle { get; set; } = "Journal style";
            public string JournalHideBorders { get; set; } = "Hide borders";
            public string HideTimestamp { get; set; } = "Hide timestamp";
            public string JournalAnchor { get; set; } = "Make anchorable";
            #endregion

            #region ModernPaperdoll
            public string ModernPaperdoll { get; set; } = "Modern paperdoll";
            public string EnableModernPaperdoll { get; set; } = "Enable modern paperdoll";
            public string PaperdollHue { get; set; } = "Paperdoll hue";
            public string DurabilityBarHue { get; set; } = "Durability bar hue";
            public string ShowDurabilityBarBelow { get; set; } = "Show durability bar below %";
            public string PaperdollAnchor { get; set; } = "Make anchorable";
            #endregion

            #region Nameplates
            public string Nameplates { get; set; } = "Nameplates";
            public string NameplatesAlsoActAsHealthBars { get; set; } = "Nameplates also act as health bars";
            public string HpOpacity { get; set; } = "HP opacity";
            public string HideNameplatesIfFullHealth { get; set; } = "Hide nameplates if full health";
            public string OnlyInWarmode { get; set; } = "Only in warmode";
            public string BorderOpacity { get; set; } = "Border opacity";
            public string BackgroundOpacity { get; set; } = "Background opacity";
            #endregion

            #region Mobile
            public string Mobiles { get; set; } = "Mobiles";
            public string DamageToSelf { get; set; } = "Damage to self";
            public string DamageToOthers { get; set; } = "Damage to others";
            public string DamageToPets { get; set; } = "Damage to pets";
            public string DamageToAllies { get; set; } = "Damage to allies";
            public string DamageToLastAttack { get; set; } = "Damage to last attack";
            public string DisplayPartyChatOverPlayerHeads { get; set; } = "Display party chat over player heads";
            public string TooltipPartyChat { get; set; } = "If a party member uses party chat their text will also show above their head to you";
            public string OverheadTextWidth { get; set; } = "Overhead text width";
            public string TooltipOverheadText { get; set; } = "This adjusts the maximum width for text over players, setting to 0 will allow it to use any width needed to stay one line";
            public string BelowMobileHealthBarScale { get; set; } = "Below mobile health bar scale";
            public string AutomaticallyOpenHealthBarsForLastAttack { get; set; } = "Automatically open health bars for last attack";
            public string UpdateOneBarAsLastAttack { get; set; } = "Update one bar as last attack";
            public string HiddenPlayerOpacity { get; set; } = "Hidden player opacity";
            public string HiddenPlayerHue { get; set; } = "Hidden player hue";
            public string RegularPlayerOpacity { get; set; } = "Regular player opacity";
            public string AutoFollowDistance { get; set; } = "Auto follow distance";
            public string DisableAutoFollow { get; set; } = "Disable alt click to auto follow";
            public string DisableMouseInteractionsForOverheadText { get; set; } = "Disable mouse interactions for overhead text";
            public string OverridePartyMemberHues { get; set; } = "Override party member body hues with friendly hue";
            #endregion

            #region Misc
            public string Misc { get; set; } = "Misc";
            public string DisableSystemChat { get; set; } = "Disable system chat";
            public string EnableImprovedBuffGump { get; set; } = "Enable improved buff gump";
            public string BuffGumpHue { get; set; } = "Buff gump hue";
            public string MainGameWindowBackground { get; set; } = "Main game window background";
            public string EnableHealthIndicatorBorder { get; set; } = "Enable health indicator border";
            public string OnlyShowBelowHp { get; set; } = "Only show below hp %";
            public string Size { get; set; } = "Size";
            public string SpellIconScale { get; set; } = "Spell icon scale";
            public string DisplayMatchingHotkeysOnSpellIcons { get; set; } = "Display matching hotkeys on spell icons";
            public string HotkeyTextHue { get; set; } = "Hotkey text hue";
            public string EnableGumpOpacityAdjustViaAltScroll { get; set; } = "Enable gump opacity adjust via Alt + Scroll";
            public string EnableAdvancedShopGump { get; set; } = "Enable advanced shop gump";
            public string DisplaySkillProgressBarOnSkillChanges { get; set; } = "Display skill progress bar on skill changes";
            public string TextFormat { get; set; } = "Text format";
            public string EnableSpellIndicatorSystem { get; set; } = "Enable spell indicator system";
            public string ImportFromUrl { get; set; } = "Import from url";
            public string InputRequestUrl { get; set; } = "Enter the url for the spell config. \n/c[red]This will override your current config.";
            public string Download { get; set; } = "Download";
            public string Cancel { get; set; } = "Cancel";
            public string AttemptingToDownloadSpellConfig { get; set; } = "Attempting to download spell config..";
            public string SuccesfullyDownloadedNewSpellConfig { get; set; } = "Succesfully downloaded new spell config.";
            public string FailedToDownloadTheSpellConfigExMessage { get; set; } = "Failed to download the spell config. ({0})";
            public string AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars { get; set; } = "Also close anchored healthbars when auto closing healthbars";
            public string EnableAutoResyncOnHangDetection { get; set; } = "Enable auto resync on hang detection";
            public string PlayerOffsetX { get; set; } = "Player Offset X";
            public string PlayerOffsetY { get; set; } = "Player Offset Y";
            public string UseLandTexturesWhereAvailable { get; set; } = "Use land textures where available(Experimental)";
            public string SOSGumpID { get; set; } = "SOS Gump ID";
            #endregion

            #region Tooltips
            public string Tooltips { get; set; } = "Tooltips";
            public string AlignTooltipsToTheLeftSide { get; set; } = "Align tooltips to the left side";
            public string AlignMobileTooltipsToCenter { get; set; } = "Align mobile tooltips to center";
            public string BackgroundHue { get; set; } = "Background hue";
            public string HeaderFormatItemName { get; set; } = "Header format(Item name)";
            public string TooltipOverrideSettings { get; set; } = "Tooltip override settings";
            #endregion

            #region Fontsettings
            public string FontSettings { get; set; } = "Font settings";
            public string TtfFontBorder { get; set; } = "TTF Font border";
            public string InfobarFont { get; set; } = "Infobar font";
            public string SharedSize { get; set; } = "Size";
            public string SystemChatFont { get; set; } = "System chat font";
            public string TooltipFont { get; set; } = "Tooltip font";
            public string OverheadFont { get; set; } = "Overhead font";
            public string JournalFont { get; set; } = "Journal font";
            public string NameplateFont { get; set; } = "Nameplate font";
            #endregion

            #region Controller
            public string Controller { get; set; } = "Controller";
            public string MouseSesitivity { get; set; } = "Mouse Sensitivity";
            #endregion

            #region SettingsTransfer
            public string SettingsTransfers { get; set; } = "Settings transfers";
            public string SettingsWarning { get; set; } = "/es/c[red]! Warning !/cd\n" +
                "This will override other character's profile options!\n" +
                "This is not reversable!\n" +
                "You have {0} other profiles that will may overridden with the settings in this profile.\n\n" +
                "This will not override: Macros, skill groups, info bar, grid container data, or gump saved positions.";
            public string OverrideAll { get; set; } = "Override {0} other profiles with this one.";
            public string OverrideSuccess { get; set; } = "{0} profiles overriden.";
            public string OverrideSame { get; set; } = "Override {0} other profiles on this same server with this one.";
            #endregion

            #region GumpScaling
            public string GumpScaling { get; set; } = "Gump scaling";
            public string ScalingInfo { get; set; } = "Some of these settings may only take effect after closing and reopening. Visual bugs may occur until the gump is closed and reopened.";
            public string PaperdollGump { get; set; } = "Paperdoll Gump";
            #endregion

            public string AutoLoot { get; set; } = "Autoloot";

            #region VisibileLayers
            public string VisibleLayers { get; set; } = "Visible Layers";
            public string VisLayersInfo { get; set; } = "These settings are to hide layers on in-game mobiles. Check the box to hide that layer.";
            public string OnlyForYourself { get; set; } = "Only for yourself";
            #endregion
        }
    }

    public class ErrorsLanguage
    {
        public string CommandNotFound { get; set; } = "Command was not found: {0}";
    }

    public class MapLanguage
    {
        public string Follow { get; set; } = "Follow";
        public string Yourself { get; set; } = "Yourself";
    }

    public class TopBarGumpLanguage
    {
        public string CommandsEntry { get; set; } = "Client Commands";
    }
}
