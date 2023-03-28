using System.Threading.Tasks;
using Godot;

public abstract class StructureProvider
{
    public Structure Kind;
    public readonly ChunkCoord MaxArea;
    public int RollsPerChunk = 1;
    protected readonly BlockCoord maxSize;
    //keep a record of this structure in region tree
    public bool Record = false;

    public StructureProvider(BlockCoord maxSize) {
        this.maxSize = maxSize;
        //add 1 to account for spilling over to neighboring chunks
        MaxArea = (ChunkCoord)maxSize + new ChunkCoord(1,1,1);
    }

    //return true if spot is good for the structure
    public abstract bool SuitableLocation(World world, BlockCoord coord);
    public abstract Task<Structure> PlaceStructure(ChunkCollection c, BlockCoord position);
}