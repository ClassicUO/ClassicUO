using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ClassicUO.Assets;

namespace ClassicUO.Game
{
    [Flags]
    public enum Notoriety : byte
    {
        Unknown = 0x00,
        Innocent = 0x01,
        Ally = 0x02,
        Gray = 0x03,
        Criminal = 0x04,
        Enemy = 0x05,
        Murderer = 0x06,
        Invulnerable = 0x07,
    }

    public class Mobile : Entity
    {
        private ushort _hits;
        private ushort _hitsMax;
        private ushort _mana;
        private ushort _manaMax;
        private ushort _stamina;
        private ushort _staminaMax;
        private Notoriety _notoriety;
        private bool _warMode;
        private bool _renamable;
        private bool _isSA_Poisoned;

        public Mobile(Serial serial) : base(serial)
        {
        }

        public event EventHandler HitsChanged;
        public event EventHandler ManaChanged;
        public event EventHandler StaminaChanged;

        public ushort Hits
        {
            get { return _hits; }
            internal set
            {
                if (_hits != value)
                {
                    _hits = value;
                    _delta |= Delta.Hits;
                }
            }
        }

        public ushort HitsMax
        {
            get { return _hitsMax; }
            internal set
            {
                if (_hitsMax != value)
                {
                    _hitsMax = value;
                    _delta |= Delta.Hits;
                }
            }
        }

        public ushort Mana
        {
            get { return _mana; }
            internal set
            {
                if (_mana != value)
                {
                    _mana = value;
                    _delta |= Delta.Mana;
                }
            }
        }

        public ushort ManaMax
        {
            get { return _manaMax; }
            internal set
            {
                if (_manaMax != value)
                {
                    _manaMax = value;
                    _delta |= Delta.Mana;
                }
            }
        }

        public ushort Stamina
        {
            get { return _stamina; }
            internal set
            {
                if (_stamina != value)
                {
                    _stamina = value;
                    _delta |= Delta.Stamina;
                }
            }
        }

        public ushort StaminaMax
        {
            get { return _staminaMax; }
            internal set
            {
                if (_staminaMax != value)
                {
                    _staminaMax = value;
                    _delta |= Delta.Stamina;
                }
            }
        }

        public Notoriety Notoriety
        {
            get { return _notoriety; }
            internal set
            {
                if (_notoriety != value)
                {
                    _notoriety = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        public bool WarMode
        {
            get { return _warMode; }
            internal set
            {
                if (_warMode != value)
                {
                    _warMode = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        public bool Renamable
        {
            get { return _renamable; }
            internal set
            {
                if (_renamable != value)
                {
                    _renamable = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        public bool Paralyzed => Flags.HasFlag(Flags.Frozen);
        public bool YellowBar => Flags.HasFlag(Flags.YellowBar);
        public bool Poisoned => FileManager.ClientVersion >= ClientVersions.CV_7000 ? _isSA_Poisoned : Flags.HasFlag(Flags.Poisoned);
        public bool Hidden => Flags.HasFlag(Flags.Hidden);
        public bool IsDead => Graphic == 402 || Graphic == 403 || Graphic == 607 || Graphic == 608 || Graphic == 970;
        public bool IsFlying => FileManager.ClientVersion >= ClientVersions.CV_7000 ? Flags.HasFlag(Flags.Flying) : false;
        public bool IsWarMode => Flags.HasFlag(Flags.WarMode);
        public bool IsHuman => 
               MathHelper.InRange(Graphic, 0x0190, 0x0193)
            || MathHelper.InRange(Graphic, 0x00B7, 0x00BA)
            || MathHelper.InRange(Graphic, 0x025D, 0x0260)
            || MathHelper.InRange(Graphic, 0x029A, 0x029B)
            || MathHelper.InRange(Graphic, 0x02B6, 0x02B7)
            || Graphic == 0x03DB || Graphic == 0x03DF || Graphic == 0x03E2;

        internal void SetSAPoison(bool value) => _isSA_Poisoned = value;


        public Item GetItemAtLayer(Layer layer) => Items.SingleOrDefault(s => s.Layer == layer);


        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Hits))
                HitsChanged.Raise(this);

            if (d.HasFlag(Delta.Mana))
                ManaChanged.Raise(this);

            if (d.HasFlag(Delta.Stamina))
                StaminaChanged.Raise(this);
        }

        public override bool Exists => World.Contains(Serial);


    }
}
