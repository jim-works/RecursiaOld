using System.IO;
namespace Recursia;

public class ChunkBuffer : ISerializable
{
    public ChunkCoord Position;
    private Block?[,,] Blocks;
    public ChunkMesh? Mesh;
    public bool SaveDirtyFlag;

    public ChunkBuffer(BinaryReader br)
    {
        Deserialize(br);
        if (Blocks == null) Blocks = new Block[Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE];
    }
    public ChunkBuffer(ChunkCoord chunkCoords)
    {
        Position = chunkCoords;
        Blocks = new Block[Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE];
    }

    public Block? this[BlockCoord index]
    {
        get { return this[index.X,index.Y,index.Z]; }
        set { this[index.X,index.Y,index.Z]=value; SaveDirtyFlag = true;}
    }
    public Block? this[int x, int y, int z]
    {
        get { return Blocks[x, y, z]; }
        set
        {
            Blocks[x, y, z] = value;
        }
    }

    //places the blocks in the buffer on the chunk
    public void AddTo(Chunk c)
    {
        for (int x=0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (Blocks[x,y,z] != null) c[x,y,z] = Blocks[x,y,z];
                }
            }
        }
    }
    public void AddTo(ChunkBuffer b)
    {
        for (int x=0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (Blocks[x,y,z] != null) b[x,y,z] = Blocks[x,y,z];
                }
            }
        }
    }

    public void Serialize(BinaryWriter bw)
    {
        Position.Serialize(bw);
        //serialize blocks
        SerializationExtensions.Serialize(Blocks, bw);
    }
    public void Deserialize(BinaryReader br)
    {
        Position.Deserialize(br);
        Blocks = SerializationExtensions.DeserializeBlockArray(br);
    }

    public override string ToString()
    {
        return $"ChunkBuffer at {Position}";
    }
}
