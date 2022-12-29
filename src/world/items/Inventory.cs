using Godot;

public class Inventory
{
    private ItemStack[] items;
    public int Size {get => items.Length;}
    public event System.Action<Inventory> OnUpdate;

    public Inventory(int slots)
    {
        items = new ItemStack[slots];
    }

    //auto stacks into existing items in inventory, then first free slot
    public void AddItem(ref ItemStack item)
    {
        if (item.Item == null || item.Size < 1) return; //invalid item
        int firstEmptySlot = -1;
        for(int i = 0; i < items.Length; i++)
        {
            if (items[i].Item == null && firstEmptySlot == -1)
            {
                //keep empty slot for later, try to stack with existing items first.
                firstEmptySlot = i;
            }
            if (items[i].Item == item.Item && items[i].Item.MaxStack > items[i].Size)
            {
                //add to existing stack
                int toAdd = Mathf.Min(item.Size, items[i].Item.MaxStack-items[i].Size);
                items[i].Size += toAdd;
                item.Size -= toAdd;
            }
        }
        if (firstEmptySlot != -1 && item.Size > 0)
        {
            items[firstEmptySlot] = item;
            item.Size = 0;
        }
        OnUpdate?.Invoke(this);
    }

    public void SwapItems(int a, int b)
    {
        var tmp = items[a];
        items[a] = items[b];
        items[b] = tmp;
        OnUpdate?.Invoke(this);
    }

    public ItemStack GetItem(int i) => items[i];

    //returns true if successful, false if invalid operation (cannot take one item type into another)
    //if count is larger then the number of items in the slot, it will take all the items in the slot and return true.
    public bool TakeItems(int slot, int count, ref ItemStack into)
    {
        //cannot take from empty slot or into mismatched stack
        if (items[slot].Item == null || (into.Item != null && items[slot].Item != into.Item)) return false;
        into.Item = items[slot].Item;
        int toTake = Mathf.Min(count, items[slot].Size);
        into.Size += toTake;
        items[slot].Size -= toTake;
        if (items[slot].Size <= 0) ClearSlot(slot);
        OnUpdate?.Invoke(this);
        return true;
    }
    
    //returns true if successful, false if invalid operation (cannot take one item type into another)
    //puts min(from.Item.MaxStack-SlotItem.Size,from.Size,count) items into the slot
    //caller may have to clean up from's item if we take all the items in the stack
    public bool PutItem(int slot, int count, ref ItemStack from)
    {
        //cannot take from empty slot or into mismatched stack
        if (from.Item == null || (items[slot].Item != null && items[slot].Item != from.Item)) return false;
        items[slot].Item = from.Item;
        int toPut = Mathf.Min(Mathf.Min(from.Item.MaxStack-items[slot].Size,count),from.Size);
        from.Size -= toPut;
        items[slot].Size += toPut;
        OnUpdate?.Invoke(this);
        return true;
    }

    public void ClearSlot(int slot)
    {
        items[slot] = new ItemStack();
        OnUpdate?.Invoke(this);
    }

    public bool HasItem(Item item)
    {
        foreach (var stack in items)
        {
            if (stack.Item == item) return true;
        }
        return false;
    }
}