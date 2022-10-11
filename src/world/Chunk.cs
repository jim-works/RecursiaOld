using System;

public class Chunk
{
    public const int CHUNK_SIZE = 16;
    public Int3 ChunkCoords;
    public Block[,,] Blocks;

    public Chunk(Int3 chunkCoords)
    {
        Blocks = new Block[CHUNK_SIZE,CHUNK_SIZE,CHUNK_SIZE];
        ChunkCoords = chunkCoords;
    }

    public Block this[Int3 index] {
        get {return Blocks[index.x,index.y,index.z];}
        set {Blocks[index.x,index.y,index.z] = value;}
    }
    public Block this[int x, int y, int z] {
        get {return Blocks[x,y,z];}
        set {Blocks[x,y,z] = value;}
    }

    public Int3 LocalToWorld(Int3 local) {
        Int3 coords = CHUNK_SIZE*ChunkCoords;
        return coords + local;
    }

    public static Int3 WorldToLocal(Int3 coord)
    {
        Int3 coords = coord % CHUNK_SIZE;
        coords.x = coords.x < 0 ? coords.x + CHUNK_SIZE : coords.x;
        coords.y = coords.y < 0 ? coords.y + CHUNK_SIZE : coords.y;
        coords.z = coords.z < 0 ? coords.z + CHUNK_SIZE : coords.z;
        return coords;
    }
    public static Int3 WorldToChunkPos(Int3 coord)
    {
        return new Int3(Divide.ToNegative(coord.x,CHUNK_SIZE),Divide.ToNegative(coord.y,CHUNK_SIZE),Divide.ToNegative(coord.z,CHUNK_SIZE));
    }
}
