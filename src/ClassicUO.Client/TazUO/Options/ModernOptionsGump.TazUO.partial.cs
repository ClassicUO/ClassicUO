using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility;
using ClassicUO.TazUO.Managers;
using ClassicUO.TazUO.Options;
using ClassicUO.TazUO.UI.Gumps;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    internal partial class ModernOptionsGump
    {
                private void BuildTazUO()
        {
            LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(mainContent.RightWidth, mainContent.Height, (int)(mainContent.RightWidth * 0.3), 0, false);
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
            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.JournalMessagesOnlyInJournalBox, 0, profile.JournalMessagesOnlyInJournalBox, (b) =>
            {
                profile.JournalMessagesOnlyInJournalBox = b;
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
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AutoAvoidObstacules, isChecked: profile.AutoAvoidObstacules, valueChanged: (b) => { profile.AutoAvoidObstacules = b; }), true, page);

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
                profile.SkillBarFormat = ((ClassicUO.Game.UI.Controls.StbTextBox)s).Text;
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
            content.AddToRight(new InputFieldWithLabel(lang.GetTazUO.SOSGumpID, Theme.INPUT_WIDTH, profile.SOSGumpID.ToString(), true, (s, e) => { if (uint.TryParse(((ClassicUO.Game.UI.Controls.StbTextBox)s).Text, out uint id)) { profile.SOSGumpID = id; } }), true, page);
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
            content.AddToRight(new ModernColorPickerWithLabel(lang.GetToolTips.ToolTipFont, profile.TooltipTextHue, (h) =>
            {
                profile.TooltipTextHue = h;
            }), true, page);
            content.BlankLine();
            content.AddToRight(new InputFieldWithLabel(lang.GetTazUO.HeaderFormatItemName, 140, profile.TooltipHeaderFormat, false, (s, e) =>
            {
                profile.TooltipHeaderFormat = ((ClassicUO.Game.UI.Controls.StbTextBox)s).Text;
            }), true, page);
            content.BlankLine();
            content.AddToRight(c = new ModernButton(0, 0, 150, 40, ButtonAction.Activate, lang.GetTazUO.TooltipOverrideSettings, Theme.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page);
            c.MouseUp += (s, e) => { UIManager.GetGump<ToolTipOverideMenu>()?.Dispose(); UIManager.Add(new ToolTipOverideMenu()); };

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

            List<ProfileTransferHelper.ProfileLocationData> locations = new List<ProfileTransferHelper.ProfileLocationData>();
            List<ProfileTransferHelper.ProfileLocationData> sameServerLocations = new List<ProfileTransferHelper.ProfileLocationData>();
            string[] allAccounts = Directory.GetDirectories(rootpath);

            foreach (string account in allAccounts)
            {
                string[] allServers = Directory.GetDirectories(account);
                foreach (string server in allServers)
                {
                    string[] allCharacters = Directory.GetDirectories(server);
                    foreach (string character in allCharacters)
                    {
                        locations.Add(new ProfileTransferHelper.ProfileLocationData(server, account, character));
                        if (FileSystemHelper.RemoveInvalidChars(profile.ServerName) == FileSystemHelper.RemoveInvalidChars(Path.GetFileName(server)))
                        {
                            sameServerLocations.Add(new ProfileTransferHelper.ProfileLocationData(server, account, character));
                        }
                    }
                }
            }

            content.AddToRight(new UOLabel(string.Format(lang.GetTazUO.SettingsWarning, locations.Count), Theme.FONT, 0x0481, Assets.TEXT_ALIGN_TYPE.TS_CENTER, content.RightWidth - 20, FontStyle.None), true, page);

            content.AddToRight(c = new ModernButton(0, 0, content.RightWidth - 20, 40, ButtonAction.Activate, string.Format(lang.GetTazUO.OverrideAll, locations.Count - 1), Theme.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page);
            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    ProfileTransferHelper.OverrideAllProfiles(locations);
                    GameActions.Print(string.Format(lang.GetTazUO.OverrideSuccess, locations.Count - 1), 32, Data.MessageType.System);
                }
            };

            content.AddToRight(c = new ModernButton(0, 0, content.RightWidth - 20, 40, ButtonAction.Activate, string.Format(lang.GetTazUO.OverrideSame, sameServerLocations.Count - 1), Theme.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page);
            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    ProfileTransferHelper.OverrideAllProfiles(sameServerLocations);
                    GameActions.Print(string.Format(lang.GetTazUO.OverrideSuccess, sameServerLocations.Count - 1), 32, Data.MessageType.System);
                }
            };
            #endregion

            #region Gump scaling
            page = ((int)PAGE.TUOOptions + 1010);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.GumpScaling, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new UOLabel(lang.GetTazUO.ScalingInfo, 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_CENTER, content.RightWidth - 20), true, page);

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
            content.AddToRight(new UOLabel(lang.GetTazUO.VisLayersInfo, 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_CENTER, content.RightWidth - 20), true, page);
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
    }
}