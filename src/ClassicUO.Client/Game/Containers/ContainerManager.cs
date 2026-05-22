// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Containers
{
    /// <summary>
    /// Facade over the Containers collaborators: keeps the existing public
    /// surface (<c>_world.ContainerManager.X</c>) and delegates to dedicated
    /// cohesive classes. Layout storage lives in
    /// <see cref="IContainerLayoutStore"/>, gump placement in
    /// <see cref="IContainerPositionCalculator"/>. <see cref="PendingGridLootSerial"/>
    /// is a single shared field that stays on the facade — too small to warrant
    /// its own collaborator.
    /// </summary>
    internal sealed class ContainerManager
    {
        private readonly IContainerLayoutStore _layout;
        private readonly IContainerPositionCalculator _position;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public ContainerManager(World world)
            : this(new ContainerLayoutStore(), new ContainerPositionCalculator(world))
        {
        }

        /// <summary>Full DI seam — inject both collaborators.</summary>
        internal ContainerManager(IContainerLayoutStore layout, IContainerPositionCalculator position)
        {
            _layout = layout;
            _position = position;
            _layout.BuildContainerFile(false);
        }

        // ---- Position facade ----
        public int DefaultX => _position.DefaultX;
        public int DefaultY => _position.DefaultY;
        public int X => _position.X;
        public int Y => _position.Y;
        public void CalculateContainerPosition(uint serial, ushort graphic) => _position.CalculateContainerPosition(serial, graphic);

        // ---- Pending grid loot state ----
        public uint PendingGridLootSerial { get; set; }

        // ---- Layout facade ----
        public ContainerData Get(ushort graphic) => _layout.Get(graphic);
        public void BuildContainerFile(bool force) => _layout.BuildContainerFile(force);
    }
}
