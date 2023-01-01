using Godot;
using System;

#pragma warning disable CS0660, CS0661 //intentionally not overriding gethashcode or .equals heres
public class Block
{
    public string Name;
    public bool Transparent = false;
    public bool Collidable = true;
    public AtlasTextureInfo TextureInfo;
    public float ExplosionResistance=0;
    public DropTable DropTable;
    public Texture ItemTexture;

    //allows subclasses to override .Equals, keeping == and != consistent with that
    public static bool operator ==(Block a, Block b)
    {
        return (object)a == null ? (object)b == null : a.Equals(b);
    }
    public static bool operator !=(Block a, Block b) => !(a==b);
}
