using Godot;

public partial class BlockFactoryItem : Item
{
    [Export] public string BlockName;
    [Export] public float Reach = 10;

    public override void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        BlockcastHit hit = World.Singleton.Blockcast(position, dir*Reach);
        if (hit != null && hit.Normal != Vector3.Zero) { //zero normal means we are inside the block we are gonna place
            World.Singleton.SetBlock(hit.BlockPos+(BlockCoord)hit.Normal, BlockTypes.Get(BlockName));
        }
        source.Decrement(1);
        base.OnUse(user, position, dir, ref source);
    }

    public override bool Equals(object obj)
    {
        if (obj is BlockFactoryItem other && other.BlockName == BlockName && other.Reach == Reach) return true;
        return false;
    }

    public override int GetHashCode()
    {
        unchecked {
            return BlockName.GetHashCode()^Reach.GetHashCode();
        }
    }
}