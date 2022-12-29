using Godot;
using System;

public class Block
{
    public string Name;
    public bool Transparent = false;
    public bool Collidable = true;
    public AtlasTextureInfo TextureInfo;
    public float ExplosionResistance=0;
}
