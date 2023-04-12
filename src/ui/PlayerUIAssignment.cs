using Godot;

namespace Recursia;
public partial class PlayerUIAssignment : Node
{
    [Export] public NodePath HealthBar;
    [Export] public NodePath Inventory;
    [Export] public NodePath MouseInventory;
    [Export] public NodePath RecipeList;
    [Export] public NodePath CoordinateText;

    public override void _EnterTree()
    {
        Player.OnLocalPlayerAssigned += Assign;
        if (Player.LocalPlayer != null) Assign(Player.LocalPlayer);
        base._EnterTree();
    }

    public void Assign(Player player)
    {
        GetNode<HealthBar>(HealthBar).Tracking = player;
        GetNode<InventoryUI>(Inventory).TrackInventory(player.Inventory);
        GetNode<InventoryUI>(MouseInventory).TrackInventory(player.MouseInventory);
        GetNode<RecipeListUI>(RecipeList).DisplayList(RecpieList.Search(""));
        GetNode<CoordinateTextUI>(CoordinateText).Tracking = player;
        base._Ready();
    }
}