using Godot;

public class PlayerUIAssignment : Node
{
    [Export] public NodePath HealthBar;
    [Export] public NodePath Inventory;
    [Export] public NodePath MouseInventory;
    [Export] public NodePath RecipeList;

    public override void _Ready()
    {
        GetNode<HealthBar>(HealthBar).Tracking = World.Singleton.Players[0];
        GetNode<InventoryUI>(Inventory).TrackInventory(World.Singleton.Players[0].Inventory);
        GetNode<InventoryUI>(MouseInventory).TrackInventory(World.Singleton.Players[0].MouseInventory);
        GetNode<RecipeListUI>(RecipeList).DisplayList(RecpieList.Search(""));
        base._Ready();
    }
}