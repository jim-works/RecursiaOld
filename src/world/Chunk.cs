using System;

public class Chunk
{
    public const int CHUNK_SIZE = 16;
    public Block[,,] Blocks;

    public Chunk()
    {
        Blocks = new Block[CHUNK_SIZE,CHUNK_SIZE,CHUNK_SIZE];
    }

    public Block this[Int3 index] {
        get {return Blocks[index.x,index.y,index.z];}
        set {Blocks[index.x,index.y,index.z] = value;}
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
