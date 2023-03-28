using System.Threading.Tasks;
using Godot;

public partial class BoxStructureProvider : StructureProvider
{
    private const int SIZE = 20;
    private Block boxBlock;
    private Block grass;
    private FastNoiseLite leafNoise = new FastNoiseLite();

    public BoxStructureProvider() : base(new BlockCoord(SIZE,SIZE,SIZE))
    {
        Kind = new Structure();
        Kind.Name = "Box";
        Kind.Mutex = false;
        Kind.Priority = 0;

        boxBlock = BlockTypes.Get("water");
        grass = BlockTypes.Get("grass");

        leafNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
    }
    public override bool SuitableLocation(World world, BlockCoord coord)
    {
        return world.GetBlock(coord) == grass; //tree must be planted on grass
    }
    public override Task<Structure> PlaceStructure(ChunkCollection c, BlockCoord position)
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
        return Task.FromResult<Structure>(null);
    }
}