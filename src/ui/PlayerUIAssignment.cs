using Godot;

public partial class PlayerUIAssignment : Node
{
    [Export] public NodePath HealthBar;
    [Export] public NodePath Inventory;
    [Export] public NodePath MouseInventory;
    [Export] public NodePath RecipeList;
    [Export] public NodePath CoordinateText;

    public override void _Ready()
    {
        GetNode<HealthBar>(HealthBar).Tracking = World.Singleton.Players[0];
        GetNode<InventoryUI>(Inventory).TrackInventory(World.Singleton.Players[0].Inventory);
        GetNode<InventoryUI>(MouseInventory).TrackInventory(World.Singleton.Players[0].MouseInventory);
        GetNode<RecipeListUI>(RecipeList).DisplayList(RecpieList.Search(""));
        GetNode<CoordinateTextUI>(CoordinateText).Tracking = World.Singleton.Players[0];
        base._Ready();
    }
}