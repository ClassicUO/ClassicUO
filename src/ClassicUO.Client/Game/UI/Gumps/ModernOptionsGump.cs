using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernOptionsGump : Gump
    {
        private LeftSideMenuRightSideContent mainContent;
        private List<SettingsOption> options = new List<SettingsOption>();

        public static string SearchText { get; private set; } = string.Empty;
        public static event EventHandler SearchValueChanged;
        private Profile profile;
        private ModernOptionsGumpLanguage lang;

        private static ThemeSettings _settings;
        private static ThemeSettings Theme
        {
            get
            {
                if (_settings == null)
                {
                    _settings = (ThemeSettings)UISettings.Load<ThemeSettings>(typeof(ModernOptionsGump).ToString());
                    if (_settings == null)
                    {
                        _settings = new ThemeSettings();
                        ThemeSettings.Save<ThemeSettings>(typeof(ModernOptionsGump).ToString(), _settings);
                    }
                    else
                    { //Save changes if things have changed
                        ThemeSettings.Save<ThemeSettings>(typeof(ModernOptionsGump).ToString(), _settings);
                    }
                    return _settings;
                }
                else
                {
                    return _settings;
                }
            }
        }

        public ModernOptionsGump() : base(0, 0)
        {
            lang = Language.Instance.GetModernOptionsGumpLanguage;
            profile = ProfileManager.CurrentProfile;
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            Width = 900;
            Height = 700;

            X = (Client.Game.Window.ClientBounds.Width >> 1) - (Width >> 1);
            Y = (Client.Game.Window.ClientBounds.Height >> 1) - (Height >> 1);

            Add(new ColorBox(Width, Height, Theme.BACKGROUND) { AcceptMouseInput = true, CanMove = true, Alpha = 0.85f });

            Add(new ColorBox(Width, 40, Theme.SEARCH_BACKGROUND) { AcceptMouseInput = true, CanMove = true, Alpha = 0.85f });

            Add(new TextBox(lang.OptionsTitle, Theme.FONT, 30, null, Color.White, strokeEffect: false) { X = 10, Y = 7, AcceptMouseInput = false });

            Control c;
            Add(c = new TextBox(lang.Search, Theme.FONT, 30, null, Color.White, strokeEffect: false) { Y = 7, AcceptMouseInput = false });

            InputField search;
            Add(search = new InputField(400, 30) { X = Width - 405, Y = 5 });
            search.TextChanged += (s, e) => { SearchText = search.Text; SearchValueChanged.Raise(); };

            c.X = search.X - c.Width - 5;

            Add(mainContent = new LeftSideMenuRightSideContent(Width, Height - 40, (int)(Width * 0.23)) { Y = 40 });
            mainContent.RightArea.ToggleScrollBarVisibility(false);
            mainContent.RightArea.GetScrollBar.Dispose();
            mainContent.RightArea.GetScrollBar = null;

            ModernButton b;
            mainContent.AddToLeft(b = CategoryButton(lang.ButtonGeneral, (int)PAGE.General, mainContent.LeftWidth));
            b.IsSelected = true;
            mainContent.AddToLeft(CategoryButton(lang.ButtonSound, (int)PAGE.Sound, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonVideo, (int)PAGE.Video, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonMacros, (int)PAGE.Macros, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonTooltips, (int)PAGE.Tooltip, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonSpeech, (int)PAGE.Speech, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonCombatSpells, (int)PAGE.CombatSpells, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonCounters, (int)PAGE.Counters, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonInfobar, (int)PAGE.InfoBar, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonContainers, (int)PAGE.Containers, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonExperimental, (int)PAGE.Experimental, mainContent.LeftWidth));
            mainContent.AddToLeft(b = new ModernButton(0, 0, mainContent.LeftWidth, 40, ButtonAction.Activate, lang.ButtonIgnoreList, Theme.BUTTON_FONT_COLOR) { ButtonParameter = 999 });
            b.MouseUp += (s, e) =>
            {
                UIManager.GetGump<IgnoreManagerGump>()?.Dispose();
                UIManager.Add(new IgnoreManagerGump());
            };
            mainContent.AddToLeft(CategoryButton(lang.ButtonNameplates, (int)PAGE.NameplateOptions, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonCooldowns, (int)PAGE.TUOCooldowns, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton(lang.ButtonTazUO, (int)PAGE.TUOOptions, mainContent.LeftWidth));

            BuildGeneral();
            BuildSound();
            BuildVideo();
            BuildMacros();
            BuildTooltips();
            BuildSpeech();
            BuildCombatSpells();
            BuildCounters();
            BuildInfoBar();
            BuildContainers();
            BuildExperimental();
            BuildNameplates();
            BuildCooldowns();
            BuildTazUO();

            foreach (SettingsOption option in options)
            {
                mainContent.AddToRight(option.FullControl, false, (int)option.OptionsPage);
            }

            ChangePage((int)PAGE.General);
        }

        private void BuildGeneral()
        {
            LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(mainContent.RightWidth, mainContent.Height, (int)(mainContent.RightWidth * 0.3));
            Control c;
            int page;

            #region General
            page = ((int)PAGE.General + 1000);
            content.AddToLeft(SubCategoryButton(lang.ButtonGeneral, page, content.LeftWidth));

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HighlightObjects, isChecked: profile.HighlightGameObjects, valueChanged: (b) => { profile.HighlightGameObjects = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.Pathfinding, isChecked: profile.EnablePathfind, valueChanged: (b) => { profile.EnablePathfind = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShiftPathfinding, isChecked: profile.UseShiftToPathfind, valueChanged: (b) => { profile.UseShiftToPathfind = b; }), true, page);
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.SingleClickPathfind, isChecked: profile.PathfindSingleClick, valueChanged: (b) => { profile.PathfindSingleClick = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AlwaysRun, isChecked: profile.AlwaysRun, valueChanged: (b) => { profile.AlwaysRun = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.RunUnlessHidden, isChecked: profile.AlwaysRunUnlessHidden, valueChanged: (b) => { profile.AlwaysRunUnlessHidden = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AutoOpenDoors, isChecked: profile.AutoOpenDoors, valueChanged: (b) => { profile.AutoOpenDoors = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AutoOpenPathfinding, isChecked: profile.SmoothDoors, valueChanged: (b) => { profile.SmoothDoors = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AutoOpenCorpse, isChecked: profile.AutoOpenCorpses, valueChanged: (b) => { profile.AutoOpenCorpses = b; }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetGeneral.CorpseOpenDistance, 0, Theme.SLIDER_WIDTH, 0, 5, profile.AutoOpenCorpseRange, (r) => { profile.AutoOpenCorpseRange = r; }), true, page);
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.CorpseSkipEmpty, isChecked: profile.SkipEmptyCorpse, valueChanged: (b) => { profile.SkipEmptyCorpse = b; }), true, page);
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.CorpseOpenOptions, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.CorpseOptNone, lang.GetGeneral.CorpseOptNotTarg, lang.GetGeneral.CorpseOptNotHiding, lang.GetGeneral.CorpseOptBoth }, profile.CorpseOpenOptions, (s, n) => { profile.CorpseOpenOptions = s; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.OutRangeColor, isChecked: profile.NoColorObjectsOutOfRange, valueChanged: (b) => { profile.NoColorObjectsOutOfRange = b; }), true, page);

            content.BlankLine();

            content.AddToRight(c = new CheckboxWithLabel(lang.GetGeneral.SallosEasyGrab, isChecked: profile.SallosEasyGrab, valueChanged: (b) => { profile.SallosEasyGrab = b; }), true, page);
            c.SetTooltip(lang.GetGeneral.SallosTooltip);

            if (Client.Version > ClientVersion.CV_70796)
            {
                content.BlankLine();
                content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShowHouseContent, isChecked: profile.ShowHouseContent, valueChanged: (b) => { profile.ShowHouseContent = b; }), true, page);
            }

            if (Client.Version >= ClientVersion.CV_7090)
            {
                content.BlankLine();
                content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.SmoothBoat, isChecked: profile.UseSmoothBoatMovement, valueChanged: (b) => { profile.UseSmoothBoatMovement = b; }), true, page);
            }

            content.BlankLine();
            #endregion

            #region Mobiles
            page = ((int)PAGE.General + 1001);
            content.AddToLeft(SubCategoryButton(lang.ButtonMobiles, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShowMobileHP, isChecked: profile.ShowMobilesHP, valueChanged: (b) => { profile.ShowMobilesHP = b; }), true, page);
            content.Indent();
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.MobileHPType, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.HPTypePerc, lang.GetGeneral.HPTypeBar, lang.GetGeneral.HPTypeNBoth }, profile.MobileHPType, (s, n) => { profile.MobileHPType = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.HPShowWhen, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.HPShowWhen_Always, lang.GetGeneral.HPShowWhen_Less100, lang.GetGeneral.HPShowWhen_Smart }, profile.MobileHPShowWhen, (s, n) => { profile.MobileHPShowWhen = s; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HighlightPoisoned, isChecked: profile.HighlightMobilesByPoisoned, valueChanged: (b) => { profile.HighlightMobilesByPoisoned = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetGeneral.PoisonHighlightColor, profile.PoisonHue, (h) => { profile.PoisonHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HighlightPara, isChecked: profile.HighlightMobilesByParalize, valueChanged: (b) => { profile.HighlightMobilesByParalize = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetGeneral.ParaHighlightColor, profile.ParalyzedHue, (h) => { profile.ParalyzedHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HighlightInvul, isChecked: profile.HighlightMobilesByInvul, valueChanged: (b) => { profile.HighlightMobilesByInvul = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetGeneral.InvulHighlightColor, profile.InvulnerableHue, (h) => { profile.InvulnerableHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.IncomingMobiles, isChecked: profile.ShowNewMobileNameIncoming, valueChanged: (b) => { profile.ShowNewMobileNameIncoming = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.IncomingCorpses, isChecked: profile.ShowNewCorpseNameIncoming, valueChanged: (b) => { profile.ShowNewCorpseNameIncoming = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.AuraUnderFeet, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.AuraOptDisabled, lang.GetGeneral.AuroOptWarmode, lang.GetGeneral.AuraOptCtrlShift, lang.GetGeneral.AuraOptAlways }, profile.AuraUnderFeetType, (s, n) => { profile.AuraUnderFeetType = s; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AuraForParty, isChecked: profile.PartyAura, valueChanged: (b) => { profile.PartyAura = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetGeneral.AuraPartyColor, profile.PartyAuraHue, (h) => { profile.PartyAuraHue = h; }), true, page);
            content.RemoveIndent();
            content.RemoveIndent();
            #endregion

            #region Gumps & Context
            page = ((int)PAGE.General + 1002);
            content.AddToLeft(SubCategoryButton(lang.ButtonGumpContext, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.DisableTopMenu, isChecked: profile.TopbarGumpIsDisabled, valueChanged: (b) => { profile.TopbarGumpIsDisabled = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AltForAnchorsGumps, isChecked: profile.HoldDownKeyAltToCloseAnchored, valueChanged: (b) => { profile.HoldDownKeyAltToCloseAnchored = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AltToMoveGumps, isChecked: profile.HoldAltToMoveGumps, valueChanged: (b) => { profile.HoldAltToMoveGumps = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.CloseEntireAnchorWithRClick, isChecked: profile.CloseAllAnchoredGumpsInGroupWithRightClick, valueChanged: (b) => { profile.CloseAllAnchoredGumpsInGroupWithRightClick = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.OriginalSkillsGump, isChecked: profile.StandardSkillsGump, valueChanged: (b) => { profile.StandardSkillsGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.OldStatusGump, isChecked: profile.UseOldStatusGump, valueChanged: (b) => { profile.UseOldStatusGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.PartyInviteGump, isChecked: profile.PartyInviteGump, valueChanged: (b) => { profile.PartyInviteGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ModernHealthBars, isChecked: profile.CustomBarsToggled, valueChanged: (b) => { profile.CustomBarsToggled = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ModernHPBlackBG, isChecked: profile.CBBlackBGToggled, valueChanged: (b) => { profile.CBBlackBGToggled = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.SaveHPBars, isChecked: profile.SaveHealthbars, valueChanged: (b) => { profile.SaveHealthbars = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.CloseHPGumpsWhen, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.CloseHPOptDisable, lang.GetGeneral.CloseHPOptOOR, lang.GetGeneral.CloseHPOptDead, lang.GetGeneral.CloseHPOptBoth }, profile.CloseHealthBarType, (s, n) => { profile.CloseHealthBarType = s; }), true, page);

            content.BlankLine();

            content.AddToRight(c = new ComboBoxWithLabel(lang.GetGeneral.GridLoot, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.GridLootOptDisable, lang.GetGeneral.GridLootOptOnly, lang.GetGeneral.GridLootOptBoth }, profile.GridLootType, (s, n) => { profile.GridLootType = s; }), true, page);
            c.SetTooltip(lang.GetGeneral.GridLootTooltip);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShiftContext, isChecked: profile.HoldShiftForContext, valueChanged: (b) => { profile.HoldShiftForContext = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShiftSplit, isChecked: profile.HoldShiftToSplitStack, valueChanged: (b) => { profile.HoldShiftToSplitStack = b; }), true, page);
            #endregion

            #region Misc
            page = ((int)PAGE.General + 1003);
            content.AddToLeft(SubCategoryButton(lang.ButtonMisc, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.EnableCOT, isChecked: profile.UseCircleOfTransparency, valueChanged: (b) => { profile.UseCircleOfTransparency = b; }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetGeneral.COTDistance, 0, Theme.SLIDER_WIDTH, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, profile.CircleOfTransparencyRadius, (r) => { profile.CircleOfTransparencyRadius = r; }), true, page);
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.COTType, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.COTTypeOptFull, lang.GetGeneral.COTTypeOptGrad, lang.GetGeneral.COTTypeOptModern }, profile.CircleOfTransparencyType, (s, n) => { profile.CircleOfTransparencyType = s; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HideScreenshotMessage, isChecked: profile.HideScreenshotStoredInMessage, valueChanged: (b) => { profile.HideScreenshotStoredInMessage = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ObjFade, isChecked: profile.UseObjectsFading, valueChanged: (b) => { profile.UseObjectsFading = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.TextFade, isChecked: profile.TextFading, valueChanged: (b) => { profile.TextFading = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.CursorRange, isChecked: profile.ShowTargetRangeIndicator, valueChanged: (b) => { profile.ShowTargetRangeIndicator = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.DragSelectHP, isChecked: profile.EnableDragSelect, valueChanged: (b) => { profile.EnableDragSelect = b; }), true, page);
            content.Indent();
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.DragKeyMod, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt }, profile.DragSelectModifierKey, (s, n) => { profile.DragSelectModifierKey = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.DragPlayersOnly, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt }, profile.DragSelect_PlayersModifier, (s, n) => { profile.DragSelect_PlayersModifier = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.DragMobsOnly, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt }, profile.DragSelect_MonstersModifier, (s, n) => { profile.DragSelect_MonstersModifier = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.DragNameplatesOnly, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt }, profile.DragSelect_NameplateModifier, (s, n) => { profile.DragSelect_NameplateModifier = s; }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetGeneral.DragX, 0, Theme.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width, profile.DragSelectStartX, (r) => { profile.DragSelectStartX = r; }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetGeneral.DragY, 0, Theme.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width, profile.DragSelectStartY, (r) => { profile.DragSelectStartY = r; }), true, page);
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.DragAnchored, isChecked: profile.DragSelectAsAnchor, valueChanged: (b) => { profile.DragSelectAsAnchor = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShowStatsChangedMsg, isChecked: profile.ShowStatsChangedMessage, valueChanged: (b) => { profile.ShowStatsChangedMessage = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShowSkillsChangedMsg, isChecked: profile.ShowSkillsChangedMessage, valueChanged: (b) => { profile.ShowStatsChangedMessage = b; }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetGeneral.ChangeVolume, 0, Theme.SLIDER_WIDTH, 0, 100, profile.ShowSkillsChangedDeltaValue, (r) => { profile.ShowSkillsChangedDeltaValue = r; }), true, page);
            content.RemoveIndent();
            #endregion

            #region Terrain and statics
            page = ((int)PAGE.General + 1004);
            content.AddToLeft(SubCategoryButton(lang.ButtonTerrainStatics, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HideRoof, isChecked: !profile.DrawRoofs, valueChanged: (b) => { profile.DrawRoofs = !b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.TreesToStump, isChecked: profile.TreeToStumps, valueChanged: (b) => { profile.TreeToStumps = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HideVegetation, isChecked: profile.HideVegetation, valueChanged: (b) => { profile.HideVegetation = b; }), true, page);

            //content.BlankLine();

            //content.AddToRight(new CheckboxWithLabel("Mark cave tiles", isChecked: profile.EnableCaveBorder, valueChanged: (b) => { profile.EnableCaveBorder = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel(lang.GetGeneral.MagicFieldType, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetGeneral.MagicFieldOpt_Normal, lang.GetGeneral.MagicFieldOpt_Static, lang.GetGeneral.MagicFieldOpt_Tile }, profile.FieldsType, (s, n) => { profile.FieldsType = s; }), true, page);

            #endregion

            options.Add(new SettingsOption(
                    "",
                    content,
                    mainContent.RightWidth,
                    PAGE.General
                ));
        }

        private void BuildSound()
        {
            SettingsOption s;

            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSound.EnableSound, 0, profile.EnableSound, (b) => { profile.EnableSound = b; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new SliderWithLabel(lang.GetSound.SharedVolume, 0, Theme.SLIDER_WIDTH, 0, 100, profile.SoundVolume, (i) => { profile.SoundVolume = i; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSound.EnableMusic, 0, profile.EnableMusic, (b) => { profile.EnableMusic = b; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new SliderWithLabel(lang.GetSound.SharedVolume, 0, Theme.SLIDER_WIDTH, 0, 100, profile.MusicVolume, (i) => { profile.MusicVolume = i; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSound.LoginMusic, 0, Settings.GlobalSettings.LoginMusic, (b) => { Settings.GlobalSettings.LoginMusic = b; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new SliderWithLabel(lang.GetSound.SharedVolume, 0, Theme.SLIDER_WIDTH, 0, 100, Settings.GlobalSettings.LoginMusicVolume, (i) => { Settings.GlobalSettings.LoginMusicVolume = i; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSound.PlayFootsteps, 0, profile.EnableFootstepsSound, (b) => { profile.EnableFootstepsSound = b; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSound.CombatMusic, 0, profile.EnableCombatMusic, (b) => { profile.EnableCombatMusic = b; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSound.BackgroundMusic, 0, profile.ReproduceSoundsInBackground, (b) => { profile.ReproduceSoundsInBackground = b; }),
                    mainContent.RightWidth,
                    PAGE.Sound
                ));
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildVideo()
        {
            LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(mainContent.RightWidth, mainContent.Height, (int)(mainContent.RightWidth * 0.3));
            #region Game window
            int page = ((int)PAGE.Video + 1000);
            content.AddToLeft(SubCategoryButton(lang.ButtonGameWindow, page, content.LeftWidth));

            content.AddToRight(new SliderWithLabel(lang.GetVideo.FPSCap, 0, Theme.SLIDER_WIDTH, Constants.MIN_FPS, Constants.MAX_FPS, Settings.GlobalSettings.FPS, (r) => { Settings.GlobalSettings.FPS = r; Client.Game.SetRefreshRate(r); }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.BackgroundFPS, isChecked: profile.ReduceFPSWhenInactive, valueChanged: (b) => { profile.ReduceFPSWhenInactive = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.FullsizeViewport, isChecked: profile.GameWindowFullSize, valueChanged: (b) =>
            {
                profile.GameWindowFullSize = b;
                if (b)
                {
                    UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));
                    UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(new Point(-5, -5));
                    profile.GameWindowPosition = new Point(-5, -5);
                }
                else
                {
                    UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(new Point(600, 480));
                    UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(new Point(25, 25));
                    profile.GameWindowPosition = new Point(25, 25);
                }
            }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.FullScreen, isChecked: profile.WindowBorderless, valueChanged: (b) => { profile.WindowBorderless = b; Client.Game.SetWindowBorderless(b); }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.LockViewport, isChecked: profile.GameWindowLock, valueChanged: (b) => { profile.GameWindowLock = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetVideo.ViewportX, 0, Theme.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Width, profile.GameWindowPosition.X, (r) => { profile.GameWindowPosition = new Point(r, profile.GameWindowPosition.Y); UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(profile.GameWindowPosition); }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetVideo.ViewportY, 0, Theme.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Height, profile.GameWindowPosition.Y, (r) => { profile.GameWindowPosition = new Point(profile.GameWindowPosition.X, r); UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(profile.GameWindowPosition); }), true, page);

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetVideo.ViewportW, 0, Theme.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Width, profile.GameWindowSize.X, (r) => { profile.GameWindowSize = new Point(r, profile.GameWindowSize.Y); UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(profile.GameWindowSize); }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetVideo.ViewportH, 0, Theme.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Height, profile.GameWindowSize.Y, (r) => { profile.GameWindowSize = new Point(profile.GameWindowSize.X, r); UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(profile.GameWindowSize); }), true, page);

            #endregion

            #region Zoom
            page = ((int)PAGE.Video + 1001);
            content.AddToLeft(SubCategoryButton(lang.ButtonZoom, page, content.LeftWidth));
            content.ResetRightSide();

            var cameraZoomCount = (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.ZoomMin) / Client.Game.Scene.Camera.ZoomStep);
            var cameraZoomIndex = cameraZoomCount - (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.Zoom) / Client.Game.Scene.Camera.ZoomStep);
            content.AddToRight(new SliderWithLabel(lang.GetVideo.DefaultZoom, 0, Theme.SLIDER_WIDTH, 0, cameraZoomCount, cameraZoomIndex, (r) => { profile.DefaultScale = Client.Game.Scene.Camera.Zoom = (r * Client.Game.Scene.Camera.ZoomStep) + Client.Game.Scene.Camera.ZoomMin; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.ZoomWheel, isChecked: profile.EnableMousewheelScaleZoom, valueChanged: (b) => { profile.EnableMousewheelScaleZoom = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.ReturnDefaultZoom, isChecked: profile.RestoreScaleAfterUnpressCtrl, valueChanged: (b) => { profile.RestoreScaleAfterUnpressCtrl = b; }), true, page);
            #endregion

            #region Lighting
            page = ((int)PAGE.Video + 1002);
            content.AddToLeft(SubCategoryButton(lang.ButtonLighting, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.AltLights, isChecked: profile.UseAlternativeLights, valueChanged: (b) => { profile.UseAlternativeLights = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.CustomLLevel, isChecked: profile.UseCustomLightLevel, valueChanged: (b) =>
            {
                profile.UseCustomLightLevel = b;
                if (b)
                {
                    World.Light.Overall = profile.LightLevelType == 1 ? Math.Min(World.Light.RealOverall, profile.LightLevel) : profile.LightLevel;
                    World.Light.Personal = 0;
                }
                else
                {
                    World.Light.Overall = World.Light.RealOverall;
                    World.Light.Personal = World.Light.RealPersonal;
                }
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetVideo.Level, 0, Theme.SLIDER_WIDTH, 0, 0x1E, 0x1E - profile.LightLevel, (r) =>
            {
                profile.LightLevel = (byte)(0x1E - r);
                if (profile.UseCustomLightLevel)
                {
                    World.Light.Overall = profile.LightLevelType == 1 ? Math.Min(World.Light.RealOverall, profile.LightLevel) : profile.LightLevel;
                    World.Light.Personal = 0;
                }
                else
                {
                    World.Light.Overall = World.Light.RealOverall;
                    World.Light.Personal = World.Light.RealPersonal;
                }
            }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel(lang.GetVideo.LightType, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetVideo.LightType_Absolute, lang.GetVideo.LightType_Minimum }, profile.LightLevelType, (s, n) => { profile.LightLevelType = s; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.DarkNight, isChecked: profile.UseDarkNights, valueChanged: (b) => { profile.UseDarkNights = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.ColoredLight, isChecked: profile.UseColoredLights, valueChanged: (b) => { profile.UseColoredLights = b; }), true, page);

            #endregion

            #region Misc
            page = ((int)PAGE.Video + 1003);
            content.AddToLeft(SubCategoryButton(lang.ButtonMisc, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.EnableDeathScreen, isChecked: profile.EnableDeathScreen, valueChanged: (b) => { profile.EnableDeathScreen = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.BWDead, isChecked: profile.EnableBlackWhiteEffect, valueChanged: (b) => { profile.EnableBlackWhiteEffect = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.MouseThread, isChecked: Settings.GlobalSettings.RunMouseInASeparateThread, valueChanged: (b) => { Settings.GlobalSettings.RunMouseInASeparateThread = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.TargetAura, isChecked: profile.AuraOnMouse, valueChanged: (b) => { profile.AuraOnMouse = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.AnimWater, isChecked: profile.AnimatedWaterEffect, valueChanged: (b) => { profile.AnimatedWaterEffect = b; }), true, page);
            #endregion

            #region Shadows
            page = ((int)PAGE.Video + 1004);
            content.AddToLeft(SubCategoryButton(lang.ButtonShadows, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.EnableShadows, isChecked: profile.ShadowsEnabled, valueChanged: (b) => { profile.ShadowsEnabled = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.RockTreeShadows, isChecked: profile.ShadowsStatics, valueChanged: (b) => { profile.ShadowsStatics = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetVideo.TerrainShadowLevel, 0, Theme.SLIDER_WIDTH, Constants.MIN_TERRAIN_SHADOWS_LEVEL, Constants.MAX_TERRAIN_SHADOWS_LEVEL, profile.TerrainShadowsLevel, (r) => { profile.TerrainShadowsLevel = r; }), true, page);
            #endregion

            options.Add(new SettingsOption(
                    "",
                    content,
                    mainContent.RightWidth,
                    PAGE.Video
                ));
        }

        private void BuildMacros()
        {
            LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(mainContent.RightWidth, mainContent.Height, (int)(mainContent.RightWidth * 0.3));
            int page = ((int)PAGE.Macros + 1000);

            #region New Macro
            ModernButton b;
            content.AddToLeft(b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetMacros.NewMacro, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false });

            b.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new EntryDialog
                (
                    250,
                    150,
                    ResGumps.MacroName,
                    name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }

                        MacroManager manager = Client.Game.GetScene<GameScene>().Macros;

                        if (manager.FindMacro(name) != null)
                        {
                            return;
                        }

                        ModernButton nb;

                        MacroControl macroControl = new MacroControl(name);

                        content.AddToLeft(nb = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, name, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page + 1 + content.LeftArea.Children.Count, Tag = macroControl.Macro });
                        content.ResetRightSide();
                        content.AddToRight(macroControl, true, nb.ButtonParameter);

                        nb.IsSelected = true;
                        content.ActivePage = nb.ButtonParameter;

                        manager.PushToBack(macroControl.Macro);

                        nb.DragBegin += (sss, eee) =>
                        {
                            ModernButton mupNiceButton = (ModernButton)sss;

                            Macro m = mupNiceButton.Tag as Macro;

                            if (m == null)
                            {
                                return;
                            }

                            if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                            {
                                return;
                            }

                            UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == m)?.Dispose();

                            MacroButtonGump macroButtonGump = new MacroButtonGump(m, Mouse.Position.X, Mouse.Position.Y);

                            macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                            macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);

                            UIManager.Add(macroButtonGump);

                            UIManager.AttemptDragControl(macroButtonGump, true);
                        };
                    }
                )
                {
                    CanCloseWithRightClick = true
                };

                UIManager.Add(dialog);
            };
            #endregion

            #region Delete Macro
            page = ((int)PAGE.Macros + 1001);
            content.AddToLeft(b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetMacros.DelMacro, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false });

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    QuestionGump dialog = new QuestionGump
                    (
                        ResGumps.MacroDeleteConfirmation,
                        b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (nb.Tag is Macro macro)
                            {
                                UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == macro)?.Dispose();
                                Client.Game.GetScene<GameScene>().Macros.Remove(macro);
                                nb.Dispose();
                            }
                        }
                    );

                    UIManager.Add(dialog);
                }
            };
            #endregion

            content.AddToLeft(new Line(0, 0, content.LeftWidth, 1, Color.Gray.PackedValue));

            #region Macros
            page = ((int)PAGE.Macros + 1002);
            MacroManager macroManager = Client.Game.GetScene<GameScene>().Macros;
            for (Macro macro = (Macro)macroManager.Items; macro != null; macro = (Macro)macro.Next)
            {
                content.AddToLeft(b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, macro.Name, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page + 1 + content.LeftArea.Children.Count, Tag = macro });

                b.DragBegin += (sss, eee) =>
                {
                    ModernButton mupNiceButton = (ModernButton)sss;

                    Macro m = mupNiceButton.Tag as Macro;

                    if (m == null)
                    {
                        return;
                    }

                    if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                    {
                        return;
                    }

                    UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == m)?.Dispose();

                    MacroButtonGump macroButtonGump = new MacroButtonGump(m, Mouse.Position.X, Mouse.Position.Y);

                    macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                    macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);

                    UIManager.Add(macroButtonGump);

                    UIManager.AttemptDragControl(macroButtonGump, true);
                };

                content.ResetRightSide();
                content.AddToRight(new MacroControl(macro.Name), true, b.ButtonParameter);
            }

            b.IsSelected = true;
            content.ActivePage = b.ButtonParameter;
            #endregion

            options.Add(new SettingsOption(
                    "",
                    content,
                    mainContent.RightWidth,
                    PAGE.Macros
                ));
        }

        private void BuildTooltips()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetToolTips.EnableToolTips, 0, profile.UseTooltip, (b) => { profile.UseTooltip = b; }),
                    mainContent.RightWidth,
                    PAGE.Tooltip
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new SliderWithLabel(lang.GetToolTips.ToolTipDelay, 0, Theme.SLIDER_WIDTH, 0, 1000, profile.TooltipDelayBeforeDisplay, (i) => { profile.TooltipDelayBeforeDisplay = i; }),
                    mainContent.RightWidth,
                    PAGE.Tooltip
                ));
            PositionHelper.PositionControl(s.FullControl);

            options.Add(s = new SettingsOption(
                    "",
                    new SliderWithLabel(lang.GetToolTips.ToolTipBG, 0, Theme.SLIDER_WIDTH, 0, 100, profile.TooltipBackgroundOpacity, (i) => { profile.TooltipBackgroundOpacity = i; }),
                    mainContent.RightWidth,
                    PAGE.Tooltip
                ));
            PositionHelper.PositionControl(s.FullControl);

            options.Add(s = new SettingsOption(
                    "",
                    new ModernColorPickerWithLabel(lang.GetToolTips.ToolTipFont, profile.TooltipTextHue, (h) => { profile.TooltipTextHue = h; }),
                    mainContent.RightWidth,
                    PAGE.Tooltip
                ));
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildSpeech()
        {
            SettingsOption s, ss;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.ScaleSpeechDelay, 0, profile.ScaleSpeechDelay, (b) => { profile.ScaleSpeechDelay = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new SliderWithLabel(lang.GetSpeech.SpeechDelay, 0, Theme.SLIDER_WIDTH, 0, 1000, profile.SpeechDelay, (i) => { profile.SpeechDelay = i; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();


            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.SaveJournalE, 0, profile.SaveJournalToFile, (b) => { profile.SaveJournalToFile = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.ChatEnterActivation, 0, profile.ActivateChatAfterEnter, (b) => { profile.ActivateChatAfterEnter = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.ChatEnterSpecial, 0, profile.ActivateChatAdditionalButtons, (b) => { profile.ActivateChatAdditionalButtons = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.ShiftEnterChat, 0, profile.ActivateChatShiftEnterSupport, (b) => { profile.ActivateChatShiftEnterSupport = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();


            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.ChatGradient, 0, profile.HideChatGradient, (b) => { profile.HideChatGradient = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.HideGuildChat, 0, profile.IgnoreGuildMessages, (b) => { profile.IgnoreGuildMessages = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetSpeech.HideAllianceChat, 0, profile.IgnoreAllianceMessages, (b) => { profile.IgnoreAllianceMessages = b; }),
                    mainContent.RightWidth,
                    PAGE.Speech
                ));
            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.SpeechColor, profile.SpeechHue, (h) => { profile.SpeechHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.YellColor, profile.YellHue, (h) => { profile.YellHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.PartyColor, profile.PartyMessageHue, (h) => { profile.PartyMessageHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.AllianceColor, profile.AllyMessageHue, (h) => { profile.AllyMessageHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.EmoteColor, profile.EmoteHue, (h) => { profile.EmoteHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.WhisperColor, profile.WhisperHue, (h) => { profile.WhisperHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.GuildColor, profile.GuildMessageHue, (h) => { profile.GuildMessageHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetSpeech.CharColor, profile.ChatMessageHue, (h) => { profile.ChatMessageHue = h; }),
                mainContent.RightWidth,
                PAGE.Speech
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);
        }

        private void BuildCombatSpells()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCombatSpells.HoldTabForCombat, 0, profile.HoldDownKeyTab, (b) => { profile.HoldDownKeyTab = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCombatSpells.QueryBeforeAttack, 0, profile.EnabledCriminalActionQuery, (b) => { profile.EnabledCriminalActionQuery = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCombatSpells.QueryBeforeBeneficial, 0, profile.EnabledBeneficialCriminalActionQuery, (b) => { profile.EnabledBeneficialCriminalActionQuery = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCombatSpells.EnableOverheadSpellFormat, 0, profile.EnabledSpellFormat, (b) => { profile.EnabledSpellFormat = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCombatSpells.EnableOverheadSpellHue, 0, profile.EnabledSpellHue, (b) => { profile.EnabledSpellHue = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCombatSpells.SingleClickForSpellIcons, 0, profile.CastSpellsByOneClick, (b) => { profile.CastSpellsByOneClick = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCombatSpells.ShowBuffDurationOnOldStyleBuffBar, 0, profile.BuffBarTime, (b) => { profile.BuffBarTime = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            Control c;
            options.Add(s = new SettingsOption(
                    "",
                    c = new CheckboxWithLabel(lang.GetCombatSpells.EnableFastSpellHotkeyAssigning, 0, profile.FastSpellsAssign, (b) => { profile.FastSpellsAssign = b; }),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                ));
            PositionHelper.PositionControl(s.FullControl);
            c.SetTooltip(lang.GetCombatSpells.TooltipFastSpellAssign);

            PositionHelper.BlankLine();

            SettingsOption ss;
            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.InnocentColor, profile.InnocentHue, (h) => { profile.InnocentHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.BeneficialSpell, profile.BeneficHue, (h) => { profile.BeneficHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.FriendColor, profile.FriendHue, (h) => { profile.FriendHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.HarmfulSpell, profile.HarmfulHue, (h) => { profile.HarmfulHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.Criminal, profile.CriminalHue, (h) => { profile.CriminalHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.NeutralSpell, profile.NeutralHue, (h) => { profile.NeutralHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.CanBeAttackedHue, profile.CanAttackHue, (h) => { profile.CanAttackHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.Murderer, profile.MurdererHue, (h) => { profile.MurdererHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);
            ss = s;

            options.Add(s = new SettingsOption(
                "",
                new ModernColorPickerWithLabel(lang.GetCombatSpells.Enemy, profile.EnemyHue, (h) => { profile.EnemyHue = h; }),
                mainContent.RightWidth,
                PAGE.CombatSpells
            ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.BlankLine();

            InputField spellFormat;
            options.Add(s = new SettingsOption(
                    lang.GetCombatSpells.SpellOverheadFormat,
                    spellFormat = new InputField(200, 40),
                    mainContent.RightWidth,
                    PAGE.CombatSpells
                    ));
            spellFormat.SetText(profile.SpellDisplayFormat);
            spellFormat.TextChanged += (s, e) =>
            {
                profile.SpellDisplayFormat = spellFormat.Text;
            };
            PositionHelper.PositionControl(s.FullControl);
            s.FullControl.SetTooltip(lang.GetCombatSpells.TooltipSpellFormat);
        }

        private void BuildCounters()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCounters.EnableCounters, 0, profile.CounterBarEnabled, (b) =>
                    {
                        profile.CounterBarEnabled = b;
                        CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

                        if (b)
                        {
                            if (counterGump != null)
                            {
                                counterGump.IsEnabled = counterGump.IsVisible = b;
                            }
                            else
                            {
                                UIManager.Add(counterGump = new CounterBarGump(200, 200));
                            }
                        }
                        else
                        {
                            if (counterGump != null)
                            {
                                counterGump.IsEnabled = counterGump.IsVisible = b;
                            }
                        }

                        counterGump?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
                    }),
                    mainContent.RightWidth,
                    PAGE.Counters
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCounters.HighlightItemsOnUse, 0, profile.CounterBarHighlightOnUse, (b) => { profile.CounterBarHighlightOnUse = b; }),
                    mainContent.RightWidth,
                    PAGE.Counters
                ));
            PositionHelper.PositionControl(s.FullControl);

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetCounters.AbbreviatedValues, 0, profile.CounterBarDisplayAbbreviatedAmount, (b) => { profile.CounterBarDisplayAbbreviatedAmount = b; }),
                    mainContent.RightWidth,
                    PAGE.Counters
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    lang.GetCounters.AbbreviateIfAmountExceeds,
                    new InputField(100, 40, text: profile.CounterBarAbbreviatedAmount.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                    {
                        if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                        {
                            profile.CounterBarAbbreviatedAmount = v;
                        }
                    }),
                    mainContent.RightWidth,
                    PAGE.Counters
                    ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetCounters.HighlightRedWhenAmountIsLow, 0, profile.CounterBarHighlightOnAmount, (b) => { profile.CounterBarHighlightOnAmount = b; }),
                mainContent.RightWidth,
                PAGE.Counters
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                lang.GetCounters.HighlightRedIfAmountIsBelow,
                new InputField(100, 40, text: profile.CounterBarHighlightAmount.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                {
                    if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                    {
                        profile.CounterBarHighlightAmount = v;
                    }
                }),
                mainContent.RightWidth,
                PAGE.Counters
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.RemoveIndent();

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                lang.GetCounters.CounterLayout,
                new Area(false),
                mainContent.RightWidth,
                PAGE.Counters
                ));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                "",
                new SliderWithLabel(lang.GetCounters.GridSize, 0, Theme.SLIDER_WIDTH, 30, 100, profile.CounterBarCellSize, (v) =>
                {
                    profile.CounterBarCellSize = v;
                    UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
                }),
                mainContent.RightWidth,
                PAGE.Counters
                ));
            PositionHelper.PositionControl(s.FullControl);

            options.Add(s = new SettingsOption(
                lang.GetCounters.Rows,
                new InputField(100, 40, text: profile.CounterBarRows.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                {
                    if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                    {
                        profile.CounterBarRows = v;
                        UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
                    }
                }),
                mainContent.RightWidth,
                PAGE.Counters
                ));
            PositionHelper.PositionControl(s.FullControl);
            SettingsOption ss = s;

            options.Add(s = new SettingsOption(
                lang.GetCounters.Columns,
                new InputField(100, 40, text: profile.CounterBarColumns.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                {
                    if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                    {
                        profile.CounterBarColumns = v;
                        UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
                    }
                }),
                mainContent.RightWidth,
                PAGE.Counters
                ));
            PositionHelper.PositionExact(s.FullControl, ss.FullControl.X + ss.FullControl.Width + 30, ss.FullControl.Y);
        }

        private void BuildInfoBar()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetInfoBars.ShowInfoBar, 0, profile.ShowInfoBar, (b) =>
                    {
                        profile.ShowInfoBar = b;
                        InfoBarGump infoBarGump = UIManager.GetGump<InfoBarGump>();

                        if (b)
                        {
                            if (infoBarGump == null)
                            {
                                UIManager.Add(new InfoBarGump { X = 300, Y = 300 });
                            }
                            else
                            {
                                infoBarGump.ResetItems();
                                infoBarGump.SetInScreen();
                            }
                        }
                        else
                        {
                            infoBarGump?.Dispose();
                        }
                    }),
                    mainContent.RightWidth,
                    PAGE.InfoBar
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                "",
                new ComboBoxWithLabel(lang.GetInfoBars.HighlightType, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetInfoBars.HighLightOpt_TextColor, lang.GetInfoBars.HighLightOpt_ColoredBars }, profile.InfoBarHighlightType, (i, s) => { profile.InfoBarHighlightType = i; }),
                mainContent.RightWidth,
                PAGE.InfoBar
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();

            PositionHelper.BlankLine();

            DataBox infoBarItems = new DataBox(0, 0, 0, 0) { AcceptMouseInput = true };

            ModernButton addItem;
            options.Add(s = new SettingsOption(
                "",
                addItem = new ModernButton(0, 0, 150, 40, ButtonAction.Activate, lang.GetInfoBars.AddItem, Theme.BUTTON_FONT_COLOR) { ButtonParameter = -1, IsSelectable = true, IsSelected = true },
                mainContent.RightWidth,
                PAGE.InfoBar
            ));
            addItem.MouseUp += (s, e) =>
            {
                InfoBarItem ibi;
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(ibi = new InfoBarItem("HP", InfoBarVars.HP, 0x3B9));
                infoBarItems.Add(ibbc);
                infoBarItems.ReArrangeChildren();
                infoBarItems.ForceSizeUpdate();
                infoBarItems.Parent?.ForceSizeUpdate();
                Client.Game.GetScene<GameScene>().InfoBars?.AddItem(ibi);
                UIManager.GetGump<InfoBarGump>()?.ResetItems();
            };
            PositionHelper.PositionControl(s.FullControl);
            SettingsOption ss = s;
            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(lang.GetInfoBars.Label, new Area(false), mainContent.RightWidth, PAGE.InfoBar));
            PositionHelper.PositionExact(s.FullControl, ss.FullControl.X, ss.FullControl.Y + ss.FullControl.Height + 40);
            ss = s;

            options.Add(s = new SettingsOption(lang.GetInfoBars.Color, new Area(false), mainContent.RightWidth, PAGE.InfoBar));
            PositionHelper.PositionExact(s.FullControl, ss.FullControl.X + 150, ss.FullControl.Y);
            ss = s;

            options.Add(s = new SettingsOption(lang.GetInfoBars.Data, new Area(false), mainContent.RightWidth, PAGE.InfoBar));
            PositionHelper.PositionExact(s.FullControl, ss.FullControl.X + 55, ss.FullControl.Y);
            ss = s;

            options.Add(s = new SettingsOption(
                    "",
                    new Line(0, 0, mainContent.RightWidth, 1, Color.Gray.PackedValue) { AcceptMouseInput = false },
                    mainContent.RightWidth,
                    PAGE.InfoBar
                ));
            PositionHelper.PositionExact(s.FullControl, ss.FullControl.X - 205, ss.FullControl.Y + ss.FullControl.Height + 2);
            PositionHelper.BlankLine();


            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>().InfoBars;
            List<InfoBarItem> _infoBarItems = ibmanager.GetInfoBars();

            for (int i = 0; i < _infoBarItems.Count; i++)
            {
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(_infoBarItems[i]);
                infoBarItems.Add(ibbc);
            }
            infoBarItems.ReArrangeChildren();
            infoBarItems.ForceSizeUpdate();

            options.Add(s = new SettingsOption(
                    "",
                    infoBarItems,
                    mainContent.RightWidth,
                    PAGE.InfoBar
                ));
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildContainers()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                lang.GetContainers.Description,
                new Area(false),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();
            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            if (Client.Version >= ClientVersion.CV_705301)
            {
                options.Add(s = new SettingsOption(
                    "",
                    new ComboBoxWithLabel(lang.GetContainers.CharacterBackpackStyle, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetContainers.BackpackOpt_Default, lang.GetContainers.BackpackOpt_Suede, lang.GetContainers.BackpackOpt_PolarBear, lang.GetContainers.BackpackOpt_GhoulSkin }, profile.BackpackStyle, (i, s) => { profile.BackpackStyle = i; }),
                    mainContent.RightWidth,
                    PAGE.Containers
                ));
                PositionHelper.PositionControl(s.FullControl);
                PositionHelper.BlankLine();
            }

            options.Add(s = new SettingsOption(
                "",
                new SliderWithLabel(lang.GetContainers.ContainerScale, 0, Theme.SLIDER_WIDTH, Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC, profile.ContainersScale, (i) =>
                {
                    profile.ContainersScale = (byte)i;
                    UIManager.ContainerScale = (byte)i / 100f;
                    foreach (ContainerGump resizableGump in UIManager.Gumps.OfType<ContainerGump>())
                    {
                        resizableGump.RequestUpdateContents();
                    }
                }),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetContainers.AlsoScaleItems, 0, profile.ScaleItemsInsideContainers, (b) => { profile.ScaleItemsInsideContainers = b; }),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();

            if (Client.Version >= ClientVersion.CV_706000)
            {
                options.Add(s = new SettingsOption(
                    "",
                    new CheckboxWithLabel(lang.GetContainers.UseLargeContainerGumps, 0, profile.UseLargeContainerGumps, (b) => { profile.UseLargeContainerGumps = b; }),
                    mainContent.RightWidth,
                    PAGE.Containers
                ));
                PositionHelper.PositionControl(s.FullControl);
                PositionHelper.BlankLine();
            }

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetContainers.DoubleClickToLootItemsInsideContainers, 0, profile.DoubleClickToLootInsideContainers, (b) => { profile.DoubleClickToLootInsideContainers = b; }),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetContainers.RelativeDragAndDropItemsInContainers, 0, profile.RelativeDragAndDropItems, (b) => { profile.RelativeDragAndDropItems = b; }),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetContainers.HighlightContainerOnGroundWhenMouseIsOverAContainerGump, 0, profile.HighlightContainerWhenSelected, (b) => { profile.HighlightContainerWhenSelected = b; }),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetContainers.RecolorContainerGumpByWithContainerHue, 0, profile.HueContainerGumps, (b) => { profile.HueContainerGumps = b; }),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetContainers.OverrideContainerGumpLocations, 0, profile.OverrideContainerLocation, (b) => { profile.OverrideContainerLocation = b; }),
                mainContent.RightWidth,
                PAGE.Containers
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                    "",
                    new ComboBoxWithLabel(lang.GetContainers.OverridePosition, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetContainers.PositionOpt_NearContainer, lang.GetContainers.PositionOpt_TopRight, lang.GetContainers.PositionOpt_LastDraggedPosition, lang.GetContainers.RememberEachContainer }, profile.OverrideContainerLocationSetting, (i, s) => { profile.OverrideContainerLocationSetting = i; }),
                    mainContent.RightWidth,
                    PAGE.Containers
                ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            ModernButton rebuildContainers;
            options.Add(s = new SettingsOption(
                "",
                rebuildContainers = new ModernButton(0, 0, 130, 40, ButtonAction.Activate, lang.GetContainers.RebuildContainersTxt, Theme.BUTTON_FONT_COLOR, 999) { IsSelected = true, IsSelectable = true },
                mainContent.RightWidth,
                PAGE.Containers
            ));
            rebuildContainers.MouseUp += (s, e) =>
            {
                ContainerManager.BuildContainerFile(true);
            };
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildExperimental()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetExperimental.DisableDefaultUoHotkeys, 0, profile.DisableDefaultHotkeys, (b) => { profile.DisableDefaultHotkeys = b; }),
                mainContent.RightWidth,
                PAGE.Experimental
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetExperimental.DisableArrowsNumlockArrowsPlayerMovement, 0, profile.DisableArrowBtn, (b) => { profile.DisableArrowBtn = b; }),
                mainContent.RightWidth,
                PAGE.Experimental
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetExperimental.DisableTabToggleWarmode, 0, profile.DisableTabBtn, (b) => { profile.DisableTabBtn = b; }),
                mainContent.RightWidth,
                PAGE.Experimental
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetExperimental.DisableCtrlQWMessageHistory, 0, profile.DisableCtrlQWBtn, (b) => { profile.DisableCtrlQWBtn = b; }),
                mainContent.RightWidth,
                PAGE.Experimental
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                "",
                new CheckboxWithLabel(lang.GetExperimental.DisableRightLeftClickAutoMove, 0, profile.DisableAutoMove, (b) => { profile.DisableAutoMove = b; }),
                mainContent.RightWidth,
                PAGE.Experimental
            ));
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildNameplates()
        {
            LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(mainContent.RightWidth, mainContent.Height, (int)(mainContent.RightWidth * 0.3));
            int page = ((int)PAGE.NameplateOptions + 1000);

            #region New entry
            ModernButton b;
            content.AddToLeft(b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetNamePlates.NewEntry, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false });

            b.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new
                (
                    250,
                    150,
                    lang.GetNamePlates.NameOverheadEntryName,
                    name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }
                        if (NameOverHeadManager.FindOption(name) != null)
                        {
                            return;
                        }

                        NameOverheadOption option = new NameOverheadOption(name);

                        ModernButton nb;
                        content.AddToLeft
                        (
                            nb = new ModernButton
                            (
                                0,
                                0,
                                content.LeftWidth,
                                40,
                                ButtonAction.SwitchPage,
                                name,
                                Theme.BUTTON_FONT_COLOR
                            )
                            {
                                ButtonParameter = page + 1 + content.LeftArea.Children.Count,
                                Tag = option
                            }
                        );
                        nb.IsSelected = true;
                        content.ActivePage = nb.ButtonParameter;
                        NameOverHeadManager.AddOption(option);

                        content.AddToRight(new NameOverheadAssignControl(option), false, nb.ButtonParameter);
                    }
                )
                {
                    CanCloseWithRightClick = true
                };
                UIManager.Add(dialog);
            };
            #endregion

            #region Delete entry
            page = ((int)PAGE.Macros + 1001);
            content.AddToLeft(b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetNamePlates.DeleteEntry, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false });

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    QuestionGump dialog = new QuestionGump
                    (
                        ResGumps.MacroDeleteConfirmation,
                        b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (nb.Tag is NameOverheadOption option)
                            {
                                NameOverHeadManager.RemoveOption(option);
                                nb.Dispose();
                            }
                        }
                    );

                    UIManager.Add(dialog);
                }
            };
            #endregion

            content.AddToLeft(new Line(0, 0, content.LeftWidth, 1, Color.Gray.PackedValue));

            var opts = NameOverHeadManager.GetAllOptions();
            ModernButton nb = null;

            for (int i = 0; i < opts.Count; i++)
            {
                var option = opts[i];
                if (option == null)
                {
                    continue;
                }

                content.AddToLeft
                (
                    nb = new ModernButton
                    (
                        0,
                        0,
                        content.LeftWidth,
                        40,
                        ButtonAction.SwitchPage,
                        option.Name,
                        Theme.BUTTON_FONT_COLOR
                    )
                    {
                        ButtonParameter = page + 1 + content.LeftArea.Children.Count,
                        Tag = option
                    }
                );

                content.AddToRight(new NameOverheadAssignControl(option), false, nb.ButtonParameter);
            }

            if (nb != null)
            {
                nb.IsSelected = true;
                content.ActivePage = nb.ButtonParameter;
            }

            options.Add(new SettingsOption(
                "",
                content,
                mainContent.RightWidth,
                PAGE.NameplateOptions
            ));
        }

        private void BuildCooldowns()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(
                lang.GetCooldowns.CustomCooldownBars,
                new Area(false),
                mainContent.RightWidth,
                PAGE.TUOCooldowns
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add(s = new SettingsOption(
                lang.GetCooldowns.PositionX,
                new InputField(100, 40, text: profile.CoolDownX.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                {
                    if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                    {
                        profile.CoolDownX = v;
                    }
                }),
                mainContent.RightWidth,
                PAGE.TUOCooldowns
            ));
            PositionHelper.PositionControl(s.FullControl);

            options.Add(s = new SettingsOption(
                lang.GetCooldowns.PositionY,
                new InputField(100, 40, text: profile.CoolDownY.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                {
                    if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                    {
                        profile.CoolDownY = v;
                    }
                }),
                mainContent.RightWidth,
                PAGE.TUOCooldowns
            ));
            PositionHelper.PositionControl(s.FullControl);

            options.Add(s = new SettingsOption(
                string.Empty,
                new CheckboxWithLabel(lang.GetCooldowns.UseLastMovedBarPosition, 0, profile.UseLastMovedCooldownPosition, (b) => { profile.UseLastMovedCooldownPosition = b; }),
                mainContent.RightWidth,
                PAGE.TUOCooldowns
            ));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(
                lang.GetCooldowns.Conditions,
                new Area(false),
                mainContent.RightWidth,
                PAGE.TUOCooldowns
            ));
            PositionHelper.PositionControl(s.FullControl);

            DataBox conditionsDataBox = new DataBox(0, 0, 0, 0) { WantUpdateSize = true };

            ModernButton addcond;
            options.Add(s = new SettingsOption(
                "",
                addcond = new ModernButton(0, 0, 175, 40, ButtonAction.Activate, lang.GetCooldowns.AddCondition, Theme.BUTTON_FONT_COLOR),
                mainContent.RightWidth,
                PAGE.TUOCooldowns
            ));
            addcond.MouseUp += (s, e) =>
            {
                CoolDownBar.CoolDownConditionData.GetConditionData(profile.CoolDownConditionCount, true);

                Gump g = UIManager.GetGump<ModernOptionsGump>();
                if (g != null)
                {
                    Point pos = g.Location;
                    g.Dispose();
                    g = new ModernOptionsGump() { Location = pos };
                    g.ChangePage((int)PAGE.TUOCooldowns);
                    UIManager.Add(g);
                }
            };
            PositionHelper.PositionControl(s.FullControl);

            int count = profile.CoolDownConditionCount;
            for (int i = 0; i < count; i++)
            {
                conditionsDataBox.Add(GenConditionControl(i, mainContent.RightWidth - 19, false));
            }
            conditionsDataBox.ReArrangeChildren();
            conditionsDataBox.ForceSizeUpdate();

            options.Add(s = new SettingsOption(
                "",
                conditionsDataBox,
                mainContent.RightWidth,
                PAGE.TUOCooldowns
            ));
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildTazUO()
        {
            LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(mainContent.RightWidth, mainContent.Height, (int)(mainContent.RightWidth * 0.3));
            Control c;
            int page;

            #region General
            page = ((int)PAGE.TUOOptions + 1000);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.GridContainers, page, content.LeftWidth));
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableGridContainers, 0, profile.UseGridLayoutContainerGumps, (b) =>
            {
                profile.UseGridLayoutContainerGumps = b;
            }), true, page);

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetTazUO.GridContainerScale, 0, Theme.SLIDER_WIDTH, 50, 200, profile.GridContainersScale, (i) =>
            {
                profile.GridContainersScale = (byte)i;
            }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.AlsoScaleItems, 0, profile.GridContainerScaleItems, (b) =>
            {
                profile.GridContainerScaleItems = b;
            }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetTazUO.GridItemBorderOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.GridBorderAlpha, (i) =>
            {
                profile.GridBorderAlpha = (byte)i;
            }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.BorderColor, profile.GridBorderHue, (h) =>
            {
                profile.GridBorderHue = h;
            }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetTazUO.ContainerOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.ContainerOpacity, (i) =>
            {
                profile.ContainerOpacity = (byte)i;
                GridContainer.UpdateAllGridContainers();
            }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.BackgroundColor, profile.AltGridContainerBackgroundHue, (h) =>
            {
                profile.AltGridContainerBackgroundHue = h;
                GridContainer.UpdateAllGridContainers();
            }), true, page);
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.UseContainersHue, 0, profile.Grid_UseContainerHue, (b) =>
            {
                profile.Grid_UseContainerHue = b;
                GridContainer.UpdateAllGridContainers();
            }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel(lang.GetTazUO.SearchStyle, 0, Theme.COMBO_BOX_WIDTH, new string[] { lang.GetTazUO.OnlyShow, lang.GetTazUO.Highlight }, profile.GridContainerSearchMode, (i, s) =>
            {
                profile.GridContainerSearchMode = i;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.EnableContainerPreview, 0, profile.GridEnableContPreview, (b) =>
            {
                profile.GridEnableContPreview = b;
            }), true, page);
            c.SetTooltip(lang.GetTazUO.TooltipPreview);

            content.BlankLine();

            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.MakeAnchorable, 0, profile.EnableGridContainerAnchor, (b) =>
            {
                profile.EnableGridContainerAnchor = b;
                GridContainer.UpdateAllGridContainers();
            }), true, page);
            c.SetTooltip(lang.GetTazUO.TooltipGridAnchor);

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel(lang.GetTazUO.ContainerStyle, 0, Theme.COMBO_BOX_WIDTH, Enum.GetNames(typeof(GridContainer.BorderStyle)), profile.Grid_BorderStyle, (i, s) =>
            {
                profile.Grid_BorderStyle = i;
                GridContainer.UpdateAllGridContainers();
            }), true, page);

            content.BlankLine();

            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.HideBorders, 0, profile.Grid_HideBorder, (b) =>
            {
                profile.Grid_HideBorder = b;
                GridContainer.UpdateAllGridContainers();
            }), true, page);

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetTazUO.DefaultGridRows, 0, Theme.SLIDER_WIDTH, 1, 20, profile.Grid_DefaultRows, (i) =>
            {
                profile.Grid_DefaultRows = i;
            }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.DefaultGridColumns, 0, Theme.SLIDER_WIDTH, 1, 20, profile.Grid_DefaultColumns, (i) =>
            {
                profile.Grid_DefaultColumns = i;
            }), true, page);

            content.BlankLine();

            content.AddToRight(c = new ModernButton(0, 0, 200, 40, ButtonAction.Activate, lang.GetTazUO.GridHighlightSettings, Theme.BUTTON_FONT_COLOR), true, page);
            c.MouseUp += (s, e) =>
            {
                UIManager.GetGump<GridHightlightMenu>()?.Dispose();
                UIManager.Add(new GridHightlightMenu());
            };
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.GridHighlightSize, 0, Theme.SLIDER_WIDTH, 1, 5, profile.GridHightlightSize, (i) =>
            {
                profile.GridHightlightSize = i;
            }), true, page);
            #endregion

            #region Journal
            page = ((int)PAGE.TUOOptions + 1001);
            content.ResetRightSide();

            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Journal, page, content.LeftWidth));
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.MaxJournalEntries, 0, Theme.SLIDER_WIDTH, 100, 2000, profile.MaxJournalEntries, (i) =>
            {
                profile.MaxJournalEntries = i;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.JournalOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.JournalOpacity, (i) =>
            {
                profile.JournalOpacity = (byte)i;
                ResizableJournal.UpdateJournalOptions();
            }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.JournalBackgroundColor, profile.AltJournalBackgroundHue, (h) =>
            {
                profile.AltJournalBackgroundHue = h;
                ResizableJournal.UpdateJournalOptions();
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new ComboBoxWithLabel(lang.GetTazUO.JournalStyle, 0, Theme.COMBO_BOX_WIDTH, Enum.GetNames(typeof(ResizableJournal.BorderStyle)), profile.JournalStyle, (i, s) =>
            {
                profile.JournalStyle = i;
                ResizableJournal.UpdateJournalOptions();
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.JournalHideBorders, 0, profile.HideJournalBorder, (b) =>
            {
                profile.HideJournalBorder = b;
                ResizableJournal.UpdateJournalOptions();
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.HideTimestamp, 0, profile.HideJournalTimestamp, (b) =>
            {
                profile.HideJournalTimestamp = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.MakeAnchorable, 0, profile.JournalAnchorEnabled, (b) =>
            {
                profile.JournalAnchorEnabled = b;
                ResizableJournal.UpdateJournalOptions();
            }), true, page);
            #endregion

            #region Modern paperdoll
            page = ((int)PAGE.TUOOptions + 1002);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.ModernPaperdoll, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.EnableModernPaperdoll, 0, profile.UseModernPaperdoll, (b) =>
            {
                profile.UseModernPaperdoll = b;
            }), true, page);
            content.Indent();
            content.BlankLine();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.PaperdollHue, profile.ModernPaperDollHue, (h) =>
            {
                profile.ModernPaperDollHue = h;
                ModernPaperdoll.UpdateAllOptions();
            }), true, page);
            content.BlankLine();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.DurabilityBarHue, profile.ModernPaperDollDurabilityHue, (h) =>
            {
                profile.ModernPaperDollDurabilityHue = h;
                ModernPaperdoll.UpdateAllOptions();
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.ShowDurabilityBarBelow, 0, Theme.SLIDER_WIDTH, 1, 100, profile.ModernPaperDoll_DurabilityPercent, (i) =>
            {
                profile.ModernPaperDoll_DurabilityPercent = i;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.PaperdollAnchor, 0, profile.ModernPaperdollAnchorEnabled, (b) =>
            {
                profile.ModernPaperdollAnchorEnabled = b;
                ModernPaperdoll.UpdateAllOptions();
            }), true, page);
            #endregion

            #region Nameplates
            page = ((int)PAGE.TUOOptions + 1003);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Nameplates, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.NameplatesAlsoActAsHealthBars, 0, profile.NamePlateHealthBar, (b) =>
            {
                profile.NamePlateHealthBar = b;
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.HpOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.NamePlateHealthBarOpacity, (i) =>
            {
                profile.NamePlateHealthBarOpacity = (byte)i;
            }), true, page);
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.HideNameplatesIfFullHealth, 0, profile.NamePlateHideAtFullHealth, (b) =>
            {
                profile.NamePlateHideAtFullHealth = b;
            }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.OnlyInWarmode, 0, profile.NamePlateHideAtFullHealthInWarmode, (b) =>
            {
                profile.NamePlateHideAtFullHealthInWarmode = b;
            }), true, page);
            content.RemoveIndent();
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.BorderOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.NamePlateBorderOpacity, (i) =>
            {
                profile.NamePlateBorderOpacity = (byte)i;
            }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.BackgroundOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.NamePlateOpacity, (i) =>
            {
                profile.NamePlateOpacity = (byte)i;
            }), true, page);
            #endregion

            #region Mobiles
            page = ((int)PAGE.TUOOptions + 1004);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Mobiles, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(c = new ModernColorPickerWithLabel(lang.GetTazUO.DamageToSelf, profile.DamageHueSelf, (h) =>
            {
                profile.DamageHueSelf = h;
            }), true, page);
            content.AddToRight(c = new ModernColorPickerWithLabel(lang.GetTazUO.DamageToOthers, profile.DamageHueOther, (h) =>
            {
                profile.DamageHueOther = h;
            })
            { X = 250, Y = c.Y }, false, page);
            content.AddToRight(c = new ModernColorPickerWithLabel(lang.GetTazUO.DamageToPets, profile.DamageHuePet, (h) =>
            {
                profile.DamageHuePet = h;
            }), true, page);
            content.AddToRight(c = new ModernColorPickerWithLabel(lang.GetTazUO.DamageToAllies, profile.DamageHueAlly, (h) =>
            {
                profile.DamageHueAlly = h;
            })
            { X = 250, Y = c.Y }, false, page);
            content.AddToRight(c = new ModernColorPickerWithLabel(lang.GetTazUO.DamageToLastAttack, profile.DamageHueLastAttck, (h) =>
            {
                profile.DamageHueLastAttck = h;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.DisplayPartyChatOverPlayerHeads, 0, profile.DisplayPartyChatOverhead, (b) =>
            {
                profile.DisplayPartyChatOverhead = b;
            }), true, page);
            c.SetTooltip(lang.GetTazUO.TooltipPartyChat);
            content.BlankLine();
            content.AddToRight(c = new SliderWithLabel(lang.GetTazUO.OverheadTextWidth, 0, Theme.SLIDER_WIDTH, 0, 600, profile.OverheadChatWidth, (i) =>
            {
                profile.OverheadChatWidth = i;
            }), true, page);
            c.SetTooltip(lang.GetTazUO.TooltipOverheadText);
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.BelowMobileHealthBarScale, 0, Theme.SLIDER_WIDTH, 1, 5, profile.HealthLineSizeMultiplier, (i) =>
            {
                profile.HealthLineSizeMultiplier = (byte)i;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.AutomaticallyOpenHealthBarsForLastAttack, 0, profile.OpenHealthBarForLastAttack, (b) =>
            {
                profile.OpenHealthBarForLastAttack = b;
            }), true, page);
            content.Indent();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.UpdateOneBarAsLastAttack, 0, profile.UseOneHPBarForLastAttack, (b) =>
            {
                profile.UseOneHPBarForLastAttack = b;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.HiddenPlayerOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.HiddenBodyAlpha, (i) =>
            {
                profile.HiddenBodyAlpha = (byte)i;
            }), true, page);
            content.Indent();
            content.AddToRight(c = new ModernColorPickerWithLabel(lang.GetTazUO.HiddenPlayerHue, profile.HiddenBodyHue, (h) =>
            {
                profile.HiddenBodyHue = h;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.RegularPlayerOpacity, 0, Theme.SLIDER_WIDTH, 0, 100, profile.PlayerConstantAlpha, (i) =>
            {
                profile.PlayerConstantAlpha = i;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.AutoFollowDistance, 0, Theme.SLIDER_WIDTH, 0, 10, profile.AutoFollowDistance, (i) =>
            {
                profile.AutoFollowDistance = i;
            }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.DisableAutoFollow, 0, profile.DisableAutoFollowAlt, (i) =>
            {
                profile.DisableAutoFollowAlt = i;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.DisableMouseInteractionsForOverheadText, 0, profile.DisableMouseInteractionOverheadText, (b) =>
            {
                profile.DisableMouseInteractionOverheadText = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.OverridePartyMemberHues, 0, profile.OverridePartyAndGuildHue, (b) =>
            {
                profile.OverridePartyAndGuildHue = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.ShowTargetIndicator, isChecked: profile.ShowTargetIndicator, valueChanged: (b) => { profile.ShowTargetIndicator = b; }), true, page);
            #endregion

            #region Misc
            page = ((int)PAGE.TUOOptions + 1005);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Misc, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.DisableSystemChat, 0, profile.DisableSystemChat, (b) =>
            {
                profile.DisableSystemChat = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableImprovedBuffGump, 0, profile.UseImprovedBuffBar, (b) =>
            {
                profile.UseImprovedBuffBar = b;
            }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.BuffGumpHue, profile.ImprovedBuffBarHue, (h) =>
            {
                profile.ImprovedBuffBarHue = h;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.MainGameWindowBackground, profile.MainWindowBackgroundHue, (h) =>
            {
                profile.MainWindowBackgroundHue = h;
                GameController.UpdateBackgroundHueShader();
            }), true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableHealthIndicatorBorder, 0, profile.EnableHealthIndicator, (b) =>
            {
                profile.EnableHealthIndicator = b;
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.OnlyShowBelowHp, 0, Theme.SLIDER_WIDTH, 1, 100, (int)profile.ShowHealthIndicatorBelow * 100, (i) =>
            {
                profile.ShowHealthIndicatorBelow = i / 100f;
            }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.Size, 0, Theme.SLIDER_WIDTH, 1, 25, profile.HealthIndicatorWidth, (i) =>
            {
                profile.HealthIndicatorWidth = i;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.SpellIconScale, 0, Theme.SLIDER_WIDTH, 50, 300, profile.SpellIconScale, (i) =>
            {
                profile.SpellIconScale = i;
            }), true, page);
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.DisplayMatchingHotkeysOnSpellIcons, 0, profile.SpellIcon_DisplayHotkey, (b) =>
            {
                profile.SpellIcon_DisplayHotkey = b;
            }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.HotkeyTextHue, profile.SpellIcon_HotkeyHue, (h) =>
            {
                profile.SpellIcon_HotkeyHue = h;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableGumpOpacityAdjustViaAltScroll, 0, profile.EnableAlphaScrollingOnGumps, (b) =>
            {
                profile.EnableAlphaScrollingOnGumps = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableAdvancedShopGump, 0, profile.UseModernShopGump, (b) =>
            {
                profile.UseModernShopGump = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.DisplaySkillProgressBarOnSkillChanges, 0, profile.DisplaySkillBarOnChange, (b) =>
            {
                profile.DisplaySkillBarOnChange = b;
            }), true, page);
            content.Indent();
            content.AddToRight(new InputFieldWithLabel(lang.GetTazUO.TextFormat, Theme.INPUT_WIDTH, profile.SkillBarFormat, false, (s, e) =>
            {
                profile.SkillBarFormat = ((InputField.StbTextBox)s).Text;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableSpellIndicatorSystem, 0, profile.EnableSpellIndicators, (b) =>
            {
                profile.EnableSpellIndicators = b;
            }), true, page);
            content.Indent();
            content.AddToRight(c = new ModernButton(0, 0, 200, 40, ButtonAction.Activate, lang.GetTazUO.ImportFromUrl, Theme.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page);
            c.MouseUp += (s, e) =>
              {
                  if (e.Button == MouseButtonType.Left)
                  {
                      UIManager.Add(new InputRequest(lang.GetTazUO.InputRequestUrl, lang.GetTazUO.Download, lang.GetTazUO.Cancel, (r, s) =>
                      {
                          if (r == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(s))
                          {
                              if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
                              {
                                  GameActions.Print(lang.GetTazUO.AttemptingToDownloadSpellConfig);
                                  Task.Factory.StartNew(() =>
                                  {
                                      try
                                      {
                                          using HttpClient httpClient = new HttpClient();
                                          string result = httpClient.GetStringAsync(uri).Result;
                                          if (SpellVisualRangeManager.Instance.LoadFromString(result))
                                          {
                                              GameActions.Print(lang.GetTazUO.SuccesfullyDownloadedNewSpellConfig);
                                          }
                                      }
                                      catch (Exception ex)
                                      {
                                          GameActions.Print(string.Format(lang.GetTazUO.FailedToDownloadTheSpellConfigExMessage, ex.Message));
                                      }
                                  });
                              }
                          }
                      }, "https://gist.githubusercontent.com/bittiez/c70ddcb58fc59f74a0c4d2c5b4fc6478/raw/SpellVisualRange.json")
                      { X = (Client.Game.Window.ClientBounds.Width >> 1) - 50, Y = (Client.Game.Window.ClientBounds.Height >> 1) - 50 });
                  }
              };
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars, content.RightWidth - 30, profile.CloseHealthBarIfAnchored, (b) =>
            {
                profile.CloseHealthBarIfAnchored = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableAutoResyncOnHangDetection, 0, profile.ForceResyncOnHang, (b) =>
            {
                profile.ForceResyncOnHang = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.PlayerOffsetX, 0, Theme.SLIDER_WIDTH, -20, 20, profile.PlayerOffset.X, (i) =>
            {
                profile.PlayerOffset = new Point(i, profile.PlayerOffset.Y);
            }), true, page);
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.PlayerOffsetY, 0, Theme.SLIDER_WIDTH, -20, 20, profile.PlayerOffset.Y, (i) =>
            {
                profile.PlayerOffset = new Point(profile.PlayerOffset.X, i);
            }), true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.UseLandTexturesWhereAvailable, 0, profile.UseLandTextures, (b) =>
            {
                profile.UseLandTextures = b;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new InputFieldWithLabel(lang.GetTazUO.SOSGumpID, Theme.INPUT_WIDTH, profile.SOSGumpID.ToString(), true, (s, e) => { if (uint.TryParse(((InputField.StbTextBox)s).Text, out uint id)) { profile.SOSGumpID = id; } }), true, page);
            #endregion

            #region Tooltips
            page = ((int)PAGE.TUOOptions + 1006);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Tooltips, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.AlignTooltipsToTheLeftSide, 0, profile.LeftAlignToolTips, (b) =>
            {
                profile.LeftAlignToolTips = b;
            }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.AlignMobileTooltipsToCenter, 0, profile.ForceCenterAlignTooltipMobiles, (b) =>
            {
                profile.ForceCenterAlignTooltipMobiles = b;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetTazUO.BackgroundHue, profile.ToolTipBGHue, (h) =>
            {
                profile.ToolTipBGHue = h;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new InputFieldWithLabel(lang.GetTazUO.HeaderFormatItemName, Theme.INPUT_WIDTH, profile.TooltipHeaderFormat, false, (s, e) =>
            {
                profile.TooltipHeaderFormat = ((InputField.StbTextBox)s).Text;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new ModernButton(0, 0, 200, 40, ButtonAction.Activate, lang.GetTazUO.TooltipOverrideSettings, Theme.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page);
            c.MouseUp += (s, e) => { UIManager.GetGump<ToolTipOverideMenu>()?.Dispose(); UIManager.Add(new ToolTipOverideMenu()); };

            #endregion

            #region Font settings
            page = ((int)PAGE.TUOOptions + 1007);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.FontSettings, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.TtfFontBorder, 0, Theme.SLIDER_WIDTH, 0, 2, profile.TextBorderSize, (i) =>
            {
                profile.TextBorderSize = i;
            }), true, page);
            content.BlankLine();
            content.BlankLine();
            content.AddToRight(GenerateFontSelector(lang.GetTazUO.InfobarFont, ProfileManager.CurrentProfile.InfoBarFont, (i, s) =>
            {
                ProfileManager.CurrentProfile.InfoBarFont = s;
                InfoBarGump.UpdateAllOptions();
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.SharedSize, 0, Theme.SLIDER_WIDTH, 5, 40, profile.InfoBarFontSize, (i) =>
            {
                profile.InfoBarFontSize = i;
                InfoBarGump.UpdateAllOptions();
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(GenerateFontSelector(lang.GetTazUO.SystemChatFont, ProfileManager.CurrentProfile.GameWindowSideChatFont, (i, s) =>
            {
                ProfileManager.CurrentProfile.GameWindowSideChatFont = s;
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.SharedSize, 0, Theme.SLIDER_WIDTH, 5, 40, profile.GameWindowSideChatFontSize, (i) =>
            {
                profile.GameWindowSideChatFontSize = i;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(GenerateFontSelector(lang.GetTazUO.TooltipFont, ProfileManager.CurrentProfile.SelectedToolTipFont, (i, s) =>
            {
                ProfileManager.CurrentProfile.SelectedToolTipFont = s;
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.SharedSize, 0, Theme.SLIDER_WIDTH, 5, 40, profile.SelectedToolTipFontSize, (i) =>
            {
                profile.SelectedToolTipFontSize = i;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(GenerateFontSelector(lang.GetTazUO.OverheadFont, ProfileManager.CurrentProfile.OverheadChatFont, (i, s) =>
            {
                ProfileManager.CurrentProfile.OverheadChatFont = s;
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.SharedSize, 0, Theme.SLIDER_WIDTH, 5, 40, profile.OverheadChatFontSize, (i) =>
            {
                profile.OverheadChatFontSize = i;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(GenerateFontSelector(lang.GetTazUO.JournalFont, ProfileManager.CurrentProfile.SelectedTTFJournalFont, (i, s) =>
            {
                ProfileManager.CurrentProfile.SelectedTTFJournalFont = s;
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.SharedSize, 0, Theme.SLIDER_WIDTH, 5, 40, profile.SelectedJournalFontSize, (i) =>
            {
                profile.SelectedJournalFontSize = i;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            content.AddToRight(GenerateFontSelector(lang.GetTazUO.NameplateFont, ProfileManager.CurrentProfile.NamePlateFont, (i, s) =>
            {
                ProfileManager.CurrentProfile.NamePlateFont = s;
            }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel(lang.GetTazUO.SharedSize, 0, Theme.SLIDER_WIDTH, 5, 40, profile.NamePlateFontSize, (i) =>
            {
                profile.NamePlateFontSize = i;
            }), true, page);
            content.RemoveIndent();
            content.BlankLine();
            #endregion

            #region Controller settings
            page = ((int)PAGE.TUOOptions + 1008);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Controller, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new SliderWithLabel(lang.GetTazUO.MouseSesitivity, 0, Theme.SLIDER_WIDTH, 1, 20, profile.ControllerMouseSensativity, (i) => { profile.ControllerMouseSensativity = i; }), true, page);

            #endregion

            #region Settings transfers
            page = ((int)PAGE.TUOOptions + 1009);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.SettingsTransfers, page, content.LeftWidth));
            content.ResetRightSide();

            string rootpath;

            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath))
            {
                rootpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            }
            else
            {
                rootpath = Settings.GlobalSettings.ProfilesPath;
            }

            List<ProfileLocationData> locations = new List<ProfileLocationData>();
            List<ProfileLocationData> sameServerLocations = new List<ProfileLocationData>();
            string[] allAccounts = Directory.GetDirectories(rootpath);

            foreach (string account in allAccounts)
            {
                string[] allServers = Directory.GetDirectories(account);
                foreach (string server in allServers)
                {
                    string[] allCharacters = Directory.GetDirectories(server);
                    foreach (string character in allCharacters)
                    {
                        locations.Add(new ProfileLocationData(server, account, character));
                        if (FileSystemHelper.RemoveInvalidChars(profile.ServerName) == FileSystemHelper.RemoveInvalidChars(Path.GetFileName(server)))
                        {
                            sameServerLocations.Add(new ProfileLocationData(server, account, character));
                        }
                    }
                }
            }

            content.AddToRight(new TextBox(
                string.Format(lang.GetTazUO.SettingsWarning, locations.Count),
                Theme.FONT,
                Theme.STANDARD_TEXT_SIZE,
                content.RightWidth - 20,
                Theme.TEXT_FONT_COLOR,
                FontStashSharp.RichText.TextHorizontalAlignment.Center,
                false), true, page);

            content.AddToRight(c = new ModernButton(0, 0, content.RightWidth - 20, 40, ButtonAction.Activate, string.Format(lang.GetTazUO.OverrideAll, locations.Count - 1), Theme.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page);
            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OverrideAllProfiles(locations);
                    GameActions.Print(string.Format(lang.GetTazUO.OverrideSuccess, locations.Count - 1), 32, Data.MessageType.System);
                }
            };

            content.AddToRight(c = new ModernButton(0, 0, content.RightWidth - 20, 40, ButtonAction.Activate, string.Format(lang.GetTazUO.OverrideSame, sameServerLocations.Count - 1), Theme.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page);
            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OverrideAllProfiles(sameServerLocations);
                    GameActions.Print(string.Format(lang.GetTazUO.OverrideSuccess, sameServerLocations.Count - 1), 32, Data.MessageType.System);
                }
            };
            #endregion

            #region Gump scaling
            page = ((int)PAGE.TUOOptions + 1010);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.GumpScaling, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new TextBox(lang.GetTazUO.ScalingInfo, Theme.FONT, Theme.STANDARD_TEXT_SIZE, content.RightWidth - 20, Theme.TEXT_FONT_COLOR, FontStashSharp.RichText.TextHorizontalAlignment.Center, false), true, page);

            content.BlankLine();

            content.AddToRight(new SliderWithLabel(lang.GetTazUO.PaperdollGump, 0, Theme.SLIDER_WIDTH, 50, 300, (int)(profile.PaperdollScale * 100), (i) =>
            {
                //Must be cast even though VS thinks it's redundant.
                double v = (double)i / (double)100;
                profile.PaperdollScale = v > 0 ? v : 1f;
            }), true, page);
            #endregion

            content.AddToLeft(c = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetTazUO.AutoLoot, Theme.BUTTON_FONT_COLOR));
            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    AutoLootOptions.AddToUI();
                }
            };

            #region Hidden layers
            page = ((int)PAGE.TUOOptions + 1011);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.VisibleLayers, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(new TextBox(lang.GetTazUO.VisLayersInfo, Theme.FONT, Theme.STANDARD_TEXT_SIZE, content.RightWidth - 20, Theme.TEXT_FONT_COLOR, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { AcceptMouseInput = false }, true, page);
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.OnlyForYourself, 0, profile.HideLayersForSelf, (b) =>
            {
                profile.HideLayersForSelf = b;
            }), true, page);
            content.BlankLine();

            bool rightSide = false;
            foreach (Layer layer in (Layer[])Enum.GetValues(typeof(Layer)))
            {
                if (layer == Layer.Invalid || layer == Layer.Hair || layer == Layer.Beard || layer == Layer.Backpack || layer == Layer.ShopBuyRestock || layer == Layer.ShopBuy || layer == Layer.ShopSell || layer == Layer.Bank || layer == Layer.Face || layer == Layer.Talisman || layer == Layer.Mount)
                {
                    continue;
                }
                if (!rightSide)
                {
                    content.AddToRight(c = new CheckboxWithLabel(layer.ToString(), 0, profile.HiddenLayers.Contains((int)layer), (b) => { if (b) { profile.HiddenLayers.Add((int)layer); } else { profile.HiddenLayers.Remove((int)layer); } }), true, page);
                    rightSide = true;
                }
                else
                {
                    content.AddToRight(new CheckboxWithLabel(layer.ToString(), 0, profile.HiddenLayers.Contains((int)layer), (b) => { if (b) { profile.HiddenLayers.Add((int)layer); } else { profile.HiddenLayers.Remove((int)layer); } }) { X = 200, Y = c.Y }, false, page);
                    rightSide = false;
                }
            }
            #endregion

            options.Add(
            new SettingsOption(
                "",
                content,
                mainContent.RightWidth,
                PAGE.TUOOptions
                )
            );
        }

        public string GetPageString()
        {
            string page = mainContent.ActivePage.ToString();

            foreach (Control c in mainContent.RightArea.Children)
            {
                if (c is Area && c.Page == mainContent.ActivePage)
                {
                    foreach (Control c2 in c.Children)
                    {
                        if (c2 is LeftSideMenuRightSideContent)
                        {
                            page += ":" + c2.ActivePage;
                            return page;
                        }
                    }
                }
            }
            return page;
        }

        public void GoToPage(string pageString)
        {
            string[] parts = pageString.Split(':');

            if (parts.Length >= 1)
            {
                if (int.TryParse(parts[0], out int p))
                {
                    ChangePage(p);

                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[1], out int pp))
                        {
                            foreach (Control c in mainContent.RightArea.Children)
                            {
                                if (c is Area && c.Page == p)
                                {
                                    foreach (Control c2 in c.Children)
                                    {
                                        if (c2 is LeftSideMenuRightSideContent lsc)
                                        {
                                            lsc.ActivePage = pp;
                                            foreach (Control mb in lsc.LeftArea.Children)
                                            {
                                                if (mb is ModernButton button && button.ButtonParameter == pp && button.IsSelectable)
                                                {
                                                    button.IsSelected = true;
                                                    break;
                                                }
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


        }

        public override void ChangePage(int pageIndex)
        {
            base.ChangePage(pageIndex);
            foreach (Control mb in mainContent.LeftArea.Children)
            {
                if (mb is ModernButton button && button.ButtonParameter == pageIndex && button.IsSelectable)
                {
                    button.IsSelected = true;
                    break;
                }
            }
        }

        private void OverrideAllProfiles(List<ProfileLocationData> allProfiles)
        {
            foreach (var profile in allProfiles)
            {
                ProfileManager.CurrentProfile.Save(profile.ToString(), false);
            }
        }

        private ComboBoxWithLabel GenerateFontSelector(string label, string selectedFont = "", Action<int, string> onSelect = null)
        {
            string[] fontArray = TrueTypeLoader.Instance.Fonts;
            int selectedFontInd = Array.IndexOf(fontArray, selectedFont);
            return new ComboBoxWithLabel(label, 0, Theme.COMBO_BOX_WIDTH, fontArray, selectedFontInd, onSelect);
        }

        private ModernButton CategoryButton(string text, int page, int width, int height = 40)
        {
            return new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page, FullPageSwitch = true };
        }

        private ModernButton SubCategoryButton(string text, int page, int width, int height = 40)
        {
            return new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page };
        }

        public Control GenConditionControl(int key, int width, bool createIfNotExists)
        {
            CoolDownBar.CoolDownConditionData data = CoolDownBar.CoolDownConditionData.GetConditionData(key, createIfNotExists);
            Area main = new Area
            {
                Width = width
            };

            AlphaBlendControl _background = new AlphaBlendControl();
            main.Add(_background);

            ModernButton _delete = new ModernButton(1, 1, 30, 40, ButtonAction.Activate, "X", Theme.BUTTON_FONT_COLOR);
            _delete.SetTooltip("Delete this cooldown bar");
            _delete.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    CoolDownBar.CoolDownConditionData.RemoveCondition(key);

                    Gump g = UIManager.GetGump<ModernOptionsGump>();
                    if (g != null)
                    {
                        Point pos = g.Location;
                        g.Dispose();
                        g = new ModernOptionsGump() { Location = pos };
                        g.ChangePage((int)PAGE.TUOCooldowns);
                        UIManager.Add(g);
                    }
                }
            };
            main.Add(_delete);


            TextBox _hueLabel = new TextBox("Hue:", Theme.FONT, Theme.STANDARD_TEXT_SIZE, null, Theme.BUTTON_FONT_COLOR, strokeEffect: false);
            _hueLabel.X = _delete.X + _delete.Width + 5;
            _hueLabel.Y = 10;
            main.Add(_hueLabel);

            ModernColorPickerWithLabel _hueSelector = new ModernColorPickerWithLabel(string.Empty, data.hue) { X = _hueLabel.X + _hueLabel.Width + 5, Y = 10 };
            main.Add(_hueSelector);


            InputField _name = new InputField(140, 40, text: data.label) { X = _hueSelector.X + _hueSelector.Width + 10, Y = 1 };
            main.Add(_name);


            TextBox _cooldownLabel = new TextBox("Cooldown:", Theme.FONT, Theme.STANDARD_TEXT_SIZE, null, Theme.BUTTON_FONT_COLOR, strokeEffect: false);
            _cooldownLabel.X = _name.X + _name.Width + 10;
            _cooldownLabel.Y = 10;
            main.Add(_cooldownLabel);

            InputField _cooldown = new InputField(45, 40, numbersOnly: true, text: data.cooldown.ToString()) { Y = 1 };
            _cooldown.X = _cooldownLabel.X + _cooldownLabel.Width + 10;
            main.Add(_cooldown);

            ComboBoxWithLabel _message_type = new ComboBoxWithLabel(string.Empty, 0, 85, new string[] { "All", "Self", "Other" }, data.message_type) { X = _cooldown.X + _cooldown.Width + 10, Y = 10 };
            main.Add(_message_type);

            InputField _conditionText = new InputField(main.Width - 50, 40, text: data.trigger) { X = 1, Y = _delete.Height + 5 };
            main.Add(_conditionText);

            CheckboxWithLabel _replaceIfExists = new CheckboxWithLabel(isChecked: data.replace_if_exists) { X = _conditionText.X + _conditionText.Width + 2, Y = _conditionText.Y + 5 };
            _replaceIfExists.SetTooltip("Replace any active cooldown of this type with a new one if triggered again.");
            main.Add(_replaceIfExists);

            ModernButton _save = new ModernButton(0, 1, 40, 40, ButtonAction.Activate, "Save", Theme.BUTTON_FONT_COLOR);
            _save.X = main.Width - _save.Width;
            _save.IsSelectable = true;
            _save.IsSelected = true;
            _save.MouseUp += (s, e) =>
            {
                CoolDownBar.CoolDownConditionData.SaveCondition(key, _hueSelector.Hue, _name.Text, _conditionText.Text, int.Parse(_cooldown.Text), false, _message_type.SelectedIndex, _replaceIfExists.IsChecked);
            };
            main.Add(_save);

            ModernButton _preview = new ModernButton(0, 1, 65, 40, ButtonAction.Activate, "Preview", Theme.BUTTON_FONT_COLOR);
            _preview.X = _save.X - _preview.Width - 15;
            _preview.IsSelectable = true;
            _preview.IsSelected = true;
            _preview.MouseUp += (s, e) =>
            {
                if (int.TryParse(_cooldown.Text, out int value))
                {
                    CoolDownBarManager.AddCoolDownBar(TimeSpan.FromSeconds(value), _name.Text, _hueSelector.Hue, _replaceIfExists.IsChecked);
                }
            };
            main.Add(_preview);

            main.Height = _conditionText.Bounds.Bottom;

            _background.Width = width;
            _background.Height = main.Height;
            return main;
        }

        public override void OnPageChanged()
        {
            base.OnPageChanged();

            mainContent.ActivePage = ActivePage;
        }

        public override void Dispose()
        {
            base.Dispose();

            SearchValueChanged = null;
        }

        public static void SetParentsForMatchingSearch(Control c, int page)
        {
            for (Control p = c.Parent; p != null; p = p.Parent)
            {
                if (p is LeftSideMenuRightSideContent content)
                {
                    content.SetMatchingButton(page);
                }
            }
        }

        #region Custom Controls For Options
        private class ModernColorPickerWithLabel : Control, SearchableOption
        {
            private TextBox _label;
            private ModernColorPicker.HueDisplay _colorPicker;

            public ModernColorPickerWithLabel(string text, ushort hue, Action<ushort> hueSelected = null, int maxWidth = 0)
            {
                AcceptMouseInput = true;
                CanMove = true;
                WantUpdateSize = false;

                Add(_colorPicker = new ModernColorPicker.HueDisplay(hue, hueSelected, true));

                Add(_label = new TextBox(text, Theme.FONT, Theme.STANDARD_TEXT_SIZE, maxWidth > 0 ? maxWidth : null, Theme.TEXT_FONT_COLOR, strokeEffect: false) { X = _colorPicker.Width + 5 });

                Width = _label.Width + _colorPicker.Width + 5;
                Height = Math.Max(_colorPicker.Height, _label.MeasuredSize.Y);

                ModernOptionsGump.SearchValueChanged += ModernOptionsGump_SearchValueChanged;
            }

            public ushort Hue => _colorPicker.Hue;

            private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
            {
                if (!string.IsNullOrEmpty(ModernOptionsGump.SearchText))
                {
                    if (Search(ModernOptionsGump.SearchText))
                    {
                        OnSearchMatch();
                        ModernOptionsGump.SetParentsForMatchingSearch(this, Page);
                    }
                    else
                    {
                        _label.Alpha = Theme.NO_MATCH_SEARCH;
                    }
                }
                else
                {
                    _label.Alpha = 1f;
                }
            }

            public bool Search(string text)
            {
                return _label.Text.ToLower().Contains(text.ToLower());
            }

            public void OnSearchMatch()
            {
                _label.Alpha = 1f;
            }
        }

        private class CheckboxWithLabel : Control, SearchableOption
        {
            private bool _isChecked;
            private readonly TextBox _text;

            public TextBox TextLabel => _text;

            private Vector3 hueVector = ShaderHueTranslator.GetHueVector(Theme.CHECKBOX, false, 0.9f);

            public CheckboxWithLabel(
                string text = "",
                int maxWidth = 0,
                bool isChecked = false,
                Action<bool> valueChanged = null
            )
            {
                _isChecked = isChecked;
                ValueChanged = valueChanged;
                _text = new TextBox(text, Theme.FONT, Theme.STANDARD_TEXT_SIZE, maxWidth == 0 ? null : maxWidth, Theme.TEXT_FONT_COLOR, strokeEffect: false) { X = Theme.CHECKBOX_SIZE + 5, AcceptMouseInput = false };

                Width = Theme.CHECKBOX_SIZE + 5 + _text.Width;
                Height = Math.Max(Theme.CHECKBOX_SIZE, _text.MeasuredSize.Y);

                _text.Y = (Height / 2) - (_text.Height / 2);

                CanMove = true;
                AcceptMouseInput = true;

                ModernOptionsGump.SearchValueChanged += ModernOptionsGump_SearchValueChanged;
            }

            private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
            {
                if (!string.IsNullOrEmpty(ModernOptionsGump.SearchText))
                {
                    if (Search(ModernOptionsGump.SearchText))
                    {
                        OnSearchMatch();
                        ModernOptionsGump.SetParentsForMatchingSearch(this, Page);
                    }
                    else
                    {
                        _text.Alpha = Theme.NO_MATCH_SEARCH;
                    }
                }
                else
                {
                    _text.Alpha = 1f;
                }
            }

            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked != value)
                    {
                        _isChecked = value;
                        OnCheckedChanged();
                    }
                }
            }

            public override ClickPriority Priority => ClickPriority.High;

            public string Text => _text.Text;

            public Action<bool> ValueChanged { get; }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (IsDisposed)
                {
                    return false;
                }

                batcher.Draw(
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle(x, y, Theme.CHECKBOX_SIZE, Theme.CHECKBOX_SIZE),
                    hueVector
                );

                if (IsChecked)
                {
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(Color.Black),
                        new Rectangle(x + (Theme.CHECKBOX_SIZE / 2) / 2, y + (Theme.CHECKBOX_SIZE / 2) / 2, Theme.CHECKBOX_SIZE / 2, Theme.CHECKBOX_SIZE / 2),
                        hueVector
                    );
                }

                _text.Draw(batcher, x + _text.X, y + _text.Y);

                return base.Draw(batcher, x, y);
            }

            protected virtual void OnCheckedChanged()
            {
                ValueChanged?.Invoke(IsChecked);
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left && MouseIsOver)
                {
                    IsChecked = !IsChecked;
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                _text?.Dispose();
            }

            public bool Search(string text)
            {
                return _text.Text.ToLower().Contains(text.ToLower());
            }

            public void OnSearchMatch()
            {
                _text.Alpha = 1f;
            }
        }

        private class SliderWithLabel : Control, SearchableOption
        {
            private readonly TextBox _label;
            private readonly Slider _slider;

            public SliderWithLabel(string label, int textWidth, int barWidth, int min, int max, int value, Action<int> valueChanged = null)
            {
                AcceptMouseInput = true;
                CanMove = true;

                Add(_label = new TextBox(label, Theme.FONT, Theme.STANDARD_TEXT_SIZE, textWidth > 0 ? textWidth : null, Theme.TEXT_FONT_COLOR, strokeEffect: false));
                Add(_slider = new Slider(barWidth, min, max, value, valueChanged) { X = _label.X + _label.Width + 5 });

                Width = textWidth + barWidth + 5;
                Height = Math.Max(_label.Height, _slider.Height);

                _slider.Y = (Height / 2) - (_slider.Height / 2);

                ModernOptionsGump.SearchValueChanged += ModernOptionsGump_SearchValueChanged;
            }

            private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
            {
                if (!string.IsNullOrEmpty(ModernOptionsGump.SearchText))
                {
                    if (Search(ModernOptionsGump.SearchText))
                    {
                        OnSearchMatch();
                        ModernOptionsGump.SetParentsForMatchingSearch(this, Page);
                    }
                    else
                    {
                        _label.Alpha = Theme.NO_MATCH_SEARCH;
                    }
                }
                else
                {
                    _label.Alpha = 1f;
                }
            }

            public bool Search(string text)
            {
                return _label.Text.ToLower().Contains(text.ToLower());
            }

            public void OnSearchMatch()
            {
                _label.Alpha = 1f;
            }

            private class Slider : Control
            {
                private bool _clicked;
                private int _sliderX;
                private readonly TextBox _text;
                private int _value = -1;

                public Slider(
                    int barWidth,
                    int min,
                    int max,
                    int value,
                    Action<int> valueChanged = null
                )
                {
                    _text = new TextBox(string.Empty, Theme.FONT, Theme.STANDARD_TEXT_SIZE, barWidth, Theme.TEXT_FONT_COLOR, strokeEffect: false);

                    MinValue = min;
                    MaxValue = max;
                    BarWidth = barWidth;
                    AcceptMouseInput = true;
                    AcceptKeyboardInput = true;
                    Width = barWidth;
                    Height = Math.Max(_text.MeasuredSize.Y, 15);

                    CalculateOffset();

                    Value = value;
                    ValueChanged = valueChanged;
                }

                public int MinValue { get; set; }

                public int MaxValue { get; set; }

                public int BarWidth { get; set; }

                public float Percents { get; private set; }

                public int Value
                {
                    get => _value;
                    set
                    {
                        if (_value != value)
                        {
                            int oldValue = _value;
                            _value = /*_newValue =*/
                            value;
                            //if (IsInitialized)
                            //    RecalculateSliderX();

                            if (_value < MinValue)
                            {
                                _value = MinValue;
                            }
                            else if (_value > MaxValue)
                            {
                                _value = MaxValue;
                            }

                            if (_text != null)
                            {
                                _text.Text = Value.ToString();
                            }

                            if (_value != oldValue)
                            {
                                CalculateOffset();
                            }

                            ValueChanged?.Invoke(_value);
                        }
                    }
                }

                public Action<int> ValueChanged { get; }

                public override void Update()
                {
                    base.Update();

                    if (_clicked)
                    {
                        int x = Mouse.Position.X - X - ParentX;
                        int y = Mouse.Position.Y - Y - ParentY;

                        CalculateNew(x);
                    }
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(Theme.BACKGROUND);

                    int mx = x;

                    //Draw background line
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(Color.White),
                        new Rectangle(mx, y + 3, BarWidth, 10),
                        hueVector
                        );

                    hueVector = ShaderHueTranslator.GetHueVector(Theme.SEARCH_BACKGROUND);

                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(Color.White),
                        new Rectangle(mx + _sliderX, y, 15, 16),
                        hueVector
                        );

                    _text?.Draw(batcher, mx + BarWidth + 2, y + (Height >> 1) - (_text.Height >> 1));

                    return base.Draw(batcher, x, y);
                }

                protected override void OnMouseDown(int x, int y, MouseButtonType button)
                {
                    if (button != MouseButtonType.Left)
                    {
                        return;
                    }

                    _clicked = true;
                }

                protected override void OnMouseUp(int x, int y, MouseButtonType button)
                {
                    if (button != MouseButtonType.Left)
                    {
                        return;
                    }

                    _clicked = false;
                    CalculateNew(x);
                }

                protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
                {
                    base.OnKeyUp(key, mod);
                    switch (key)
                    {
                        case SDL.SDL_Keycode.SDLK_LEFT:
                            Value--;
                            break;
                        case SDL.SDL_Keycode.SDLK_RIGHT:
                            Value++;
                            break;
                    }
                }

                protected override void OnMouseEnter(int x, int y)
                {
                    base.OnMouseEnter(x, y);
                    UIManager.KeyboardFocusControl = this;
                }

                private void CalculateNew(int x)
                {
                    int len = BarWidth;
                    int maxValue = MaxValue - MinValue;

                    len -= 15;
                    float perc = x / (float)len * 100.0f;
                    Value = (int)(maxValue * perc / 100.0f) + MinValue;
                    CalculateOffset();
                }

                private void CalculateOffset()
                {
                    if (Value < MinValue)
                    {
                        Value = MinValue;
                    }
                    else if (Value > MaxValue)
                    {
                        Value = MaxValue;
                    }

                    int value = Value - MinValue;
                    int maxValue = MaxValue - MinValue;
                    int length = BarWidth;

                    length -= 15;

                    if (maxValue > 0)
                    {
                        Percents = value / (float)maxValue * 100.0f;
                    }
                    else
                    {
                        Percents = 0;
                    }

                    _sliderX = (int)(length * Percents / 100.0f);

                    if (_sliderX < 0)
                    {
                        _sliderX = 0;
                    }
                }

                public override void Dispose()
                {
                    _text?.Dispose();
                    base.Dispose();
                }
            }
        }

        private class ComboBoxWithLabel : Control, SearchableOption
        {
            private TextBox _label;
            private Combobox _comboBox;
            private readonly string[] options;

            public ComboBoxWithLabel(string label, int labelWidth, int comboWidth, string[] options, int selectedIndex, Action<int, string> onOptionSelected = null)
            {
                AcceptMouseInput = true;
                CanMove = true;

                Add(_label = new TextBox(label, Theme.FONT, Theme.STANDARD_TEXT_SIZE, labelWidth > 0 ? labelWidth : null, Theme.TEXT_FONT_COLOR, strokeEffect: false) { AcceptMouseInput = false });
                Add(_comboBox = new Combobox(comboWidth, options, selectedIndex, onOptionSelected: onOptionSelected) { X = _label.MeasuredSize.X + _label.X + 5 });

                Width = labelWidth + comboWidth + 5;
                Height = Math.Max(_label.MeasuredSize.Y, _comboBox.Height);

                ModernOptionsGump.SearchValueChanged += ModernOptionsGump_SearchValueChanged;
                this.options = options;
            }

            private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
            {
                if (!string.IsNullOrEmpty(ModernOptionsGump.SearchText))
                {
                    if (Search(ModernOptionsGump.SearchText))
                    {
                        OnSearchMatch();
                        ModernOptionsGump.SetParentsForMatchingSearch(this, Page);
                    }
                    else
                    {
                        _label.Alpha = Theme.NO_MATCH_SEARCH;
                    }
                }
                else
                {
                    _label.Alpha = 1f;
                }
            }

            public bool Search(string text)
            {
                if (_label.Text.ToLower().Contains(text.ToLower()))
                {
                    return true;
                }

                foreach (string o in options)
                {
                    if (o.ToLower().Contains(text.ToLower()))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void OnSearchMatch()
            {
                _label.Alpha = 1f;
            }

            public int SelectedIndex => _comboBox.SelectedIndex;

            private class Combobox : Control
            {
                private readonly string[] _items;
                private readonly int _maxHeight;
                private TextBox _label;
                private int _selectedIndex;

                public Combobox
                (
                    int width,
                    string[] items,
                    int selected = -1,
                    int maxHeight = 200,
                    Action<int, string> onOptionSelected = null
                    )
                {
                    Width = width;
                    Height = 25;
                    SelectedIndex = selected;
                    _items = items;
                    _maxHeight = maxHeight;
                    OnOptionSelected = onOptionSelected;
                    AcceptMouseInput = true;

                    string initialText = selected > -1 ? items[selected] : string.Empty;

                    Add(new ColorBox(Width, Height, Theme.SEARCH_BACKGROUND));

                    Add
                    (
                        _label = new TextBox(initialText, Theme.FONT, Theme.STANDARD_TEXT_SIZE, width, Theme.TEXT_FONT_COLOR, strokeEffect: false)
                        {
                            X = 2
                        }
                    );
                    _label.Y = (Height >> 1) - (_label.Height >> 1);
                }

                public int SelectedIndex
                {
                    get => _selectedIndex;
                    set
                    {
                        _selectedIndex = value;

                        if (_items != null)
                        {
                            _label.Text = _items[value];

                            OnOptionSelected?.Invoke(value, _items[value]);
                        }
                    }
                }

                public Action<int, string> OnOptionSelected { get; }

                protected override void OnMouseUp(int x, int y, MouseButtonType button)
                {
                    if (button != MouseButtonType.Left)
                    {
                        return;
                    }

                    int comboY = ScreenCoordinateY + Offset.Y;

                    if (comboY < 0)
                    {
                        comboY = 0;
                    }
                    else if (comboY + _maxHeight > Client.Game.Window.ClientBounds.Height)
                    {
                        comboY = Client.Game.Window.ClientBounds.Height - _maxHeight;
                    }

                    UIManager.Add
                    (
                        new ComboboxGump
                        (
                            ScreenCoordinateX,
                            comboY,
                            Width,
                            _maxHeight,
                            _items,
                            this
                        )
                    );

                    base.OnMouseUp(x, y, button);
                }

                private class ComboboxGump : Gump
                {
                    private readonly Combobox _combobox;

                    public ComboboxGump
                    (
                        int x,
                        int y,
                        int width,
                        int maxHeight,
                        string[] items,
                        Combobox combobox
                    ) : base(0, 0)
                    {
                        CanMove = false;
                        AcceptMouseInput = true;
                        X = x;
                        Y = y;

                        IsModal = true;
                        LayerOrder = UILayer.Over;
                        ModalClickOutsideAreaClosesThisControl = true;

                        _combobox = combobox;

                        ColorBox cb;
                        Add(cb = new ColorBox(width, 0, Theme.BACKGROUND));

                        HoveredLabel[] labels = new HoveredLabel[items.Length];

                        for (int i = 0; i < items.Length; i++)
                        {
                            string item = items[i];

                            if (item == null)
                            {
                                item = string.Empty;
                            }

                            HoveredLabel label = new HoveredLabel
                            (
                                item,
                                Theme.DROPDOWN_OPTION_NORMAL_HUE,
                                Theme.DROPDOWN_OPTION_HOVER_HUE,
                                Theme.DROPDOWN_OPTION_SELECTED_HUE,
                                width
                            )
                            {
                                X = 2,
                                Tag = i,
                                IsSelected = combobox.SelectedIndex == i ? true : false
                            };

                            label.Y = i * label.Height + 5;

                            label.MouseUp += LabelOnMouseUp;

                            labels[i] = label;
                        }

                        int totalHeight = Math.Min(maxHeight, labels.Max(o => o.Y + o.Height));
                        int maxWidth = Math.Max(width, labels.Max(o => o.X + o.Width));

                        ScrollArea area = new ScrollArea
                        (
                            0,
                            0,
                            maxWidth + 15,
                            totalHeight
                        )
                        { AcceptMouseInput = true };

                        foreach (HoveredLabel label in labels)
                        {
                            area.Add(label);
                        }

                        Add(area);

                        cb.Width = maxWidth;
                        cb.Height = totalHeight;
                        Width = maxWidth;
                        Height = totalHeight;
                    }

                    private void LabelOnMouseUp(object sender, MouseEventArgs e)
                    {
                        if (e.Button == MouseButtonType.Left)
                        {
                            _combobox.SelectedIndex = (int)((HoveredLabel)sender).Tag;
                            Dispose();
                        }
                    }

                    private class HoveredLabel : Control
                    {
                        private readonly Color _overHue, _normalHue, _selectedHue;

                        private readonly TextBox _label;

                        public HoveredLabel
                        (
                            string text,
                            Color hue,
                            Color overHue,
                            Color selectedHue,
                            int maxwidth = 0
                        )
                        {
                            _overHue = overHue;
                            _normalHue = hue;
                            _selectedHue = selectedHue;
                            AcceptMouseInput = true;

                            _label = new TextBox(text, Theme.FONT, Theme.STANDARD_TEXT_SIZE, maxwidth > 0 ? maxwidth : null, Theme.TEXT_FONT_COLOR, strokeEffect: false) { AcceptMouseInput = true };
                            Height = _label.MeasuredSize.Y;
                            Width = Math.Max(_label.MeasuredSize.X, maxwidth);

                            IsVisible = !string.IsNullOrEmpty(text);
                        }

                        public bool DrawBackgroundCurrentIndex = true;
                        public bool IsSelected, ForceHover;

                        public Color Hue;

                        public override void Update()
                        {
                            if (IsSelected)
                            {
                                if (Hue != _selectedHue)
                                {
                                    Hue = _selectedHue;
                                    _label.Fontcolor = Hue;
                                }
                            }
                            else if (MouseIsOver || ForceHover)
                            {
                                if (Hue != _overHue)
                                {
                                    Hue = _overHue;
                                    _label.Fontcolor = Hue;
                                }
                            }
                            else if (Hue != _normalHue)
                            {
                                Hue = _normalHue;
                                _label.Fontcolor = Hue;
                            }
                            base.Update();
                        }

                        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                        {
                            if (DrawBackgroundCurrentIndex && MouseIsOver && !string.IsNullOrWhiteSpace(_label.Text))
                            {
                                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                                batcher.Draw
                                (
                                    SolidColorTextureCache.GetTexture(Color.Gray),
                                    new Rectangle
                                    (
                                        x,
                                        y + 2,
                                        Width - 4,
                                        Height - 4
                                    ),
                                    hueVector
                                );
                            }

                            _label.Draw(batcher, x, y);

                            return base.Draw(batcher, x, y);
                        }
                    }
                }
            }
        }

        private class InputFieldWithLabel : Control, SearchableOption
        {
            private readonly InputField _inputField;
            private readonly TextBox _label;

            public InputFieldWithLabel(string label, int inputWidth, string inputText, bool numbersonly = false, EventHandler onTextChange = null)
            {
                AcceptMouseInput = true;
                CanMove = true;

                Add(_label = new TextBox(label, Theme.FONT, Theme.STANDARD_TEXT_SIZE, null, Theme.TEXT_FONT_COLOR, strokeEffect: false) { AcceptMouseInput = false });

                Add(_inputField = new InputField(inputWidth, 40, 0, -1, inputText, numbersonly, onTextChange) { X = _label.Width + _label.X + 5 });

                _label.Y = (_inputField.Height >> 1) - (_label.Height >> 1);

                Width = _label.Width + _inputField.Width + 5;
                Height = Math.Max(_label.Height, _inputField.Height);

                ModernOptionsGump.SearchValueChanged += ModernOptionsGump_SearchValueChanged;
            }

            private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
            {
                if (!string.IsNullOrEmpty(ModernOptionsGump.SearchText))
                {
                    if (Search(ModernOptionsGump.SearchText))
                    {
                        OnSearchMatch();
                        ModernOptionsGump.SetParentsForMatchingSearch(this, Page);
                    }
                    else
                    {
                        _label.Alpha = Theme.NO_MATCH_SEARCH;
                    }
                }
                else
                {
                    _label.Alpha = 1f;
                }
            }

            public bool Search(string text)
            {
                if (_label.Text.ToLower().Contains(text.ToLower()))
                {
                    return true;
                }

                return false;
            }

            public void OnSearchMatch()
            {
                _label.Alpha = 1f;
            }
        }

        private class InputField : Control
        {
            private readonly StbTextBox _textbox;

            public event EventHandler TextChanged { add { _textbox.TextChanged += value; } remove { _textbox.TextChanged -= value; } }

            public InputField
            (
                int width,
                int height,
                int maxWidthText = 0,
                int maxCharsCount = -1,
                string text = "",
                bool numbersOnly = false,
                EventHandler onTextChanges = null
            )
            {
                WantUpdateSize = false;

                Width = width;
                Height = height;

                _textbox = new StbTextBox
                (
                    maxCharsCount,
                    maxWidthText
                )
                {
                    X = 4,
                    Width = width - 8,
                };
                _textbox.Y = (height >> 1) - (_textbox.Height >> 1);
                _textbox.Text = text;
                _textbox.NumbersOnly = numbersOnly;

                Add(new AlphaBlendControl() { Width = Width, Height = Height });
                Add(_textbox);
                if (onTextChanges != null)
                {
                    TextChanged += onTextChanges;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    base.Draw(batcher, x, y);

                    batcher.ClipEnd();
                }

                return true;
            }


            public string Text => _textbox.Text;

            public override bool AcceptKeyboardInput
            {
                get => _textbox.AcceptKeyboardInput;
                set => _textbox.AcceptKeyboardInput = value;
            }

            public bool NumbersOnly
            {
                get => _textbox.NumbersOnly;
                set => _textbox.NumbersOnly = value;
            }


            public void SetText(string text)
            {
                _textbox.SetText(text);
            }


            internal class StbTextBox : Control, ITextEditHandler
            {
                protected static readonly Color SELECTION_COLOR = new Color() { PackedValue = 0x80a06020 };
                private const int FONT_SIZE = 20;
                private readonly int _maxCharCount = -1;


                public StbTextBox
                (
                    int max_char_count = -1,
                    int maxWidth = 0
                )
                {
                    AcceptKeyboardInput = true;
                    AcceptMouseInput = true;
                    CanMove = false;
                    IsEditable = true;

                    _maxCharCount = max_char_count;

                    Stb = new TextEdit(this);
                    Stb.SingleLine = true;

                    _rendererText = new TextBox(string.Empty, Theme.FONT, FONT_SIZE, maxWidth > 0 ? maxWidth : null, Theme.TEXT_FONT_COLOR, strokeEffect: false, supportsCommands: false, ignoreColorCommands: true, calculateGlyphs: true);
                    _rendererCaret = new TextBox("_", Theme.FONT, FONT_SIZE, null, Theme.TEXT_FONT_COLOR, strokeEffect: false, supportsCommands: false, ignoreColorCommands: true);

                    Height = _rendererCaret.Height;
                    LoseFocusOnEscapeKey = true;
                }

                protected TextEdit Stb { get; }

                public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

                public bool AllowTAB { get; set; }
                public bool NoSelection { get; set; }

                public bool LoseFocusOnEscapeKey { get; set; }

                public int CaretIndex
                {
                    get => Stb.CursorIndex;
                    set
                    {
                        Stb.CursorIndex = value;
                        UpdateCaretScreenPosition();
                    }
                }

                public bool Multiline
                {
                    get => !Stb.SingleLine;
                    set => Stb.SingleLine = !value;
                }

                public bool NumbersOnly { get; set; }

                public int SelectionStart
                {
                    get => Stb.SelectStart;
                    set
                    {
                        if (AllowSelection)
                        {
                            Stb.SelectStart = value;
                        }
                    }
                }

                public int SelectionEnd
                {
                    get => Stb.SelectEnd;
                    set
                    {
                        if (AllowSelection)
                        {
                            Stb.SelectEnd = value;
                        }
                    }
                }

                public bool AllowSelection { get; set; } = true;

                internal int TotalHeight
                {
                    get
                    {
                        return _rendererText.Height;
                    }
                }

                public string Text
                {
                    get => _rendererText.Text;

                    set
                    {
                        if (_maxCharCount > 0)
                        {
                            if (value != null && value.Length > _maxCharCount)
                            {
                                value = value.Substring(0, _maxCharCount);
                            }
                        }

                        _rendererText.Text = value;

                        if (!_is_writing)
                        {
                            OnTextChanged();
                        }
                    }
                }

                public int Length => Text?.Length ?? 0;

                public float GetWidth(int index)
                {
                    if (Text != null)
                    {
                        if (index < _rendererText.Text.Length)
                        {
                            var glyphRender = _rendererText.RTL.GetGlyphInfoByIndex(index);
                            if (glyphRender != null)
                            {
                                return glyphRender.Value.Bounds.Width;
                            }
                        }
                    }
                    return 0;
                }

                public TextEditRow LayoutRow(int startIndex)
                {
                    TextEditRow r = new TextEditRow() { num_chars = _rendererText.Text.Length };

                    int sx = ScreenCoordinateX;
                    int sy = ScreenCoordinateY;

                    r.x0 += sx;
                    r.x1 += sx;
                    r.ymin += sy;
                    r.ymax += sy;

                    return r;
                }

                protected Point _caretScreenPosition;
                protected bool _is_writing;
                protected bool _leftWasDown, _fromServer;
                protected TextBox _rendererText, _rendererCaret;

                public event EventHandler TextChanged;

                public void SelectAll()
                {
                    if (AllowSelection)
                    {
                        Stb.SelectStart = 0;
                        Stb.SelectEnd = Length;
                    }
                }

                protected void UpdateCaretScreenPosition()
                {
                    _caretScreenPosition = GetCoordsForIndex(Stb.CursorIndex);
                }

                protected Point GetCoordsForIndex(int index)
                {
                    int x = 0, y = 0;

                    if (Text != null)
                    {
                        if (index < Text.Length)
                        {
                            var glyphRender = _rendererText.RTL.GetGlyphInfoByIndex(index);
                            if (glyphRender != null)
                            {
                                x += glyphRender.Value.Bounds.Left;
                                y += glyphRender.Value.LineTop;
                            }
                        }
                        else if (_rendererText.RTL.Lines != null && _rendererText.RTL.Lines.Count > 0)
                        {
                            // After last glyph
                            var lastLine = _rendererText.RTL.Lines[_rendererText.RTL.Lines.Count - 1];
                            if (lastLine.Count > 0)
                            {
                                var glyphRender = lastLine.GetGlyphInfoByIndex(lastLine.Count - 1);

                                x += glyphRender.Value.Bounds.Right;
                                y += glyphRender.Value.LineTop;
                            }
                            else if (_rendererText.RTL.Lines.Count > 1)
                            {
                                var previousLine = _rendererText.RTL.Lines[_rendererText.RTL.Lines.Count - 2];
                                if (previousLine.Count > 0)
                                {
                                    var glyphRender = previousLine.GetGlyphInfoByIndex(0);
                                    y += glyphRender.Value.LineTop + lastLine.Size.Y + _rendererText.RTL.VerticalSpacing;
                                }
                            }
                        }
                    }

                    return new Point(x, y);
                }

                protected int GetIndexFromCoords(Point coords)
                {
                    if (Text != null)
                    {
                        var line = _rendererText.RTL.GetLineByY(coords.Y);
                        if (line != null)
                        {
                            int? index = line.GetGlyphIndexByX(coords.X);
                            if (index != null)
                            {
                                return (int)index;
                            }
                        }
                    }
                    return 0;
                }

                protected Point GetCoordsForClick(Point clicked)
                {
                    if (Text != null)
                    {
                        var line = _rendererText.RTL.GetLineByY(clicked.Y);
                        if (line != null)
                        {
                            int? index = line.GetGlyphIndexByX(clicked.X);
                            if (index != null)
                            {
                                return GetCoordsForIndex((int)index);
                            }
                        }
                    }
                    return Point.Zero;
                }

                private ControlKeys ApplyShiftIfNecessary(ControlKeys k)
                {
                    if (Keyboard.Shift && !NoSelection)
                    {
                        k |= ControlKeys.Shift;
                    }

                    return k;
                }

                private bool IsMaxCharReached(int count)
                {
                    return _maxCharCount >= 0 && Length + count >= _maxCharCount;
                }

                protected virtual void OnTextChanged()
                {
                    TextChanged?.Raise(this);

                    UpdateCaretScreenPosition();
                }

                internal override void OnFocusEnter()
                {
                    base.OnFocusEnter();
                    CaretIndex = Text?.Length ?? 0;
                }

                internal override void OnFocusLost()
                {
                    if (Stb != null)
                    {
                        Stb.SelectStart = Stb.SelectEnd = 0;
                    }

                    base.OnFocusLost();
                }

                protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
                {
                    ControlKeys? stb_key = null;
                    bool update_caret = false;

                    switch (key)
                    {
                        case SDL.SDL_Keycode.SDLK_TAB:
                            if (AllowTAB)
                            {
                                // UO does not support '\t' char in its fonts
                                OnTextInput("   ");
                            }
                            else
                            {
                                Parent?.KeyboardTabToNextFocus(this);
                            }

                            break;

                        case SDL.SDL_Keycode.SDLK_a when Keyboard.Ctrl && !NoSelection:
                            SelectAll();

                            break;

                        case SDL.SDL_Keycode.SDLK_ESCAPE:
                            if (LoseFocusOnEscapeKey && SelectionStart == SelectionEnd)
                            {
                                UIManager.KeyboardFocusControl = null;
                            }
                            SelectionStart = 0;
                            SelectionEnd = 0;
                            break;

                        case SDL.SDL_Keycode.SDLK_INSERT when IsEditable:
                            stb_key = ControlKeys.InsertMode;

                            break;

                        case SDL.SDL_Keycode.SDLK_c when Keyboard.Ctrl && !NoSelection:
                            int selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                            int selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                            if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                            {
                                SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));
                            }

                            break;

                        case SDL.SDL_Keycode.SDLK_x when Keyboard.Ctrl && !NoSelection:
                            selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                            selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                            if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                            {
                                SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));

                                if (IsEditable)
                                {
                                    Stb.Cut();
                                }
                            }

                            break;

                        case SDL.SDL_Keycode.SDLK_v when Keyboard.Ctrl && IsEditable:
                            OnTextInput(StringHelper.GetClipboardText(Multiline));

                            break;

                        case SDL.SDL_Keycode.SDLK_z when Keyboard.Ctrl && IsEditable:
                            stb_key = ControlKeys.Undo;

                            break;

                        case SDL.SDL_Keycode.SDLK_y when Keyboard.Ctrl && IsEditable:
                            stb_key = ControlKeys.Redo;

                            break;

                        case SDL.SDL_Keycode.SDLK_LEFT:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.WordLeft;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.Left;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.WordLeft;
                            }
                            else
                            {
                                stb_key = ControlKeys.Left;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_RIGHT:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.WordRight;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.Right;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.WordRight;
                            }
                            else
                            {
                                stb_key = ControlKeys.Right;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_UP:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.Up);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_DOWN:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.Down);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_BACKSPACE when IsEditable:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.BackSpace);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_DELETE when IsEditable:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.Delete);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_HOME:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.TextStart;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.LineStart;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.TextStart;
                            }
                            else
                            {
                                stb_key = ControlKeys.LineStart;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_END:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.TextEnd;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.LineEnd;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.TextEnd;
                            }
                            else
                            {
                                stb_key = ControlKeys.LineEnd;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_KP_ENTER:
                        case SDL.SDL_Keycode.SDLK_RETURN:
                            if (IsEditable)
                            {
                                if (Multiline)
                                {
                                    if (!_fromServer && !IsMaxCharReached(0))
                                    {
                                        OnTextInput("\n");
                                    }
                                }
                                else
                                {
                                    Parent?.OnKeyboardReturn(0, Text);

                                    if (UIManager.SystemChat != null && UIManager.SystemChat.TextBoxControl != null && IsFocused)
                                    {
                                        if (!IsFromServer || !UIManager.SystemChat.TextBoxControl.IsVisible)
                                        {
                                            OnFocusLost();
                                            OnFocusEnter();
                                        }
                                        else if (UIManager.KeyboardFocusControl == null || UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl)
                                        {
                                            UIManager.SystemChat.TextBoxControl.SetKeyboardFocus();
                                        }
                                    }
                                }
                            }

                            break;
                    }

                    if (stb_key != null)
                    {
                        Stb.Key(stb_key.Value);
                    }

                    if (update_caret)
                    {
                        UpdateCaretScreenPosition();
                    }

                    base.OnKeyDown(key, mod);
                }

                public void SetText(string text)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        ClearText();
                    }
                    else
                    {
                        if (_maxCharCount > 0)
                        {
                            if (text.Length > _maxCharCount)
                            {
                                text = text.Substring(0, _maxCharCount);
                            }
                        }

                        Stb.ClearState(!Multiline);
                        Text = text;

                        Stb.CursorIndex = Length;

                        if (!_is_writing)
                        {
                            OnTextChanged();
                        }
                    }
                }

                public void ClearText()
                {
                    if (Length != 0)
                    {
                        SelectionStart = 0;
                        SelectionEnd = 0;
                        Stb.Delete(0, Length);

                        if (!_is_writing)
                        {
                            OnTextChanged();
                        }
                    }
                }

                public void AppendText(string text)
                {
                    Stb.Paste(text);
                }

                protected override void OnTextInput(string c)
                {
                    if (c == null || !IsEditable)
                    {
                        return;
                    }

                    _is_writing = true;

                    if (SelectionStart != SelectionEnd)
                    {
                        Stb.DeleteSelection();
                    }

                    int count;

                    if (_maxCharCount > 0)
                    {
                        int remains = _maxCharCount - Length;

                        if (remains <= 0)
                        {
                            _is_writing = false;

                            return;
                        }

                        count = Math.Min(remains, c.Length);

                        if (remains < c.Length && count > 0)
                        {
                            c = c.Substring(0, count);
                        }
                    }
                    else
                    {
                        count = c.Length;
                    }

                    if (count > 0)
                    {
                        if (NumbersOnly)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                if (!char.IsNumber(c[i]))
                                {
                                    _is_writing = false;

                                    return;
                                }
                            }

                            if (_maxCharCount > 0 && int.TryParse(Stb.text + c, out int val))
                            {
                                if (val > _maxCharCount)
                                {
                                    _is_writing = false;
                                    SetText(_maxCharCount.ToString());

                                    return;
                                }
                            }
                        }


                        if (count > 1)
                        {
                            Stb.Paste(c);
                            OnTextChanged();
                        }
                        else
                        {
                            Stb.InputChar(c[0]);
                            OnTextChanged();
                        }
                    }

                    _is_writing = false;
                }

                private int GetXOffset()
                {
                    if (_caretScreenPosition.X > Width)
                    {
                        return _caretScreenPosition.X - Width + 5;
                    }

                    return 0;
                }

                public void Click(Point pos)
                {
                    pos = new Point((pos.X - ScreenCoordinateX), pos.Y - ScreenCoordinateY);
                    CaretIndex = GetIndexFromCoords(pos);
                    SelectionStart = 0;
                    SelectionEnd = 0;
                    Stb.HasPreferredX = false;
                }

                public void Drag(Point pos)
                {
                    pos = new Point((pos.X - ScreenCoordinateX), pos.Y - ScreenCoordinateY);

                    if (SelectionStart == SelectionEnd)
                    {
                        SelectionStart = CaretIndex;
                    }

                    CaretIndex = SelectionEnd = GetIndexFromCoords(pos);
                }

                private protected void DrawSelection(UltimaBatcher2D batcher, int x, int y)
                {
                    if (!AllowSelection)
                    {
                        return;
                    }

                    int selectStart = Math.Min(SelectionStart, SelectionEnd);
                    int selectEnd = Math.Max(SelectionStart, SelectionEnd);

                    if (selectStart < selectEnd)
                    { //Show selection
                        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

                        Point start = GetCoordsForIndex(selectStart);
                        Point size = GetCoordsForIndex(selectEnd);
                        size = new Point(size.X - start.X, _rendererText.Height);

                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                            new Rectangle
                            (
                                x + start.X,
                                y + start.Y,
                                size.X,
                                size.Y
                            ),
                            hueVector
                        );
                    }
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    int slideX = x - GetXOffset();

                    if (batcher.ClipBegin(x, y, Width, Height))
                    {
                        base.Draw(batcher, x, y);
                        DrawSelection(batcher, slideX, y);
                        _rendererText.Draw(batcher, slideX, y);
                        DrawCaret(batcher, slideX, y);
                        batcher.ClipEnd();
                    }

                    return true;
                }

                protected virtual void DrawCaret(UltimaBatcher2D batcher, int x, int y)
                {
                    if (HasKeyboardFocus)
                    {
                        _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
                    }
                }

                protected override void OnMouseDown(int x, int y, MouseButtonType button)
                {
                    if (button == MouseButtonType.Left && IsEditable)
                    {
                        if (!NoSelection)
                        {
                            _leftWasDown = true;
                        }

                        Click(new Point(x + ScreenCoordinateX + GetXOffset(), y + ScreenCoordinateY));
                    }

                    base.OnMouseDown(x, y, button);
                }

                protected override void OnMouseUp(int x, int y, MouseButtonType button)
                {
                    if (button == MouseButtonType.Left)
                    {
                        _leftWasDown = false;
                    }

                    base.OnMouseUp(x, y, button);
                }

                protected override void OnMouseOver(int x, int y)
                {
                    base.OnMouseOver(x, y);

                    if (!_leftWasDown)
                    {
                        return;
                    }

                    Drag(new Point(x + ScreenCoordinateX + GetXOffset(), y + ScreenCoordinateY));
                }

                public override void Dispose()
                {
                    _rendererText?.Dispose();
                    _rendererCaret?.Dispose();

                    base.Dispose();
                }

                protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
                {
                    if (!NoSelection && CaretIndex < Text.Length && CaretIndex >= 0 && !char.IsWhiteSpace(Text[CaretIndex]))
                    {
                        int idx = CaretIndex;

                        if (idx - 1 >= 0 && char.IsWhiteSpace(Text[idx - 1]))
                        {
                            ++idx;
                        }

                        SelectionStart = Stb.MoveToPreviousWord(idx);
                        SelectionEnd = Stb.MoveToNextWord(idx);

                        if (SelectionEnd < Text.Length)
                        {
                            --SelectionEnd;
                        }

                        return true;
                    }

                    return base.OnMouseDoubleClick(x, y, button);
                }
            }
        }

        private class InfoBarBuilderControl : Control
        {
            private readonly InputField infoLabel;
            private readonly ModernColorPickerWithLabel labelColor;
            private readonly ComboBoxWithLabel varStat;

            public InfoBarBuilderControl(InfoBarItem item)
            {
                AcceptMouseInput = true;
                infoLabel = new InputField(130, 40, text: item.label, onTextChanges: (s, e) => { item.label = ((InputField.StbTextBox)s).Text; UIManager.GetGump<InfoBarGump>()?.ResetItems(); }) { X = 5 };

                string[] dataVars = InfoBarManager.GetVars();

                varStat = new ComboBoxWithLabel(string.Empty, 0, 170, dataVars, (int)item.var, onOptionSelected: (i, s) => { item.var = (InfoBarVars)i; UIManager.GetGump<InfoBarGump>()?.ResetItems(); }) { X = 200, Y = 8 };

                labelColor = new ModernColorPickerWithLabel(string.Empty, item.hue, (h) => { item.hue = h; UIManager.GetGump<InfoBarGump>()?.ResetItems(); }) { X = 150, Y = 10 };


                ModernButton deleteButton = new ModernButton
                (
                    390,
                    8,
                    60,
                    25,
                    ButtonAction.Activate,
                    "Delete",
                    Theme.BUTTON_FONT_COLOR
                )
                { ButtonParameter = 999 };

                deleteButton.MouseUp += (sender, e) =>
                {
                    Dispose();
                    if (Parent != null && Parent is DataBox db)
                    {
                        db.Remove(this);
                        db.ReArrangeChildren();
                        db.ForceSizeUpdate();
                    }
                    Client.Game.GetScene<GameScene>().InfoBars?.RemoveItem(item);
                    UIManager.GetGump<InfoBarGump>()?.ResetItems();
                };

                Add(infoLabel);
                Add(varStat);
                Add(labelColor);
                Add(deleteButton);

                ForceSizeUpdate();
            }

            public override void Update()
            {
                if (IsDisposed)
                {
                    return;
                }

                if (Children.Count != 0)
                {
                    for (int i = 0; i < Children.Count; i++)
                    {
                        Control c = Children[i];

                        if (c.IsDisposed)
                        {
                            OnChildRemoved();
                            Children.RemoveAt(i--);

                            continue;
                        }

                        c.Update();
                    }
                }

            }

            public string LabelText => infoLabel.Text;
            public InfoBarVars Var => (InfoBarVars)varStat.SelectedIndex;
            public ushort Hue => labelColor.Hue;
        }

        private class LeftSideMenuRightSideContent : Control
        {
            private ScrollArea left, right;
            private int leftY, rightY = Theme.TOP_PADDING, leftX, rightX;

            public ScrollArea LeftArea => left;
            public ScrollArea RightArea => right;

            public new int ActivePage
            {
                get => base.ActivePage;
                set
                {
                    base.ActivePage = value;
                    right.ActivePage = value;
                }
            }

            public LeftSideMenuRightSideContent(int width, int height, int leftWidth, int page = 0)
            {
                Width = width;
                Height = height;
                CanMove = true;
                CanCloseWithRightClick = true;
                AcceptMouseInput = true;

                Add(new AlphaBlendControl() { Width = leftWidth, Height = Height, CanMove = true }, page);
                Add(left = new ScrollArea(0, 0, leftWidth, height) { CanMove = true, AcceptMouseInput = true }, page);
                Add(right = new ScrollArea(leftWidth, 0, Width - leftWidth, height) { CanMove = true, AcceptMouseInput = true }, page);

                LeftWidth = leftWidth - Theme.SCROLL_BAR_WIDTH;
                RightWidth = Width - leftWidth;
            }

            public int LeftWidth { get; }
            public int RightWidth { get; }

            public void AddToLeft(Control c, bool autoPosition = true, int page = 0)
            {
                if (autoPosition)
                {
                    c.Y = leftY;
                    c.X = leftX;
                    leftY += c.Height;
                }

                left.Add(c, page);
            }

            public void AddToRight(Control c, bool autoPosition = true, int page = 0)
            {
                if (autoPosition)
                {
                    c.Y = rightY;
                    c.X = rightX;
                    rightY += c.Height + Theme.TOP_PADDING;
                }

                right.Add(c, page);
            }

            public void BlankLine()
            {
                rightY += Theme.BLANK_LINE;
            }

            public void Indent()
            {
                rightX += Theme.INDENT_SPACE;
            }

            public void RemoveIndent()
            {
                rightX -= Theme.INDENT_SPACE;
                if (rightX < 0)
                {
                    rightX = 0;
                }
            }

            public void ResetRightSide()
            {
                rightY = Theme.TOP_PADDING;
                rightX = 0;
            }

            public void SetMatchingButton(int page)
            {
                foreach (Control c in left.Children)
                {
                    if (c is ModernButton button && button.ButtonParameter == page)
                    {
                        ((SearchableOption)button).OnSearchMatch();
                        int p = Parent == null ? Page : Parent.Page;
                        ModernOptionsGump.SetParentsForMatchingSearch(this, p);
                    }
                }
            }
        }

        private class ModernButton : HitBox, SearchableOption
        {
            private readonly ButtonAction _action;
            private readonly int _groupnumber;
            private bool _isSelected;

            public bool DisplayBorder;

            public bool FullPageSwitch;

            public ModernButton
            (
                int x,
                int y,
                int w,
                int h,
                ButtonAction action,
                string text,
                Color fontColor,
                int groupnumber = 0,
                FontStashSharp.RichText.TextHorizontalAlignment align = FontStashSharp.RichText.TextHorizontalAlignment.Center
            ) : base(x, y, w, h)
            {
                _action = action;

                Add
                (
                    TextLabel = new TextBox(text, Theme.FONT, 20, w, fontColor, align, false)
                );

                TextLabel.Y = (h - TextLabel.Height) >> 1;
                _groupnumber = groupnumber;

                ModernOptionsGump.SearchValueChanged += ModernOptionsGump_SearchValueChanged;
            }

            private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
            {
                if (!string.IsNullOrEmpty(ModernOptionsGump.SearchText))
                {
                    if (Search(SearchText))
                    {
                        OnSearchMatch();
                        ModernOptionsGump.SetParentsForMatchingSearch(this, Page);
                    }
                    else
                    {
                        TextLabel.Alpha = Theme.NO_MATCH_SEARCH;
                    }
                }
                else
                {
                    TextLabel.Alpha = 1f;
                }
            }

            internal TextBox TextLabel { get; }

            public int ButtonParameter { get; set; }

            public bool IsSelectable { get; set; } = true;

            public bool IsSelected
            {
                get => _isSelected && IsSelectable;
                set
                {
                    if (!IsSelectable)
                    {
                        return;
                    }

                    _isSelected = value;

                    if (value)
                    {
                        Control p = Parent;

                        if (p == null)
                        {
                            return;
                        }

                        IEnumerable<ModernButton> list = p.FindControls<ModernButton>();

                        foreach (ModernButton b in list)
                        {
                            if (b != this && b._groupnumber == _groupnumber)
                            {
                                b.IsSelected = false;
                            }
                        }
                    }
                }
            }

            internal static ModernButton GetSelected(Control p, int group)
            {
                IEnumerable<ModernButton> list = p.FindControls<ModernButton>();

                foreach (ModernButton b in list)
                {
                    if (b._groupnumber == group && b.IsSelected)
                    {
                        return b;
                    }
                }

                return null;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    IsSelected = true;

                    if (_action == ButtonAction.SwitchPage)
                    {
                        if (!FullPageSwitch)
                        {
                            if (Parent != null)
                            { //Scroll area
                                Parent.ActivePage = ButtonParameter;
                                if (Parent.Parent != null && Parent.Parent is LeftSideMenuRightSideContent)
                                { //LeftSideMenuRightSideContent
                                    ((LeftSideMenuRightSideContent)Parent.Parent).ActivePage = ButtonParameter;
                                }
                            }
                        }
                        else
                        {
                            ChangePage(ButtonParameter);
                        }
                    }
                    else
                    {
                        OnButtonClick(ButtonParameter);
                    }
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (IsSelected)
                {
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

                    batcher.Draw
                    (
                        _texture,
                        new Vector2(x, y),
                        new Rectangle(0, 0, Width, Height),
                        hueVector
                    );
                }

                if (DisplayBorder)
                {
                    batcher.DrawRectangle(
                        SolidColorTextureCache.GetTexture(Color.LightGray),
                        x, y,
                        Width, Height,
                        ShaderHueTranslator.GetHueVector(0, false, Alpha)
                        );
                }

                return base.Draw(batcher, x, y);
            }

            public bool Search(string text)
            {
                return TextLabel.Text.ToLower().Contains(text.ToLower());
            }

            public void OnSearchMatch()
            {
                TextLabel.Alpha = 1f;
            }
        }

        private class ScrollArea : Control
        {
            private ScrollBar _scrollBar;

            public ScrollBar GetScrollBar { get { return _scrollBar; } set { _scrollBar = value; } }

            public ScrollArea
            (
                int x,
                int y,
                int w,
                int h,
                int scroll_max_height = -1
            )
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;

                _scrollBar = new ScrollBar(Width - Theme.SCROLL_BAR_WIDTH, 0, Height);

                ScrollMaxHeight = scroll_max_height;

                _scrollBar.MinValue = 0;
                _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
                _scrollBar.Parent = this;
                _scrollBar.IsVisible = true;

                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
            }

            public int ScrollMaxHeight { get; set; } = -1;
            public int ScrollValue => _scrollBar.Value;
            public int ScrollMinValue => _scrollBar.MinValue;
            public int ScrollMaxValue => _scrollBar.MaxValue;

            public void ToggleScrollBarVisibility(bool visible = true)
            {
                if (_scrollBar != null)
                {
                    _scrollBar.IsVisible = visible;
                }
            }

            public override void Update()
            {
                base.Update();

                CalculateScrollBarMaxValue();
            }

            public void Scroll(bool isup)
            {
                if (isup)
                {
                    _scrollBar.Value -= _scrollBar.ScrollStep;
                }
                else
                {
                    _scrollBar.Value += _scrollBar.ScrollStep;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                int sbar = 0, start = 0;

                if (_scrollBar != null)
                {
                    _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);
                    sbar = _scrollBar.Width;
                    start = 1;
                }

                if (batcher.ClipBegin(x, y, Width - sbar, Height))
                {
                    for (int i = start; i < Children.Count; i++)
                    {
                        Control child = Children[i];

                        if (!child.IsVisible || (child.Page != ActivePage && child.Page != 0))
                        {
                            continue;
                        }

                        int finalY = y + child.Y - (_scrollBar == null ? 0 : _scrollBar.Value);

                        child.Draw(batcher, x + child.X, finalY);
                    }

                    batcher.ClipEnd();
                }

                return true;
            }

            protected override void OnMouseEnter(int x, int y)
            {
                base.OnMouseEnter(x, y);
                if (UIManager.KeyboardFocusControl == UIManager.SystemChat.TextBoxControl || UIManager.KeyboardFocusControl == null)
                {
                    UIManager.KeyboardFocusControl = this; //Dirty fix for mouse wheel macros
                }
            }

            protected override void OnMouseWheel(MouseEventType delta)
            {
                if (IsDisposed || _scrollBar == null)
                {
                    return;
                }

                switch (delta)
                {
                    case MouseEventType.WheelScrollUp:
                        _scrollBar.Value -= _scrollBar.ScrollStep;
                        break;

                    case MouseEventType.WheelScrollDown:
                        _scrollBar.Value += _scrollBar.ScrollStep;
                        break;
                }
            }

            public override void Clear()
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].Dispose();
                }
            }

            private void CalculateScrollBarMaxValue()
            {
                if (_scrollBar == null)
                {
                    return;
                }

                _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

                int startY = 0, endY = 0;

                for (int i = 1; i < Children.Count; i++)
                {
                    Control c = Children[i];

                    if (c.IsVisible && !c.IsDisposed && (c.Page == 0 || c.Page == ActivePage))
                    {
                        if (c.Y < startY)
                        {
                            startY = c.Y;
                        }

                        if (c.Bounds.Bottom > endY)
                        {
                            endY = c.Bounds.Bottom;
                        }
                    }
                }

                int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
                height = Math.Max(0, height);

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.Value = _scrollBar.MaxValue = 0;
                }

                _scrollBar.UpdateOffset(0, Offset.Y);

                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].UpdateOffset(0, -_scrollBar.Value);
                }
            }

            public class ScrollBar : ScrollBarBase
            {
                private Rectangle _rectSlider,
                    _emptySpace;

                private Vector3 hueVector = ShaderHueTranslator.GetHueVector(Theme.BACKGROUND, false, 0.75f);
                private Vector3 hueVectorForeground = ShaderHueTranslator.GetHueVector(Theme.BLACK, false, 0.75f);
                private Texture2D whiteTexture = SolidColorTextureCache.GetTexture(Color.White);

                public ScrollBar(int x, int y, int height)
                {
                    Height = height;
                    Location = new Point(x, y);
                    AcceptMouseInput = true;

                    Width = Theme.SCROLL_BAR_WIDTH;

                    _rectSlider = new Rectangle(
                        0,
                        _sliderPosition,
                        Width,
                        20
                    );

                    _emptySpace.X = 0;
                    _emptySpace.Y = 0;
                    _emptySpace.Width = Width;
                    _emptySpace.Height = Height;
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    if (Height <= 0 || !IsVisible || IsDisposed)
                    {
                        return false;
                    }

                    // draw scrollbar background
                    batcher.Draw(
                        whiteTexture,
                        new Rectangle(x, y, Width, Height),
                        hueVector
                    );


                    // draw slider
                    if (MaxValue > MinValue)
                    {
                        batcher.Draw(
                            whiteTexture,
                            new Rectangle(x, y + _sliderPosition, Width, 20),
                            hueVectorForeground
                        );
                    }

                    return true;// base.Draw(batcher, x, y);
                }

                protected override int GetScrollableArea()
                {
                    return Height - _rectSlider.Height;
                }

                protected override void OnMouseDown(int x, int y, MouseButtonType button)
                {
                    base.OnMouseDown(x, y, button);

                    if (_btnSliderClicked && _emptySpace.Contains(x, y))
                    {
                        CalculateByPosition(x, y);
                    }
                }

                protected override void CalculateByPosition(int x, int y)
                {
                    if (y != _clickPosition.Y)
                    {
                        y -= _emptySpace.Y + (_rectSlider.Height >> 1);

                        if (y < 0)
                        {
                            y = 0;
                        }

                        int scrollableArea = GetScrollableArea();

                        if (y > scrollableArea)
                        {
                            y = scrollableArea;
                        }

                        _sliderPosition = y;
                        _clickPosition.X = x;
                        _clickPosition.Y = y;

                        if (
                            y == 0
                            && _clickPosition.Y < (_rectSlider.Height >> 1)
                        )
                        {
                            _clickPosition.Y = _rectSlider.Height >> 1;
                        }
                        else if (
                            y == scrollableArea
                            && _clickPosition.Y
                                > Height - (_rectSlider.Height >> 1)
                        )
                        {
                            _clickPosition.Y =
                                Height - (_rectSlider.Height >> 1);
                        }

                        _value = (int)
                            Math.Round(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
                    }
                }

                public override bool Contains(int x, int y)
                {
                    return x >= 0 && x <= Width && y >= 0 && y <= Height;
                }
            }
        }

        private class MacroControl : Control
        {
            private static readonly string[] _allHotkeysNames = Enum.GetNames(typeof(MacroType));
            private static readonly string[] _allSubHotkeysNames = Enum.GetNames(typeof(MacroSubType));
            private readonly DataBox _databox;
            private readonly HotkeyBox _hotkeyBox;

            private enum buttonsOption
            {
                AddBtn,
                RemoveBtn,
                CreateNewMacro,
                OpenMacroOptions,
                OpenButtonEditor
            }

            public MacroControl(string name)
            {
                CanMove = true;
                TextBox _keyBinding;
                Add(_keyBinding = new TextBox("Hotkey", Theme.FONT, Theme.STANDARD_TEXT_SIZE, null, Theme.TEXT_FONT_COLOR, strokeEffect: false));

                _hotkeyBox = new HotkeyBox();
                _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
                _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;
                _hotkeyBox.X = _keyBinding.X + _keyBinding.Width + 5;


                Add(_hotkeyBox);

                Control c;
                Add(c = new ModernButton(0, _hotkeyBox.Height + 3, 200, 40, ButtonAction.Activate, ResGumps.CreateMacroButton, Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)buttonsOption.CreateNewMacro, IsSelectable = true, IsSelected = true });
                Add(c = new ModernButton(c.Width + c.X + 10, c.Y, 200, 40, ButtonAction.Activate, ResGumps.MacroButtonEditor, Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)buttonsOption.OpenButtonEditor, IsSelectable = true, IsSelected = true });

                Add(c = new Line(0, c.Y + c.Height + 5, 325, 1, Color.Gray.PackedValue));

                Add(c = new ModernButton(0, c.Y + 5, 75, 40, ButtonAction.Activate, ResGumps.Add, Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)buttonsOption.AddBtn, IsSelectable = false });

                Add(_databox = new DataBox(0, c.Y + c.Height + 5, 280, 280));

                Macro = Client.Game.GetScene<GameScene>().Macros.FindMacro(name) ?? Macro.CreateEmptyMacro(name);

                SetupKeyByDefault();
                SetupMacroUI();
            }

            public Macro Macro { get; }

            private void AddEmptyMacro()
            {
                MacroObject ob = (MacroObject)Macro.Items;

                if (ob == null || ob.Code == MacroType.None)
                {
                    return;
                }

                while (ob.Next != null)
                {
                    MacroObject next = (MacroObject)ob.Next;

                    if (next.Code == MacroType.None)
                    {
                        return;
                    }

                    ob = next;
                }

                MacroObject obj = Macro.Create(MacroType.None);

                Macro.PushToBack(obj);

                _databox.Add(new MacroEntry(this, obj, _allHotkeysNames));
                _databox.ReArrangeChildren();
                _databox.ForceSizeUpdate();
                ForceSizeUpdate();
            }

            private void RemoveLastCommand()
            {
                if (_databox.Children.Count != 0)
                {
                    LinkedObject last = Macro.GetLast();

                    Macro.Remove(last);

                    _databox.Children[_databox.Children.Count - 1].Dispose();

                    SetupMacroUI();
                }

                if (_databox.Children.Count == 0)
                {
                    AddEmptyMacro();
                }
            }

            private void SetupMacroUI()
            {
                if (Macro == null)
                {
                    return;
                }

                _databox.Clear();
                _databox.Children.Clear();

                if (Macro.Items == null)
                {
                    Macro.Items = Macro.Create(MacroType.None);
                }

                MacroObject obj = (MacroObject)Macro.Items;
                while (obj != null)
                {
                    _databox.Add(new MacroEntry(this, obj, _allHotkeysNames));

                    if (obj.Next != null && obj.Code == MacroType.None)
                    {
                        break;
                    }
                    obj = (MacroObject)obj.Next;
                }

                _databox.ReArrangeChildren();
                _databox.ForceSizeUpdate();
            }

            private void SetupKeyByDefault()
            {
                if (Macro == null || _hotkeyBox == null)
                {
                    return;
                }

                if (Macro.ControllerButtons != null && Macro.ControllerButtons.Length > 0)
                {
                    _hotkeyBox.SetButtons(Macro.ControllerButtons);
                }

                SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE;

                if (Macro.Alt)
                {
                    mod |= SDL.SDL_Keymod.KMOD_ALT;
                }

                if (Macro.Shift)
                {
                    mod |= SDL.SDL_Keymod.KMOD_SHIFT;
                }

                if (Macro.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.KMOD_CTRL;
                }

                if (Macro.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    _hotkeyBox.SetKey(Macro.Key, mod);
                }
                else if (Macro.MouseButton != MouseButtonType.None)
                {
                    _hotkeyBox.SetMouseButton(Macro.MouseButton, mod);
                }
                else if (Macro.WheelScroll == true)
                {
                    _hotkeyBox.SetMouseWheel(Macro.WheelUp, mod);
                }
            }

            private void BoxOnHotkeyChanged(object sender, EventArgs e)
            {
                bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
                bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
                bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

                if (_hotkeyBox.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    Macro macro = Client.Game.GetScene<GameScene>().Macros.FindMacro(_hotkeyBox.Key, alt, ctrl, shift);

                    if (macro != null)
                    {
                        if (Macro == macro)
                        {
                            return;
                        }

                        SetupKeyByDefault();
                        UIManager.Add(new MessageBoxGump(250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, macro.Name), null));

                        return;
                    }
                }
                else if (_hotkeyBox.MouseButton != MouseButtonType.None)
                {
                    Macro macro = Client.Game.GetScene<GameScene>().Macros.FindMacro(_hotkeyBox.MouseButton, alt, ctrl, shift);

                    if (macro != null)
                    {
                        if (Macro == macro)
                        {
                            return;
                        }

                        SetupKeyByDefault();
                        UIManager.Add(new MessageBoxGump(250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, macro.Name), null));

                        return;
                    }
                }
                else if (_hotkeyBox.WheelScroll == true)
                {
                    Macro macro = Client.Game.GetScene<GameScene>().Macros.FindMacro(_hotkeyBox.WheelUp, alt, ctrl, shift);

                    if (macro != null)
                    {
                        if (Macro == macro)
                        {
                            return;
                        }

                        SetupKeyByDefault();
                        UIManager.Add(new MessageBoxGump(250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, macro.Name), null));

                        return;
                    }
                }
                else if (_hotkeyBox.Buttons != null && _hotkeyBox.Buttons.Length > 0)
                {

                }
                else
                {
                    return;
                }

                Macro m = Macro;
                if (_hotkeyBox.Buttons != null && _hotkeyBox.Buttons.Length > 0)
                {
                    m.ControllerButtons = _hotkeyBox.Buttons;
                }
                m.Key = _hotkeyBox.Key;
                m.MouseButton = _hotkeyBox.MouseButton;
                m.WheelScroll = _hotkeyBox.WheelScroll;
                m.WheelUp = _hotkeyBox.WheelUp;
                m.Shift = shift;
                m.Alt = alt;
                m.Ctrl = ctrl;
            }

            private void BoxOnHotkeyCancelled(object sender, EventArgs e)
            {
                Macro m = Macro;
                m.Alt = m.Ctrl = m.Shift = false;
                m.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                m.MouseButton = MouseButtonType.None;
                m.WheelScroll = false;
            }

            public override void OnButtonClick(int buttonID)
            {
                switch (buttonID)
                {
                    case (int)buttonsOption.AddBtn:
                        AddEmptyMacro();
                        break;
                    case (int)buttonsOption.RemoveBtn:
                        RemoveLastCommand();
                        break;
                    case (int)buttonsOption.CreateNewMacro:
                        UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == Macro)?.Dispose();

                        MacroButtonGump macroButtonGump = new MacroButtonGump(Macro, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(macroButtonGump);
                        break;
                    case (int)buttonsOption.OpenMacroOptions:
                        UIManager.Gumps.OfType<MacroGump>().FirstOrDefault()?.Dispose();

                        GameActions.OpenSettings(4);
                        break;
                    case (int)buttonsOption.OpenButtonEditor:
                        UIManager.Gumps.OfType<MacroButtonEditorGump>().FirstOrDefault()?.Dispose();
                        OpenMacroButtonEditor(Macro, null);
                        break;
                }
            }

            private void OpenMacroButtonEditor(Macro macro, Vector2? position = null)
            {
                MacroButtonEditorGump btnEditorGump = UIManager.GetGump<MacroButtonEditorGump>();

                if (btnEditorGump == null)
                {
                    var posX = (Client.Game.Window.ClientBounds.Width >> 1) - 300;
                    var posY = (Client.Game.Window.ClientBounds.Height >> 1) - 250;
                    Gump opt = UIManager.GetGump<ModernOptionsGump>();
                    if (opt != null)
                    {
                        posX = opt.X + opt.Width + 5;
                        posY = opt.Y;
                    }
                    if (position.HasValue)
                    {
                        posX = (int)position.Value.X;
                        posY = (int)position.Value.Y;
                    }
                    btnEditorGump = new MacroButtonEditorGump(macro, posX, posY);
                    UIManager.Add(btnEditorGump);
                }
                btnEditorGump.SetInScreen();
                btnEditorGump.BringOnTop();
            }

            private class MacroEntry : Control
            {
                private readonly MacroControl _control;
                private readonly MacroObject _obj;
                private readonly string[] _items;
                public event EventHandler<MacroObject> OnDelete;
                ComboBoxWithLabel mainBox;

                public MacroEntry(MacroControl control, MacroObject obj, string[] items)
                {
                    _control = control;
                    _items = items;
                    _obj = obj;

                    mainBox = new ComboBoxWithLabel(string.Empty, 0, 200, _items, (int)obj.Code, BoxOnOnOptionSelected) { Tag = obj };

                    Add(mainBox);

                    Control c;
                    Add(c = new ModernButton(mainBox.Width + 10, 0, 75, 40, ButtonAction.Activate, ResGumps.Remove, Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)buttonsOption.RemoveBtn, IsSelectable = false });

                    mainBox.Y = (c.Height >> 1) - (mainBox.Height >> 1);

                    Height = c.Height;

                    AddSubMacro(obj);

                    ForceSizeUpdate();
                }


                private void AddSubMacro(MacroObject obj)
                {
                    if (obj == null || obj.Code == 0)
                    {
                        return;
                    }

                    switch (obj.SubMenuType)
                    {
                        case 1:
                            int count = 0;
                            int offset = 0;
                            Macro.GetBoundByCode(obj.Code, ref count, ref offset);

                            string[] names = new string[count];

                            for (int i = 0; i < count; i++)
                            {
                                names[i] = _allSubHotkeysNames[i + offset];
                            }

                            ComboBoxWithLabel sub = new ComboBoxWithLabel(string.Empty, 0, 200, names, (int)obj.SubCode - offset, (i, s) =>
                            {
                                Macro.GetBoundByCode(obj.Code, ref count, ref offset);
                                MacroSubType subType = (MacroSubType)(offset + i);
                                obj.SubCode = subType;
                            })
                            { Tag = obj, X = 20, Y = Height };

                            Add(sub);

                            //Height += sub.Height;
                            break;

                        case 2:
                            InputField textbox = new InputField(400, 40, 0, 80, obj.HasString() ? ((MacroObjectString)obj).Text : string.Empty, false, (s, e) =>
                            {
                                if (obj.HasString())
                                {
                                    ((MacroObjectString)obj).Text = ((InputField.StbTextBox)s).Text;
                                }
                            })
                            {
                                X = 20,
                                Y = Height
                            };

                            textbox.SetText(obj.HasString() ? ((MacroObjectString)obj).Text : string.Empty);

                            Add(textbox);

                            break;
                    }
                    ForceSizeUpdate();
                    _control._databox.ReArrangeChildren();
                    _control._databox.ForceSizeUpdate();
                    _control.ForceSizeUpdate();
                }

                public override void OnButtonClick(int buttonID)
                {
                    switch (buttonID)
                    {
                        case (int)buttonsOption.RemoveBtn:

                            _control.Macro.Remove(_obj);
                            Dispose();
                            _control._databox.ReArrangeChildren();
                            _control._databox.ForceSizeUpdate();
                            _control.ForceSizeUpdate();
                            //_control.SetupMacroUI();
                            OnDelete?.Invoke(this, _obj);
                            break;
                    }
                }

                private void BoxOnOnOptionSelected(int selected, string val)
                {
                    WantUpdateSize = true;

                    MacroObject currentMacroObj = _obj;

                    if (selected == 0)
                    {
                        _control.Macro.Remove(currentMacroObj);

                        mainBox.Tag = null;

                        Dispose();

                        _control.SetupMacroUI();
                    }
                    else
                    {
                        MacroObject newMacroObj = Macro.Create((MacroType)selected);

                        _control.Macro.Insert(currentMacroObj, newMacroObj);
                        _control.Macro.Remove(currentMacroObj);

                        mainBox.Tag = newMacroObj;


                        for (int i = 2; i < Children.Count; i++)
                        {
                            Children[i]?.Dispose();
                        }
                        AddSubMacro(newMacroObj);
                    }
                }
            }
        }

        private class HotkeyBox : Control
        {
            private bool _actived;
            private readonly ModernButton _buttonOK, _buttonCancel;
            private readonly TextBox _label;

            public HotkeyBox()
            {
                CanMove = false;
                AcceptMouseInput = true;
                AcceptKeyboardInput = true;

                Width = 300;
                Height = 40;

                AlphaBlendControl bg = new AlphaBlendControl() { Width = 150, Height = 40, AcceptMouseInput = true };
                Add(bg);
                bg.MouseUp += LabelOnMouseUp;

                Add(_label = new TextBox("None", Theme.FONT, Theme.STANDARD_TEXT_SIZE, 150, Theme.TEXT_FONT_COLOR, align: FontStashSharp.RichText.TextHorizontalAlignment.Center, strokeEffect: false));
                _label.Y = (bg.Height >> 1) - (_label.Height >> 1);

                _label.MouseUp += LabelOnMouseUp;

                Add(_buttonOK = new ModernButton(152, 0, 75, 40, ButtonAction.Activate, "Save", Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)ButtonState.Ok });

                Add(_buttonCancel = new ModernButton(_buttonOK.Bounds.Right + 5, 0, 75, 40, ButtonAction.Activate, "Cancel", Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)ButtonState.Cancel });

                WantUpdateSize = false;
                IsActive = false;
            }

            public SDL.SDL_Keycode Key { get; private set; }
            public SDL.SDL_GameControllerButton[] Buttons { get; private set; }
            public MouseButtonType MouseButton { get; private set; }
            public bool WheelScroll { get; private set; }
            public bool WheelUp { get; private set; }
            public SDL.SDL_Keymod Mod { get; private set; }

            public bool IsActive
            {
                get => _actived;
                set
                {
                    _actived = value;

                    if (value)
                    {
                        _buttonOK.IsVisible = _buttonCancel.IsVisible = true;
                        _buttonOK.IsEnabled = _buttonCancel.IsEnabled = true;
                    }
                    else
                    {
                        _buttonOK.IsVisible = _buttonCancel.IsVisible = false;
                        _buttonOK.IsEnabled = _buttonCancel.IsEnabled = false;
                    }
                }
            }

            public event EventHandler HotkeyChanged, HotkeyCancelled;

            protected override void OnControllerButtonDown(SDL.SDL_GameControllerButton button)
            {
                if (IsActive)
                {
                    SetButtons(Controller.PressedButtons());
                }
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                if (IsActive)
                {
                    SetKey(key, mod);
                }
            }

            public void SetButtons(SDL.SDL_GameControllerButton[] buttons)
            {
                ResetBinding();
                Buttons = buttons;
                _label.Text = Controller.GetButtonNames(buttons);
            }

            public void SetKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                if (key == SDL.SDL_Keycode.SDLK_UNKNOWN && mod == SDL.SDL_Keymod.KMOD_NONE)
                {
                    ResetBinding();

                    Key = key;
                    Mod = mod;
                }
                else
                {
                    string newvalue = KeysTranslator.TryGetKey(key, mod);

                    if (!string.IsNullOrEmpty(newvalue) && key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                    {
                        ResetBinding();

                        Key = key;
                        Mod = mod;
                        _label.Text = newvalue;
                    }
                }
            }

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Middle || button == MouseButtonType.XButton1 || button == MouseButtonType.XButton2)
                {
                    SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE;

                    if (Keyboard.Alt)
                    {
                        mod |= SDL.SDL_Keymod.KMOD_ALT;
                    }

                    if (Keyboard.Shift)
                    {
                        mod |= SDL.SDL_Keymod.KMOD_SHIFT;
                    }

                    if (Keyboard.Ctrl)
                    {
                        mod |= SDL.SDL_Keymod.KMOD_CTRL;
                    }

                    SetMouseButton(button, mod);
                }
            }

            public void SetMouseButton(MouseButtonType button, SDL.SDL_Keymod mod)
            {
                string newvalue = KeysTranslator.GetMouseButton(button, mod);

                if (!string.IsNullOrEmpty(newvalue) && button != MouseButtonType.None)
                {
                    ResetBinding();

                    MouseButton = button;
                    Mod = mod;
                    _label.Text = newvalue;
                }
            }

            protected override void OnMouseWheel(MouseEventType delta)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE;

                if (Keyboard.Alt)
                {
                    mod |= SDL.SDL_Keymod.KMOD_ALT;
                }

                if (Keyboard.Shift)
                {
                    mod |= SDL.SDL_Keymod.KMOD_SHIFT;
                }

                if (Keyboard.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.KMOD_CTRL;
                }

                if (delta == MouseEventType.WheelScrollUp)
                {
                    SetMouseWheel(true, mod);
                }
                else if (delta == MouseEventType.WheelScrollDown)
                {
                    SetMouseWheel(false, mod);
                }
            }

            public void SetMouseWheel(bool wheelUp, SDL.SDL_Keymod mod)
            {
                string newvalue = KeysTranslator.GetMouseWheel(wheelUp, mod);

                if (!string.IsNullOrEmpty(newvalue))
                {
                    ResetBinding();

                    WheelScroll = true;
                    WheelUp = wheelUp;
                    Mod = mod;
                    _label.Text = newvalue;
                }
            }

            private void ResetBinding()
            {
                Key = 0;
                MouseButton = MouseButtonType.None;
                WheelScroll = false;
                Mod = 0;
                _label.Text = "None";
                Buttons = null;
            }

            private void LabelOnMouseUp(object sender, MouseEventArgs e)
            {
                IsActive = true;
                SetKeyboardFocus();
            }

            public override void OnButtonClick(int buttonID)
            {
                switch ((ButtonState)buttonID)
                {
                    case ButtonState.Ok:
                        HotkeyChanged.Raise(this);

                        break;

                    case ButtonState.Cancel:
                        _label.Text = "None";

                        HotkeyCancelled.Raise(this);

                        Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                        Mod = SDL.SDL_Keymod.KMOD_NONE;

                        break;
                }

                IsActive = false;
            }

            private enum ButtonState
            {
                Ok,
                Cancel
            }
        }

        private class NameOverheadAssignControl : Control
        {
            private readonly HotkeyBox _hotkeyBox;
            private readonly Dictionary<NameOverheadOptions, CheckboxWithLabel> checkboxDict = new();

            private enum ButtonType
            {
                CheckAll,
                UncheckAll,
            }

            public NameOverheadAssignControl(NameOverheadOption option)
            {
                Option = option;

                CanMove = true;

                Control c;
                c = AddLabel("Set hotkey:");

                _hotkeyBox = new HotkeyBox
                {
                    X = c.Bounds.Right + 5
                };

                _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
                _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;

                Add(_hotkeyBox);

                Add(c = new ModernButton(0, _hotkeyBox.Height + 3, 100, 40, ButtonAction.Activate, "Uncheck all", Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)ButtonType.UncheckAll, IsSelectable = false });

                Add(new ModernButton(c.Bounds.Right + 5, _hotkeyBox.Height + 3, 100, 40, ButtonAction.Activate, "Check all", Theme.BUTTON_FONT_COLOR) { ButtonParameter = (int)ButtonType.CheckAll, IsSelectable = false });

                SetupOptionCheckboxes();

                UpdateCheckboxesByCurrentOptionFlags();
                UpdateValueInHotkeyBox();
            }

            private void SetupOptionCheckboxes()
            {
                int rightPosX = 200;
                Control c;
                PositionHelper.Reset();

                PositionHelper.Y = 100;

                c = AddLabel("Items");
                PositionHelper.PositionControl(c);

                c = AddCheckbox("Containers", NameOverheadOptions.Containers);
                PositionHelper.PositionControl(c);

                c = AddCheckbox("Gold", NameOverheadOptions.Gold);
                PositionHelper.PositionExact(c, rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Stackable", NameOverheadOptions.Stackable));
                PositionHelper.PositionExact(AddCheckbox("Locked down", NameOverheadOptions.LockedDown), rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Other items", NameOverheadOptions.Other));



                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Corpses"));

                PositionHelper.PositionControl(AddCheckbox("Monster corpses", NameOverheadOptions.MonsterCorpses));
                PositionHelper.PositionExact(AddCheckbox("Humanoid corpses", NameOverheadOptions.HumanoidCorpses), rightPosX, PositionHelper.LAST_Y);
                //AddCheckbox("Own corpses", NameOverheadOptions.OwnCorpses, 0, y);



                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Mobiles by type"));

                PositionHelper.PositionControl(AddCheckbox("Humanoid", NameOverheadOptions.Humanoid));
                PositionHelper.PositionExact(AddCheckbox("Monster", NameOverheadOptions.Monster), rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Your Followers", NameOverheadOptions.OwnFollowers));
                PositionHelper.PositionExact(AddCheckbox("Yourself", NameOverheadOptions.Self), rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Exclude yourself", NameOverheadOptions.ExcludeSelf));



                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Mobiles by notoriety"));

                CheckboxWithLabel cb;
                PositionHelper.PositionControl(cb = AddCheckbox("Innocent", NameOverheadOptions.Innocent));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.InnocentHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Allied", NameOverheadOptions.Ally), rightPosX, PositionHelper.LAST_Y);
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.FriendHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Attackable", NameOverheadOptions.Gray));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.CanAttackHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Criminal", NameOverheadOptions.Criminal), rightPosX, PositionHelper.LAST_Y);
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.CriminalHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Enemy", NameOverheadOptions.Enemy));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.EnemyHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Murderer", NameOverheadOptions.Murderer), rightPosX, PositionHelper.LAST_Y);
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.MurdererHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Invulnerable", NameOverheadOptions.Invulnerable));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.InvulnerableHue;
            }

            private TextBox AddLabel(string name)
            {
                var label = new TextBox(name, Theme.FONT, Theme.STANDARD_TEXT_SIZE, null, Theme.TEXT_FONT_COLOR, strokeEffect: false);

                Add(label);

                return label;
            }

            private CheckboxWithLabel AddCheckbox(string checkboxName, NameOverheadOptions optionFlag)
            {
                var checkbox = new CheckboxWithLabel(checkboxName, 0, true, (b) =>
                {
                    if (b)
                        Option.NameOverheadOptionFlags |= (int)optionFlag;
                    else
                        Option.NameOverheadOptionFlags &= ~(int)optionFlag;

                    if (NameOverHeadManager.LastActiveNameOverheadOption == Option.Name)
                        NameOverHeadManager.ActiveOverheadOptions = (NameOverheadOptions)Option.NameOverheadOptionFlags;
                });

                checkboxDict.Add(optionFlag, checkbox);

                Add(checkbox);

                return checkbox;
            }

            public NameOverheadOption Option { get; }

            private void UpdateValueInHotkeyBox()
            {
                if (Option == null || _hotkeyBox == null)
                {
                    return;
                }

                if (Option.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE;

                    if (Option.Alt)
                    {
                        mod |= SDL.SDL_Keymod.KMOD_ALT;
                    }

                    if (Option.Shift)
                    {
                        mod |= SDL.SDL_Keymod.KMOD_SHIFT;
                    }

                    if (Option.Ctrl)
                    {
                        mod |= SDL.SDL_Keymod.KMOD_CTRL;
                    }

                    _hotkeyBox.SetKey(Option.Key, mod);
                }
            }

            private void BoxOnHotkeyChanged(object sender, EventArgs e)
            {
                bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
                bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
                bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

                if (_hotkeyBox.Key == SDL.SDL_Keycode.SDLK_UNKNOWN)
                    return;

                NameOverheadOption option = NameOverHeadManager.FindOptionByHotkey(_hotkeyBox.Key, alt, ctrl, shift);

                if (option == null)
                {
                    Option.Key = _hotkeyBox.Key;
                    Option.Shift = shift;
                    Option.Alt = alt;
                    Option.Ctrl = ctrl;

                    return;
                }

                if (Option == option)
                    return;

                UpdateValueInHotkeyBox();
                UIManager.Add(new MessageBoxGump(250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, option.Name), null));
            }

            private void BoxOnHotkeyCancelled(object sender, EventArgs e)
            {
                Option.Alt = Option.Ctrl = Option.Shift = false;
                Option.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
            }

            public override void OnButtonClick(int buttonID)
            {
                switch ((ButtonType)buttonID)
                {
                    case ButtonType.CheckAll:
                        Option.NameOverheadOptionFlags = int.MaxValue;
                        UpdateCheckboxesByCurrentOptionFlags();

                        break;

                    case ButtonType.UncheckAll:
                        Option.NameOverheadOptionFlags = 0x0;
                        UpdateCheckboxesByCurrentOptionFlags();

                        break;
                }
            }

            private void UpdateCheckboxesByCurrentOptionFlags()
            {
                foreach (var kvp in checkboxDict)
                {
                    var flag = kvp.Key;
                    var checkbox = kvp.Value;

                    checkbox.IsChecked = ((NameOverheadOptions)Option.NameOverheadOptionFlags).HasFlag(flag);
                }
            }
        }
        #endregion

        private class ProfileLocationData
        {
            public readonly DirectoryInfo Server;
            public readonly DirectoryInfo Username;
            public readonly DirectoryInfo Character;

            public ProfileLocationData(string server, string username, string character)
            {
                this.Server = new DirectoryInfo(server);
                this.Username = new DirectoryInfo(username);
                this.Character = new DirectoryInfo(character);
            }

            public override string ToString()
            {
                return Character.ToString();
            }
        }

        private class SettingsOption
        {
            public SettingsOption(string optionLabel, Control control, int maxTotalWidth, PAGE optionsPage, int x = 0, int y = 0)
            {
                OptionLabel = optionLabel;
                OptionControl = control;
                OptionsPage = optionsPage;
                FullControl = new Area(false) { AcceptMouseInput = true, CanMove = true, CanCloseWithRightClick = true };

                if (!string.IsNullOrEmpty(OptionLabel))
                {
                    Control labelTextBox = new TextBox(OptionLabel, Theme.FONT, 20, null, Theme.TEXT_FONT_COLOR, strokeEffect: false) { AcceptMouseInput = false };
                    FullControl.Add(labelTextBox, (int)optionsPage);

                    if (labelTextBox.Width > maxTotalWidth)
                    {
                        labelTextBox.Width = maxTotalWidth;
                    }

                    if (OptionControl != null)
                    {
                        if (labelTextBox.Width + OptionControl.Width + 5 > maxTotalWidth)
                        {
                            OptionControl.Y = labelTextBox.Height + 5;
                            OptionControl.X = 15;
                        }
                        else
                        {
                            OptionControl.X = labelTextBox.Width + 5;
                        }
                    }

                    FullControl.Width += labelTextBox.Width + 5;
                    FullControl.Height = labelTextBox.Height;
                }

                if (OptionControl != null)
                {
                    FullControl.Add(OptionControl, (int)optionsPage);
                    FullControl.Width += OptionControl.Width;
                    FullControl.ActivePage = (int)optionsPage;

                    if (OptionControl.Height > FullControl.Height)
                    {
                        FullControl.Height = OptionControl.Height;
                    }
                }

                FullControl.X = x;
                FullControl.Y = y;
            }

            public string OptionLabel { get; }
            public Control OptionControl { get; }
            public PAGE OptionsPage { get; }
            public Area FullControl { get; }
        }

        private class ThemeSettings : UISettings
        {
            public int SLIDER_WIDTH { get; set; } = 150;
            public int COMBO_BOX_WIDTH { get; set; } = 225;
            public int SCROLL_BAR_WIDTH { get; set; } = 15;
            public int INPUT_WIDTH { get; set; } = 200;
            public int TOP_PADDING { get; set; } = 5;
            public int INDENT_SPACE { get; set; } = 40;
            public int BLANK_LINE { get; set; } = 20;
            public int HORIZONTAL_SPACING_CONTROLS { get; set; } = 20;

            public int STANDARD_TEXT_SIZE { get; set; } = 20;

            public float NO_MATCH_SEARCH { get; set; } = 0.5f;

            public ushort BACKGROUND { get; set; } = 897;
            public ushort SEARCH_BACKGROUND { get; set; } = 899;
            public ushort CHECKBOX { get; set; } = 899;
            public int CHECKBOX_SIZE { get; set; } = 30;
            public ushort BLACK { get; set; } = 0;

            [JsonConverter(typeof(ColorJsonConverter))]
            public Color DROPDOWN_OPTION_NORMAL_HUE { get; set; } = Color.White;
            [JsonConverter(typeof(ColorJsonConverter))]

            public Color DROPDOWN_OPTION_HOVER_HUE { get; set; } = Color.AntiqueWhite;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color DROPDOWN_OPTION_SELECTED_HUE { get; set; } = Color.CadetBlue;

            [JsonConverter(typeof(ColorJsonConverter))]
            public Color BUTTON_FONT_COLOR { get; set; } = Color.White;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color TEXT_FONT_COLOR { get; set; } = Color.White;

            public string FONT { get; set; } = TrueTypeLoader.EMBEDDED_FONT;
        }

        private static class PositionHelper
        {
            public static int X, Y = Theme.TOP_PADDING, LAST_Y = Theme.TOP_PADDING;

            public static void BlankLine()
            {
                LAST_Y = Y;
                Y += Theme.BLANK_LINE;
            }

            public static void Indent()
            {
                X += Theme.INDENT_SPACE;
            }

            public static void RemoveIndent()
            {
                X -= Theme.INDENT_SPACE;
            }

            public static void PositionControl(Control c)
            {
                c.X = X;
                c.Y = Y;

                LAST_Y = Y;
                Y += c.Height + Theme.TOP_PADDING;
            }

            public static void PositionExact(Control c, int x, int y)
            {
                c.X = x;
                c.Y = y;
            }

            public static void Reset()
            {
                X = 0;
                Y = Theme.TOP_PADDING;
                LAST_Y = Y;
            }
        }

        private enum PAGE
        {
            None,
            General,
            Sound,
            Video,
            Macros,
            Tooltip,
            Speech,
            CombatSpells,
            Counters,
            InfoBar,
            Containers,
            Experimental,
            IgnoreList,
            NameplateOptions,
            TUOCooldowns,
            TUOOptions
        }

        private interface SearchableOption
        {
            public bool Search(string text);

            public void OnSearchMatch();
        }
    }
}
