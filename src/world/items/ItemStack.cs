public struct ItemStack : ISerializable
{
    public int Size;
    public Item Item;

    //returns number of items decremented
    //sets Item to null if stack is depleted
    public int Decrement(int amount)
    {
        if (amount >= Size)
        {
            int old = Size;
            Size = 0;
            Item = null;
            return old;
        }
        Size -= amount;
        return amount;
    }
    public void Clear()
    {
        Size = 0;
        Item = null;
    }

    public void Serialize(System.IO.BinaryWriter bw) {
        if (Item == null) {
            bw.Write("");
            return;
        }
        bw.Write(Item.TypeName);
        Item.Serialize(bw);
        bw.Write(Size);
    }
    public static ItemStack Deserialize(System.IO.BinaryReader br) {
        string name = br.ReadString();
        if (name == "") {
            return new ItemStack();
        }
        int size = br.ReadInt32();
        Item item = ItemTypes.Get(name);
        item.Deserialize(br);
        return new ItemStack {
            Item= item,
            Size = size
        };
    }
}