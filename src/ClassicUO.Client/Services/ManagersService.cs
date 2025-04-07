using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Platforms;

namespace ClassicUO.Services;

internal class ManagersService : IService
{
    public WorldMapEntityManager WMapManager { get; } = new();

    public ActiveSpellIconsManager ActiveSpellIcons { get; } = new();

    public ObjectPropertiesListManager OPL { get; } = new ();

    public CorpseManager CorpseManager { get; } = new();

    public PartyManager Party { get; } = new();

    public HouseManager HouseManager { get; } = new();

    public MessageManager MessageManager { get; } = new();

    public ContainerManager ContainerManager { get; } = new();

    public IgnoreManager IgnoreManager { get; } = new();

    public SkillsGroupManager SkillsGroupManager { get; } = new();

    public AuraManager AuraManager { get; } = new();

    public UoAssist UoAssist { get; } = new();

    public TargetManager TargetManager { get; } = new();

    public DelayedObjectClickManager DelayedObjectClickManager { get; } = new();

    public BoatMovingManager BoatMovingManager { get; } = new();

    public NameOverHeadManager NameOverHeadManager { get; } = new();

    public MacroManager Macros { get; } = new();

    public CommandManager CommandManager { get; } = new();

    public Weather Weather { get; } = new();
}