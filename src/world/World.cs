using Godot;
using System;
using System.Collections.Generic;


public class World : Node
{
    public static World Singleton;
    public Dictionary<Int3, Chunk> Chunks = new Dictionary<Int3, Chunk>();

    public override void _EnterTree()
    {
        Singleton = this;
        base._EnterTree();
    }

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
    public Block GetBlock(Vector3 worldCoords) {
        return GetBlock((Int3)worldCoords);
    }
    public void SetBlock(Int3 coords, Block block) {
        Int3 chunkCoords = Chunk.WorldToChunkPos(coords);
        Int3 blockCoords = Chunk.WorldToLocal(coords);
        GetOrCreateChunk(chunkCoords)[blockCoords] = block;
    }
    //returns the block closest to origin that intersects the line segment from origin to (origin + line)
    public RaycastHit Raycast(Vector3 origin, Vector3 line) {
        //TODO: improve this
        float stepSize = 0.5f;
        float lineLength = line.Length();
        Vector3 lineNorm = line/lineLength;
        for (float t = 0; t < lineLength; t += stepSize) {
            Vector3 testPoint = origin + t*lineNorm;
            Block b = GetBlock(testPoint);
            SetBlock((Int3)testPoint, BlockTypes.Get("sand"));
            if (b != null) {
                return new RaycastHit {
                    Block = b,
                    BlockPos = (Int3)testPoint,
                    HitPos = testPoint
                };
            }
        }
        Mesher.Singleton.MeshAll();
        return null;
    }
}
