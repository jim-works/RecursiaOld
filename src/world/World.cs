using Godot;
using System;
using System.Collections.Generic;


public class World : Node
{
    public Dictionary<Int3, Chunk> Chunks = new Dictionary<Int3, Chunk>();
    
    public Chunk GetOrCreateChunk(Int3 chunkCoords) {
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            //chunk already exists
            return c;
        }
        //create new chunk
        c = new Chunk(chunkCoords);
        Chunks[chunkCoords] = c;
        return c;
    }
    public Chunk GetChunk(Int3 chunkCoords) {
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            return c;
        }
        return null; //chunk not found
    }
    public Block GetBlock(Int3 coords)
    {
        Int3 chunkCoords = Chunk.WorldToChunkPos(coords);
        Int3 blockCoords = Chunk.WorldToLocal(coords);
        Godot.GD.Print($"{chunkCoords}, {blockCoords}");
        return GetChunk(coords)?[coords];
    }
    public void SetBlock(Int3 coords, Block block) {
        Int3 chunkCoords = Chunk.WorldToChunkPos(coords);
        Int3 blockCoords = Chunk.WorldToLocal(coords);
        GetOrCreateChunk(chunkCoords)[blockCoords] = block;
    }
}
