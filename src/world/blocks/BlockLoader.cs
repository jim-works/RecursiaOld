using Godot;

namespace Recursia;
public static class BlockLoader
{
    private static Texture2D blockTextures;
    //sets BlockTextures to textures, loads all blocks
    public static void Load(Texture2D textures)
    {
        blockTextures = textures;
        TextureAtlas standard = new()
        {
            CellSize=8,
            TexWidth=256,
            TexHeight=256
        };

        createBasic("dirt", standard, 0, 0);
        createBasic("grass", standard, new int[] {1,1,1,1,0,1}, new int[] {2,0,2,2,0,2});
        createBasic("stone", standard, 0, 1,2);
        createBasic("sand", standard, 1, 1,100);
        createBasic("lava", standard, 2, 0,100);
        createBasic("copper_ore", standard, 3, 0,100);
        createBasic("silicon_ore", standard, 3, 1,100);
        createBasic("log", standard, 4, 1,100);
        createBasic("leaves", standard, 4, 2,100);
        createBasic("water", standard, 0,2,transparent:true);
        createFactory<LootBlock>("loot", standard, new int[]{2,2,2,2,2,2}, new int[]{1,2,1,1,2,1}, usable: true);
    }

    private static void createBasic(string name, TextureAtlas atlas, int x, int y, float explosionResistance=0, bool transparent=false)
    {
        Block b = new()
        {
            Name=name,
            TextureInfo=atlas.Sample(x,y),
            ExplosionResistance=explosionResistance,
            Transparent=transparent,
        };
        BlockTypes.CreateType(name, () => b);
        b.ItemTexture = getItemTexture(atlas.Sample(x,y));
        b.DropTable = new DropTable {
            drop = new ItemStack{Item=ItemTypes.GetBlockItem(name), Size=1}
        };
    }

    private static void createBasic(string name, TextureAtlas atlas, int[] x, int[] y, float explosionResistance=0, Direction itemTexDir=Direction.PosX)
    {
        Block b = new()
        {
            Name=name,
            TextureInfo=atlas.Sample(x,y),
            ExplosionResistance=explosionResistance,
        };
        BlockTypes.CreateType(name, () => b);
        b.ItemTexture = getItemTexture(atlas.Sample(x,y), itemTexDir);
        b.DropTable = new DropTable {
            drop = new ItemStack{Item=ItemTypes.GetBlockItem(name), Size=1}
        };
    }

    private static void createFactory<T>(string name, TextureAtlas atlas, int[] x, int[] y, bool usable=false, System.Action<T> init=null, Direction itemTexDir = Direction.PosX) where T : Block, new()
    {
        var texInfo = atlas.Sample(x,y);
        var itemTex = getItemTexture(texInfo, itemTexDir);
        BlockTypes.CreateType(name, () => {
            T b = new()
            {
                Name = name,
                TextureInfo = texInfo,
                ItemTexture = itemTex,
                Usable = usable,
            };
            b.DropTable = new DropTable {
                drop = new ItemStack{Item=ItemTypes.GetBlockItem(b), Size=1}
            };
            init?.Invoke(b);
            return b;
        });
    }

    private static AtlasTexture getItemTexture(AtlasTextureInfo t, Direction dir = Direction.PosX)
    {
        AtlasTexture itemTex = new()
        {
            Atlas = blockTextures
        };
        Vector2 min = new(t.UVMin[(int)dir].X*t.Atlas.TexWidth, t.UVMin[(int)dir].Y*t.Atlas.TexHeight);
        Vector2 max = new(t.UVMax[(int)dir].X*t.Atlas.TexWidth, t.UVMax[(int)dir].Y*t.Atlas.TexHeight);
        itemTex.Region = new Rect2(min,max-min);
        return itemTex;
    }
}