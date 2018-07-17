using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.WorldObjects.Views;

namespace ClassicUO.Game.WorldObjects
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

    public enum RaceType : byte
    {
        HUMAN,
        ELF,
        GARGOYLE
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
        private RaceType _race;

        public Mobile(Serial serial) : base(serial)
        {
        }

        public event EventHandler HitsChanged;
        public event EventHandler ManaChanged;
        public event EventHandler StaminaChanged;


        protected override WorldRenderObject CreateView() => new MobileView(this);

        public new MobileView ViewObject => (MobileView)base.ViewObject;


        public RaceType Race
        {
            get => _race;
            set
            {
                if (_race != value)
                {
                    _race = value;
                    _delta |= Delta.Appearance;
                }
            }
        }

        public ushort Hits
        {
            get { return _hits; }
            set
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
            set
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
            set
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
            set
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
            set
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
            set
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
            set
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
            set
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
            set
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

        public void SetSAPoison(in bool value) => _isSA_Poisoned = value;


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




        public bool IsMounted => GetItemAtLayer(Layer.Mount) != null;
        public bool IsRunning => (Direction & Direction.Running) == Direction.Running;
        public double MoveSequence { get; set; }

        internal MovementsHistory MovementHandler { get; } = new MovementsHistory();
        internal Position GoalPosition { get; set; }
        internal bool IsMoving => GoalPosition != Position.Invalid && Position != GoalPosition;
        internal DateTime _timeToNextTile;


        public void MoveTo(in Position position, in Direction direction)
        {
            MovementHandler.Reset();
            Direction = direction;
            Position = position;
            GoalPosition = Position.Invalid;

            ProcessDelta();
        }

        public void EnqueueMovement(in Position position, in Direction direction, bool isfromuser = false)
            => MovementHandler.Enqueue(position, direction, isfromuser);


        internal void DoMovements(in double ticks)
        {
            bool isPlayer = this == World.Player;

            if (isPlayer)
            {
                if (_timeToNextTile < DateTime.Now && !IsMoving)
                    World.Player.CheckIfNeedToMove();

                while (MovementHandler.TryDequeue(out byte seq, out var movement))
                {
                    Direction = movement.Direction;
                    if (movement.IsFromUser)
                    {
                        new Network.PWalkRequest(movement.Direction, seq).SendToServer();
                    }

                    if (movement.Position != Position)
                    {
                        GoalPosition = movement.Position;

                        double animationTime = MovementSpeed.TimeToCompleteMovement(this, Direction);
                        _timeToNextTile = DateTime.Now.AddMilliseconds(animationTime);
                        break;
                    }
                }
            }
            else
            {
                var movement = MovementHandler.GetNextMovement(out byte seq);
                if (movement != null)
                {
                    Direction = movement.Direction;
                    if (movement.Position != Position)
                    {
                        GoalPosition = movement.Position;
                    }
                }
            }


            if (IsMoving)
            {
                if (DateTime.Now >= _timeToNextTile) // GoalPosition OK!
                {
                    Position = GoalPosition;

                    ProcessDelta();
                }
            }


        }

    }

    public class MovementsHistory
    {
        private byte? _lastACK;
        private byte _sendedACK;
        private byte _nextACK;
        private SingleMovement[] _history;

        public MovementsHistory()
        {
            Reset();
        }

        public void Reset()
        {
            _lastACK = null;
            _sendedACK = 0;
            _nextACK = 0;
            _history = new SingleMovement[256];
        }


        public void Enqueue(in Position position, in Direction direction, bool isfromuser = false)
        {
            SingleMovement movement = new SingleMovement(position, direction)
            {
                IsFromUser = isfromuser
            };
            _history[_sendedACK++] = movement;
            if (_sendedACK > byte.MaxValue)
                _sendedACK = 1;
        }

        public SingleMovement Dequeue(out byte sequence)
        {
            if (_history[_nextACK] != null)
            {
                var m = _history[_nextACK];
                _history[_nextACK] = null;
                sequence = _nextACK++;
                if (_nextACK > byte.MaxValue)
                    _nextACK = 1;
                return m;
            }

            sequence = 0;
            return null;
        }

        public bool TryDequeue(out byte seq, out SingleMovement movement)
        {
            if (_history[_nextACK] != null)
            {
                movement = _history[_nextACK];
                _history[_nextACK] = null;
                seq = _nextACK++;
                if (_nextACK > byte.MaxValue)
                    _nextACK = 1;
                return true;
            }
            seq = 0;
            movement = null;
            return false;
        }

        public SingleMovement Peek() => _history[_nextACK];

        public SingleMovement GetAt(in int seq)
        {
            if (_history[_nextACK] != null)
            {
                var m = _history[_nextACK];
                _history[_nextACK++] = null;
                if (_nextACK > byte.MaxValue)
                    _nextACK = 1;
                return m;
            }
            return null;
        }

        public SingleMovement GetNextMovement(out byte seq)
        {
            SingleMovement movement = null;
            SingleMovement lastMovement;

            while (TryDequeue(out seq, out var nextMovement))
            {
                lastMovement = movement;
                movement = nextMovement;
                nextMovement = Peek();

                if (nextMovement == null && lastMovement != null && movement.Position == lastMovement.Position && movement.Direction != lastMovement.Direction)
                {
                    Enqueue(movement.Position, movement.Direction);
                    return lastMovement;
                }
            }

            return movement;
        }

        public void ACKReceived(in byte seq)
        {
            _history[seq] = null;
            _lastACK = seq;
        }

        public void RejectedMovementRequest(in byte seq, out Position position, out Direction direction)
        {
            if (_history[seq] != null)
            {
                var e = _history[seq];
                position = e.Position;
                direction = e.Direction;
            }
            else
            {
                position = Position.Invalid;
                direction = Direction.NONE;
            }

            Reset();
        }
    }

    public class SingleMovement
    {
        public SingleMovement(in Position position, in Direction direction)
        {
            Position = position; Direction = direction;
            FastWalkKey = 0; // atm set to 0
        }

        public Position Position { get; }
        public Direction Direction { get; }
        public int FastWalkKey { get; }
        public bool IsFromUser { get; set; }

        public override string ToString() => string.Format("{0} {1}", Position, Direction);
    }


    public static class MovementSpeed
    {
        const double TIME_WALK_FOOT = (8d / 20d) * 1000d;
        const double TIME_RUN_FOOT = (4d / 20d) * 1000d;
        const double TIME_WALK_MOUNT = (4d / 20d) * 1000d;
        const double TIME_RUN_MOUNT = (2d / 20d) * 1000d;


        public static double TimeToCompleteMovement(in Mobile mobile, in Direction direction)
        {
            bool isrunning  = (direction & Direction.Running) == Direction.Running;

            if (mobile.IsMounted)
                return isrunning ? TIME_RUN_MOUNT : TIME_WALK_MOUNT;
            return isrunning ? TIME_RUN_FOOT : TIME_WALK_FOOT;
        }
    }
}
