#region license

//  Copyright (C) 2020 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering
//	new technologies.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using SDL2;

namespace ClassicUO.Configuration
{
    internal sealed class Profile
    {
        [JsonConstructor]
        public Profile(string username, string servername, string charactername)
        {
            Username = username;
            ServerName = servername;
            CharacterName = charactername;
        }

        [JsonIgnore] public string Username { get; set; }
        [JsonIgnore] public string ServerName { get; set;  }
        [JsonIgnore] public string CharacterName { get; set;  }

        // sounds
        [JsonProperty] public bool EnableSound { get; set; } = true;
        [JsonProperty] public int SoundVolume { get; set; } = 100;
        [JsonProperty] public bool EnableMusic { get; set; } = true;
        [JsonProperty] public int MusicVolume { get; set; } = 100;
        [JsonProperty] public bool EnableFootstepsSound { get; set; } = true;
        [JsonProperty] public bool EnableCombatMusic { get; set; } = true;
        [JsonProperty] public bool ReproduceSoundsInBackground { get; set; }

        // fonts and speech
        [JsonProperty] public byte ChatFont { get; set; } = 1;
        [JsonProperty] public int SpeechDelay { get; set; } = 100;
        [JsonProperty] public bool ScaleSpeechDelay { get; set; } = true;
        [JsonProperty] public bool SaveJournalToFile { get; set; }
        [JsonProperty] public bool ForceUnicodeJournal { get; set; }

        // hues
        [JsonProperty] public ushort SpeechHue { get; set; } = 0x02B2;
        [JsonProperty] public ushort WhisperHue { get; set; } = 0x0033;
        [JsonProperty] public ushort EmoteHue { get; set; } = 0x0021;
        [JsonProperty] public ushort YellHue { get; set; } = 0x0021;
        [JsonProperty] public ushort PartyMessageHue { get; set; } = 0x0044;
        [JsonProperty] public ushort GuildMessageHue { get; set; } = 0x0044;
        [JsonProperty] public ushort AllyMessageHue { get; set; } = 0x0057;
        [JsonProperty] public ushort ChatMessageHue { get; set; } = 0x0256;
        [JsonProperty] public ushort InnocentHue { get; set; } = 0x005A;
        [JsonProperty] public ushort PartyAuraHue { get; set; } = 0x0044;
        [JsonProperty] public ushort FriendHue { get; set; } = 0x0044;
        [JsonProperty] public ushort CriminalHue { get; set; } = 0x03B2;
        [JsonProperty] public ushort AnimalHue { get; set; } = 0x03B2;
        [JsonProperty] public ushort EnemyHue { get; set; } = 0x0031;
        [JsonProperty] public ushort MurdererHue { get; set; } = 0x0023;
        [JsonProperty] public ushort BeneficHue { get; set; } = 0x0059;
        [JsonProperty] public ushort HarmfulHue { get; set; } = 0x0020;
        [JsonProperty] public ushort NeutralHue { get; set; } = 0x03B1;
        [JsonProperty] public bool EnabledSpellHue { get; set; }
        [JsonProperty] public bool EnabledSpellFormat { get; set; }
        [JsonProperty] public string SpellDisplayFormat { get; set; } = "{power} [{spell}]";
        [JsonProperty] public ushort PoisonHue { get; set; } = 0x0044;
        [JsonProperty] public ushort ParalyzedHue { get; set; } = 0x014C;
        [JsonProperty] public ushort InvulnerableHue { get; set; } = 0x0030;

        // visual
        [JsonProperty] public bool EnabledCriminalActionQuery { get; set; } = true;
        [JsonProperty] public bool ShowIncomingNames { get; set; } = true;
        [JsonProperty] public bool EnableStatReport { get; set; } = true;
        [JsonProperty] public bool EnableSkillReport { get; set; } = true;
        [JsonProperty] public bool UseOldStatusGump { get; set; }
        [JsonProperty] public int BackpackStyle { get; set; }
        [JsonProperty] public bool HighlightGameObjects { get; set; }
        [JsonProperty] public bool HighlightMobilesByFlags { get; set; } = true;
        [JsonProperty] public bool ShowMobilesHP { get; set; }
        [JsonProperty] public int MobileHPType { get; set; } // 0 = %, 1 = line, 2 = both
        [JsonProperty] public int MobileHPShowWhen { get; set; } // 0 = Always, 1 - <100%
        [JsonProperty] public bool DrawRoofs { get; set; } = true;
        [JsonProperty] public bool TreeToStumps { get; set; }
        [JsonProperty] public bool EnableCaveBorder { get; set; }
        [JsonProperty] public bool HideVegetation { get; set; }
        [JsonProperty] public int FieldsType { get; set; } // 0 = normal, 1 = static, 2 = tile
        [JsonProperty] public bool NoColorObjectsOutOfRange { get; set; }
        [JsonProperty] public bool UseCircleOfTransparency { get; set; }
        [JsonProperty] public int CircleOfTransparencyRadius { get; set; } = Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS / 2;
        [JsonProperty] public int VendorGumpHeight { get; set; } = 60; //original vendor gump size
        [JsonProperty] public float ScaleZoom { get; set; } = 1.0f;
        [JsonProperty] public float RestoreScaleValue { get; set; } = 1.0f;
        [JsonProperty] public bool EnableScaleZoom { get; set; }
        [JsonProperty] public bool SaveScaleAfterClose { get; set; }
        [JsonProperty] public bool RestoreScaleAfterUnpressCtrl { get; set; }
        [JsonProperty] public bool BandageSelfOld { get; set; } = true;
        [JsonProperty] public bool EnableDeathScreen { get; set; } = true;
        [JsonProperty] public bool EnableBlackWhiteEffect { get; set; } = true;

        // tooltip
        [JsonProperty] public bool EnableTooltip { get; set; } = true;
        [JsonProperty] public int DelayShowTooltip { get; set; } = 250;
        [JsonProperty] public ushort TooltipTextHue { get; set; } = 0xFFFF;

        // movements
        [JsonProperty] public bool EnablePathfind { get; set; }
        [JsonProperty] public bool UseShiftToPathfind { get; set; }
        [JsonProperty] public bool AlwaysRun { get; set; }
        [JsonProperty] public bool AlwaysRunUnlessHidden { get; set; }
        [JsonProperty] public bool SmoothMovements { get; set; } = true;
        [JsonProperty] public bool HoldDownKeyTab { get; set; } = true;
        [JsonProperty] public bool HoldShiftForContext { get; set; } = false;
        [JsonProperty] public bool HoldShiftToSplitStack { get; set; } = false;

        // general
        [JsonProperty] public Point WindowClientBounds { get; set; } = new Point(600, 480);
        [JsonProperty] public Point ContainerDefaultPosition { get; set; } = new Point(24, 24);
        [JsonProperty] public Point GameWindowPosition { get; set; } = new Point(10, 10);
        [JsonProperty] public bool GameWindowLock { get; set; }
        [JsonProperty] public bool GameWindowFullSize { get; set; }
        [JsonProperty] public bool WindowBorderless { get; set; } = false;
        [JsonProperty] public Point GameWindowSize { get; set; } = new Point(600, 480);
        [JsonProperty] public Point TopbarGumpPosition { get; set; } = new Point(0, 0);
        [JsonProperty] public bool TopbarGumpIsMinimized { get; set; }
        [JsonProperty] public bool TopbarGumpIsDisabled { get; set; }
        [JsonProperty] public bool UseAlternativeLights { get; set; }
        [JsonProperty] public bool UseCustomLightLevel { get; set; }
        [JsonProperty] public byte LightLevel { get; set; }
        [JsonProperty] public bool UseColoredLights { get; set; } = true;
        [JsonProperty] public bool UseDarkNights { get; set; }
        [JsonProperty] public int CloseHealthBarType { get; set; } // 0 = none, 1 == not exists, 2 == is dead
        [JsonProperty] public bool ActivateChatAfterEnter { get; set; }
        [JsonProperty] public bool ActivateChatAdditionalButtons { get; set; } = true;
        [JsonProperty] public bool ActivateChatShiftEnterSupport { get; set; } = true;
        [JsonProperty] public bool UseObjectsFading { get; set; } = true;
        [JsonProperty] public bool HoldDownKeyAltToCloseAnchored { get; set; } = true;
        [JsonProperty] public bool CloseAllAnchoredGumpsInGroupWithRightClick { get; set; } = false;
        [JsonProperty] public bool HoldAltToMoveGumps { get; set; }

        // Experimental
        [JsonProperty] public bool EnableSelectionArea { get; set; }
        [JsonProperty] public bool DebugGumpIsDisabled { get; set; }
        [JsonProperty] public Point DebugGumpPosition { get; set; } = new Point(25, 25);
        [JsonProperty] public bool DebugGumpIsMinimized { get; set; } = true;
        [JsonProperty] public bool RestoreLastGameSize { get; set; }
        [JsonProperty] public bool CastSpellsByOneClick { get; set; }
        [JsonProperty] public bool BuffBarTime { get; set; }
        [JsonProperty] public bool AutoOpenDoors { get; set; }
        [JsonProperty] public bool SmoothDoors { get; set; }
        [JsonProperty] public bool AutoOpenCorpses { get; set; }
        [JsonProperty] public int AutoOpenCorpseRange { get; set; } = 2;
        [JsonProperty] public int CorpseOpenOptions { get; set; } = 3;
        [JsonProperty] public bool SkipEmptyCorpse { get; set; }
        [JsonProperty] public bool DisableDefaultHotkeys { get; set; }
        [JsonProperty] public bool DisableArrowBtn { get; set; }
        [JsonProperty] public bool DisableTabBtn { get; set; }
        [JsonProperty] public bool DisableCtrlQWBtn { get; set; }
        [JsonProperty] public bool EnableDragSelect { get; set; }
        [JsonProperty] public int DragSelectModifierKey { get; set; } // 0 = none, 1 = control, 2 = shift
        [JsonProperty] public bool OverrideContainerLocation { get; set; }
        [JsonProperty] public int OverrideContainerLocationSetting { get; set; } // 0 = container position, 1 = top right of screen, 2 = last dragged position
        [JsonProperty] public Point OverrideContainerLocationPosition { get; set; } = new Point(200, 200);
        [JsonProperty] public bool DragSelectHumanoidsOnly { get; set; }
        [JsonProperty] public NameOverheadTypeAllowed NameOverheadTypeAllowed { get; set; } = NameOverheadTypeAllowed.All;
        [JsonProperty] public bool NameOverheadToggled { get; set; } = false;
        [JsonProperty] public bool ShowTargetRangeIndicator { get; set; }
        [JsonProperty] public bool PartyInviteGump { get; set; }
        [JsonProperty] public bool CustomBarsToggled { get; set; }
        [JsonProperty] public bool CBBlackBGToggled { get; set; }

        [JsonProperty] public bool ShowInfoBar { get; set; }
        [JsonProperty] public int InfoBarHighlightType { get; set; } // 0 = text colour changes, 1 = underline
      

        [JsonProperty]
        public InfoBarItem[] InfoBarItems { get; set; }// [FILE_FIX] TODO: REMOVE IT
        [JsonProperty]
        public Macro[] Macros { get; set; } // [FILE_FIX] TODO: REMOVE IT


        [JsonProperty] public bool CounterBarEnabled { get; set; }
        [JsonProperty] public bool CounterBarHighlightOnUse { get; set; }
        [JsonProperty] public bool CounterBarHighlightOnAmount { get; set; }
        [JsonProperty] public bool CounterBarDisplayAbbreviatedAmount { get; set; }
        [JsonProperty] public int CounterBarAbbreviatedAmount { get; set; } = 1000;
        [JsonProperty] public int CounterBarHighlightAmount { get; set; } = 5;
        [JsonProperty] public int CounterBarCellSize { get; set; } = 40;
        [JsonProperty] public int CounterBarRows { get; set; } = 1;
        [JsonProperty] public int CounterBarColumns { get; set; } = 1;


        [JsonProperty] public bool ShadowsEnabled { get; set; } = true;
        [JsonProperty] public int AuraUnderFeetType { get; set; } // 0 = NO, 1 = in warmode, 2 = ctrl+shift, 3 = always
        [JsonProperty] public bool AuraOnMouse { get; set; } = true;
        [JsonProperty] public bool ShowNetworkStats { get; set; }
        [JsonProperty] public bool PartyAura { get; set; }

        [JsonProperty] public bool UseXBR { get; set; } = true;

        [JsonProperty] public bool HideChatGradient { get; set; } = false;

        [JsonProperty] public bool StandardSkillsGump { get; set; } = true;

        [JsonProperty] public bool ShowNewMobileNameIncoming { get; set; } = true;
        [JsonProperty] public bool ShowNewCorpseNameIncoming { get; set; } = true;

        [JsonProperty] public uint GrabBagSerial { get; set; }

        [JsonProperty] public int GridLootType { get; set; } // 0 = none, 1 = only grid, 2 = both

        [JsonProperty] public bool ReduceFPSWhenInactive { get; set; }

        [JsonProperty] public bool OverrideAllFonts { get; set; }
        [JsonProperty] public bool OverrideAllFontsIsUnicode { get; set; } = true;

        [JsonProperty] public bool SallosEasyGrab { get; set; }

        [JsonProperty] public float Brighlight { get; set; }

        [JsonProperty] public bool JournalDarkMode { get; set; }

        [JsonProperty] public byte ContainersScale { get; set; } = 100;

        [JsonProperty] public bool ScaleItemsInsideContainers { get; set; }

        [JsonProperty] public bool DoubleClickToLootInsideContainers { get; set; }

        [JsonProperty] public bool RelativeDragAndDropItems { get; set; }

        [JsonProperty] public bool ShowHouseContent { get; set; }


        internal static string ProfilePath { get; } = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
        internal static string DataPath { get; } = Path.Combine(CUOEnviroment.ExecutablePath, "Data");

        public void Save(List<Gump> gumps = null)
        {
            if (string.IsNullOrEmpty(ServerName))
                throw new InvalidDataException();

            if (string.IsNullOrEmpty(Username))
                throw new InvalidDataException();

            if (string.IsNullOrEmpty(CharacterName))
                throw new InvalidDataException();

            string path = FileSystemHelper.CreateFolderIfNotExists(ProfilePath, Username.Trim(), ServerName.Trim(), CharacterName.Trim());

            Log.Trace( $"Saving path:\t\t{path}");

            // Save profile settings
            ConfigurationResolver.Save(this, Path.Combine(path, "profile.json"), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });

            // Save opened gumps
            SaveGumps(path, gumps);

            Log.Trace( "Saving done!");
        }

        private void SaveGumps(string path, List<Gump> gumps)
        {
            string gumpsXmlPath = Path.Combine(path, "gumps.xml");

            using (XmlTextWriter xml = new XmlTextWriter(gumpsXmlPath, Encoding.UTF8)
            {
                Formatting = System.Xml.Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("gumps");

                foreach (Gump gump in gumps)
                {
                    xml.WriteStartElement("gump");
                    gump.Save(xml);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }

            using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(path, "anchors.bin"))))
                UIManager.AnchorManager.Save(writer);

            SkillsGroupManager.Save();
        }

        public static uint GumpsVersion { get; private set; }

        public List<Gump> ReadGumps()
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(ProfilePath, Username.Trim(), ServerName.Trim(), CharacterName.Trim());
            List<Gump> gumps = new List<Gump>();



            // #########################################################
            // [FILE_FIX]
            // TODO: this code is a workaround to port old macros to the new xml system.
            string skillsGroupsPath = Path.Combine(path, "skillsgroups.bin");

            if (File.Exists(skillsGroupsPath))
            {
                try
                {
                    using (BinaryReader reader = new BinaryReader(File.OpenRead(skillsGroupsPath)))
                    {
                        try
                        {
                            int version = reader.ReadInt32();

                            int groupCount = reader.ReadInt32();

                            for (int i = 0; i < groupCount; i++)
                            {
                                int entriesCount = reader.ReadInt32();
                                string groupName = reader.ReadUTF8String(reader.ReadInt32());

                                if (!SkillsGroupManager.Groups.TryGetValue(groupName, out var list) || list == null)
                                {
                                    list = new List<int>();
                                    SkillsGroupManager.Groups[groupName] = list;
                                }

                                for (int j = 0; j < entriesCount; j++)
                                {
                                    int skillIndex = reader.ReadInt32();
                                    if (skillIndex < UOFileManager.Skills.SkillsCount)
                                        list.Add(skillIndex);
                                }
                            }
                        }
                        catch
                        {

                        }
                    }

                }
                catch (Exception e)
                {
                    SkillsGroupManager.LoadDefault();
                    Log.Error( e.StackTrace);
                }

               
                SkillsGroupManager.Save();

                try
                {
                    File.Delete(skillsGroupsPath);
                }
                catch { }
            }

            string binpath = Path.Combine(path, "gumps.bin");

            if (File.Exists(binpath))
            {
                using (BinaryReader reader = new BinaryReader(File.OpenRead(binpath)))
                {
                    if (reader.BaseStream.Position + 12 < reader.BaseStream.Length)
                    {
                        GumpsVersion = reader.ReadUInt32();
                        uint empty = reader.ReadUInt32();

                        int count = reader.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            try
                            {
                                int typeLen = reader.ReadUInt16();
                                string typeName = reader.ReadUTF8String(typeLen);
                                int x = reader.ReadInt32();
                                int y = reader.ReadInt32();

                                Type type = Type.GetType(typeName, true);
                                Gump gump = (Gump) Activator.CreateInstance(type);
                                gump.Initialize();
                                gump.Restore(reader);
                                gump.X = x;
                                gump.Y = y;

                                //gump.SetInScreen();

                                if (gump.LocalSerial != 0)
                                    UIManager.SavePosition(gump.LocalSerial, new Point(x, y));

                                if (!gump.IsDisposed)
                                    gumps.Add(gump);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e.StackTrace);
                            }
                        }
                    }
                }

                SaveGumps(path, gumps);

                gumps.Clear();

                try
                {
                    File.Delete(binpath);
                }
                catch
                {

                }
            }
            // #########################################################



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
                    foreach (XmlElement xml in root.GetElementsByTagName("gump"))
                    {
                        try
                        {
                            GUMP_TYPE type = (GUMP_TYPE) int.Parse(xml.GetAttribute("type"));
                            int x = int.Parse(xml.GetAttribute("x"));
                            int y = int.Parse(xml.GetAttribute("y"));
                            uint serial = uint.Parse(xml.GetAttribute("serial"));

                            Gump gump = null;
                            switch (type)
                            {
                                case GUMP_TYPE.GT_BUFF:
                                    gump = new BuffGump();
                                    break;
                                case GUMP_TYPE.GT_CONTAINER:
                                    gump = new ContainerGump();
                                    break;
                                case GUMP_TYPE.GT_COUNTERBAR:
                                    gump = new CounterBarGump();
                                    break;
                                case GUMP_TYPE.GT_HEALTHBAR:
                                    if (CustomBarsToggled)
                                        gump = new HealthBarGumpCustom();
                                    else
                                        gump = new HealthBarGump();
                                    break;
                                case GUMP_TYPE.GT_INFOBAR:
                                    gump = new InfoBarGump();
                                    break;
                                case GUMP_TYPE.GT_JOURNAL:
                                    gump = new JournalGump();
                                    break;
                                case GUMP_TYPE.GT_MACROBUTTON:
                                    gump = new MacroButtonGump();
                                    break;
                                case GUMP_TYPE.GT_MINIMAP:
                                    gump = new MiniMapGump();
                                    break;
                                case GUMP_TYPE.GT_PAPERDOLL:
                                    gump = new PaperDollGump();
                                    break;
                                case GUMP_TYPE.GT_SKILLMENU:
                                    if (StandardSkillsGump)
                                        gump = new StandardSkillsGump();
                                    else
                                        gump = new SkillGumpAdvanced();
                                    break;
                                case GUMP_TYPE.GT_SPELLBOOK:
                                    gump = new SpellbookGump();
                                    break;
                                case GUMP_TYPE.GT_STATUSGUMP:
                                    switch (Settings.GlobalSettings.ShardType)
                                    {
                                        default:
                                        case 0: // modern

                                            gump = new StatusGumpModern();

                                            break;

                                        case 1: // old

                                            gump = new StatusGumpOld();

                                            break;

                                        case 2: // outlands

                                            gump = new StatusGumpOutlands();

                                            break;
                                    }
                                    break;
                                //case GUMP_TYPE.GT_TIPNOTICE: 
                                //    gump = new TipNoticeGump();
                                //    break;
                                case GUMP_TYPE.GT_ABILITYBUTTON:
                                    gump = new UseAbilityButtonGump();
                                    break;
                                case GUMP_TYPE.GT_SPELLBUTTON:
                                    gump = new UseSpellButtonGump();
                                    break;
                                case GUMP_TYPE.GT_SKILLBUTTON:
                                    gump = new SkillButtonGump();
                                    break;
                                case GUMP_TYPE.GT_RACIALBUTTON:
                                    gump = new RacialAbilityButton();
                                    break;
                            }

                            if (gump == null)
                                continue;

                            gump.LocalSerial = serial;
                            gump.Initialize();
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
                }
            }

          
            // load anchors
            string anchorsPath = Path.Combine(path, "anchors.bin");

            if (File.Exists(anchorsPath))
            {
                try
                {
                    using (BinaryReader reader = new BinaryReader(File.OpenRead(anchorsPath)))
                        UIManager.AnchorManager.Restore(reader, gumps);
                }
                catch (Exception e)
                {
                    Log.Error( e.StackTrace);
                }
            }


            return gumps;
        }
    }
}
