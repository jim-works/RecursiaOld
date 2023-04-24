using System.Threading.Tasks;
using Godot;

namespace Recursia;
public class BoxStructureProvider : WorldStructureProvider
{
    private const int SIZE = 20;
    private readonly Block? boxBlock;
    private readonly Block? grass;
    private readonly FastNoiseLite leafNoise = new();

    public BoxStructureProvider() : base(0, new BlockCoord(SIZE, SIZE, SIZE),
        new WorldStructure("Box")
        {
            Mutex = false,
            Priority = 0
        })
    {
        BlockTypes.TryGet("water", out boxBlock);
        BlockTypes.TryGet("grass", out grass);
        RollsPerChunk = 10;

        leafNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
    }
    public override bool SuitableLocation(Chunk c, BlockCoord coord)
    {
        return c[coord] == grass;
    }
    public override WorldStructure? PlaceStructure(ChunkCollection chunks, BlockCoord position)
    {
        chunks.BatchSetBlock(set =>
        {
            for (int x = -SIZE; x <= SIZE; x++)
            {
                for (int y = -SIZE; y <= SIZE; y++)
                {
                    for (int z = -SIZE; z <= SIZE; z++)
                    {
                        set(new BlockCoord(x, y, z) + position, boxBlock);
                    }
                }
            }
        });
        return null;
    }
}