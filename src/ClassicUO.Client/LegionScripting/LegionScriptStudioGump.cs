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
        private const int RIGHT_WIDTH = 280;
        private const int MIN_WIDTH = 900;
        private const int MIN_HEIGHT = 500;

        private static readonly Color DARK_BG = Color.FromNonPremultiplied(18, 5, 5, 255);
        private static readonly Color HEADER_BG = Color.FromNonPremultiplied(45, 12, 12, 255);
        private static readonly Color PANEL_BG = Color.FromNonPremultiplied(35, 10, 10, 255);
        private static readonly Color ACCENT_COLOR = Color.FromNonPremultiplied(180, 50, 50, 255);
        private static readonly Color TEXT_COLOR = Color.White;

        private AlphaBlendControl _leftBg;
        private ScrollArea _leftScroll;
        private AlphaBlendControl _centerBg;
        private ScrollArea _centerScroll;
        private LineNumberEditor _editorPanel;
        private AlphaBlendControl _rightBg;
        private ScrollArea _rightScroll;
        private TextBox _docLabel;
        private bool _showRunningPanel = true;
        private NiceButton _saveBtn;
        private NiceButton _playBtn;
        private NiceButton _stopBtn;
        private NiceButton _deleteBtn;
        private NiceButton _moveBtn;
        private NiceButton _runningBtn;
        private TextBox _titleLabel;
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
            int runWidth = 75;
            int moveWidth = 65;
            int delWidth = 55;
            int stdWidth = 60;
            int rightMargin = border + 16;
            int xRight = Width - rightMargin;

            int xRun = xRight - runWidth;
            int xMove = xRun - gap - moveWidth;
            int xDel = xMove - gap - delWidth;
            int xStop = xDel - gap - stdWidth;
            int xPlay = xStop - gap - stdWidth;
            int xSave = xPlay - gap - stdWidth;

            int titleLeft = border + 16;
            int titleMaxWidth = Math.Max(120, xSave - titleLeft - gap);

            Add(_titleLabel = new TextBox("Legion Script Studio", TrueTypeLoader.EMBEDDED_FONT, 20, titleMaxWidth, TEXT_COLOR, FontStashSharp.RichText.TextHorizontalAlignment.Left, false)
            {
                X = titleLeft,
                Y = border + 12,
                AcceptMouseInput = false
            });

            Add(_saveBtn = new NiceButton(xSave, btnY, stdWidth, 28, ButtonAction.Default, "Save") { IsSelectable = false });
            _saveBtn.MouseUp += OnSave;

            Add(_playBtn = new NiceButton(xPlay, btnY, stdWidth, 28, ButtonAction.Default, "Play") { IsSelectable = false });
            _playBtn.MouseUp += OnPlay;

            Add(_stopBtn = new NiceButton(xStop, btnY, stdWidth, 28, ButtonAction.Default, "Stop") { IsSelectable = false });
            _stopBtn.MouseUp += OnStop;

            Add(_deleteBtn = new NiceButton(xDel, btnY, delWidth, 28, ButtonAction.Default, "Delete") { IsSelectable = false });
            _deleteBtn.MouseUp += OnDelete;

            Add(_moveBtn = new NiceButton(xMove, btnY, moveWidth, 28, ButtonAction.Default, "Move") { IsSelectable = false });
            _moveBtn.MouseUp += OnMove;

            Add(_runningBtn = new NiceButton(xRun, btnY, runWidth, 28, ButtonAction.Default, "Running") { IsSelectable = true, IsSelected = true });
            _runningBtn.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;
                _showRunningPanel = !_showRunningPanel;
                _runningBtn.IsSelected = _showRunningPanel;
                RebuildRightPanel();
            };

            int leftX = border;
            int leftW = LEFT_WIDTH;
            int centerX = leftX + leftW + 4;
            int centerW = Width - leftW - RIGHT_WIDTH - border * 2 - 8;
            int rightX = centerX + centerW + 4;
            int rightW = RIGHT_WIDTH;
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
                        BuildScriptList();
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
                        BuildScriptList();
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
                        BuildScriptList();
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
                BuildScriptList();
                GameActions.Print("Script list refreshed.");
            };
            _leftScroll.Add(refreshBtn);

            BuildScriptList();

            Add(_centerBg = new AlphaBlendControl(0.3f) { X = centerX, Y = contentY, Width = centerW, Height = contentH, BaseColor = PANEL_BG });
            Add(_centerScroll = new ScrollArea(centerX, contentY, centerW, contentH, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });
            int editW = centerW - _centerScroll.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12;
            _centerScroll.Add(_editorPanel = new LineNumberEditor(editW, contentH - 20, "// Select a script or create new (.lscript .py .uos)") { X = 4, Y = 4 });
            _editorPanel.TextChanged += (s, e) =>
            {
                int h = _editorPanel.Editor.TextBox.TotalHeight > _centerScroll.Height ? _editorPanel.Editor.TextBox.TotalHeight : _centerScroll.Height;
                _editorPanel.UpdateSize(_centerScroll.Width - _centerScroll.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12, h);
            };

            Add(_rightBg = new AlphaBlendControl(0.5f) { X = rightX, Y = contentY, Width = rightW, Height = contentH, BaseColor = PANEL_BG });
            Add(_rightScroll = new ScrollArea(rightX, contentY, rightW, contentH, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });
            _docLabel = new TextBox(LoadDocForType(null), TrueTypeLoader.EMBEDDED_FONT, 12, rightW - 24, 0xFFFF, strokeEffect: false) { X = 4, Y = 4, AcceptMouseInput = false };
            RebuildRightPanel();

            LegionScripting.ScriptStartedEvent += OnScriptStarted;
            LegionScripting.ScriptStoppedEvent += OnScriptStopped;

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

        private void BuildScriptList()
        {
            while (_leftScroll.Children.Count > 5)
                _leftScroll.Remove(_leftScroll.Children[5]);
            var flat = new List<(string path, ScriptFile sf)>();
            foreach (ScriptFile sf in LegionScripting.LoadedScripts.OrderBy(s => Path.Combine(s.Group, s.SubGroup, s.FileName)))
            {
                string display = string.IsNullOrEmpty(sf.SubGroup) ? sf.FileName : $"{sf.SubGroup}/{sf.FileName}";
                if (!string.IsNullOrEmpty(sf.Group) && sf.Group != ScriptManagerGump.NOGROUPTEXT)
                    display = $"{sf.Group}/{display}";
                flat.Add((display, sf));
            }
            int y = 60;
            int w = _leftScroll.Width - (_leftScroll.ScrollBarWidth() > 0 ? _leftScroll.ScrollBarWidth() : 14) - 8;
            foreach (var (path, sf) in flat)
            {
                var btn = new Label(path, true, 0xFFFF, w, font: 1) { X = 4, Y = y, AcceptMouseInput = true };
                btn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                        SelectScript(sf);
                };
                _leftScroll.Add(btn);
                y += btn.Height + 2;
            }
        }

        private void RebuildRightPanel()
        {
            if (_rightScroll == null)
                return;

            while (_rightScroll.Children.Count > 1)
                _rightScroll.Remove(_rightScroll.Children[1]);

            int y = 4;

            if (_showRunningPanel)
            {
                var runningTitle = new TextBox("Running Scripts", TrueTypeLoader.EMBEDDED_FONT, 14, _rightScroll.Width - 24, 0xFFFF, strokeEffect: false)
                {
                    X = 4,
                    Y = y,
                    AcceptMouseInput = false
                };
                _rightScroll.Add(runningTitle);
                y += runningTitle.Height + 4;

                var running = LegionScripting.GetRunningScripts();
                foreach (var sf in running)
                {
                    string name = sf.FileName;
                    var label = new Label(name, true, 0xFFFF, _rightScroll.Width - 90, font: 1)
                    {
                        X = 4,
                        Y = y,
                        AcceptMouseInput = true
                    };
                    label.MouseUp += (s, e) =>
                    {
                        if (e.Button == MouseButtonType.Left)
                            SelectScript(sf);
                    };
                    _rightScroll.Add(label);

                    var stop = new NiceButton(_rightScroll.Width - 70, y - 2, 60, 22, ButtonAction.Default, "Stop")
                    {
                        IsSelectable = false
                    };
                    stop.MouseUp += (s, e) =>
                    {
                        if (e.Button != MouseButtonType.Left) return;
                        LegionScripting.StopScript(sf);
                        RebuildRightPanel();
                    };
                    _rightScroll.Add(stop);

                    y += label.Height + 4;
                }
                y += 8;
            }

            var docTitle = new TextBox("Documentation", TrueTypeLoader.EMBEDDED_FONT, 14, _rightScroll.Width - 24, 0xFFFF, strokeEffect: false)
            {
                X = 4,
                Y = y,
                AcceptMouseInput = false
            };
            _rightScroll.Add(docTitle);
            y += docTitle.Height + 4;

            if (_docLabel != null)
            {
                _docLabel.X = 4;
                _docLabel.Y = y;
                _rightScroll.Add(_docLabel);
            }
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
            if (_currentScript != null && _editorPanel.Text != string.Join(Environment.NewLine, _currentScript.FileContents))
            {
                try
                {
                    File.WriteAllText(_currentScript.FullPath, _editorPanel.Text);
                    _currentScript.ReadFromFile();
                }
                catch { }
            }
            _currentScript = sf;
            _editorPanel.SetText(string.Join("\n", sf.FileContents));
            _titleLabel.Text = $"Legion Script Studio - {sf.FileName}{GetLanguageBadge(sf.ScriptType)}";
            _docLabel.Text = LoadDocForType(sf.ScriptType);
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
                BuildScriptList();
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
                    _docLabel.Text = LoadDocForType(null);
                    LegionScripting.LoadScriptsFromFile();
                    BuildScriptList();
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
                    BuildScriptList();
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
            if (_showRunningPanel)
                RebuildRightPanel();
        }

        private void OnScriptStopped(object sender, ScriptInfoEvent ev)
        {
            UpdatePlayStopState();
            if (_showRunningPanel)
                RebuildRightPanel();
        }

        private void UpdatePlayStopState()
        {
            bool playing = _currentScript != null && _currentScript.IsPlaying;
            _playBtn.IsEnabled = _currentScript != null && !playing;
            _stopBtn.IsEnabled = playing;
        }

        public override void OnResize()
        {
            base.OnResize();
            if (_titleLabel == null) return;
            int border = BorderControl.BorderSize;
            int headerH = 48;
            int leftW = LEFT_WIDTH;
            int centerW = Width - leftW - RIGHT_WIDTH - border * 2 - 8;
            int rightW = RIGHT_WIDTH;
            int contentY = border + headerH;
            int contentH = Height - contentY - border;
            int centerX = border + leftW + 4;
            int rightX = centerX + centerW + 4;

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
            int runWidth = 75;
            int moveWidth = 65;
            int delWidth = 55;
            int stdWidth = 60;
            int rightMargin = border + 16;
            int xRight = Width - rightMargin;

            int xRun = xRight - runWidth;
            int xMove = xRun - gap - moveWidth;
            int xDel = xMove - gap - delWidth;
            int xStop = xDel - gap - stdWidth;
            int xPlay = xStop - gap - stdWidth;
            int xSave = xPlay - gap - stdWidth;

            int titleLeft = border + 16;
            int titleMaxWidth = Math.Max(120, xSave - titleLeft - gap);
            _titleLabel.X = titleLeft;
            _titleLabel.Width = titleMaxWidth;

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
            if (_runningBtn != null)
            {
                _runningBtn.X = xRun;
                _runningBtn.Y = btnY;
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

            _rightBg.X = rightX;
            _rightBg.Y = contentY;
            _rightBg.Width = rightW;
            _rightBg.Height = contentH;

            _rightScroll.X = rightX;
            _rightScroll.Y = contentY;
            _rightScroll.Width = rightW;
            _rightScroll.Height = contentH;
            _rightScroll.UpdateScrollbarPosition();

            _docLabel.Width = rightW - 24;
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
            if (_currentScript != null && _editorPanel != null && _editorPanel.Text != string.Join("\n", _currentScript.FileContents))
            {
                try
                {
                    File.WriteAllText(_currentScript.FullPath, _editorPanel.Text);
                }
                catch { }
            }
            base.Dispose();
        }
    }
}
