using System.IO;
using Godot;

public partial class LootBlock : Block
{
    public ItemStack[] Drops;

    public override void OnUse(Combatant c, BlockCoord pos)
    {
        foreach (var item in Drops)
        {
            c.Inventory.CopyItem(item);
        }
        c.World.SetBlock(pos, null);
    }

    public override void Serialize(BinaryWriter bw)
    {
        Drops.Serialize(bw);
    }
    public override void Deserialize(BinaryReader br)
    {
        Drops = SerializationExtensions.DeserializeArray<ItemStack>(br,br => ItemStack.Deserialize(br));
    }
}