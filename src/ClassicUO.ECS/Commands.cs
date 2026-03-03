// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.ECS
{
    // ── Infrastructure ──────────────────────────────────────────────────

    /// <summary>Tag marking a transient command entity. Destroyed after NetApply.</summary>
    public struct NetworkCommand;

    /// <summary>
    /// Sequence index for deterministic command ordering within a frame.
    /// NetApply queries sort by this value to preserve packet order.
    /// </summary>
    public record struct SequenceIndex(int Index);

    /// <summary>Debug counters for network command diagnostics.</summary>
    public record struct NetDebugCounters(
        int CommandsEnqueued,
        int CommandsApplied,
        int CommandsFailed
    );

    // ── Phase A: Enter World ─────────────────────────────────────────────

    /// <summary>
    /// 0x1B EnterWorld. Creates/bootstraps the player entity on login.
    /// Must be processed before other NetApply commands.
    /// </summary>
    public struct CmdEnterWorld
    {
        public uint Serial;
        public ushort Graphic;
        public ushort X;
        public ushort Y;
        public sbyte Z;
        public byte Direction;
        public byte MapIndex;
    }

    // ── Phase A: Movement / Player Update Commands ──────────────────────

    /// <summary>
    /// 0x20 UpdatePlayer, 0x77 UpdateCharacter, 0x78 UpdateObject (mobile path).
    /// Creates or updates a mobile entity.
    /// </summary>
    public struct CmdCreateOrUpdateMobile
    {
        public uint Serial;
        public ushort Graphic;
        public ushort Hue;
        public uint Flags;
        public ushort X;
        public ushort Y;
        public sbyte Z;
        public byte Direction;
        public byte Notoriety;
        public bool IsPlayer;
    }

    /// <summary>0x22 ConfirmWalk. Confirms a pending movement step.</summary>
    public struct CmdConfirmWalk
    {
        public byte Sequence;
        public byte Notoriety;
    }

    /// <summary>0x21 DenyWalk. Rejects a pending movement step.</summary>
    public struct CmdDenyWalk
    {
        public byte Sequence;
        public ushort X;
        public ushort Y;
        public sbyte Z;
        public byte Direction;
    }

    /// <summary>0x97 MovePlayer. Server-initiated direction change.</summary>
    public struct CmdMovePlayer
    {
        public byte Direction;
        public bool Running;
    }

    // ── Phase A: Item / Container Lifecycle Commands ─────────────────────

    /// <summary>
    /// 0x1A UpdateItem, 0x78 UpdateObject (item path).
    /// Creates or updates an item entity on the ground or in the world.
    /// </summary>
    public struct CmdCreateOrUpdateItem
    {
        public uint Serial;
        public ushort Graphic;
        public ushort Hue;
        public ushort Amount;
        public ushort X;
        public ushort Y;
        public sbyte Z;
        public byte Direction;
        public uint Flags;
        public byte ItemType; // 0=normal, 1=multi, 2=?
    }

    /// <summary>0x1D DeleteObject. Removes a mobile or item.</summary>
    public struct CmdDeleteEntity
    {
        public uint Serial;
    }

    /// <summary>0x24 OpenContainer. Triggers container/spellbook/vendor UI.</summary>
    public struct CmdOpenContainer
    {
        public uint Serial;
        public ushort Graphic;
    }

    /// <summary>
    /// 0x25 UpdateContainedItem (single), also used per-item in 0x3C bulk.
    /// Adds or updates an item inside a container.
    /// </summary>
    public struct CmdContainedItem
    {
        public uint Serial;
        public ushort Graphic;
        public ushort Amount;
        public ushort X;
        public ushort Y;
        public uint ContainerSerial;
        public ushort Hue;
    }

    /// <summary>
    /// Emitted before 0x3C bulk items to clear existing container contents.
    /// </summary>
    public struct CmdClearContainer
    {
        public uint ContainerSerial;
        public bool KeepEquipped;
    }

    /// <summary>0x2E EquipItem. Places an item on a mobile's equipment layer.</summary>
    public struct CmdEquipItem
    {
        public uint Serial;
        public ushort Graphic;
        public byte Layer;
        public uint ContainerSerial;
        public ushort Hue;
    }

    // ── Phase B: Status / Combat Commands ───────────────────────────────

    /// <summary>Vitals update (covers 0xA1, 0xA2, 0xA3 packets).</summary>
    public struct CmdUpdateVitals
    {
        public uint Serial;
        public ushort Hits;
        public ushort HitsMax;
        public ushort Mana;
        public ushort ManaMax;
        public ushort Stamina;
        public ushort StaminaMax;
        /// <summary>Bitmask: 1=Hits, 2=Mana, 4=Stamina (which fields are valid).</summary>
        public byte ValidFields;
    }

    /// <summary>0x72 SetWarmode.</summary>
    public struct CmdSetWarmode
    {
        public uint Serial;
        public bool WarMode;
    }

    /// <summary>Map change command (0xBF sub, etc.).</summary>
    public struct CmdSetMap
    {
        public byte MapIndex;
    }

    /// <summary>Season change command.</summary>
    public struct CmdChangeSeason
    {
        public byte Season;
        public byte MusicIndex;
    }

    // ── Phase B: Combat Commands ─────────────────────────────────────────

    /// <summary>0xAA AttackCharacter. Server confirms attack target.</summary>
    public struct CmdAttackTarget
    {
        public uint TargetSerial;
    }

    /// <summary>0x2F Swing. Server notifies a melee swing from attacker to defender.</summary>
    public struct CmdSwing
    {
        public uint AttackerSerial;
        public uint DefenderSerial;
    }

    /// <summary>0x0B Damage. Server sends damage amount for overhead display.</summary>
    public struct CmdDamage
    {
        public uint Serial;
        public ushort Amount;
    }

    // ── Animation Commands ─────────────────────────────────────────────

    /// <summary>0x6E CharacterAnimation. Server-driven animation on a mobile.</summary>
    public struct CmdCharacterAnimation
    {
        public uint Serial;
        public ushort Action;
        public byte FrameCount;
        public byte RepeatCount;
        public bool Forward;
        public bool Repeat;
        public byte Delay;
    }

    /// <summary>0xE2 NewCharacterAnimation. SA/HS era animation format.</summary>
    public struct CmdNewCharacterAnimation
    {
        public uint Serial;
        public ushort AnimationType;
        public ushort Action;
        public byte Mode;
    }

    // ── Stats / Skills Commands ──────────────────────────────────────────

    /// <summary>0x11 CharacterStatus. Full stat block for a mobile.</summary>
    public struct CmdCharacterStatus
    {
        public uint Serial;
        public byte StatusType; // 0-6, determines which extended fields are valid
        public ushort Hits, HitsMax, Mana, ManaMax, Stamina, StaminaMax;
        // Extended fields (valid when StatusType >= 3):
        public ushort Str, Dex, Int;
        public uint Gold;
        public ushort Weight, WeightMax;
        public byte Race;
        public ushort PhysResist, FireResist, ColdResist, PoisonResist, EnergyResist;
        public short Luck;
        public ushort DamageMin, DamageMax;
        public uint TithingPoints;
        public ushort StatsCap;
        public byte Followers, FollowersMax;
        // Type >= 6 extended combat stats:
        public short MaxPhysResist, MaxFireResist, MaxColdResist, MaxPoisonResist, MaxEnergyResist;
        public short DefenseChanceInc, MaxDefenseChanceInc;
        public short HitChanceInc, SwingSpeedInc, DamageInc;
        public short LowerReagentCost, SpellDamageInc;
        public short FasterCastRecovery, FasterCasting, LowerManaCost;
    }

    /// <summary>0x3A SkillList — single skill update.</summary>
    public struct CmdUpdateSkill
    {
        public ushort SkillId;
        public ushort Value;
        public ushort Base;
        public ushort Cap;
        public byte Lock;
    }

    /// <summary>0xBF sub-0x19 ExtendedStats.</summary>
    public struct CmdExtendedStats
    {
        public uint Serial;
        public byte SubType;    // 2=StatLock, 4=BondedStatus, 5=AnimOverride
        public byte StatIndex;  // for SubType 2: 0=Str, 1=Dex, 2=Int
        public byte LockValue;  // for SubType 2: lock value
        public bool BondedDead; // for SubType 4
    }

    /// <summary>0xD6 OPL revision. Tracks tooltip property list version.</summary>
    public struct CmdOplRevision
    {
        public uint Serial;
        public uint Revision;
    }

    // ── Death / Corpse Commands ─────────────────────────────────────────

    /// <summary>0xAF DisplayDeath. A mobile has died; create corpse, set dead state.</summary>
    public struct CmdDisplayDeath
    {
        public uint Serial;        // dying mobile serial
        public uint CorpseSerial;  // corpse item serial
        public bool WasRunning;
    }

    /// <summary>0x2C DeathScreen. The player has died.</summary>
    public struct CmdDeathScreen
    {
        public byte Action; // 1 = resurface, other = death
    }

    /// <summary>0x89 CorpseEquipment. Assigns an item layer on a corpse.</summary>
    public struct CmdCorpseEquipment
    {
        public uint CorpseSerial;
        public byte Layer;
        public uint ItemSerial;
    }

    // ── Player Input Commands ────────────────────────────────────────────
    // Emitted by UI/input handlers in the Input phase, consumed by systems.

    /// <summary>Player requests a movement step in a direction.</summary>
    public struct CmdRequestMove
    {
        public byte Direction;
        public bool Run;
    }

    /// <summary>Player requests to attack a target.</summary>
    public struct CmdRequestAttack
    {
        public uint TargetSerial;
    }

    /// <summary>Player double-clicks (uses) an object.</summary>
    public struct CmdUseObject
    {
        public uint Serial;
    }

    /// <summary>Player single-clicks an object (request name/tooltip).</summary>
    public struct CmdSingleClick
    {
        public uint Serial;
    }

    /// <summary>Player picks up an item.</summary>
    public struct CmdPickUp
    {
        public uint Serial;
        public ushort Amount;
    }

    /// <summary>Player drops an item at a world position or into a container.</summary>
    public struct CmdDropItem
    {
        public uint Serial;
        public ushort X;
        public ushort Y;
        public sbyte Z;
        public uint ContainerSerial; // 0xFFFFFFFF = ground
    }

    /// <summary>Player equips an item.</summary>
    public struct CmdEquipRequest
    {
        public uint Serial;
        public byte Layer;
    }

    /// <summary>Player toggles warmode.</summary>
    public struct CmdToggleWarMode
    {
        public bool WarMode;
    }

    /// <summary>Player sets a target (entity or ground).</summary>
    public struct CmdTargetEntity
    {
        public uint Serial;
    }

    /// <summary>Player sets a ground target.</summary>
    public struct CmdTargetPosition
    {
        public ushort X;
        public ushort Y;
        public sbyte Z;
        public ushort Graphic;
    }

    /// <summary>Player cancels current targeting.</summary>
    public struct CmdCancelTarget;

    /// <summary>Player casts a spell by index.</summary>
    public struct CmdCastSpell
    {
        public int SpellIndex;
    }

    /// <summary>Player uses a skill by index.</summary>
    public struct CmdUseSkill
    {
        public int SkillIndex;
    }

    // ── World / Environment Commands ─────────────────────────────────────

    /// <summary>0xF6 BoatMoving. Updates boat position and direction.</summary>
    public struct CmdBoatMoving
    {
        public uint BoatSerial;
        public byte Speed;
        public byte MovingDirection;
        public byte FacingDirection;
        public ushort X, Y;
        public short Z;
    }

    /// <summary>Sub-entity update within a boat move packet.</summary>
    public struct CmdBoatEntityUpdate
    {
        public uint Serial;
        public ushort X, Y;
        public short Z;
    }

    /// <summary>0x38 Pathfinding. Server requests pathfind to position.</summary>
    public struct CmdPathfind
    {
        public ushort X, Y;
        public ushort Z;
    }

    /// <summary>0x65 SetWeather.</summary>
    public struct CmdSetWeather
    {
        public byte Type;
        public byte Count;
        public byte Temperature;
    }

    /// <summary>0x54 PlaySoundEffect.</summary>
    public struct CmdPlaySound
    {
        public ushort SoundId;
        public ushort X, Y;
        public short Z;
    }

    /// <summary>0x6D PlayMusic.</summary>
    public struct CmdPlayMusic
    {
        public ushort MusicId;
    }

    /// <summary>0xBF sub-0x1D / 0xD8 HouseRevision tracking.</summary>
    public struct CmdHouseRevision
    {
        public uint Serial;
        public uint Revision;
    }

    // ── Name / Social Commands ──────────────────────────────────────────

    /// <summary>0x98 UpdateName. Updates entity name via string table index.</summary>
    public struct CmdUpdateName
    {
        public uint Serial;
        public int NameTableIndex;  // index into EcsRuntimeHost string table
    }

    /// <summary>Party add member.</summary>
    public struct CmdPartyAddMember
    {
        public uint Serial;
    }

    /// <summary>Party remove member.</summary>
    public struct CmdPartyRemoveMember
    {
        public uint Serial;
    }

    /// <summary>Party disband — clear all members.</summary>
    public struct CmdPartyDisband;

    /// <summary>
    /// Speech/overhead text. Uses string table index for text content.
    /// Type: 0=speech, 1=system, 2=emote, 6=label, etc.
    /// </summary>
    public struct CmdSpeech
    {
        public uint Serial;
        public ushort Graphic;
        public byte Type;
        public ushort Hue;
        public ushort Font;
        public uint ClilocId;       // 0 if raw text
        public int TextIndex;       // index into EcsRuntimeHost string table
        public int NameIndex;       // index into EcsRuntimeHost string table
    }

    // ── Health Bar / Buff Commands ─────────────────────────────────────

    /// <summary>0x16/0x17 NewHealthbarUpdate. Per-mobile SA healthbar flags.</summary>
    public struct CmdHealthBarUpdate
    {
        public uint Serial;
        public ushort Type;     // 1=green (poison), 2=yellow
        public bool Enabled;
    }

    /// <summary>0xDF AddBuff. Adds a buff/debuff icon to a mobile.</summary>
    public struct CmdAddBuff
    {
        public uint Serial;
        public ushort IconId;
        public ushort Duration;
        public uint TitleCliloc;
        public uint DescriptionCliloc;
    }

    /// <summary>0xDF RemoveBuff. Removes a buff/debuff icon from a mobile.</summary>
    public struct CmdRemoveBuff
    {
        public uint Serial;
        public ushort IconId;
    }

    /// <summary>0xDE UpdateMobileStatus. Stub for future use.</summary>
    public struct CmdMobileStatus
    {
        public uint Serial;
        public byte Status;
        public uint AttackerSerial;
    }

    // ── Effect Spawn Commands ────────────────────────────────────────

    /// <summary>
    /// 0x70 / 0xC0 / 0xC7 GraphicEffect. Creates a visual effect entity.
    /// Type: 0=moving, 1=lightning, 2=fixed, 3=moving+fixed target.
    /// </summary>
    public struct CmdSpawnEffect
    {
        public byte Type;
        public uint SourceSerial;
        public uint TargetSerial;
        public ushort Graphic;
        public ushort Hue;
        public ushort SourceX, SourceY;
        public sbyte SourceZ;
        public ushort TargetX, TargetY;
        public sbyte TargetZ;
        public byte Speed;
        public byte Duration;
        public bool FixedDirection;
        public bool Explode;
        public uint RenderMode;
        public ushort EffectId;
    }

    /// <summary>0x23 DragAnimation. Item drag visual effect between two positions.</summary>
    public struct CmdDragEffect
    {
        public ushort Graphic;
        public ushort Hue;
        public ushort FromX, FromY;
        public sbyte FromZ;
        public ushort ToX, ToY;
        public sbyte ToZ;
    }

    // ── Item Hold / Drag Commands ────────────────────────────────────

    /// <summary>0x27 DenyMoveItem. Server rejects item pick up / move.</summary>
    public struct CmdDenyMoveItem
    {
        public byte Reason;
    }

    /// <summary>0x29 EndDraggingItem. Server signals end of drag.</summary>
    public struct CmdEndDragging;

    /// <summary>0x2A DropItemAccepted. Server confirms item drop.</summary>
    public struct CmdDropItemAccepted;

    // ── Singleton-Update Commands ───────────────────────────────────
    // Emitted by packet handlers to update ECS singletons.

    /// <summary>0xC8 ClientViewRange.</summary>
    public struct CmdSetViewRange
    {
        public byte Range;
    }

    /// <summary>0xB9 EnableLockedFeatures.</summary>
    public struct CmdSetLockedFeatures
    {
        public ulong Flags;
    }

    /// <summary>0xBF sub-0x26 SpeedMode.</summary>
    public struct CmdSetSpeedMode
    {
        public byte Mode;
    }

    /// <summary>0x4E PersonalLightLevel.</summary>
    public struct CmdSetPersonalLight
    {
        public byte Level;
    }

    /// <summary>0x4F LightLevel (overall).</summary>
    public struct CmdSetOverallLight
    {
        public byte Level;
    }
}
