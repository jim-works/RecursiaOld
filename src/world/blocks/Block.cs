using Godot;
using System.IO;

#pragma warning disable CS0660, CS0661 //intentionally not overriding gethashcode or .equals heres
public class Block : ISerializable
{
    public string Name;
    public bool Transparent = false;
    public bool Collidable = true;
    public bool Usable = false; //set to true if player should use block instead of using whatever item they have equipped 
    public bool HasOnLoad = false;
    public AtlasTextureInfo TextureInfo;
    public float ExplosionResistance=0;
    public DropTable DropTable;
    public Texture ItemTexture;

    public virtual void OnUse(Combatant c, BlockCoord pos) {}
    public virtual void OnLoad(BlockCoord pos, Chunk c) {}
    public virtual void OnUnload(BlockCoord pos, Chunk c) {}

    //only need to override if block contains instance data
    public virtual void Serialize(BinaryWriter bw) {}

    //allows subclasses to override .Equals, keeping == and != consistent with that
    public static bool operator ==(Block a, Block b)
    {
        return (object)a == null ? (object)b == null : a.Equals(b);
    }
    public static bool operator !=(Block a, Block b) => !(a==b);
}
