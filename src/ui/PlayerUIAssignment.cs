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
        GetNode<HealthBar>(HealthBar).Tracking = Player.LocalPlayer;
        GetNode<InventoryUI>(Inventory).TrackInventory(Player.LocalPlayer.Inventory);
        GetNode<InventoryUI>(MouseInventory).TrackInventory(Player.LocalPlayer.MouseInventory);
        GetNode<RecipeListUI>(RecipeList).DisplayList(RecpieList.Search(""));
        GetNode<CoordinateTextUI>(CoordinateText).Tracking = Player.LocalPlayer;
        base._Ready();
    }
}