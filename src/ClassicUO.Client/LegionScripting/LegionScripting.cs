using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;
using LScript;
using Microsoft.Scripting.Hosting;
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
        public static IReadOnlyList<ScriptFile> GetRunningScripts() => runningScripts;
        public static bool IsScriptRunning(ScriptFile script) => script != null && runningScripts.Contains(script);

        public static ScriptFile FindScriptByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            string n = name.Trim();
            foreach (ScriptFile s in LoadedScripts)
            {
                if (string.Equals(s.FileName, n, StringComparison.OrdinalIgnoreCase))
                    return s;
                string noExt = System.IO.Path.GetFileNameWithoutExtension(s.FileName);
                if (string.Equals(noExt, n, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return null;
        }
        public static Dictionary<int, ScriptFile> PyThreads = new Dictionary<int, ScriptFile>();

        public static event EventHandler<ScriptInfoEvent> ScriptStartedEvent;
        public static event EventHandler<ScriptInfoEvent> ScriptStoppedEvent;

        public static void Init()
        {
            UOSBridge.Register();
            Task.Factory.StartNew(() => Python.CreateEngine());
            ScriptPath = Path.GetFullPath(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts"));

            CommandManager.Register("lscript", (args) =>
            {
                if (args.Length == 1)
                {
                    UIManager.Add(new ScriptManagerGump());
                }
            });

            CommandManager.Register("scriptrecorder", (args) => UIManager.Add(new ScriptRecordingGump()));
            CommandManager.Register("scriptinfo", (args) => ScriptingInfoGump.Show());

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

            CommandManager.Register("playlscript", (args) =>
            {
                if (args.Length < 2)
                {
                    GameActions.Print("Usage: playlscript <scriptname>");
                    return;
                }
                string name = string.Join(" ", args.Skip(1));
                foreach (ScriptFile f in LoadedScripts)
                {
                    if (f.FileName == name)
                    {
                        PlayScript(f);
                        return;
                    }
                }
            });

            CommandManager.Register("stoplscript", (args) =>
            {
                if (args.Length < 2)
                {
                    GameActions.Print("Usage: stoplscript <scriptname>");
                    return;
                }
                string name = string.Join(" ", args.Skip(1));
                foreach (ScriptFile sf in runningScripts)
                {
                    if (sf.FileName == name)
                    {
                        StopScript(sf);
                        return;
                    }
                }
            });

            CommandManager.Register("togglelscript", (args) =>
            {
                if (args.Length < 2)
                {
                    GameActions.Print("Usage: togglelscript <scriptname>");
                    return;
                }
                string name = string.Join(" ", args.Skip(1));
                foreach (ScriptFile sf in runningScripts)
                {
                    if (sf.FileName == name)
                    {
                        StopScript(sf);
                        return;
                    }
                }
                foreach (ScriptFile f in LoadedScripts)
                {
                    if (f.FileName == name)
                    {
                        PlayScript(f);
                        return;
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
            PersistentVars.Load();
            AutoPlayGlobal();
            AutoPlayChar();
            _enabled = true;
        }
        private static void EventSink_JournalEntryAdded(object sender, JournalEntry e)
        {
            if (e == null)
                return;
            foreach (ScriptFile script in runningScripts)
            {
                if (script == null)
                    continue;
                if (script.ScriptType == ScriptType.LegionScript)
                    script.GetScript?.JournalEntryAdded(e);
                else
                    script.scopedAPI?.JournalEntries.Enqueue(e);
            }
        }
        public static void LoadScriptsFromFile()
        {
            if (!Directory.Exists(ScriptPath))
                Directory.CreateDirectory(ScriptPath);

            LoadedScripts.RemoveAll(ls => !ls.FileExists());

            List<string> groups = [ScriptPath, .. HandleScriptsInDirectory(ScriptPath)];

            List<string> subgroups = new List<string>();

            foreach (string file in groups)
                subgroups.AddRange(HandleScriptsInDirectory(file));

            foreach (string file in subgroups)
                HandleScriptsInDirectory(file);
        }

        public static void LoadScriptsFromDirectory(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                return;
            try
            {
                rootPath = Path.GetFullPath(rootPath);
                if (!Directory.Exists(rootPath))
                    return;
            }
            catch
            {
                return;
            }

            var toProcess = new List<string> { rootPath };
            while (toProcess.Count > 0)
            {
                string dir = toProcess[0];
                toProcess.RemoveAt(0);
                try
                {
                    var subdirs = HandleScriptsInDirectory(dir);
                    toProcess.AddRange(subdirs);
                }
                catch
                {
                }
            }
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
                string fname = Path.GetFileName(file);
                if (fname == "API.py" || fname.StartsWith("_"))
                    continue;
                if (file.EndsWith(".lscript") || file.EndsWith(".py") || file.EndsWith(".uos"))
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
                    for (int i = 0; i < lScriptSettings.CharAutoStartScripts.Count; i++)
                    {
                        var val = lScriptSettings.CharAutoStartScripts.ElementAt(i);
                        val.Value.RemoveAll(script => !LoadedScripts.Any(s => s.FileName == script));
                    }
                    lScriptSettings.GlobalAutoStartScripts.RemoveAll(script => !LoadedScripts.Any(s => s.FileName == script));
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error loading lscript settings: {ex}");
            }

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
            catch (Exception ex)
            {
                Log.Error($"Error saving lscript settings: {ex}");
            }
        }
        public static void Unload()
        {
            while (runningScripts.Count > 0)
                StopScript(runningScripts[0]);

            Interpreter.ClearAllLists();
            PyThreads.Clear();

            SaveScriptSettings();
            PersistentVars.Unload();

            _enabled = false;
        }
        public static void OnUpdate()
        {
            if (!_enabled || !World.InGame)
                return;

            MainThreadQueue.Process();

                foreach (ScriptFile script in runningScripts)
                {
                    if (script.ScriptType == ScriptType.Python)
                        continue;
                    try
                    {
                        if (script.ScriptType == ScriptType.UOScript)
                        {
                            if (!global::UOScript.Interpreter.ExecuteScript())
                                removeRunningScripts.Add(script);
                        }
                        else if (script.ScriptType == ScriptType.LegionScript)
                        {
                            if (!LScript.Interpreter.ExecuteScript(script.GetScript))
                                removeRunningScripts.Add(script);
                        }
                    }
                    catch (Exception e)
                    {
                        removeRunningScripts.Add(script);
                        string msg = e.Message;
                        if (e.InnerException != null)
                            msg += " -> " + e.InnerException.Message;
                        LScriptError($"Execution of script failed. -> [{msg}]");
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
            if (script == null)
                return;
            if (runningScripts.Contains(script))
                return;

            if (script.ScriptType == ScriptType.LegionScript)
            {
                script.GenerateScript();
                if (script.GetScript == null)
                {
                    LScriptError("Unable to play script, it is likely malformed and we were unable to generate the script from your file.");
                    return;
                }
                script.GetScript.IsPlaying = true;
            }
            else if (script.ScriptType == ScriptType.UOScript)
            {
                script.GenerateUOScript();
                if (script.UOScript == null)
                {
                    string detail = string.IsNullOrEmpty(script.LastUOScriptParseError) ? "" : $" ({script.LastUOScriptParseError})";
                    LScriptError("Unable to play UOScript, it is likely malformed." + detail);
                    return;
                }
                if (!global::UOScript.Interpreter.StartScript(script.UOScript))
                {
                    LScriptError("Unable to start UOScript.");
                    return;
                }
            }
            else if (script.ScriptType == ScriptType.Python)
            {
                if (script.PythonThread == null || !script.PythonThread.IsAlive)
                {
                    script.ReadFromFile();
                    script.PythonThread = new Thread(() => ExecutePythonScript(script));
                    PyThreads[script.PythonThread.ManagedThreadId] = script;
                    script.PythonThread.Start();
                }
            }

            runningScripts.Add(script);
            ScriptStartedEvent?.Invoke(null, new ScriptInfoEvent(script));
        }

        private static void ExecutePythonScript(ScriptFile script)
        {
            script.SetupPythonEngine();
            script.SetupPythonScope();
            try
            {
                script.pythonEngine.Execute(script.FileContentsJoined, script.pythonScope);
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception e)
            {
                var eo = script.pythonEngine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo?.FormatException(e) ?? e.ToString();
                GameActions.Print("Python Script Error:");
                GameActions.Print(error);
            }
            MainThreadQueue.EnqueueAction(() => StopScript(script));
        }
        public static void StopScript(ScriptFile script)
        {
            if (script == null)
                return;
            if (runningScripts.Contains(script))
                runningScripts.Remove(script);

            if (script.ScriptType == ScriptType.LegionScript && script.GetScript != null)
            {
                script.GetScript.Reset();
                script.GetScript.IsPlaying = false;
            }
            else if (script.ScriptType == ScriptType.UOScript && script.UOScript != null)
            {
                global::UOScript.Interpreter.StopScript();
            }
            else if (script.ScriptType == ScriptType.Python)
            {
                if (script.PythonThread != null)
                {
                    try
                    {
                        PyThreads.Remove(script.PythonThread.ManagedThreadId);
                        script.PythonThread.Abort();
                    }
                    catch { }
                    script.PythonThread = null;
                }
                if (script.scopedAPI != null || script.pythonScope != null)
                    script.PythonScriptStopped();
            }

            ScriptStoppedEvent?.Invoke(null, new ScriptInfoEvent(script));
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
            Interpreter.RegisterCommandHandler("addcooldown", AddCoolDown);
            Interpreter.RegisterCommandHandler("togglescript", ToggleScript);
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
            Interpreter.RegisterExpressionHandler("nearestcorpse", NearestCorpse);
            Interpreter.RegisterExpressionHandler("diffstam", DiffStam);
            Interpreter.RegisterExpressionHandler("diffmana", DiffMana);
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
            string prefix = Interpreter.ActiveScript != null ? $"[{Interpreter.ActiveScript.CurrentLine}]" : "";
            GameActions.Print($"{prefix}[LScript Error]{msg}");
        }
        public static void LScriptWarning(string msg)
        {
            string prefix = Interpreter.ActiveScript != null ? $"[{Interpreter.ActiveScript.CurrentLine}]" : "";
            GameActions.Print($"{prefix}[LScript Warning]{msg}");
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

    internal enum ScriptType
    {
        LegionScript,
        Python,
        UOScript
    }

    internal class ScriptFile
    {
        public string Path;
        public string FileName;
        public string FullPath;
        public string Group = string.Empty;
        public string SubGroup = string.Empty;
        public LScript.Script GetScript;
        public global::UOScript.Script UOScript;
        public string LastUOScriptParseError;
        public string[] FileContents;
        public string FileContentsJoined;
        public ScriptType ScriptType = ScriptType.LegionScript;
        public Thread PythonThread;
        public ScriptEngine pythonEngine;
        public ScriptScope pythonScope;
        public API scopedAPI;

        public bool IsPlaying
        {
            get
            {
                if (ScriptType == ScriptType.LegionScript && GetScript != null)
                    return GetScript.IsPlaying;
                if (ScriptType == ScriptType.UOScript)
                    return LegionScripting.IsScriptRunning(this);
                return PythonThread != null;
            }
        }

        public ScriptFile(string path, string fileName)
        {
            Path = path;

            var cleanPath = path.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            var cleanBasePath = LegionScripting.ScriptPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            cleanPath = cleanPath.Substring(cleanPath.IndexOf(cleanBasePath) + cleanBasePath.Length);

            if (cleanPath.Length > 0)
            {
                var paths = cleanPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (paths.Length > 0)
                    Group = paths[0];
                if (paths.Length > 1)
                    SubGroup = paths[1];
            }

            FileName = fileName;
            FullPath = System.IO.Path.Combine(Path, FileName);
            FileContents = ReadFromFile();
            if (FileName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                ScriptType = ScriptType.Python;
            else if (FileName.EndsWith(".uos", StringComparison.OrdinalIgnoreCase))
                ScriptType = ScriptType.UOScript;
            if (ScriptType == ScriptType.LegionScript)
                GenerateScript();
            else if (ScriptType == ScriptType.UOScript)
                GenerateUOScript();
        }

        public ScriptFile(string path, string source, string fileName)
        {
            Path = path;
            FileName = fileName;
            FullPath = System.IO.Path.Combine(Path, FileName);
            FileContents = source.Split(new[] { '\n' }, StringSplitOptions.None);
            GetScript = new LScript.Script(LScript.Lexer.Lex(FileContents));
        }

        public void ReloadFromFile()
        {
            FileContents = ReadFromFile();
            if (ScriptType == ScriptType.UOScript)
                GenerateUOScript();
            else
                GenerateScript();
        }

        public string[] ReadFromFile()
        {
            try
            {
                var c = File.ReadAllLines(FullPath);
                FileContents = c;
                FileContentsJoined = string.Join("\n", c);
                if (ScriptType == ScriptType.Python)
                {
                    FileContentsJoined = System.Text.RegularExpressions.Regex.Replace(
                        FileContentsJoined,
                        @"^\s*(?:from\s+[\w.]+\s+import\s+API|import\s+API)\s*$",
                        string.Empty,
                        System.Text.RegularExpressions.RegexOptions.Multiline);
                }
                return c;
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading script file: {ex}");
                return new string[0];
            }
        }

        public bool IsPython => ScriptType == ScriptType.Python;
        public bool IsUOScript => ScriptType == ScriptType.UOScript;

        public void GenerateUOScript()
        {
            LegionScripting.StopScript(this);
            LastUOScriptParseError = null;
            try
            {
                UOScript = new global::UOScript.Script(global::UOScript.Lexer.Lex(FullPath));
            }
            catch (Exception ex)
            {
                LastUOScriptParseError = ex.Message;
                Log.Error($"Error parsing UOScript {FileName}: {ex}");
                UOScript = null;
            }
        }

        public void GenerateScript()
        {
            LegionScripting.StopScript(this);
            if (IsPython || IsUOScript)
            {
                GetScript = null;
                return;
            }
            try
            {
                if (GetScript == null)
                    GetScript = new LScript.Script(LScript.Lexer.Lex(FullPath));
                else
                    GetScript.UpdateScript(LScript.Lexer.Lex(FullPath));
            }
            catch (Exception ex)
            {
                Log.Error($"Error generating script: {ex}");
            }
        }

        public void SetupPythonEngine()
        {
            if (pythonEngine != null)
                return;
            pythonEngine = Python.CreateEngine();
            string dir = System.IO.Path.GetDirectoryName(FullPath);
            var paths = pythonEngine.GetSearchPaths();
            paths.Add(System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts"));
            if (!string.IsNullOrWhiteSpace(dir))
                paths.Add(dir);
            pythonEngine.SetSearchPaths(paths);
        }

        public void SetupPythonScope()
        {
            pythonScope = pythonEngine.CreateScope();
            var api = new API(pythonEngine);
            scopedAPI = api;
            pythonScope.SetVariable("API", api);
            InjectClassicAssistGlobals(api, pythonScope);
        }

        private static void InjectClassicAssistGlobals(API api, Microsoft.Scripting.Hosting.ScriptScope scope)
        {
            scope.SetVariable("Msg", new Action<string>(api.Msg));
            scope.SetVariable("Say", new Action<string>(api.Say));
            scope.SetVariable("HeadMsg", new Action<string, uint>((msg, serial) => api.HeadMsg(msg, serial)));
            scope.SetVariable("SysMessage", new Action<string>(msg => api.SysMessage(msg)));
            scope.SetVariable("SysMsg", new Action<string>(msg => api.SysMsg(msg)));
            scope.SetVariable("PartyMsg", new Action<string>(api.PartyMsg));
            scope.SetVariable("GuildMsg", new Action<string>(api.GuildMsg));
            scope.SetVariable("AllyMsg", new Action<string>(api.AllyMsg));
            scope.SetVariable("WhisperMsg", new Action<string>(api.WhisperMsg));
            scope.SetVariable("YellMsg", new Action<string>(api.YellMsg));
            scope.SetVariable("EmoteMsg", new Action<string>(api.EmoteMsg));
            scope.SetVariable("Pause", new Action<double>(api.Pause));
            scope.SetVariable("UseObject", new Action<uint>(s => api.UseObject(s)));
            scope.SetVariable("ClickObject", new Action<uint>(api.ClickObject));
            scope.SetVariable("BandageSelf", new Func<bool>(api.BandageSelf));
            scope.SetVariable("Attack", new Action<uint>(api.Attack));
            scope.SetVariable("Target", new Action<uint>(api.Target));
            scope.SetVariable("WaitForTarget", new Action<int>(api.WaitForTarget));
            scope.SetVariable("RequestTarget", new Func<uint>(api.RequestTarget));
            scope.SetVariable("FindType", new Func<uint, dynamic>(g => api.FindType(g)));
            scope.SetVariable("FindLayer", new Func<string, dynamic>(layer => api.FindLayer(layer)));
            scope.SetVariable("MoveItem", new Action<uint, uint>((s, d) => api.MoveItem(s, d)));
            scope.SetVariable("UseSkill", new Action<string>(api.UseSkill));
            scope.SetVariable("Cast", new Action<string>(api.Cast));
            scope.SetVariable("CastSpell", new Action<string>(api.CastSpell));
            scope.SetVariable("ClearJournal", new Action(api.ClearJournal));
            scope.SetVariable("InJournal", new Func<string, bool>(api.InJournal));
            scope.SetVariable("WaitForJournal", new Func<string, double, bool>(api.WaitForJournal));
            scope.SetVariable("SetAlias", new Action<string, uint>(api.SetAlias));
            scope.SetVariable("GetAlias", new Func<string, uint>(api.GetAlias));
            scope.SetVariable("EquipItem", new Action<uint>(api.EquipItem));
            scope.SetVariable("ContextMenu", new Action<uint, ushort>(api.ContextMenu));
            scope.SetVariable("UseType", new Action<uint>(g => api.UseType(g)));
            scope.SetVariable("IgnoreObject", new Action<uint>(api.IgnoreObject));
            scope.SetVariable("ClearIgnoreList", new Action(api.ClearIgnoreList));
            scope.SetVariable("Pathfind", new Func<int, int, int, int, bool, int, bool>(api.Pathfind));
            scope.SetVariable("CancelPathfinding", new Action(api.CancelPathfinding));
            scope.SetVariable("Backpack", new Func<uint>(() => api.Backpack));
            scope.SetVariable("LastTarget", new Func<uint>(() => api.LastTargetSerial));
        }

        public void PythonScriptStopped()
        {
            scopedAPI?.CloseGumps();
            pythonScope = null;
            scopedAPI = null;
        }

        public bool FileExists()
        {
            return File.Exists(FullPath);
        }
    }
}
