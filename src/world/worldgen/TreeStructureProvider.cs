using System.Threading.Tasks;
using Godot;

namespace Recursia;
public partial class TreeStructureProvider : WorldStructureProvider
{
    private const int LEAF_SIZE = 4;
    private const int TRUNK_HEIGHT = 7;
    private const float CUTOFF=20;
    private const float FREQ=12.384f;
    private const float BASE_DIST=15;
    private readonly Block log;
    private readonly Block grass;
    private readonly Block leaves;
    private readonly FastNoiseLite leafNoise = new();

    public TreeStructureProvider() : base(new BlockCoord(LEAF_SIZE*2+1,TRUNK_HEIGHT+LEAF_SIZE,LEAF_SIZE*2+1))
    {
        RollsPerChunk = 1;
        Kind = new WorldStructure
        {
            Name = "Tree",
            Mutex = false,
            Priority = 0
        };

        grass = BlockTypes.Get("grass");
        log = BlockTypes.Get("log");
        leaves = BlockTypes.Get("leaves");

        leafNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
    }
    public override bool SuitableLocation(World world, BlockCoord coord)
    {
        return world.GetBlock(coord) == grass; //tree must be planted on grass
    }
    public override WorldStructure PlaceStructure(AtomicChunkCollection c, BlockCoord position)
    {
        for (int dy = 1; dy < TRUNK_HEIGHT; dy++)
        {
            c.SetBlock(new BlockCoord(0,dy,0)+position, log);
        }
        for (int x = -LEAF_SIZE; x <= LEAF_SIZE; x++)
        {
            for (int y = -LEAF_SIZE; y <= LEAF_SIZE; y++)
            {
                for (int z = -LEAF_SIZE; z <= LEAF_SIZE; z++)
                {
                    float sample = BASE_DIST+(Mathf.Abs(x)+Mathf.Abs(y)+Mathf.Abs(z))*(1+0.5f*leafNoise.GetNoise(FREQ*(position.X+x),FREQ*(position.Y+y),FREQ*(position.Z+z))); //0..3*LEAF_SIZE
                    if (sample < CUTOFF) c.SetIfNull(new BlockCoord(x,y+TRUNK_HEIGHT,z)+position, leaves);
                }
            }
        }
        return null;
    }
}