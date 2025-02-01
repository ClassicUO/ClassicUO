using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Ecs;

internal sealed class AssetsServer
{
    private readonly UOFileManager _fileManager;

    internal AssetsServer(UOFileManager fileManager, GraphicsDevice device)
    {
        _fileManager = fileManager;

        Arts = new Renderer.Arts.Art(fileManager.Arts, fileManager.Hues, device);
        Texmaps = new Renderer.Texmaps.Texmap(fileManager.Texmaps, device);
        Animations = new Renderer.Animations.Animations(fileManager.Animations, device);
        Gumps = new Renderer.Gumps.Gump(fileManager.Gumps, device);
    }


    public Renderer.Arts.Art Arts { get; }
    public Renderer.Animations.Animations Animations { get; }
    public Renderer.Texmaps.Texmap Texmaps { get; }
    public Renderer.Gumps.Gump Gumps { get; }
}
