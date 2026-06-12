using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Ecs;

internal sealed class AssetsServer
{
    internal AssetsServer(UOFileManager fileManager, GraphicsDevice device)
    {
        Arts = new Renderer.Arts.Art(fileManager.Arts, fileManager.Hues, device);
        Texmaps = new Renderer.Texmaps.Texmap(fileManager.Texmaps, device);
        Animations = new Renderer.Animations.Animations(fileManager.Animations, device);
        Gumps = new Renderer.Gumps.Gump(fileManager.Gumps, device);
        MultiMaps = new Renderer.MultiMaps.MultiMap(fileManager.MultiMaps, device);
        Skills = fileManager.Skills;
        Sounds = new Renderer.Sounds.Sound(fileManager.Sounds);
    }

    // Test-only: an assets server with no loaders (loaders need UO files + a
    // GraphicsDevice, both absent headless). UiHitTest.PixelHit / UiPick only
    // reach the loaders for real sprite kinds; for UOCustomKind.None or a null
    // custom payload they answer from the bounding box alone — which is what
    // headless gump-gesture tests drive. Touching a real sprite mask through
    // this instance would NRE, by design.
    internal AssetsServer() { }


    public Renderer.Arts.Art Arts { get; }
    public Renderer.Animations.Animations Animations { get; }
    public Renderer.Texmaps.Texmap Texmaps { get; }
    public Renderer.Gumps.Gump Gumps { get; }
    public Renderer.MultiMaps.MultiMap MultiMaps { get; }
    public SkillsLoader Skills { get; }
    public Renderer.Sounds.Sound Sounds { get; }
}
