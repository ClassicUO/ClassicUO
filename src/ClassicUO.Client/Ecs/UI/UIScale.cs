namespace ClassicUO.Ecs;

// Container/gump scale settings ported off the legacy UIManager.ContainerScale +
// Profile.ScaleItemsInsideContainers globals. Owned as an App resource so
// systems can mutate it without touching the legacy world.
internal sealed class UIScale
{
    public float ContainerScale { get; set; } = 1f;
    public bool ScaleItemsInsideContainers { get; set; }
    public bool HueContainerGumps { get; set; } = true;
    public int BackpackStyle { get; set; }
    public bool RelativeDragAndDropItems { get; set; }
    public bool SkipEmptyCorpse { get; set; }
    public bool OverrideContainerLocation { get; set; }
}
