using Godot;
using System;

namespace Recursia;
public partial class BlockFactoryItem : Item
{
    [Export] public string BlockName;
    [Export] public float Reach = 10;
    public Action<Block>? InitPlaced;

    public BlockFactoryItem(string typeName, string blockName, float reach) : base(typeName, blockName)
    {
        BlockName = blockName;
        Reach = reach;
    }

    public override bool OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        BlockcastHit? hit = user.World!.Blockcast(position, dir*Reach);
        if (hit != null && hit.Normal != Vector3.Zero) { //zero normal means we are inside the block we are gonna place
            if(BlockTypes.TryGet(BlockName, out Block? placing))
            {
                InitPlaced?.Invoke(placing);
                user.World.SetBlock(hit.BlockPos+(BlockCoord)hit.Normal, placing);
            }
            else
            {
                GD.PushError($"Cannot get block {BlockName} for block factory item.");
            }
        }
        source.Decrement(1);
        base.OnUse(user, position, dir, ref source);
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is BlockFactoryItem other && other.BlockName == BlockName && other.Reach == Reach;
    }

    public override int GetHashCode()
    {
        unchecked {
            return BlockName.GetHashCode()^Reach.GetHashCode();
        }
    }
}