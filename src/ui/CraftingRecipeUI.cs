using Godot;
using System.Collections.Generic;

namespace Recursia;
public partial class CraftingRecipeUI : Control
{
    [Export]
    public PackedScene? ItemSlotUI;
    [Export]
    public int Padding = 2;
    [Export]
    public int SlotSizePx = 64;
    private readonly List<ItemSlotUI> slots = new();
    private Player? player;
    private Recipe? displaying;

    private float getHeight(int rows) => rows*SlotSizePx+(rows+1)*Padding;

    public override void _Ready()
    {
        if (ItemSlotUI == null)
        {
            GD.PushError($"Null ItemSlotUI on CraftingRecipeUI {Name}");
        }
    }

    //returns the height of the container after displaying everything
    //TODO: pooling
    public float DisplayRecipe(Recipe r)
    {
        player = Player.LocalPlayer;
        displaying = r;
        foreach (var slot in slots)
        {
            slot.QueueFree();
        }
        slots.Clear();
        Control ingredientsLabel = GetNode<Label>("IngredientsLabel");
        Control productsLabel = GetNode<Label>("ProductsLabel");
        //ingredients
        float offsetY = ingredientsLabel.Size.Y+Padding;
        int endrow = displayList(r.Ingredients, new Vector2(0,offsetY), 0);
        //products
        productsLabel.Position = new Vector2(productsLabel.Position.X, offsetY+getHeight(endrow+1)+Padding);
        offsetY += productsLabel.Size.Y+Padding*2;
        endrow = displayList(r.Product, new Vector2(0,offsetY), endrow+1, (_) => craft());
        Size = new Vector2(Size.X, offsetY + getHeight(endrow+1)+Padding);
        return Size.Y;
    }

    private int displayList(List<ItemStack> items, Vector2 offset, int startRow, System.Action<MouseButton>? onclick=null)
    {
        int slotsPerRow = Mathf.Max((int)Size.X/(SlotSizePx+Padding),1);
        int row = startRow-1;
        //ingredients
        for (int i = 0; i < items.Count; i++)
        {
            int column = i % slotsPerRow;
            if (column == 0) row++;
            ItemSlotUI slot = ItemSlotUI!.Instantiate<ItemSlotUI>();
            AddChild(slot);
            slot.Position = new Vector2(offset.X+column*SlotSizePx+(column+1)*Padding, offset.Y+getHeight(row));
            if (onclick != null) slot.OnClick += onclick;
            slot.DisplayItem(items[i]);
            slots.Add(slot);
        }
        return row;
    }
    private void craft()
    {
        //temporary
        if (displaying != null && player?.MouseInventory.Items[0].Size == 0) displaying.Craft(player.Inventory!, player.MouseInventory);
    }
}