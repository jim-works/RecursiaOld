using Godot;

namespace Recursia;
public partial class BlockItem : Item
{
    public Block Placing;
    [Export] public float Reach = 10;

    public BlockItem(string typeName, string displayname, Block placing) : base(typeName, displayname)
    {
        Placing = placing;
    }

    public override void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        BlockcastHit? hit = user.World!.Blockcast(position, dir*Reach);
        if (hit != null && hit.Normal != Vector3.Zero) { //zero normal means we are inside the block we are gonna place
            user.World.SetBlock(hit.BlockPos+(BlockCoord)hit.Normal, Placing);
        }
        source.Decrement(1);
        base.OnUse(user, position, dir, ref source);
    }

    public override bool Equals(object? obj)
    {
        return obj is BlockItem other && other.Placing == Placing && other.Reach == Reach;
    }

    public override int GetHashCode()
    {
        unchecked {
            return Placing.GetHashCode()^Reach.GetHashCode();
        }
    }
}