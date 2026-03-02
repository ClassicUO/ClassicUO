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
}
