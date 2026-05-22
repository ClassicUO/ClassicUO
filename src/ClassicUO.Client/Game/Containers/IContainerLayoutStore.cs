// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Containers
{
    /// <summary>
    /// Container layout dictionary keyed by gump graphic. One responsibility:
    /// load / persist <c>containers.txt</c> and provide layout lookups (with a
    /// lazily-created default fallback for unknown graphics).
    /// </summary>
    internal interface IContainerLayoutStore
    {
        ContainerData Get(ushort graphic);
        void BuildContainerFile(bool force);
    }
}
