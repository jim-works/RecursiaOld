using Godot;
using System.Collections.Generic;

namespace Recursia;
public partial class InventoryUI : Control
{
    [Export]
    public PackedScene ItemSlotUI;
    [Export]
    public int Padding = 2;
    [Export]
    public int SlotSizePx = 64;
    private Inventory tracking;
    private readonly List<ItemSlotUI> slots = new();
    private Player player;

    public void TrackInventory(Inventory inv)
    {
        if (tracking != null) tracking.OnUpdate -= onInventoryUpdate;
        tracking = inv;
        inv.OnUpdate += onInventoryUpdate;
        player = Player.LocalPlayer;
        resetItemSlots(inv);
    }

    //signal
    public void OnPause()
    {
        Visible = true;
    }

    //signal
    public void OnUnpause()
    {
        Visible = false;
    }

    private void resetItemSlots(Inventory inv)
    {
        foreach (var slot in slots)
        {
            slot.QueueFree();
        }
        slots.Clear();
        int slotsPerRow = Mathf.Max((int)Size.X/(SlotSizePx+Padding),1);
        int row = -1;
        for (int i = 0; i < inv.Size; i++)
        {
            int column = i % slotsPerRow;
            if (column == 0) row++;
            ItemSlotUI slot = ItemSlotUI.Instantiate<ItemSlotUI>();
            AddChild(slot);
            slot.Position = new Vector2(column*SlotSizePx+(column+1)*Padding, row*SlotSizePx+(row+1)*Padding);
            int idx = i;
            slot.OnClick += (b) => slotClicked(b, idx, inv);
            slots.Add(slot);
        }
        onInventoryUpdate(inv);
    }

    private void onInventoryUpdate(Inventory inv)
    {
        for(int i = 0; i < slots.Count; i++)
        {
            slots[i].DisplayItem(inv.GetItem(i));
        }
    }

    private void slotClicked(MouseButton button, int slot, Inventory inv)
    {
        if (inv != tracking) return; //not sure this is necessary but IDC
        if (button == MouseButton.Left) slotLeftClicked(slot, inv);
        else if (button == MouseButton.Right) slotRightClicked(slot, inv);
        else return; //no need to update
        player.MouseInventory.TriggerUpdate();
    }

    private void slotLeftClicked(int slot, Inventory inv)
    {
        //try to stack items, if not possible, swap them.
        if (!inv.PutItem(slot, ref player.MouseInventory.Items[0])) inv.SwapItems(slot, player.MouseInventory, 0);
    }

    private void slotRightClicked(int slot, Inventory inv)
    {
        //if there's a mouse item we want to try to put one of it into our inventory
        if (player.MouseInventory.Items[0].Item != null) {
            inv.PutItem(slot, ref player.MouseInventory.Items[0], 1);
        }
        else {
            //if not, split the stack and put the other half on the mouse
            int count = inv.GetItem(slot).Size/2;
            inv.TakeItems(slot, count, ref player.MouseInventory.Items[0]);
        }
    }
}