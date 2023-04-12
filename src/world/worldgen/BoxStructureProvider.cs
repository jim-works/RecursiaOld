using System.Threading.Tasks;
using Godot;

namespace Recursia;
public partial class BoxStructureProvider : WorldStructureProvider
{
    private const int SIZE = 20;
    private readonly Block boxBlock;
    private readonly Block grass;
    private readonly FastNoiseLite leafNoise = new();

    public BoxStructureProvider() : base(new BlockCoord(SIZE,SIZE,SIZE))
    {
        Kind = new WorldStructure
        {
            Name = "Box",
            Mutex = false,
            Priority = 0
        };

        boxBlock = BlockTypes.Get("water");
        grass = BlockTypes.Get("grass");

        leafNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
    }
    public override bool SuitableLocation(World world, BlockCoord coord)
    {
        return world.GetBlock(coord) == grass; //tree must be planted on grass
    }
    public override WorldStructure PlaceStructure(AtomicChunkCollection c, BlockCoord position)
    {
        for (int x = -SIZE; x <= SIZE; x++)
        {
            for (int y = -SIZE; y <= SIZE; y++)
            {
                for (int z = -SIZE; z <= SIZE; z++)
                {
                    c.SetIfNull(new BlockCoord(x,y,z)+position, boxBlock);
                }
            }
        }
        return null;
    }
}