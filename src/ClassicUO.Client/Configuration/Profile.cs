// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
// ## BEGIN - END ## // MACROS
using ClassicUO.Dust765.External;
// ## BEGIN - END ## // MACROS
// ## BEGIN - END ## // MODERNCOOLDOWNBAR
using ClassicUO.Dust765.Dust765;
// ## BEGIN - END ## // MODERNCOOLDOWNBAR
using ClassicUO.Configuration.Json;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    //[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
    [JsonSerializable(typeof(GlobalProfile), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(Profile), GenerationMode = JsonSourceGenerationMode.Metadata)]
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

    internal sealed class GlobalProfile
    {
        public int MaxJournalFiles { get; set; } = 100;
        public bool JournalFileWithSerial { get; set; } = false;
    }

    internal sealed class Profile
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
        public bool SaveJournalToFile { get; set; } = true;
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

        // visual
        public bool EnabledCriminalActionQuery { get; set; } = true;
        public bool EnabledBeneficialCriminalActionQuery { get; set; } = false;
        public bool EnableStatReport { get; set; } = true;
        public bool EnableSkillReport { get; set; } = true;
        public bool UseOldStatusGump { get; set; }
        public bool StatusGumpBarMutuallyExclusive { get; set; } = true;
        public int BackpackStyle { get; set; }
        public bool HighlightGameObjects { get; set; }
        public bool HighlightMobilesByParalize { get; set; } = true;
        public bool HighlightMobilesByPoisoned { get; set; } = true;
        public bool HighlightMobilesByInvul { get; set; } = true;
        public bool ShowMobilesHP { get; set; }
        public int MobileHPType { get; set; }     // 0 = %, 1 = line, 2 = both
        public int MobileHPShowWhen { get; set; } // 0 = Always, 1 - <100%
        public bool DrawRoofs { get; set; } = true;
        public bool TreeToStumps { get; set; }
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

        // tooltip
        public bool UseTooltip { get; set; } = true;
        public ushort TooltipTextHue { get; set; } = 0xFFFF;
        public int TooltipDelayBeforeDisplay { get; set; } = 250;
        public int TooltipDisplayZoom { get; set; } = 100;
        public int TooltipBackgroundOpacity { get; set; } = 70;
        public byte TooltipFont { get; set; } = 1;

        // movements
        public bool EnablePathfind { get; set; }
        public bool UseShiftToPathfind { get; set; }
        public bool AlwaysRun { get; set; }
        public bool AlwaysRunUnlessHidden { get; set; }
        public bool SmoothMovements { get; set; } = true;
        public bool HoldDownKeyTab { get; set; } = true;
        public bool HoldShiftForContext { get; set; } = false;
        public bool HoldShiftToSplitStack { get; set; } = false;

        // general
        [JsonConverter(typeof(Point2Converter))] public Point WindowClientBounds { get; set; } = new Point(600, 480);
        [JsonConverter(typeof(Point2Converter))] public Point ContainerDefaultPosition { get; set; } = new Point(24, 24);
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowPosition { get; set; } = new Point(10, 10);
        public bool GameWindowLock { get; set; }
        public bool GameWindowFullSize { get; set; }
        public bool WindowBorderless { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowSize { get; set; } = new Point(600, 480);
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

        public bool HideScreenshotStoredInMessage { get; set; }

        // Experimental
        public bool CastSpellsByOneClick { get; set; }
        public bool BuffBarTime { get; set; }
        public bool FastSpellsAssign { get; set; }
        public bool AutoOpenDoors { get; set; }
        public bool SmoothDoors { get; set; }
        public bool AutoOpenCorpses { get; set; }
        public int AutoOpenCorpseRange { get; set; } = 2;
        public int CorpseOpenOptions { get; set; } = 3;
        public bool SkipEmptyCorpse { get; set; }
        public bool DisableDefaultHotkeys { get; set; }
        public bool DisableArrowBtn { get; set; }
        public bool DisableTabBtn { get; set; }
        public bool DisableCtrlQWBtn { get; set; }
        public bool DisableAutoMove { get; set; }
        public bool EnableDragSelect { get; set; }
        public int DragSelectModifierKey { get; set; } // 0 = none, 1 = control, 2 = shift
        public bool OverrideContainerLocation { get; set; }

        public int OverrideContainerLocationSetting { get; set; } // 0 = container position, 1 = top right of screen, 2 = last dragged position, 3 = remember every container

        [JsonConverter(typeof(Point2Converter))] public Point OverrideContainerLocationPosition { get; set; } = new Point(200, 200);
        public bool HueContainerGumps { get; set; } = true;
        public bool DragSelectHumanoidsOnly { get; set; }
        public bool DragSelectHostileOnly { get; set; }
        public int DragSelectStartX { get; set; } = 100;
        public int DragSelectStartY { get; set; } = 100;
        public bool DragSelectAsAnchor { get; set; } = false;
        public NameOverheadTypeAllowed NameOverheadTypeAllowed { get; set; } = NameOverheadTypeAllowed.All;
        public bool NameOverheadToggled { get; set; } = false;
        public bool ShowTargetRangeIndicator { get; set; }
        public bool PartyInviteGump { get; set; }
        public bool CustomBarsToggled { get; set; }
        public bool CBBlackBGToggled { get; set; }

        public bool ShowInfoBar { get; set; }
        public int InfoBarHighlightType { get; set; } // 0 = text colour changes, 1 = underline

        public bool CounterBarEnabled { get; set; }
        public bool CounterBarHighlightOnChange { get; set; } = true;
        public bool CounterBarHighlightOnAmount { get; set; }
        public bool CounterBarDisplayAbbreviatedAmount { get; set; }
        public int CounterBarAbbreviatedAmount { get; set; } = 1000;
        public int CounterBarHighlightAmount { get; set; } = 5;
        public int CounterBarCellSize { get; set; } = 40;

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

        public bool ScaleItemsInsideContainers { get; set; }

        public bool DoubleClickToLootInsideContainers { get; set; }

        public bool UseLargeContainerGumps { get; set; } = false;

        public bool RelativeDragAndDropItems { get; set; }

        public bool HighlightContainerWhenSelected { get; set; }

        public bool UseNewTargetSystem { get; set; } = true;
        public bool UseKrEquipUnequipPacket { get; set; }
        public bool ShowHouseContent { get; set; }
        public bool SaveHealthbars { get; set; }
        public bool TextFading { get; set; } = true;

        public bool UseSmoothBoatMovement { get; set; } = false;

        public bool IgnoreStaminaCheck { get; set; } = false;

        // ## BEGIN - END ## 
        public bool ColorStealth { get; set; }
        public ushort StealthHue { get; set; } = 0x0044;
        public int StealthNeonType { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire
        public int BlockerType { get; set; } = 0; // 0 = off, 1 = stump, 2 = tile
        public bool ColorBlockerTile { get; set; }
        public ushort BlockerTileHue { get; set; } = 0x0044;
        public int TreeType { get; set; } = 0; // 0 = off, 1 = stump, 2 = tile
        public bool ColorTreeTile { get; set; }
        public ushort TreeTileHue { get; set; } = 0x0044;
        public int GlowingWeaponsType { get; set; } = 0; // 0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire, 5 = custom
        public ushort HighlightGlowingWeaponsTypeHue { get; set; } = 0x0044;
        public bool BlockWoS { get; set; } = false;
        public bool BlockWoSFelOnly { get; set; } = false;
        public uint BlockWoSArt { get; set; } = 1872;
        public bool BlockWoSArtForceAoS { get; set; } = false;
        public bool BlockEnergyF { get; set; } = false;
        public bool BlockEnergyFFelOnly { get; set; } = false;
        public uint BlockEnergyFArt { get; set; } = 1872;
        public bool BlockEnergyFArtForceAoS { get; set; } = false;
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
        public bool ShowMapCloseFriend { get; set; }
        public bool AutoAvoidMobiles { get; set; }
        [JsonConverter(typeof(Point2Converter))] public Point BandageGumpOffset { get; set; } = new Point(0, 0);
        public bool BandageGump { get; set; }
        public bool BandageGumpUpDownToggle { get; set; } = false;
        public bool ModernCooldwonBar_locked { get; set; } = false;
        public bool OnCastingGump { get; set; }
        public bool OnCastingGump_hidden { get; set; } = false;
        public bool ShowAllLayers { get; set; }
        public bool ShowAllLayersPaperdoll { get; set; }
        public int ShowAllLayersPaperdoll_X { get; set; } = 166;
        // ## BEGIN - END ## 

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
        public bool WorldMapShowSextantCoordinates { get; set; } = false;
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
        public bool WorldMapAllowPositionalTarget { get; set; } = false;
        public bool ShowDPSWithDamageNumbers { get; set; } = true;

        // ## BEGIN - END ## // TAZUO
        public ushort HiddenBodyHue { get; set; } = 0x038E;
        public byte HiddenBodyAlpha { get; set; } = 40;
        #region COOLDOWNS
        public int CoolDownX { get; set; } = 50;
        public int CoolDownY { get; set; } = 50;

        public List<ushort> Condition_Hue { get; set; } = new List<ushort>();
        public List<string> Condition_Label { get; set; } = new List<string>();
        public List<int> Condition_Duration { get; set; } = new List<int>();
        public List<string> Condition_Trigger { get; set; } = new List<string>();
        public List<int> Condition_Type { get; set; } = new List<int>();
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
        // ## BEGIN - END ## // TAZUO

        public static uint GumpsVersion { get; private set; }

        //Alternate Journal
        public bool UseAlternateJournal { get; set; }
        public Dictionary<string, MessageType[]> JournalTabs { get; } = new Dictionary<string, MessageType[]>()
        {
            { "All", new MessageType[] {
                MessageType.Alliance, MessageType.Command, MessageType.Emote,
                MessageType.Encoded, MessageType.Focus, MessageType.Guild,
                MessageType.Label, MessageType.Limit3Spell, MessageType.Party,
                MessageType.Regular, MessageType.Spell, MessageType.System,
                MessageType.Whisper, MessageType.Yell }
            },
            { "Chat", new MessageType[] {
                MessageType.Regular,
                MessageType.Guild,
                MessageType.Alliance,
                MessageType.Emote,
                MessageType.Party,
                MessageType.Whisper,
                MessageType.Yell,
            }
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

        public bool OverheadPartyMessages { get; set; }

        public void Save(World world, string path)
        {
            Log.Trace($"Saving path:\t\t{path}");

            ProfileManager.Save(this, path);

            // Save opened gumps
            SaveGumps(world, path);

            Log.Trace("Saving done!");
        }

        private void SaveGumps(World world, string path)
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
                        Item item = world.Items.Get(gump.LocalSerial);

                        if (item != null && !item.IsDestroyed && item.Opened)
                        {
                            while (SerialHelper.IsItem(item.Container))
                            {
                                item = world.Items.Get(item.Container);
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


            world.SkillsGroupManager.Save();
        }

        private static void SaveItemsGumpRecursive(Item parent, XmlTextWriter xml, LinkedList<Gump> list)
        {
            if (parent != null && !parent.IsDestroyed && parent.Opened)
            {
                SaveItemsGump(parent, xml, list);

                Item first = (Item) parent.Items;

                while (first != null)
                {
                    Item next = (Item) first.Next;

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


        public List<Gump> ReadGumps(World world, string path)
        {
            List<Gump> gumps = new List<Gump>();

            // load skillsgroup
            world.SkillsGroupManager.Load();

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
                    foreach (XmlElement xml in root.ChildNodes /*.GetElementsByTagName("gump")*/)
                    {
                        if (xml.Name != "gump")
                        {
                            continue;
                        }

                        try
                        {
                            GumpType type = (GumpType) int.Parse(xml.GetAttribute(nameof(type)));
                            int x = int.Parse(xml.GetAttribute(nameof(x)));
                            int y = int.Parse(xml.GetAttribute(nameof(y)));
                            uint serial = uint.Parse(xml.GetAttribute(nameof(serial)));

                            Gump gump = null;

                            switch (type)
                            {
                                case GumpType.Buff:
                                    
                                    // ## BEGIN - END ## // TAZUO
                                    //gump = new BuffGump();
                                    // ## BEGIN - END ## // TAZUO
                                    if (ProfileManager.CurrentProfile.UseImprovedBuffBar)
                                        gump = new ImprovedBuffGump();
                                    else
                                        gump = new BuffGump(world);
                                    // ## BEGIN - END ## // TAZUO

                                    break;
                                // ## BEGIN - END ## // MODERNCOOLDOWNBAR
                                case GumpType.ECBuffGump:
                                    gump = new ECBuffGump();

                                    break;

                                case GumpType.ECDebuffGump:
                                    gump = new ECDebuffGump();

                                    break;

                                case GumpType.ECStateGump:
                                    gump = new ECStateGump();

                                    break;

                                case GumpType.ModernCooldownBar:
                                    gump = new ModernCooldownBar();

                                    break;
                                // ## BEGIN - END ## // MODERNCOOLDOWNBAR
                                case GumpType.Container:
                                    gump = new ContainerGump(world);

                                    break;

                                case GumpType.CounterBar:
                                    gump = new CounterBarGump(world);

                                    break;

                                case GumpType.HealthBar:
                                    if (CustomBarsToggled)
                                    {
                                        gump = new HealthBarGumpCustom(world);
                                    }
                                    else
                                    {
                                        gump = new HealthBarGump(world);
                                    }

                                    break;

                                case GumpType.InfoBar:
                                    gump = new InfoBarGump(world);

                                    break;

                                case GumpType.Journal:
                                    if(ProfileManager.CurrentProfile.UseAlternateJournal)
                                        gump = new ResizableJournal(world);
                                    else
                                        gump = new JournalGump(world);

                                    break;

                                case GumpType.MacroButton:
                                    gump = new MacroButtonGump(world);

                                    break;

                                case GumpType.MiniMap:
                                    gump = new MiniMapGump(world);

                                    break;

                                case GumpType.PaperDoll:
                                    gump = new PaperDollGump(world);

                                    break;

                                case GumpType.SkillMenu:
                                    if (StandardSkillsGump)
                                    {
                                        gump = new StandardSkillsGump(world);
                                    }
                                    else
                                    {
                                        gump = new SkillGumpAdvanced(world);
                                    }

                                    break;

                                case GumpType.SpellBook:
                                    gump = new SpellbookGump(world);

                                    break;

                                case GumpType.StatusGump:
                                    gump = StatusGumpBase.AddStatusGump(world, 0, 0);

                                    break;

                                //case GumpType.TipNotice:
                                //    gump = new TipNoticeGump();
                                //    break;
                                case GumpType.AbilityButton:
                                    gump = new UseAbilityButtonGump(world);

                                    break;

                                case GumpType.SpellButton:
                                    gump = new UseSpellButtonGump(world);

                                    break;

                                case GumpType.SkillButton:
                                    gump = new SkillButtonGump(world);

                                    break;

                                case GumpType.RacialButton:
                                    gump = new RacialAbilityButton(world);

                                    break;

                                case GumpType.WorldMap:
                                    gump = new WorldMapGump(world);

                                    break;

                                case GumpType.Debug:
                                    gump = new DebugGump(world, 100, 100);

                                    break;

                                case GumpType.NetStats:
                                    gump = new NetworkStatsGump(world, 100, 100);

                                    break;

                                case GumpType.NameOverHeadHandler:
                                    NameOverHeadHandlerGump.LastPosition = new Point(x, y);
                                    // Gump gets opened by NameOverHeadManager, we just want to save the last position from profile
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
                                GumpType type = (GumpType) int.Parse(xml.GetAttribute("type"));
                                int x = int.Parse(xml.GetAttribute("x"));
                                int y = int.Parse(xml.GetAttribute("y"));
                                uint serial = uint.Parse(xml.GetAttribute("serial"));

                                int matrix_x = int.Parse(xml.GetAttribute("matrix_x"));
                                int matrix_y = int.Parse(xml.GetAttribute("matrix_y"));

                                AnchorableGump gump = null;

                                switch (type)
                                {
                                    case GumpType.SpellButton:
                                        gump = new UseSpellButtonGump(world);

                                        break;

                                    case GumpType.SkillButton:
                                        gump = new SkillButtonGump(world);

                                        break;

                                    case GumpType.HealthBar:
                                        if (CustomBarsToggled)
                                        {
                                            gump = new HealthBarGumpCustom(world);
                                        }
                                        else
                                        {
                                            gump = new HealthBarGump(world);
                                        }

                                        break;

                                    case GumpType.AbilityButton:
                                        gump = new UseAbilityButtonGump(world);

                                        break;

                                    case GumpType.MacroButton:
                                        gump = new MacroButtonGump(world);

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