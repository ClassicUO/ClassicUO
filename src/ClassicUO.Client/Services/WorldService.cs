using ClassicUO.Game;

namespace ClassicUO.Services;

internal class WorldService : IService
{
    private readonly World _world;

    public WorldService(World world)
    {
        _world = world;
    }

    public World World => _world;
}