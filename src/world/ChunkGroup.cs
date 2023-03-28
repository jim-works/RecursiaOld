public class ChunkGroup
{
    //each group is a cube of side length GROUP_SIZE
    public const byte GROUP_SIZE = 16;
    public Chunk[,,] Chunks = new Chunk[GROUP_SIZE,GROUP_SIZE,GROUP_SIZE];

    public int ChunksLoaded = 0;
    public ChunkGroupCoord Position;
    public ChunkGroup(ChunkGroupCoord pos)
    {
        Position = pos;
    }
    public void AddChunk(Chunk c)
    {
        ChunkCoord coords = c.Position%GROUP_SIZE;
        Chunks[coords.X,coords.Y,coords.Z] = c;
        c.Group = this;
    }
    //assumes p is in the chunk group
    public Chunk GetChunk(ChunkCoord p)
    {
        ChunkCoord index = p%GROUP_SIZE;
        return Chunks[index.X,index.Y,index.Z];
    }
}