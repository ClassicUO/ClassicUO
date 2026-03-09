namespace ClassicUO.TazUO
{
    public class TazUOLanguage
    {
        public string TazuoVersionHistory { get; set; } = "Dust765 3.0 Version History";
        public string CurrentVersion { get; set; } = "Current Version: ";
        public string TazUOWiki { get; set; } = "TazUO Wiki";
        public string TazUODiscord { get; set; } = "TazUO Discord";
        public string ButtonTazUO { get; set; } = "TazUO Specific";

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
        public string JournalMessagesOnlyInJournalBox { get; set; } = "Journal messages only in journal box (clean game view)";
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
        public string NearbyItemGump { get; set; } = "Enable nearby item gump";
        public string ShowUseLootModalOnCtrl { get; set; } = "Show Use/Loot modal when pressing Ctrl (nearby items)";
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
