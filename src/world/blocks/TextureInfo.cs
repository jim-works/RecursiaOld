using Godot;

namespace Recursia;
public class AtlasTextureInfo
{
    public Vector2[] UVMin;
    public Vector2[] UVMax;
    public TextureAtlas Atlas;

    public AtlasTextureInfo(Vector2[] uvMin, Vector2[] uvMax, TextureAtlas atlas)
    {
        UVMin = uvMin;
        UVMax = uvMax;
        Atlas = atlas;
    }
}