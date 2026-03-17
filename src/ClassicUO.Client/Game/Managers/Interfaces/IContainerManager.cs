using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers
{
    internal interface IContainerManager
    {
        int DefaultX { get; }
        int DefaultY { get; }
        int X { get; }
        int Y { get; }

        ContainerData Get(ushort graphic);
        void CalculateContainerPosition(uint serial, ushort g);
        void BuildContainerFile(bool force);
    }
}
