using System.IO;
namespace Recursia;
public struct ItemStack : ISerializable
{
    public int Size;
    public Item Item {
        get { return _item ?? Item.Empty;}
        set { _item = value; }
    }
    private Item? _item;
    public bool IsEmpty => Size == 0 || ReferenceEquals(Item, Item.Empty);
    public ItemStack(BinaryReader br)
    {
        Size = 0;
        _item = null;
        Deserialize(br);
    }

    //returns number of items decremented
    //sets Item to null if stack is depleted
    public int Decrement(int amount)
    {
        if (amount >= Size)
        {
            int old = Size;
            Size = 0;
            Item = Item.Empty;
            return old;
        }
        Size -= amount;
        return amount;
    }
    public void Clear()
    {
        Size = 0;
        Item = Item.Empty;
    }

    public void Serialize(BinaryWriter bw) {
        if (IsEmpty)
        {
            bw.Write("");
        }
        else
        {
            bw.Write(Item.TypeName);
            bw.Write(Size);
            Item.Serialize(bw);
        }
    }
    public void Deserialize(BinaryReader br)
    {
        string name = br.ReadString();
        if (string.IsNullOrEmpty(name)) {
            Item = Item.Empty;
            Size = 0;
            return;
        }
        int size = br.ReadInt32();
        if (size <= 0)
        {
            Clear();
            return;
        }
        if (ItemTypes.TryGet(name, out Item? item))
        {
            item.Deserialize(br);
            Item = item;
            Size = size;
        }
        else
        {
            Godot.GD.PushWarning($"Unknown item: {name}");
        }
    }
}