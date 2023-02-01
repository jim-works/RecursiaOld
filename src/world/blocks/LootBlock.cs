using Godot;

public class LootBlock : Block
{
    public ItemStack[] Drops;

    public override void OnUse(Combatant c, BlockCoord pos)
    {
        foreach (var item in Drops)
        {
            c.Inventory.CopyItem(item);
        }
        World.Singleton.SetBlock(pos, null);
    }
}