using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Game.UI.Gumps
{
    internal partial class OptionsGump
    {
        private const int ACTION_BAR_PAGE = 14;
        private List<OptionsGumpActionBarSlotControl> _actionBarSlotControls;
        private DataBox _actionBarSlotsDataBox;

        private void BuildActionBar()
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile.ActionBarSlots == null)
                profile.ActionBarSlots = new List<ActionBarSlotData>();
            if (profile.ActionBarSlots.Count == 0)
            {
                for (int i = 0; i < ActionBarManager.SLOT_COUNT; i++)
                    profile.ActionBarSlots.Add(new ActionBarSlotData());
            }

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 550, true);
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            int startX = 5;
            int startY = 5;

            _actionBarEnabled = AddCheckBox(rightArea, _lang?.GetActionBar?.ShowActionBar ?? "Action Bar", profile.ActionBarEnabled, startX, startY);
            startY += _actionBarEnabled.Height + 10;

            string selfLabel = _lang?.GetActionBar?.TargetSelf ?? "Self";
            string lastLabel = _lang?.GetActionBar?.TargetLast ?? "Last";
            string hotkeyLabel = _lang?.GetActionBar?.Hotkey ?? "Hotkey";
            string slotLabel = (_lang?.GetActionBar?.Slot ?? "Slot {0}").Replace("{0}", "#");
            string dragLabel = _lang?.GetActionBar?.DragSpellHere ?? "Drag spell here";

            var headerLabel = new Label($"{slotLabel}  |  {dragLabel}  |  {hotkeyLabel}  |  {selfLabel}/{lastLabel}", true, 0x0481) { X = startX, Y = startY };
            rightArea.Add(headerLabel);
            startY += headerLabel.Height + 5;

            _actionBarSlotControls = new List<OptionsGumpActionBarSlotControl>();
            _actionBarSlotsDataBox = new DataBox(startX, startY + 33, rightArea.Width - 15, 1) { WantUpdateSize = true };

            var addBtn = new NiceButton(startX, startY, 80, 25, ButtonAction.Activate, "+ Add");
            addBtn.MouseUp += (s, e) =>
            {
                if (profile.ActionBarSlots.Count >= ActionBarManager.MAX_SLOT_COUNT) return;
                profile.ActionBarSlots.Add(new ActionBarSlotData());
                int newIdx = profile.ActionBarSlots.Count - 1;
                var newSlot = new OptionsGumpActionBarSlotControl(newIdx, profile.ActionBarSlots[newIdx], _lang, () => UIManager.GetGump<ActionBarGump>()?.RefreshSlots(), RemoveSlotAt);
                newSlot.Y = newIdx * (44 + 8);
                _actionBarSlotControls.Add(newSlot);
                _actionBarSlotsDataBox.Add(newSlot);
                for (int j = 0; j < _actionBarSlotControls.Count; j++)
                    _actionBarSlotControls[j].Y = j * (44 + 8);
                _actionBarSlotsDataBox.WantUpdateSize = true;
                RefreshActionBarGump();
            };
            rightArea.Add(addBtn);
            rightArea.Add(_actionBarSlotsDataBox);

            void RemoveSlotAt(int idx)
            {
                if (profile.ActionBarSlots == null || idx < 0 || idx >= profile.ActionBarSlots.Count || profile.ActionBarSlots.Count <= 1) return;
                profile.ActionBarSlots.RemoveAt(idx);
                var ctrl = _actionBarSlotControls[idx];
                _actionBarSlotControls.RemoveAt(idx);
                ctrl.Parent?.Remove(ctrl);
                ctrl.Dispose();
                for (int j = idx; j < _actionBarSlotControls.Count; j++)
                    _actionBarSlotControls[j].SetSlotIndex(j);
                const int slotHeight = 44;
                const int slotSpacing = 8;
                int baseY = 0;
                for (int j = 0; j < _actionBarSlotControls.Count; j++)
                {
                    _actionBarSlotControls[j].Y = baseY + j * (slotHeight + slotSpacing);
                }
                _actionBarSlotsDataBox.WantUpdateSize = true;
                RefreshActionBarGump();
            }

            void RefreshActionBarGump()
            {
                var g = UIManager.GetGump<ActionBarGump>();
                if (profile.ActionBarEnabled && g != null)
                {
                    g.Dispose();
                    UIManager.Add(new ActionBarGump());
                }
            }

            for (int i = 0; i < profile.ActionBarSlots.Count; i++)
            {
                var slotControl = new OptionsGumpActionBarSlotControl(i, profile.ActionBarSlots[i], _lang, () => UIManager.GetGump<ActionBarGump>()?.RefreshSlots(), RemoveSlotAt);
                slotControl.Y = i * (44 + 8);
                _actionBarSlotControls.Add(slotControl);
                _actionBarSlotsDataBox.Add(slotControl);
            }

            Add(rightArea, ACTION_BAR_PAGE);
        }

        private class OptionsGumpActionBarSlotControl : Control, IActionBarDropTarget
        {
            private readonly ActionBarSlotData _slotData;
            private readonly Action _onChanged;
            private readonly Action<int> _onRemove;
            private GumpPic _icon;
            private readonly AlphaBlendControl _dropZone;
            private readonly HotkeyBox _hotkeyBox;
            private readonly Combobox _targetCombo;
            private readonly Label _slotNumberBox;

            public int SlotIndex { get; private set; }

            public OptionsGumpActionBarSlotControl(int slotIndex, ActionBarSlotData slotData, OptionsGumpLanguage lang, Action onChanged, Action<int> onRemove)
            {
                SlotIndex = slotIndex;
                _slotData = slotData;
                _onChanged = onChanged;
                _onRemove = onRemove;
                AcceptMouseInput = true;
                Width = 500;
                Height = 44;

                _slotNumberBox = new Label((slotIndex + 1).ToString(), true, 0x0481) { X = 5, Y = 12 };
                Add(_slotNumberBox);

                _dropZone = new AlphaBlendControl(0.3f) { X = 50, Y = 2, Width = 44, Height = 44 };
                _dropZone.AcceptMouseInput = true;
                Add(_dropZone);
                RefreshIcon();

                _hotkeyBox = new HotkeyBox { X = 110, Y = 8 };
                _hotkeyBox.HotkeyChanged += (s, e) =>
                {
                    bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                    bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                    bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                    _slotData.Key = (int)_hotkeyBox.Key;
                    _slotData.Alt = alt;
                    _slotData.Ctrl = ctrl;
                    _slotData.Shift = shift;
                    _onChanged?.Invoke();
                };
                _hotkeyBox.HotkeyCancelled += (s, e) =>
                {
                    _slotData.Key = 0;
                    _slotData.Alt = _slotData.Ctrl = _slotData.Shift = false;
                    _onChanged?.Invoke();
                };
                if (slotData.Key != 0)
                {
                    SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                    if (slotData.Alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                    if (slotData.Ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                    if (slotData.Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                    _hotkeyBox.SetKey((SDL.SDL_Keycode)slotData.Key, mod);
                }
                Add(_hotkeyBox);

                string selfLabel = lang?.GetActionBar?.TargetSelf ?? "Self";
                string lastLabel = lang?.GetActionBar?.TargetLast ?? "Last";
                _targetCombo = new Combobox(320, 8, 100, new[] { selfLabel, lastLabel }, slotData.TargetType);
                _targetCombo.OnOptionSelected += (s, value) => { _slotData.TargetType = value; _onChanged?.Invoke(); };
                Add(_targetCombo);

                var delBtn = new NiceButton(430, 8, 32, 28, ButtonAction.Activate, "X");
                delBtn.MouseUp += (s, e) => _onRemove?.Invoke(SlotIndex);
                Add(delBtn);
            }

            public void SetSlotIndex(int index)
            {
                SlotIndex = index;
                _slotNumberBox.Text = (index + 1).ToString();
            }

            private void RefreshIcon()
            {
                _icon?.Dispose();
                _icon = null;
                if ((_slotData.SlotType == (int)ActionBarSlotType.Spell || _slotData.SlotType == 0) && _slotData.SpellID > 0)
                {
                    var spell = SpellDefinition.FullIndexGetSpell(_slotData.SpellID);
                    if (spell != null && spell.GumpIconSmallID > 0)
                    {
                        _icon = new GumpPic(0, 0, (ushort)spell.GumpIconSmallID, 0) { X = 52, Y = 4, Width = 40, Height = 40, AcceptMouseInput = false };
                        Add(_icon);
                    }
                }
                else if (_slotData.SlotType == (int)ActionBarSlotType.Skill && _slotData.SkillIndex >= 0 && World.InGame && World.Player != null && _slotData.SkillIndex < World.Player.Skills.Length)
                {
                    _icon = new GumpPic(0, 0, 0x24B8, 0) { X = 52, Y = 4, Width = 40, Height = 40, AcceptMouseInput = false };
                    Add(_icon);
                }
                else if (_slotData.SlotType == (int)ActionBarSlotType.Ability && _slotData.AbilityIndex > 0 && _slotData.AbilityIndex <= AbilityData.Abilities.Length)
                {
                    ref readonly var def = ref AbilityData.Abilities[_slotData.AbilityIndex - 1];
                    _icon = new GumpPic(0, 0, def.Icon, 0) { X = 52, Y = 4, Width = 40, Height = 40, AcceptMouseInput = false };
                    Add(_icon);
                }
            }

            public void AcceptSpell(int spellId)
            {
                _slotData.SlotType = (int)ActionBarSlotType.Spell;
                _slotData.SpellID = spellId;
                _slotData.SkillIndex = -1;
                _slotData.AbilityIndex = 0;
                RefreshIcon();
                _onChanged?.Invoke();
            }

            public void AcceptSkill(int skillIndex)
            {
                _slotData.SlotType = (int)ActionBarSlotType.Skill;
                _slotData.SpellID = 0;
                _slotData.SkillIndex = skillIndex;
                _slotData.AbilityIndex = 0;
                RefreshIcon();
                _onChanged?.Invoke();
            }

            public void AcceptAbility(int abilityIndex)
            {
                _slotData.SlotType = (int)ActionBarSlotType.Ability;
                _slotData.SpellID = 0;
                _slotData.SkillIndex = -1;
                _slotData.AbilityIndex = abilityIndex;
                RefreshIcon();
                _onChanged?.Invoke();
            }
        }
    }
}
