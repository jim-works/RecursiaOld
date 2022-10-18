using Godot;
using System;
using System.Collections.Generic;


public class World : Node
{
    public static World Singleton;
    public Dictionary<ChunkCoord, Chunk> Chunks = new Dictionary<ChunkCoord, Chunk>();

    public override void _EnterTree()
    {
        Singleton = this;
        base._EnterTree();
    }

    public Chunk GetOrCreateChunk(ChunkCoord chunkCoords) {
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            //chunk already exists
            return c;
        }
        //create new chunk
        c = new Chunk(chunkCoords);
        Chunks[chunkCoords] = c;
        return c;
    }
    public Chunk GetChunk(ChunkCoord chunkCoords) {
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            return c;
        }
        return null; //chunk not found
    }
    public Block GetBlock(BlockCoord coords)
    {
        ChunkCoord chunkCoords = (ChunkCoord)coords;
        BlockCoord blockCoords = Chunk.WorldToLocal(coords);
        Chunk c = GetChunk(chunkCoords);
        if (c == null) return null;
        return c[blockCoords];
    }
    public Block GetBlock(Vector3 worldCoords) {
        return GetBlock((BlockCoord)worldCoords);
    }
    public void SetBlock(BlockCoord coords, Block block, bool meshChunk=true) {
        ChunkCoord chunkCoords = (ChunkCoord)coords;
        BlockCoord blockCoords = Chunk.WorldToLocal(coords);
        Chunk c = GetOrCreateChunk(chunkCoords);
        c[blockCoords] = block;
        if (meshChunk) {
            Mesher.Singleton.MeshDeferred(c);
            //mesh neighbors if needed
            if (blockCoords.x == 0 && GetChunk(chunkCoords+new ChunkCoord(-1,0,0)) is Chunk nx)
            {
                Mesher.Singleton.MeshDeferred(nx);
            }
            if (blockCoords.y == 0 && GetChunk(chunkCoords+new ChunkCoord(0,-1,0)) is Chunk ny)
            {
                Mesher.Singleton.MeshDeferred(ny);
            }
            if (blockCoords.z == 0 && GetChunk(chunkCoords+new ChunkCoord(0,0,-1)) is Chunk nz)
            {
                Mesher.Singleton.MeshDeferred(nz);
            }
            if (blockCoords.x == Chunk.CHUNK_SIZE-1 && GetChunk(chunkCoords+new ChunkCoord(1,0,0)) is Chunk px)
            {
                Mesher.Singleton.MeshDeferred(px);
            }
            if (blockCoords.y == Chunk.CHUNK_SIZE-1 && GetChunk(chunkCoords+new ChunkCoord(0,1,0)) is Chunk py)
            {
                Mesher.Singleton.MeshDeferred(py);
            }
            if (blockCoords.z == Chunk.CHUNK_SIZE-1 && GetChunk(chunkCoords+new ChunkCoord(0,0,1)) is Chunk pz)
            {
                Mesher.Singleton.MeshDeferred(pz);
            }
        }
    }
    //returns the block closest to origin that intersects the line segment from origin to (origin + line)
    public BlockcastHit Blockcast(Vector3 origin, Vector3 line) {
        //TODO: improve this
        float stepSize = 0.05f;
        float lineLength = line.Length();
        Vector3 lineNorm = line/lineLength;
        BlockCoord oldCoords = (BlockCoord)origin;
        Block b = GetBlock(oldCoords);
        if (b != null) return new BlockcastHit{
            HitPos=origin,
            BlockPos=oldCoords,
            Block = b,
        };
        for (float t = 0; t < lineLength; t += stepSize) {
            //only query world when we are in a new block
            Vector3 testPoint = (origin + t*lineNorm);
            BlockCoord coords = (BlockCoord)testPoint;
            if (coords == oldCoords) continue;
            oldCoords=coords;
            b = GetBlock(testPoint);
            if (b != null) return new BlockcastHit{
                HitPos=testPoint,
                BlockPos=oldCoords,
                Block =b,
            };
        }
        return null;
    }
    //adds all non-null blocks intersecting the line between origin and dest to buffer
    public void BlockcastAll(Vector3 origin, Vector3 dest, List<BlockcastHit> buffer) {
        //TODO: improve this
        float stepSize = 0.05f;
        Vector3 d = dest-origin;
        float lineLength = d.Length();
        Vector3 lineNorm = d/lineLength;
        BlockCoord oldCoords = (BlockCoord)origin;
        Block b = GetBlock(oldCoords);
        if (b != null) buffer.Add(new BlockcastHit{
            HitPos=origin,
            BlockPos=oldCoords,
            Block = b,
        });
        for (float t = 0; t < lineLength; t += stepSize) {
            //only query world when we are in a new block
            Vector3 testPoint = (origin + t*lineNorm);
            BlockCoord coords = (BlockCoord)testPoint;
            if (coords == oldCoords) continue;
            oldCoords=coords;
            b = GetBlock(testPoint);
            if (b != null) buffer.Add(new BlockcastHit{
                HitPos=testPoint,
                BlockPos=oldCoords,
                Block =b,
            });
        }
    }
    public void CreateExplosion(Vector3 origin, float strength) {
        //TODO: make this better
        //expanding cube at the center of the explosion. keep track of cumulative power using a 3d array
        //r^4 algorithm -> r^3
        BlockCoord minBounds = new BlockCoord((int)(origin.x-strength),(int)(origin.y-strength),(int)(origin.z-strength));
        BlockCoord maxBounds = new BlockCoord((int)(origin.x + strength), (int)(origin.y + strength), (int)(origin.z + strength));
        BlockCoord originInt = (BlockCoord)origin;
        List<BlockcastHit> buffer = new List<BlockcastHit>();
        for (int x = minBounds.x; x < maxBounds.x; x++)
        {
            for (int y = minBounds.y; y < maxBounds.y; y++)
            {
                for (int z = minBounds.z; z < maxBounds.z; z++)
                {
                    BlockCoord p = new BlockCoord(x,y,z);
                    float sqrDist = (p-originInt).sqrMag();
                    if (sqrDist > strength*strength) continue; //outside of blast radius 
                    float power = strength-Mathf.Sqrt(sqrDist);
                    BlockcastAll((Vector3)originInt, (Vector3)p, buffer);
                    foreach (var item in buffer)
                    {
                        if (item.Block != null) power -= item.Block.ExplosionResistance;
                    }
                    buffer.Clear();
                    if (power > 0) SetBlock(p, null);
                }
            }
        }
    }
}
