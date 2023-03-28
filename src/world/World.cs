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
    public Dictionary<ChunkCoord, List<PhysicsObject>> PhysicsObjects = new();
    public Dictionary<ChunkCoord, List<Combatant>> Combatants = new ();
    public List<Player> Players = new List<Player>();
    public Player LocalPlayer;
    public HashSet<Node3D> ChunkLoaders = new HashSet<Node3D>();
    public WorldGenerator WorldGen;
    public event System.Action<Chunk> OnChunkUpdate;
    public event System.Action<Chunk> OnChunkReady;
    public event System.Action<Chunk> OnChunkUnload;
    
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
            OnChunkReady?.Invoke(item);
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
    public bool ClosestEnemy(Vector3 pos, Team team, float maxDist, out Combatant enemy)
    {
        float minSqrDist = float.PositiveInfinity;
        enemy = null;
        //TODO: only check chunks in range
        foreach (var l in Combatants.Values)
        foreach(var c in l)
        {
            float sqrDist = (pos-c.GlobalPosition).LengthSquared();
            if (sqrDist < minSqrDist && sqrDist < maxDist*maxDist && c.Team != team) {
                minSqrDist = sqrDist;
                enemy = c;
            }
        }
        return enemy != null;
    }
    public IEnumerable<Combatant> GetEnemiesInRange(Vector3 pos, float range, Team team)
    {
        //TODO: only check chunks in range
        foreach (var l in Combatants.Values)
        {
            foreach (var c in l)
            {
                if (c.Team != team && (c.GlobalPosition - pos).LengthSquared() < range * range) yield return c;
            }
        }
    }
    public IEnumerable<PhysicsObject> GetPhysicsObjectsInRange(Vector3 pos, float range)
    {
        //TODO: only check chunks in range
        foreach (var kvp in PhysicsObjects)
        {
            foreach (var obj in kvp.Value)
            {
                if ((obj.GlobalPosition - pos).LengthSquared() < range * range) yield return obj;
            }
        }
    }
    public Combatant CollidesWithEnemy(Box box, Team team)
    {
        //TODO: only check chunks in range
        foreach (var l in Combatants.Values)
        foreach (var c in l)
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
            if (chunk.State == ChunkState.Unloaded) OnChunkReady?.Invoke(chunk);
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
                if (inGroup.State == ChunkState.Unloaded) OnChunkReady?.Invoke(inGroup);
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
                                OnChunkReady?.Invoke(cgc);
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
        OnChunkUnload?.Invoke(c);
        //Mesher.Singleton.Unload(c);
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
    //only updates chunks at the end of the batch
    //call batch(coord, block) to set the blocks
    public void BatchSetBlock(System.Action<System.Action<BlockCoord, Block>> batch) {
        List<Chunk> chunksToUpdate = new List<Chunk>();
        batch((coords, block) => {
            ChunkCoord chunkCoords = (ChunkCoord)coords;
            BlockCoord blockCoords = Chunk.WorldToLocal(coords);
            Chunk c = GetOrCreateChunk(chunkCoords);
            c[blockCoords] = block;
            if (!chunksToUpdate.Contains(c)) chunksToUpdate.Add(c);
        });
        foreach (Chunk c in chunksToUpdate) {
            OnChunkUpdate?.Invoke(c);
        }
    }
    public void SetBlock(BlockCoord coords, Block block, bool updateChunk=true) {
        ChunkCoord chunkCoords = (ChunkCoord)coords;
        BlockCoord blockCoords = Chunk.WorldToLocal(coords);
        Chunk c = GetOrCreateChunk(chunkCoords);
        c[blockCoords] = block;
        if (updateChunk && c.State == ChunkState.Loaded) {
            OnChunkUpdate?.Invoke(c);
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
    private void physicsObjectCrossChunkBoundary(PhysicsObject p, ChunkCoord oldChunk)
    {
        GD.Print("PhysicsObject " + p + " crossed chunk boundary from " + oldChunk + " to " + (ChunkCoord)p.GlobalPosition);
        //remove from old list, then add to new one
        //keep combatant list updated if applicable
        if (PhysicsObjects.TryGetValue(oldChunk, out List<PhysicsObject> oldList))
        {
            oldList.Remove(p);
            if (oldList.Count == 0) PhysicsObjects.Remove(oldChunk);
        }
        if (PhysicsObjects.TryGetValue((ChunkCoord)p.GlobalPosition, out List<PhysicsObject> list))
        {
            list.Add(p);
        }
        else
        {
            PhysicsObjects[(ChunkCoord)p.GlobalPosition] = new List<PhysicsObject> { p };
        }
        if (p is Combatant c)
        {
            if (Combatants.TryGetValue(oldChunk, out List<Combatant> oldList2))
            {
                oldList2.Remove(c);
                if (oldList2.Count == 0) Combatants.Remove(oldChunk);
            }
            if (Combatants.TryGetValue((ChunkCoord)c.GlobalPosition, out List<Combatant> list2))
            {
                list2.Add(c);
            }
            else
            {
                Combatants[(ChunkCoord)c.GlobalPosition] = new List<Combatant> { c };
            }
        }
    }
    //init runs before object is added to scene tree
    public T SpawnObject<T>(PackedScene prefab, Vector3 position, System.Action<T> init=null) where T :PhysicsObject
    {
        T c = prefab.Instantiate<T>();
        c.Registered = true;
        init?.Invoke(c);
        AddChild(c);
        c.OnCrossChunkBoundary += physicsObjectCrossChunkBoundary;
        c.GlobalPosition = position;
        physicsObjectCrossChunkBoundary(c, (ChunkCoord)position);
        return c;
    }

    public void RegisterObject(PhysicsObject obj)
    {
        obj.OnCrossChunkBoundary += physicsObjectCrossChunkBoundary;
        physicsObjectCrossChunkBoundary(obj, (ChunkCoord)obj.GlobalPosition);
    }
}
