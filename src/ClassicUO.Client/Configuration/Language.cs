using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Configuration
{
    public class Language
    {
        public ModernOptionsGumpLanguage GetModernOptionsGumpLanguage { get; set; } = new ModernOptionsGumpLanguage();

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

        public General GetGeneral { get; set; } = new General();

        public class General
        {
            public string SharedNone { get; set; } = "None";
            public string SharedShift { get; set; } = "Shift";
            public string SharedCtrl { get; set; } = "Ctrl";

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
            #endregion
        }
    }
}
