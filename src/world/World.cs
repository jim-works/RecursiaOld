using Godot;
using System;
using System.Collections.Generic;

//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
public class World : Node
{
    public static World Singleton;
    public ChunkCollection Chunks = new ChunkCollection();
    public RegionOctree Octree = new RegionOctree(1,new BlockCoord(0,0,0));
    //todo: optimize these
    public List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();
    public List<Combatant> Combatants = new List<Combatant>();
    public List<Player> Players = new List<Player>();
    public HashSet<Spatial> ChunkLoaders = new HashSet<Spatial>();
    public WorldGenerator WorldGen;
    private List<Chunk> fromWorldGen = new List<Chunk>();
    private HashSet<ChunkCoord> loadedChunks = new HashSet<ChunkCoord>();
    private List<ChunkCoord> toUnload = new List<ChunkCoord>();
    
    [Export] private int loadDistance = 10;

    public override void _EnterTree()
    {
        Singleton = this;
        base._EnterTree();
    }

    public override void _Ready()
    {
        WorldGen = new WorldGenerator();
        base._Ready();
        doChunkLoading();
    }

    public override void _Process(float delta)
    {
        WorldGen.GetFinishedChunks(fromWorldGen);
        foreach (var item in fromWorldGen)
        {
            Mesher.Singleton.MeshDeferred(item);
        }
        fromWorldGen.Clear();
        if (GlobalConfig.UseInfiniteWorlds) doChunkLoading();
        base._Process(delta);
    }
    public Player ClosestPlayer(Vector3 pos)
    {
        float minSqrDist = float.PositiveInfinity;
        Player minPlayer = null;
        foreach(var Player in Players)
        {
            float sqrDist = (pos-Player.Position).LengthSquared();
            if (sqrDist < minSqrDist) {
                minSqrDist = sqrDist;
                minPlayer = Player;
            }
        }
        return minPlayer;
    }
    public bool ClosestEnemy(Vector3 pos, Team team, out Combatant enemy)
    {
        float minSqrDist = float.PositiveInfinity;
        enemy = null;
        foreach(var c in Combatants)
        {
            float sqrDist = (pos-c.Position).LengthSquared();
            if (sqrDist < minSqrDist && c.Team != team) {
                minSqrDist = sqrDist;
                enemy = c;
            }
        }
        return enemy != null;
    }
    public Combatant CollidesWithEnemy(Box box, Team team)
    {
        foreach (var c in Combatants)
        {
            if (c.Team == team) continue;
            if (c.GetBox().IntersectsBox(box)) return c;
        }
        return null;
    }
    private void doChunkLoading()
    {
        loadedChunks.Clear();
        toUnload.Clear();
        foreach (Spatial loader in ChunkLoaders)
        {
            ChunkCoord center = (ChunkCoord)loader.GlobalTransform.origin;
            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int y = -loadDistance; y <= loadDistance; y++)
                {
                    for (int z = -loadDistance; z <= loadDistance; z++)
                    {
                        if (x*x+y*y+z+z > loadDistance*loadDistance) continue; //load in a sphere instead of cube
                        loadedChunks.Add(center + new ChunkCoord(x,y,z));
                    }
                }
            }
        }
        foreach (var kvp in Chunks) {
            if (!loadedChunks.Contains(kvp.Key)) {
                toUnload.Add(kvp.Key);
            }
        }
        foreach (var c in toUnload) {
            unloadChunk(c);
        }
        foreach (var c in loadedChunks) {
            loadChunk(c);
        }

    }
    private void loadChunk(ChunkCoord coord) {
        if (Chunks.TryGetValue(coord, out Chunk chunk)){
            chunk.Loaded = true;
            return; //already loaded
        } 
        Chunk c = CreateChunk(coord);
        c.Loaded = true;
        WorldGen.GenerateDeferred(c);
    }
    private void unloadChunk(ChunkCoord coord) {
        if (!Chunks.Contains(coord)) return;//already unloaded
        Chunk c = Chunks[coord];
        //allow us to keep some unloaded chunks in memory
        if (c.Loaded) {
            Chunks.Remove(coord);
        } 
        c.Loaded = false;
        Mesher.Singleton.Unload(c);
    }
    public Chunk GetOrCreateChunk(ChunkCoord chunkCoords) {
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            //chunk already exists
            return c;
        }
        return CreateChunk(chunkCoords);
    }
    public Chunk CreateChunk(ChunkCoord chunkCoords) {
        Chunk c = new Chunk(chunkCoords);
        Chunks[chunkCoords] = c;
        Octree.AddRegion(c);
        c.Loaded = false;
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
    public ItemStack BreakBlock(BlockCoord coords) {
        DropTable dt = GetBlock(coords).DropTable;
        if (dt == null) {
            GD.Print("Null droptable!");
        }
        SetBlock(coords, null);
        return dt?.GetDrop() ?? new ItemStack();
    }
    public void SetBlock(BlockCoord coords, Block block, bool meshChunk=true) {
        ChunkCoord chunkCoords = (ChunkCoord)coords;
        BlockCoord blockCoords = Chunk.WorldToLocal(coords);
        Chunk c = GetOrCreateChunk(chunkCoords);
        c[blockCoords] = block;
        if (meshChunk && c.Loaded) {
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
            Normal = Vector3.Zero,
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
                //normal direction will be the greatest difference from the center
                Normal = Math.MaxComponent(testPoint-((Vector3)oldCoords+new Vector3(0.5f,0.5f,0.5f))).Normalized()
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
            Normal = Vector3.Zero
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
                //normal direction will be the greatest difference from the center
                Normal = Math.MaxComponent((Vector3)oldCoords+new Vector3(0.5f,0.5f,0.5f)-testPoint).Normalized()
            });
        }
    }
}
