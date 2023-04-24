using System.Threading.Tasks;
using Godot;

namespace Recursia;
public class CherryBlossomStructureProvider : WorldStructureProvider
{
    private const int LEAF_SIZE = 4;
    private const int TRUNK_HEIGHT = 7;
    private const float CUTOFF = 20;
    private const float FREQ = 12.384f;
    private const float BASE_DIST = 15;
    private readonly Block? log;
    private readonly Block? grass;
    private readonly Block? leaves;
    private readonly Block? leaves2;
    private readonly FastNoiseLite leafNoise = new();

    public CherryBlossomStructureProvider(uint seed) : base(seed, new BlockCoord(LEAF_SIZE * 2 + 1, TRUNK_HEIGHT + LEAF_SIZE, LEAF_SIZE * 2 + 1),
        new WorldStructure("CherryBlossomTree")
        {
            Mutex = false,
            Priority = 0
        })
    {
        RollsPerChunk = 1;
        BlockTypes.TryGet("grass", out grass);
        BlockTypes.TryGet("log", out log);
        BlockTypes.TryGet("cherry_blossom_leaves", out leaves);
        BlockTypes.TryGet("cherry_blossom_leaves2", out leaves2);

        leafNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
    }
    public override bool SuitableLocation(Chunk c, BlockCoord coord)
    {
        return c[coord] == grass; //tree must be planted on grass
    }
    public override WorldStructure? PlaceStructure(ChunkCollection chunks, BlockCoord position)
    {
        chunks.BatchSetBlock(set =>
        {
            for (int x = -LEAF_SIZE; x <= LEAF_SIZE; x++)
            {
                for (int y = -LEAF_SIZE; y <= LEAF_SIZE; y++)
                {
                    for (int z = -LEAF_SIZE; z <= LEAF_SIZE; z++)
                    {
                        float sample = BASE_DIST + (Mathf.Abs(x) + Mathf.Abs(y) + Mathf.Abs(z)) * (1 + 0.5f * leafNoise.GetNoise(FREQ * (position.X + x), FREQ * (position.Y + y), FREQ * (position.Z + z))); //0..3*LEAF_SIZE
                        BlockCoord leafPos = new BlockCoord(x, y + TRUNK_HEIGHT, z) + position;
                        if (sample < CUTOFF) set(leafPos, Rng.CoinFlip(1,2,Seed,leafPos) ? leaves : leaves2);
                    }
                }
            }
            for (int dy = 1; dy < TRUNK_HEIGHT; dy++)
            {
                set(new BlockCoord(0, dy, 0) + position, log);
            }
        });
        return null;
    }
}