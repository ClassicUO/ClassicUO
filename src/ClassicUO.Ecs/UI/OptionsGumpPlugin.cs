// Modern flex-layout Options window (opened from the top bar's gear/Debug
// button). Not a UO gump — a dark-theme panel built entirely from Bevy.UI
// flex nodes so it renders regardless of the loaded dataset.
//
// Settings link the same way the legacy OptionsGump does: each row reads its
// current value from Res<Profile> when built and writes it back on change.
// The catalog (s_options) is data-driven — category, label, search keywords
// and getter/setter per entry — so the search box can filter across every
// category at once.
//
// All row controls are FLOW children (toggle pill, +/- stepper, cycle
// button): absolute-positioned children escape Clay's scroll clip, so no
// UO-art sliders inside the scrolling list.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct OptionsGumpPlugin : IPlugin
{
    // The UI lays out at the logical surface size (640x480), upscaled to the
    // window. Keep the window inside that or it overflows off-screen.
    private const int WinW = 624;
    private const int Pad = 14;
    private const int HeaderH = 50;
    private const int SidebarW = 120;
    private const int ScrollW = 14;
    private const int RowH = 38;
    private const int RowGap = 7;
    private const int BottomPad = 6;
    private const int SideBtnH = 30;

    // The window height is derived so the scroll viewport holds a WHOLE number
    // of rows — a viewport that ends mid-row leaves a clipped sliver + the
    // trailing inter-row gap, which reads as dead space at the bottom. The
    // section title is fixed-height so the row math is exact. Row count adapts
    // to the screen between Min/Max.
    private const int RowPitch = RowH + RowGap;   // per-row vertical advance
    private const int SectionH = 26;              // fixed section-title height
    private const int MinRows = 6;
    private const int MaxRows = 12;
    // Vertical chrome around the scroll area: top pad + header + header/body gap + bottom pad.
    private const int ChromeV = Pad + (HeaderH - Pad) + 12 + BottomPad;
    // Inner width available to a row: window - panel padding - sidebar - gaps - scrollbar.
    private const int RowW = WinW - Pad * 2 - SidebarW - 10 - ScrollW - 6;
    private const int LabelW = 250;

    // Modern dark palette. Layered slate (panel < card/row < control) for depth,
    // a blue accent, and a green/grey switch track.
    private static readonly ClayColor s_panelBg     = new(22, 24, 30, 252);
    private static readonly ClayColor s_panelBorder = new(58, 63, 78, 200);
    private static readonly ClayColor s_shadow      = new(0, 0, 0, 90);
    private static readonly ClayColor s_headerBg    = new(30, 33, 41, 255);
    private static readonly ClayColor s_rowBg       = new(38, 41, 51, 255);
    private static readonly ClayColor s_rowHover    = new(48, 52, 64, 255);
    private static readonly ClayColor s_controlBg   = new(54, 58, 71, 255);
    private static readonly ClayColor s_controlHover = new(68, 73, 89, 255);
    private static readonly ClayColor s_accent      = new(92, 132, 224, 255);
    private static readonly ClayColor s_accentHover = new(110, 148, 236, 255);
    private static readonly ClayColor s_sideBg      = new(33, 36, 45, 255);
    private static readonly ClayColor s_sideHover   = new(46, 50, 62, 255);
    private static readonly ClayColor s_toggleOn    = new(74, 178, 112, 255);
    private static readonly ClayColor s_toggleOff   = new(66, 70, 84, 255);
    private static readonly ClayColor s_knob        = new(238, 240, 246, 255);
    private static readonly ClayColor s_field       = new(216, 218, 224, 255);
    private static readonly ClayColor s_textMain    = new(236, 238, 244, 255);
    private static readonly ClayColor s_textDim     = new(150, 154, 168, 255);
    private static readonly ClayColor s_textFaint   = new(108, 112, 126, 255);

    private enum OptionKind { Toggle, Stepper, Cycle, Hue, Action }

    private sealed class OptionDef
    {
        public string Category;
        public string Label;
        public string Keywords = string.Empty;
        public OptionKind Kind;
        public Func<Profile, bool> GetB;
        public Action<Profile, bool> SetB;
        public Func<Profile, int> GetI;
        public Action<Profile, int> SetI;
        public int Min, Max, Step = 1;
        public string[] Choices;
        public Action<Commands, OptionsButtonParams> Run;

        public bool Matches(string needle) =>
            Label.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || Category.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || Keywords.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    // Catalog helpers — keep the (large) registry readable.
    private static OptionDef T(string cat, string label, Func<Profile, bool> g, Action<Profile, bool> s, string kw = "")
        => new() { Category = cat, Label = label, Keywords = kw, Kind = OptionKind.Toggle, GetB = g, SetB = s };

    private static OptionDef I(string cat, string label, int min, int max, int step,
        Func<Profile, int> g, Action<Profile, int> s, string kw = "")
        => new() { Category = cat, Label = label, Keywords = kw, Kind = OptionKind.Stepper, Min = min, Max = max, Step = step, GetI = g, SetI = s };

    private static OptionDef C(string cat, string label, string[] choices,
        Func<Profile, int> g, Action<Profile, int> s, string kw = "")
        => new() { Category = cat, Label = label, Keywords = kw, Kind = OptionKind.Cycle, Choices = choices, GetI = g, SetI = s };

    // Hue setting: numbers-only editable value + a swatch that opens the
    // palette overlay.
    private static OptionDef H(string cat, string label, Func<Profile, int> g, Action<Profile, int> s, string kw = "")
        => new() { Category = cat, Label = label, Keywords = kw + " hue color", Kind = OptionKind.Hue,
                   Min = 0, Max = 3000, Step = 1, GetI = g, SetI = s };

    // The settings catalog. Mirrors legacy OptionsGump's profile linkage for
    // every setting the legacy Profile carries (positions / string formats /
    // macro & infobar editors excluded). Read on build, written on change.
    private static readonly OptionDef[] s_options =
    {
        // ===== General =====
        T("General", "Enable pathfinding", p => p.EnablePathfind, (p, v) => p.EnablePathfind = v, "walk auto move"),
        T("General", "Shift to pathfind", p => p.UseShiftToPathfind, (p, v) => p.UseShiftToPathfind = v),
        T("General", "Always run", p => p.AlwaysRun, (p, v) => p.AlwaysRun = v, "movement speed"),
        T("General", "Always run unless hidden", p => p.AlwaysRunUnlessHidden, (p, v) => p.AlwaysRunUnlessHidden = v),
        T("General", "Fast rotation", p => p.FastRotation, (p, v) => p.FastRotation = v, "turn movement"),
        T("General", "Smooth movements", p => p.SmoothMovements, (p, v) => p.SmoothMovements = v, "interpolation step"),
        T("General", "Auto open doors", p => p.AutoOpenDoors, (p, v) => p.AutoOpenDoors = v),
        T("General", "Smooth doors", p => p.SmoothDoors, (p, v) => p.SmoothDoors = v),
        T("General", "Auto open corpses", p => p.AutoOpenCorpses, (p, v) => p.AutoOpenCorpses = v, "loot"),
        I("General", "Corpse open range", 0, 5, 1, p => p.AutoOpenCorpseRange, (p, v) => p.AutoOpenCorpseRange = v, "loot distance"),
        C("General", "Corpse open options", new[] { "Always", "Not targeting", "Not hiding", "Both" },
            p => p.CorpseOpenOptions, (p, v) => p.CorpseOpenOptions = v, "loot"),
        T("General", "Skip empty corpses", p => p.SkipEmptyCorpse, (p, v) => p.SkipEmptyCorpse = v, "loot"),
        T("General", "Sallos easy grab", p => p.SallosEasyGrab, (p, v) => p.SallosEasyGrab = v, "loot pickup"),
        T("General", "Show house content", p => p.ShowHouseContent, (p, v) => p.ShowHouseContent = v, "public"),
        T("General", "Smooth boat movement", p => p.UseSmoothBoatMovement, (p, v) => p.UseSmoothBoatMovement = v, "ship"),
        T("General", "Hold shift for context menu", p => p.HoldShiftForContext, (p, v) => p.HoldShiftForContext = v, "popup"),
        T("General", "Hold shift to split stacks", p => p.HoldShiftToSplitStack, (p, v) => p.HoldShiftToSplitStack = v, "amount"),
        T("General", "Hold tab for combat", p => p.HoldDownKeyTab, (p, v) => p.HoldDownKeyTab = v, "warmode toggle"),
        T("General", "Alt closes anchored gumps", p => p.HoldDownKeyAltToCloseAnchored, (p, v) => p.HoldDownKeyAltToCloseAnchored = v),
        T("General", "Alt to move gumps", p => p.HoldAltToMoveGumps, (p, v) => p.HoldAltToMoveGumps = v, "drag window"),
        T("General", "Right click closes anchored group", p => p.CloseAllAnchoredGumpsInGroupWithRightClick,
            (p, v) => p.CloseAllAnchoredGumpsInGroupWithRightClick = v),
        T("General", "Disable top bar", p => p.TopbarGumpIsDisabled, (p, v) => p.TopbarGumpIsDisabled = v, "menu"),
        T("General", "Hide screenshot stored message", p => p.HideScreenshotStoredInMessage, (p, v) => p.HideScreenshotStoredInMessage = v),
        T("General", "Show stats changed message", p => p.ShowStatsChangedMessage, (p, v) => p.ShowStatsChangedMessage = v, "str dex int"),
        T("General", "Show skills changed message", p => p.ShowSkillsChangedMessage, (p, v) => p.ShowSkillsChangedMessage = v),
        I("General", "Skills changed delta (x0.1)", 0, 100, 1, p => p.ShowSkillsChangedDeltaValue, (p, v) => p.ShowSkillsChangedDeltaValue = v, "message threshold"),
        T("General", "Ignore stamina check", p => p.IgnoreStaminaCheck, (p, v) => p.IgnoreStaminaCheck = v, "push"),

        // ===== Mobiles =====
        T("Mobiles", "Show mobiles HP", p => p.ShowMobilesHP, (p, v) => p.ShowMobilesHP = v, "health overhead"),
        C("Mobiles", "HP display", new[] { "Percentage", "Line", "Both" }, p => p.MobileHPType, (p, v) => p.MobileHPType = v, "health"),
        C("Mobiles", "HP show when", new[] { "Always", "Less than 100%", "Smart" }, p => p.MobileHPShowWhen, (p, v) => p.MobileHPShowWhen = v, "health"),
        T("Mobiles", "Poll mobiles HP status (OSI)", p => p.PollMobileStatus, (p, v) => p.PollMobileStatus = v, "health refresh 500ms normalized"),
        T("Mobiles", "Highlight game objects", p => p.HighlightGameObjects, (p, v) => p.HighlightGameObjects = v, "selection"),
        T("Mobiles", "Highlight poisoned", p => p.HighlightMobilesByPoisoned, (p, v) => p.HighlightMobilesByPoisoned = v),
        H("Mobiles", "Poison hue", p => p.PoisonHue, (p, v) => p.PoisonHue = (ushort)v),
        T("Mobiles", "Highlight paralyzed", p => p.HighlightMobilesByParalize, (p, v) => p.HighlightMobilesByParalize = v),
        H("Mobiles", "Paralyzed hue", p => p.ParalyzedHue, (p, v) => p.ParalyzedHue = (ushort)v),
        T("Mobiles", "Highlight invulnerable", p => p.HighlightMobilesByInvul, (p, v) => p.HighlightMobilesByInvul = v),
        H("Mobiles", "Invulnerable hue", p => p.InvulnerableHue, (p, v) => p.InvulnerableHue = (ushort)v),
        T("Mobiles", "Incoming mobile names", p => p.ShowNewMobileNameIncoming, (p, v) => p.ShowNewMobileNameIncoming = v),
        T("Mobiles", "Incoming corpse names", p => p.ShowNewCorpseNameIncoming, (p, v) => p.ShowNewCorpseNameIncoming = v),
        C("Mobiles", "Aura under feet", new[] { "Off", "Warmode", "Ctrl+Shift", "Always" },
            p => p.AuraUnderFeetType, (p, v) => p.AuraUnderFeetType = v, "circle"),
        T("Mobiles", "Custom party aura", p => p.PartyAura, (p, v) => p.PartyAura = v),
        H("Mobiles", "Party aura hue", p => p.PartyAuraHue, (p, v) => p.PartyAuraHue = (ushort)v),
        T("Mobiles", "Name overheads toggled", p => p.NameOverheadToggled, (p, v) => p.NameOverheadToggled = v, "nameplate"),
        T("Mobiles", "Name overhead gump", p => p.NameOverheadShowGump, (p, v) => p.NameOverheadShowGump = v, "nameplate handler"),
        T("Mobiles", "Name overhead HP bar", p => p.NameOverheadShowHpBar, (p, v) => p.NameOverheadShowHpBar = v, "nameplate health"),
        T("Mobiles", "Target range indicator", p => p.ShowTargetRangeIndicator, (p, v) => p.ShowTargetRangeIndicator = v),
        T("Mobiles", "Drag select health bars", p => p.EnableDragSelect, (p, v) => p.EnableDragSelect = v, "lasso"),
        C("Mobiles", "Drag select modifier", new[] { "None", "Ctrl", "Shift" },
            p => p.DragSelectModifierKey, (p, v) => p.DragSelectModifierKey = v),
        T("Mobiles", "Drag select humanoids only", p => p.DragSelectHumanoidsOnly, (p, v) => p.DragSelectHumanoidsOnly = v),
        T("Mobiles", "Drag select hostile only", p => p.DragSelectHostileOnly, (p, v) => p.DragSelectHostileOnly = v),
        I("Mobiles", "Drag select start X", 0, 2000, 10, p => p.DragSelectStartX, (p, v) => p.DragSelectStartX = v),
        I("Mobiles", "Drag select start Y", 0, 2000, 10, p => p.DragSelectStartY, (p, v) => p.DragSelectStartY = v),
        T("Mobiles", "Drag select as anchor", p => p.DragSelectAsAnchor, (p, v) => p.DragSelectAsAnchor = v),

        // ===== Video =====
        T("Video", "Game window full size", p => p.GameWindowFullSize, (p, v) => p.GameWindowFullSize = v, "viewport fullscreen resize"),
        T("Video", "Lock game window", p => p.GameWindowLock, (p, v) => p.GameWindowLock = v, "viewport move"),
        I("Video", "Game window X", 0, 2000, 10, p => p.GameWindowPosition.X, (p, v) => p.GameWindowPosition = new Point(v, p.GameWindowPosition.Y), "viewport position"),
        I("Video", "Game window Y", 0, 2000, 10, p => p.GameWindowPosition.Y, (p, v) => p.GameWindowPosition = new Point(p.GameWindowPosition.X, v), "viewport position"),
        I("Video", "Game window width", 200, 2048, 20, p => p.GameWindowSize.X, (p, v) => p.GameWindowSize = new Point(v, p.GameWindowSize.Y), "viewport size"),
        I("Video", "Game window height", 200, 2048, 20, p => p.GameWindowSize.Y, (p, v) => p.GameWindowSize = new Point(p.GameWindowSize.X, v), "viewport size"),
        T("Video", "Borderless window", p => p.WindowBorderless, (p, v) => p.WindowBorderless = v, "fullscreen"),
        I("Video", "Default zoom (%)", 50, 300, 10, p => (int)(p.DefaultScale * 100f), (p, v) => p.DefaultScale = v / 100f, "scale camera"),
        T("Video", "Mousewheel zoom", p => p.EnableMousewheelScaleZoom, (p, v) => p.EnableMousewheelScaleZoom = v, "ctrl scale"),
        T("Video", "Save zoom on close", p => p.SaveScaleAfterClose, (p, v) => p.SaveScaleAfterClose = v, "scale"),
        T("Video", "Restore zoom after ctrl release", p => p.RestoreScaleAfterUnpressCtrl, (p, v) => p.RestoreScaleAfterUnpressCtrl = v, "scale"),
        T("Video", "Alternative lights", p => p.UseAlternativeLights, (p, v) => p.UseAlternativeLights = v),
        T("Video", "Custom light level", p => p.UseCustomLightLevel, (p, v) => p.UseCustomLightLevel = v),
        I("Video", "Light level", 0, 30, 1, p => p.LightLevel, (p, v) => p.LightLevel = (byte)v, "brightness"),
        C("Video", "Light level type", new[] { "Absolute", "Minimum" }, p => p.LightLevelType, (p, v) => p.LightLevelType = v),
        T("Video", "Colored lights", p => p.UseColoredLights, (p, v) => p.UseColoredLights = v),
        T("Video", "Dark nights", p => p.UseDarkNights, (p, v) => p.UseDarkNights = v),
        T("Video", "Shadows", p => p.ShadowsEnabled, (p, v) => p.ShadowsEnabled = v),
        T("Video", "Shadows on statics", p => p.ShadowsStatics, (p, v) => p.ShadowsStatics = v),
        I("Video", "Terrain shadow level", 5, 25, 1, p => p.TerrainShadowsLevel, (p, v) => p.TerrainShadowsLevel = v),
        T("Video", "Object fading", p => p.UseObjectsFading, (p, v) => p.UseObjectsFading = v, "transparency"),
        T("Video", "Text fading", p => p.TextFading, (p, v) => p.TextFading = v, "overhead"),
        T("Video", "Death screen", p => p.EnableDeathScreen, (p, v) => p.EnableDeathScreen = v),
        T("Video", "Black & white death effect", p => p.EnableBlackWhiteEffect, (p, v) => p.EnableBlackWhiteEffect = v, "grayscale"),
        T("Video", "Animated water", p => p.AnimatedWaterEffect, (p, v) => p.AnimatedWaterEffect = v),
        T("Video", "Aura on mouse target", p => p.AuraOnMouse, (p, v) => p.AuraOnMouse = v),
        T("Video", "xBR upscaling", p => p.UseXBR, (p, v) => p.UseXBR = v, "filter shader"),
        T("Video", "Reduce FPS when inactive", p => p.ReduceFPSWhenInactive, (p, v) => p.ReduceFPSWhenInactive = v, "background framerate"),
        T("Video", "Draw roofs", p => p.DrawRoofs, (p, v) => p.DrawRoofs = v, "hide house"),
        T("Video", "Trees to stumps", p => p.TreeToStumps, (p, v) => p.TreeToStumps = v, "filter"),
        T("Video", "Hide vegetation", p => p.HideVegetation, (p, v) => p.HideVegetation = v, "filter grass"),
        T("Video", "Mark cave tiles", p => p.EnableCaveBorder, (p, v) => p.EnableCaveBorder = v, "border"),
        C("Video", "Field types", new[] { "Normal", "Static", "Tile" }, p => p.FieldsType, (p, v) => p.FieldsType = v, "fire poison wall"),
        T("Video", "Circle of transparency", p => p.UseCircleOfTransparency, (p, v) => p.UseCircleOfTransparency = v),
        I("Video", "Circle radius", 50, 200, 10, p => p.CircleOfTransparencyRadius, (p, v) => p.CircleOfTransparencyRadius = v, "transparency"),
        C("Video", "Circle type", new[] { "Full", "Gradient" }, p => p.CircleOfTransparencyType, (p, v) => p.CircleOfTransparencyType = v, "transparency"),
        T("Video", "No color out of range", p => p.NoColorObjectsOutOfRange, (p, v) => p.NoColorObjectsOutOfRange = v, "gray"),

        // ===== Sound =====
        T("Sound", "Enable sounds", p => p.EnableSound, (p, v) => p.EnableSound = v, "audio effects"),
        I("Sound", "Sound volume", 0, 100, 5, p => p.SoundVolume, (p, v) => p.SoundVolume = v, "audio"),
        T("Sound", "Enable music", p => p.EnableMusic, (p, v) => p.EnableMusic = v, "audio"),
        I("Sound", "Music volume", 0, 100, 5, p => p.MusicVolume, (p, v) => p.MusicVolume = v, "audio"),
        T("Sound", "Footstep sounds", p => p.EnableFootstepsSound, (p, v) => p.EnableFootstepsSound = v, "walk"),
        T("Sound", "Combat music", p => p.EnableCombatMusic, (p, v) => p.EnableCombatMusic = v, "warmode"),
        T("Sound", "Sounds in background", p => p.ReproduceSoundsInBackground, (p, v) => p.ReproduceSoundsInBackground = v, "focus audio"),

        // ===== Chat =====
        I("Chat", "Chat font", 0, 9, 1, p => p.ChatFont, (p, v) => p.ChatFont = (byte)v, "speech"),
        I("Chat", "Speech delay", 0, 1000, 50, p => p.SpeechDelay, (p, v) => p.SpeechDelay = v, "overhead duration"),
        T("Chat", "Scale speech delay", p => p.ScaleSpeechDelay, (p, v) => p.ScaleSpeechDelay = v, "overhead duration"),
        T("Chat", "Save journal to file", p => p.SaveJournalToFile, (p, v) => p.SaveJournalToFile = v, "log"),
        T("Chat", "Force unicode journal", p => p.ForceUnicodeJournal, (p, v) => p.ForceUnicodeJournal = v, "font"),
        T("Chat", "Override game font", p => p.OverrideAllFonts, (p, v) => p.OverrideAllFonts = v, "text"),
        T("Chat", "Overridden font is unicode", p => p.OverrideAllFontsIsUnicode, (p, v) => p.OverrideAllFontsIsUnicode = v, "ascii"),
        T("Chat", "Ignore guild messages", p => p.IgnoreGuildMessages, (p, v) => p.IgnoreGuildMessages = v, "mute"),
        T("Chat", "Ignore alliance messages", p => p.IgnoreAllianceMessages, (p, v) => p.IgnoreAllianceMessages = v, "mute"),
        T("Chat", "Activate chat after enter", p => p.ActivateChatAfterEnter, (p, v) => p.ActivateChatAfterEnter = v, "input focus"),
        T("Chat", "Chat additional buttons", p => p.ActivateChatAdditionalButtons, (p, v) => p.ActivateChatAdditionalButtons = v, "modifier"),
        T("Chat", "Shift+Enter sends message", p => p.ActivateChatShiftEnterSupport, (p, v) => p.ActivateChatShiftEnterSupport = v),
        T("Chat", "Hide chat gradient", p => p.HideChatGradient, (p, v) => p.HideChatGradient = v, "background"),
        T("Chat", "Overhead party messages", p => p.OverheadPartyMessages, (p, v) => p.OverheadPartyMessages = v),
        T("Chat", "Journal dark mode", p => p.JournalDarkMode, (p, v) => p.JournalDarkMode = v),
        T("Chat", "Use alternate journal", p => p.UseAlternateJournal, (p, v) => p.UseAlternateJournal = v, "resizable tabs"),
        T("Chat", "Journal: client messages", p => p.ShowJournalClient, (p, v) => p.ShowJournalClient = v, "filter"),
        T("Chat", "Journal: object messages", p => p.ShowJournalObjects, (p, v) => p.ShowJournalObjects = v, "filter"),
        T("Chat", "Journal: system messages", p => p.ShowJournalSystem, (p, v) => p.ShowJournalSystem = v, "filter"),
        T("Chat", "Journal: guild & alliance", p => p.ShowJournalGuildAlly, (p, v) => p.ShowJournalGuildAlly = v, "filter"),
        H("Chat", "Speech hue", p => p.SpeechHue, (p, v) => p.SpeechHue = (ushort)v, "say"),
        H("Chat", "Whisper hue", p => p.WhisperHue, (p, v) => p.WhisperHue = (ushort)v),
        H("Chat", "Emote hue", p => p.EmoteHue, (p, v) => p.EmoteHue = (ushort)v),
        H("Chat", "Yell hue", p => p.YellHue, (p, v) => p.YellHue = (ushort)v),
        H("Chat", "Party message hue", p => p.PartyMessageHue, (p, v) => p.PartyMessageHue = (ushort)v),
        H("Chat", "Guild message hue", p => p.GuildMessageHue, (p, v) => p.GuildMessageHue = (ushort)v),
        H("Chat", "Alliance message hue", p => p.AllyMessageHue, (p, v) => p.AllyMessageHue = (ushort)v),
        H("Chat", "Chat message hue", p => p.ChatMessageHue, (p, v) => p.ChatMessageHue = (ushort)v),

        // ===== Combat =====
        T("Combat", "New target system", p => p.UseNewTargetSystem, (p, v) => p.UseNewTargetSystem = v),
        T("Combat", "Query before criminal actions", p => p.EnabledCriminalActionQuery, (p, v) => p.EnabledCriminalActionQuery = v, "confirm attack"),
        T("Combat", "Query before beneficial acts", p => p.EnabledBeneficialCriminalActionQuery, (p, v) => p.EnabledBeneficialCriminalActionQuery = v, "confirm heal criminal"),
        T("Combat", "Cast spells with one click", p => p.CastSpellsByOneClick, (p, v) => p.CastSpellsByOneClick = v, "spellbook"),
        T("Combat", "Show buff duration", p => p.BuffBarTime, (p, v) => p.BuffBarTime = v, "timer countdown"),
        T("Combat", "Fast spell assign", p => p.FastSpellsAssign, (p, v) => p.FastSpellsAssign = v, "hotkey"),
        T("Combat", "Overhead spell format", p => p.EnabledSpellFormat, (p, v) => p.EnabledSpellFormat = v, "power words"),
        T("Combat", "Overhead spell hue", p => p.EnabledSpellHue, (p, v) => p.EnabledSpellHue = v, "color"),
        T("Combat", "DPS with damage numbers", p => p.ShowDPSWithDamageNumbers, (p, v) => p.ShowDPSWithDamageNumbers = v),
        T("Combat", "Old bandage-self behavior", p => p.BandageSelfOld, (p, v) => p.BandageSelfOld = v, "heal"),
        T("Combat", "Stat change report", p => p.EnableStatReport, (p, v) => p.EnableStatReport = v, "message"),
        T("Combat", "Skill change report", p => p.EnableSkillReport, (p, v) => p.EnableSkillReport = v, "message"),
        H("Combat", "Innocent hue", p => p.InnocentHue, (p, v) => p.InnocentHue = (ushort)v, "notoriety blue"),
        H("Combat", "Friend hue", p => p.FriendHue, (p, v) => p.FriendHue = (ushort)v, "notoriety"),
        H("Combat", "Criminal hue", p => p.CriminalHue, (p, v) => p.CriminalHue = (ushort)v, "notoriety gray"),
        H("Combat", "Can-attack hue", p => p.CanAttackHue, (p, v) => p.CanAttackHue = (ushort)v, "notoriety gray"),
        H("Combat", "Murderer hue", p => p.MurdererHue, (p, v) => p.MurdererHue = (ushort)v, "notoriety red"),
        H("Combat", "Enemy hue", p => p.EnemyHue, (p, v) => p.EnemyHue = (ushort)v, "notoriety orange"),
        H("Combat", "Beneficial spell hue", p => p.BeneficHue, (p, v) => p.BeneficHue = (ushort)v),
        H("Combat", "Harmful spell hue", p => p.HarmfulHue, (p, v) => p.HarmfulHue = (ushort)v),
        H("Combat", "Neutral spell hue", p => p.NeutralHue, (p, v) => p.NeutralHue = (ushort)v),

        // ===== Containers =====
        C("Containers", "Backpack style", new[] { "Default", "Suede", "Polar bear", "Ghoul skin" },
            p => p.BackpackStyle, (p, v) => p.BackpackStyle = v),
        I("Containers", "Container scale (%)", 70, 200, 10, p => p.ContainersScale, (p, v) => p.ContainersScale = (byte)v, "size zoom"),
        T("Containers", "Scale items inside", p => p.ScaleItemsInsideContainers, (p, v) => p.ScaleItemsInsideContainers = v, "zoom"),
        T("Containers", "Large container gumps", p => p.UseLargeContainerGumps, (p, v) => p.UseLargeContainerGumps = v),
        T("Containers", "Double click to loot", p => p.DoubleClickToLootInsideContainers, (p, v) => p.DoubleClickToLootInsideContainers = v),
        T("Containers", "Relative drag and drop", p => p.RelativeDragAndDropItems, (p, v) => p.RelativeDragAndDropItems = v),
        T("Containers", "Highlight on hover", p => p.HighlightContainerWhenSelected, (p, v) => p.HighlightContainerWhenSelected = v),
        T("Containers", "Hue container gumps", p => p.HueContainerGumps, (p, v) => p.HueContainerGumps = v, "color"),
        T("Containers", "Grid containers", p => p.UseGridContainers, (p, v) => p.UseGridContainers = v, "grid view search sort"),
        T("Containers", "Override container location", p => p.OverrideContainerLocation, (p, v) => p.OverrideContainerLocation = v, "position"),
        C("Containers", "Container location mode", new[] { "Near container", "Top right", "Last dragged", "Remember each" },
            p => p.OverrideContainerLocationSetting, (p, v) => p.OverrideContainerLocationSetting = v, "position"),
        C("Containers", "Grid loot", new[] { "Disabled", "Grid only", "Grid + classic" },
            p => p.GridLootType, (p, v) => p.GridLootType = v, "corpse"),

        // ===== Interface =====
        T("Interface", "Use old status gump", p => p.UseOldStatusGump, (p, v) => p.UseOldStatusGump = v, "classic layout"),
        T("Interface", "Status bars mutually exclusive", p => p.StatusGumpBarMutuallyExclusive, (p, v) => p.StatusGumpBarMutuallyExclusive = v),
        T("Interface", "Custom health bars", p => p.CustomBarsToggled, (p, v) => p.CustomBarsToggled = v, "hp"),
        T("Interface", "Custom bars black background", p => p.CBBlackBGToggled, (p, v) => p.CBBlackBGToggled = v, "hp"),
        T("Interface", "Save health bars on logout", p => p.SaveHealthbars, (p, v) => p.SaveHealthbars = v),
        C("Interface", "Close health bars", new[] { "Never", "Out of range", "On death" },
            p => p.CloseHealthBarType, (p, v) => p.CloseHealthBarType = v, "hp dispose"),
        T("Interface", "Show party invite gump", p => p.PartyInviteGump, (p, v) => p.PartyInviteGump = v, "popup"),
        I("Interface", "Vendor gump height", 30, 120, 10, p => p.VendorGumpHeight, (p, v) => p.VendorGumpHeight = v, "shop size"),
        T("Interface", "Standard skills gump", p => p.StandardSkillsGump, (p, v) => p.StandardSkillsGump = v, "advanced"),
        T("Interface", "Use tooltips", p => p.UseTooltip, (p, v) => p.UseTooltip = v, "item properties"),
        I("Interface", "Tooltip delay (ms)", 0, 1000, 50, p => p.TooltipDelayBeforeDisplay, (p, v) => p.TooltipDelayBeforeDisplay = v),
        I("Interface", "Tooltip zoom (%)", 100, 200, 10, p => p.TooltipDisplayZoom, (p, v) => p.TooltipDisplayZoom = v),
        I("Interface", "Tooltip background opacity", 0, 100, 5, p => p.TooltipBackgroundOpacity, (p, v) => p.TooltipBackgroundOpacity = v, "transparent"),
        H("Interface", "Tooltip text hue", p => p.TooltipTextHue, (p, v) => p.TooltipTextHue = (ushort)v),
        I("Interface", "Tooltip font", 0, 9, 1, p => p.TooltipFont, (p, v) => p.TooltipFont = (byte)v),
        T("Interface", "Show info bar", p => p.ShowInfoBar, (p, v) => p.ShowInfoBar = v, "hud"),
        C("Interface", "Info bar highlight", new[] { "Text color", "Bars" },
            p => p.InfoBarHighlightType, (p, v) => p.InfoBarHighlightType = v),
        T("Interface", "Enable counter bar", p => p.CounterBarEnabled, (p, v) => p.CounterBarEnabled = v, "consumables"),
        T("Interface", "Counter: highlight on change", p => p.CounterBarHighlightOnChange, (p, v) => p.CounterBarHighlightOnChange = v),
        T("Interface", "Counter: highlight when low", p => p.CounterBarHighlightOnAmount, (p, v) => p.CounterBarHighlightOnAmount = v, "red threshold"),
        I("Interface", "Counter: low threshold", 1, 100, 1, p => p.CounterBarHighlightAmount, (p, v) => p.CounterBarHighlightAmount = v),
        T("Interface", "Counter: abbreviate amounts", p => p.CounterBarDisplayAbbreviatedAmount, (p, v) => p.CounterBarDisplayAbbreviatedAmount = v, "1k"),
        I("Interface", "Counter: abbreviate from", 100, 10000, 100, p => p.CounterBarAbbreviatedAmount, (p, v) => p.CounterBarAbbreviatedAmount = v),
        I("Interface", "Counter: cell size", 30, 80, 5, p => p.CounterBarCellSize, (p, v) => p.CounterBarCellSize = v),

        // ===== World Map =====
        I("World Map", "Map width", 200, 2000, 50, p => p.WorldMapWidth, (p, v) => p.WorldMapWidth = v, "size"),
        I("World Map", "Map height", 200, 2000, 50, p => p.WorldMapHeight, (p, v) => p.WorldMapHeight = v, "size"),
        I("World Map", "Map font", 1, 6, 1, p => p.WorldMapFont, (p, v) => p.WorldMapFont = v),
        I("World Map", "Zoom index", 0, 10, 1, p => p.WorldMapZoomIndex, (p, v) => p.WorldMapZoomIndex = v),
        T("World Map", "Flip map", p => p.WorldMapFlipMap, (p, v) => p.WorldMapFlipMap = v, "rotate 45"),
        T("World Map", "Top most", p => p.WorldMapTopMost, (p, v) => p.WorldMapTopMost = v, "always on top"),
        T("World Map", "Free view", p => p.WorldMapFreeView, (p, v) => p.WorldMapFreeView = v, "pan"),
        T("World Map", "Show party members", p => p.WorldMapShowParty, (p, v) => p.WorldMapShowParty = v),
        T("World Map", "Show coordinates", p => p.WorldMapShowCoordinates, (p, v) => p.WorldMapShowCoordinates = v),
        T("World Map", "Show mouse coordinates", p => p.WorldMapShowMouseCoordinates, (p, v) => p.WorldMapShowMouseCoordinates = v),
        T("World Map", "Show sextant coordinates", p => p.WorldMapShowSextantCoordinates, (p, v) => p.WorldMapShowSextantCoordinates = v),
        T("World Map", "Show mobiles", p => p.WorldMapShowMobiles, (p, v) => p.WorldMapShowMobiles = v),
        T("World Map", "Show player name", p => p.WorldMapShowPlayerName, (p, v) => p.WorldMapShowPlayerName = v),
        T("World Map", "Show player health bar", p => p.WorldMapShowPlayerBar, (p, v) => p.WorldMapShowPlayerBar = v),
        T("World Map", "Show group names", p => p.WorldMapShowGroupName, (p, v) => p.WorldMapShowGroupName = v, "party"),
        T("World Map", "Show group health bars", p => p.WorldMapShowGroupBar, (p, v) => p.WorldMapShowGroupBar = v, "party"),
        T("World Map", "Show markers", p => p.WorldMapShowMarkers, (p, v) => p.WorldMapShowMarkers = v, "pins"),
        T("World Map", "Show marker names", p => p.WorldMapShowMarkersNames, (p, v) => p.WorldMapShowMarkersNames = v, "pins labels"),
        T("World Map", "Show multis", p => p.WorldMapShowMultis, (p, v) => p.WorldMapShowMultis = v, "houses boats"),
        T("World Map", "Show grid when zoomed", p => p.WorldMapShowGridIfZoomed, (p, v) => p.WorldMapShowGridIfZoomed = v),
        T("World Map", "Allow positional targeting", p => p.WorldMapAllowPositionalTarget, (p, v) => p.WorldMapAllowPositionalTarget = v, "click target"),

        // ===== Experimental =====
        T("Experimental", "Disable default UO hotkeys", p => p.DisableDefaultHotkeys, (p, v) => p.DisableDefaultHotkeys = v, "keyboard"),
        T("Experimental", "Disable arrow keys movement", p => p.DisableArrowBtn, (p, v) => p.DisableArrowBtn = v, "keyboard"),
        T("Experimental", "Disable tab key", p => p.DisableTabBtn, (p, v) => p.DisableTabBtn = v, "keyboard warmode"),
        T("Experimental", "Disable Ctrl+Q/W history", p => p.DisableCtrlQWBtn, (p, v) => p.DisableCtrlQWBtn = v, "keyboard message"),
        T("Experimental", "Disable click auto-move", p => p.DisableAutoMove, (p, v) => p.DisableAutoMove = v, "mouse"),
        T("Experimental", "KR equip/unequip packets", p => p.UseKrEquipUnequipPacket, (p, v) => p.UseKrEquipUnequipPacket = v, "network"),

        // ===== Debug (client-local feature launchers; the old test window's rows) =====
        new() { Category = "Debug", Label = "Open color picker", Keywords = "dye hue test",
                Kind = OptionKind.Action, Run = static (cmd, p) =>
                    ColorPickerPlugin.Open(cmd, p.Assets.Value, p.Builder.Value, p.Files.Value.Hues,
                        p.ZCounter.Value, p.Surface.Value, serial: 0, graphic: 0x0FAB) },
    };

    private static readonly string[] s_categories =
    {
        "General", "Mobiles", "Video", "Sound", "Chat",
        "Combat", "Containers", "Interface", "World Map", "Experimental", "Debug",
    };

    public void Build(App app)
    {
        app.AddResource(new OptionsUiState());

        Action<Commands, Res<OptionsUiState>, Query<Data<OptionsWindow>>> teardownFn =
            (cmd, state, q) =>
            {
                foreach (var (ent, _) in q) cmd.Entity(ent.Ref).Despawn();
                state.Value.Window = 0;
            };
        app.AddSystem(teardownFn).OnExit(GameState.GameScreen).Build();

        // Right-click close (UiMovable) despawns the window without going
        // through the X button — keep the state in sync or the rebuild system
        // would AddChild onto a dead container.
        app.AddObserver((On<OnRemove<OptionsWindow>> trig, Res<OptionsUiState> state) =>
        {
            if (state.Value.Window == trig.EntityId) state.Value.Window = 0;
        });

        var pollSearchFn = PollSearch;
        app.AddSystem(pollSearchFn)
            .InStage(Stage.Update)
            .RunIf((Res<OptionsUiState> s) => s.Value.Window != 0)
            .Build();

        var rebuildFn = Rebuild;
        app.AddSystem(rebuildFn)
            .InStage(Stage.Update)
            .RunIf((Res<OptionsUiState> s) => s.Value.Dirty && s.Value.Window != 0)
            .Build();

        var syncHueFn = SyncHueEdits;
        app.AddSystem(syncHueFn)
            .InStage(Stage.Update)
            .RunIf((Res<OptionsUiState> s) => s.Value.Window != 0)
            .Build();

        var closeOverlayFn = CloseOverlayOnOutsideClick;
        app.AddSystem(closeOverlayFn)
            .InStage(Stage.Update)
            .RunIf((Res<OptionsUiState> s) => s.Value.Window != 0)
            .Build();

        // Hover tint: any element carrying OptionsHover swaps its background
        // between Normal/Hover based on Clay's Interaction state. One system
        // covers every control (rows, sidebar, buttons, dropdown items).
        var hoverFn = ApplyHover;
        app.AddSystem(hoverFn)
            .InStage(Stage.Last)
            .RunIf((Res<OptionsUiState> s) => s.Value.Window != 0)
            .Build();

#if AGENT_BUILD
        app.AddResource(new DebugOptionsQueue());
        var drainFn = DrainDebugOpen;
        app.AddSystem(drainFn).InStage(Stage.First).Build();
#endif
    }

#if AGENT_BUILD
    private static void DrainDebugOpen(
        Commands commands,
        ResMut<DebugOptionsQueue> q,
        Res<UiZCounter> zc,
        Res<UiSurface> surface,
        Res<OptionsUiState> state,
        Query<Data<OptionsWindow>> existing)
    {
        if (!q.Value.OpenRequested) return;
        q.Value.OpenRequested = false;
        OpenOrFocus(commands, zc.Value, surface.Value, state.Value, existing);
    }
#endif

    private static void ApplyHover(Query<Data<Interaction, BackgroundColor, OptionsHover>> q)
    {
        foreach (var (_, it, bg, hv) in q)
        {
            var want = it.Ref == Interaction.None ? hv.Ref.Normal : hv.Ref.Hover;
            if (bg.Ref.Value.R != want.R || bg.Ref.Value.G != want.G
                || bg.Ref.Value.B != want.B || bg.Ref.Value.A != want.A)
                bg.Ref = new BackgroundColor(want);
        }
    }

    internal static void OpenOrFocus(
        Commands commands, UiZCounter zc, UiSurface surface, OptionsUiState state,
        Query<Data<OptionsWindow>> existing)
    {
        foreach (var (ent, _) in existing)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zc.Bump()));
            return;
        }

        // Derive height from a whole number of rows so the viewport never clips
        // mid-row (no bottom sliver/gap). Row count fills the screen, clamped.
        int surfH = (int)surface.LogicalSize.Y;
        int rows = Math.Clamp((surfH - 24 - ChromeV - SectionH) / RowPitch, MinRows, MaxRows);
        int viewH = SectionH + rows * RowPitch;
        int winH = viewH + ChromeV;

        var cx = MathF.Max(0, (surface.LogicalSize.X - WinW) * 0.5f);
        var cy = MathF.Max(0, (surface.LogicalSize.Y - winH) * 0.5f);

        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                FlexDirection = FlexDirection.Column,
                Left = Val.Px(cx), Top = Val.Px(cy),
                Width = Val.Px(WinW), Height = Val.Px(winH),
                // Tighter bottom: the scroll list ends on a whole row + the
                // inter-row gap, so a full Pad below it reads as dead space.
                Padding = new UiRect { Left = Val.Px(Pad), Right = Val.Px(Pad), Top = Val.Px(Pad), Bottom = Val.Px(BottomPad) },
                Gap = Val.Px(12),
                Border = UiRect.All(1),
            })
            .Insert(new BackgroundColor(s_panelBg))
            .Insert(new BorderColor(s_panelBorder))
            .Insert(BorderRadius.All(14))
            .Insert(new BoxShadow { Color = s_shadow, OffsetX = 0, OffsetY = 6, BlurRadius = 16, SpreadRadius = 0 })
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(zc.Bump()))
            .Insert(new OptionsWindow());
        var rootId = root.Id;

        // ---- header: title + search field + close ----
        var header = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Val.Px(WinW - Pad * 2), Height = Val.Px(HeaderH - Pad),
                Gap = Val.Px(12),
            });
        commands.AddChild(rootId, header.Id);
        var headerId = header.Id;

        // Accent tab on the title so the header reads as a header.
        commands.AddChild(headerId, commands.Spawn()
            .Insert(new Node { Width = Val.Px(4), Height = Val.Px(22) })
            .Insert(new BackgroundColor(s_accent))
            .Insert(BorderRadius.All(2))
            .Id);
        commands.AddChild(headerId, commands.Spawn()
            .Insert(new Node { Width = Val.Px(92), Height = Val.Auto })
            .Insert(new Text("Options"))
            .Insert(new TextFont { FontId = 1, Size = 18 })
            .Insert(new TextColor(s_textMain))
            .Id);

        // Search box: light frame so the dark ASCII glyphs stay readable.
        var searchFrame = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Val.Px(300), Height = Val.Px(28),
                Padding = new UiRect { Left = Val.Px(8), Right = Val.Px(8) },
            })
            .Insert(new BackgroundColor(s_field))
            .Insert(BorderRadius.All(8));
        var searchTextId = GuiPlugin.SpawnTextField(
            commands, searchFrame, new Vector2(8, 6),
            new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 16 },
            UoFontRuntime.AsciiHue(1), string.Empty, masked: false);
        commands.Entity(searchTextId).Insert<OptionsSearchText>();
        commands.AddChild(headerId, searchFrame.Id);

        commands.AddChild(headerId, commands.Spawn()
            .Insert(new Node { Width = Val.Px(96), Height = Val.Auto })
            .Insert(new Text("Search settings"))
            .Insert(new TextFont { FontId = 1, Size = 11 })
            .Insert(new TextColor(s_textFaint))
            .Id);

        // Close button — absolute so it pins to the header's right edge
        // (Bevy.UI has no space-between; the header is not scrolled, so an
        // absolute child is safe here).
        commands.AddChild(headerId, commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(WinW - Pad * 2 - 28), Top = Val.Px(0),
                Width = Val.Px(28), Height = Val.Px(28),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            })
            .Insert(new BackgroundColor(s_controlBg))
            .Insert(new OptionsHover { Normal = s_controlBg, Hover = new ClayColor(196, 72, 72, 255) })
            .Insert(BorderRadius.All(8))
            .Insert(new Text("X"))
            .Insert(new TextFont { FontId = 1, Size = 12 })
            .Insert(new TextColor(s_textMain))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Observe((On<UiClick> _, Commands cmd, Res<OptionsUiState> st) =>
            {
                if (st.Value.Window != 0) cmd.Entity(st.Value.Window).Despawn();
                st.Value.Window = 0;
            })
            .Id);

        // ---- body: sidebar + scrollable rows ----
        var body = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Row,
                Width = Val.Px(WinW - Pad * 2),
                // Fills the area below the header down to the bottom padding;
                // equals the viewport height (a whole number of rows).
                Height = Val.Px(viewH),
                Gap = Val.Px(10),
            });
        commands.AddChild(rootId, body.Id);
        var bodyId = body.Id;

        // Nav tabs stack tight at the top of the card (no gaps between them);
        // the empty space falls below the last tab.
        const int sideGap = 0;
        var sidebar = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Val.Px(SidebarW), Height = Val.Percent(100),
                Padding = UiRect.All(6),
                Gap = Val.Px(sideGap),
            })
            .Insert(new BackgroundColor(s_sideBg))
            .Insert(BorderRadius.All(10));
        commands.AddChild(bodyId, sidebar.Id);
        var viewport = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Val.Px(RowW), Height = Val.Px(viewH),
                Gap = Val.Px(RowGap),
                Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition());
        commands.AddChild(bodyId, viewport.Id);

        // Flat-styled scrollbar (same lib widgets AddVScrollbar wires, but
        // BackgroundColor visuals instead of UO art). Absolute child of the
        // body container — not scrolled, so no clip-escape concern.
        var sbTrack = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(SidebarW + 10 + RowW + 6), Top = Val.Px(0),
                Width = Val.Px(8), Height = Val.Px(viewH),
            })
            .Insert(new BackgroundColor(new ClayColor(30, 32, 40, 255)))
            .Insert(BorderRadius.All(4))
            .Insert(new Scrollbar { Target = viewport.Id, Orientation = ScrollbarOrientation.Vertical, MinThumbLength = 28f })
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        var sbThumb = commands.Spawn()
            .Insert(new Node { Width = Val.Px(8), Height = Val.Px(28) })
            .Insert(new BackgroundColor(new ClayColor(86, 92, 112, 255)))
            .Insert(new OptionsHover { Normal = new ClayColor(86, 92, 112, 255), Hover = new ClayColor(110, 118, 142, 255) })
            .Insert(BorderRadius.All(4))
            .Insert(new ScrollbarThumb())
            .Insert(new ScrollbarDragState())
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        commands.AddChild(sbTrack.Id, sbThumb.Id);
        commands.AddChild(bodyId, sbTrack.Id);

        state.Window = rootId;
        state.Sidebar = sidebar.Id;
        state.Viewport = viewport.Id;
        state.Search = string.Empty;
        state.Category = s_categories[0];
        state.Dirty = true;
    }

    // The search field's Text is edited by the global text editor; diff it
    // against the cached value and flag a rebuild on change.
    private static void PollSearch(
        Res<OptionsUiState> state,
        Query<Data<Text>, Filter<With<OptionsSearchText>>> q)
    {
        foreach (var (_, t) in q)
        {
            var v = t.Ref.Value ?? string.Empty;
            if (!string.Equals(v, state.Value.Search, StringComparison.Ordinal))
            {
                state.Value.Search = v;
                state.Value.Dirty = true;
                state.Value.FilterChanged = true;
            }
        }
    }

    private static void Rebuild(
        Commands commands,
        Res<OptionsUiState> stateRes,
        Res<Profile> profile,
        Res<UOFileManager> files,
        Query<Data<Node>, Filter<With<OptionsListItem>>> itemsQ,
        Query<Data<ScrollPosition>> scrollQ)
    {
        var state = stateRes.Value;
        state.Dirty = false;

        // New list, new scroll — but only when the FILTER changed (category /
        // search), not on a value tweak: a stepper click rebuilds too, and
        // jumping to the top would be obnoxious mid-edit.
        if (state.FilterChanged && scrollQ.TryGet(state.Viewport, out var scrollRow))
        {
            var (_, sp) = scrollRow;
            sp.Ref.OffsetX = 0;
            sp.Ref.OffsetY = 0;
        }
        state.FilterChanged = false;

        foreach (var (ent, _) in itemsQ) commands.Entity(ent.Ref).Despawn();

        bool searching = !string.IsNullOrWhiteSpace(state.Search);

        // Sidebar category buttons (highlight ignored while a search is live).
        int innerW = SidebarW - 12;
        foreach (var cat in s_categories)
        {
            var selected = !searching && cat == state.Category;
            var catCopy = cat;
            var btn = commands.Spawn()
                .Insert<OptionsListItem>()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    FlexDirection = FlexDirection.Row,
                    Width = Val.Px(innerW), Height = Val.Px(SideBtnH),
                    AlignItems = AlignItems.Center,
                    Gap = Val.Px(8),
                    Padding = new UiRect { Left = Val.Px(8), Right = Val.Px(8) },
                })
                .Insert(new BackgroundColor(selected ? s_accent : s_rowBg))
                .Insert(BorderRadius.All(7))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Observe((On<UiClick> _, Res<OptionsUiState> st) =>
                {
                    st.Value.Category = catCopy;
                    st.Value.Dirty = true;
                    st.Value.FilterChanged = true;
                });
            if (!selected)
                btn.Insert(new OptionsHover { Normal = s_rowBg, Hover = s_sideHover });
            var btnId = btn.Id;
            commands.AddChild(state.Sidebar, btnId);
            // Accent dot marks the active page; a placeholder keeps inactive
            // labels aligned to the same left edge.
            commands.AddChild(btnId, commands.Spawn()
                .Insert(new Node { Width = Val.Px(6), Height = Val.Px(6) })
                .Insert(new BackgroundColor(selected ? s_textMain : s_textFaint))
                .Insert(BorderRadius.All(3))
                .Id);
            commands.AddChild(btnId, commands.Spawn()
                .Insert(new Node { Width = Val.Px(innerW - 30), Height = Val.Auto })
                .Insert(new Text(cat))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(selected ? s_textMain : s_textDim))
                .Id);
        }

        // Section header at the top of the list. Fixed height (SectionH) so the
        // viewport holds a whole number of rows below it — keeps the scroll
        // bottom flush instead of clipping a row sliver.
        var headerLabel = searching ? $"Search: \"{state.Search.Trim()}\"" : state.Category;
        commands.AddChild(state.Viewport, commands.Spawn()
            .Insert<OptionsListItem>()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                Width = Val.Px(RowW), Height = Val.Px(SectionH),
                Padding = new UiRect { Left = Val.Px(4) },
            })
            .Insert(new Text(headerLabel))
            .Insert(new TextFont { FontId = 1, Size = 15 })
            .Insert(new TextColor(s_textMain))
            .Id);

        for (var i = 0; i < s_options.Length; i++)
        {
            var def = s_options[i];
            if (searching)
            {
                if (!def.Matches(state.Search.Trim())) continue;
            }
            else if (def.Category != state.Category)
            {
                continue;
            }

            SpawnRow(commands, profile.Value, files.Value.Hues, state, def, i, searching);
        }
    }

    private static void SpawnRow(
        Commands commands, Profile profile, HuesLoader hues, OptionsUiState state,
        OptionDef def, int defIndex, bool searching)
    {
        var row = commands.Spawn()
            .Insert<OptionsListItem>()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Val.Px(RowW), Height = Val.Px(RowH),
                Padding = new UiRect { Left = Val.Px(14), Right = Val.Px(12) },
            })
            .Insert(new BackgroundColor(s_rowBg))
            .Insert(new OptionsHover { Normal = s_rowBg, Hover = s_rowHover })
            .Insert(BorderRadius.All(8))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        commands.AddChild(state.Viewport, row.Id);
        var rowId = row.Id;

        // While searching the row carries its category so results from other
        // pages stay readable.
        var labelText = searching ? $"{def.Category}  ·  {def.Label}" : def.Label;
        commands.AddChild(rowId, commands.Spawn()
            .Insert(new Node { Width = Val.Px(LabelW), Height = Val.Auto })
            .Insert(new Text(labelText))
            .Insert(new TextFont { FontId = 1, Size = 13 })
            .Insert(new TextColor(s_textMain))
            .Id);

        int contentW = RowW - 20;
        switch (def.Kind)
        {
            case OptionKind.Toggle:
            {
                const int TrackW = 46, TrackH = 24, Knob = 18;
                AddSpacer(commands, rowId, contentW - LabelW - TrackW);
                var on = def.GetB(profile);
                // Real switch: a rounded track whose flow child (the knob) is
                // pushed to Start/End. Flow — not absolute — so it rides the
                // scroll clip with the rest of the list.
                var track = commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        FlexDirection = FlexDirection.Row,
                        Width = Val.Px(TrackW), Height = Val.Px(TrackH),
                        JustifyContent = on ? JustifyContent.End : JustifyContent.Start,
                        AlignItems = AlignItems.Center,
                        Padding = new UiRect { Left = Val.Px(3), Right = Val.Px(3) },
                    })
                    .Insert(new BackgroundColor(on ? s_toggleOn : s_toggleOff))
                    .Insert(BorderRadius.All(TrackH / 2))
                    .Insert(Interaction.None)
                    .Insert<UiNoWindowDrag>()
                    .Observe((On<UiClick> _, Res<Profile> p, Res<OptionsUiState> st) =>
                    {
                        def.SetB(p.Value, !def.GetB(p.Value));
                        st.Value.Dirty = true;
                    });
                var trackId = track.Id;
                commands.AddChild(trackId, commands.Spawn()
                    .Insert(new Node { Width = Val.Px(Knob), Height = Val.Px(Knob) })
                    .Insert(new BackgroundColor(s_knob))
                    .Insert(BorderRadius.All(Knob / 2))
                    .Id);
                commands.AddChild(rowId, trackId);
                break;
            }

            case OptionKind.Stepper:
            {
                const int StepW = 26, ValW = 58;
                AddSpacer(commands, rowId, contentW - LabelW - (StepW + ValW + StepW + 8));
                SpawnStepButton(commands, rowId, "−", () => def, -1);
                AddSpacer(commands, rowId, 4);
                commands.AddChild(rowId, commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        Width = Val.Px(ValW), Height = Val.Px(26),
                        JustifyContent = JustifyContent.Center,
                        AlignItems = AlignItems.Center,
                    })
                    .Insert(new BackgroundColor(new ClayColor(30, 32, 40, 255)))
                    .Insert(BorderRadius.All(6))
                    .Insert(new Text(def.GetI(profile).ToString()))
                    .Insert(new TextFont { FontId = 1, Size = 13 })
                    .Insert(new TextColor(s_textMain))
                    .Id);
                AddSpacer(commands, rowId, 4);
                SpawnStepButton(commands, rowId, "+", () => def, +1);
                break;
            }

            case OptionKind.Cycle:
            {
                AddSpacer(commands, rowId, contentW - LabelW - 140);
                var current = Math.Clamp(def.GetI(profile), 0, def.Choices.Length - 1);
                // Selectbox: shows the current choice; click opens a dropdown
                // overlay anchored under the control (spawned on the window
                // root — an overlay inside the scrolled list would be clipped).
                var box = commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        Width = Val.Px(150), Height = Val.Px(28),
                        JustifyContent = JustifyContent.Center,
                        AlignItems = AlignItems.Center,
                    })
                    .Insert(new BackgroundColor(s_controlBg))
                    .Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover })
                    .Insert(BorderRadius.All(8))
                    .Insert(new Text($"{def.Choices[current]}   v"))
                    .Insert(new TextFont { FontId = 1, Size = 12 })
                    .Insert(new TextColor(s_textMain))
                    .Insert(Interaction.None)
                    .Insert<UiNoWindowDrag>();
                var boxId = box.Id;
                box.Observe((
                    On<UiClick> _,
                    Commands cmd,
                    Res<Profile> p,
                    Res<OptionsUiState> st,
                    Query<Data<ComputedNode>> geomQ,
                    Query<Data<Node>, Filter<With<OptionsOverlay>>> overlays) =>
                        OpenSelectOverlay(cmd, st.Value, p.Value, defIndex, boxId, geomQ, overlays));
                commands.AddChild(rowId, boxId);
                break;
            }

            case OptionKind.Hue:
            {
                AddSpacer(commands, rowId, contentW - LabelW - (60 + 8 + 28));

                // Numbers-only editable value (SyncHueEdits enforces + applies).
                var field = commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        Width = Val.Px(60), Height = Val.Px(24),
                    })
                    .Insert(new BackgroundColor(s_field))
                    .Insert(BorderRadius.All(6));
                // flowRow: an absolute glyph row would escape the list's
                // scroll clip and paint the value outside the window.
                var glyphId = GuiPlugin.SpawnTextField(
                    commands, field, new Vector2(5, 3),
                    new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 16 },
                    UoFontRuntime.AsciiHue(1), def.GetI(profile).ToString(), masked: false,
                    flowRow: true);
                commands.Entity(glyphId).Insert(new HueValueText { Index = defIndex });
                commands.AddChild(rowId, field.Id);

                AddSpacer(commands, rowId, 8);

                // Swatch picker icon — shows the current hue, opens the palette.
                var swatch = commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        Width = Val.Px(28), Height = Val.Px(24),
                    })
                    .Insert(new UiCustom
                    {
                        Data = new UOCustomRender
                        {
                            Kind = UOCustomKind.HueGrid,
                            GridRows = 1, GridCols = 1, CellW = 28, CellH = 24,
                            SelectedIndex = -1,
                            GridColors = new[] { BakeHue(hues, (ushort)def.GetI(profile)) },
                        }
                    })
                    .Insert(new HueSwatch { Index = defIndex })
                    .Insert(Interaction.None)
                    .Insert<UiContainsByBounds>()
                    .Insert<UiNoWindowDrag>()
                    .Observe((
                        On<UiClick> _,
                        Commands cmd,
                        Res<Profile> p,
                        Res<OptionsUiState> st,
                        OptionsButtonParams bp) =>
                    {
                        // The classic dye picker in local mode: OK hands the
                        // hue back instead of sending the dye packet. Profile
                        // and state are app-lifetime resources — safe captures.
                        var profileRef = p.Value;
                        var stateRef = st.Value;
                        ColorPickerPlugin.Open(cmd, bp.Assets.Value, bp.Builder.Value,
                            bp.Files.Value.Hues, bp.ZCounter.Value, bp.Surface.Value,
                            serial: 0, graphic: 0x0FAB,
                            onPick: hue =>
                            {
                                def.SetI(profileRef, hue);
                                stateRef.Dirty = true;
                            });
                    });
                commands.AddChild(rowId, swatch.Id);
                break;
            }

            case OptionKind.Action:
            {
                AddSpacer(commands, rowId, contentW - LabelW - 80);
                var act = commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        Width = Val.Px(80), Height = Val.Px(28),
                        JustifyContent = JustifyContent.Center,
                        AlignItems = AlignItems.Center,
                    })
                    .Insert(new BackgroundColor(s_accent))
                    .Insert(new OptionsHover { Normal = s_accent, Hover = s_accentHover })
                    .Insert(BorderRadius.All(8))
                    .Insert(new Text("Open"))
                    .Insert(new TextFont { FontId = 1, Size = 12 })
                    .Insert(new TextColor(s_textMain))
                    .Insert(Interaction.None)
                    .Insert<UiNoWindowDrag>()
                    .Observe((On<UiClick> _, Commands cmd, OptionsButtonParams p) => def.Run(cmd, p));
                commands.AddChild(rowId, act.Id);
                break;
            }
        }
    }

    private static void SpawnStepButton(
        Commands commands, ulong rowId, string glyph, Func<OptionDef> defGet, int direction)
    {
        var def = defGet();
        var btn = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                Width = Val.Px(26), Height = Val.Px(26),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            })
            .Insert(new BackgroundColor(s_controlBg))
            .Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover })
            .Insert(BorderRadius.All(7))
            .Insert(new Text(glyph))
            .Insert(new TextFont { FontId = 1, Size = 15 })
            .Insert(new TextColor(s_textMain))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Observe((On<UiClick> _, Res<Profile> p, Res<OptionsUiState> st) =>
            {
                var v = Math.Clamp(def.GetI(p.Value) + direction * def.Step, def.Min, def.Max);
                def.SetI(p.Value, v);
                st.Value.Dirty = true;
            });
        commands.AddChild(rowId, btn.Id);
    }

    // Fixed-width filler that pushes the control to the row's right edge —
    // Bevy.UI's JustifyContent has no space-between, and an absolute child
    // would escape the scroll clip.
    private static void AddSpacer(Commands commands, ulong rowId, int width)
    {
        commands.AddChild(rowId, commands.Spawn()
            .Insert(new Node { Width = Val.Px(Math.Max(0, width)), Height = Val.Px(1) })
            .Id);
    }

    private static uint BakeHue(HuesLoader hues, ushort hue)
    {
        uint c = hues.GetPolygoneColor(30, hue);
        if ((c >> 24) == 0) c |= 0xFF000000u;
        return c;
    }

    // Two-way binding for hue value fields, keyed on edit focus:
    //  - focused: the field drives the profile (numbers-only enforcement,
    //    4-digit cap, clamp) and patches the matching swatch in place. No
    //    Dirty flag — a rebuild would despawn the field mid-typing.
    //  - not focused: the profile drives the field, so an overlay pick (or
    //    any other writer) shows up without a rebuild fighting stale text.
    private static void SyncHueEdits(
        Res<Profile> profile,
        Res<UOFileManager> files,
        Res<ActiveTextEdit> edit,
        Query<Data<Text, HueValueText>> texts,
        Query<Data<UiCustom, HueSwatch>> swatches)
    {
        foreach (var (ent, t, link) in texts)
        {
            var def = s_options[link.Ref.Index];

            if (edit.Value.Entity != ent.Ref)
            {
                var pv = def.GetI(profile.Value).ToString();
                if (!string.Equals(t.Ref.Value, pv, StringComparison.Ordinal))
                    t.Ref = new Text(pv);
                continue;
            }

            var raw = t.Ref.Value ?? string.Empty;
            Span<char> keep = stackalloc char[Math.Min(raw.Length, 4)];
            int n = 0;
            foreach (var ch in raw)
            {
                if (!char.IsAsciiDigit(ch)) continue;
                keep[n++] = ch;
                if (n == keep.Length) break;
            }
            var sanitized = new string(keep[..n]);
            if (!string.Equals(sanitized, raw, StringComparison.Ordinal))
                t.Ref = new Text(sanitized);

            if (n == 0 || !int.TryParse(sanitized, out var v)) continue;

            v = Math.Clamp(v, def.Min, def.Max);
            if (def.GetI(profile.Value) == v) continue;

            def.SetI(profile.Value, v);
            foreach (var (_, uc, sw) in swatches)
            {
                if (sw.Ref.Index != link.Ref.Index) continue;
                var r = uc.Ref.Render();
                if (r?.GridColors is { Length: > 0 })
                    r.GridColors[0] = BakeHue(files.Value.Hues, (ushort)v);
            }
        }
    }

    // Dropdown for a Cycle option, anchored under the clicked selectbox.
    // Child of the window ROOT (a later sibling of the body, so it paints
    // above the scrolled list and never gets clipped by it).
    private static void OpenSelectOverlay(
        Commands commands, OptionsUiState state, Profile profile, int defIndex, ulong anchorId,
        Query<Data<ComputedNode>> geomQ,
        Query<Data<Node>, Filter<With<OptionsOverlay>>> overlays)
    {
        foreach (var (ent, _) in overlays) commands.Entity(ent.Ref).Despawn();

        var def = s_options[defIndex];
        if (!geomQ.TryGet(anchorId, out var anchorRow) || !geomQ.TryGet(state.Window, out var winRow))
            return;
        var (_, an) = anchorRow;
        var (_, wn) = winRow;

        const int ItemH = 28, PanelW = 150;
        int listH = def.Choices.Length * ItemH + 10;
        float x = an.Ref.Position.X - wn.Ref.Position.X;
        float y = an.Ref.Position.Y - wn.Ref.Position.Y + an.Ref.Size.Y + 4;
        if (y + listH > wn.Ref.Size.Y - 6) y = an.Ref.Position.Y - wn.Ref.Position.Y - listH - 4;

        var panel = commands.Spawn()
            .Insert<OptionsOverlay>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                FlexDirection = FlexDirection.Column,
                Left = Val.Px(x), Top = Val.Px(y),
                Width = Val.Px(PanelW), Height = Val.Px(listH),
                Padding = UiRect.All(5),
                Gap = Val.Px(3),
                Border = UiRect.All(1),
            })
            .Insert(new BackgroundColor(new ClayColor(28, 30, 38, 254)))
            .Insert(new BorderColor(s_panelBorder))
            .Insert(BorderRadius.All(10))
            .Insert(new BoxShadow { Color = s_shadow, OffsetX = 0, OffsetY = 5, BlurRadius = 12, SpreadRadius = 0 });
        commands.AddChild(state.Window, panel.Id);
        var panelId = panel.Id;

        var current = Math.Clamp(def.GetI(profile), 0, def.Choices.Length - 1);
        for (var i = 0; i < def.Choices.Length; i++)
        {
            var idx = i;
            var sel = i == current;
            var item = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(PanelW - 10), Height = Val.Px(ItemH - 2),
                    JustifyContent = JustifyContent.Center,
                    AlignItems = AlignItems.Center,
                })
                .Insert(new BackgroundColor(sel ? s_accent : s_controlBg))
                .Insert(BorderRadius.All(6))
                .Insert(new Text(def.Choices[i]))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Observe((On<UiClick> _, Commands cmd, Res<Profile> p, Res<OptionsUiState> st) =>
                {
                    def.SetI(p.Value, idx);
                    st.Value.Dirty = true;
                    cmd.Entity(panelId).Despawn();
                });
            if (!sel)
                item.Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover });
            commands.AddChild(panelId, item.Id);
        }
    }

    // Close any open dropdown when a press lands outside it. Runs on the
    // press edge only; a press inside the panel leaves it for the item's own
    // UiClick (which fires on release).
    private static void CloseOverlayOnOutsideClick(
        Commands commands,
        Res<MouseContext> mouse,
        Query<Data<ComputedNode>, Filter<With<OptionsOverlay>>> overlays)
    {
        if (!mouse.Value.IsPressedOnce(MouseButtonType.Left)) return;
        var pos = mouse.Value.Position;
        foreach (var (ent, cn) in overlays)
        {
            var bb = cn.Ref;
            bool inside = pos.X >= bb.Position.X && pos.X <= bb.Position.X + bb.Size.X
                       && pos.Y >= bb.Position.Y && pos.Y <= bb.Position.Y + bb.Size.Y;
            if (!inside) commands.Entity(ent.Ref).Despawn();
        }
    }
}

internal struct OptionsWindow;

// Marker on the search field's editable Text entity.
internal struct OptionsSearchText;

// Marker on every rebuilt list element (sidebar buttons + option rows).
internal struct OptionsListItem;

// Marker on any open dropdown / hue-palette overlay (one at a time).
internal struct OptionsOverlay;

#if AGENT_BUILD
internal sealed class DebugOptionsQueue { public bool OpenRequested; }
#endif

// Per-element hover tint. ApplyHover swaps BackgroundColor between Normal and
// Hover based on the element's Clay Interaction state each frame.
internal struct OptionsHover
{
    public ClayColor Normal;
    public ClayColor Hover;
}

// Editable hue value text — Index into the s_options catalog.
internal struct HueValueText { public int Index; }

// Hue swatch picker icon — Index into the s_options catalog.
internal struct HueSwatch { public int Index; }

// Window/session state for the options UI. Entity ids of the static chrome
// containers the rebuild system fills, plus the current filter.
internal sealed class OptionsUiState
{
    public ulong Window;
    public ulong Sidebar;
    public ulong Viewport;
    public string Search = string.Empty;
    public string Category = "General";
    public bool Dirty;
    // Category/search change (scroll back to top) vs value tweak (keep scroll).
    public bool FilterChanged;
}

// Resources an Action row's click handler may need (e.g. the Color Picker
// opener needs asset/hue/layout services).
internal sealed class OptionsButtonParams : CompositeSystemParam
{
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<UOFileManager> Files;
    public readonly Res<UiZCounter> ZCounter;
    public readonly Res<UiSurface> Surface;

    public OptionsButtonParams()
    {
        Assets   = Add(new Res<AssetsServer>());
        Builder  = Add(new Res<GumpBuilder>());
        Files    = Add(new Res<UOFileManager>());
        ZCounter = Add(new Res<UiZCounter>());
        Surface  = Add(new Res<UiSurface>());
    }
}
