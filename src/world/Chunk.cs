using System.IO;
using System.Collections.Generic;
using System;

public enum ChunkState
{
    Unloaded,
    Loaded
}
public partial class Chunk : ISerializable
{
    public const int CHUNK_SIZE = 16;
    public ChunkGroup Group;
    public ChunkCoord Position;
    private Block[,,] Blocks;
    public ChunkMesh Mesh;
    public ChunkGenerationState GenerationState;
    public ChunkState State { get; private set; }
    public List<Structure> Structures = new List<Structure>();
    public bool SaveDirtyFlag = true;
    //if the chunk is made of only one type of block, we don't allocate the block array
    private Block uniformBlock = null;

    public Chunk(ChunkCoord chunkCoords, ChunkGroup group)
    {
        Position = chunkCoords;
        group.AddChunk(this);
    }

    public void ChunkTick(float dt)
    {

    }

    public Block this[BlockCoord index]
    {
        get { return this[index.X,index.Y,index.Z]; }
        set { this[index.X,index.Y,index.Z]=value; }
    }
    public Block this[int x, int y, int z]
    {
        get { return Blocks != null ? Blocks[x, y, z] : uniformBlock; }
        set
        {
            if (value != uniformBlock)
            {
                if (Blocks == null) Blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
                Blocks[x, y, z] = value;
                SaveDirtyFlag = true;
            }
        }
    }

    public void Load()
    {
        if (State != ChunkState.Loaded) Group.ChunksLoaded++;
        State = ChunkState.Loaded;
    }

    public void Unload()
    {
        if (State != ChunkState.Unloaded) Group.ChunksLoaded--;
        State = ChunkState.Unloaded;
    }

    public BlockCoord LocalToWorld(BlockCoord local)
    {
        return (BlockCoord)Position + local;
    }

    public static BlockCoord WorldToLocal(BlockCoord coord)
    {
        return coord % (int)CHUNK_SIZE;
    }

    public void Serialize(BinaryWriter bw)
    {
        Position.Serialize(bw);
        if (Blocks == null)
        {
            //this case doesn't need to exist, but should be faster than the other
            bw.Write(Chunk.CHUNK_SIZE*Chunk.CHUNK_SIZE*Chunk.CHUNK_SIZE);
            if (uniformBlock == null) bw.Write(0);
            else { bw.Write(1); uniformBlock.Serialize(bw); }
        }
        else
        {
            Block curr = Blocks[0, 0, 0];
            int run = 0;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        Block b = Blocks[x, y, z];
                        if (curr == b)
                        {
                            run++;
                            continue;
                        }
                        bw.Write(run);
                        if (curr == null) bw.Write(0);
                        else { bw.Write(1); curr.Serialize(bw); }
                        run = 1;
                        curr = b;
                    }
                }
            }

            bw.Write(run);
            if (curr == null) bw.Write(0);
            else { bw.Write(1); curr.Serialize(bw); }
        }
    }

    public static void Deserialize(BinaryReader br, ChunkGroup into)
    {
        var pos = ChunkCoord.Deserialize(br);
        Chunk c = new Chunk(pos, into);
        int run = 0;
        Block read = null;
        c.SaveDirtyFlag = false;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (run == 0)
                    {
                        run = br.ReadInt32();
                        bool nullBlock = br.ReadInt32() == 0;
                        if (nullBlock) read = null;
                        else { read = BlockTypes.Get(br.ReadString()); read.Deserialize(br); }
                    }
                    c[x, y, z] = read;
                    run--;
                }
            }
        }
    }

    public override string ToString()
    {
        return $"Chunk at {Position}";
    }
}
