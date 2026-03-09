using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ActionBarGump : AnchorableGump
    {
        private const int SLOT_SIZE = 40;
        private const int SLOT_PADDING = 2;
        private const int BAR_PADDING = 8;
        private const int BORDER = 4;
        private const int HOTKEY_BADGE_SIZE = 22;
        private static readonly Color BgColor = Color.FromNonPremultiplied(25, 25, 35, 240);
        private static readonly Color SlotBg = Color.FromNonPremultiplied(45, 45, 60, 200);
        private static readonly Color GlowColor = Color.FromNonPremultiplied(255, 200, 100, 200);
        private static readonly Color SelectedColor = Color.FromNonPremultiplied(100, 150, 255, 180);
        private static readonly Color HotkeyBadgeBg = Color.FromNonPremultiplied(30, 30, 40, 220);

        private readonly List<ActionBarSlotControl> _slots = new List<ActionBarSlotControl>();
        private int _selectedSlotIndex = -1;
        private int _listeningForHotkeySlot = -1;

        public override GumpType GumpType => GumpType.ActionBar;

        public ActionBarGump() : base(0, 0)
        {
            AnchorType = ANCHOR_TYPE.DISABLED;
            var profile = ProfileManager.CurrentProfile;
            X = profile.ActionBarPosition.X;
            Y = profile.ActionBarPosition.Y;
            int slotCount = Math.Max(1, Math.Min(profile?.ActionBarSlots?.Count ?? ActionBarManager.SLOT_COUNT, ActionBarManager.MAX_SLOT_COUNT));

            int barWidth = slotCount * (SLOT_SIZE + SLOT_PADDING) + BAR_PADDING * 2 + BORDER * 2;
            int barHeight = SLOT_SIZE + BAR_PADDING * 2 + BORDER * 2;

            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;
            CanCloseWithRightClick = false;
            Width = barWidth;
            Height = barHeight;

            Add(new BorderControl(0, 0, Width, Height, BORDER));
            Add(new AlphaBlendControl(0.95f) { X = BORDER, Y = BORDER, Width = Width - BORDER * 2, Height = Height - BORDER * 2, BaseColor = BgColor });

            for (int i = 0; i < slotCount; i++)
            {
                int idx = i;
                int x = BORDER + BAR_PADDING + i * (SLOT_SIZE + SLOT_PADDING);
                int y = BORDER + BAR_PADDING;
                var slotControl = new ActionBarSlotControl(this, x, y, SLOT_SIZE, idx);
                _slots.Add(slotControl);
                Add(slotControl);
            }

            SetInScreen();
        }

        public void RefreshSlots()
        {
            foreach (var slot in _slots)
            {
                slot.RefreshSlot();
            }
        }

        public void StartListeningForHotkey(int slotIndex)
        {
            _listeningForHotkeySlot = slotIndex;
            SetKeyboardFocus();
        }

        public void ClearSlot(int slotIndex)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile?.ActionBarSlots == null || slotIndex < 0 || slotIndex >= profile.ActionBarSlots.Count) return;
            var slot = profile.ActionBarSlots[slotIndex];
            slot.SlotType = (int)ActionBarSlotType.Spell;
            slot.SpellID = 0;
            slot.SkillIndex = -1;
            slot.AbilityIndex = 0;
            slot.Key = 0;
            slot.Alt = false;
            slot.Ctrl = false;
            slot.Shift = false;
            _slots[slotIndex].RefreshSlot();
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (_listeningForHotkeySlot >= 0)
            {
                bool alt = (mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                bool ctrl = (mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                bool shift = (mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                var profile = ProfileManager.CurrentProfile;
                if (profile?.ActionBarSlots != null && _listeningForHotkeySlot < profile.ActionBarSlots.Count)
                {
                    var slot = profile.ActionBarSlots[_listeningForHotkeySlot];
                    slot.Key = (int)key;
                    slot.Alt = alt;
                    slot.Ctrl = ctrl;
                    slot.Shift = shift;
                    _slots[_listeningForHotkeySlot].RefreshSlot();
                }
                _listeningForHotkeySlot = -1;
                UIManager.KeyboardFocusControl = null;
                return;
            }
            base.OnKeyDown(key, mod);
        }

        public override void Update()
        {
            base.Update();
            foreach (var slot in _slots)
            {
                slot.UpdateState();
            }
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            var profile = ProfileManager.CurrentProfile;
            if (profile != null)
            {
                profile.ActionBarPosition = new Point(X, Y);
            }
        }

        private void SetSelectedSlot(int index)
        {
            _selectedSlotIndex = index;
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].IsSelected = i == index;
            }
        }

        private void BuildSlotContextMenu(int slotIndex, ContextMenuControl menu)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile?.ActionBarSlots == null || slotIndex >= profile.ActionBarSlots.Count) return;
            var slot = profile.ActionBarSlots[slotIndex];
            bool hasAction = (slot.SlotType == (int)ActionBarSlotType.Spell || slot.SlotType == 0) && slot.SpellID > 0
                || slot.SlotType == (int)ActionBarSlotType.Skill && slot.SkillIndex >= 0
                || slot.SlotType == (int)ActionBarSlotType.Ability && slot.AbilityIndex > 0;
            menu.Add(Language.Instance?.GetOptionsGumpLanguage?.GetActionBar?.ResetSlot ?? "Reset slot", () => ClearSlot(slotIndex));
            menu.Add(Language.Instance?.GetOptionsGumpLanguage?.GetActionBar?.SetHotkey ?? "Set Hotkey", () => StartListeningForHotkey(slotIndex));
        }

        private class ActionBarSlotControl : Control, IActionBarDropTarget
        {
            private readonly ActionBarGump _gump;
            private readonly int _slotIndex;
            private readonly int _iconSize;
            private GumpPic _icon;
            private Control _skillText;
            private Control _cooldownOverlay;
            private ClassicUO.Game.RenderedText _hotkeyRendered;
            private bool _glowing;
            private bool _isSelected;
            private int _lastType;
            private int _lastId;
            private string _lastName;
            private string _hotkeyText;
            private string _hotkeyFullText;

            public ActionBarSlotControl(ActionBarGump gump, int x, int y, int iconSize, int slotIndex)
            {
                _gump = gump;
                X = x;
                Y = y;
                _iconSize = iconSize;
                Width = iconSize;
                Height = iconSize;
                _slotIndex = slotIndex;
                AcceptMouseInput = true;
                _cooldownOverlay = new AlphaBlendControl(0.6f) { X = 0, Y = 0, Width = iconSize, Height = iconSize, BaseColor = Color.Black };
                Add(new AlphaBlendControl(0.5f) { X = 0, Y = 0, Width = iconSize, Height = iconSize, BaseColor = SlotBg });
                Add(_cooldownOverlay);
                ContextMenu = new ContextMenuControl();
                MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Right)
                    {
                        _gump.SetSelectedSlot(_slotIndex);
                        ContextMenu.Dispose();
                        ContextMenu = new ContextMenuControl();
                        _gump.BuildSlotContextMenu(_slotIndex, ContextMenu);
                        ContextMenu.Show();
                    }
                    else if (e.Button == MouseButtonType.Left)
                    {
                        _gump.SetSelectedSlot(_slotIndex);
                    }
                };
                RefreshSlot();
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                    }
                }
            }

            public void RefreshSlot()
            {
                var profile = ProfileManager.CurrentProfile;
                if (profile?.ActionBarSlots == null || _slotIndex >= profile.ActionBarSlots.Count)
                {
                    SetEmpty();
                    return;
                }
                var slot = profile.ActionBarSlots[_slotIndex];
                UpdateHotkeyBadge(slot);
                if ((slot.SlotType == (int)ActionBarSlotType.Spell || slot.SlotType == 0) && slot.SpellID > 0)
                    SetSpell(slot.SpellID);
                else if (slot.SlotType == (int)ActionBarSlotType.Skill && slot.SkillIndex >= 0)
                    SetSkill(slot.SkillIndex);
                else if (slot.SlotType == (int)ActionBarSlotType.Ability && slot.AbilityIndex > 0)
                    SetAbility(slot.AbilityIndex);
                else
                    SetEmpty();
            }

            private void UpdateHotkeyBadge(ActionBarSlotData slot)
            {
                if (slot.Key != 0 && slot.Key != (int)SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                    if (slot.Alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                    if (slot.Ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                    if (slot.Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                    string full = KeysTranslator.TryGetKey((SDL.SDL_Keycode)slot.Key, mod);
                    var sb = new System.Text.StringBuilder();
                    if (slot.Ctrl) sb.Append("C");
                    if (slot.Alt) sb.Append("A");
                    if (slot.Shift) sb.Append("S");
                    string keyPart = KeysTranslator.TryGetKey((SDL.SDL_Keycode)slot.Key, SDL.SDL_Keymod.SDL_KMOD_NONE);
                    if (!string.IsNullOrEmpty(keyPart)) sb.Append(keyPart.Replace(" ", ""));
                    _hotkeyText = sb.ToString();
                    _hotkeyFullText = full ?? _hotkeyText;
                }
                else
                {
                    _hotkeyText = "";
                    _hotkeyFullText = "";
                }
                string displayText = _hotkeyText.Length > 4 ? _hotkeyText.Substring(0, 4) : _hotkeyText;
                _hotkeyRendered = string.IsNullOrEmpty(displayText) ? null : ClassicUO.Game.RenderedText.Create(displayText, 0x0481, 0xFF, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER, 0);
            }

            private void UpdateTooltip()
            {
                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(_lastName))
                    sb.Append(_lastName);
                if (!string.IsNullOrEmpty(_hotkeyFullText))
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("Hotkey: ").Append(_hotkeyFullText);
                }
                if (sb.Length > 0)
                    SetTooltip(sb.ToString(), 180);
                else
                    ClearTooltip();
            }

            private void SetEmpty()
            {
                if (_lastType == -1 && _lastId == 0) return;
                _lastType = -1;
                _lastId = 0;
                _lastName = "";
                _icon?.Dispose();
                _icon = null;
                _skillText?.Dispose();
                _skillText = null;
                UpdateTooltip();
            }

            private void SetSpell(int spellId)
            {
                if (_lastType == 0 && _lastId == spellId) return;
                _lastType = 0;
                _lastId = spellId;
                _icon?.Dispose();
                _icon = null;
                _skillText?.Dispose();
                _skillText = null;
                var spell = SpellDefinition.FullIndexGetSpell(spellId);
                _lastName = spell?.Name ?? "";
                UpdateTooltip();
                if (spell != null && spell.GumpIconSmallID > 0)
                {
                    _icon = new GumpPic(0, 0, (ushort)spell.GumpIconSmallID, 0) { Width = _iconSize, Height = _iconSize, AcceptMouseInput = false };
                    Add(_icon);
                }
            }

            private void SetSkill(int skillIndex)
            {
                if (_lastType == 1 && _lastId == skillIndex) return;
                _lastType = 1;
                _lastId = skillIndex;
                _icon?.Dispose();
                _icon = null;
                _skillText?.Dispose();
                _skillText = null;
                if (World.InGame && World.Player != null && skillIndex >= 0 && skillIndex < World.Player.Skills.Length)
                {
                    var skill = World.Player.Skills[skillIndex];
                    _lastName = skill?.Name ?? "";
                }
                else
                {
                    _lastName = "";
                }
                UpdateTooltip();
                _skillText = new Label("Skill", true, 0x0481, 0, 1, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER) { X = 0, Y = (_iconSize - 12) / 2, Width = _iconSize, AcceptMouseInput = false };
                Add(_skillText);
            }

            private void SetAbility(int abilityIndex)
            {
                if (_lastType == 2 && _lastId == abilityIndex) return;
                _lastType = 2;
                _lastId = abilityIndex;
                _icon?.Dispose();
                _icon = null;
                _skillText?.Dispose();
                _skillText = null;
                if (abilityIndex > 0 && abilityIndex <= AbilityData.Abilities.Length)
                {
                    ref readonly var def = ref AbilityData.Abilities[abilityIndex - 1];
                    _lastName = def.Name;
                    _icon = new GumpPic(0, 0, def.Icon, 0) { Width = _iconSize, Height = _iconSize, AcceptMouseInput = false };
                    Add(_icon);
                }
                else
                {
                    _lastName = "";
                }
                UpdateTooltip();
            }

            public void AcceptSpell(int spellId)
            {
                var profile = ProfileManager.CurrentProfile;
                if (profile?.ActionBarSlots == null || _slotIndex >= profile.ActionBarSlots.Count) return;
                var slot = profile.ActionBarSlots[_slotIndex];
                slot.SlotType = (int)ActionBarSlotType.Spell;
                slot.SpellID = spellId;
                slot.SkillIndex = -1;
                slot.AbilityIndex = 0;
                RefreshSlot();
            }

            public void AcceptSkill(int skillIndex)
            {
                var profile = ProfileManager.CurrentProfile;
                if (profile?.ActionBarSlots == null || _slotIndex >= profile.ActionBarSlots.Count) return;
                var slot = profile.ActionBarSlots[_slotIndex];
                slot.SlotType = (int)ActionBarSlotType.Skill;
                slot.SpellID = 0;
                slot.SkillIndex = skillIndex;
                slot.AbilityIndex = 0;
                RefreshSlot();
            }

            public void AcceptAbility(int abilityIndex)
            {
                var profile = ProfileManager.CurrentProfile;
                if (profile?.ActionBarSlots == null || _slotIndex >= profile.ActionBarSlots.Count) return;
                var slot = profile.ActionBarSlots[_slotIndex];
                slot.SlotType = (int)ActionBarSlotType.Ability;
                slot.SpellID = 0;
                slot.SkillIndex = -1;
                slot.AbilityIndex = abilityIndex;
                RefreshSlot();
            }

            public void UpdateState()
            {
                var manager = Client.Game.GetScene<GameScene>()?.ActionBar;
                if (manager == null) return;
                bool onCd = manager.IsSlotOnCooldown(_slotIndex);
                _cooldownOverlay.IsVisible = onCd;
                if (onCd)
                {
                    double pct = manager.GetCooldownPercent(_slotIndex);
                    _cooldownOverlay.Height = (int)(_iconSize * pct);
                    _cooldownOverlay.Y = _iconSize - _cooldownOverlay.Height;
                }
                _glowing = manager.IsSlotGlowing(_slotIndex);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                int badgeX = x + _iconSize - HOTKEY_BADGE_SIZE - 2;
                int badgeY = y + _iconSize - HOTKEY_BADGE_SIZE - 2;
                if (!string.IsNullOrEmpty(_hotkeyText))
                {
                    batcher.Draw(SolidColorTextureCache.GetTexture(HotkeyBadgeBg), new Rectangle(badgeX, badgeY, HOTKEY_BADGE_SIZE + 2, HOTKEY_BADGE_SIZE + 2), ShaderHueTranslator.GetHueVector(0, false, 1f));
                }
                if (!base.Draw(batcher, x, y)) return false;
                if (_hotkeyRendered != null && !_hotkeyRendered.IsDestroyed)
                {
                    int tx = badgeX + (HOTKEY_BADGE_SIZE + 2 - _hotkeyRendered.Width) / 2;
                    int ty = badgeY + (HOTKEY_BADGE_SIZE + 2 - _hotkeyRendered.Height) / 2;
                    _hotkeyRendered.Draw(batcher, tx, ty);
                }
                if (_isSelected)
                {
                    batcher.Draw(SolidColorTextureCache.GetTexture(SelectedColor), new Rectangle(x, y, _iconSize, _iconSize), ShaderHueTranslator.GetHueVector(0, false, 0.8f));
                }
                if (_glowing)
                {
                    batcher.SetBlendState(BlendState.Additive);
                    batcher.Draw(SolidColorTextureCache.GetTexture(GlowColor), new Rectangle(x, y, _iconSize, _iconSize), ShaderHueTranslator.GetHueVector(0, false, 0.7f));
                    batcher.SetBlendState(null);
                }
                return true;
            }
        }
    }
}
