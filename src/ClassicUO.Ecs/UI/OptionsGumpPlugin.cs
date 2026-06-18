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
using Microsoft.Xna.Framework.Input;
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

    // Slider widget geometry (flow children — fill + knob + filler — so it rides
    // the scroll clip with the rest of the list, like the toggle pill; the lib
    // SliderPlugin positions its thumb absolutely, which would escape the clip).
    private const int SliderTrackW = 120;
    private const int SliderTrackH = 16;
    private const int SliderKnob = 16;
    private const int SliderValW = 34;

    private static readonly ClayColor s_panelBg     = new(16, 17, 22, 255);
    private static readonly ClayColor s_panelBorder = new(44, 47, 58, 255);
    private static readonly ClayColor s_shadow      = new(0, 0, 0, 120);
    private static readonly ClayColor s_headerBg    = new(22, 24, 30, 255);
    private static readonly ClayColor s_rowBg       = new(30, 32, 40, 255);
    private static readonly ClayColor s_rowHover    = new(38, 41, 51, 255);
    private static readonly ClayColor s_controlBg   = new(52, 55, 66, 255);
    private static readonly ClayColor s_controlHover = new(60, 64, 78, 255);
    private static readonly ClayColor s_accent      = new(92, 132, 224, 255);
    private static readonly ClayColor s_accentHover = new(110, 148, 236, 255);
    private static readonly ClayColor s_sideBg      = new(22, 24, 30, 255);
    private static readonly ClayColor s_sideHover   = new(38, 41, 51, 255);
    private static readonly ClayColor s_toggleOn    = new(74, 178, 112, 255);
    private static readonly ClayColor s_toggleOff   = new(52, 55, 66, 255);
    private static readonly ClayColor s_knob        = new(238, 240, 246, 255);
    private static readonly ClayColor s_field       = new(216, 218, 224, 255);
    private static readonly ClayColor s_textMain    = new(236, 238, 244, 255);
    private static readonly ClayColor s_textDim     = new(120, 124, 140, 255);
    private static readonly ClayColor s_textFaint   = new(120, 124, 140, 255);
    private static readonly ClayColor s_card        = new(24, 26, 33, 255);
    private static readonly ClayColor s_sliderTrack = new(40, 43, 54, 255);

    private enum OptionKind { Toggle, Stepper, Slider, Cycle, Hue, Action }

    private sealed class OptionDef
    {
        public string Category;
        public string Group = string.Empty;
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

    // Catalog builder. Cat() opens a category page; Grp() opens a labelled card
    // within it; the per-kind helpers add a row. Group order = display order,
    // so related settings render together inside one card. Read on build,
    // written on change — mirrors the legacy OptionsGump's profile linkage.
    private static OptionDef[] BuildCatalog()
    {
        var list = new List<OptionDef>();
        string cat = string.Empty, grp = string.Empty;
        void Cat(string c) => cat = c;
        void Grp(string g) => grp = g;
        void AddDef(OptionDef d) { d.Category = cat; d.Group = grp; list.Add(d); }

        // Toggle.
        void T(string label, Func<Profile, bool> g, Action<Profile, bool> s, string kw = "")
            => AddDef(new OptionDef { Label = label, Keywords = kw, Kind = OptionKind.Toggle, GetB = g, SetB = s });
        // Stepper (−/+ for precise values: coords, indices, fonts).
        void I(string label, int min, int max, int step, Func<Profile, int> g, Action<Profile, int> s, string kw = "")
            => AddDef(new OptionDef { Label = label, Keywords = kw, Kind = OptionKind.Stepper, Min = min, Max = max, Step = step, GetI = g, SetI = s });
        // Slider (drag/click for bounded magnitudes: volumes, %, delays).
        void S(string label, int min, int max, int step, Func<Profile, int> g, Action<Profile, int> s, string kw = "")
            => AddDef(new OptionDef { Label = label, Keywords = kw, Kind = OptionKind.Slider, Min = min, Max = max, Step = step, GetI = g, SetI = s });
        // Cycle (selectbox + dropdown overlay).
        void C(string label, string[] choices, Func<Profile, int> g, Action<Profile, int> s, string kw = "")
            => AddDef(new OptionDef { Label = label, Keywords = kw, Kind = OptionKind.Cycle, Choices = choices, GetI = g, SetI = s });
        // Hue (numbers-only editable value + swatch that opens the palette).
        void H(string label, Func<Profile, int> g, Action<Profile, int> s, string kw = "")
            => AddDef(new OptionDef { Label = label, Keywords = kw + " hue color", Kind = OptionKind.Hue, Min = 0, Max = 3000, Step = 1, GetI = g, SetI = s });

        Cat("General");
            Grp("Movement");
                T("Enable pathfinding", p => p.EnablePathfind, (p, v) => p.EnablePathfind = v, "walk auto move");
                T("Shift to pathfind", p => p.UseShiftToPathfind, (p, v) => p.UseShiftToPathfind = v);
                T("Always run", p => p.AlwaysRun, (p, v) => p.AlwaysRun = v, "movement speed");
                T("Always run unless hidden", p => p.AlwaysRunUnlessHidden, (p, v) => p.AlwaysRunUnlessHidden = v);
                T("Fast rotation", p => p.FastRotation, (p, v) => p.FastRotation = v, "turn movement");
                T("Smooth movements", p => p.SmoothMovements, (p, v) => p.SmoothMovements = v, "interpolation step");
            Grp("Doors & Corpses");
                T("Auto open doors", p => p.AutoOpenDoors, (p, v) => p.AutoOpenDoors = v);
                T("Smooth doors", p => p.SmoothDoors, (p, v) => p.SmoothDoors = v);
                T("Auto open corpses", p => p.AutoOpenCorpses, (p, v) => p.AutoOpenCorpses = v, "loot");
                S("Corpse open range", 0, 5, 1, p => p.AutoOpenCorpseRange, (p, v) => p.AutoOpenCorpseRange = v, "loot distance");
                C("Corpse open options", new[] { "Always", "Not targeting", "Not hiding", "Both" }, p => p.CorpseOpenOptions, (p, v) => p.CorpseOpenOptions = v, "loot");
                T("Skip empty corpses", p => p.SkipEmptyCorpse, (p, v) => p.SkipEmptyCorpse = v, "loot");
            Grp("Interaction");
                T("Sallos easy grab", p => p.SallosEasyGrab, (p, v) => p.SallosEasyGrab = v, "loot pickup");
                T("Show house content", p => p.ShowHouseContent, (p, v) => p.ShowHouseContent = v, "public");
                T("Smooth boat movement", p => p.UseSmoothBoatMovement, (p, v) => p.UseSmoothBoatMovement = v, "ship");
                T("Hold shift for context menu", p => p.HoldShiftForContext, (p, v) => p.HoldShiftForContext = v, "popup");
                T("Hold shift to split stacks", p => p.HoldShiftToSplitStack, (p, v) => p.HoldShiftToSplitStack = v, "amount");
                T("Hold tab for combat", p => p.HoldDownKeyTab, (p, v) => p.HoldDownKeyTab = v, "warmode toggle");
                T("Alt closes anchored gumps", p => p.HoldDownKeyAltToCloseAnchored, (p, v) => p.HoldDownKeyAltToCloseAnchored = v);
                T("Alt to move gumps", p => p.HoldAltToMoveGumps, (p, v) => p.HoldAltToMoveGumps = v, "drag window");
                T("Right click closes anchored group", p => p.CloseAllAnchoredGumpsInGroupWithRightClick, (p, v) => p.CloseAllAnchoredGumpsInGroupWithRightClick = v);
                T("Disable top bar", p => p.TopbarGumpIsDisabled, (p, v) => p.TopbarGumpIsDisabled = v, "menu");
                T("Ignore stamina check", p => p.IgnoreStaminaCheck, (p, v) => p.IgnoreStaminaCheck = v, "push");
            Grp("Messages");
                T("Hide screenshot stored message", p => p.HideScreenshotStoredInMessage, (p, v) => p.HideScreenshotStoredInMessage = v);
                T("Show stats changed message", p => p.ShowStatsChangedMessage, (p, v) => p.ShowStatsChangedMessage = v, "str dex int");
                T("Show skills changed message", p => p.ShowSkillsChangedMessage, (p, v) => p.ShowSkillsChangedMessage = v);
                I("Skills changed delta (x0.1)", 0, 100, 1, p => p.ShowSkillsChangedDeltaValue, (p, v) => p.ShowSkillsChangedDeltaValue = v, "message threshold");

        Cat("Mobiles");
            Grp("Health Display");
                T("Show mobiles HP", p => p.ShowMobilesHP, (p, v) => p.ShowMobilesHP = v, "health overhead");
                C("HP display", new[] { "Percentage", "Line", "Both" }, p => p.MobileHPType, (p, v) => p.MobileHPType = v, "health");
                C("HP show when", new[] { "Always", "Less than 100%", "Smart" }, p => p.MobileHPShowWhen, (p, v) => p.MobileHPShowWhen = v, "health");
                T("Poll mobiles HP status (OSI)", p => p.PollMobileStatus, (p, v) => p.PollMobileStatus = v, "health refresh 500ms normalized");
            Grp("Highlighting");
                T("Highlight game objects", p => p.HighlightGameObjects, (p, v) => p.HighlightGameObjects = v, "selection");
                T("Highlight poisoned", p => p.HighlightMobilesByPoisoned, (p, v) => p.HighlightMobilesByPoisoned = v);
                H("Poison hue", p => p.PoisonHue, (p, v) => p.PoisonHue = (ushort)v);
                T("Highlight paralyzed", p => p.HighlightMobilesByParalize, (p, v) => p.HighlightMobilesByParalize = v);
                H("Paralyzed hue", p => p.ParalyzedHue, (p, v) => p.ParalyzedHue = (ushort)v);
                T("Highlight invulnerable", p => p.HighlightMobilesByInvul, (p, v) => p.HighlightMobilesByInvul = v);
                H("Invulnerable hue", p => p.InvulnerableHue, (p, v) => p.InvulnerableHue = (ushort)v);
            Grp("Aura");
                C("Aura under feet", new[] { "Off", "Warmode", "Ctrl+Shift", "Always" }, p => p.AuraUnderFeetType, (p, v) => p.AuraUnderFeetType = v, "circle");
                T("Custom party aura", p => p.PartyAura, (p, v) => p.PartyAura = v);
                H("Party aura hue", p => p.PartyAuraHue, (p, v) => p.PartyAuraHue = (ushort)v);
            Grp("Names");
                T("Incoming mobile names", p => p.ShowNewMobileNameIncoming, (p, v) => p.ShowNewMobileNameIncoming = v);
                T("Incoming corpse names", p => p.ShowNewCorpseNameIncoming, (p, v) => p.ShowNewCorpseNameIncoming = v);
                T("Name overheads toggled", p => p.NameOverheadToggled, (p, v) => p.NameOverheadToggled = v, "nameplate");
                T("Name overhead gump", p => p.NameOverheadShowGump, (p, v) => p.NameOverheadShowGump = v, "nameplate handler");
                T("Name overhead HP bar", p => p.NameOverheadShowHpBar, (p, v) => p.NameOverheadShowHpBar = v, "nameplate health");
                T("Target range indicator", p => p.ShowTargetRangeIndicator, (p, v) => p.ShowTargetRangeIndicator = v);
            Grp("Drag Select");
                T("Drag select health bars", p => p.EnableDragSelect, (p, v) => p.EnableDragSelect = v, "lasso");
                C("Drag select modifier", new[] { "None", "Ctrl", "Shift" }, p => p.DragSelectModifierKey, (p, v) => p.DragSelectModifierKey = v);
                T("Drag select humanoids only", p => p.DragSelectHumanoidsOnly, (p, v) => p.DragSelectHumanoidsOnly = v);
                T("Drag select hostile only", p => p.DragSelectHostileOnly, (p, v) => p.DragSelectHostileOnly = v);
                I("Drag select start X", 0, 2000, 10, p => p.DragSelectStartX, (p, v) => p.DragSelectStartX = v);
                I("Drag select start Y", 0, 2000, 10, p => p.DragSelectStartY, (p, v) => p.DragSelectStartY = v);
                T("Drag select as anchor", p => p.DragSelectAsAnchor, (p, v) => p.DragSelectAsAnchor = v);

        Cat("Video");
            Grp("Window");
                T("Game window full size", p => p.GameWindowFullSize, (p, v) => p.GameWindowFullSize = v, "viewport fullscreen resize");
                T("Lock game window", p => p.GameWindowLock, (p, v) => p.GameWindowLock = v, "viewport move");
                I("Game window X", 0, 2000, 10, p => p.GameWindowPosition.X, (p, v) => p.GameWindowPosition = new Point(v, p.GameWindowPosition.Y), "viewport position");
                I("Game window Y", 0, 2000, 10, p => p.GameWindowPosition.Y, (p, v) => p.GameWindowPosition = new Point(p.GameWindowPosition.X, v), "viewport position");
                I("Game window width", 200, 2048, 20, p => p.GameWindowSize.X, (p, v) => p.GameWindowSize = new Point(v, p.GameWindowSize.Y), "viewport size");
                I("Game window height", 200, 2048, 20, p => p.GameWindowSize.Y, (p, v) => p.GameWindowSize = new Point(p.GameWindowSize.X, v), "viewport size");
                T("Borderless window", p => p.WindowBorderless, (p, v) => p.WindowBorderless = v, "fullscreen");
            Grp("Zoom");
                S("Default zoom (%)", 50, 300, 10, p => (int)(p.DefaultScale * 100f), (p, v) => p.DefaultScale = v / 100f, "scale camera");
                T("Mousewheel zoom", p => p.EnableMousewheelScaleZoom, (p, v) => p.EnableMousewheelScaleZoom = v, "ctrl scale");
                T("Save zoom on close", p => p.SaveScaleAfterClose, (p, v) => p.SaveScaleAfterClose = v, "scale");
                T("Restore zoom after ctrl release", p => p.RestoreScaleAfterUnpressCtrl, (p, v) => p.RestoreScaleAfterUnpressCtrl = v, "scale");
            Grp("Lighting");
                T("Alternative lights", p => p.UseAlternativeLights, (p, v) => p.UseAlternativeLights = v);
                T("Custom light level", p => p.UseCustomLightLevel, (p, v) => p.UseCustomLightLevel = v);
                S("Light level", 0, 30, 1, p => p.LightLevel, (p, v) => p.LightLevel = (byte)v, "brightness");
                C("Light level type", new[] { "Absolute", "Minimum" }, p => p.LightLevelType, (p, v) => p.LightLevelType = v);
                T("Colored lights", p => p.UseColoredLights, (p, v) => p.UseColoredLights = v);
                T("Dark nights", p => p.UseDarkNights, (p, v) => p.UseDarkNights = v);
            Grp("Shadows");
                T("Shadows", p => p.ShadowsEnabled, (p, v) => p.ShadowsEnabled = v);
                T("Shadows on statics", p => p.ShadowsStatics, (p, v) => p.ShadowsStatics = v);
                S("Terrain shadow level", 5, 25, 1, p => p.TerrainShadowsLevel, (p, v) => p.TerrainShadowsLevel = v);
            Grp("Effects");
                T("Object fading", p => p.UseObjectsFading, (p, v) => p.UseObjectsFading = v, "transparency");
                T("Text fading", p => p.TextFading, (p, v) => p.TextFading = v, "overhead");
                T("Death screen", p => p.EnableDeathScreen, (p, v) => p.EnableDeathScreen = v);
                T("Black & white death effect", p => p.EnableBlackWhiteEffect, (p, v) => p.EnableBlackWhiteEffect = v, "grayscale");
                T("Animated water", p => p.AnimatedWaterEffect, (p, v) => p.AnimatedWaterEffect = v);
                T("Aura on mouse target", p => p.AuraOnMouse, (p, v) => p.AuraOnMouse = v);
                T("xBR upscaling", p => p.UseXBR, (p, v) => p.UseXBR = v, "filter shader");
                T("Reduce FPS when inactive", p => p.ReduceFPSWhenInactive, (p, v) => p.ReduceFPSWhenInactive = v, "background framerate");
            Grp("World Filter");
                T("Draw roofs", p => p.DrawRoofs, (p, v) => p.DrawRoofs = v, "hide house");
                T("Trees to stumps", p => p.TreeToStumps, (p, v) => p.TreeToStumps = v, "filter");
                T("Hide vegetation", p => p.HideVegetation, (p, v) => p.HideVegetation = v, "filter grass");
                T("Mark cave tiles", p => p.EnableCaveBorder, (p, v) => p.EnableCaveBorder = v, "border");
                C("Field types", new[] { "Normal", "Static", "Tile" }, p => p.FieldsType, (p, v) => p.FieldsType = v, "fire poison wall");
                T("Circle of transparency", p => p.UseCircleOfTransparency, (p, v) => p.UseCircleOfTransparency = v);
                S("Circle radius", 50, 200, 10, p => p.CircleOfTransparencyRadius, (p, v) => p.CircleOfTransparencyRadius = v, "transparency");
                C("Circle type", new[] { "Full", "Gradient" }, p => p.CircleOfTransparencyType, (p, v) => p.CircleOfTransparencyType = v, "transparency");
                T("No color out of range", p => p.NoColorObjectsOutOfRange, (p, v) => p.NoColorObjectsOutOfRange = v, "gray");

        Cat("Sound");
            Grp("Audio");
                T("Enable sounds", p => p.EnableSound, (p, v) => p.EnableSound = v, "audio effects");
                S("Sound volume", 0, 100, 5, p => p.SoundVolume, (p, v) => p.SoundVolume = v, "audio");
                T("Enable music", p => p.EnableMusic, (p, v) => p.EnableMusic = v, "audio");
                S("Music volume", 0, 100, 5, p => p.MusicVolume, (p, v) => p.MusicVolume = v, "audio");
                T("Footstep sounds", p => p.EnableFootstepsSound, (p, v) => p.EnableFootstepsSound = v, "walk");
                T("Combat music", p => p.EnableCombatMusic, (p, v) => p.EnableCombatMusic = v, "warmode");
                T("Sounds in background", p => p.ReproduceSoundsInBackground, (p, v) => p.ReproduceSoundsInBackground = v, "focus audio");

        Cat("Chat");
            Grp("Speech");
                I("Chat font", 0, 9, 1, p => p.ChatFont, (p, v) => p.ChatFont = (byte)v, "speech");
                S("Speech delay", 0, 1000, 50, p => p.SpeechDelay, (p, v) => p.SpeechDelay = v, "overhead duration");
                T("Scale speech delay", p => p.ScaleSpeechDelay, (p, v) => p.ScaleSpeechDelay = v, "overhead duration");
                T("Activate chat after enter", p => p.ActivateChatAfterEnter, (p, v) => p.ActivateChatAfterEnter = v, "input focus");
                T("Chat additional buttons", p => p.ActivateChatAdditionalButtons, (p, v) => p.ActivateChatAdditionalButtons = v, "modifier");
                T("Shift+Enter sends message", p => p.ActivateChatShiftEnterSupport, (p, v) => p.ActivateChatShiftEnterSupport = v);
                T("Hide chat gradient", p => p.HideChatGradient, (p, v) => p.HideChatGradient = v, "background");
                T("Overhead party messages", p => p.OverheadPartyMessages, (p, v) => p.OverheadPartyMessages = v);
            Grp("Journal");
                T("Save journal to file", p => p.SaveJournalToFile, (p, v) => p.SaveJournalToFile = v, "log");
                T("Force unicode journal", p => p.ForceUnicodeJournal, (p, v) => p.ForceUnicodeJournal = v, "font");
                T("Journal dark mode", p => p.JournalDarkMode, (p, v) => p.JournalDarkMode = v);
                T("Use alternate journal", p => p.UseAlternateJournal, (p, v) => p.UseAlternateJournal = v, "resizable tabs");
                T("Journal: client messages", p => p.ShowJournalClient, (p, v) => p.ShowJournalClient = v, "filter");
                T("Journal: object messages", p => p.ShowJournalObjects, (p, v) => p.ShowJournalObjects = v, "filter");
                T("Journal: system messages", p => p.ShowJournalSystem, (p, v) => p.ShowJournalSystem = v, "filter");
                T("Journal: guild & alliance", p => p.ShowJournalGuildAlly, (p, v) => p.ShowJournalGuildAlly = v, "filter");
            Grp("Fonts & Filters");
                T("Override game font", p => p.OverrideAllFonts, (p, v) => p.OverrideAllFonts = v, "text");
                T("Overridden font is unicode", p => p.OverrideAllFontsIsUnicode, (p, v) => p.OverrideAllFontsIsUnicode = v, "ascii");
                T("Ignore guild messages", p => p.IgnoreGuildMessages, (p, v) => p.IgnoreGuildMessages = v, "mute");
                T("Ignore alliance messages", p => p.IgnoreAllianceMessages, (p, v) => p.IgnoreAllianceMessages = v, "mute");
            Grp("Message Hues");
                H("Speech hue", p => p.SpeechHue, (p, v) => p.SpeechHue = (ushort)v, "say");
                H("Whisper hue", p => p.WhisperHue, (p, v) => p.WhisperHue = (ushort)v);
                H("Emote hue", p => p.EmoteHue, (p, v) => p.EmoteHue = (ushort)v);
                H("Yell hue", p => p.YellHue, (p, v) => p.YellHue = (ushort)v);
                H("Party message hue", p => p.PartyMessageHue, (p, v) => p.PartyMessageHue = (ushort)v);
                H("Guild message hue", p => p.GuildMessageHue, (p, v) => p.GuildMessageHue = (ushort)v);
                H("Alliance message hue", p => p.AllyMessageHue, (p, v) => p.AllyMessageHue = (ushort)v);
                H("Chat message hue", p => p.ChatMessageHue, (p, v) => p.ChatMessageHue = (ushort)v);

        Cat("Combat");
            Grp("Targeting");
                T("New target system", p => p.UseNewTargetSystem, (p, v) => p.UseNewTargetSystem = v);
                T("Query before criminal actions", p => p.EnabledCriminalActionQuery, (p, v) => p.EnabledCriminalActionQuery = v, "confirm attack");
                T("Query before beneficial acts", p => p.EnabledBeneficialCriminalActionQuery, (p, v) => p.EnabledBeneficialCriminalActionQuery = v, "confirm heal criminal");
            Grp("Spells");
                T("Cast spells with one click", p => p.CastSpellsByOneClick, (p, v) => p.CastSpellsByOneClick = v, "spellbook");
                T("Show buff duration", p => p.BuffBarTime, (p, v) => p.BuffBarTime = v, "timer countdown");
                T("Fast spell assign", p => p.FastSpellsAssign, (p, v) => p.FastSpellsAssign = v, "hotkey");
                T("Overhead spell format", p => p.EnabledSpellFormat, (p, v) => p.EnabledSpellFormat = v, "power words");
                T("Overhead spell hue", p => p.EnabledSpellHue, (p, v) => p.EnabledSpellHue = v, "color");
            Grp("Feedback");
                T("DPS with damage numbers", p => p.ShowDPSWithDamageNumbers, (p, v) => p.ShowDPSWithDamageNumbers = v);
                T("Old bandage-self behavior", p => p.BandageSelfOld, (p, v) => p.BandageSelfOld = v, "heal");
                T("Stat change report", p => p.EnableStatReport, (p, v) => p.EnableStatReport = v, "message");
                T("Skill change report", p => p.EnableSkillReport, (p, v) => p.EnableSkillReport = v, "message");
            Grp("Notoriety Hues");
                H("Innocent hue", p => p.InnocentHue, (p, v) => p.InnocentHue = (ushort)v, "notoriety blue");
                H("Friend hue", p => p.FriendHue, (p, v) => p.FriendHue = (ushort)v, "notoriety");
                H("Criminal hue", p => p.CriminalHue, (p, v) => p.CriminalHue = (ushort)v, "notoriety gray");
                H("Can-attack hue", p => p.CanAttackHue, (p, v) => p.CanAttackHue = (ushort)v, "notoriety gray");
                H("Murderer hue", p => p.MurdererHue, (p, v) => p.MurdererHue = (ushort)v, "notoriety red");
                H("Enemy hue", p => p.EnemyHue, (p, v) => p.EnemyHue = (ushort)v, "notoriety orange");
            Grp("Spell Hues");
                H("Beneficial spell hue", p => p.BeneficHue, (p, v) => p.BeneficHue = (ushort)v);
                H("Harmful spell hue", p => p.HarmfulHue, (p, v) => p.HarmfulHue = (ushort)v);
                H("Neutral spell hue", p => p.NeutralHue, (p, v) => p.NeutralHue = (ushort)v);

        Cat("Containers");
            Grp("Appearance");
                C("Backpack style", new[] { "Default", "Suede", "Polar bear", "Ghoul skin" }, p => p.BackpackStyle, (p, v) => p.BackpackStyle = v);
                S("Container scale (%)", 70, 200, 10, p => p.ContainersScale, (p, v) => p.ContainersScale = (byte)v, "size zoom");
                T("Scale items inside", p => p.ScaleItemsInsideContainers, (p, v) => p.ScaleItemsInsideContainers = v, "zoom");
                T("Large container gumps", p => p.UseLargeContainerGumps, (p, v) => p.UseLargeContainerGumps = v);
                T("Hue container gumps", p => p.HueContainerGumps, (p, v) => p.HueContainerGumps = v, "color");
            Grp("Behaviour");
                T("Double click to loot", p => p.DoubleClickToLootInsideContainers, (p, v) => p.DoubleClickToLootInsideContainers = v);
                T("Relative drag and drop", p => p.RelativeDragAndDropItems, (p, v) => p.RelativeDragAndDropItems = v);
                T("Highlight on hover", p => p.HighlightContainerWhenSelected, (p, v) => p.HighlightContainerWhenSelected = v);
                T("Grid containers", p => p.UseGridContainers, (p, v) => p.UseGridContainers = v, "grid view search sort");
                T("Override container location", p => p.OverrideContainerLocation, (p, v) => p.OverrideContainerLocation = v, "position");
                C("Container location mode", new[] { "Near container", "Top right", "Last dragged", "Remember each" }, p => p.OverrideContainerLocationSetting, (p, v) => p.OverrideContainerLocationSetting = v, "position");
                C("Grid loot", new[] { "Disabled", "Grid only", "Grid + classic" }, p => p.GridLootType, (p, v) => p.GridLootType = v, "corpse");

        Cat("Interface");
            Grp("Status & Health");
                T("Use old status gump", p => p.UseOldStatusGump, (p, v) => p.UseOldStatusGump = v, "classic layout");
                T("Status bars mutually exclusive", p => p.StatusGumpBarMutuallyExclusive, (p, v) => p.StatusGumpBarMutuallyExclusive = v);
                T("Custom health bars", p => p.CustomBarsToggled, (p, v) => p.CustomBarsToggled = v, "hp");
                T("Custom bars black background", p => p.CBBlackBGToggled, (p, v) => p.CBBlackBGToggled = v, "hp");
                T("Save health bars on logout", p => p.SaveHealthbars, (p, v) => p.SaveHealthbars = v);
                C("Close health bars", new[] { "Never", "Out of range", "On death" }, p => p.CloseHealthBarType, (p, v) => p.CloseHealthBarType = v, "hp dispose");
                T("Show party invite gump", p => p.PartyInviteGump, (p, v) => p.PartyInviteGump = v, "popup");
            Grp("Skills & Vendors");
                S("Vendor gump height", 30, 120, 10, p => p.VendorGumpHeight, (p, v) => p.VendorGumpHeight = v, "shop size");
                T("Standard skills gump", p => p.StandardSkillsGump, (p, v) => p.StandardSkillsGump = v, "advanced");
            Grp("Tooltips");
                T("Use tooltips", p => p.UseTooltip, (p, v) => p.UseTooltip = v, "item properties");
                S("Tooltip delay (ms)", 0, 1000, 50, p => p.TooltipDelayBeforeDisplay, (p, v) => p.TooltipDelayBeforeDisplay = v);
                S("Tooltip zoom (%)", 100, 200, 10, p => p.TooltipDisplayZoom, (p, v) => p.TooltipDisplayZoom = v);
                S("Tooltip background opacity", 0, 100, 5, p => p.TooltipBackgroundOpacity, (p, v) => p.TooltipBackgroundOpacity = v, "transparent");
                H("Tooltip text hue", p => p.TooltipTextHue, (p, v) => p.TooltipTextHue = (ushort)v);
                I("Tooltip font", 0, 9, 1, p => p.TooltipFont, (p, v) => p.TooltipFont = (byte)v);
            Grp("Info Bar & Counters");
                T("Show info bar", p => p.ShowInfoBar, (p, v) => p.ShowInfoBar = v, "hud");
                C("Info bar highlight", new[] { "Text color", "Bars" }, p => p.InfoBarHighlightType, (p, v) => p.InfoBarHighlightType = v);
                T("Enable counter bar", p => p.CounterBarEnabled, (p, v) => p.CounterBarEnabled = v, "consumables");
                T("Counter: highlight on change", p => p.CounterBarHighlightOnChange, (p, v) => p.CounterBarHighlightOnChange = v);
                T("Counter: highlight when low", p => p.CounterBarHighlightOnAmount, (p, v) => p.CounterBarHighlightOnAmount = v, "red threshold");
                S("Counter: low threshold", 1, 100, 1, p => p.CounterBarHighlightAmount, (p, v) => p.CounterBarHighlightAmount = v);
                T("Counter: abbreviate amounts", p => p.CounterBarDisplayAbbreviatedAmount, (p, v) => p.CounterBarDisplayAbbreviatedAmount = v, "1k");
                I("Counter: abbreviate from", 100, 10000, 100, p => p.CounterBarAbbreviatedAmount, (p, v) => p.CounterBarAbbreviatedAmount = v);
                S("Counter: cell size", 30, 80, 5, p => p.CounterBarCellSize, (p, v) => p.CounterBarCellSize = v);

        Cat("World Map");
            Grp("Window");
                I("Map width", 200, 2000, 50, p => p.WorldMapWidth, (p, v) => p.WorldMapWidth = v, "size");
                I("Map height", 200, 2000, 50, p => p.WorldMapHeight, (p, v) => p.WorldMapHeight = v, "size");
                I("Map font", 1, 6, 1, p => p.WorldMapFont, (p, v) => p.WorldMapFont = v);
                I("Zoom index", 0, 10, 1, p => p.WorldMapZoomIndex, (p, v) => p.WorldMapZoomIndex = v);
                T("Flip map", p => p.WorldMapFlipMap, (p, v) => p.WorldMapFlipMap = v, "rotate 45");
                T("Top most", p => p.WorldMapTopMost, (p, v) => p.WorldMapTopMost = v, "always on top");
                T("Free view", p => p.WorldMapFreeView, (p, v) => p.WorldMapFreeView = v, "pan");
            Grp("Coordinates");
                T("Show coordinates", p => p.WorldMapShowCoordinates, (p, v) => p.WorldMapShowCoordinates = v);
                T("Show mouse coordinates", p => p.WorldMapShowMouseCoordinates, (p, v) => p.WorldMapShowMouseCoordinates = v);
                T("Show sextant coordinates", p => p.WorldMapShowSextantCoordinates, (p, v) => p.WorldMapShowSextantCoordinates = v);
                T("Show grid when zoomed", p => p.WorldMapShowGridIfZoomed, (p, v) => p.WorldMapShowGridIfZoomed = v);
                T("Allow positional targeting", p => p.WorldMapAllowPositionalTarget, (p, v) => p.WorldMapAllowPositionalTarget = v, "click target");
            Grp("Entities");
                T("Show party members", p => p.WorldMapShowParty, (p, v) => p.WorldMapShowParty = v);
                T("Show mobiles", p => p.WorldMapShowMobiles, (p, v) => p.WorldMapShowMobiles = v);
                T("Show player name", p => p.WorldMapShowPlayerName, (p, v) => p.WorldMapShowPlayerName = v);
                T("Show player health bar", p => p.WorldMapShowPlayerBar, (p, v) => p.WorldMapShowPlayerBar = v);
                T("Show group names", p => p.WorldMapShowGroupName, (p, v) => p.WorldMapShowGroupName = v, "party");
                T("Show group health bars", p => p.WorldMapShowGroupBar, (p, v) => p.WorldMapShowGroupBar = v, "party");
                T("Show markers", p => p.WorldMapShowMarkers, (p, v) => p.WorldMapShowMarkers = v, "pins");
                T("Show marker names", p => p.WorldMapShowMarkersNames, (p, v) => p.WorldMapShowMarkersNames = v, "pins labels");
                T("Show multis", p => p.WorldMapShowMultis, (p, v) => p.WorldMapShowMultis = v, "houses boats");

        Cat("Experimental");
            Grp("Keyboard");
                T("Disable default UO hotkeys", p => p.DisableDefaultHotkeys, (p, v) => p.DisableDefaultHotkeys = v, "keyboard");
                T("Disable arrow keys movement", p => p.DisableArrowBtn, (p, v) => p.DisableArrowBtn = v, "keyboard");
                T("Disable tab key", p => p.DisableTabBtn, (p, v) => p.DisableTabBtn = v, "keyboard warmode");
                T("Disable Ctrl+Q/W history", p => p.DisableCtrlQWBtn, (p, v) => p.DisableCtrlQWBtn = v, "keyboard message");
            Grp("Mouse");
                T("Disable click auto-move", p => p.DisableAutoMove, (p, v) => p.DisableAutoMove = v, "mouse");
            Grp("Network");
                T("KR equip/unequip packets", p => p.UseKrEquipUnequipPacket, (p, v) => p.UseKrEquipUnequipPacket = v, "network");

        return list.ToArray();
    }

    private static readonly OptionDef[] s_options = BuildCatalog();
    private static readonly string[] s_categories = DistinctCategories(s_options);

    // Category list in first-seen catalog order (drives the sidebar tabs).
    private static string[] DistinctCategories(OptionDef[] opts)
    {
        var list = new List<string>();
        foreach (var o in opts)
            if (!list.Contains(o.Category)) list.Add(o.Category);
        // Hotkeys has no s_options rows — its page is rendered specially in
        // Rebuild from Profile.Hotkeys.
        list.Add("Hotkeys");
        return list.ToArray();
    }

    public void Build(App app)
    {
        app.AddResource(new OptionsUiState());
        app.AddResource(new OptionsSliderDrag());

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
        app.AddObserver((On<OnRemove<OptionsWindow>> trig, Res<OptionsUiState> state,
            ResMut<HotkeyCapture> cap, Res<Profile> profile) =>
        {
            if (state.Value.Window == trig.EntityId) state.Value.Window = 0;
            // Abort an in-progress hotkey recording (restore the snapshot) so the
            // capture latch doesn't outlive the window and wedge dispatch off.
            if (cap.Value.Index >= 0)
            {
                HotkeyPlugin.RestoreCapture(cap.Value, profile.Value);
                cap.Value.Index = -1;
            }
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

        var sliderFn = UpdateSliderDrag;
        app.AddSystem(sliderFn)
            .InStage(Stage.Update)
            .RunIf((Res<OptionsUiState> s) => s.Value.Window != 0)
            .Build();

        // Hover tint: any element carrying OptionsHover swaps its background
        // between Normal/Hover based on Clay's Interaction state. One system
        // covers every control (rows, sidebar, buttons, dropdown items).
        var hoverFn = ApplyHover;
        app.AddSystem(hoverFn)
            .InStage(Stage.Last)
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
            .Insert(new BoxShadow { Color = s_shadow, OffsetX = 0, OffsetY = 8, BlurRadius = 32, SpreadRadius = 0 })
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
            .Insert(new BackgroundColor(new ClayColor(22, 24, 30, 255)))
            .Insert(BorderRadius.All(3))
            .Insert(new Scrollbar { Target = viewport.Id, Orientation = ScrollbarOrientation.Vertical, MinThumbLength = 28f })
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        var sbThumb = commands.Spawn()
            .Insert(new Node { Width = Val.Px(8), Height = Val.Px(28) })
            .Insert(new BackgroundColor(new ClayColor(52, 55, 66, 255)))
            .Insert(new OptionsHover { Normal = new ClayColor(52, 55, 66, 255), Hover = new ClayColor(72, 76, 92, 255) })
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
        Res<HotkeyCapture> capture,
        Res<AssetsServer> assets,
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

        if (searching)
        {
            // Flat results — each row carries its category and its own pill bg.
            var needle = state.Search.Trim();
            for (var i = 0; i < s_options.Length; i++)
            {
                var def = s_options[i];
                if (!def.Matches(needle)) continue;
                SpawnRow(commands, profile.Value, files.Value.Hues, state, def, i,
                    searching: true, parentId: state.Viewport, rowWidth: RowW, transparent: false);
            }
            // Hotkeys aren't in s_options — match them by name/category/combo too.
            var hk = profile.Value.Hotkeys;
            if (hk != null)
                for (var i = 0; i < hk.Count; i++)
                    if (hk[i] != null && HotkeyMatches(hk[i], needle, assets.Value))
                        SpawnHotkeyRow(commands, profile.Value, capture.Value, assets.Value, i, state.Viewport, RowW, searching: true);
            return;
        }

        // Hotkeys page: not in s_options — rendered from Profile.Hotkeys with a
        // key-capture box + pressed/released select per binding.
        if (state.Category == "Hotkeys")
        {
            BuildHotkeysPage(commands, profile.Value, capture.Value, assets.Value, state);
            return;
        }

        // Category mode: bucket the page's options into labelled cards so
        // related settings read together. The card is tagged OptionsListItem;
        // its rows are untagged and die with it on the next rebuild's cascade.
        string curGroup = null;
        ulong cardId = 0;
        int cardInnerW = RowW - 24;
        for (var i = 0; i < s_options.Length; i++)
        {
            var def = s_options[i];
            if (def.Category != state.Category) continue;
            if (!string.Equals(def.Group, curGroup, StringComparison.Ordinal))
            {
                curGroup = def.Group;
                cardId = SpawnCard(commands, state.Viewport, curGroup);
            }
            SpawnRow(commands, profile.Value, files.Value.Hues, state, def, i,
                searching: false, parentId: cardId, rowWidth: cardInnerW, transparent: true);
        }
    }

    // A labelled card grouping related rows. Child of the viewport (flows with
    // the scroll). Auto height = title + rows; rows are added as children.
    private static ulong SpawnCard(Commands commands, ulong viewport, string title)
    {
        var card = commands.Spawn()
            .Insert<OptionsListItem>()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Val.Px(RowW), Height = Val.Auto,
                Padding = new UiRect { Left = Val.Px(12), Right = Val.Px(12), Top = Val.Px(10), Bottom = Val.Px(10) },
                Gap = Val.Px(3),
            })
            .Insert(new BackgroundColor(s_card))
            .Insert(BorderRadius.All(10));
        var cardId = card.Id;
        commands.AddChild(cardId, commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Val.Px(RowW - 24), Height = Val.Px(18),
            })
            .Insert(new Text(title.ToUpperInvariant()))
            .Insert(new TextFont { FontId = 1, Size = 11 })
            .Insert(new TextColor(s_accent))
            .Id);
        commands.AddChild(viewport, cardId);
        return cardId;
    }

    // Display order of the hotkey category cards on the Hotkeys page. Spells and
    // Skills are user-built lists (an "+ Add" button per card).
    private static readonly string[] s_hotkeyCategories =
        { "Movement", "Combat & Targeting", "Spells", "Skills", "Windows", "Chat", "General" };

    private static string HotkeyCategory(HotkeyAction a) => a switch
    {
        HotkeyAction.WalkNorth or HotkeyAction.WalkSouth or HotkeyAction.WalkEast or HotkeyAction.WalkWest
            or HotkeyAction.ToggleAlwaysRun or HotkeyAction.OpenDoor => "Movement",
        HotkeyAction.ToggleWar or HotkeyAction.TargetSelf or HotkeyAction.LastTarget
            or HotkeyAction.ClearTargetQueue or HotkeyAction.CancelTarget => "Combat & Targeting",
        HotkeyAction.CastSpell => "Spells",
        HotkeyAction.UseSkill => "Skills",
        HotkeyAction.OpenPaperdoll or HotkeyAction.OpenBackpack or HotkeyAction.OpenJournal
            or HotkeyAction.OpenSkills or HotkeyAction.OpenWorldMap or HotkeyAction.OpenMinimap
            or HotkeyAction.OpenStatus or HotkeyAction.OpenBuffs or HotkeyAction.OpenCombatBook
            or HotkeyAction.OpenOptions => "Windows",
        HotkeyAction.ChatHistoryPrev or HotkeyAction.ChatHistoryNext => "Chat",
        _ => "General",
    };

    // The display name shown on a hotkey row (spell/skill rows resolve their Param).
    private static string HotkeyLabel(Hotkey hk, AssetsServer assets) => hk.Action switch
    {
        HotkeyAction.CastSpell => HotkeySpells.NameOf(hk.Param),
        HotkeyAction.UseSkill => SkillName(assets, hk.Param),
        _ => ActionName(hk.Action),
    };

    private static string SkillName(AssetsServer assets, int index)
    {
        var skills = assets.Skills?.Skills;
        return skills != null && index >= 0 && index < skills.Count ? skills[index].Name : "(skill)";
    }

    // Search match for a hotkey row: display name, category, bound combo, or the
    // literal "hotkeys" (so a search for "hotkey" surfaces them all).
    private static bool HotkeyMatches(Hotkey hk, string needle, AssetsServer assets) =>
        HotkeyLabel(hk, assets).Contains(needle, StringComparison.OrdinalIgnoreCase)
        || HotkeyCategory(hk.Action).Contains(needle, StringComparison.OrdinalIgnoreCase)
        || FormatCombo(hk).Contains(needle, StringComparison.OrdinalIgnoreCase)
        || "hotkeys".Contains(needle, StringComparison.OrdinalIgnoreCase);

    // The Hotkeys page: one card per category. Fixed-action cards spawn only when
    // non-empty; the Spells/Skills cards always show (with an "+ Add" button) since
    // they're user-built lists.
    private static void BuildHotkeysPage(
        Commands commands, Profile profile, HotkeyCapture capture, AssetsServer assets, OptionsUiState state)
    {
        var hotkeys = profile.Hotkeys;
        if (hotkeys == null) return;
        int cardInnerW = RowW - 24;
        foreach (var cat in s_hotkeyCategories)
        {
            bool addable = cat == "Spells" || cat == "Skills";
            ulong cardId = 0;
            for (int i = 0; i < hotkeys.Count; i++)
            {
                if (hotkeys[i] == null || HotkeyCategory(hotkeys[i].Action) != cat) continue;
                if (cardId == 0) cardId = SpawnCard(commands, state.Viewport, cat);
                SpawnHotkeyRow(commands, profile, capture, assets, i, cardId, cardInnerW);
            }
            if (addable)
            {
                if (cardId == 0) cardId = SpawnCard(commands, state.Viewport, cat);
                SpawnAddHotkeyButton(commands, cardId,
                    cat == "Spells" ? HotkeyAction.CastSpell : HotkeyAction.UseSkill);
            }
        }
    }

    // "+ Add Spell" / "+ Add Skill" button at the bottom of those cards. Appends a
    // new hotkey of that kind (defaulting to the first available entry).
    private static void SpawnAddHotkeyButton(Commands commands, ulong cardId, HotkeyAction action)
    {
        var btn = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                Width = Val.Px(140), Height = Val.Px(26),
                JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
            })
            .Insert(new BackgroundColor(s_accent))
            .Insert(new OptionsHover { Normal = s_accent, Hover = s_accentHover })
            .Insert(BorderRadius.All(7))
            .Insert(new Text(action == HotkeyAction.CastSpell ? "+ Add Spell" : "+ Add Skill"))
            .Insert(new TextFont { FontId = 1, Size = 12 })
            .Insert(new TextColor(s_textMain))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        var act = action;
        btn.Observe((On<UiClick> _, ResMut<HotkeyCapture> cap, Res<Profile> p, Res<OptionsUiState> st, Res<AssetsServer> a) =>
        {
            if (cap.Value.Index >= 0) return; // not while recording
            int param = act == HotkeyAction.CastSpell
                ? (HotkeySpells.All.Length > 0 ? HotkeySpells.All[0].CastId : 0)
                : FirstUsableSkill(a.Value);
            p.Value.Hotkeys.Add(new Hotkey { Action = act, Param = param });
            st.Value.Dirty = true;
        });
        commands.AddChild(cardId, btn.Id);
    }

    private static int FirstUsableSkill(AssetsServer assets)
    {
        var skills = assets.Skills?.Skills;
        if (skills != null)
            foreach (var s in skills)
                if (s.HasAction) return s.Index;
        return 0;
    }

    // Scrollable spell picker (grouped by book). Sets the hotkey's Param to the
    // chosen server cast id.
    private static void OpenSpellPicker(
        Commands commands, OptionsUiState state, Profile profile, int hkIndex, ulong anchorId,
        Query<Data<ComputedNode>> geomQ,
        Query<Data<Node>, Filter<With<OptionsOverlay>>> overlays)
    {
        foreach (var (ent, _) in overlays) commands.Entity(ent.Ref).Despawn();
        if (hkIndex < 0 || hkIndex >= profile.Hotkeys.Count) return;
        if (!geomQ.TryGet(state.Window, out var winRow)) return;
        var (_, wn) = winRow;

        const int PanelW = 210, ItemH = 22, HeaderH = 20;
        int contentH = 8 * (HeaderH + 2) + HotkeySpells.All.Length * (ItemH + 2) + 12;
        ulong panelId = SpawnPickerPanel(commands, state,
            wn.Ref.Position.X, wn.Ref.Position.Y, wn.Ref.Size.X, wn.Ref.Size.Y, anchorId, geomQ, PanelW, contentH);

        var curBook = (SpellBookType)0xFF;
        foreach (var e in HotkeySpells.All)
        {
            if (e.Book != curBook)
            {
                curBook = e.Book;
                commands.AddChild(panelId, commands.Spawn()
                    .Insert(new Node { Display = Display.Flex, AlignItems = AlignItems.Center, Width = Val.Px(PanelW - 14), Height = Val.Px(HeaderH) })
                    .Insert(new Text(HotkeySpells.BookName(curBook).ToUpperInvariant()))
                    .Insert(new TextFont { FontId = 1, Size = 11 })
                    .Insert(new TextColor(s_accent))
                    .Id);
            }
            var castId = e.CastId;
            var idx = hkIndex;
            var item = commands.Spawn()
                .Insert(new Node { Display = Display.Flex, Width = Val.Px(PanelW - 14), Height = Val.Px(ItemH), JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center })
                .Insert(new BackgroundColor(s_controlBg))
                .Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover })
                .Insert(BorderRadius.All(5))
                .Insert(new Text(e.Name))
                .Insert(new TextFont { FontId = 1, Size = 11 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            item.Observe((On<UiClick> _, Commands cmd, Res<Profile> p, Res<OptionsUiState> st) =>
            {
                if (idx >= 0 && idx < p.Value.Hotkeys.Count) p.Value.Hotkeys[idx].Param = castId;
                st.Value.Dirty = true;
                cmd.Entity(panelId).Despawn();
            });
            commands.AddChild(panelId, item.Id);
        }
    }

    // Scrollable usable-skill picker. Sets the hotkey's Param to the skill index.
    private static void OpenSkillPicker(
        Commands commands, OptionsUiState state, Profile profile, AssetsServer assets, int hkIndex, ulong anchorId,
        Query<Data<ComputedNode>> geomQ,
        Query<Data<Node>, Filter<With<OptionsOverlay>>> overlays)
    {
        foreach (var (ent, _) in overlays) commands.Entity(ent.Ref).Despawn();
        if (hkIndex < 0 || hkIndex >= profile.Hotkeys.Count) return;
        if (!geomQ.TryGet(state.Window, out var winRow)) return;
        var (_, wn) = winRow;
        var skills = assets.Skills?.Skills;
        if (skills == null) return;

        const int PanelW = 210, ItemH = 22;
        int usable = 0;
        foreach (var s in skills) if (s.HasAction) usable++;
        int contentH = usable * (ItemH + 2) + 12;
        ulong panelId = SpawnPickerPanel(commands, state,
            wn.Ref.Position.X, wn.Ref.Position.Y, wn.Ref.Size.X, wn.Ref.Size.Y, anchorId, geomQ, PanelW, contentH);

        foreach (var s in skills)
        {
            if (!s.HasAction) continue;
            var useId = s.Index;
            var idx = hkIndex;
            var item = commands.Spawn()
                .Insert(new Node { Display = Display.Flex, Width = Val.Px(PanelW - 14), Height = Val.Px(ItemH), JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center })
                .Insert(new BackgroundColor(s_controlBg))
                .Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover })
                .Insert(BorderRadius.All(5))
                .Insert(new Text(s.Name))
                .Insert(new TextFont { FontId = 1, Size = 11 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            item.Observe((On<UiClick> _, Commands cmd, Res<Profile> p, Res<OptionsUiState> st) =>
            {
                if (idx >= 0 && idx < p.Value.Hotkeys.Count) p.Value.Hotkeys[idx].Param = useId;
                st.Value.Dirty = true;
                cmd.Entity(panelId).Despawn();
            });
            commands.AddChild(panelId, item.Id);
        }
    }

    // Shared scrollable overlay panel for the spell/skill pickers: anchored under
    // the button, clamped inside the window, scrolls via the wheel (RouteWheel).
    private static ulong SpawnPickerPanel(
        Commands commands, OptionsUiState state,
        float winX, float winY, float winW, float winH,
        ulong anchorId, Query<Data<ComputedNode>> geomQ, int panelW, int contentH)
    {
        int maxH = Math.Max(120, (int)winH - 40);
        int panelH = Math.Min(maxH, contentH);

        float x = 6, y = 30;
        if (geomQ.TryGet(anchorId, out var anchorRow))
        {
            var (_, an) = anchorRow;
            x = an.Ref.Position.X - winX;
            y = an.Ref.Position.Y - winY + an.Ref.Size.Y + 4;
        }
        if (y + panelH > winH - 6) y = Math.Max(6, winH - 6 - panelH);
        if (x + panelW > winW - 6) x = Math.Max(6, winW - 6 - panelW);

        var panel = commands.Spawn()
            .Insert<OptionsOverlay>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                FlexDirection = FlexDirection.Column,
                Left = Val.Px(x), Top = Val.Px(y),
                Width = Val.Px(panelW), Height = Val.Px(panelH),
                Padding = UiRect.All(5),
                Gap = Val.Px(2),
                Border = UiRect.All(1),
                Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition())
            .Insert(new BackgroundColor(new ClayColor(20, 21, 27, 254)))
            .Insert(new BorderColor(s_panelBorder))
            .Insert(BorderRadius.All(10))
            .Insert(new BoxShadow { Color = s_shadow, OffsetX = 0, OffsetY = 8, BlurRadius = 32, SpreadRadius = 0 });
        commands.AddChild(state.Window, panel.Id);
        return panel.Id;
    }

    // One hotkey row. Idle: [label] [capture box] [pressed/released select].
    // Recording: [label] [live combo box] [OK] [Cancel] — recording stays open
    // until OK commits (with the duplicate check) or Cancel restores the snapshot.
    private static void SpawnHotkeyRow(
        Commands commands, Profile profile, HotkeyCapture capture, AssetsServer assets,
        int hkIndex, ulong parentId, int rowWidth, bool searching = false)
    {
        const int LabelWHk = 150, GapW = 6, TriggerW = 92, OkW = 42, BtnGap = 4, CancelW = 60, RemoveW = 28;

        var hk = profile.Hotkeys[hkIndex];
        // Spell/skill rows: the label is a clickable picker button, and the
        // trailing control is a remove (×) instead of pressed/released.
        bool isMacro = hk.Action == HotkeyAction.CastSpell || hk.Action == HotkeyAction.UseSkill;
        bool recording = capture.Index == hkIndex;
        int captureW = recording ? 116 : 132;
        int controlsW = recording ? (OkW + BtnGap + CancelW) : (isMacro ? RemoveW : TriggerW);
        int contentW = rowWidth - 20;
        int controlsTotal = captureW + GapW + controlsW;
        // Search rows + macro rows give the label all the room left of the
        // controls; fixed category rows use the fixed label column.
        int labelW = (searching || isMacro) ? Math.Max(LabelWHk, contentW - controlsTotal) : LabelWHk;

        // Search rows are flat pills tagged for the rebuild sweep; card rows are
        // transparent and die with their card's cascade.
        var rowBg = searching ? s_rowBg : new ClayColor(0, 0, 0, 0);
        var row = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Val.Px(rowWidth), Height = Val.Px(RowH),
                Padding = new UiRect { Left = Val.Px(14), Right = Val.Px(12) },
            })
            .Insert(new BackgroundColor(rowBg))
            .Insert(new OptionsHover { Normal = rowBg, Hover = s_rowHover })
            .Insert(BorderRadius.All(8))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        if (searching)
            row.Insert<OptionsListItem>();
        commands.AddChild(parentId, row.Id);
        var rowId = row.Id;

        if (isMacro)
        {
            // Clickable name button — opens the spell / skill picker.
            var name = HotkeyLabel(hk, assets);
            var nameBtn = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(labelW), Height = Val.Px(26),
                    JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
                    Padding = new UiRect { Left = Val.Px(8), Right = Val.Px(8) },
                })
                .Insert(new BackgroundColor(s_controlBg))
                .Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover })
                .Insert(BorderRadius.All(7))
                .Insert(new Text(searching ? $"Hotkeys  ·  {name}" : name))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            var nameId = nameBtn.Id;
            var pickIndex = hkIndex;
            var pickAction = hk.Action;
            nameBtn.Observe((
                On<UiClick> _,
                Commands cmd,
                Res<Profile> p,
                Res<OptionsUiState> st,
                Res<AssetsServer> a,
                Query<Data<ComputedNode>> geomQ,
                Query<Data<Node>, Filter<With<OptionsOverlay>>> overlays) =>
            {
                if (pickAction == HotkeyAction.CastSpell)
                    OpenSpellPicker(cmd, st.Value, p.Value, pickIndex, nameId, geomQ, overlays);
                else
                    OpenSkillPicker(cmd, st.Value, p.Value, a.Value, pickIndex, nameId, geomQ, overlays);
            });
            commands.AddChild(rowId, nameId);
        }
        else
        {
            var labelText = searching ? $"Hotkeys  ·  {ActionName(hk.Action)}" : ActionName(hk.Action);
            commands.AddChild(rowId, commands.Spawn()
                .Insert(new Node { Width = Val.Px(labelW), Height = Val.Auto })
                .Insert(new Text(labelText))
                .Insert(new TextFont { FontId = 1, Size = 13 })
                .Insert(new TextColor(s_textMain))
                .Id);
        }

        AddSpacer(commands, rowId, contentW - labelW - controlsTotal);

        // Capture box. Idle: click arms recording. Recording: shows the live
        // combo (or "press a key…" before the first press).
        var capLabel = recording
            ? (capture.HasPending ? FormatCombo(hk) : "press a key…")
            : FormatCombo(hk);
        var capBox = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                Width = Val.Px(captureW), Height = Val.Px(26),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            })
            .Insert(new BackgroundColor(recording ? s_accent : s_controlBg))
            .Insert(BorderRadius.All(7))
            .Insert(new Text(capLabel))
            .Insert(new TextFont { FontId = 1, Size = 12 })
            .Insert(new TextColor(s_textMain))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        if (!recording)
            capBox.Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover });
        var armIndex = hkIndex;
        capBox.Observe((On<UiClick> _, ResMut<HotkeyCapture> cap, Res<Profile> p, Res<OptionsUiState> st) =>
        {
            if (cap.Value.Index >= 0) return; // already recording one — finish it first
            HotkeyPlugin.BeginCapture(cap.Value, p.Value, armIndex);
            st.Value.Dirty = true;
        });
        commands.AddChild(rowId, capBox.Id);

        AddSpacer(commands, rowId, GapW);

        if (recording)
        {
            var ok = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(OkW), Height = Val.Px(26),
                    JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
                })
                .Insert(new BackgroundColor(s_accent))
                .Insert(new OptionsHover { Normal = s_accent, Hover = s_accentHover })
                .Insert(BorderRadius.All(7))
                .Insert(new Text("OK"))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            ok.Observe((
                On<UiClick> _,
                Commands cmd,
                ResMut<HotkeyCapture> cap,
                Res<Profile> p,
                Res<OptionsUiState> st,
                Res<GumpBuilder> b,
                Res<AssetsServer> a,
                Res<UiSurface> sf,
                Res<UiZCounter> z) =>
            {
                int i = cap.Value.Index;
                if (i < 0) return;
                if (HotkeyPlugin.IsDuplicate(p.Value.Hotkeys, i))
                {
                    MessageBoxGumpPlugin.Open(cmd, b.Value, a.Value, sf.Value, z.Value,
                        280, 140, "This key combination is already assigned to another hotkey.",
                        MessageButtonType.OK);
                    return; // keep recording — pick another combo or Cancel
                }
                cap.Value.Index = -1; // commit (the live binding stays)
                st.Value.Dirty = true;
            });
            commands.AddChild(rowId, ok.Id);

            AddSpacer(commands, rowId, BtnGap);

            var cancel = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(CancelW), Height = Val.Px(26),
                    JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
                })
                .Insert(new BackgroundColor(s_controlBg))
                .Insert(new OptionsHover { Normal = s_controlBg, Hover = new ClayColor(196, 72, 72, 255) })
                .Insert(BorderRadius.All(7))
                .Insert(new Text("Cancel"))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            cancel.Observe((On<UiClick> _, ResMut<HotkeyCapture> cap, Res<Profile> p, Res<OptionsUiState> st) =>
            {
                if (cap.Value.Index >= 0)
                    HotkeyPlugin.RestoreCapture(cap.Value, p.Value);
                cap.Value.Index = -1;
                st.Value.Dirty = true;
            });
            commands.AddChild(rowId, cancel.Id);
        }
        else if (isMacro)
        {
            // Remove this spell/skill hotkey.
            var rm = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(RemoveW), Height = Val.Px(26),
                    JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
                })
                .Insert(new BackgroundColor(s_controlBg))
                .Insert(new OptionsHover { Normal = s_controlBg, Hover = new ClayColor(196, 72, 72, 255) })
                .Insert(BorderRadius.All(7))
                .Insert(new Text("X"))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            var rmIndex = hkIndex;
            rm.Observe((On<UiClick> _, ResMut<HotkeyCapture> cap, Res<Profile> p, Res<OptionsUiState> st) =>
            {
                if (cap.Value.Index >= 0) return; // not while recording
                if (rmIndex >= 0 && rmIndex < p.Value.Hotkeys.Count)
                    p.Value.Hotkeys.RemoveAt(rmIndex);
                st.Value.Dirty = true;
            });
            commands.AddChild(rowId, rm.Id);
        }
        else
        {
            // Pressed/released select (dropdown overlay, like the Cycle widget).
            var trig = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(TriggerW), Height = Val.Px(26),
                    JustifyContent = JustifyContent.Center,
                    AlignItems = AlignItems.Center,
                })
                .Insert(new BackgroundColor(s_controlBg))
                .Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover })
                .Insert(BorderRadius.All(7))
                .Insert(new Text((hk.OnRelease ? "Released" : "Pressed") + "  v"))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            var trigId = trig.Id;
            var selIndex = hkIndex;
            trig.Observe((
                On<UiClick> _,
                Commands cmd,
                Res<Profile> p,
                Res<OptionsUiState> st,
                Query<Data<ComputedNode>> geomQ,
                Query<Data<Node>, Filter<With<OptionsOverlay>>> overlays) =>
                    OpenHotkeyTriggerOverlay(cmd, st.Value, p.Value, selIndex, trigId, geomQ, overlays));
            commands.AddChild(rowId, trigId);
        }
    }

    private static string ActionName(HotkeyAction a) => a switch
    {
        HotkeyAction.ToggleWar => "Toggle War",
        HotkeyAction.WalkNorth => "Walk North",
        HotkeyAction.WalkSouth => "Walk South",
        HotkeyAction.WalkEast => "Walk East",
        HotkeyAction.WalkWest => "Walk West",
        HotkeyAction.ChatHistoryPrev => "Chat History Prev",
        HotkeyAction.ChatHistoryNext => "Chat History Next",
        HotkeyAction.AllNames => "All Names",
        HotkeyAction.TargetSelf => "Target Self",
        HotkeyAction.LastTarget => "Last Target",
        HotkeyAction.UseLastObject => "Use Last Object",
        HotkeyAction.ToggleAlwaysRun => "Toggle Always Run",
        HotkeyAction.OpenDoor => "Open Door",
        HotkeyAction.OpenBackpack => "Open Backpack",
        HotkeyAction.OpenPaperdoll => "Open Paperdoll",
        HotkeyAction.OpenJournal => "Open Journal",
        HotkeyAction.OpenWorldMap => "Open World Map",
        HotkeyAction.OpenMinimap => "Open Minimap",
        HotkeyAction.OpenSkills => "Open Skills",
        HotkeyAction.OpenStatus => "Open Status",
        HotkeyAction.OpenBuffs => "Open Buffs",
        HotkeyAction.OpenCombatBook => "Open Combat Book",
        HotkeyAction.OpenOptions => "Open Options",
        HotkeyAction.Logout => "Logout",
        HotkeyAction.CancelTarget => "Cancel Target",
        HotkeyAction.ClearTargetQueue => "Clear Target Queue",
        HotkeyAction.CastSpell => "Cast Spell",
        HotkeyAction.UseSkill => "Use Skill",
        _ => a.ToString(),
    };

    private static string FormatCombo(Hotkey hk)
    {
        if (hk.Key == 0 && hk.Mouse == 0 && hk.Wheel == 0) return "(unbound)";
        var s = string.Empty;
        if (hk.Ctrl) s += "Ctrl + ";
        if (hk.Shift) s += "Shift + ";
        if (hk.Alt) s += "Alt + ";
        if (hk.Key != 0) s += ((Keys)hk.Key).ToString();
        else if (hk.Wheel > 0) s += "Wheel Up";
        else if (hk.Wheel < 0) s += "Wheel Down";
        else s += "Mouse: " + ((MouseButtonType)hk.Mouse);
        return s;
    }

    // Pressed/released dropdown for a hotkey, reusing the Cycle overlay style.
    private static void OpenHotkeyTriggerOverlay(
        Commands commands, OptionsUiState state, Profile profile, int hkIndex, ulong anchorId,
        Query<Data<ComputedNode>> geomQ,
        Query<Data<Node>, Filter<With<OptionsOverlay>>> overlays)
    {
        foreach (var (ent, _) in overlays) commands.Entity(ent.Ref).Despawn();
        if (hkIndex < 0 || hkIndex >= profile.Hotkeys.Count) return;
        if (!geomQ.TryGet(anchorId, out var anchorRow) || !geomQ.TryGet(state.Window, out var winRow))
            return;
        var (_, an) = anchorRow;
        var (_, wn) = winRow;

        string[] choices = { "Pressed", "Released" };
        const int ItemH = 28, PanelW = 110;
        int listH = choices.Length * ItemH + 10;
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
            .Insert(new BackgroundColor(new ClayColor(20, 21, 27, 254)))
            .Insert(new BorderColor(s_panelBorder))
            .Insert(BorderRadius.All(10))
            .Insert(new BoxShadow { Color = s_shadow, OffsetX = 0, OffsetY = 8, BlurRadius = 32, SpreadRadius = 0 });
        commands.AddChild(state.Window, panel.Id);
        var panelId = panel.Id;

        int current = profile.Hotkeys[hkIndex].OnRelease ? 1 : 0;
        for (var i = 0; i < choices.Length; i++)
        {
            var idx = hkIndex;
            var release = i == 1;
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
                .Insert(new Text(choices[i]))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Observe((On<UiClick> _, Commands cmd, Res<Profile> p, Res<OptionsUiState> st) =>
                {
                    if (idx >= 0 && idx < p.Value.Hotkeys.Count)
                        p.Value.Hotkeys[idx].OnRelease = release;
                    st.Value.Dirty = true;
                    cmd.Entity(panelId).Despawn();
                });
            if (!sel)
                item.Insert(new OptionsHover { Normal = s_controlBg, Hover = s_controlHover });
            commands.AddChild(panelId, item.Id);
        }
    }

    private static void SpawnRow(
        Commands commands, Profile profile, HuesLoader hues, OptionsUiState state,
        OptionDef def, int defIndex, bool searching, ulong parentId, int rowWidth, bool transparent)
    {
        // Card rows are transparent (the card paints the surface) and despawn
        // via the card's cascade; search rows are flat pills tagged for the
        // rebuild sweep.
        var rowBg = transparent ? new ClayColor(0, 0, 0, 0) : s_rowBg;
        var row = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Val.Px(rowWidth), Height = Val.Px(RowH),
                Padding = new UiRect { Left = Val.Px(14), Right = Val.Px(12) },
            })
            .Insert(new BackgroundColor(rowBg))
            .Insert(new OptionsHover { Normal = rowBg, Hover = s_rowHover })
            .Insert(BorderRadius.All(8))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        if (searching)
            row.Insert<OptionsListItem>();
        commands.AddChild(parentId, row.Id);
        var rowId = row.Id;

        // While searching the row carries its category so results from other
        // pages stay readable.
        var labelText = searching ? $"{def.Category}  ·  {def.Label}" : def.Label;
        int contentW = rowWidth - 20;
        int labelW = rowWidth - 186;
        commands.AddChild(rowId, commands.Spawn()
            .Insert(new Node { Width = Val.Px(labelW), Height = Val.Auto })
            .Insert(new Text(labelText))
            .Insert(new TextFont { FontId = 1, Size = 13 })
            .Insert(new TextColor(s_textMain))
            .Id);

        switch (def.Kind)
        {
            case OptionKind.Toggle:
            {
                const int TrackW = 46, TrackH = 24, Knob = 18;
                AddSpacer(commands, rowId, contentW - labelW - TrackW);
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
                AddSpacer(commands, rowId, contentW - labelW - (StepW + ValW + StepW + 8));
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
                    .Insert(new BackgroundColor(new ClayColor(20, 21, 27, 255)))
                    .Insert(BorderRadius.All(6))
                    .Insert(new Text(def.GetI(profile).ToString()))
                    .Insert(new TextFont { FontId = 1, Size = 13 })
                    .Insert(new TextColor(s_textMain))
                    .Id);
                AddSpacer(commands, rowId, 4);
                SpawnStepButton(commands, rowId, "+", () => def, +1);
                break;
            }

            case OptionKind.Slider:
            {
                // Flow widget: [fill][knob][filler] inside a rounded track, plus
                // a value readout. No absolute children, so it stays inside the
                // scroll clip. The drag/click handling lives in UpdateSliderDrag
                // (keyed by defIndex, which the OptionsSlider marker carries).
                int sval = Math.Clamp(def.GetI(profile), def.Min, def.Max);
                float ratio = def.Max > def.Min ? (sval - def.Min) / (float)(def.Max - def.Min) : 0f;
                int fillPx = (int)MathF.Round(ratio * (SliderTrackW - SliderKnob));
                int restPx = Math.Max(0, SliderTrackW - SliderKnob - fillPx);

                AddSpacer(commands, rowId, contentW - labelW - (SliderTrackW + 8 + SliderValW));

                var track = commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        FlexDirection = FlexDirection.Row,
                        AlignItems = AlignItems.Center,
                        Width = Val.Px(SliderTrackW), Height = Val.Px(SliderTrackH),
                    })
                    .Insert(new BackgroundColor(s_sliderTrack))
                    .Insert(BorderRadius.All(SliderTrackH / 2))
                    .Insert(new OptionsSlider { Index = defIndex })
                    .Insert<UiNoWindowDrag>();
                var trackId = track.Id;
                if (fillPx > 0)
                    commands.AddChild(trackId, commands.Spawn()
                        .Insert(new Node { Width = Val.Px(fillPx), Height = Val.Px(SliderTrackH) })
                        .Insert(new BackgroundColor(s_accent))
                        .Insert(BorderRadius.All(SliderTrackH / 2))
                        .Id);
                commands.AddChild(trackId, commands.Spawn()
                    .Insert(new Node { Width = Val.Px(SliderKnob), Height = Val.Px(SliderKnob) })
                    .Insert(new BackgroundColor(s_knob))
                    .Insert(BorderRadius.All(SliderKnob / 2))
                    .Id);
                AddSpacer(commands, trackId, restPx);
                commands.AddChild(rowId, trackId);

                AddSpacer(commands, rowId, 8);

                commands.AddChild(rowId, commands.Spawn()
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        Width = Val.Px(SliderValW), Height = Val.Px(20),
                        JustifyContent = JustifyContent.End,
                        AlignItems = AlignItems.Center,
                    })
                    .Insert(new Text(sval.ToString()))
                    .Insert(new TextFont { FontId = 1, Size = 13 })
                    .Insert(new TextColor(s_textDim))
                    .Id);
                break;
            }

            case OptionKind.Cycle:
            {
                AddSpacer(commands, rowId, contentW - labelW - 140);
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
                AddSpacer(commands, rowId, contentW - labelW - (60 + 8 + 28));

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
                AddSpacer(commands, rowId, contentW - labelW - 80);
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
            .Insert(new BackgroundColor(new ClayColor(20, 21, 27, 254)))
            .Insert(new BorderColor(s_panelBorder))
            .Insert(BorderRadius.All(10))
            .Insert(new BoxShadow { Color = s_shadow, OffsetX = 0, OffsetY = 8, BlurRadius = 32, SpreadRadius = 0 });
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

    // Continuous-held slider drag + click-to-set. Latch is keyed by catalog
    // Index (NOT entity id) so it survives the per-tweak rebuild that despawns
    // and respawns the track. Reads the raw mouse against the track's
    // ComputedNode bounds — the same gesture style as window drag, which works
    // because the gump children are deliberately non-interactive.
    private static void UpdateSliderDrag(
        Res<MouseContext> mouse,
        Res<Profile> profile,
        Res<OptionsUiState> stateRes,
        ResMut<OptionsSliderDrag> dragRes,
        Query<Data<ComputedNode, OptionsSlider>> sliders)
    {
        bool down = mouse.Value.IsPressed(MouseButtonType.Left)
                 || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!down)
        {
            dragRes.Value.ActiveIndex = -1;
            return;
        }

        var pos = mouse.Value.Position;

        // Latch the slider under the cursor on the press edge.
        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            dragRes.Value.ActiveIndex = -1;
            foreach (var (_, cn, sl) in sliders)
            {
                var bb = cn.Ref;
                // Thin track, tall row: pad the grab band vertically.
                if (pos.X >= bb.Position.X && pos.X <= bb.Position.X + bb.Size.X
                    && pos.Y >= bb.Position.Y - 10 && pos.Y <= bb.Position.Y + bb.Size.Y + 10)
                {
                    dragRes.Value.ActiveIndex = sl.Ref.Index;
                    break;
                }
            }
        }

        int active = dragRes.Value.ActiveIndex;
        if (active < 0) return;

        foreach (var (_, cn, sl) in sliders)
        {
            if (sl.Ref.Index != active) continue;
            var def = s_options[active];
            var bb = cn.Ref;
            float usable = MathF.Max(1f, bb.Size.X - SliderKnob);
            float ratio = Math.Clamp((pos.X - bb.Position.X - SliderKnob * 0.5f) / usable, 0f, 1f);
            int span = def.Max - def.Min;
            int step = Math.Max(1, def.Step);
            int snapped = def.Min + (int)MathF.Round(ratio * span / step) * step;
            int v = Math.Clamp(snapped, def.Min, def.Max);
            if (def.GetI(profile.Value) != v)
            {
                def.SetI(profile.Value, v);
                stateRes.Value.Dirty = true;
            }
            break;
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

// Slider track marker — Index into the s_options catalog. UpdateSliderDrag maps
// cursor X over the track's ComputedNode bounds to the option's value.
internal struct OptionsSlider { public int Index; }

// Active slider-drag latch, keyed by catalog Index (NOT entity id) so it
// survives the per-tweak rebuild that despawns + respawns the slider track.
internal sealed class OptionsSliderDrag { public int ActiveIndex = -1; }

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
