// Regression for "one item can't be moved after re-opening the backpack":
// TinyEcs RelationshipEntityMapper used to cascade World.Delete into a
// reparented/unlinked entity's children (CleanupPolicy.DeleteDescendants ran
// on detach, not just on deletion). Every 0x25/0x3C that re-AddChild'd a
// nested container (pouch/bag) wiped its loaded content entities, leaving
// container-window slots whose serials no longer resolve through
// NetworkEntitiesMap — pickup's gate failed and the item was undraggable
// until close+reopen recreated the entities.

using TinyEcs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

public class ReparentWipeRepro
{
    [Fact]
    public void ReAddChild_SameParent_KeepsGrandchildren()
    {
        using var world = new World();

        var backpack = world.Entity();
        var pouch = world.Entity();
        var itemInPouch = world.Entity();

        world.AddChild(backpack.ID, pouch.ID);     // pouch lives in backpack
        world.AddChild(pouch.ID, itemInPouch.ID);  // pouch contents loaded

        // Server re-lists backpack contents (0x3C on reopen): the pouch gets
        // AddChild'd to the same parent again. Its contents must survive.
        world.AddChild(backpack.ID, pouch.ID);

        Assert.True(world.Exists(pouch.ID));
        Assert.True(world.Exists(itemInPouch.ID));
        Assert.Equal(pouch.ID, world.GetParent(itemInPouch.ID));
    }

    // Gump teardown leans on this: despawning a window ROOT must cascade to
    // the whole subtree (RelationshipEntityMapper = DeleteDescendants), so
    // plugins don't need their own recursive DespawnSubtree walks.
    [Fact]
    public void Delete_Root_CascadesToWholeSubtree()
    {
        using var world = new World();

        var root = world.Entity();
        var child = world.Entity();
        var grandchild = world.Entity();

        world.AddChild(root.ID, child.ID);
        world.AddChild(child.ID, grandchild.ID);

        world.Delete(root.ID);

        Assert.False(world.Exists(root.ID));
        Assert.False(world.Exists(child.ID));
        Assert.False(world.Exists(grandchild.ID));
    }

    [Fact]
    public void RemoveChild_Unlink_KeepsChildren()
    {
        using var world = new World();

        var player = world.Entity();
        var quiver = world.Entity();
        var arrows = world.Entity();

        world.AddChild(player.ID, quiver.ID);
        world.AddChild(quiver.ID, arrows.ID);

        // 0x2E equip path: commands.RemoveChild(quiver) just unlinks it from
        // its old container — its contents must survive.
        world.RemoveChild(quiver.ID);

        Assert.True(world.Exists(quiver.ID));
        Assert.True(world.Exists(arrows.ID));
        Assert.Equal(quiver.ID, world.GetParent(arrows.ID));
    }
}
