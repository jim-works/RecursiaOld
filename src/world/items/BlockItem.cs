using Godot;

public partial class BlockItem : Item
{
    public Block Placing;
    public float Reach = 10;

    public override void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        BlockcastHit hit = World.Singleton.Blockcast(position, dir*Reach);
        if (hit != null && hit.Normal != Vector3.Zero) { //zero normal means we are inside the block we are gonna place
            World.Singleton.SetBlock(hit.BlockPos+(BlockCoord)hit.Normal, Placing);
        }
        source.Decrement(1);
        base.OnUse(user, position, dir, ref source);
    }

    public override bool Equals(object obj)
    {
        if (obj is BlockItem other && other.Placing == Placing && other.Reach == Reach) return true;
        return false;
    }

    public override int GetHashCode()
    {
        unchecked {
            return Placing.GetHashCode()^Reach.GetHashCode();
        }
    }
}