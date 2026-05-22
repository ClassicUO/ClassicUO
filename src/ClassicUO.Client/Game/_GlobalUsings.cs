// SPDX-License-Identifier: BSD-2-Clause

// Feature folders under Game/<Feature>/ define their own namespaces. To
// avoid sprinkling per-feature `using` directives across the hundreds of
// files that previously imported `ClassicUO.Game.Managers`, the new
// namespaces are re-exported as project-wide globals here.

global using ClassicUO.Game.Audio;
global using ClassicUO.Game.Boats;
global using ClassicUO.Game.Chat;
global using ClassicUO.Game.Commands;
global using ClassicUO.Game.Containers;
global using ClassicUO.Game.Corpses;
global using ClassicUO.Game.Effects;
global using ClassicUO.Game.Entities.Items;
global using ClassicUO.Game.Entities.Mobiles;
global using ClassicUO.Game.Events;
global using ClassicUO.Game.Houses;
global using ClassicUO.Game.Houses.Customization;
global using ClassicUO.Game.Input;
global using ClassicUO.Game.Input.Hotkeys;
global using ClassicUO.Game.Login;
global using ClassicUO.Game.Macros;
global using ClassicUO.Game.Map;
global using ClassicUO.Game.Messaging;
global using ClassicUO.Game.Messaging.Journal;
global using ClassicUO.Game.Opl;
global using ClassicUO.Game.Party;
global using ClassicUO.Game.Players.Social;
global using ClassicUO.Game.Seasons;
global using ClassicUO.Game.Skills;
global using ClassicUO.Game.Spells;
global using ClassicUO.Game.Targeting;
global using ClassicUO.Game.UI;
global using ClassicUO.Game.UI.Anchoring;
global using ClassicUO.Game.UI.HealthBars;
global using ClassicUO.Game.UI.InfoBar;
global using ClassicUO.Game.UI.Names;
global using ClassicUO.Game.UI.WorldMap;
global using ClassicUO.Game.WorldText;
