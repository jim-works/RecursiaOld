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
        createBasic("lava", standard, 2, 0,100);
        createFactory<LootBlock>("loot", standard, 2, 1, usable: true);
    }

    private static void createBasic(string name, TextureAtlas atlas, int x, int y, float explosionResistance=0)
    {
        Block b = new Block {
            Name=name,
            TextureInfo=atlas.Sample(x,y),
            ExplosionResistance=explosionResistance,
        };
        BlockTypes.CreateType(name, () => b);
        b.ItemTexture = getBlockTexture(atlas.Sample(x,y));
        b.DropTable = new DropTable {
            drop = new ItemStack{Item=ItemTypes.GetBlockItem(name), Size=1}
        };
    }

    private static void createFactory<T>(string name, TextureAtlas atlas, int x, int y, bool usable=false, System.Action<T> init=null) where T : Block, new()
    {
        var texInfo = atlas.Sample(x,y);
        var itemTex = getBlockTexture(texInfo);
        BlockTypes.CreateType(name, () => {
            T b = new T();
            b.Name = name;
            b.TextureInfo = texInfo;
            b.ItemTexture = itemTex;
            b.Usable = usable;
            b.DropTable = new DropTable {
                drop = new ItemStack{Item=ItemTypes.GetBlockItem(b), Size=1}
            };
            if (init != null) init(b);
            return b;
        });
    }

    private static AtlasTexture getBlockTexture(AtlasTextureInfo t)
    {
        if (Mesher.Singleton == null) {
            GD.PrintErr("Cannot get block texture yet, mesher is not initialized");
            return null;
        }
        Texture chunkTex = ((SpatialMaterial)Mesher.Singleton.ChunkMaterial).AlbedoTexture;
        AtlasTexture itemTex = new AtlasTexture();
        itemTex.Atlas = chunkTex;
        Vector2 min = new Vector2(t.UVMin.x*t.Atlas.TexWidth, t.UVMin.y*t.Atlas.TexHeight);
        Vector2 max = new Vector2(t.UVMax.x*t.Atlas.TexWidth, t.UVMax.y*t.Atlas.TexHeight);
        
        itemTex.Region = new Rect2(min,max-min);
        return itemTex;
    }
}