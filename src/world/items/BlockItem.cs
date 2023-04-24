using System.IO;
using Godot;

namespace Recursia;
public partial class BlockItem : Item
{
    public Block? Placing {
        get => _placing;
        set {
            _placing = value;
            DisplayName = value == null ? "Empty Block" : $"{value.Name} Block";
            if (value != null) Texture2D = value.ItemTexture;
        }
    }
    private Block? _placing;
    [Export] public float Reach = 10;

    public BlockItem(string typeName) : base(typeName) {}

    public override bool OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        BlockcastHit? hit = user.World!.Blockcast(position, dir*Reach);
        if (hit != null && hit.Normal != Vector3.Zero) { //zero normal means we are inside the block we are gonna place
            user.World.Chunks.SetBlock(hit.BlockPos+(BlockCoord)hit.Normal, Placing);
            source.Decrement(1);
            return true;
        }
        base.OnUse(user, position, dir, ref source);
        return false;
    }

    public override void Serialize(BinaryWriter bw)
    {
        base.Serialize(bw);
        SerializationExtensions.SerializeBlock(bw, Placing);
    }
    public override void Deserialize(BinaryReader br)
    {
        base.Deserialize(br);
        Placing = SerializationExtensions.DeserializeBlock(br);
    }
    public override bool Equals(object? obj)
    {
        return obj is BlockItem other && other.Placing == Placing && other.Reach == Reach;
    }

    public override int GetHashCode()
    {
        unchecked {
            return (Placing?.GetHashCode() ?? 0)^Reach.GetHashCode();
        }
    }
}