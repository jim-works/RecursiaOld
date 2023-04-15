using System.IO;
using Godot;

namespace Recursia;
public class LootBlock : Block
{
    public ItemStack[] Drops;
    public LootBlock(string name, AtlasTextureInfo textureInfo, Texture2D itemTexture, ItemStack[] drops) : base(name,textureInfo,itemTexture)
    {
        Drops = drops;
    }
    public override void OnUse(Combatant c, BlockCoord pos)
    {
        if (c.Inventory == null) return;
        foreach (var item in Drops)
        {
            c.Inventory.CopyItem(item);
        }
        c.World!.SetBlock(pos, null);
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