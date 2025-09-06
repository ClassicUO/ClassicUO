using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using LScript;
using static ClassicUO.LegionScripting.Commands;
using static ClassicUO.LegionScripting.Expressions;

namespace ClassicUO.LegionScripting
{
    internal static class LegionScripting
    {
        public static string ScriptPath;

        private static bool _enabled, _loaded;

        private static List<ScriptFile> runningScripts = new List<ScriptFile>();
        private static List<ScriptFile> removeRunningScripts = new List<ScriptFile>();
        private static LScriptSettings lScriptSettings;

        public static List<ScriptFile> LoadedScripts = new List<ScriptFile>();

        public static event EventHandler<ScriptInfoEvent> ScriptStartedEvent;
        public static event EventHandler<ScriptInfoEvent> ScriptStoppedEvent;

        public static void Init()
        {
            ScriptPath = Path.GetFullPath(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts"));

            CommandManager.Register("lscript", (args) =>
            {
                if (args.Length == 1)
                {
                    UIManager.Add(new ScriptManagerGump());
                }
            });

            CommandManager.Register("lscriptfile", (args) =>
            {
                if (args.Length < 2)
                    return;

                string file = args[1];

                if (!file.EndsWith(".lscript"))
                    file += ".lscript";

                foreach (ScriptFile script in LoadedScripts)
                {
                    if (script.FileName == file && script.GetScript != null)
                    {
                        PlayScript(script);
                        break;
                    }
                }
            });


            if (!_loaded)
            {
                RegisterCommands();

                EventSink.JournalEntryAdded += EventSink_JournalEntryAdded;
                _loaded = true;
            }

            LoadScriptsFromFile();
            LoadLScriptSettings();
            AutoPlayGlobal();
            AutoPlayChar();
            _enabled = true;
        }
        private static void EventSink_JournalEntryAdded(object sender, JournalEntry e)
        {
            foreach (ScriptFile script in runningScripts)
            {
                script.GetScript.JournalEntryAdded(e);
            }
        }
        public static void LoadScriptsFromFile()
        {
            if (!Directory.Exists(ScriptPath))
                Directory.CreateDirectory(ScriptPath);

            LoadedScripts.RemoveAll(ls => !ls.FileExists());

            List<string> groups = [ScriptPath, .. HandleScriptsInDirectory(ScriptPath)];

            List<string> subgroups = new List<string>();

            //First level directory(groups)
            foreach (string file in groups)
                subgroups.AddRange(HandleScriptsInDirectory(file));

            foreach (string file in subgroups)
                HandleScriptsInDirectory(file); //No third level supported, ignore directories
        }
        private static void AddScriptFromFile(string path)
        {
            string p = Path.GetDirectoryName(path);
            string fname = Path.GetFileName(path);

            LoadedScripts.Add(new ScriptFile(p, fname));
        }
        /// <summary>
        /// Returns a list of sub directories
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static List<string> HandleScriptsInDirectory(string path)
        {
            HashSet<string> loadedScripts = new HashSet<string>();

            foreach (ScriptFile script in LoadedScripts)
                loadedScripts.Add(script.FullPath);

            List<string> groups = new List<string>();
            foreach (string file in Directory.EnumerateFileSystemEntries(path))
            {
                if (file.EndsWith(".lscript"))
                {
                    if (loadedScripts.Contains(file)) continue;
                    AddScriptFromFile(file);
                    loadedScripts.Add(file);
                }
                else if (Directory.Exists(file))
                {
                    groups.Add(file);
                }
            }

            return groups;
        }
        public static void SetAutoPlay(ScriptFile script, bool global, bool enabled)
        {
            if (global)
            {
                if (enabled)
                {
                    if (!lScriptSettings.GlobalAutoStartScripts.Contains(script.FileName))
                        lScriptSettings.GlobalAutoStartScripts.Add(script.FileName);
                }
                else
                {
                    lScriptSettings.GlobalAutoStartScripts.Remove(script.FileName);
                }

            }
            else
            {
                if (lScriptSettings.CharAutoStartScripts.ContainsKey(GetAccountCharName()))
                {
                    if (enabled)
                    {
                        if (!lScriptSettings.CharAutoStartScripts[GetAccountCharName()].Contains(script.FileName))
                            lScriptSettings.CharAutoStartScripts[GetAccountCharName()].Add(script.FileName);
                    }
                    else
                        lScriptSettings.CharAutoStartScripts[GetAccountCharName()].Remove(script.FileName);
                }
                else
                {
                    if (enabled)
                        lScriptSettings.CharAutoStartScripts.Add(GetAccountCharName(), new List<string> { script.FileName });
                }
            }
        }
        public static bool AutoLoadEnabled(ScriptFile script, bool global)
        {
            if (!_enabled)
                return false;

            if (global)
                return lScriptSettings.GlobalAutoStartScripts.Contains(script.FileName);
            else
            {
                if (lScriptSettings.CharAutoStartScripts.TryGetValue(GetAccountCharName(), out var scripts))
                {
                    return scripts.Contains(script.FileName);
                }
            }

            return false;
        }
        private static void AutoPlayGlobal()
        {
            foreach (string script in lScriptSettings.GlobalAutoStartScripts)
            {
                foreach (ScriptFile f in LoadedScripts)
                    if (f.FileName == script)
                        PlayScript(f);
            }
        }
        private static void AutoPlayChar()
        {
            if (World.Player == null)
                return;

            if (lScriptSettings.CharAutoStartScripts.TryGetValue(GetAccountCharName(), out var scripts))
                foreach (ScriptFile f in LoadedScripts)
                    if (scripts.Contains(f.FileName))
                        PlayScript(f);

        }
        private static string GetAccountCharName()
        {
            return ProfileManager.CurrentProfile.Username + ProfileManager.CurrentProfile.CharacterName;
        }
        public static bool IsGroupCollapsed(string group, string subgroup = "")
        {
            var path = group;
            if (!string.IsNullOrEmpty(subgroup))
                path += "/" + subgroup;

            if (lScriptSettings.GroupCollapsed.ContainsKey(path))
                return lScriptSettings.GroupCollapsed[path];

            return false;
        }
        public static void SetGroupCollapsed(string group, string subgroup = "", bool expanded = false)
        {
            var path = group;
            if (!string.IsNullOrEmpty(subgroup))
                path += "/" + subgroup;

            lScriptSettings.GroupCollapsed[path] = expanded;
        }
        private static void LoadLScriptSettings()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "lscript.json");

            try
            {
                if (File.Exists(path))
                {
                    lScriptSettings = JsonSerializer.Deserialize<LScriptSettings>(File.ReadAllText(path));
                    return;
                }
            }
            catch (Exception e) { }

            lScriptSettings = new LScriptSettings();
        }
        private static void SaveScriptSettings()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "lscript.json");

            string json = JsonSerializer.Serialize(lScriptSettings);
            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception e) { }
        }
        public static void Unload()
        {
            while (runningScripts.Count > 0)
                StopScript(runningScripts[0]);

            Interpreter.ClearAllLists();

            SaveScriptSettings();

            _enabled = false;
        }
        public static void OnUpdate()
        {
            if (!_enabled || !World.InGame)
                return;

            foreach (ScriptFile script in runningScripts)
            {
                try
                {
                    if (!Interpreter.ExecuteScript(script.GetScript))
                    {
                        removeRunningScripts.Add(script);
                    }
                }
                catch (Exception e)
                {
                    removeRunningScripts.Add(script);
                    LScriptError($"Execution of script failed. -> [{e.Message}]");
                }
            }

            if (removeRunningScripts.Count > 0)
            {
                foreach (ScriptFile script in removeRunningScripts)
                    StopScript(script);

                removeRunningScripts.Clear();
            }
        }
        public static void PlayScript(ScriptFile script)
        {
            if (script != null)
            {
                if (runningScripts.Contains(script)) //Already playing
                    return;

                script.GenerateScript();
                runningScripts.Add(script);
                script.GetScript.IsPlaying = true;

                ScriptStartedEvent?.Invoke(null, new ScriptInfoEvent(script));
            }
        }
        public static void StopScript(ScriptFile script)
        {
            if (script != null)
            {
                if (runningScripts.Contains(script))
                    runningScripts.Remove(script);

                script.GetScript.Reset();
                script.GetScript.IsPlaying = false;

                ScriptStoppedEvent?.Invoke(null, new ScriptInfoEvent(script));
            }
        }
        private static uint DefaultAlias(string alias)
        {
            if (World.InGame)
                switch (alias)
                {
                    case "backpack": return World.Player.FindItemByLayer(Layer.Backpack);
                    case "bank": return World.Player.FindItemByLayer(Layer.Bank);
                    case "lastobject": return World.LastObject;
                    case "lasttarget": return TargetManager.LastTargetInfo.Serial;
                    case "lefthand": return World.Player.FindItemByLayer(Layer.OneHanded);
                    case "righthand": return World.Player.FindItemByLayer(Layer.TwoHanded);
                    case "self": return World.Player;
                    case "mount": return World.Player.FindItemByLayer(Layer.Mount);
                    case "bandage": return World.Player.FindBandage();
                    case "any": return Constants.MAX_SERIAL;
                    case "anycolor": return ushort.MaxValue;
                }

            return 0;
        }
        private static void RegisterCommands()
        {
            #region Commands
            Interpreter.RegisterCommandHandler("togglefly", CommandFly);
            Interpreter.RegisterCommandHandler("useprimaryability", UsePrimaryAbility);
            Interpreter.RegisterCommandHandler("usesecondaryability", UseSecondaryAbility);
            Interpreter.RegisterCommandHandler("attack", CommandAttack);
            Interpreter.RegisterCommandHandler("clickobject", ClickObject);
            Interpreter.RegisterCommandHandler("bandageself", BandageSelf);
            Interpreter.RegisterCommandHandler("useobject", UseObject);
            Interpreter.RegisterCommandHandler("target", TargetSerial);
            Interpreter.RegisterCommandHandler("waitfortarget", WaitForTarget);
            Interpreter.RegisterCommandHandler("usetype", UseType);
            Interpreter.RegisterCommandHandler("pause", PauseCommand);
            Interpreter.RegisterCommandHandler("useskill", UseSkill);
            Interpreter.RegisterCommandHandler("walk", CommandWalk);
            Interpreter.RegisterCommandHandler("run", CommandRun);
            Interpreter.RegisterCommandHandler("canceltarget", CancelTarget);
            Interpreter.RegisterCommandHandler("sysmsg", SystemMessage);
            Interpreter.RegisterCommandHandler("moveitem", MoveItem);
            Interpreter.RegisterCommandHandler("moveitemoffset", MoveItemOffset);
            Interpreter.RegisterCommandHandler("cast", CastSpell);
            Interpreter.RegisterCommandHandler("waitforjournal", WaitForJournal);
            Interpreter.RegisterCommandHandler("settimer", SetTimer);
            Interpreter.RegisterCommandHandler("setalias", SetAlias);
            Interpreter.RegisterCommandHandler("unsetalias", UnsetAlias);
            Interpreter.RegisterCommandHandler("movetype", MoveType);
            Interpreter.RegisterCommandHandler("removetimer", RemoveTimer);
            Interpreter.RegisterCommandHandler("msg", MsgCommand);
            Interpreter.RegisterCommandHandler("toggleautoloot", ToggleAutoLoot);
            Interpreter.RegisterCommandHandler("info", InfoGump);
            Interpreter.RegisterCommandHandler("setskill", SetSkillLock);
            Interpreter.RegisterCommandHandler("getproperties", GetProperties);
            Interpreter.RegisterCommandHandler("turn", TurnCommand);
            Interpreter.RegisterCommandHandler("createlist", CreateList);
            Interpreter.RegisterCommandHandler("pushlist", PushList);
            Interpreter.RegisterCommandHandler("rename", RenamePet);
            Interpreter.RegisterCommandHandler("logout", Logout);
            Interpreter.RegisterCommandHandler("shownames", ShowNames);
            Interpreter.RegisterCommandHandler("clearlist", ClearList);
            Interpreter.RegisterCommandHandler("removelist", RemoveList);
            Interpreter.RegisterCommandHandler("togglehands", ToggleHands);
            Interpreter.RegisterCommandHandler("equipitem", EquipItem);
            Interpreter.RegisterCommandHandler("togglemounted", ToggleMounted);
            Interpreter.RegisterCommandHandler("promptalias", PromptAlias);
            Interpreter.RegisterCommandHandler("waitforgump", WaitForGump);
            Interpreter.RegisterCommandHandler("replygump", ReplyGump);
            Interpreter.RegisterCommandHandler("closegump", CloseGump);
            Interpreter.RegisterCommandHandler("clearjournal", ClearJournal);
            Interpreter.RegisterCommandHandler("poplist", PopList);
            Interpreter.RegisterCommandHandler("targettilerel", TargetTileRel);
            Interpreter.RegisterCommandHandler("targetlandrel", TargetLandRel);
            Interpreter.RegisterCommandHandler("virtue", Virtue);
            Interpreter.RegisterCommandHandler("playmacro", PlayMacro);
            Interpreter.RegisterCommandHandler("headmsg", HeadMsg);
            Interpreter.RegisterCommandHandler("partymsg", PartyMsg);
            Interpreter.RegisterCommandHandler("guildmsg", GuildMsg);
            Interpreter.RegisterCommandHandler("allymsg", AllyMsg);
            Interpreter.RegisterCommandHandler("whispermsg", WhisperMsg);
            Interpreter.RegisterCommandHandler("yellmsg", YellMsg);
            Interpreter.RegisterCommandHandler("emotemsg", EmoteMsg);
            Interpreter.RegisterCommandHandler("waitforprompt", WaitForPrompt);
            Interpreter.RegisterCommandHandler("cancelprompt", CancelPrompt);
            Interpreter.RegisterCommandHandler("promptresponse", PromptResponse);
            Interpreter.RegisterCommandHandler("contextmenu", ContextMenu);
            Interpreter.RegisterCommandHandler("ignoreobject", IgnoreObject);
            Interpreter.RegisterCommandHandler("clearignorelist", ClearIgnoreList);
            Interpreter.RegisterCommandHandler("goto", Goto);
            Interpreter.RegisterCommandHandler("return", Return);
            Interpreter.RegisterCommandHandler("follow", Follow);
            Interpreter.RegisterCommandHandler("pathfind", Pathfind);
            Interpreter.RegisterCommandHandler("cancelpathfind", CancelPathfind);
            #endregion

            #region Expressions
            Interpreter.RegisterExpressionHandler("timerexists", TimerExists);
            Interpreter.RegisterExpressionHandler("timerexpired", TimerExpired);
            Interpreter.RegisterExpressionHandler("findtype", FindType);
            Interpreter.RegisterExpressionHandler("findtypelist", FindTypeList);
            Interpreter.RegisterExpressionHandler("findalias", FindAlias);
            Interpreter.RegisterExpressionHandler("skill", SkillValue);
            Interpreter.RegisterExpressionHandler("poisoned", PoisonedStatus);
            Interpreter.RegisterExpressionHandler("war", CheckWar);
            Interpreter.RegisterExpressionHandler("contents", CountContents);
            Interpreter.RegisterExpressionHandler("findobject", FindObject);
            Interpreter.RegisterExpressionHandler("distance", DistanceCheck);
            Interpreter.RegisterExpressionHandler("injournal", InJournal);
            Interpreter.RegisterExpressionHandler("inparty", InParty);
            Interpreter.RegisterExpressionHandler("property", PropertySearch);
            Interpreter.RegisterExpressionHandler("buffexists", BuffExists);
            Interpreter.RegisterExpressionHandler("findlayer", FindLayer);
            Interpreter.RegisterExpressionHandler("gumpexists", GumpExists);
            Interpreter.RegisterExpressionHandler("listcount", ListCount);
            Interpreter.RegisterExpressionHandler("listexists", ListExists);
            Interpreter.RegisterExpressionHandler("inlist", InList);
            Interpreter.RegisterExpressionHandler("nearesthostile", NearestHostile);
            Interpreter.RegisterExpressionHandler("counttype", CountType);
            Interpreter.RegisterExpressionHandler("ping", Ping);
            Interpreter.RegisterExpressionHandler("itemamt", ItemAmt);
            Interpreter.RegisterExpressionHandler("primaryabilityactive", PrimaryAbilityActive);
            Interpreter.RegisterExpressionHandler("secondaryabilityactive", SecondaryAbilityActive);
            Interpreter.RegisterExpressionHandler("pathfinding", IsPathfinding);
            #endregion

            #region Player Values
            Interpreter.RegisterExpressionHandler("mana", GetPlayerMana);
            Interpreter.RegisterExpressionHandler("maxmana", GetPlayerMaxMana);
            Interpreter.RegisterExpressionHandler("hits", GetPlayerHits);
            Interpreter.RegisterExpressionHandler("maxhits", GetPlayerMaxHits);
            Interpreter.RegisterExpressionHandler("stam", GetPlayerStam);
            Interpreter.RegisterExpressionHandler("maxstam", GetPlayerMaxStam);
            Interpreter.RegisterExpressionHandler("x", GetPosX);
            Interpreter.RegisterExpressionHandler("y", GetPosY);
            Interpreter.RegisterExpressionHandler("z", GetPosZ);
            Interpreter.RegisterExpressionHandler("name", GetPlayerName);
            Interpreter.RegisterExpressionHandler("true", GetTrue);
            Interpreter.RegisterExpressionHandler("false", GetFalse);
            Interpreter.RegisterExpressionHandler("dead", IsDead);
            Interpreter.RegisterExpressionHandler("paralyzed", IsParalyzed);
            Interpreter.RegisterExpressionHandler("mounted", IsMounted);
            Interpreter.RegisterExpressionHandler("diffhits", DiffHits);
            Interpreter.RegisterExpressionHandler("str", GetStr);
            Interpreter.RegisterExpressionHandler("dex", GetDex);
            Interpreter.RegisterExpressionHandler("int", GetInt);
            Interpreter.RegisterExpressionHandler("followers", GetFollowers);
            Interpreter.RegisterExpressionHandler("maxfollowers", GetMaxFollowers);
            Interpreter.RegisterExpressionHandler("gold", GetGold);
            Interpreter.RegisterExpressionHandler("hidden", IsHidden);
            Interpreter.RegisterExpressionHandler("weight", GetPlayerWeight);
            Interpreter.RegisterExpressionHandler("maxweight", GetPlayerMaxWeight);
            #endregion

            #region Default aliases
            Interpreter.RegisterAliasHandler("backpack", DefaultAlias);
            Interpreter.RegisterAliasHandler("bank", DefaultAlias);
            Interpreter.RegisterAliasHandler("lastobject", DefaultAlias);
            Interpreter.RegisterAliasHandler("lasttarget", DefaultAlias);
            Interpreter.RegisterAliasHandler("lefthand", DefaultAlias);
            Interpreter.RegisterAliasHandler("righthand", DefaultAlias);
            Interpreter.RegisterAliasHandler("self", DefaultAlias);
            Interpreter.RegisterAliasHandler("mount", DefaultAlias);
            Interpreter.RegisterAliasHandler("bandage", DefaultAlias);
            Interpreter.RegisterAliasHandler("any", DefaultAlias);
            Interpreter.RegisterAliasHandler("anycolor", DefaultAlias);
            #endregion
        }

        public static bool ReturnTrue() //Avoids creating a bunch of functions that need to be GC'ed
        {
            return true;
        }
        public static void LScriptError(string msg)
        {
            GameActions.Print($"[{Interpreter.ActiveScript.CurrentLine}][LScript Error]" + msg);
        }
        public static void LScriptWarning(string msg)
        {
            GameActions.Print($"[{Interpreter.ActiveScript.CurrentLine}][LScript Warning]" + msg);
        }
    }

    internal class ScriptInfoEvent
    {
        public ScriptFile GetScript;

        public ScriptInfoEvent(ScriptFile getScript)
        {
            GetScript = getScript;
        }
    }

    internal class ScriptFile
    {
        public string Path;
        public string FileName;
        public string FullPath;
        public string Group = string.Empty;
        public string SubGroup = string.Empty;
        public Script GetScript;
        public string[] FileContents;

        public ScriptFile(string path, string fileName)
        {
            Path = path;

            var cleanPath = path.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            var cleanBasePath = LegionScripting.ScriptPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            cleanPath = cleanPath.Substring(cleanPath.IndexOf(cleanBasePath) + cleanBasePath.Length);

            if (cleanPath.Length > 0)
            {
                var paths = cleanPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
                if (paths.Length > 0)
                    Group = paths[0];
                if (paths.Length > 1)
                    SubGroup = paths[1];
            }

            FileName = fileName;
            FullPath = System.IO.Path.Combine(Path, FileName);
            FileContents = File.ReadAllLines(FullPath);
            GenerateScript();
        }

        public ScriptFile(string path, string source, string fileName)
        {
            Path = path;
            FileName = fileName;
            FullPath = System.IO.Path.Combine(Path, FileName);
            FileContents = source.Split(new[] { '\n' }, StringSplitOptions.None);
            GetScript = new Script(Lexer.Lex(FileContents));
        }

        public void GenerateScript()
        {
            try
            {
                if (GetScript == null)
                    GetScript = new Script(Lexer.Lex(FullPath));
                else
                    GetScript.UpdateScript(Lexer.Lex(FullPath));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public bool FileExists()
        {
            return File.Exists(FullPath);
        }
    }
}
