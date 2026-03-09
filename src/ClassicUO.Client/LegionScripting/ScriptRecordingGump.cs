using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using SDL3;
using static SDL3.SDL;

namespace ClassicUO.LegionScripting
{
    internal class ScriptRecordingGump : ResizableGump
    {
        private ScrollArea _scrollArea;
        private NiceButton _recordButton;
        private NiceButton _pauseButton;
        private NiceButton _clearButton;
        private NiceButton _copyButton;
        private NiceButton _saveButton;
        private Label _titleBar;
        private Label _statusText;
        private Label _durationText;
        private Label _actionCountText;
        private Checkbox _recordPausesCheckbox;
        private List<RecordedAction> _displayedActions = new List<RecordedAction>();

        private static int _lastX = 100, _lastY = 100;
        private static int _lastWidth = 400, _lastHeight = 500;
        private const int MIN_WIDTH = 350;
        private const int MIN_HEIGHT = 400;

        public ScriptRecordingGump() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            X = _lastX;
            Y = _lastY;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;
            BuildGump();
            SubscribeToRecorderEvents();
            UpdateUI();
        }

        private void BuildGump()
        {
            int border = BorderControl.BorderSize;
            int currentY = border + 10;

            _titleBar = new Label("Script Recording - Stopped", true, 52, Width - border * 2 - 20, font: 1) { X = border + 10, Y = currentY };
            Add(_titleBar);
            currentY += _titleBar.Height + 15;

            _recordButton = new NiceButton(border + 10, currentY, 80, 25, ButtonAction.Activate, "Record", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = (int)RecordingAction.ToggleRecord, DisplayBorder = true };
            _recordButton.MouseUp += OnButtonClick;
            Add(_recordButton);

            _pauseButton = new NiceButton(border + 100, currentY, 60, 25, ButtonAction.Activate, "Pause", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = (int)RecordingAction.Pause, IsEnabled = false, DisplayBorder = true };
            _pauseButton.MouseUp += OnButtonClick;
            Add(_pauseButton);

            _clearButton = new NiceButton(border + 170, currentY, 60, 25, ButtonAction.Activate, "Clear", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = (int)RecordingAction.Clear, DisplayBorder = true };
            _clearButton.MouseUp += OnButtonClick;
            Add(_clearButton);
            currentY += 35;

            _statusText = new Label("Status: Ready", true, 0xFFFF, Width - border * 2 - 20, font: 1) { X = border + 10, Y = currentY };
            Add(_statusText);
            currentY += _statusText.Height + 5;

            _durationText = new Label("Duration: 0:00", true, 999, Width - border * 2 - 20, font: 1) { X = border + 10, Y = currentY };
            Add(_durationText);
            currentY += _durationText.Height + 5;

            _actionCountText = new Label("Actions: 0", true, 999, Width - border * 2 - 20, font: 1) { X = border + 10, Y = currentY };
            Add(_actionCountText);
            currentY += _actionCountText.Height + 10;

            _recordPausesCheckbox = new Checkbox(0x00D2, 0x00D3, "Include pauses (timing delays)", 1, 0xFFFF) { X = border + 10, Y = currentY, IsChecked = true };
            Add(_recordPausesCheckbox);
            currentY += _recordPausesCheckbox.Height + 15;

            var actionListLabel = new Label("Recorded Actions:", true, 0x35, Width - border * 2 - 20, font: 1) { X = border + 10, Y = currentY };
            Add(actionListLabel);
            currentY += actionListLabel.Height + 5;

            int listHeight = Height - currentY - 80;
            _scrollArea = new ScrollArea(border + 10, currentY, Width - 2 * border - 20, listHeight, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView };
            Add(_scrollArea);

            int bottomY = Height - border - 35;
            _copyButton = new NiceButton(border + 10, bottomY, 100, 25, ButtonAction.Activate, "Copy Script", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = (int)RecordingAction.Copy, DisplayBorder = true };
            _copyButton.MouseUp += OnButtonClick;
            Add(_copyButton);

            _saveButton = new NiceButton(border + 120, bottomY, 100, 25, ButtonAction.Activate, "Save Script", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = (int)RecordingAction.Save, DisplayBorder = true };
            _saveButton.MouseUp += OnButtonClick;
            Add(_saveButton);
        }

        private void SubscribeToRecorderEvents()
        {
            ScriptRecorder.Instance.RecordingStateChanged += OnRecordingStateChanged;
            ScriptRecorder.Instance.ActionRecorded += OnActionRecorded;
        }

        private void UnsubscribeFromRecorderEvents()
        {
            ScriptRecorder.Instance.RecordingStateChanged -= OnRecordingStateChanged;
            ScriptRecorder.Instance.ActionRecorded -= OnActionRecorded;
        }

        private void OnRecordingStateChanged(object sender, EventArgs e) => UpdateUI();

        private void OnActionRecorded(object sender, RecordedAction action)
        {
            _displayedActions.Add(action);
            UpdateActionList();
            UpdateActionCount();
        }

        private void OnButtonClick(object sender, MouseEventArgs e)
        {
            if (sender is NiceButton button)
            {
                var action = (RecordingAction)button.ButtonParameter;
                switch (action)
                {
                    case RecordingAction.ToggleRecord:
                        if (ScriptRecorder.Instance.IsRecording)
                            ScriptRecorder.Instance.StopRecording();
                        else
                            ScriptRecorder.Instance.StartRecording();
                        break;
                    case RecordingAction.Pause:
                        if (ScriptRecorder.Instance.IsPaused)
                            ScriptRecorder.Instance.ResumeRecording();
                        else
                            ScriptRecorder.Instance.PauseRecording();
                        break;
                    case RecordingAction.Clear:
                        ScriptRecorder.Instance.ClearRecording();
                        _displayedActions.Clear();
                        UpdateActionList();
                        break;
                    case RecordingAction.Copy:
                        CopyScriptToClipboard();
                        break;
                    case RecordingAction.Save:
                        SaveScriptToFile();
                        break;
                }
            }
        }

        private void OnActionButtonClick(object sender, MouseEventArgs e)
        {
            if (sender is NiceButton button && button.Tag is string actionType)
            {
                int index = button.ButtonParameter;
                switch (actionType)
                {
                    case "delete": DeleteAction(index); break;
                    case "moveup": MoveActionUp(index); break;
                    case "movedown": MoveActionDown(index); break;
                }
            }
        }

        private void DeleteAction(int index)
        {
            if (index >= 0 && index < _displayedActions.Count)
            {
                _displayedActions.RemoveAt(index);
                ScriptRecorder.Instance.RemoveActionAt(index);
                UpdateActionList();
                UpdateActionCount();
            }
        }

        private void MoveActionUp(int index)
        {
            if (index > 0 && index < _displayedActions.Count)
            {
                var temp = _displayedActions[index];
                _displayedActions[index] = _displayedActions[index - 1];
                _displayedActions[index - 1] = temp;
                ScriptRecorder.Instance.SwapActions(index, index - 1);
                UpdateActionList();
            }
        }

        private void MoveActionDown(int index)
        {
            if (index >= 0 && index < _displayedActions.Count - 1)
            {
                var temp = _displayedActions[index];
                _displayedActions[index] = _displayedActions[index + 1];
                _displayedActions[index + 1] = temp;
                ScriptRecorder.Instance.SwapActions(index, index + 1);
                UpdateActionList();
            }
        }

        private void UpdateUI()
        {
            var recorder = ScriptRecorder.Instance;
            string status = recorder.IsRecording ? (recorder.IsPaused ? "Paused" : "Recording") : "Stopped";
            _titleBar.Text = $"Script Recording - {status}";
            _recordButton.TextLabel.Text = recorder.IsRecording ? "Stop" : "Record";
            _pauseButton.IsEnabled = recorder.IsRecording;
            _pauseButton.TextLabel.Text = recorder.IsPaused ? "Resume" : "Pause";
            _statusText.Text = $"Status: {status}";
            UpdateDuration();
            UpdateActionCount();
        }

        private void UpdateDuration()
        {
            uint duration = ScriptRecorder.Instance.RecordingDuration;
            uint seconds = duration / 1000;
            uint minutes = seconds / 60;
            seconds %= 60;
            _durationText.Text = $"Duration: {minutes}:{seconds:D2}";
        }

        private void UpdateActionCount() => _actionCountText.Text = $"Actions: {ScriptRecorder.Instance.ActionCount}";

        private void UpdateActionList()
        {
            _scrollArea.Clear();
            int listWidth = _scrollArea.Width - (_scrollArea.ScrollBarWidth() > 0 ? _scrollArea.ScrollBarWidth() : 14) - 10;
            for (int i = 0; i < _displayedActions.Count; i++)
            {
                var container = CreateActionRowContainer(_displayedActions[i], i, listWidth);
                container.Y = i * 27;
                _scrollArea.Add(container);
            }
            _scrollArea.ScrollMaxHeight = Math.Max(_scrollArea.Height, _displayedActions.Count * 27);
        }

        private Control CreateActionRowContainer(RecordedAction action, int index, int width)
        {
            var container = new HitBox(0, 0, width, 25, null, 0f);
            string actionText = FormatActionForDisplay(action);
            var actionLabel = new Label(actionText, true, 0xFFFF, width - 90, font: 1) { X = 0, Y = 2 };
            container.Add(actionLabel);
            var deleteButton = new NiceButton(width - 85, 2, 25, 20, ButtonAction.Activate, "×", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = index, DisplayBorder = true, Tag = "delete" };
            deleteButton.MouseUp += OnActionButtonClick;
            container.Add(deleteButton);
            var moveUpButton = new NiceButton(width - 57, 2, 25, 20, ButtonAction.Activate, "↑", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = index, DisplayBorder = true, IsEnabled = index > 0, Tag = "moveup" };
            moveUpButton.MouseUp += OnActionButtonClick;
            container.Add(moveUpButton);
            var moveDownButton = new NiceButton(width - 29, 2, 25, 20, ButtonAction.Activate, "↓", 0, Assets.TEXT_ALIGN_TYPE.TS_CENTER) { ButtonParameter = index, DisplayBorder = true, IsEnabled = index < _displayedActions.Count - 1, Tag = "movedown" };
            moveDownButton.MouseUp += OnActionButtonClick;
            container.Add(moveDownButton);
            return container;
        }

        private string FormatActionForDisplay(RecordedAction action)
        {
            switch (action.ActionType.ToLower())
            {
                case "walk":
                    var wd = action.Parameters.ContainsKey("direction") && action.Parameters["direction"] is Direction wdir ? Utility.GetDirectionString(wdir) : "?";
                    return $"Walk {wd}";
                case "run":
                    var rd = action.Parameters.ContainsKey("direction") && action.Parameters["direction"] is Direction rdir ? Utility.GetDirectionString(rdir) : "?";
                    return $"Run {rd}";
                case "cast": return action.Parameters.TryGetValue("spell", out var s) ? $"Cast \"{s}\"" : "Cast ?";
                case "say": var m = action.Parameters.TryGetValue("message", out var msg) ? msg.ToString() : "?"; return m.Length > 30 ? $"Say \"{m.Substring(0, 27)}...\"" : $"Say \"{m}\"";
                case "useitem": return action.Parameters.TryGetValue("serial", out var ser) ? $"Use Item 0x{ser:X8}" : "Use Item ?";
                case "dragdrop": var f = action.Parameters.TryGetValue("from", out var fr); var t = action.Parameters.TryGetValue("to", out var to); return f && t ? $"DragDrop 0x{fr:X8} → 0x{to:X8}" : "DragDrop ?";
                case "target": return action.Parameters.TryGetValue("serial", out var ts) ? $"Target 0x{ts:X8}" : "Target ?";
                case "targetlocation": var tx = action.Parameters.TryGetValue("x", out var x); var ty = action.Parameters.TryGetValue("y", out var y); var tz = action.Parameters.TryGetValue("z", out var z); return tx && ty && tz ? $"Target Loc ({x}, {y}, {z})" : "Target Loc ?";
                case "closecontainer": return action.Parameters.TryGetValue("serial", out var cs) ? $"Close 0x{cs:X8}" : "Close ?";
                case "attack": return action.Parameters.TryGetValue("serial", out var aser) ? $"Attack 0x{aser:X8}" : "Attack ?";
                case "bandageself": return "Bandage Self";
                case "contextmenu": return action.Parameters.TryGetValue("serial", out var cms) && action.Parameters.TryGetValue("index", out var ci) ? $"Context 0x{cms:X8} [{ci}]" : "Context ?";
                case "useskill": return action.Parameters.TryGetValue("skill", out var sk) ? $"Skill \"{sk}\"" : "Skill ?";
                case "equipitem": return action.Parameters.TryGetValue("serial", out var es) ? $"Equip 0x{es:X8}" : "Equip ?";
                case "replygump": return action.Parameters.TryGetValue("button", out var gb) ? $"Gump Btn {gb}" : "Gump ?";
                case "partymsg": case "guildmsg": case "allymsg": case "whispermsg": case "yellmsg": case "emotemsg":
                    var pm = action.Parameters.TryGetValue("message", out var pmt) ? pmt.ToString() : "?"; return pm.Length > 25 ? pm.Substring(0, 22) + "..." : pm;
                case "mount": return action.Parameters.TryGetValue("serial", out var ms) ? $"Mount 0x{ms:X8}" : "Mount ?";
                case "dismount": return "Dismount";
                case "ability": return action.Parameters.TryGetValue("ability", out var ab) ? $"Ability \"{ab}\"" : "Ability ?";
                case "virtue": return action.Parameters.TryGetValue("virtue", out var v) ? $"Virtue \"{v}\"" : "Virtue ?";
                case "waitforgump": return "Wait for gump";
                default: return $"{action.ActionType}(...)";
            }
        }

        private void CopyScriptToClipboard()
        {
            try
            {
                string script = ScriptRecorder.Instance.GenerateScript(_recordPausesCheckbox.IsChecked);
                SDL_SetClipboardText(script);
                GameActions.Print("Script copied to clipboard!");
            }
            catch (Exception ex) { GameActions.Print($"Failed to copy: {ex.Message}"); }
        }

        private void SaveScriptToFile()
        {
            try
            {
                string script = ScriptRecorder.Instance.GenerateScript(_recordPausesCheckbox.IsChecked);
                string fileName = $"recorded_script_{DateTime.Now:yyyyMMdd_HHmmss}.py";
                string filePath = Path.Combine(LegionScripting.ScriptPath, fileName);
                File.WriteAllText(filePath, script);
                GameActions.Print($"Script saved as {fileName}");
            }
            catch (Exception ex) { GameActions.Print($"Failed to save: {ex.Message}"); }
        }

        public override void Update()
        {
            base.Update();
            if (ScriptRecorder.Instance.IsRecording && !ScriptRecorder.Instance.IsPaused)
                UpdateDuration();
        }

        public override void OnResize()
        {
            base.OnResize();
            _lastX = X; _lastY = Y; _lastWidth = Width; _lastHeight = Height;
        }

        public override void Dispose()
        {
            UnsubscribeFromRecorderEvents();
            base.Dispose();
        }

        private enum RecordingAction { ToggleRecord, Pause, Clear, Copy, Save }
    }
}
