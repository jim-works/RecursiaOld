using System.IO;
using System.Collections.Generic;
using System;

//leaf node of region octree structure
public partial class Chunk : Region
{
    public const int CHUNK_SIZE = 16;
    public ChunkCoord Position;
    public Block[,,] Blocks;
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
        set {Blocks[index.X,index.Y,index.Z] = value;}
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

    public override void Serialize(BinaryWriter bw)
    {
        Position.Serialize(bw);
        Block curr = null;
        int run = 0;
        for (int i = 0; i < Blocks.Length; i++)
        {
            Block b = (Block)Blocks.GetValue(i);
            if (curr == b)
            {
                run++;
                continue;
            }
            bw.Write(run);
            if (curr == null) bw.Write(0);
            else {bw.Write(1); curr.Serialize(bw);}
            run = 1;
            curr = b;
        }
        bw.Write(run);
        if (curr == null) bw.Write(0);
        else {bw.Write(1); curr.Serialize(bw);}
    }

    new public static Chunk Deserialize(BinaryReader br)
    {
        var pos = ChunkCoord.Deserialize(br);
        Chunk c = new Chunk(pos);

        for (int i = 0; i < Chunk.CHUNK_SIZE*Chunk.CHUNK_SIZE*Chunk.CHUNK_SIZE; i++)
        {
            int run = br.ReadInt32();
            bool nullBlock = br.ReadInt32() == 0;
            Block read;
            if (nullBlock) read = null;
            else {read = BlockTypes.Get(br.ReadString()); read.Deserialize(br);}
            for (int j = i; j < i+run;j++) {
                c.Blocks.SetValue(read, j);
            }
            i += run;
        }
        return c;
    }

    public override string ToString()
    {
        return $"Chunk at {Position} (origin {Origin})";
    }
}
