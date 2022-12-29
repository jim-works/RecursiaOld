public struct ItemStack
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
}