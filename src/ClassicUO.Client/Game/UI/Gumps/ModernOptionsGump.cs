using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using SDL2;
using System.Linq;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernOptionsGump : Gump
    {
        private LeftSideMenuRightSideContent mainContent;
        private List<SettingsOption> options = new List<SettingsOption>();

        public static string SearchText { get; private set; } = String.Empty;
        public static event EventHandler SearchValueChanged;

        public ModernOptionsGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            Width = 900;
            Height = 700;

            Add(new ColorBox(Width, Height, Theme.BACKGROUND) { AcceptMouseInput = true, CanMove = true, Alpha = 0.85f });

            Add(new ColorBox(Width, 40, Theme.SEARCH_BACKGROUND) { AcceptMouseInput = true, CanMove = true, Alpha = 0.85f });

            Add(new TextBox("Options", TrueTypeLoader.EMBEDDED_FONT, 30, null, Color.White, strokeEffect: false) { X = 10, Y = 10 });

            Control c;
            Add(c = new TextBox("Search", TrueTypeLoader.EMBEDDED_FONT, 30, null, Color.White, strokeEffect: false) { X = (int)(Width * 0.3), Y = 10 });

            InputField search;
            Add(search = new InputField(400, 30) { X = c.X + c.Width + 5, Y = 5 });
            search.TextChanged += (s, e) => { SearchText = search.Text; SearchValueChanged.Raise(); };

            Add(mainContent = new LeftSideMenuRightSideContent(Width, Height - 40, (int)(Width * 0.23)) { Y = 40 });

            ModernButton b;
            mainContent.AddToLeft(b = CategoryButton("General", (int)PAGE.General, mainContent.LeftWidth));
            b.IsSelected = true;
            mainContent.AddToLeft(CategoryButton("Sound", (int)PAGE.Sound, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Video", (int)PAGE.Video, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Macros", (int)PAGE.Macros, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Tooltips", (int)PAGE.Tooltip, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Speech", (int)PAGE.Speech, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Combat & Spells", (int)PAGE.CombatSpells, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Counters", (int)PAGE.Counters, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Infobar", (int)PAGE.InfoBar, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Containers", (int)PAGE.Containers, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Experimental", (int)PAGE.Experimental, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Ignore List", (int)PAGE.IgnoreList, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Nameplate Options", (int)PAGE.NameplateOptions, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Cooldown bars", (int)PAGE.TUOCooldowns, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("TazUO Specific", (int)PAGE.TUOOptions, mainContent.LeftWidth));

            BuildGeneral();

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
            content.AddToLeft(SubCategoryButton("General", page, content.LeftWidth));

            content.AddToRight(new CheckboxWithLabel("Highlight objects under cursor", isChecked: ProfileManager.CurrentProfile.HighlightGameObjects, valueChanged: (b) => { ProfileManager.CurrentProfile.HighlightGameObjects = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Enable pathfinding", isChecked: ProfileManager.CurrentProfile.EnablePathfind, valueChanged: (b) => { ProfileManager.CurrentProfile.EnablePathfind = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel("Use shift for pathfinding", isChecked: ProfileManager.CurrentProfile.UseShiftToPathfind, valueChanged: (b) => { ProfileManager.CurrentProfile.UseShiftToPathfind = b; }), true, page);
            content.AddToRight(new CheckboxWithLabel("Single click for pathfinding", isChecked: ProfileManager.CurrentProfile.PathfindSingleClick, valueChanged: (b) => { ProfileManager.CurrentProfile.PathfindSingleClick = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Always run", isChecked: ProfileManager.CurrentProfile.AlwaysRun, valueChanged: (b) => { ProfileManager.CurrentProfile.AlwaysRun = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel("Unless hidden", isChecked: ProfileManager.CurrentProfile.AlwaysRunUnlessHidden, valueChanged: (b) => { ProfileManager.CurrentProfile.AlwaysRunUnlessHidden = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Automatically open doors", isChecked: ProfileManager.CurrentProfile.AutoOpenDoors, valueChanged: (b) => { ProfileManager.CurrentProfile.AutoOpenDoors = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel("Open doors while pathfinding", isChecked: ProfileManager.CurrentProfile.SmoothDoors, valueChanged: (b) => { ProfileManager.CurrentProfile.SmoothDoors = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Automatically open corpses", isChecked: ProfileManager.CurrentProfile.AutoOpenCorpses, valueChanged: (b) => { ProfileManager.CurrentProfile.AutoOpenCorpses = b; }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel("Corpse open distance", 0, Theme.SLIDER_WIDTH, 0, 5, ProfileManager.CurrentProfile.AutoOpenCorpseRange, (r) => { ProfileManager.CurrentProfile.AutoOpenCorpseRange = r; }), true, page);
            content.AddToRight(new CheckboxWithLabel("Skip empty corpses", isChecked: ProfileManager.CurrentProfile.SkipEmptyCorpse, valueChanged: (b) => { ProfileManager.CurrentProfile.SkipEmptyCorpse = b; }), true, page);
            content.AddToRight(new ComboBoxWithLabel("Corpse open options", 0, Theme.COMBO_BOX_WIDTH, new string[] { "None", "Not targeting", "Not hiding", "Both" }, ProfileManager.CurrentProfile.CorpseOpenOptions, (s, n) => { ProfileManager.CurrentProfile.CorpseOpenOptions = s; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("No color for out of range objects", isChecked: ProfileManager.CurrentProfile.NoColorObjectsOutOfRange, valueChanged: (b) => { ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = b; }), true, page);

            content.BlankLine();

            content.AddToRight(c = new CheckboxWithLabel("Enable sallos easy grab", isChecked: ProfileManager.CurrentProfile.SallosEasyGrab, valueChanged: (b) => { ProfileManager.CurrentProfile.SallosEasyGrab = b; }), true, page);
            c.SetTooltip("Sallos easy grab is not recommended with grid containers enabled.");

            if (Client.Version > ClientVersion.CV_70796)
            {
                content.BlankLine();
                content.AddToRight(new CheckboxWithLabel("Show house content", isChecked: ProfileManager.CurrentProfile.ShowHouseContent, valueChanged: (b) => { ProfileManager.CurrentProfile.ShowHouseContent = b; }), true, page);
            }

            if (Client.Version >= ClientVersion.CV_7090)
            {
                content.BlankLine();
                content.AddToRight(new CheckboxWithLabel("Smooth boat movements", isChecked: ProfileManager.CurrentProfile.UseSmoothBoatMovement, valueChanged: (b) => { ProfileManager.CurrentProfile.UseSmoothBoatMovement = b; }), true, page);
            }

            content.BlankLine();
            #endregion

            #region Mobiles
            page = ((int)PAGE.General + 1001);
            content.AddToLeft(SubCategoryButton("Mobiles", page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel("Show mobile's HP", isChecked: ProfileManager.CurrentProfile.ShowMobilesHP, valueChanged: (b) => { ProfileManager.CurrentProfile.ShowMobilesHP = b; }), true, page);
            content.Indent();
            content.AddToRight(new ComboBoxWithLabel("Type", 0, Theme.COMBO_BOX_WIDTH, new string[] { "Percentage", "Bar", "Both" }, ProfileManager.CurrentProfile.MobileHPType, (s, n) => { ProfileManager.CurrentProfile.MobileHPType = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel("Show when", 0, Theme.COMBO_BOX_WIDTH, new string[] { "Always", "Less than 100%", "Smart" }, ProfileManager.CurrentProfile.MobileHPShowWhen, (s, n) => { ProfileManager.CurrentProfile.MobileHPShowWhen = s; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Highlight poisoned mobiles", isChecked: ProfileManager.CurrentProfile.HighlightMobilesByPoisoned, valueChanged: (b) => { ProfileManager.CurrentProfile.HighlightMobilesByPoisoned = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel("Highlight color", ProfileManager.CurrentProfile.PoisonHue, (h) => { ProfileManager.CurrentProfile.PoisonHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Highlight paralyzed mobiles", isChecked: ProfileManager.CurrentProfile.HighlightMobilesByParalize, valueChanged: (b) => { ProfileManager.CurrentProfile.HighlightMobilesByParalize = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel("Highlight color", ProfileManager.CurrentProfile.ParalyzedHue, (h) => { ProfileManager.CurrentProfile.ParalyzedHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Highlight invulnerable mobiles", isChecked: ProfileManager.CurrentProfile.HighlightMobilesByInvul, valueChanged: (b) => { ProfileManager.CurrentProfile.HighlightMobilesByInvul = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel("Highlight color", ProfileManager.CurrentProfile.InvulnerableHue, (h) => { ProfileManager.CurrentProfile.InvulnerableHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Show incoming mobile names", isChecked: ProfileManager.CurrentProfile.ShowNewMobileNameIncoming, valueChanged: (b) => { ProfileManager.CurrentProfile.ShowNewMobileNameIncoming = b; }), true, page);

            content.BlankLine(); 
            
            content.AddToRight(new CheckboxWithLabel("Show incoming corpse names", isChecked: ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming, valueChanged: (b) => { ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel("Show aura under feet", 0, Theme.COMBO_BOX_WIDTH, new string[] { "Disabled", "Warmode", "Ctrl + Shift", "Always" }, ProfileManager.CurrentProfile.AuraUnderFeetType, (s, n) => { ProfileManager.CurrentProfile.AuraUnderFeetType = s; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel("Use a custom color for party members", isChecked: ProfileManager.CurrentProfile.PartyAura, valueChanged: (b) => { ProfileManager.CurrentProfile.PartyAura = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel("Party aura color", ProfileManager.CurrentProfile.PartyAuraHue, (h) => { ProfileManager.CurrentProfile.PartyAuraHue = h; }), true, page);
            content.RemoveIndent();
            content.RemoveIndent();
            #endregion

            #region Gumps & Context
            page = ((int)PAGE.General + 1002);
            content.AddToLeft(SubCategoryButton("Gumps & Context", page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel("Disable top menu bar", isChecked: ProfileManager.CurrentProfile.TopbarGumpIsDisabled, valueChanged: (b) => { ProfileManager.CurrentProfile.TopbarGumpIsDisabled = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Require alt to close anchored gumps", isChecked: ProfileManager.CurrentProfile.HoldDownKeyAltToCloseAnchored, valueChanged: (b) => { ProfileManager.CurrentProfile.HoldDownKeyAltToCloseAnchored = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Require alt to move gumps", isChecked: ProfileManager.CurrentProfile.HoldAltToMoveGumps, valueChanged: (b) => { ProfileManager.CurrentProfile.HoldAltToMoveGumps = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Close entire group of anchored gumps with right click", isChecked: ProfileManager.CurrentProfile.CloseAllAnchoredGumpsInGroupWithRightClick, valueChanged: (b) => { ProfileManager.CurrentProfile.CloseAllAnchoredGumpsInGroupWithRightClick = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Use original skills gump", isChecked: ProfileManager.CurrentProfile.StandardSkillsGump, valueChanged: (b) => { ProfileManager.CurrentProfile.StandardSkillsGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Use old status gump", isChecked: ProfileManager.CurrentProfile.UseOldStatusGump, valueChanged: (b) => { ProfileManager.CurrentProfile.UseOldStatusGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Show party invite gump", isChecked: ProfileManager.CurrentProfile.PartyInviteGump, valueChanged: (b) => { ProfileManager.CurrentProfile.PartyInviteGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Use modern health bar gumps", isChecked: ProfileManager.CurrentProfile.CustomBarsToggled, valueChanged: (b) => { ProfileManager.CurrentProfile.CustomBarsToggled = b; }), true, page);
            content.Indent();
            content.AddToRight(new CheckboxWithLabel("Use black background", isChecked: ProfileManager.CurrentProfile.CBBlackBGToggled, valueChanged: (b) => { ProfileManager.CurrentProfile.CBBlackBGToggled = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Save health bars on logout", isChecked: ProfileManager.CurrentProfile.SaveHealthbars, valueChanged: (b) => { ProfileManager.CurrentProfile.SaveHealthbars = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel("Close health bars when", 0, Theme.COMBO_BOX_WIDTH, new string[] { "Disabled", "Out of range", "Dead" }, ProfileManager.CurrentProfile.CloseHealthBarType, (s, n) => { ProfileManager.CurrentProfile.CloseHealthBarType = s; }), true, page);

            content.BlankLine();

            content.AddToRight(c = new ComboBoxWithLabel("Grid Loot", 0, Theme.COMBO_BOX_WIDTH, new string[] { "Disabled", "Grid loot only", "Grid loot and normal container" }, ProfileManager.CurrentProfile.GridLootType, (s, n) => { ProfileManager.CurrentProfile.GridLootType = s; }), true, page);
            c.SetTooltip("This is not the same as Grid Containers, this is a simple grid gump used for looting corpses.");

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Require shift to open context menus", isChecked: ProfileManager.CurrentProfile.HoldShiftForContext, valueChanged: (b) => { ProfileManager.CurrentProfile.HoldShiftForContext = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Require shift to split stacks of items", isChecked: ProfileManager.CurrentProfile.HoldShiftToSplitStack, valueChanged: (b) => { ProfileManager.CurrentProfile.HoldShiftToSplitStack = b; }), true, page);
            #endregion

            #region Misc
            page = ((int)PAGE.General + 1003);
            content.AddToLeft(SubCategoryButton("Misc", page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel("Enable circle of transparency", isChecked: ProfileManager.CurrentProfile.UseCircleOfTransparency, valueChanged: (b) => { ProfileManager.CurrentProfile.UseCircleOfTransparency = b; }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel("Distance", 0, Theme.SLIDER_WIDTH, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, ProfileManager.CurrentProfile.CircleOfTransparencyRadius, (r) => { ProfileManager.CurrentProfile.CircleOfTransparencyRadius = r; }), true, page);
            content.AddToRight(new ComboBoxWithLabel("Type", 0, Theme.COMBO_BOX_WIDTH, new string[] { "Full", "Gradient", "Modern" }, ProfileManager.CurrentProfile.CircleOfTransparencyType, (s, n) => { ProfileManager.CurrentProfile.CircleOfTransparencyType = s; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Hide 'screenshot stored in' message", isChecked: ProfileManager.CurrentProfile.HideScreenshotStoredInMessage, valueChanged: (b) => { ProfileManager.CurrentProfile.HideScreenshotStoredInMessage = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Enable object fading", isChecked: ProfileManager.CurrentProfile.UseObjectsFading, valueChanged: (b) => { ProfileManager.CurrentProfile.UseObjectsFading = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Enable text fading", isChecked: ProfileManager.CurrentProfile.TextFading, valueChanged: (b) => { ProfileManager.CurrentProfile.TextFading = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Show target range indicator", isChecked: ProfileManager.CurrentProfile.ShowTargetRangeIndicator, valueChanged: (b) => { ProfileManager.CurrentProfile.ShowTargetRangeIndicator = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Enable drag select for health bars", isChecked: ProfileManager.CurrentProfile.EnableDragSelect, valueChanged: (b) => { ProfileManager.CurrentProfile.EnableDragSelect = b; }), true, page);
            content.Indent();
            content.AddToRight(new ComboBoxWithLabel("Key modifier", 0, Theme.COMBO_BOX_WIDTH, new string[] { "None", "Ctrl", "Shift" }, ProfileManager.CurrentProfile.DragSelectModifierKey, (s, n) => { ProfileManager.CurrentProfile.DragSelectModifierKey = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel("Players only", 0, Theme.COMBO_BOX_WIDTH, new string[] { "None", "Ctrl", "Shift" }, ProfileManager.CurrentProfile.DragSelect_PlayersModifier, (s, n) => { ProfileManager.CurrentProfile.DragSelect_PlayersModifier = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel("Monsters only", 0, Theme.COMBO_BOX_WIDTH, new string[] { "None", "Ctrl", "Shift" }, ProfileManager.CurrentProfile.DragSelect_MonstersModifier, (s, n) => { ProfileManager.CurrentProfile.DragSelect_MonstersModifier = s; }), true, page);
            content.AddToRight(new ComboBoxWithLabel("Visible nameplates only", 0, Theme.COMBO_BOX_WIDTH, new string[] { "None", "Ctrl", "Shift" }, ProfileManager.CurrentProfile.DragSelect_NameplateModifier, (s, n) => { ProfileManager.CurrentProfile.DragSelect_NameplateModifier = s; }), true, page);
            content.AddToRight(new SliderWithLabel("X Position of healthbars", 0, Theme.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width, ProfileManager.CurrentProfile.DragSelectStartX, (r) => { ProfileManager.CurrentProfile.DragSelectStartX = r; }), true, page);
            content.AddToRight(new SliderWithLabel("Y Position of healthbars", 0, Theme.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width, ProfileManager.CurrentProfile.DragSelectStartY, (r) => { ProfileManager.CurrentProfile.DragSelectStartY = r; }), true, page);
            content.AddToRight(new CheckboxWithLabel("Anchor opened health bars together", isChecked: ProfileManager.CurrentProfile.DragSelectAsAnchor, valueChanged: (b) => { ProfileManager.CurrentProfile.DragSelectAsAnchor = b; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Show stats changed messages", isChecked: ProfileManager.CurrentProfile.ShowStatsChangedMessage, valueChanged: (b) => { ProfileManager.CurrentProfile.ShowStatsChangedMessage = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Show skills changed messages", isChecked: ProfileManager.CurrentProfile.ShowSkillsChangedMessage, valueChanged: (b) => { ProfileManager.CurrentProfile.ShowStatsChangedMessage = b; }), true, page);
            content.Indent();
            content.AddToRight(new SliderWithLabel("Changed by", 0, Theme.SLIDER_WIDTH, 0, 100, ProfileManager.CurrentProfile.ShowSkillsChangedDeltaValue, (r) => { ProfileManager.CurrentProfile.ShowSkillsChangedDeltaValue = r; }), true, page);
            content.RemoveIndent();
            #endregion

            #region Terrain and statics
            page = ((int)PAGE.General + 1004);
            content.AddToLeft(SubCategoryButton("Terrain & Statics", page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel("Hide roof tiles", isChecked: !ProfileManager.CurrentProfile.DrawRoofs, valueChanged: (b) => { ProfileManager.CurrentProfile.DrawRoofs = !b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Change trees to strumps", isChecked: ProfileManager.CurrentProfile.TreeToStumps, valueChanged: (b) => { ProfileManager.CurrentProfile.TreeToStumps = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Hide vegetation", isChecked: ProfileManager.CurrentProfile.HideVegetation, valueChanged: (b) => { ProfileManager.CurrentProfile.HideVegetation = b; }), true, page);

            //content.BlankLine();

            //content.AddToRight(new CheckboxWithLabel("Mark cave tiles", isChecked: ProfileManager.CurrentProfile.EnableCaveBorder, valueChanged: (b) => { ProfileManager.CurrentProfile.EnableCaveBorder = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new ComboBoxWithLabel("Field types", 0, Theme.COMBO_BOX_WIDTH, new string[] { "Normal", "Static", "Tile" }, ProfileManager.CurrentProfile.FieldsType, (s, n) => { ProfileManager.CurrentProfile.FieldsType = s; }), true, page);

            #endregion

            options.Add(new SettingsOption(
                    "",
                    content,
                    mainContent.RightWidth,
                    PAGE.General
                ));
        }

        private ModernButton CategoryButton(string text, int page, int width, int height = 40)
        {
            return new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page, FullPageSwitch = true };
        }

        private ModernButton SubCategoryButton(string text, int page, int width, int height = 40)
        {
            return new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, Theme.BUTTON_FONT_COLOR) { ButtonParameter = page };
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
            private const int CHECKBOX_SIZE = 30;

            private bool _isChecked;
            private readonly TextBox _text;

            private Vector3 hueVector = ShaderHueTranslator.GetHueVector(Theme.SEARCH_BACKGROUND, false, 0.9f);

            public CheckboxWithLabel(
                string text = "",
                int maxWidth = 0,
                bool isChecked = false,
                Action<bool> valueChanged = null
            )
            {
                _isChecked = isChecked;
                ValueChanged = valueChanged;
                _text = new TextBox(text, Theme.FONT, Theme.STANDARD_TEXT_SIZE, maxWidth == 0 ? null : maxWidth, Theme.TEXT_FONT_COLOR, strokeEffect: false) { X = CHECKBOX_SIZE + 5 };

                Width = CHECKBOX_SIZE + 5 + _text.Width;
                Height = Math.Max(CHECKBOX_SIZE, _text.MeasuredSize.Y);

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
                    new Rectangle(x, y, CHECKBOX_SIZE, CHECKBOX_SIZE),
                    hueVector
                );

                if (IsChecked)
                {
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(Color.Black),
                        new Rectangle(x + (CHECKBOX_SIZE / 2) / 2, y + (CHECKBOX_SIZE / 2) / 2, CHECKBOX_SIZE / 2, CHECKBOX_SIZE / 2),
                        hueVector
                    );
                }

                _text.Draw(batcher, x + _text.X, y + _text.Y);

                return base.Draw(batcher, x, y);
            }

            protected virtual void OnCheckedChanged()
            {
                ValueChanged.Invoke(IsChecked);
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

                protected override void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
                {
                    base.OnKeyUp(key, mod);
                    switch(key)
                    {
                        case SDL.SDL_Keycode.SDLK_LEFT:
                            Value--;
                            break;
                        case SDL.SDL_Keycode.SDLK_RIGHT:
                            Value++;
                            break;
                    }
                }

                //protected override void OnMouseWheel(MouseEventType delta)
                //{
                //    switch (delta)
                //    {
                //        case MouseEventType.WheelScrollUp:
                //            Value++;

                //            break;

                //        case MouseEventType.WheelScrollDown:
                //            Value--;

                //            break;
                //    }

                //    CalculateOffset();
                //}

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

                    string initialText = selected > -1 ? items[selected] : string.Empty;

                    Add(new ColorBox(Width, Height, Theme.SEARCH_BACKGROUND));

                    Add
                    (
                        _label = new TextBox(initialText, Theme.FONT, Theme.STANDARD_TEXT_SIZE, width, Theme.TEXT_FONT_COLOR, strokeEffect: false)
                        {
                            X = 2,
                            Y = 5
                        }
                    );
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

        private class InputField : Control
        {
            private readonly StbTextBox _textbox;

            public event EventHandler TextChanged { add { _textbox.TextChanged += value; } remove { _textbox.TextChanged -= value; } }

            public InputField
            (
                int width,
                int height,
                int maxWidthText = 0,
                int maxCharsCount = -1
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
                    Y = 4,
                    Width = width - 8,
                    Height = height - 8
                };

                Add(new AlphaBlendControl() { Width = Width, Height = Height });
                Add(_textbox);
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


            private class StbTextBox : Control, ITextEditHandler
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

                    _rendererText = new TextBox(string.Empty, Theme.FONT, FONT_SIZE, maxWidth > 0 ? maxWidth : null, Theme.TEXT_FONT_COLOR, strokeEffect: false);


                    _rendererCaret = new TextBox("_", Theme.FONT, FONT_SIZE, null, Theme.TEXT_FONT_COLOR, strokeEffect: false);

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
                    if (index >= _rendererText.Text.Length - 1)
                    {
                        return _rendererText.GetStringWidth(_rendererText.Text.Substring(index, 1));
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
                    //Fix this based off of Stb.CaretIndex
                    _caretScreenPosition = new Point(_rendererText.X, _rendererText.Y);
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

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    if (batcher.ClipBegin(x, y, Width, Height))
                    {
                        base.Draw(batcher, x, y);
                        //DrawSelection(batcher, x, y);
                        _rendererText.Draw(batcher, x, y);
                        //DrawCaret(batcher, x, y);

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

                        Stb.Click(Mouse.Position.X, Mouse.Position.Y);
                        UpdateCaretScreenPosition();
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

                    Stb.Drag(Mouse.Position.X, Mouse.Position.Y);
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
        #endregion

        private class LeftSideMenuRightSideContent : Control
        {
            private const int TOP_PADDING = 5;
            private const int INDENT_SPACE = 40;
            private const int BLANK_LINE = 20;

            private ScrollArea left, right;
            private int leftY, rightY = TOP_PADDING, leftX, rightX;

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
                    rightY += c.Height + TOP_PADDING;
                }

                right.Add(c, page);
            }

            public void BlankLine()
            {
                rightY += BLANK_LINE;
            }

            public void Indent()
            {
                rightX += INDENT_SPACE;
            }

            public void RemoveIndent()
            {
                rightX -= INDENT_SPACE;
                if (rightX < 0)
                {
                    rightX = 0;
                }
            }

            public void ResetRightSide()
            {
                rightY = TOP_PADDING;
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

        private class SettingsOption
        {
            public SettingsOption(string optionLabel, Control control, int maxTotalWidth, PAGE optionsPage)
            {
                OptionLabel = optionLabel;
                OptionControl = control;
                OptionsPage = optionsPage;
                FullControl = new Area(false) { AcceptMouseInput = true, CanMove = true, CanCloseWithRightClick = true };

                if (!string.IsNullOrEmpty(optionLabel))
                {
                    Control labelTextBox;
                    FullControl.Add(labelTextBox = new TextBox(optionLabel, Theme.FONT, 20, null, Theme.TEXT_FONT_COLOR, strokeEffect: false));

                    if (labelTextBox.Width > maxTotalWidth)
                    {
                        labelTextBox.Width = maxTotalWidth;
                    }

                    if (labelTextBox.Width + control.Width + 5 > maxTotalWidth)
                    {
                        control.Y = labelTextBox.Height + 5;
                        control.X = 15;
                    }
                    else
                    {
                        control.X = labelTextBox.Width + 5;
                    }
                }

                FullControl.Add(OptionControl);
            }

            public string OptionLabel { get; }
            public Control OptionControl { get; }
            public PAGE OptionsPage { get; }
            public Area FullControl { get; }

            public bool CanBeDisplayed()
            {
                return true;
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
            private readonly ScrollBar _scrollBar;

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

            public Rectangle ScissorRectangle;

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
                _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);

                if (batcher.ClipBegin(x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - _scrollBar.Width + ScissorRectangle.Width, Height + ScissorRectangle.Height))
                {
                    for (int i = 1; i < Children.Count; i++)
                    {
                        Control child = Children[i];

                        if (!child.IsVisible || (child.Page != ActivePage && child.Page != 0))
                        {
                            continue;
                        }

                        int finalY = y + child.Y - _scrollBar.Value + ScissorRectangle.Y;

                        child.Draw(batcher, x + child.X, finalY);
                    }

                    batcher.ClipEnd();
                }

                return true;
            }

            protected override void OnMouseWheel(MouseEventType delta)
            {
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
                height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

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
                    Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
                }
            }

            private class ScrollBar : ScrollBarBase
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
                    if (Height <= 0 || !IsVisible)
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

                    return base.Draw(batcher, x, y);
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



        private static class Theme
        {
            public const int SLIDER_WIDTH = 150;
            public const int COMBO_BOX_WIDTH = 225;
            public const int SCROLL_BAR_WIDTH = 15;

            public const int STANDARD_TEXT_SIZE = 20;

            public const float NO_MATCH_SEARCH = 0.5f;

            public const ushort BACKGROUND = 897;
            public const ushort SEARCH_BACKGROUND = 899;
            public const ushort BLACK = 0;

            public static Color DROPDOWN_OPTION_NORMAL_HUE = Color.White;
            public static Color DROPDOWN_OPTION_HOVER_HUE = Color.AntiqueWhite;
            public static Color DROPDOWN_OPTION_SELECTED_HUE = Color.CadetBlue;

            public static Color BUTTON_FONT_COLOR = Color.White;
            public static Color TEXT_FONT_COLOR = Color.White;

            public static string FONT = TrueTypeLoader.EMBEDDED_FONT;
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
