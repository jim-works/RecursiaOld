using Godot;

public class Inventory
{
    public ItemStack[] Items;
    public int Size {get => Items.Length;}
    public event System.Action<Inventory> OnUpdate;

    public Inventory(int slots)
    {
        Items = new ItemStack[slots];
    }
    public void TriggerUpdate()
    {
        OnUpdate?.Invoke(this);
    }
    //auto stacks into existing items in inventory, then first free slot
    public void AddItem(ref ItemStack item)
    {
        if (item.Item == null || item.Size < 1) return; //invalid item
        int firstEmptySlot = -1;
        for(int i = 0; i < Items.Length; i++)
        {
            if (Items[i].Item == null && firstEmptySlot == -1)
            {
                //keep empty slot for later, try to stack with existing items first.
                firstEmptySlot = i;
            }
            if (Items[i].Item == item.Item && Items[i].Item.MaxStack > Items[i].Size)
            {
                //add to existing stack
                int toAdd = Mathf.Min(item.Size, Items[i].Item.MaxStack-Items[i].Size);
                Items[i].Size += toAdd;
                item.Decrement(toAdd);
                if (item.Size == 0) break; //stack empty, break out now
            }
        }
        if (firstEmptySlot != -1 && item.Size > 0)
        {
            Items[firstEmptySlot] = item;
            item.Clear();
        }
        OnUpdate?.Invoke(this);
    }

    public void SwapItems(int a, int b)
    {
        var tmp = Items[a];
        Items[a] = Items[b];
        Items[b] = tmp;
        OnUpdate?.Invoke(this);
    }
    public void SwapItems(int slot, Inventory other, int otherSlot)
    {
        var tmp = Items[slot];
        Items[slot] = other.GetItem(otherSlot);
        other.Items[otherSlot] = tmp;
        OnUpdate?.Invoke(this);
        other.OnUpdate?.Invoke(other);
    }

    public ItemStack GetItem(int i) => Items[i];

    //returns true if successful, false if invalid operation (cannot take one item type into another)
    //if count is larger then the number of items in the slot, it will take all the items in the slot and return true.
    public bool TakeItems(int slot, int count, ref ItemStack into)
    {
        //cannot take from empty slot or into mismatched stack
        if (Items[slot].Item == null || (into.Item != null && Items[slot].Item != into.Item)) return false;
        into.Item = Items[slot].Item;
        int toTake = Mathf.Min(count, Items[slot].Size);
        into.Size += toTake;
        Items[slot].Size -= toTake;
        if (Items[slot].Size <= 0) ClearSlot(slot);
        OnUpdate?.Invoke(this);
        return true;
    }
    
    //returns true if successful, false if invalid operation (cannot take one item type into another)
    //puts as many items as possible into the slot
    //caller may have to clean up from's item if we take all the items in the stack
    public bool PutItem(int slot, ref ItemStack from)
    {
        return PutItem(slot, ref from, from.Item?.MaxStack ?? 0);
    }

    //returns true if successful, false if invalid operation (cannot take one item type into another)
    //puts min(from.Item.MaxStack-SlotItem.Size,from.Size,count) items into the slot
    //caller may have to clean up from's item if we take all the items in the stack
    public bool PutItem(int slot, ref ItemStack from, int count)
    {
        //cannot take from empty slot or into mismatched stack
        if (from.Item == null || (Items[slot].Item != null && Items[slot].Item != from.Item)) return false;
        Items[slot].Item = from.Item;
        int toPut = Mathf.Min(Mathf.Min(from.Item.MaxStack-Items[slot].Size,count),from.Size);
        from.Decrement(toPut);
        Items[slot].Size += toPut;
        OnUpdate?.Invoke(this);
        return true;
    }

    public void ClearSlot(int slot)
    {
        Items[slot] = new ItemStack();
        OnUpdate?.Invoke(this);
    }

    public bool HasItem(Item item)
    {
        foreach (var stack in Items)
        {
            if (stack.Item == item) return true;
        }
        return false;
    }
}