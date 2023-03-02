using Godot;

public partial class TextureAtlas
{
    public int TexWidth, TexHeight, CellSize;

    //gets the texture in the yth row and xth column in this atlas
    public AtlasTextureInfo Sample(int x, int y)
    {
        float cellWidthRatio = (float)CellSize/TexWidth;
        float cellHeightRatio = (float)CellSize/TexHeight;
        return new AtlasTextureInfo {
            UVMin = new Vector2((float)x*cellWidthRatio, (float)y*cellHeightRatio),
            UVMax = new Vector2((float)(x+1)*cellWidthRatio, (float)(y+1)*cellHeightRatio),
            Atlas = this
        };
    }
}