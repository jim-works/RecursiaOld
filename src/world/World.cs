using Godot;
using System;
using System.Collections.Generic;


public class World : Node
{
    public Dictionary<Int3, Chunk> Chunks = new Dictionary<Int3, Chunk>();
    
    public Block GetBlock(Int3 coords)
    {
        Int3 chunkCoords = Chunk.WorldToChunkPos(coords);
        Int3 blockCoords = Chunk.WorldToLocal(coords);
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            return c[blockCoords];
        }
        return null; //chunk not found
    }
    public void SetBlock(Int3 coords, Block block) {
        Int3 chunkCoords = Chunk.WorldToChunkPos(coords);
        Int3 blockCoords = Chunk.WorldToLocal(coords);
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            //chunk already exists
            c[blockCoords] = block;
            return;
        }
        //create new chunk
        c = new Chunk();
        c[blockCoords] = block;
        Chunks[chunkCoords] = c;
    }
}
