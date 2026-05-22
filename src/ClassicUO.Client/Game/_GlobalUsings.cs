// SPDX-License-Identifier: BSD-2-Clause

// Feature folders under Game/<Feature>/ define their own namespaces
// (ClassicUO.Game.Audio, ClassicUO.Game.Chat, etc.) for the manager
// facades and their collaborators. To avoid sprinkling per-feature
// `using` directives across the ~145 files that previously imported
// `ClassicUO.Game.Managers`, the new namespaces are re-exported as
// project-wide globals here.

global using ClassicUO.Game.Audio;
global using ClassicUO.Game.Chat;
global using ClassicUO.Game.Corpses;
global using ClassicUO.Game.Houses;
global using ClassicUO.Game.Messaging;
global using ClassicUO.Game.Opl;
global using ClassicUO.Game.Party;
global using ClassicUO.Game.Targeting;
global using ClassicUO.Game.WorldText;
