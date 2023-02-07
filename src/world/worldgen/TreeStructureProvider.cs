using System.Threading.Tasks;

public class TreeStructureProvider : StructureProvider
{
    private Block dirt;
    private Block grass;

    public TreeStructureProvider() : base(new BlockCoord(5,100,5))
    {
        Kind = new Structure();
        Kind.Name = "Tree";
        Kind.Mutex = false;
        Kind.Priority = 0;

        grass = BlockTypes.Get("grass");
        dirt = BlockTypes.Get("dirt");
    }
    public override bool SuitableLocation(World world, BlockCoord coord)
    {
        return world.GetBlock(coord) == grass; //tree must be planted on grass
    }
    public override Task<Structure> PlaceStructure(ChunkCollection c, BlockCoord position)
    {
        for (int dy = 1; dy < 100; dy++)
        {
            c.SetBlock(new BlockCoord(0,dy,0)+position, dirt);
        }
        return Task.FromResult<Structure>(null);
    }
}