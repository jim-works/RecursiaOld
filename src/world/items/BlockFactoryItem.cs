using Godot;
using System.IO;

namespace Recursia;
public partial class BlockFactoryItem : Item
{
    [Export] public string? FactoryName {get => _factoryName; set {
            _factoryName = value;
            if (value == null) return;
            if (BlockTypes.TryGet(value, out Block? b))
            {
                Texture2D = b.ItemTexture;
            }
            else
            {
                GD.PushWarning($"coulnd't get itemtexture of block factory item {value}");
            }
    }}
    private string? _factoryName;
    [Export] public float Reach = 10;

    public BlockFactoryItem(string typeName) : base(typeName) {}

    public override bool OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        if (FactoryName == null)
        {
            GD.PushError("null BlockFactoryItem");
            return false;
        }
        BlockcastHit? hit = user.World!.Blockcast(position, dir*Reach);
        if (hit != null && hit.Normal != Vector3.Zero) { //zero normal means we are inside the block we are gonna place
            if(BlockTypes.TryGet(FactoryName, out Block? placing))
            {
                user.World.Chunks.SetBlock(hit.BlockPos+(BlockCoord)hit.Normal, placing);
                source.Decrement(1);
            }
            else
            {
                GD.PushError($"Cannot get block {FactoryName} for block factory item.");
            }
        }
        base.OnUse(user, position, dir, ref source);
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is BlockFactoryItem other && other.FactoryName == FactoryName && other.Reach == Reach;
    }

    public override int GetHashCode()
    {
        unchecked {
            return (FactoryName?.GetHashCode() ?? 0)^Reach.GetHashCode();
        }
    }
    public override void Serialize(BinaryWriter bw)
    {
        base.Serialize(bw);
        bw.Write(FactoryName ?? "");
    }
    public override void Deserialize(BinaryReader br)
    {
        base.Deserialize(br);
        FactoryName = br.ReadString();
        if (FactoryName.Length == 0) FactoryName = null;
    }
}