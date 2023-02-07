using System.Threading.Tasks;

public class TreeStructureProvider : StructureProvider
{
    private const int LEAF_SIZE = 2;
    private const int TRUNK_HEIGHT = 7;
    private Block log;
    private Block grass;
    private Block leaves;

    public TreeStructureProvider() : base(new BlockCoord(LEAF_SIZE*2+1,TRUNK_HEIGHT+LEAF_SIZE,LEAF_SIZE*2+1))
    {
        Kind = new Structure();
        Kind.Name = "Tree";
        Kind.Mutex = false;
        Kind.Priority = 0;

        grass = BlockTypes.Get("grass");
        log = BlockTypes.Get("log");
        leaves = BlockTypes.Get("leaves");
    }
    public override bool SuitableLocation(World world, BlockCoord coord)
    {
        return world.GetBlock(coord) == grass; //tree must be planted on grass
    }
    public override Task<Structure> PlaceStructure(ChunkCollection c, BlockCoord position)
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
                    c.SetIfNull(new BlockCoord(x,y+TRUNK_HEIGHT,z)+position, leaves);
                }
            }
        }
        return Task.FromResult<Structure>(null);
    }
}