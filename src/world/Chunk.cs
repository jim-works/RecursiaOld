using System;
using Godot;


public class Chunk
{
    public const int CHUNK_SIZE = 16;
    public BlockCoord ChunkCoords;
    public Block[,,] Blocks;
    public Node Mesh;

    public Chunk(BlockCoord chunkCoords)
    {
        Blocks = new Block[CHUNK_SIZE,CHUNK_SIZE,CHUNK_SIZE];
        ChunkCoords = chunkCoords;
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
        BlockCoord coords = CHUNK_SIZE*ChunkCoords;
        return coords + local;
    }

    public static BlockCoord WorldToLocal(BlockCoord coord)
    {
        return coord % CHUNK_SIZE;
    }
    public static BlockCoord WorldToChunkPos(BlockCoord coord)
    {
        return coord/CHUNK_SIZE;
    }
}
