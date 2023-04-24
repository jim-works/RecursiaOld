using Godot;

namespace Recursia;
public static class BlockLoader
{
    private static Texture2D? blockTextures;
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
        createBasic("glass", standard, 3,2,transparent:true);
        createBasic("cherry_blossom_leaves", standard, 5,2);
        createBasic("cherry_blossom_leaves2", standard, 5, 1);
    }
    public static void LoadAfterItems()
    {
        TextureAtlas standard = new()
        {
            CellSize=8,
            TexWidth=256,
            TexHeight=256
        };
        if (ItemTypes.TryGet("marp_rod", out Item? lootItem))
        {
            createFactory("loot.patrick", standard, new int[] { 2, 2, 2, 2, 2, 2 }, new int[] { 1, 2, 1, 1, 2, 1 }, (n, i, t) =>
            {
                return new LootBlock(n, i, t, new ItemStack[] { new ItemStack() { Item = lootItem, Size = 7 } })
                {
                    Usable = true
                };
            });
        }
        else
        {
            GD.PushError("couldn't create loot block");
        }
    }

    private static void createBasic(string name, TextureAtlas atlas, int x, int y, float explosionResistance=0, bool transparent=false)
    {
        Block b = new(name,atlas.Sample(x,y),getItemTexture(atlas.Sample(x,y)))
        {
            ExplosionResistance=explosionResistance,
            Transparent=transparent,
        };
        BlockTypes.CreateType(name, () => b);
        if (ItemTypes.GetBlockFactoryItem(name) is BlockFactoryItem blockFactoryItem)
        {
            b.DropTable = new DropTable
            {
                drop = new ItemStack { Item = blockFactoryItem, Size = 1 }
            };
            return;
        }
        GD.PushError($"Couldn't find block item for {name} to add to its drop table.");
    }

    private static void createBasic(string name, TextureAtlas atlas, int[] x, int[] y, float explosionResistance=0, Direction itemTexDir=Direction.PosX)
    {
        Block b = new(name,atlas.Sample(x,y),getItemTexture(atlas.Sample(x,y),itemTexDir))
        {
            ExplosionResistance=explosionResistance,
        };
        BlockTypes.CreateType(name, () => b);
        if (ItemTypes.GetBlockFactoryItem(name) is BlockFactoryItem blockFactoryItem)
        {
            b.DropTable = new DropTable
            {
                drop = new ItemStack { Item = blockFactoryItem, Size = 1 }
            };
            return;
        }
        GD.PushError($"Couldn't find block item for {name} to add to its drop table.");
    }

    private static void createFactory<T>(string name, TextureAtlas atlas, int[] x, int[] y, System.Func<string,AtlasTextureInfo,AtlasTexture,T> ctor, Direction itemTexDir = Direction.PosX)
        where T : Block
    {
        var texInfo = atlas.Sample(x,y);
        var itemTex = getItemTexture(texInfo, itemTexDir);
        BlockTypes.CreateType(name, () => {
            T b = ctor(name,texInfo,itemTex);
            b.DropTable = new DropTable {
                drop = new ItemStack{Item=ItemTypes.GetBlockItem(b)!, Size=1}
            };
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