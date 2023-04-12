using Godot;
using System.IO;
using System;

#pragma warning disable CS0660, CS0661 //intentionally not overriding gethashcode or .equals heres
namespace Recursia;
public partial class Block : ISerializable
{
    public string Name;
    public bool Transparent;
    public bool Collidable = true;
    public bool Usable; //set to true if player should use block instead of using whatever item they have equipped 
    public AtlasTextureInfo TextureInfo;
    public float ExplosionResistance;
    public DropTable DropTable;
    public Texture2D ItemTexture;

    public virtual void OnUse(Combatant c, BlockCoord pos) {}
    public virtual void OnLoad(BlockCoord pos, Chunk c) {}
    public virtual void OnUnload(BlockCoord pos, Chunk c) {}

    //only need to override if block contains instance data
    public virtual void Serialize(BinaryWriter bw) {}
    public virtual void Deserialize(BinaryReader br) {}

    //allows subclasses to override .Equals, keeping == and != consistent with that
    public static bool operator ==(Block a, Block b)
    {
        return a is null ? b is null : a.Equals(b);
    }
    public static bool operator !=(Block a, Block b) => !(a==b);
}
