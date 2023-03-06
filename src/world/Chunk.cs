using System.IO;
using System.Collections.Generic;
using System;

//leaf node of region octree structure
public partial class Chunk : Region
{
    public const int CHUNK_SIZE = 16;
    public ChunkCoord Position;
    private Block[,,] Blocks;
    public ChunkMesh Mesh;
    public ChunkGenerationState GenerationState;

    public Chunk(ChunkCoord chunkCoords) : base(0,(BlockCoord)chunkCoords)
    {
        Blocks = new Block[CHUNK_SIZE,CHUNK_SIZE,CHUNK_SIZE];
        //Structures.Add(new Structure());
        Position = chunkCoords;
    }
    
    public void ChunkTick(float dt)
    {

    }

    public Block this[BlockCoord index] {
        get {return Blocks[index.X,index.Y,index.Z];}
        set {Blocks[index.X,index.Y,index.Z] = value; SetBlockDirty(); }
    }
    public Block this[int x, int y, int z] {
        get {return Blocks[x,y,z];}
        set {Blocks[x,y,z] = value; SetBlockDirty(); }
    }

    public BlockCoord LocalToWorld(BlockCoord local) {
        return (BlockCoord)Position + local;
    }

    public static BlockCoord WorldToLocal(BlockCoord coord)
    {
        return coord % (int)CHUNK_SIZE;
    }

    public override void Serialize(BinaryWriter bw)
    {
        long startPos = bw.BaseStream.Position;
        bw.Write(0); //placeholder for length of region in bytes
        bw.Write(Level);
        Position.Serialize(bw);
        Block curr = Blocks[0,0,0];
        int run = 0;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    Block b = Blocks[x,y,z];
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
        else {bw.Write(1); curr.Serialize(bw);}
        long endPos = bw.BaseStream.Position;
        int size = (int)(endPos-startPos);
        //seek back to start to write the size of the region
        bw.Seek((int)startPos, SeekOrigin.Begin);
        bw.Write(size);
        //go back to end so region isn't overwritten
        bw.Seek((int)endPos, SeekOrigin.Begin);
    }

    public static Chunk Deserialize(BinaryReader br)
    {
        var pos = ChunkCoord.Deserialize(br);
        Chunk c = new Chunk(pos);
        int run = 0;
        Block read = null;
        c.BlockDirtyFlag = false;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++) {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++) {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++) {
                    if (run == 0) {
                        run = br.ReadInt32();
                        bool nullBlock = br.ReadInt32() == 0;
                        if (nullBlock) read = null;
                        else { read = BlockTypes.Get(br.ReadString()); read.Deserialize(br); }
                    }
                    c.Blocks[x,y,z] = read;
                    run --;
                }
            }
        }
        return c;
    }

    public override string ToString()
    {
        return $"Chunk at {Position} (origin {Origin}) {BlockDirtyFlag}";
    }
}
