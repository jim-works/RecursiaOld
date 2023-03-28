using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
//TODO: fix unloading, I think there's a memory leak
public partial class World : Node
{
    public static World Singleton;
    [Export] public string WorldName = "World1"; 
    //loads a parent region of this level when we try to load a chunk (reduces # of times we read the file)
    [Export] public int LoadChunkRegionLevel = 1;
    public ChunkCollection Chunks = new ChunkCollection();
    //todo: optimize these
    public List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();
    public List<Combatant> Combatants = new List<Combatant>();
    public List<Player> Players = new List<Player>();
    public Player LocalPlayer;
    public HashSet<Node3D> ChunkLoaders = new HashSet<Node3D>();
    public WorldGenerator WorldGen;
    
    private Dictionary<ChunkGroupCoord, ChunkGroup> chunkGroups = new Dictionary<ChunkGroupCoord, ChunkGroup>();
    private List<Chunk> fromWorldGen = new List<Chunk>();
    private HashSet<ChunkCoord> loadedChunks = new HashSet<ChunkCoord>();
    private List<ChunkCoord> toUnload = new List<ChunkCoord>();

    private double chunkLoadingInterval = 0.5f; //seconds per chunk loading update
    private double _chunkLoadingTimer = 0;
    private WorldSaver saver;
    
    [Export] private int loadDistance = 10;

    public override void _EnterTree()
    {
        Singleton = this;
        base._EnterTree();
    }

    public override void _Ready()
    {
        WorldGen = new WorldGenerator();
        saver = GetNode<WorldSaver>("WorldSaver");
        _chunkLoadingTimer = 9999;
        doChunkLoading();
        base._Ready();
    }

    public override void _Process(double delta)
    {
        WorldGen.GetFinishedChunks(fromWorldGen);
        foreach (var item in fromWorldGen)
        {
            Mesher.Singleton.MeshDeferred(item, checkMeshed: true);
        }

        fromWorldGen.Clear();
        if (GlobalConfig.UseInfiniteWorlds)
        {
            _chunkLoadingTimer += delta;
            doChunkLoading();
        }
        base._Process(delta);
    }

    public ChunkGroup GetChunkGroup(ChunkCoord c)
    {
        return GetChunkGroup((ChunkGroupCoord)c);
    }
    public ChunkGroup GetChunkGroup(ChunkGroupCoord c)
    {
        if (chunkGroups.TryGetValue(c, out var g)) return g;
        ChunkGroup newGroup = new ChunkGroup(c);
        chunkGroups[c] = newGroup;
        return newGroup;
    }
    public Player ClosestPlayer(Vector3 pos)
    {
        float minSqrDist = float.PositiveInfinity;
        Player minPlayer = null;
        foreach(var Player in Players)
        {
            float sqrDist = (pos-Player.GlobalPosition).LengthSquared();
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
            float sqrDist = (pos-c.GlobalPosition).LengthSquared();
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
        if (_chunkLoadingTimer < chunkLoadingInterval) return;
        _chunkLoadingTimer = 0;
        loadedChunks.Clear();
        toUnload.Clear();
        foreach (Node3D loader in ChunkLoaders)
        {
            ChunkCoord center = (ChunkCoord)loader.GlobalPosition;
            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int y = -loadDistance; y <= loadDistance; y++)
                {
                    for (int z = -loadDistance; z <= loadDistance; z++)
                    {
                        if (x * x + y * y + z + z > loadDistance * loadDistance) continue; //load in a sphere instead of cube
                        loadedChunks.Add(center + new ChunkCoord(x,y,z));
                    }
                }
            }
        }
        foreach (var kvp in Chunks) {
            if (!loadedChunks.Contains(kvp.Key)) {
                toUnload.Add(kvp.Key);
            } else {
                //loaded, check if needs mesh
                Mesher.Singleton.MeshDeferred(kvp.Value, checkMeshed: true);
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
            chunk.Load();
            return; //already loaded
        }
        ChunkGroupCoord cgcoord = (ChunkGroupCoord)coord;
        //we only load/unload whole chunk groups at once
        if (!chunkGroups.TryGetValue(cgcoord, out ChunkGroup cg) && saver.PathToChunkGroupExists(cgcoord))
        {
            //TODO: multithread this part (Task.Run doesn't work immediately)
            loadChunkGroup(cgcoord);
            return;
        } 
        else if (cg != null)
        {
            //ChunkGroup is loaded, see if it contains the chunk we want
            Chunk inGroup = cg.GetChunk(coord);
            if (inGroup != null)
            {
                inGroup.Load();
                return;
            }
        }
        //need to generate the chunk
        Chunk c = CreateChunk(coord);
        c.Load();
        WorldGen.GenerateDeferred(c);
    }
    private void loadChunkGroup(ChunkGroupCoord coord)
    {
        lock (chunkGroups)
        {
            ChunkGroup cg = saver.Load(coord);
            if (cg != null)
            {
                chunkGroups[cg.Position] = cg;
                for (int x = 0; x < ChunkGroup.GROUP_SIZE; x++)
                {
                    for (int y = 0; y < ChunkGroup.GROUP_SIZE; y++)
                    {
                        for (int z = 0; z < ChunkGroup.GROUP_SIZE; z++)
                        {
                            Chunk cgc = cg.Chunks[x, y, z];
                            if (cgc != null) {
                                AddChunk(cgc);
                                cgc.Load();
                            }
                            
                        }
                    }
                }
            }
        }
    }
    private void unloadChunk(ChunkCoord coord) {
        if (!Chunks.Contains(coord)) return;//already unloaded
        Chunk c = Chunks[coord];
        c.Unload();
        //don't remove chunk from dict unless the group can be unloaded
        if (c.Group.ChunksLoaded == 0)
        {
            for (int x = 0; x < ChunkGroup.GROUP_SIZE; x++)
            {
                for (int y = 0; y < ChunkGroup.GROUP_SIZE; y++)
                {
                    for (int z = 0; z < ChunkGroup.GROUP_SIZE; z++)
                    {
                        Chunk cgc = c.Group.Chunks[x, y, z];
                        if (cgc != null) Chunks.Remove(cgc.Position);
                    }
                }
            }
            ChunkGroup cg = c.Group;
            Task.Run(() => saver.Save(cg));
            chunkGroups.Remove(cg.Position);
            GD.Print("Saved chunk group " + cg.Position.ToString() + " to disk");
        }

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
        Chunk c = new Chunk(chunkCoords, GetChunkGroup(chunkCoords));
        AddChunk(c);
        return c;
    }
    public void AddChunk(Chunk c) {
        if (!chunkGroups.TryGetValue((ChunkGroupCoord)c.Position, out ChunkGroup cg)) {
            cg = new ChunkGroup((ChunkGroupCoord)c.Position);
            chunkGroups.Add(cg.Position, cg);
        }
        cg.AddChunk(c);
        Chunks[c.Position] = c;
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
        if (meshChunk && c.State == ChunkState.Loaded) {
            Mesher.Singleton.MeshDeferred(c);
            //mesh neighbors if needed
            if (blockCoords.X == 0 && GetChunk(chunkCoords+new ChunkCoord(-1,0,0)) is Chunk nx)
            {
                Mesher.Singleton.MeshDeferred(nx);
            }
            if (blockCoords.Y == 0 && GetChunk(chunkCoords+new ChunkCoord(0,-1,0)) is Chunk ny)
            {
                Mesher.Singleton.MeshDeferred(ny);
            }
            if (blockCoords.Z == 0 && GetChunk(chunkCoords+new ChunkCoord(0,0,-1)) is Chunk nz)
            {
                Mesher.Singleton.MeshDeferred(nz);
            }
            if (blockCoords.X == Chunk.CHUNK_SIZE-1 && GetChunk(chunkCoords+new ChunkCoord(1,0,0)) is Chunk px)
            {
                Mesher.Singleton.MeshDeferred(px);
            }
            if (blockCoords.Y == Chunk.CHUNK_SIZE-1 && GetChunk(chunkCoords+new ChunkCoord(0,1,0)) is Chunk py)
            {
                Mesher.Singleton.MeshDeferred(py);
            }
            if (blockCoords.Z == Chunk.CHUNK_SIZE-1 && GetChunk(chunkCoords+new ChunkCoord(0,0,1)) is Chunk pz)
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
