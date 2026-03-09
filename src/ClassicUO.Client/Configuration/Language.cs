using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using ClassicUO.TazUO;

namespace ClassicUO.Configuration
{
    public class Language
    {
        public static readonly string[] SupportedUILanguages = { "ENG", "ESP", "PTB", "RUSSIA", "KOREANO", "CHINES", "POLONES" };

        public LoginLanguage Login { get; set; } = new LoginLanguage();
        [JsonPropertyName("GetModernOptionsGumpLanguage")]
        public OptionsGumpLanguage GetOptionsGumpLanguage { get; set; } = new OptionsGumpLanguage();
        public ErrorsLanguage ErrorsLanguage { get; set; } = new ErrorsLanguage();
        public MapLanguage MapLanguage { get; set; } = new MapLanguage();
        public TopBarGumpLanguage TopBarGump { get; set; } = new TopBarGumpLanguage();
        public TazUOLanguage GetTazUO { get; set; } = new TazUOLanguage();

        public string TazuoVersionHistory { get => GetTazUO.TazuoVersionHistory; set => GetTazUO.TazuoVersionHistory = value; }
        public string CurrentVersion { get => GetTazUO.CurrentVersion; set => GetTazUO.CurrentVersion = value; }
        public string TazUOWiki { get => GetTazUO.TazUOWiki; set => GetTazUO.TazUOWiki = value; }
        public string TazUODiscord { get => GetTazUO.TazUODiscord; set => GetTazUO.TazUODiscord = value; }
        public string CommandGump { get; set; } = "Available Client Commands";

        [JsonIgnore]
        public static Language Instance { get; private set; } = new Language();

        public static void Load()
        {
            Load(GetUILanguageCode());
        }

        public static void Load(string uiLanguageCode)
        {
            string path = GetLanguageFilePath(uiLanguageCode);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                Language f = JsonSerializer.Deserialize<Language>(json, GetJsonOptions());
                Instance = f;
                if (Instance.Login == null)
                    Instance.Login = new LoginLanguage();
                Instance.GetTazUO.ButtonTazUO = Instance.GetOptionsGumpLanguage?.ButtonTazUO ?? Instance.GetTazUO.ButtonTazUO;
                Save(path);
            }
            else if (File.Exists(languageFilePath))
            {
                string json = File.ReadAllText(languageFilePath, Encoding.UTF8);
                Language f = JsonSerializer.Deserialize<Language>(json, GetJsonOptions());
                Instance = f;
                if (Instance.Login == null)
                    Instance.Login = new LoginLanguage();
                Instance.GetTazUO.ButtonTazUO = Instance.GetOptionsGumpLanguage?.ButtonTazUO ?? Instance.GetTazUO.ButtonTazUO;
                Save(languageFilePath);
                string langPath = GetLanguageFilePath(uiLanguageCode);
                if (langPath != languageFilePath)
                    Save(langPath);
            }
            else
            {
                CreateNewLanguageFile();
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement, UnicodeRanges.Cyrillic, UnicodeRanges.CjkUnifiedIdeographs, UnicodeRanges.HangulSyllables, UnicodeRanges.LatinExtendedA, UnicodeRanges.LatinExtendedB),
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
        }

        private static string GetUILanguageCode()
        {
            try
            {
                string code = global::ClassicUO.Configuration.Settings.GlobalSettings?.UILanguage;
                if (!string.IsNullOrWhiteSpace(code))
                {
                    string upper = code.ToUpperInvariant();
                    foreach (string supported in SupportedUILanguages)
                    {
                        if (supported.Equals(upper, System.StringComparison.OrdinalIgnoreCase))
                            return supported;
                    }
                }
            }
            catch { }
            return "ENG";
        }

        private static string GetLanguageFilePath(string uiLanguageCode)
        {
            string dir = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Languages");
            return Path.Combine(dir, $"{uiLanguageCode.ToLowerInvariant()}.json");
        }

        private static void CreateNewLanguageFile()
        {
            Directory.CreateDirectory(Path.Combine(CUOEnviroment.ExecutablePath, "Data"));
            string defaultLanguage = JsonSerializer.Serialize(Instance, GetJsonOptions());
            File.WriteAllText(languageFilePath, defaultLanguage, Encoding.UTF8);
        }

        private static void Save(string path = null)
        {
            path ??= languageFilePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            Instance.GetOptionsGumpLanguage.ButtonTazUO = Instance.GetTazUO.ButtonTazUO;
            string language = JsonSerializer.Serialize(Instance, GetJsonOptions());
            File.WriteAllText(path, language, Encoding.UTF8);
        }

        private static string languageFilePath { get { return Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Language.json"); } }
    }

    public class OptionsGumpLanguage
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
        public string ButtonActionBar { get; set; } = "Action Bar";
        public string ButtonContainers { get; set; } = "Containers";
        public string ButtonExperimental { get; set; } = "Experimental";
        public string ButtonIgnoreList { get; set; } = "Ignore List";
        public string ButtonNameplates { get; set; } = "Nameplate Options";
        public string ButtonCooldowns { get; set; } = "Cooldown bars";
        public string ButtonTazUO { get; set; } = "TazUO Specific";

        public string ButtonDust { get; set; } = "Dust7654";

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
        public ActionBar GetActionBar { get; set; } = new ActionBar();
        public Containers GetContainers { get; set; } = new Containers();
        public Experimental GetExperimental { get; set; } = new Experimental();
        public NamePlates GetNamePlates { get; set; } = new NamePlates();
        public Cooldowns GetCooldowns { get; set; } = new Cooldowns();
        public TazUOLanguage GetTazUO { get; set; } = new TazUOLanguage();
        public Dust765 GetDust765 { get; set; } = new Dust765();

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

            public string AutoAvoidObstacules { get; set; } = "Auto Avoid Obstacules";
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
            public string EnableVSync { get; set; } = "Enable VSync (limits FPS to monitor refresh)";
            public string DisableFrameLimiting { get; set; } = "Unlimited FPS (ignores FPS cap)";
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
            public string VisualStyle { get; set; } = "Visual style";
            public string VisualStyleClassic { get; set; } = "Classic (2D)";
            public string VisualStyleEnhanced { get; set; } = "Enhanced (3D-like)";
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
            public string HighlightTilesOnRange { get; set; } = "Highlight tiles on range";
            public string AtRange { get; set; } = "@ range:";
            public string TileColor { get; set; } = "Tile color";
            public string HighlightTilesOnRangeSpell { get; set; } = "Highlight tiles on range for spells";
            public string PreviewFields { get; set; } = "Preview Fields";
            public string PreviewTeleportTiles { get; set; } = "Preview teleport tiles (highlight valid tiles when casting Teleport)";
            public string TeleportPreviewTileColor { get; set; } = "Teleport preview tile color";
            public string ColorOwnAuraByHP { get; set; } = "Color own aura by HP (needs aura enabled)";
            public string GlowingWeapons { get; set; } = "Glowing Weapons:";
            public string Off { get; set; } = "Off";
            public string White { get; set; } = "White";
            public string Pink { get; set; } = "Pink";
            public string Ice { get; set; } = "Ice";
            public string Fire { get; set; } = "Fire";
            public string Custom { get; set; } = "Custom";
            public string CustomColorGlowingWeapons { get; set; } = "Custom color Glowing Weapons";
            public string HighlightLasttarget { get; set; } = "Highlight lasttarget:";
            public string CustomColorLastTarget { get; set; } = "Custom color last target";
            public string HighlightLasttargetPoisoned { get; set; } = "Highlight lasttarget poisoned:";
            public string CustomColorPoisoned { get; set; } = "Custom color poisoned";
            public string HighlightLasttargetParalyzed { get; set; } = "Highlight lasttarget paralyzed:";
            public string CustomColorParalyzed { get; set; } = "Custom color paralyzed";
            public string HighlightLTHealthbar { get; set; } = "Highlight LT healthbar";
            public string HighlightHealthBarByState { get; set; } = "Highlight healthbar border by state";
            public string FlashingHealthbarOutlineSelf { get; set; } = "Flashing healthbar outline - self";
            public string FlashingHealthbarOutlineParty { get; set; } = "Flashing healthbar outline - party";
            public string FlashingHealthbarOutlineAlly { get; set; } = "Flashing healthbar outline - ally";
            public string FlashingHealthbarOutlineEnemy { get; set; } = "Flashing healthbar outline - enemy";
            public string FlashingHealthbarOutlineAll { get; set; } = "Flashing healthbar outline - all";
            public string FlashingHealthbarNegativeOnly { get; set; } = "Flashing healthbar outline on negative changes only";
            public string OnlyFlashOnHPChange { get; set; } = "Only flash on HP change >= : ";
            public string ShowSpellsOnCursor { get; set; } = "Show spells on cursor";
            public string ColorGameCursorWhenTargeting { get; set; } = "Color game cursor when targeting (hostile / friendly)";
            public string DisplayRangeInOverhead { get; set; } = "Display range in overhead (needs HP overhead enabled)";
            public string UseOldHealthlines { get; set; } = "Use old healthlines";
            public string DisplayManaStamInUnderline { get; set; } = "Display Mana / Stam in underline for self and party (requires old healthbars)";
            public string UseBiggerUnderlines { get; set; } = "Use bigger underlines for self and party (requires old healthbars)";
            public string TransparencyForSelfAndParty { get; set; } = "Transparency for self and party (close client completly), (requires old healthlines): ";
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

        public class ActionBar
        {
            public string ShowActionBar { get; set; } = "Show action bar";
            public string Slot { get; set; } = "Slot {0}";
            public string TargetSelf { get; set; } = "Self";
            public string TargetLast { get; set; } = "Last";
            public string DragSpellHere { get; set; } = "Drag spell here";
            public string Hotkey { get; set; } = "Hotkey";
            public string ResetSlot { get; set; } = "Reset slot";
            public string SetHotkey { get; set; } = "Set Hotkey";
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
            public string LockCooldownBar { get; set; } = "Lock cooldown bar position";
            public string Conditions { get; set; } = "Conditions";
            public string AddCondition { get; set; } = "+ Add condition";
        }

        public class Dust765
        {
            public string ArtHueChanges { get; set; } = "Art / Hue Changes";
            public string ColorStealth { get; set; } = "Color stealth ON / OFF";
            public string StealthColor { get; set; } = "Stealth Color";
            public string OrNeon { get; set; } = "Or Neon";
            public string ColorEnergyBolt { get; set; } = "Color Enery bolt ON / OFF";
            public string ColorEnergyBoltLabel { get; set; } = "Color Energy Bolt";
            public string OrNeonLabel { get; set; } = "Or Neon: ";
            public string ChangeEnergyBoltArtTo { get; set; } = "Change energy bolt art to:";
            public string Normal { get; set; } = "Normal";
            public string Explo { get; set; } = "Explo";
            public string Bagball { get; set; } = "Bagball";
            public string ChangeGoldArtTo { get; set; } = "Change gold art to:";
            public string Cannonball { get; set; } = "Cannonball";
            public string PrevCoin { get; set; } = "Prev Coin";
            public string ColorCannonballOrPrevCoin { get; set; } = "Color cannonball or prev coin ON / OFF";
            public string CannonballOrPrevCoinColor { get; set; } = "Cannonball or prev coin color";
            public string ChangeTreeArtTo { get; set; } = "Change tree art to:";
            public string Stump { get; set; } = "Stump";
            public string Tile { get; set; } = "Tile";
            public string ColorStumpOrTile { get; set; } = "Color stump or tile ON / OFF";
            public string StumpOrTileColor { get; set; } = "Stump or tile color";
            public string BlockerType { get; set; } = "Blocker Type:";
            public string ColorStumpOrTileBlocker { get; set; } = "Color stump or tile";
            public string HealthBars { get; set; } = "HealthBars";
            public string Cursor { get; set; } = "Cursor";
            public string OverheadUnderchar { get; set; } = "Overhead / Underchar";
            public string OldHealthlines { get; set; } = "Old Healthlines";
            public string Misc { get; set; } = "Misc";
            public string OffscreenTargeting { get; set; } = "Offscreen targeting (always on)";
            public string SetTargetOutRange { get; set; } = "Set target with is out range";
            public string OverrideContainerOpenRange { get; set; } = "Override container open range";
            public string ShowCloseFriendInWordMapGump { get; set; } = "Show Close Friend in WordMapGump";
            public string AutoAvoidObstaculesAndMobiles { get; set; } = "Auto avoid obstacules and mobiles";
            public string ShowUseLootModalOnCtrl { get; set; } = "Show Use/Loot modal when pressing Ctrl (nearby items)";
            public string RazorTargetToLasttargetString { get; set; } = "Razor * Target * to lasttarget string";
            public string TextForTargetMsgHead { get; set; } = "Text for Target Msg Head: ";
            public string OutlineStaticsBlack { get; set; } = "Outline statics black (CURRENTLY BROKEN): ";
            public string IgnoreStaminaCheck { get; set; } = "Ignore stamina check";
            public string BlockWallOfStone { get; set; } = "Block Wall of Stone";
            public string BlockWallOfStoneFelOnly { get; set; } = "Block Wall of Stone Fel only";
            public string WallOfStoneArt { get; set; } = "Wall of Stone Art (-info -> DisplayedGraphic): ";
            public string ForceWoSToArtAbove { get; set; } = "Force WoS to Art above (AoS only?) and hue 945";
            public string BlockEnergyField { get; set; } = "Block Energy Field";
            public string BlockEnergyFieldFelOnly { get; set; } = "Block Energy Field Fell Only";
            public string EnergyFieldArt { get; set; } = "Energy Field Art (-info -> DisplayedGraphic): ";
            public string ForceEnergyFToArtAbove { get; set; } = "Force EnergyF to Art above (AoS only?) and hue 293";
            public string EnableWireFrameView { get; set; } = "Enable WireFrame view (restart needed) (CURRENTLY BROKEN)";
            public string HueImpassableTiles { get; set; } = "Hue impassable Tiles";
            public string HueLabel { get; set; } = "Hue ";
            public string TransparentHousesAndItems { get; set; } = "Transparent Houses and Items (Z level):";
            public string TransparencyZ { get; set; } = "Transparency Z: ";
            public string Transparency { get; set; } = "Transparency: ";
            public string InvisibleHousesAndItems { get; set; } = "Invisible Houses and Items (Z level):";
            public string InvisibleZ { get; set; } = "Invisible Z: ";
            public string Misc2 { get; set; } = "Misc2";
            public string AutoLootSection { get; set; } = "Auto Loot";
            public string BuffbarUCC { get; set; } = "Buffbar UCC";
            public string SelfAutomations { get; set; } = "Self Automations";
            public string MacrosSection { get; set; } = "Macros";
            public string GumpsSection { get; set; } = "Gumps";
            public string TextureManager { get; set; } = "Texture Manager";
            public string LinesUI { get; set; } = "Lines (Lines UI)";
            public string PvMPvPSection { get; set; } = "PvM / PvP";
            public string PvM_DamageCounterOnLastTarget { get; set; } = "Damage counter (total/DPS) on last target";
            public string PvM_DamageCounterAsOverhead { get; set; } = "Damage counter (total/DPS) as overhead above target";
            public string PvM_AggroIndicatorOnHealthBar { get; set; } = "Aggro indicator on health bar / overhead";
            public string PvM_CorpseFilterByNotoriety { get; set; } = "Filter corpses by notoriety";
            public string PvM_LowHpAlertOnLastTarget { get; set; } = "Low HP alert on last target";
            public string PvM_KillCountMarkerPerSession { get; set; } = "Kill count marker per session";
            public string PvM_LootHighlightOnCorpse { get; set; } = "Highlight loot on corpse";
            public string PvP_CriminalAttackableAlert { get; set; } = "Criminal / attackable alert on screen";
            public string PvP_WarModeIndicator { get; set; } = "War mode indicator";
            public string PvP_GreyCriminalTimer { get; set; } = "Grey / criminal timer";
            public string PvP_LastAttackerHighlight { get; set; } = "Highlight last attacker";
            public string PvP_SpellRangeOnCursor { get; set; } = "Spell range on cursor";
            public string PvP_QuickTargetEnemyList { get; set; } = "Quick-target enemy list";
            public string PvP_OptimizedMode { get; set; } = "PvP optimized mode (auto-reduce graphics in combat)";
            public string PvX_NameOverheadProfilesByContext { get; set; } = "Name overhead profiles (PvM vs PvP)";
            public string PvX_ConfigurableSoundsPerEvent { get; set; } = "Configurable sounds per event";
            public string PvX_BlockBeneficialOnEnemies { get; set; } = "Block beneficial spells on enemies";
            public string PvX_LastTargetDirectionIndicator { get; set; } = "Last target direction (offscreen arrow)";
            public string PvX_LockLastTarget { get; set; } = "Lock last target";
        }

    }

    public class LoginLanguage
    {
        public string LoginButton { get; set; } = "LOGIN";
        public string SaveAccount { get; set; } = "Save Account";
        public string Autologin { get; set; } = "Autologin";
        public string Music { get; set; } = "Music";
        public string Support { get; set; } = "Dust765 Support";
        public string VersionFormat { get; set; } = "Dust765 Version {0}";
        public string UOVersionFormat { get; set; } = "UO Version {0}";
        public string LanguageLabel { get; set; } = "Language";
        public string RightClickAccountTooltip { get; set; } = "Right click to select another account.";
        public string UpdateAvailable { get; set; } = "A new version of TazUO is available!\n Click to open the download page.";
        public string Back { get; set; } = "BACK";
        public string Next { get; set; } = "NEXT";
        public string SelectWhichShardToPlayOn { get; set; } = "Select which shard to play on.";
        public string Latency { get; set; } = "Latency";
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
