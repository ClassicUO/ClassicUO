#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    public static class CommandManager
    {
        private static readonly Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();

        public static Dictionary<string, Action<string[]>> Commands { get { return _commands; } }

        public static void Initialize()
        {
            Register
            (
                "info",
                s =>
                {
                    if (TargetManager.IsTargeting)
                    {
                        TargetManager.CancelTarget();
                    }

                    TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
                }
            );

            Register
            (
                "datetime",
                s =>
                {
                    if (World.Player != null)
                    {
                        GameActions.Print(string.Format(ResGeneral.CurrentDateTimeNowIs0, DateTime.Now));
                    }
                }
            );

            Register
            (
                "hue",
                s =>
                {
                    if (TargetManager.IsTargeting)
                    {
                        TargetManager.CancelTarget();
                    }

                    TargetManager.SetTargeting(CursorTarget.HueCommandTarget, CursorType.Target, TargetType.Neutral);
                }
            );


            Register
            (
                "debug",
                s =>
                {
                    CUOEnviroment.Debug = !CUOEnviroment.Debug;

                }
            );

            Register
            (
                "colorpicker",
                s =>
                {
                    UIManager.Add(new UI.Gumps.ModernColorPicker(null, 8787));

                }
            );

            Register("cast", s =>
            {
                string spell = "";
                for (int i = 1; i < s.Length; i++)
                {
                    spell += s[i] + " ";
                }
                spell = spell.Trim();

                if (SpellDefinition.TryGetSpellFromName(spell, out var spellDef))
                    GameActions.CastSpell(spellDef.ID);
            });

            List<Skill> sortSkills = new List<Skill>(World.Player.Skills);

            Register("skill", s =>
            {
                string skill = "";
                for (int i = 1; i < s.Length; i++)
                {
                    skill += s[i] + " ";
                }
                skill = skill.Trim().ToLower();

                if (skill.Length > 0)
                {
                    for (int i = 0; i < World.Player.Skills.Length; i++)
                    {
                        if (World.Player.Skills[i].Name.ToLower().Contains(skill))
                        {
                            GameActions.UseSkill(World.Player.Skills[i].Index);
                            break;
                        }
                    }
                }
            });

            Register("version", s => { UIManager.Add(new VersionHistory()); });
            Register("rain", s => { Client.Game.GetScene<ClassicUO.Game.Scenes.GameScene>()?.Weather.Generate(WeatherType.WT_RAIN, 30, 75); });

            Register("marktile", s =>
            {
                if (s.Length > 1 && s[1] == "-r")
                {
                    if (s.Length == 2)
                    {
                        TileMarkerManager.Instance.RemoveTile(World.Player.X, World.Player.Y, World.Map.Index);
                    }
                    else if (s.Length == 4)
                    {
                        if (int.TryParse(s[2], out var x))
                            if (int.TryParse(s[3], out var y))
                                TileMarkerManager.Instance.RemoveTile(x, y, World.Map.Index);
                    }
                    else if (s.Length == 5)
                    {
                        if (int.TryParse(s[2], out var x))
                            if (int.TryParse(s[3], out var y))
                                if (int.TryParse(s[4], out var m))
                                    TileMarkerManager.Instance.RemoveTile(x, y, m);
                    }
                }
                else
                {
                    if (s.Length == 1)
                    {
                        TileMarkerManager.Instance.AddTile(World.Player.X, World.Player.Y, World.Map.Index, 32);
                    }
                    else if (s.Length == 2)
                    {
                        if (ushort.TryParse(s[1], out ushort h))
                            TileMarkerManager.Instance.AddTile(World.Player.X, World.Player.Y, World.Map.Index, h);
                    }
                    else if (s.Length == 4)
                    {
                        if (int.TryParse(s[1], out var x))
                            if (int.TryParse(s[2], out var y))
                                if (ushort.TryParse(s[3], out var h))
                                    TileMarkerManager.Instance.AddTile(x, y, World.Map.Index, h);
                    }
                    else if (s.Length == 5)
                    {
                        if (int.TryParse(s[1], out var x))
                            if (int.TryParse(s[2], out var y))
                                if (int.TryParse(s[3], out var m))
                                    if (ushort.TryParse(s[4], out var h))
                                        TileMarkerManager.Instance.AddTile(x, y, m, h);
                    }
                }
            });

            Register("radius", s =>
            {
                ///-radius distance hue
                if (s.Length == 1)
                    ProfileManager.CurrentProfile.DisplayRadius ^= true;
                if (s.Length > 1)
                {
                    if (int.TryParse(s[1], out var dist))
                        ProfileManager.CurrentProfile.DisplayRadiusDistance = dist;
                    ProfileManager.CurrentProfile.DisplayRadius = true;
                }
                if (s.Length > 2)
                    if (ushort.TryParse(s[2], out var h))
                        ProfileManager.CurrentProfile.DisplayRadiusHue = h;
            });

            Register("options", (s) =>
            {
                UIManager.Add(new OptionsGump());
            });

            Register("paperdoll", (s) =>
            {
                if (ProfileManager.CurrentProfile.UseModernPaperdoll)
                {
                    UIManager.Add(new PaperDollGump(World.Player, true));
                }
                else
                {
                    UIManager.Add(new ModernPaperdoll(World.Player));
                }

            });

            Register("optlink", (s) =>
            {
                ModernOptionsGump g = UIManager.GetGump<ModernOptionsGump>();
                if (s.Length > 1)
                {
                    if (g != null)
                    {
                        g.GoToPage(s[1]);
                    }
                    else
                    {
                        UIManager.Add(g = new ModernOptionsGump());
                        g.GoToPage(s[1]);
                    }
                }
                else
                {
                    if (g != null)
                    {
                        GameActions.Print(g.GetPageString());
                    }
                }
            });

            Register("genspelldef", (s) =>
            {
                Task.Run(SpellDefinition.SaveAllSpellsToJson);
            });
        }


        public static void Register(string name, Action<string[]> callback)
        {
            name = name.ToLower();

            if (!_commands.ContainsKey(name))
            {
                _commands.Add(name, callback);
            }
            else
            {
                Log.Error($"Attempted to register command: '{name}' twice.");
            }
        }

        public static void UnRegister(string name)
        {
            name = name.ToLower();

            if (_commands.ContainsKey(name))
            {
                _commands.Remove(name);
            }
        }

        public static void UnRegisterAll()
        {
            _commands.Clear();
        }

        public static void Execute(string name, params string[] args)
        {
            name = name.ToLower();

            if (_commands.TryGetValue(name, out Action<string[]> action))
            {
                action.Invoke(args);
            }
            else
            {
                GameActions.Print(string.Format(Language.Instance.ErrorsLanguage.CommandNotFound, name));
                Log.Warn($"Command: '{name}' not exists");
            }
        }

        public static void OnHueTarget(Entity entity)
        {
            if (entity != null)
            {
                TargetManager.Target(entity);
            }

            Mouse.LastLeftButtonClickTime = 0;
            GameActions.Print(string.Format(ResGeneral.ItemID0Hue1, entity.Graphic, entity.Hue));
        }
    }
}