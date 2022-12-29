using Godot;

public class PlayerUIAssignment : Node
{
    public override void _Ready()
    {
        GetNode<HealthBar>("HealthBar").Tracking = World.Singleton.Players[0];
        GetNode<InventoryUI>("InventoryUI").TrackInventory(World.Singleton.Players[0].Inventory);
        GetNode<InventoryUI>("FollowMouse/MouseInventoryUI").TrackInventory(World.Singleton.Players[0].MouseInventory);
        base._Ready();
    }
}