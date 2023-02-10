using System.Collections.Generic;

//leaf node of region octree structure
public class Chunk : Region
{
    public const int CHUNK_SIZE = 16;
    public ChunkCoord Position;
    public Block[,,] Blocks;
    public ChunkMesh Mesh;
    public bool Loaded = false;
    public ChunkGenerationState GenerationState;

    public Chunk(ChunkCoord chunkCoords) : base(0,(BlockCoord)chunkCoords)
    {
        Blocks = new Block[CHUNK_SIZE,CHUNK_SIZE,CHUNK_SIZE];
        Structures.Add(new Structure());
        Position = chunkCoords;
    }
    
    public void ChunkTick(float dt)
    {

    }

    public Block this[BlockCoord index] {
        get {return Blocks[index.x,index.y,index.z];}
        set {Blocks[index.x,index.y,index.z] = value;}
    }
    public Block this[int x, int y, int z] {
        get {return Blocks[x,y,z];}
        set {Blocks[x,y,z] = value;}
    }

    public BlockCoord LocalToWorld(BlockCoord local) {
        return (BlockCoord)Position + local;
    }

    public static BlockCoord WorldToLocal(BlockCoord coord)
    {
        return coord % (int)CHUNK_SIZE;
    }

    public override string ToString()
    {
        return $"Chunk at {Position} (origin {Origin})";
    }
}
