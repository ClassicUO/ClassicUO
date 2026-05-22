// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Containers
{
    /// <summary>
    /// Facade over the Containers collaborators: keeps the existing public
    /// surface (<c>_world.ContainerManager.X</c>) and delegates to dedicated
    /// cohesive classes. Layout storage lives in
    /// <see cref="IContainerLayoutStore"/>, gump placement in
    /// <see cref="IContainerPositionCalculator"/>, and the grid-loot pending
    /// serial in <see cref="IGridLootPending"/>.
    /// </summary>
    internal sealed class ContainerManager
    {
        private readonly IContainerLayoutStore _layout;
        private readonly IContainerPositionCalculator _position;
        private readonly IGridLootPending _gridLoot;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public ContainerManager(World world)
            : this(new ContainerLayoutStore(), new ContainerPositionCalculator(world), new GridLootPending())
        {
        }

        /// <summary>Full DI seam — inject all collaborators.</summary>
        internal ContainerManager(IContainerLayoutStore layout, IContainerPositionCalculator position, IGridLootPending gridLoot)
        {
            _layout = layout;
            _position = position;
            _gridLoot = gridLoot;
            _layout.BuildContainerFile(false);
        }

        // ---- Position facade ----
        public int DefaultX => _position.DefaultX;
        public int DefaultY => _position.DefaultY;
        public int X => _position.X;
        public int Y => _position.Y;
        public void CalculateContainerPosition(uint serial, ushort graphic) => _position.CalculateContainerPosition(serial, graphic);

        // ---- Pending grid loot facade ----
        public uint PendingGridLootSerial
        {
            get => _gridLoot.Serial;
            set => _gridLoot.Serial = value;
        }

        // ---- Layout facade ----
        public ContainerData Get(ushort graphic) => _layout.Get(graphic);
        public void BuildContainerFile(bool force) => _layout.BuildContainerFile(force);
    }
}
