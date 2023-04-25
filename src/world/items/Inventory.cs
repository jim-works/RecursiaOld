using System.IO;
using Godot;

namespace Recursia;
public class Inventory : ISerializable
{
    public ItemStack[] Items;
    public int Size {get => Items.Length;}
    public int SelectedSlot {get => _selectedSlot; set {_selectedSlot = value; TriggerUpdate();}}
    private int _selectedSlot;
    public event System.Action<Inventory>? OnUpdate;

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
        if (item.IsEmpty) return; //invalid item
        int firstEmptySlot = -1;
        for(int i = 0; i < Items.Length; i++)
        {
            if (Items[i].IsEmpty)
            {
                //keep empty slot for later, try to stack with existing items first.
                if (firstEmptySlot == -1) firstEmptySlot = i;
            }
            //Items[i].Item is not null because of first if statement, not sure why I need !.
            else if (Items[i].Item == item.Item && Items[i].Item.MaxStack > Items[i].Size)
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
    public void CopyItem(ItemStack item)
    {
        AddItem(ref item);
    }

    //selects the first slot containig an itemstack matching the query
    //returns -1 if no match
    public int Select(System.Func<ItemStack, bool> query)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (query(Items[i])) return i;
        }
        //not found
        return -1;
    }

    //selects the first slot containig an itemstack matching the query
    //returns (item, slot) if found
    //returns (Item.Empty, -1) if no match
    public (Item, int) SelectItem(System.Func<ItemStack, bool> query)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (query(Items[i])) return (Items[i].Item, i);
        }
        //not found
        return (Item.Empty, -1);
    }

    public void SwapItems(int a, int b)
    {
        (Items[b], Items[a]) = (Items[a], Items[b]);
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

    //deletes count (or as many as possible) items of type <item> from this inventory.
    //returns the numger of items deleted
    public int DeleteItems(Item item, int count)
    {
        int remaining = count;
        for (int i = 0; i < Items.Length; i++)
        {
            ref ItemStack stack = ref Items[i];
            if (stack.Item != item) continue;
            int removing = Mathf.Min(remaining, stack.Size);
            stack.Decrement(removing);
            if (remaining == 0) break;
        }
        OnUpdate?.Invoke(this);
        return count-remaining;
    }

    //deletes count (or as many as possible) item from slot.
    //returns the numger of items deleted
    public int DeleteFromSlot(int slot, int count)
    {
        ref ItemStack stack = ref Items[slot];
        int removing = Mathf.Min(count, stack.Size);
        stack.Decrement(removing);
        OnUpdate?.Invoke(this);
        return removing;
    }

    //returns true if successful, false if invalid operation (cannot take one item type into another)
    //if count is larger then the number of items in the slot, it will take all the items in the slot and return true.
    public bool TakeItems(int slot, int count, ref ItemStack into)
    {
        //cannot take from empty slot or into mismatched stack
        if (Items[slot].IsEmpty || !into.IsEmpty && Items[slot].Item != into.Item) return false;
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
        return PutItem(slot, ref from, from.Item.MaxStack);
    }

    //returns true if successful, false if invalid operation (cannot take one item type into another)
    //puts min(from.Item.MaxStack-SlotItem.Size,from.Size,count) items into the slot
    //caller may have to clean up from's item if we take all the items in the stack
    public bool PutItem(int slot, ref ItemStack from, int count)
    {
        //cannot take from empty slot or into mismatched stack
        if (from.IsEmpty || !Items[slot].IsEmpty && Items[slot].Item != from.Item) return false;
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

    public int Count(Item item)
    {
        int count = 0;
        foreach (var stack in Items)
        {
            if (stack.Item == item) count += stack.Size;
        }
        return count;
    }

    public void Serialize(BinaryWriter bw)
    {
        Items.Serialize(bw);
    }
    public void Deserialize(BinaryReader br)
    {
        Items = SerializationExtensions.DeserializeArray<ItemStack>(br);
    }
}