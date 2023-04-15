using System.Threading.Tasks;
using Godot;

namespace Recursia;
public abstract class WorldStructureProvider
{
    public WorldStructure Kind;
    public readonly ChunkCoord MaxArea;
    public int RollsPerChunk = 1;
    protected readonly BlockCoord maxSize;
    //keep a record of this structure in region tree
    public bool Record;

    protected WorldStructureProvider(BlockCoord maxSize, WorldStructure kind) {
        Kind = kind;
        this.maxSize = maxSize;
        //add 1 to account for spilling over to neighboring chunks
        MaxArea = (ChunkCoord)maxSize + new ChunkCoord(1,1,1);
    }

    //return true if spot is good for the structure
    public abstract bool SuitableLocation(World world, BlockCoord coord);
    //returns null if structure wasn't placed
    public abstract WorldStructure? PlaceStructure(ChunkCollection c, BlockCoord position);
}