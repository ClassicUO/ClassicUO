// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.GameObjects
{
    internal class PlayerMobile : Mobile
    {
        private readonly Dictionary<BuffIconType, BuffIcon> _buffIcons = new Dictionary<BuffIconType, BuffIcon>();

        public PlayerMobile(World world, uint serial) : base(world, serial)
        {
            Skills = new Skill[Client.Game.UO.FileManager.Skills.SkillsCount];

            for (int i = 0; i < Skills.Length; i++)
            {
                SkillEntry skill = Client.Game.UO.FileManager.Skills.Skills[i];
                Skills[i] = new Skill(skill.Name, skill.Index, skill.HasAction);
            }

            Walker = new WalkerManager(this);
            Pathfinder = new Pathfinder(world);
        }

        public Skill[] Skills { get; }
        public override bool InWarMode { get; set; }
        public IReadOnlyDictionary<BuffIconType, BuffIcon> BuffIcons => _buffIcons;

        public ref Ability PrimaryAbility => ref Abilities[0];
        public ref Ability SecondaryAbility => ref Abilities[1];
        protected override bool IsWalking => LastStepTime > Time.Ticks - Constants.PLAYER_WALKING_DELAY;

        internal WalkerManager Walker { get; }
        public Pathfinder Pathfinder { get; }


        public readonly Ability[] Abilities = [Ability.Invalid, Ability.Invalid];

        //private bool _lastRun, _lastMount;
        //private int _lastDir = -1, _lastDelta, _lastStepTime;


        public readonly HashSet<uint> AutoOpenedCorpses = new HashSet<uint>();
        public readonly HashSet<uint> ManualOpenedCorpses = new HashSet<uint>();

        public short ColdResistance;
        public short DamageIncrease;
        public short DamageMax;
        public short DamageMin;
        public long DeathScreenTimer;
        public short DefenseChanceIncrease;
        public Lock DexLock;
        public ushort Dexterity;
        public short DexterityIncrease;
        public short EnergyResistance;
        public short EnhancePotions;
        public short FasterCasting;
        public short FasterCastRecovery;
        public short FireResistance;
        public byte Followers;
        public byte FollowersMax;
        public uint Gold;
        public short HitChanceIncrease;
        public short HitPointsIncrease;
        public short HitPointsRegeneration;
        public ushort Intelligence;
        public short IntelligenceIncrease;
        public Lock IntLock;
        public short LowerManaCost;
        public short LowerReagentCost;
        public ushort Luck;
        public short ManaIncrease;
        public short ManaRegeneration;
        public short MaxColdResistence;
        public short MaxDefenseChanceIncrease;
        public short MaxEnergyResistence;
        public short MaxFireResistence;
        public short MaxHitPointsIncrease;
        public short MaxManaIncrease;
        public short MaxPhysicResistence;
        public short MaxPoisonResistence;
        public short MaxStaminaIncrease;
        public short PhysicalResistance;
        public short PoisonResistance;
        public short ReflectPhysicalDamage;
        public short SpellDamageIncrease;
        public short StaminaIncrease;
        public short StaminaRegeneration;
        public short StatsCap;
        public ushort Strength;
        public short StrengthIncrease;
        public Lock StrLock;
        public short SwingSpeedIncrease;
        public uint TithingPoints;
        public ushort Weight;
        public ushort WeightMax;

        public Item FindBandage()
        {
            Item backpack = FindItemByLayer(Layer.Backpack);
            Item item = null;

            if (backpack != null)
            {
                item = backpack.FindItem(0x0E21);
            }

            return item;
        }

        public Item FindItemByGraphic(ushort graphic)
        {
            Item backpack = FindItemByLayer(Layer.Backpack);

            if (backpack != null)
            {
                return FindItemInContainerRecursive(backpack, graphic);
            }

            return null;
        }

        public Item FindItemByCliloc(int cliloc)
        {
            Item backpack = FindItemByLayer(Layer.Backpack);

            if (backpack != null)
            {
                return FindItemByClilocInContainerRecursive(backpack, cliloc);
            }

            return null;
        }

        private Item FindItemInContainerRecursive(Item container, ushort graphic)
        {
            Item found = null;

            if (container != null)
            {
                for (LinkedObject i = container.Items; i != null; i = i.Next)
                {
                    Item item = (Item) i;

                    if (item.Graphic == graphic)
                    {
                        return item;
                    }

                    if (!item.IsEmpty)
                    {
                        found = FindItemInContainerRecursive(item, graphic);

                        if (found != null && found.Graphic == graphic)
                        {
                            return found;
                        }
                    }
                }
            }

            return found;
        }

        private Item FindItemByClilocInContainerRecursive(Item container, int cliloc)
        {
            Item found = null;

            if (container != null)
            {
                for (LinkedObject i = container.Items; i != null; i = i.Next)
                {
                    Item item = (Item) i;


                    if (cliloc == World.OPL.GetNameCliloc(item.Serial))
                    {
                        return item;
                    }

                    if (!item.IsEmpty)
                    {
                        found = FindItemByClilocInContainerRecursive(item, cliloc);

                        if (found != null && cliloc == World.OPL.GetNameCliloc(found.Serial))
                        {
                            return found;
                        }
                    }
                }
            }

            return found;
        }

        public Item FindPreferredItemByCliloc(System.Span<int> clilocs)
        {
            Item item = null;

            for (int i = 0; i < clilocs.Length; i++)
            {
                item = World.Player.FindItemByCliloc(clilocs[i]);

                if (item != null)
                {
                    break;
                }
            }

            return item;
        }

        public void AddBuff(BuffIconType type, ushort graphic, uint time, string text)
        {
            _buffIcons[type] = new BuffIcon(type, graphic, time, text);
        }


        public bool IsBuffIconExists(BuffIconType graphic)
        {
            return _buffIcons.ContainsKey(graphic);
        }

        public void RemoveBuff(BuffIconType graphic)
        {
            _buffIcons.Remove(graphic);
        }

        public void UpdateAbilities()
        {
            AbilityData.DefaultItemAbilities.Set(Abilities);
            
            if ((FindItemByLayer(Layer.OneHanded) ?? FindItemByLayer(Layer.TwoHanded)) is { Graphic: > 0 } weapon)
            {
                ushort animId = weapon.ItemData.AnimID;
                ushort animGraphic = 0;

                if (Client.Game.UO.FileManager.TileData.StaticData[weapon.Graphic - 1].AnimID == animId)
                {
                    animGraphic = (ushort)(weapon.Graphic - 1);
                }
                else if (Client.Game.UO.FileManager.TileData.StaticData[weapon.Graphic + 1].AnimID == animId)
                {
                    animGraphic = (ushort)(weapon.Graphic + 1);
                }

                if (AbilityData.GraphicToAbilitiesMap.TryGetValue(weapon.Graphic, out var abilities) || AbilityData.GraphicToAbilitiesMap.TryGetValue(animGraphic, out abilities))
                {
                    abilities.Set(Abilities);
                }
                else
                {
                    Log.Warn($"Could not update abilities ${weapon.Graphic} \"${weapon.Name}\" has no GraphicToAbilitiesMap[graphic] data");
                }
            }

            for (LinkedListNode<Gump> gump = UIManager.Gumps.First; gump != null; gump = gump.Next)
            {
                if (gump.Value is UseAbilityButtonGump or CombatBookGump)
                    gump.Value.RequestUpdateContents();
            }
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();

            Plugin.UpdatePlayerPosition(X, Y, Z);

            TryOpenDoors();
            TryOpenCorpses();
        }

        public void TryOpenCorpses()
        {
            if (ProfileManager.CurrentProfile.AutoOpenCorpses)
            {
                if ((ProfileManager.CurrentProfile.CorpseOpenOptions == 1 || ProfileManager.CurrentProfile.CorpseOpenOptions == 3) && World.TargetManager.IsTargeting)
                {
                    return;
                }

                if ((ProfileManager.CurrentProfile.CorpseOpenOptions == 2 || ProfileManager.CurrentProfile.CorpseOpenOptions == 3) && IsHidden)
                {
                    return;
                }

                foreach (Item item in World.Items.Values)
                {
                    if (!item.IsDestroyed && item.IsCorpse && item.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange && !AutoOpenedCorpses.Contains(item.Serial))
                    {
                        AutoOpenedCorpses.Add(item.Serial);
                        GameActions.DoubleClickQueued(item.Serial);
                    }
                }
            }
        }


        protected override void OnDirectionChanged()
        {
            base.OnDirectionChanged();
            TryOpenDoors();
        }

        private void TryOpenDoors()
        {
            if (!World.Player.IsDead && ProfileManager.CurrentProfile.AutoOpenDoors)
            {
                int x = X, y = Y, z = Z;
                Pathfinder.GetNewXY((byte) Direction, ref x, ref y);

                if (World.Items.Values.Any(s => s.ItemData.IsDoor && s.X == x && s.Y == y && s.Z - 15 <= z && s.Z + 15 >= z))
                {
                    GameActions.OpenDoor();
                }
            }
        }

        public override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            DeathScreenTimer = 0;

            Log.Warn("PlayerMobile disposed!");
            base.Destroy();
        }

        public void CloseBank()
        {
            Item bank = FindItemByLayer(Layer.Bank);

            if (bank != null && bank.Opened)
            {
                if (!bank.IsEmpty)
                {
                    Item first = (Item) bank.Items;

                    while (first != null)
                    {
                        Item next = (Item) first.Next;

                        World.RemoveItem(first, true);

                        first = next;
                    }

                    bank.Items = null;
                }

                UIManager.GetGump<ContainerGump>(bank.Serial)?.Dispose();

                bank.Opened = false;
            }
        }

        public void CloseRangedGumps()
        {
            foreach (Gump gump in UIManager.Gumps)
            {
                switch (gump)
                {
                    case PaperDollGump _:
                    case MapGump _:
                    case SpellbookGump _:

                        if (World.Get(gump.LocalSerial) == null)
                        {
                            gump.Dispose();
                        }

                        break;

                    case TradingGump _:
                    case ShopGump _:

                        Entity ent = World.Get(gump.LocalSerial);
                        int distance = int.MaxValue;

                        if (ent != null)
                        {
                            if (SerialHelper.IsItem(ent.Serial))
                            {
                                Entity top = World.Get(((Item) ent).RootContainer);

                                if (top != null)
                                {
                                    distance = top.Distance;
                                }
                            }
                            else
                            {
                                distance = ent.Distance;
                            }
                        }

                        if (distance > Constants.MIN_VIEW_RANGE)
                        {
                            gump.Dispose();
                        }

                        break;

                    case ContainerGump _:
                        distance = int.MaxValue;

                        ent = World.Get(gump.LocalSerial);

                        if (ent != null)
                        {
                            if (SerialHelper.IsItem(ent.Serial))
                            {
                                Entity top = World.Get(((Item) ent).RootContainer);

                                if (top != null)
                                {
                                    distance = top.Distance;
                                }
                            }
                            else
                            {
                                distance = ent.Distance;
                            }
                        }

                        if (distance > Constants.MAX_CONTAINER_OPENED_ON_GROUND_RANGE)
                        {
                            gump.Dispose();
                        }

                        break;
                }
            }
        }


        //public override void Update()
        //{
        //    base.Update();

        //    //const int TIME_TURN_TO_LASTTARGET = 2000;

        //    //if (TargetManager.LastAttack != 0 &&
        //    //    InWarMode &&
        //    //    Walker.LastStepRequestTime + TIME_TURN_TO_LASTTARGET < Time.Ticks)
        //    //{
        //    //    Mobile enemy = World.Mobiles.Get(TargetManager.LastAttack);

        //    //    if (enemy != null && enemy.Distance <= 1)
        //    //    {
        //    //        Direction pdir = DirectionHelper.GetDirectionAB(World.Player.X,
        //    //                                                        World.Player.Y,
        //    //                                                        enemy.X,
        //    //                                                        enemy.Y);

        //    //        if (Direction != pdir)
        //    //            Walk(pdir, false);
        //    //    }
        //    //}
        //}

        // ############# DO NOT DELETE IT! #############
        //protected override bool NoIterateAnimIndex()
        //{
        //    return false;
        //}
        // #############################################

        public bool Walk(Direction direction, bool run)
        {
            if (Walker.WalkingFailed || Walker.LastStepRequestTime > Time.Ticks || Walker.StepsCount >= Constants.MAX_STEP_COUNT || Client.Game.UO.Version >= ClientVersion.CV_60142 && IsParalyzed)
            {
                return false;
            }

            run |= ProfileManager.CurrentProfile.AlwaysRun;

            if (SpeedMode >= CharacterSpeedType.CantRun || Stamina <= 1 && !IsDead || IsHidden && ProfileManager.CurrentProfile.AlwaysRunUnlessHidden)
            {
                run = false;
            }

            int x = X;
            int y = Y;
            sbyte z = Z;
            Direction oldDirection = Direction;

            bool emptyStack = Steps.Count == 0;

            if (!emptyStack)
            {
                ref Step walkStep = ref Steps.Back();
                x = walkStep.X;
                y = walkStep.Y;
                z = walkStep.Z;
                oldDirection = (Direction) walkStep.Direction;
            }

            sbyte oldZ = z;
            ushort walkTime = Constants.TURN_DELAY;

            if ((oldDirection & Direction.Mask) == (direction & Direction.Mask))
            {
                Direction newDir = direction;
                int newX = x;
                int newY = y;
                sbyte newZ = z;

                if (!Pathfinder.CanWalk(ref newDir, ref newX, ref newY, ref newZ))
                {
                    return false;
                }

                if ((direction & Direction.Mask) != newDir)
                {
                    direction = newDir;
                }
                else
                {
                    direction = newDir;
                    x = newX;
                    y = newY;
                    z = newZ;

                    walkTime = (ushort) MovementSpeed.TimeToCompleteMovement(run, IsMounted || SpeedMode == CharacterSpeedType.FastUnmount || SpeedMode == CharacterSpeedType.FastUnmountAndCantRun || IsFlying);
                }
            }
            else
            {
                Direction newDir = direction;
                int newX = x;
                int newY = y;
                sbyte newZ = z;

                if (!Pathfinder.CanWalk(ref newDir, ref newX, ref newY, ref newZ))
                {
                    if ((oldDirection & Direction.Mask) == newDir)
                    {
                        return false;
                    }
                }

                if ((oldDirection & Direction.Mask) == newDir)
                {
                    x = newX;
                    y = newY;
                    z = newZ;

                    walkTime = (ushort) MovementSpeed.TimeToCompleteMovement(run, IsMounted || SpeedMode == CharacterSpeedType.FastUnmount || SpeedMode == CharacterSpeedType.FastUnmountAndCantRun || IsFlying);
                }

                direction = newDir;
            }

            CloseBank();

            if (emptyStack)
            {
                if (!IsWalking)
                {
                    SetAnimation(0xFF);
                }

                LastStepTime = Time.Ticks;
            }

            ref StepInfo step = ref Walker.StepInfos[Walker.StepsCount];
            step.Sequence = Walker.WalkSequence;
            step.Accepted = false;
            step.Running = run;
            step.OldDirection = (byte) (oldDirection & Direction.Mask);
            step.Direction = (byte) direction;
            step.Timer = Time.Ticks;
            step.X = (ushort) x;
            step.Y = (ushort) y;
            step.Z = z;
            step.NoRotation = step.OldDirection == (byte) direction && oldZ - z >= 11;

            Walker.StepsCount++;

            Steps.AddToBack
            (
                new Step
                {
                    X = x,
                    Y = y,
                    Z = z,
                    Direction = (byte) direction,
                    Run = run
                }
            );


            NetClient.Socket.Send_WalkRequest(direction, Walker.WalkSequence, run, Walker.FastWalkStack.GetValue());


            if (Walker.WalkSequence == 0xFF)
            {
                Walker.WalkSequence = 1;
            }
            else
            {
                Walker.WalkSequence++;
            }

            Walker.UnacceptedPacketsCount++;

            AddToTile();

            int nowDelta = 0;

            //if (_lastDir == (int) direction && _lastMount == IsMounted && _lastRun == run)
            //{
            //    nowDelta = (int) (Time.Ticks - _lastStepTime - walkTime + _lastDelta);

            //    if (Math.Abs(nowDelta) > 70)
            //        nowDelta = 0;
            //    _lastDelta = nowDelta;
            //}
            //else
            //    _lastDelta = 0;

            //_lastStepTime = (int) Time.Ticks;
            //_lastRun = run;
            //_lastMount = IsMounted;
            //_lastDir = (int) direction;


            Walker.LastStepRequestTime = Time.Ticks + walkTime - nowDelta;
            GetGroupForAnimation(this, 0, true);

            return true;
        }
    }
}