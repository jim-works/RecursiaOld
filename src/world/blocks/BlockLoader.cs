using Godot;

public static class BlockLoader
{
    public static void Load()
    {
        TextureAtlas standard = new TextureAtlas {
            CellSize=8,
            TexWidth=256,
            TexHeight=256
        };

        createBasic("dirt", standard, 0, 0);
        createBasic("grass", standard, 1, 0);
        createBasic("stone", standard, 0, 1,2);
        createBasic("sand", standard, 1, 1,100);
    }

    private static void createBasic(string name, TextureAtlas atlas, int x, int y, float explosionResistance=0)
    {
        Block b = new Block {
            Name=name,
            TextureInfo=atlas.Sample(x,y),
            ExplosionResistance=explosionResistance,
        };
        BlockTypes.CreateType(name, () => b);
        b.ItemTexture = getBlockTexture(b);
        b.DropTable = new DropTable {
            drop = new ItemStack{Item=ItemTypes.GetBlockItem(name), Size=1}
        };
    }

    private static AtlasTexture getBlockTexture(Block b)
    {
        if (Mesher.Singleton == null) {
            GD.PrintErr("Cannot get block texture yet, mesher is not initialized");
            return null;
        }
        Texture chunkTex = ((SpatialMaterial)Mesher.Singleton.ChunkMaterial).AlbedoTexture;
        AtlasTexture itemTex = new AtlasTexture();
        itemTex.Atlas = chunkTex;
        Vector2 min = new Vector2(b.TextureInfo.UVMin.x*b.TextureInfo.Atlas.TexWidth, b.TextureInfo.UVMin.y*b.TextureInfo.Atlas.TexHeight);
        Vector2 max = new Vector2(b.TextureInfo.UVMax.x*b.TextureInfo.Atlas.TexWidth, b.TextureInfo.UVMax.y*b.TextureInfo.Atlas.TexHeight);
        
        itemTex.Region = new Rect2(min,max-min);
        return itemTex;
    }
}