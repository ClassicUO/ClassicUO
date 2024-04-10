using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class VersionHistory : Gump
    {
        private static string[] updateTexts = {
            "/c[white][3.23.0]/cd\n" +
                "- Nameplate healthbar poison and invul/paralyzed colors from Elderwyn\n" +
                "- Target indiciator option from original client from Elderwyn\n" +
                "- Advanced skill gump improvements from Elderwyn",

            "/c[white][3.22.0]/cd\n" +
                "- Spell book icon fix\n" +
                "- Add option to add treasure maps as map markers instead of goto only\n" +
                "- Added the same option for SOS messages\n" +
                "- Fix text height for nameplates\n" +
                "- Added option to disable auto follow",

            "/c[white][3.21.4]/cd\n" +
                "- Various bug fixes\n" +
                "- Removed gump closing animation. Too many unforeseen issues with it.",

            "/c[white][3.21.3]/cd\n" +
            "- Changes to improve gump closing animations",

            "/c[white][3.21.2]/cd\n" +
                "- A bugfix release for 3.21 causing crashes",

            "/c[white][3.21.0]/cd\n" +
                "- A few bug fixes\n" +
                "- A few fixes from CUO\n" +
                "- Converted nameplates to use TTF fonts\n" +
                "- Added an available client commands gump\n" +
                "- World map alt lock now works, and middle mouse click will toggle freeview",

            "/c[white][3.20.0]/cd\n" +
                "- Being frozen wont cancel auto follow\n" +
                "- Fix from CUO for buffs\n" +
                "- Add ability to load custom spell definitions from an external file\n" +
                "- Customize the options gump via ui file\n" +
                "- Added saveposition tag for xml gumps\n" +
                "- Can now open multiple journals\n",

            "/c[white][3.19.0]/cd\n" +
                "- SOS Gump ID configurable in settings\n" +
                "- Added macro option to execute a client-side command\n" +
                "- Added a command doesn't exist message\n" +
                "- Follow party members on world map option\n" +
                "- Added option to override party member body hues\n" +
                "- Bug fix",

             "/c[white][3.18.0]/cd\n" +
                "- Added a language file that will contain UI text for easy language translations\n",

             "/c[white][3.17.0]/cd\n" +
                "- Added original paperdoll to customizable gump system\n" +
                "- Imroved script loading time",

             "/c[white][3.16.0]/cd\n" +
                "- Some small improvements for input boxes and the new option menu\n" +
                "- Added player position offset option in TazUO->Misc\n" +
                "- Fix for health indicator percentage\n" +
                "- Fix tooltip centered text\n" +
                "- Added a modding system almost identical to ServUO's script system\n" +
                "- Added macros to use items from your counter bar\n" +
                "- Simple auto loot improvements\n" +
                "- Hold ctrl and drop an item anywhere on the game window to drop it",

            "/c[white][3.15.0]/cd\n" +
                "- Mouse interaction for overhead text can be disabled\n" +
                "- Visable layers option added in Options->TazUO\n" +
                "- Added custom XML Gumps -> see wiki\n" +
                "- Added some controller support for movement and macros",

            "/c[white][3.14.0]/cd\n" +
                "- New options menu\n" +
                "- Small null ref bug fix\n" +
                "- No max width on item count text for smaller scaling\n" +
                "- Auto loot shift-click will no long work if you have shift for context menu or split stacks.\n" +
                "- Skill progress bars will save their position if you move them\n" +
                "- Changed skill progress bars to a queue instead of all showing at once\n" +
                "- Fix art png loading\n" +
                "- Added /c[green]-paperdoll/cd command\n" +
                "- Added an auto resync option under Options->TazUO->Misc\n" +
                "- Alt + Click paperdoll preview in modern paperdoll to copy a screenshot of it\n" +
                "- Added `both` option to auto close gumps range or dead\n" +
                "- Added shift + double click to advanced shop gump to buy/sell all of that item\n" +
                "- Added use one health bar for last attack option\n" +
                "- Added `-optlink` command",

            "/c[white][3.13.0]/cd\n" +
                "- Fix item unintentional stacking\n" +
                "- Potential small bug fix\n" +
                "- Option to close anchored healthbars automatically\n" +
                "- Added optional freeze on cast to spell indicator system\n" +
                "- Save server side gump positions\n" +
                "- Added addition equipment slots to the original paperdoll gump",

            "/c[white][3.12.0]/cd\n" +
                "- Added Exclude self to advanced nameplate options\n" +
                "- Bug fix for spell indicator loading\n" +
                "- Added override profile for same server characters only\n",

            "/c[white][3.11.0]/cd\n" +
                "- Modern shop gump fix\n" +
                "- Pull in latest changes from CUO\n" +
                "- Update client-side version checking\n" +
                "- Infobar bug fixes\n" +
                "- Other small bug fixes\n" +
                "- Modern paperdoll being anchored will be remembered now\n" +
                "- Added an option for Cooldown bars to use the position of the last moved bar\n" +
                "- Added advanced nameplate options\n" +
                "- Moved TTF Font settings to their own category\n" +
                "- Journal tabs are now fully customizable",

            "/c[white][3.10.1]/cd\n" +
                "- Bug fix for floating damage numbers\n" +
                "- Bug fix for health line color\n" +
                "- Fix skill progress bar positioning\n",

            "/c[white][3.10.0]/cd\n" +
                "- Added the option to download a spell indicator config from an external source\n" +
                "- Added a simple auto loot system\n" +
                "- Updated to ClassicUO's latest version\n" +
                "- Auto sort is container specific now\n" +
                "- InfoBar can now be resized and is using the new font rendering system\n" +
                "- InfoBar font and font size can be customized now (TazUO->Misc)\n" +
                "- Journal will now remember the last tab you were on\n" +
                "- Upgraded item comparisons, see wiki on tooltip overrides for more info\n" +
                "- Spell indicators can now be overridden with a per-character config",

            "/c[white][3.9.0]/cd\n" +
                "- Added missing race change gump\n" +
                "- If no server is set in settings.json user will get a request to type one in\n" +
                "- When opening TUO with a uo directory that is not valid a folder selection prompt will open\n" +
                "- Spell indicator system, see wiki for more details\n" +
                "- The /c[green]-marktile/cd command works on static locations also now\n" +
                "- The 'Items Only' option for nameplates will no longer include corpses\n" +
                "- Bug fix for object highlighting\n" +
                "- Bug fix for <BR> tag in tooltips",

            "/c[white][3.8.0]/cd\n" +
                "- Added sound override feature\n" +
                "- Added -radius command, see wiki for more details\n" +
                "- Added an optional skill progress bar when a skill changes\n",

            "/c[white][3.7.1]/cd\n" +
                "- Added ability to sort advanced skills gump by lock status\n" +
                "- Added import and export options for Grid Highlight settings\n" +
                "- Added a simple account selector on the login screen\n" +
                "- Added a toggle to auto sort grid containers\n" +
                "- Trees/stumps will be slightly visible with circle of transparency on\n" +
                "- Multi item move can now move items to the trade window\n" +
                "- Added -marktile command, see wiki for more details\n" +
                "- Updated TUO with CUO updates\n" +
                "- Fixed mouse interactions with art replaced using the PNG replacement system\n" +
                "- Advanced Skill Gump light support for groups added by Elderwyn\n" +
                "- Fix for backpack not loading contents when logging in\n" +
                "- Text width fix for old clients\n" +
                "- Fix for a potential small memory leak - Lasheras\n" +
                "- Fix for a bug when creating a new character\n" +
                "- Potential fix for bug when processing messages\n" +
                "- Fixed an issue on OSI where corpses would not open in grid containers\n" +
                "- Fix for some SOS messages\n" +
                "- Fix for text not being clickable\n" +
                "- Added yellow highlighting for overhead text",

            "/c[white][3.7.0]/cd\n" +
                "- Updated some default font sizes, slightly larger (New installs only)\n" +
                "- Added item count to grid containers\n" +
                "- Changed health lines back to blue\n" +
                "- Added boat control gump\n" +
                "- Fixed + symbol issue with tooltip overrides\n" +
                "- Fixed an issue with having zero tooltip overrides\n" +
                "- Fixed journal width issue when timestamps are disabled\n" +
                "- Added {3} to tooltip overrides, inserting the original tooltip property",

            "/c[white][3.6.0]/cd\n" +
                "- Tooltip import crash fix\n" +
                "- Tooltip delete all override button added\n" +
                "- Tooltip override color fix\n" +
                "- Added an error message when importing tooltip override fails\n" +
                "- Fixed tooltip background hue offset",

            "/c[white][3.5.0]/cd\n" +
                "- Bug fix for EA egg event\n" +
                "- Added tooltip header formatting(change item name color)\n" +
                "- Damage hues fixed\n" +
                "- Added fix for <h2> and <Bodytextcolor> tags\n" +
                "- Tooltip crash fix\n" +
                "- Added tooltip export and import buttons\n" +
                "- Updated to the main CUO repo",

            "/c[white][3.4.0]/cd\n" +
                "- Added this version history gump\n" +
                "- Added /c[green]-version/cd command to open this gump\n" +
                "- Made advanced skill gump more compact, height resizable and can grab skill buttons by dragging skills\n" +
                "- Added tooltip override feature (See wiki for more details)\n" +
                "- Better rain\n" +
                "- Fixed tooltips in vendor search\n" +
                "- Fixed modern shop gump displaying wrong items at animal trainers\n" +
                "- Added hide border and timestamps to journal options\n" +
                "- Added hide border option for grid containers",

            "/c[white][3.3.0]/cd\n" +
                "-Last attack automatic healthbar gump will remember its position\n" +
                "-Nameplate gump now has a search option (Ctrl + Shift)\n"+
                "-Fix number(gold) entry for trading gump\n"+
                "-Fixed red warmode outline for custom health gumps\n"+
                "-Graphics in info bar -> See wiki\n"+
                "-Tooltip background colors adjustable\n"+
                "-Tmap and SOS right click menu moved to menu icon on gump\n"+
                "- \"/c[green]-skill /c[white]skillname/cd\" command added to use skills\n",
            "\n\n/c[white]For further history please visit our discord."
        };

        public VersionHistory() : base(0, 0)
        {
            X = 300;
            Y = 200;
            Width = 400;
            Height = 500;
            CanCloseWithRightClick = true;
            CanMove = true;

            BorderControl bc = new BorderControl(0, 0, Width, Height, 36);
            bc.T_Left = 39925;
            bc.H_Border = 39926;
            bc.T_Right = 39927;
            bc.V_Border = 39928;
            bc.V_Right_Border = 39930;
            bc.B_Left = 39931;
            bc.B_Right = 39933;
            bc.H_Bottom_Border = 39932;

            Add(new GumpPicTiled(39929) { X = bc.BorderSize, Y = bc.BorderSize, Width = Width - (bc.BorderSize * 2), Height = Height - (bc.BorderSize * 2) });

            Add(bc);

            TextBox _;
            Add(_ = new TextBox(Language.Instance.TazuoVersionHistory, TrueTypeLoader.EMBEDDED_FONT, 30, Width, Color.White, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { Y = 10 });
            Add(_ = new TextBox(Language.Instance.CurrentVersion + CUOEnviroment.Version.ToString(), TrueTypeLoader.EMBEDDED_FONT, 20, Width, Color.Orange, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { Y = _.Y + _.Height + 5 });

            ScrollArea scroll = new ScrollArea(10, _.Y + _.Height, Width - 20, Height - (_.Y + _.Height) - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.45f) { Width = scroll.Width, Height = scroll.Height, X = scroll.X, Y = scroll.Y });

            int y = 0;
            foreach (string s in updateTexts)
            {
                scroll.Add(_ = new TextBox(s, TrueTypeLoader.EMBEDDED_FONT, 15, scroll.Width - scroll.ScrollBarWidth(), Color.Orange, FontStashSharp.RichText.TextHorizontalAlignment.Left, false) { Y = y });
                y += _.Height + 10;
            }

            Add(scroll);


            HitBox _hit;
            Add(_ = new TextBox(Language.Instance.TazUOWiki, TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.Orange, strokeEffect: false) { X = 25, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/bittiez/ClassicUO/wiki");
            };

            Add(_ = new TextBox(Language.Instance.TazUOWiki, TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.Orange, strokeEffect: false) { X = 280, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://discord.gg/SqwtB5g95H");
            };
        }
    }
}
