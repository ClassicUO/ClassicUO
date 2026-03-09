using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Xml;

namespace ClassicUO.LegionScripting
{
    internal class LegionScriptStudioGump : ResizableGump
    {
        private const int LEFT_WIDTH = 220;
        private const int MIN_WIDTH = 900;
        private const int MIN_HEIGHT = 500;

        private static readonly Color DARK_BG = Color.FromNonPremultiplied(18, 5, 5, 255);
        private static readonly Color HEADER_BG = Color.FromNonPremultiplied(45, 12, 12, 255);
        private static readonly Color PANEL_BG = Color.FromNonPremultiplied(35, 10, 10, 255);
        private static readonly Color ACCENT_COLOR = Color.FromNonPremultiplied(180, 50, 50, 255);
        private static readonly Color TEXT_COLOR = Color.White;

        private const int EXPLORER_GROUP_INDENT = 12;
        private const int EXPLORER_V_SPACING = 2;
        private AlphaBlendControl _leftBg;
        private ScrollArea _leftScroll;
        private UOLabel _statusLabel;
        private AlphaBlendControl _centerBg;
        private ScrollArea _centerScroll;
        private LineNumberEditor _editorPanel;
        private GothicStyleButton _saveBtn;
        private GothicStyleButton _playBtn;
        private GothicStyleButton _stopBtn;
        private GothicStyleButton _deleteBtn;
        private GothicStyleButton _moveBtn;
        private GothicStyleButton _docsBtn;
        private UOLabel _titleLabel;
        private RoundedColorBox _backgroundBox;
        private RoundedColorBox _headerBox;
        private RoundedColorBox _headerAccent;
        private ScriptFile _currentScript;
        private static int _lastX = -1;
        private static int _lastY = -1;
        private static int _lastWidth = 1000;
        private static int _lastHeight = 600;

        public override GumpType GumpType => GumpType.LegionScriptStudio;

        public LegionScriptStudioGump() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            X = _lastX;
            Y = _lastY;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;
            AnchorType = ANCHOR_TYPE.DISABLED;
            LegionScripting.LoadScriptsFromFile();

            int border = BorderControl.BorderSize;
            int headerH = 48;

            Add(_backgroundBox = new RoundedColorBox(Width - border * 2, Height - border * 2, DARK_BG, 14)
            {
                X = border,
                Y = border,
                AcceptMouseInput = false,
                CanMove = false,
                Alpha = 1f
            });

            Add(_headerBox = new RoundedColorBox(Width - border * 2, headerH, HEADER_BG, 0)
            {
                X = border,
                Y = border,
                AcceptMouseInput = false,
                CanMove = false,
                Alpha = 1f
            });

            Add(_headerAccent = new RoundedColorBox(Width - border * 2, 1, ACCENT_COLOR, 0)
            {
                X = border,
                Y = border + headerH - 1,
                AcceptMouseInput = false,
                Alpha = 0.6f
            });

            int btnY = border + headerH - 35;
            int gap = 6;
            int docsWidth = 50;
            int moveWidth = 65;
            int delWidth = 55;
            int stdWidth = 60;
            int rightMargin = border + 16;
            int xRight = Width - rightMargin;

            int xDocs = xRight - docsWidth;
            int xMove = xDocs - gap - moveWidth;
            int xDel = xMove - gap - delWidth;
            int xStop = xDel - gap - stdWidth;
            int xPlay = xStop - gap - stdWidth;
            int xSave = xPlay - gap - stdWidth;

            int titleLeft = border + 16;
            int titleMaxWidth = Math.Max(120, xSave - titleLeft - gap);

            Add(_titleLabel = new UOLabel("Legion Script Studio", 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_LEFT, titleMaxWidth) { X = titleLeft, Y = border + 10 });
            Add(_docsBtn = new GothicStyleButton(xDocs, btnY, docsWidth, 28, "Docs", null, 12));
            _docsBtn.OnClick += () => UIManager.Add(new DocumentationModalGump(
                "Documentation" + (_currentScript != null ? $" - {_currentScript.FileName}" : ""),
                LoadDocForType(_currentScript?.ScriptType)));
            Add(_statusLabel = new UOLabel("Ready", 1, 0x035F, Assets.TEXT_ALIGN_TYPE.TS_LEFT, titleMaxWidth) { X = titleLeft, Y = border + 28 });

            var saveBtn = new GothicStyleButton(xSave, btnY, stdWidth, 28, "Save", null, 12);
            saveBtn.OnClick += () => OnSave(null, new MouseEventArgs(0, 0, MouseButtonType.Left));
            Add(_saveBtn = saveBtn);

            var playBtn = new GothicStyleButton(xPlay, btnY, stdWidth, 28, "Play", null, 12);
            playBtn.OnClick += () => OnPlay(null, new MouseEventArgs(0, 0, MouseButtonType.Left));
            Add(_playBtn = playBtn);

            var stopBtn = new GothicStyleButton(xStop, btnY, stdWidth, 28, "Stop", null, 12);
            stopBtn.OnClick += () => OnStop(null, new MouseEventArgs(0, 0, MouseButtonType.Left));
            Add(_stopBtn = stopBtn);

            var deleteBtn = new GothicStyleButton(xDel, btnY, delWidth, 28, "Delete", null, 12);
            deleteBtn.OnClick += () => OnDelete(null, new MouseEventArgs(0, 0, MouseButtonType.Left));
            Add(_deleteBtn = deleteBtn);

            var moveBtn = new GothicStyleButton(xMove, btnY, moveWidth, 28, "Move", null, 12);
            moveBtn.OnClick += () => OnMove(null, new MouseEventArgs(0, 0, MouseButtonType.Left));
            Add(_moveBtn = moveBtn);

            int leftX = border;
            int leftW = LEFT_WIDTH;
            int centerX = leftX + leftW + 4;
            int centerW = Width - leftW - border * 2 - 8;
            int contentY = border + headerH;
            int contentH = Height - contentY - border;

            Add(_leftBg = new AlphaBlendControl(0.5f) { X = leftX, Y = contentY, Width = leftW, Height = contentH, BaseColor = PANEL_BG });
            Add(_leftScroll = new ScrollArea(leftX, contentY, leftW, contentH, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });
            var newBtn = new NiceButton(4, 4, (leftW - 12) / 2 - 2, 24, ButtonAction.Default, "Create") { IsSelectable = false };
            newBtn.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;
                var req = new InputRequest("Enter name (e.g. script.lscript, Group/script.py)", "Create", "Cancel", (r, input) =>
                {
                    if (r != InputRequest.Result.BUTTON1 || string.IsNullOrWhiteSpace(input)) return;
                    input = input.Trim().Replace("\\", "/");
                    string ext = ".lscript";
                    string baseName = input;
                    if (input.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                    {
                        ext = ".py";
                        baseName = input.Substring(0, input.Length - 3).TrimEnd('/');
                    }
                    else if (input.EndsWith(".uos", StringComparison.OrdinalIgnoreCase))
                    {
                        ext = ".uos";
                        baseName = input.Substring(0, input.Length - 4).TrimEnd('/');
                    }
                    else if (input.EndsWith(".lscript", StringComparison.OrdinalIgnoreCase))
                    {
                        baseName = input.Substring(0, input.Length - 8).TrimEnd('/');
                    }
                    else if (input.IndexOf('.') >= 0)
                        baseName = input.Substring(0, input.IndexOf('.')).TrimEnd('/');
                    baseName = string.Join("/", baseName.Split('/').Select(p => new string(p.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray())));
                    if (string.IsNullOrWhiteSpace(Path.GetFileName(baseName)))
                    {
                        GameActions.Print("Invalid script name.");
                        return;
                    }
                    string dirPath = Path.GetDirectoryName(baseName);
                    string fileName = Path.GetFileName(baseName) + ext;
                    string fullDir = string.IsNullOrEmpty(dirPath) ? LegionScripting.ScriptPath : Path.Combine(LegionScripting.ScriptPath, dirPath);
                    string fullPath = Path.Combine(fullDir, fileName);
                    if (File.Exists(fullPath))
                    {
                        GameActions.Print($"Script already exists: {fileName}");
                        return;
                    }
                    try
                    {
                        if (!Directory.Exists(fullDir))
                            Directory.CreateDirectory(fullDir);
                        string defaultContent = ext == ".py" ? "# My script" : ext == ".uos" ? "// My UOS script" : "// My script";
                        File.WriteAllText(fullPath, defaultContent);
                        LegionScripting.LoadScriptsFromFile();
                        BuildScriptTree();
                        var sf = LegionScripting.LoadedScripts.FirstOrDefault(x => string.Equals(x.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));
                        if (sf != null) SelectScript(sf);
                        GameActions.Print($"Created {fileName}");
                    }
                    catch (Exception ex)
                    {
                        GameActions.Print(ex.ToString());
                    }
                });
                req.CenterXInScreen();
                req.CenterYInScreen();
                UIManager.Add(req);
            };
            _leftScroll.Add(newBtn);
            var newGroupBtn = new NiceButton(6 + (leftW - 12) / 2, 4, (leftW - 12) / 2 - 2, 24, ButtonAction.Default, "New Group") { IsSelectable = false };
            newGroupBtn.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;
                var req = new InputRequest("Enter group name", "Create", "Cancel", (r, input) =>
                {
                    if (r != InputRequest.Result.BUTTON1 || string.IsNullOrWhiteSpace(input)) return;
                    input = input.Trim().Replace(".", "").Replace("/", "").Replace("\\", "");
                    if (string.IsNullOrEmpty(input)) return;
                    string path = Path.Combine(LegionScripting.ScriptPath, input);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        File.WriteAllText(
                            Path.Combine(path, "Example.lscript"),
                            "// LegionScript sample\nmsg HelloWorldFromLegionScript\npause 1000\nloop"
                        );
                        LegionScripting.LoadScriptsFromFile();
                        BuildScriptTree();
                    }
                });
                req.CenterXInScreen();
                req.CenterYInScreen();
                UIManager.Add(req);
            };
            _leftScroll.Add(newGroupBtn);

            var loadFolderBtn = new NiceButton(4, 32, (leftW - 12) / 2 - 2, 24, ButtonAction.Default, "Load folder") { IsSelectable = false };
            loadFolderBtn.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;
                var req = new InputRequest("Enter folder path to load scripts from", "Load", "Cancel", (r, input) =>
                {
                    if (r != InputRequest.Result.BUTTON1 || string.IsNullOrWhiteSpace(input)) return;
                    input = input.Trim();
                    try
                    {
                        LegionScripting.LoadScriptsFromDirectory(input);
                        BuildScriptTree();
                        GameActions.Print($"Loaded scripts from: {input}");
                    }
                    catch (Exception ex)
                    {
                        GameActions.Print(ex.Message);
                    }
                });
                req.CenterXInScreen();
                req.CenterYInScreen();
                UIManager.Add(req);
            };
            _leftScroll.Add(loadFolderBtn);

            var refreshBtn = new NiceButton(6 + (leftW - 12) / 2, 32, (leftW - 12) / 2 - 2, 24, ButtonAction.Default, "Refresh") { IsSelectable = false };
            refreshBtn.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;
                LegionScripting.LoadScriptsFromFile();
                BuildScriptTree();
                GameActions.Print("Script list refreshed.");
            };
            _leftScroll.Add(refreshBtn);

            BuildScriptTree();

            Add(_centerBg = new AlphaBlendControl(0.3f) { X = centerX, Y = contentY, Width = centerW, Height = contentH, BaseColor = PANEL_BG });
            Add(_centerScroll = new ScrollArea(centerX, contentY, centerW, contentH, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });
            int editW = centerW - _centerScroll.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12;
            _centerScroll.Add(_editorPanel = new LineNumberEditor(editW, contentH - 20, "// Select a script or create new (.lscript .py .uos)") { X = 4, Y = 4 });
            _editorPanel.TextChanged += (s, e) =>
            {
                int h = _editorPanel.Editor.TextBox.TotalHeight > _centerScroll.Height ? _editorPanel.Editor.TextBox.TotalHeight : _centerScroll.Height;
                _editorPanel.UpdateSize(_centerScroll.Width - _centerScroll.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12, h);
            };

            RebuildRightPanel();

            LegionScripting.ScriptStartedEvent += OnScriptStarted;
            LegionScripting.ScriptStoppedEvent += OnScriptStopped;
            UpdatePlayStopState();

            if (_lastX == -1 && _lastY == -1)
            {
                CenterXInViewPort();
                CenterYInViewPort();
            }
        }

        private static string LoadDocForType(ScriptType? type)
        {
            string resSuffix = type switch
            {
                ScriptType.LegionScript => "LScript.md",
                ScriptType.Python => "Python.md",
                ScriptType.UOScript => "UOS.md",
                _ => "LScript.md"
            };
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                string resName = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith(resSuffix));
                using var stream = resName != null ? asm.GetManifestResourceStream(resName) : null;
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                string baseDir = Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts");
                string path = Path.Combine(Path.GetDirectoryName(baseDir), resSuffix);
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }
            catch { }
            return type switch
            {
                ScriptType.Python => "# Python Scripting\n\nIronPython scripts. Use `import API` or the built-in ClassicAssist-style globals.\n\n**ClassicAssist compatibility:** Scripts from [ClassicAssist-Macros](https://github.com/Reetus/ClassicAssist-Macros) can run as .py files. Globals: Msg, HeadMsg, Pause, UseObject, BandageSelf, FindType, SetAlias, GetAlias, Target, WaitForTarget, MoveItem, Cast, InJournal, WaitForJournal, etc.\n\nExample (API):\n```python\nimport API\nAPI.Msg('Hello')\nAPI.Walk('North')\n```\n\nExample (ClassicAssist style):\n```python\nMsg('Hello')\nPause(1)\nif FindType(0x0E21):\n  UseObject(GetAlias('found'))\n```",
                ScriptType.UOScript => "# UOScript (UO Steam)\n\nCompatible with UO Steam/Razor scripts. Commands: msg, useobject, findtype, setalias, waitfortarget, etc.\n\nExample:\n```\nmsg 'Hello'\nif findtype 0x1bdd 'backpack'\n  useobject 'found'\n  pause 1000\nendif\n```",
                _ => "# Legion Scripting\n\nDocumentation not found. See LScript.md."
            };
        }

        private void BuildScriptTree()
        {
            while (_leftScroll.Children.Count > 5)
                _leftScroll.Remove(_leftScroll.Children[5]);
            var groupsMap = new Dictionary<string, Dictionary<string, List<ScriptFile>>>
            {
                { "", new Dictionary<string, List<ScriptFile>> { { "", new List<ScriptFile>() } } }
            };
            foreach (ScriptFile sf in LegionScripting.LoadedScripts)
            {
                string g = string.IsNullOrEmpty(sf.Group) || sf.Group == ScriptManagerGump.NOGROUPTEXT ? "" : sf.Group;
                if (!groupsMap.ContainsKey(g))
                    groupsMap[g] = new Dictionary<string, List<ScriptFile>>();
                string sg = sf.SubGroup ?? "";
                if (!groupsMap[g].ContainsKey(sg))
                    groupsMap[g][sg] = new List<ScriptFile>();
                groupsMap[g][sg].Add(sf);
            }
            int y = 60;
            int w = Math.Max(100, _leftScroll.Width - (_leftScroll.ScrollBarWidth() > 0 ? _leftScroll.ScrollBarWidth() : 14) - 12);
            foreach (var group in groupsMap)
            {
                var gc = new ScriptExplorerGroupControl(
                    group.Key == "" ? "Scripts" : group.Key,
                    w,
                    "",
                    (sf) => SelectScript(sf),
                    () => BuildScriptTree());
                gc.GroupExpandedShrunk += OnExplorerGroupExpanded;
                gc.AddGroups(group.Value);
                gc.X = 4;
                gc.Y = y;
                _leftScroll.Add(gc);
                y += gc.Height + EXPLORER_V_SPACING;
            }
        }

        private void OnExplorerGroupExpanded(object sender, EventArgs e)
        {
            RepositionExplorer();
        }

        private void RepositionExplorer()
        {
            int y = 60;
            int sbW = _leftScroll.ScrollBarWidth() > 0 ? _leftScroll.ScrollBarWidth() : 14;
            int w = Math.Max(100, _leftScroll.Width - sbW - 12);
            foreach (Control c in _leftScroll.Children)
            {
                if (c is ScrollBarBase || c is NiceButton || c is GothicStyleButton)
                    continue;
                if (c is ScriptExplorerGroupControl gc)
                {
                    gc.Y = y;
                    gc.UpdateSize(w);
                    y += gc.Height + EXPLORER_V_SPACING;
                }
            }
            _leftScroll.UpdateScrollbarPosition();
        }

        private void RebuildRightPanel()
        {
        }

        private static string GetLanguageBadge(ScriptType type) => type switch
        {
            ScriptType.LegionScript => " [LScript]",
            ScriptType.Python => " [Python]",
            ScriptType.UOScript => " [UOS]",
            _ => ""
        };

        private void SelectScript(ScriptFile sf)
        {
            if (_currentScript != null && _editorPanel.Text != string.Join("\n", _currentScript.FileContents ?? Array.Empty<string>()))
            {
                try
                {
                    File.WriteAllText(_currentScript.FullPath, _editorPanel.Text);
                    _currentScript.ReadFromFile();
                }
                catch { }
            }
            _currentScript = sf;
            _editorPanel.SetText(string.Join("\n", sf.FileContents ?? Array.Empty<string>()));
            _titleLabel.Text = $"Legion Script Studio - {sf.FileName}{GetLanguageBadge(sf.ScriptType)}";
            UpdatePlayStopState();
            int h = _editorPanel.Editor.TextBox.TotalHeight > _centerScroll.Height ? _editorPanel.Editor.TextBox.TotalHeight : _centerScroll.Height;
            _editorPanel.UpdateSize(_centerScroll.Width - _centerScroll.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12, h);
            RebuildRightPanel();
        }

        private void OnSave(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left) return;
            if (_currentScript == null)
            {
                GameActions.Print("No script selected.");
                return;
            }
            try
            {
                File.WriteAllText(_currentScript.FullPath, _editorPanel.Text);
                _currentScript.ReadFromFile();
                LegionScripting.LoadScriptsFromFile();
                BuildScriptTree();
                GameActions.Print($"Saved {_currentScript.FileName}.");
            }
            catch (Exception ex)
            {
                GameActions.Print(ex.ToString());
            }
        }

        private void OnPlay(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left) return;
            if (_currentScript == null)
            {
                GameActions.Print("No script selected.");
                return;
            }
            OnSave(sender, e);
            LegionScripting.PlayScript(_currentScript);
            UpdatePlayStopState();
        }

        private void OnStop(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left) return;
            if (_currentScript == null) return;
            LegionScripting.StopScript(_currentScript);
            UpdatePlayStopState();
        }

        private void OnDelete(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left) return;
            if (_currentScript == null)
            {
                GameActions.Print("No script selected.");
                return;
            }
            var g = new QuestionGump($"Delete {_currentScript.FileName}?", (r) =>
            {
                if (!r) return;
                try
                {
                    LegionScripting.StopScript(_currentScript);
                    LegionScripting.LoadedScripts.Remove(_currentScript);
                    File.Delete(_currentScript.FullPath);
                    _currentScript = null;
                    _editorPanel.SetText("// Select a script or create new (.lscript .py .uos)");
                    _titleLabel.Text = "Legion Script Studio";
                    LegionScripting.LoadScriptsFromFile();
                    BuildScriptTree();
                    UpdatePlayStopState();
                    GameActions.Print("Script deleted.");
                }
                catch (Exception ex)
                {
                    GameActions.Print(ex.ToString());
                }
            });
            UIManager.Add(g);
        }

        private void OnMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left) return;
            if (_currentScript == null)
            {
                GameActions.Print("No script selected.");
                return;
            }
            var req = new InputRequest("Enter target group (e.g. Group or Group/Sub)", "Move", "Cancel", (r, input) =>
            {
                if (r != InputRequest.Result.BUTTON1 || _currentScript == null) return;
                input = input?.Trim();
                if (string.IsNullOrWhiteSpace(input))
                    return;
                input = input.Replace("\\", "/").Replace(".", "");
                string[] parts = input.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return;
                string targetDir = Path.Combine(LegionScripting.ScriptPath, Path.Combine(parts));
                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);
                    string newPath = Path.Combine(targetDir, _currentScript.FileName);
                    if (string.Equals(newPath, _currentScript.FullPath, StringComparison.OrdinalIgnoreCase))
                        return;
                    if (File.Exists(newPath))
                    {
                        GameActions.Print("A script with this name already exists in target group.");
                        return;
                    }
                    File.Move(_currentScript.FullPath, newPath);
                    LegionScripting.LoadScriptsFromFile();
                    BuildScriptTree();
                    var sf = LegionScripting.LoadedScripts.FirstOrDefault(x => string.Equals(x.FullPath, newPath, StringComparison.OrdinalIgnoreCase));
                    if (sf != null)
                        SelectScript(sf);
                    GameActions.Print("Script moved.");
                }
                catch (Exception ex)
                {
                    GameActions.Print(ex.ToString());
                }
            });
            req.CenterXInScreen();
            req.CenterYInScreen();
            UIManager.Add(req);
        }

        private void OnScriptStarted(object sender, ScriptInfoEvent ev)
        {
            UpdatePlayStopState();
        }

        private void OnScriptStopped(object sender, ScriptInfoEvent ev)
        {
            UpdatePlayStopState();
        }

        private void UpdatePlayStopState()
        {
            bool playing = _currentScript != null && _currentScript.IsPlaying;
            _playBtn.IsEnabled = _currentScript != null;
            _stopBtn.IsEnabled = _currentScript != null && playing;
            _playBtn.Alpha = _playBtn.IsEnabled ? 1f : 0.5f;
            _stopBtn.Alpha = _stopBtn.IsEnabled ? 1f : 0.5f;
            var running = LegionScripting.GetRunningScripts();
            if (_statusLabel != null)
                _statusLabel.Text = running.Count > 0 ? $"● {running.Count} running" : "Ready";
        }

        public override void OnResize()
        {
            base.OnResize();
            if (_titleLabel == null) return;
            int border = BorderControl.BorderSize;
            int headerH = 48;
            int leftW = LEFT_WIDTH;
            int centerW = Width - leftW - border * 2 - 8;
            int contentY = border + headerH;
            int contentH = Height - contentY - border;
            int centerX = border + leftW + 4;

            if (_backgroundBox != null)
            {
                _backgroundBox.X = border;
                _backgroundBox.Y = border;
                _backgroundBox.Width = Width - border * 2;
                _backgroundBox.Height = Height - border * 2;
            }

            if (_headerBox != null)
            {
                _headerBox.X = border;
                _headerBox.Y = border;
                _headerBox.Width = Width - border * 2;
                _headerBox.Height = headerH;
            }

            if (_headerAccent != null)
            {
                _headerAccent.X = border;
                _headerAccent.Y = border + headerH - 1;
                _headerAccent.Width = Width - border * 2;
                _headerAccent.Height = 1;
            }

            int btnY = border + headerH - 35;
            int gap = 6;
            int docsWidth = 50;
            int moveWidth = 65;
            int delWidth = 55;
            int stdWidth = 60;
            int rightMargin = border + 16;
            int xRight = Width - rightMargin;

            int xDocs = xRight - docsWidth;
            int xMove = xDocs - gap - moveWidth;
            int xDel = xMove - gap - delWidth;
            int xStop = xDel - gap - stdWidth;
            int xPlay = xStop - gap - stdWidth;
            int xSave = xPlay - gap - stdWidth;

            int titleLeft = border + 16;
            int titleMaxWidth = Math.Max(120, xSave - titleLeft - gap);
            _titleLabel.X = titleLeft;
            _titleLabel.Width = titleMaxWidth;
            if (_statusLabel != null)
            {
                _statusLabel.X = titleLeft;
                _statusLabel.Y = border + 28;
                _statusLabel.Width = titleMaxWidth;
            }

            if (_saveBtn != null)
            {
                _saveBtn.X = xSave;
                _saveBtn.Y = btnY;
            }
            if (_playBtn != null)
            {
                _playBtn.X = xPlay;
                _playBtn.Y = btnY;
            }
            if (_stopBtn != null)
            {
                _stopBtn.X = xStop;
                _stopBtn.Y = btnY;
            }
            if (_deleteBtn != null)
            {
                _deleteBtn.X = xDel;
                _deleteBtn.Y = btnY;
            }
            if (_moveBtn != null)
            {
                _moveBtn.X = xMove;
                _moveBtn.Y = btnY;
            }
            if (_docsBtn != null)
            {
                _docsBtn.X = xDocs;
                _docsBtn.Y = btnY;
            }
            _leftBg.X = border;
            _leftBg.Y = contentY;
            _leftBg.Width = leftW;
            _leftBg.Height = contentH;

            _leftScroll.X = border;
            _leftScroll.Y = contentY;
            _leftScroll.Width = leftW;
            _leftScroll.Height = contentH;
            _leftScroll.UpdateScrollbarPosition();

            _centerBg.X = centerX;
            _centerBg.Y = contentY;
            _centerBg.Width = centerW;
            _centerBg.Height = contentH;

            _centerScroll.X = centerX;
            _centerScroll.Y = contentY;
            _centerScroll.Width = centerW;
            _centerScroll.Height = contentH;
            _centerScroll.UpdateScrollbarPosition();

            int editW = centerW - _centerScroll.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12;
            int editH = contentH > 50 ? contentH - 20 : 200;
            if (_editorPanel != null && _editorPanel.Editor.TextBox.TotalHeight > editH) editH = _editorPanel.Editor.TextBox.TotalHeight;
            _editorPanel?.UpdateSize(editW, editH);
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            _lastX = X;
            _lastY = Y;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("rw", Width.ToString());
            writer.WriteAttributeString("rh", Height.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            if (int.TryParse(xml.GetAttribute("rw"), out int w) && w >= MIN_WIDTH &&
                int.TryParse(xml.GetAttribute("rh"), out int h) && h >= MIN_HEIGHT)
            {
                ResizeWindow(new Point(w, h));
                OnResize();
            }
        }

        public override void Dispose()
        {
            LegionScripting.ScriptStartedEvent -= OnScriptStarted;
            LegionScripting.ScriptStoppedEvent -= OnScriptStopped;
            if (_currentScript != null && _editorPanel != null && _currentScript.FileContents != null && _editorPanel.Text != string.Join("\n", _currentScript.FileContents))
            {
                try
                {
                    File.WriteAllText(_currentScript.FullPath, _editorPanel.Text);
                }
                catch { }
            }
            base.Dispose();
        }

        private sealed class ScriptExplorerGroupControl : Control
        {
            public event EventHandler GroupExpandedShrunk;
            private readonly NiceButton _expand;
            private readonly UOLabel _label;
            private readonly DataBox _dataBox;
            private readonly string _group;
            private readonly string _parentGroup;
            private readonly Action<ScriptFile> _onSelect;
            private readonly Action _onFolderChanged;
            private const int HEIGHT = 22;

            public ScriptExplorerGroupControl(string group, int width, string parentGroup, Action<ScriptFile> onSelect, Action onFolderChanged = null)
            {
                Width = width;
                Height = HEIGHT;
                _group = group;
                _parentGroup = parentGroup;
                _onSelect = onSelect;
                _onFolderChanged = onFolderChanged;
                CanMove = false;
                AcceptMouseInput = true;
                _dataBox = new DataBox(0, HEIGHT, width, 0);
                _dataBox.IsVisible = parentGroup == ""
                    ? !LegionScripting.IsGroupCollapsed(group == "Scripts" ? "" : group)
                    : !LegionScripting.IsGroupCollapsed(parentGroup, group);

                _expand = new NiceButton(0, 0, 22, HEIGHT, ButtonAction.Default, _dataBox.IsVisible ? "−" : "+") { IsSelectable = false };
                _expand.MouseDown += (s, e) =>
                {
                    if (e.Button != MouseButtonType.Left) return;
                    _dataBox.IsVisible = !_dataBox.IsVisible;
                    string gKey = group == "Scripts" ? "" : group;
                    if (parentGroup == "")
                        LegionScripting.SetGroupCollapsed(gKey, expanded: _dataBox.IsVisible);
                    else
                        LegionScripting.SetGroupCollapsed(parentGroup, group, _dataBox.IsVisible);
                    _expand.TextLabel.Text = _dataBox.IsVisible ? "−" : "+";
                    ForceSizeUpdate(false);
                    GroupExpandedShrunk?.Invoke(this, EventArgs.Empty);
                };

                _label = new UOLabel(group + "  ", 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_LEFT, width - 30) { AcceptMouseInput = false };
                _label.X = _expand.Width;
                _label.Y = (HEIGHT - _label.Height) / 2;

                Add(new AlphaBlendControl(0.35f) { Height = HEIGHT, Width = width });
                Add(_expand);
                Add(_label);
                Add(_dataBox);

                bool isRoot = group == "Scripts" || (parentGroup == "" && (group == "" || group == ScriptManagerGump.NOGROUPTEXT));
                if (!isRoot && onFolderChanged != null)
                {
                    ContextMenu = new ContextMenuControl();
                    ContextMenu.Add(new ContextMenuItemEntry("Rename folder", () =>
                    {
                        string fullPath = GetFolderFullPath();
                        string currentName = Path.GetFileName(fullPath);
                        UIManager.Add(new RenameFolderModalGump(currentName, (input) =>
                        {
                            if (string.IsNullOrWhiteSpace(input)) return;
                            input = input.Trim().Replace("/", "").Replace("\\", "");
                            if (string.IsNullOrEmpty(input)) return;
                            string parentDir = Path.GetDirectoryName(fullPath);
                            string newPath = Path.Combine(parentDir ?? "", input);
                            if (Directory.Exists(newPath))
                            {
                                GameActions.Print("A folder with this name already exists.");
                                return;
                            }
                            try
                            {
                                Directory.Move(fullPath, newPath);
                                LegionScripting.LoadScriptsFromFile();
                                _onFolderChanged?.Invoke();
                                GameActions.Print($"Folder renamed to {input}");
                            }
                            catch (Exception ex)
                            {
                                GameActions.Print(ex.Message);
                            }
                        }));
                    }));
                    ContextMenu.Add(new ContextMenuItemEntry("Delete folder", () =>
                    {
                        var q = new QuestionGump("Delete this folder and all scripts inside?", (r) =>
                        {
                            if (!r) return;
                            try
                            {
                                string fullPath = GetFolderFullPath();
                                foreach (var sf in LegionScripting.LoadedScripts.ToList())
                                    if (sf.FullPath.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
                                        LegionScripting.StopScript(sf);
                                Directory.Delete(fullPath, true);
                                LegionScripting.LoadScriptsFromFile();
                                _onFolderChanged?.Invoke();
                                GameActions.Print("Folder deleted.");
                            }
                            catch (Exception ex)
                            {
                                GameActions.Print(ex.Message);
                            }
                        });
                        UIManager.Add(q);
                    }));
                    MouseDown += (s, e) =>
                    {
                        if (e.Button == MouseButtonType.Right)
                            ContextMenu.Show();
                    };
                }

                ForceSizeUpdate();
            }

            private string GetFolderFullPath()
            {
                string g = _group == "Scripts" ? "" : _group;
                string rel = string.IsNullOrEmpty(_parentGroup) ? g : Path.Combine(_parentGroup, g);
                return string.IsNullOrEmpty(rel) ? LegionScripting.ScriptPath : Path.Combine(LegionScripting.ScriptPath, rel);
            }

            public void UpdateSize(int width)
            {
                Width = width;
                foreach (Control c in _dataBox.Children)
                {
                    if (c is ScriptExplorerGroupControl gc)
                        gc.UpdateSize(width - EXPLORER_GROUP_INDENT);
                    else if (c is ScriptExplorerItemControl ic)
                        ic.UpdateSize(width);
                }
                _dataBox.ForceSizeUpdate(false);
                ForceSizeUpdate(false);
            }

            public void AddGroups(Dictionary<string, List<ScriptFile>> groups)
            {
                string gKey = _group == "Scripts" ? "" : _group;
                foreach (var obj in groups)
                {
                    if (!string.IsNullOrEmpty(obj.Key))
                    {
                        var subG = new ScriptExplorerGroupControl(obj.Key, Width - EXPLORER_GROUP_INDENT, gKey, _onSelect, _onFolderChanged)
                        { X = EXPLORER_GROUP_INDENT };
                        subG.AddItems(obj.Value);
                        subG.GroupExpandedShrunk += (s, e) =>
                        {
                            _dataBox.ReArrangeChildren(EXPLORER_V_SPACING);
                            _dataBox.ForceSizeUpdate(false);
                            ForceSizeUpdate(false);
                            GroupExpandedShrunk?.Invoke(this, EventArgs.Empty);
                        };
                        _dataBox.Add(subG);
                    }
                    else
                    {
                        AddItems(obj.Value);
                    }
                }
                _dataBox.ReArrangeChildren(EXPLORER_V_SPACING);
                _dataBox.ForceSizeUpdate();
                ForceSizeUpdate();
            }

            public void AddItems(List<ScriptFile> files)
            {
                foreach (ScriptFile file in files)
                    _dataBox.Add(new ScriptExplorerItemControl(Width, file, _onSelect));
                _dataBox.ReArrangeChildren(EXPLORER_V_SPACING);
                _dataBox.ForceSizeUpdate();
                ForceSizeUpdate();
            }
        }

        private sealed class ScriptExplorerItemControl : Control
        {
            private readonly AlphaBlendControl _bg;
            private readonly UOLabel _label;
            private readonly ScriptFile _script;
            private readonly Action<ScriptFile> _onSelect;
            private const int HEIGHT = 20;

            public ScriptExplorerItemControl(int w, ScriptFile script, Action<ScriptFile> onSelect)
            {
                Width = w;
                Height = HEIGHT;
                _script = script;
                _onSelect = onSelect;
                CanMove = false;
                AcceptMouseInput = true;
                _bg = new AlphaBlendControl(0.3f) { Height = HEIGHT, Width = w };
                Add(_bg);
                _label = new UOLabel(Path.GetFileNameWithoutExtension(script.FileName), 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_LEFT, w - 24) { AcceptMouseInput = false };
                _label.X = 20;
                _label.Y = (HEIGHT - _label.Height) / 2;
                Add(_label);
                MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                        _onSelect?.Invoke(_script);
                };
                ContextMenu = new ContextMenuControl();
                ContextMenu.Add(new ContextMenuItemEntry("Edit", () => _onSelect?.Invoke(_script)));
                ContextMenu.Add(new ContextMenuItemEntry("Run", () => { if (_script.GetScript != null && !LegionScripting.IsScriptRunning(_script)) LegionScripting.PlayScript(_script); }));
                ContextMenu.Add(new ContextMenuItemEntry("Stop", () => { if (LegionScripting.IsScriptRunning(_script)) LegionScripting.StopScript(_script); }));
                MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Right)
                        ContextMenu?.Show();
                };
                LegionScripting.ScriptStartedEvent += OnScriptStateChanged;
                LegionScripting.ScriptStoppedEvent += OnScriptStateChanged;
                UpdateRunningState();
            }

            private void OnScriptStateChanged(object sender, ScriptInfoEvent e)
            {
                UpdateRunningState();
            }

            private void UpdateRunningState()
            {
                bool running = LegionScripting.IsScriptRunning(_script);
                _bg.BaseColor = running ? Color.FromNonPremultiplied(30, 80, 30, 255) : Color.FromNonPremultiplied(25, 25, 30, 255);
            }

            public void UpdateSize(int w)
            {
                Width = w;
                _bg.Width = w;
                _label.Width = w - 24;
                UpdateRunningState();
            }

            public override void Dispose()
            {
                LegionScripting.ScriptStartedEvent -= OnScriptStateChanged;
                LegionScripting.ScriptStoppedEvent -= OnScriptStateChanged;
                base.Dispose();
            }
        }
    }
}
