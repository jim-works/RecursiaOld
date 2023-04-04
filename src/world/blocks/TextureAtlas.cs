using Godot;

public partial class TextureAtlas
{
    public int TexWidth, TexHeight, CellSize;

    //gets the texture in the yth row and xth column in this atlas
    //sets all sides to be the same
    public AtlasTextureInfo Sample(int x, int y)
    {
        float cellWidthRatio = (float)CellSize/TexWidth;
        float cellHeightRatio = (float)CellSize/TexHeight;
        //all sides of the block are the same
        Vector2[] mins = new Vector2[6];
        Vector2[] maxs = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            mins[i] = new Vector2((float)x*cellWidthRatio, (float)y*cellHeightRatio);
            maxs[i] = new Vector2((float)(x+1)*cellWidthRatio, (float)(y+1)*cellHeightRatio);
        }
        return new AtlasTextureInfo {
            UVMin = mins,
            UVMax = maxs,
            Atlas = this
        };
    }
    //length xs and ys should equal 6
    //face in direction d is assigned the texture at xs[d],ys[d] in the atlas
    public AtlasTextureInfo Sample(int[] xs, int[] ys)
    {
        float cellWidthRatio = (float)CellSize/TexWidth;
        float cellHeightRatio = (float)CellSize/TexHeight;
        //all sides of the block are the same
        Vector2[] mins = new Vector2[6];
        Vector2[] maxs = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            mins[i] = new Vector2((float)xs[i]*cellWidthRatio, (float)ys[i]*cellHeightRatio);
            maxs[i] = new Vector2((float)(xs[i]+1)*cellWidthRatio, (float)(ys[i]+1)*cellHeightRatio);
        }
        return new AtlasTextureInfo {
            UVMin = mins,
            UVMax = maxs,
            Atlas = this
        };
    }
}