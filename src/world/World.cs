using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
public partial class World : Node
{
    public static World Singleton;
    //loads a parent region of this level when we try to load a chunk (reduces # of times we read the file)
    [Export] public int LoadChunkRegionLevel = 1;
    public ChunkCollection Chunks = new ChunkCollection();
    //todo: optimize these
    public List<Player> Players = new List<Player>();
    public Player LocalPlayer;
    public HashSet<Node3D> ChunkLoaders = new HashSet<Node3D>();
    public WorldGenerator WorldGen;
    public event System.Action<Chunk> OnChunkUpdate;
    public event System.Action<Chunk> OnChunkReady;
    public event System.Action<Chunk> OnChunkUnload;
    
    private List<Chunk> fromWorldGen = new List<Chunk>();
    private HashSet<ChunkCoord> loadedChunks = new HashSet<ChunkCoord>();
    private List<ChunkCoord> toUnload = new List<ChunkCoord>();


    private Dictionary<ChunkCoord, List<PhysicsObject>> physicsObjects = new();
    private Dictionary<ChunkCoord, List<Combatant>> combatants = new ();

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

    public bool ClosestEnemy(Vector3 pos, Team team, float maxDist, out Combatant enemy)
    {
        float minSqrDist = float.PositiveInfinity;
        enemy = null;
        //TODO: only check chunks in range
        foreach (var l in combatants.Values)
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
        foreach (var l in combatants.Values)
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
        foreach (var kvp in physicsObjects)
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
        foreach (var l in combatants.Values)
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
        saver.Load(coord, c => {
            if (c == null) {
                c = CreateChunk(coord);
                WorldGen.GenerateDeferred(c);
            } else {
                AddChunk(c);
                OnChunkReady?.Invoke(c);
                c.Load();
            }
        });
    }
    private void unloadChunk(ChunkCoord coord) {
        if (!Chunks.Contains(coord)) return;//already unloaded
        Chunk c = Chunks[coord];
        c.Unload();
        saver.Save(c);
        OnChunkUnload?.Invoke(c);
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
        AddChunk(c);
        return c;
    }
    public void AddChunk(Chunk c) {
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
        removePhysicsObject(p,oldChunk);
        addPhysicsObject(p);
    }
    private void removePhysicsObject(PhysicsObject p, ChunkCoord from)
    {
        if (physicsObjects.TryGetValue(from, out List<PhysicsObject> physics))
        {
            physics.Remove(p);
            if (physics.Count == 0) physicsObjects.Remove(from);
        }
        if (GetChunk(from) is Chunk chunk)
        {
            chunk.PhysicsObjects.Remove(p);
        }
        if (p is Combatant c && combatants.TryGetValue(from, out List<Combatant> comb))
        {
            comb.Remove(c);
            if (combatants.Count == 0) combatants.Remove(from);
        }
    }
    private void addPhysicsObject(PhysicsObject p)
    {
        ChunkCoord to = (ChunkCoord)p.GlobalPosition;
        if (GetChunk(to) is Chunk chunk)
        {
            chunk.PhysicsObjects.Add(p);
        }
        if (physicsObjects.TryGetValue(to, out List<PhysicsObject> list))
        {
            list.Add(p);
        }
        else
        {
            physicsObjects[to] = new List<PhysicsObject> { p };
        }
        if (p is Combatant c)
        {
            if (combatants.TryGetValue(to, out List<Combatant> list2))
            {
                list2.Add(c);
            }
            else
            {
                combatants[to] = new List<Combatant> { c };
            }
        }
    }
    //init runs before object is added to scene tree
    //if will handle registering if T : PhysicsObject/Combatant
    public T SpawnObject<T>(PackedScene prefab, Vector3 position, System.Action<T> init=null) where T : Node3D
    {
        T obj = prefab.Instantiate<T>();
        var c = obj as PhysicsObject;
        if (c != null)
        {
            c.OldCoord = (ChunkCoord)position; //this way, if we spawn outside of (0,0,0), we won't add the object twice.
            c.InitialPosition = position;
        }
        init?.Invoke(obj);
        AddChild(obj);
        if (c != null) RegisterObject(c);
        return obj;
    }

    public void RemoveObject(PhysicsObject p)
    {
        removePhysicsObject(p, (ChunkCoord)p.GlobalPosition);
    }

    //will not register an object twice
    public void RegisterObject(PhysicsObject obj)
    {
        if (obj.Registered) return;
        obj.Registered = true;
        GD.Print("Registering object " + obj.Name);
        obj.OnCrossChunkBoundary += physicsObjectCrossChunkBoundary;
        obj.OnExitTree += RemoveObject;
        addPhysicsObject(obj);
    }
}
