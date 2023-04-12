using Godot;
using System.Collections.Generic;

//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
public partial class World : Node
{
    public ChunkCollection Chunks = new ChunkCollection();
    public EntityCollection Entities;
    
    public WorldGenerator WorldGen;
    public WorldLoader Loader;
    public event System.Action<Chunk> OnChunkUpdate;
    public event System.Action<Chunk> OnChunkReady;
    public event System.Action<Chunk> OnChunkUnload;

    private double chunkLoadingInterval = 0.5f; //seconds per chunk loading update
    private double _chunkLoadingTimer = 0;
    private WorldSaver saver;

    public override void _EnterTree()
    {
        Loader = new(this);
        Entities = new(this);
        WorldGen = new(this);
        Chunks.OnChunkOverwritten += (c) => GD.Print($"Duplicate chunk {c}");
        base._EnterTree();
    }

    public override void _Ready()
    {
        saver = GetNode<WorldSaver>("WorldSaver");
        _chunkLoadingTimer = 9999;
        Loader.UpdateChunkLoading();
        base._Ready();
    }

    public override void _Process(double delta)
    {
        WorldGen.GetFinishedChunks(fromWorldGen => {
            foreach (var item in fromWorldGen)
            {
                if (OnChunkReady == null) GD.PrintErr("no subscriber!");
                item.AddEvent("on chunk ready");
                OnChunkReady?.Invoke(item);
                item.Unstick(); //we keep chunk sticky until it's done generating
            }
        });

        if (GlobalConfig.UseInfiniteWorlds)
        {
            _chunkLoadingTimer += delta;
            if (_chunkLoadingTimer >= chunkLoadingInterval) {
                _chunkLoadingTimer = 0;
                Loader.UpdateChunkLoading();
            }
        }
        base._Process(delta);
    }
    
    public void LoadChunk(ChunkCoord coord) {
        GetStickyChunkOrLoadFromDisk(coord, (Chunk c) => {
            if (c == null) {
                GenerateChunkDeferred(coord, false);
            } else {
                //dont' want this to be sticky
                c.Unstick();
            }
        });
    }
    public void UnloadChunk(ChunkCoord coord) {
        if (!Chunks.TryGetValue(coord, out Chunk c)) return;//already unloaded
        lock (c)
        {
            if (c.State == ChunkState.Sticky) return; //don't unload sticky chunks
            c.ForceUnload();
            saver.Save(c);
            OnChunkUnload?.Invoke(c);
            Chunks.TryRemove(coord, out var _);
        }
    }
    private Chunk getOrCreateChunk(ChunkCoord chunkCoords, bool sticky) {
        if(Chunks.TryGetValue(chunkCoords, out Chunk c)) {
            //chunk already exists
            if (sticky) c.Stick();
            return c;
        }
        return createChunk(chunkCoords, sticky);
    }
    private Chunk createChunk(ChunkCoord chunkCoords, bool sticky) {
        Chunk c = new Chunk(chunkCoords);
        if (sticky) c.Stick();
        Chunks[chunkCoords] = c;
        return c;
    }
    //gets a chunk from memory, or loads it from disk if it's not in memory
    //calls callback when load is completed. returned chunk is sticky, call c.Unstick() when done.
    //doesn't generate a new chunk, callback is invoked with null if it doesn't exist
    //never returns a chunk in UNLOADED state
    public void GetStickyChunkOrLoadFromDisk(ChunkCoord coord, System.Action<Chunk> callback)
    {
        if (GetAndStick(coord) is Chunk c)
        {
            callback?.Invoke(c);
            return;
        }
        //chunk doesn't exist, load it
        saver.LoadAndStick(coord, c => {
            //make sure is hasn't already been loaded since we went to the db
            if (GetAndStick(coord) is Chunk loaded)
            {
                callback?.Invoke(loaded);
                //make sure to unload the one recieved from disk, probably older
                c?.Unstick();
                return;
            } 
            if (c == null) {
                callback?.Invoke(null);
                return;
            }
            loadChunk(c);
            c.AddEvent("loaded from disk");
            callback?.Invoke(c);
        });
    }
    public Chunk GetAndStick(ChunkCoord coord)
    {
        Chunk c = GetChunk(coord);
        if (c == null) return null;
        lock (c)
        {
            if (c.State == ChunkState.Unloaded)
            {
                //c is going to be unloaded, return null.
                return null;
            }
            c.Stick();
        }
        return c;
    }
    //doesn't hold a lock on c, take out a lock on c if needed
    private void loadChunk(Chunk c)
    {
        Chunks[c.Position] = c;
        OnChunkReady?.Invoke(c);
    }
    //generates a chunk if it doesn't exist, thread-safe
    //if stick, we sticky the chunk twice. it will get unstickied once when it spawns
    //IF STICK, YOU HAVE TO UNSTICKY
    public void GenerateChunkDeferred(ChunkCoord coord, bool stick)
    {
        //need lock to be thread safe on the condition - todo verify
        //don't need to check if worldgen is in the process of generating the chunk since it will already be in World.Chunks before it gets sent to generator
        if (GetChunk(coord) != null) return;
        Chunk c = createChunk(coord, true);
        if (stick) c.Stick();
        WorldGen.GenerateDeferred(c);
    }
    public Chunk GetChunk(ChunkCoord chunkCoords) => Chunks[chunkCoords];
    public Block GetBlock(BlockCoord coords) => Chunks.GetBlock(coords);
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
            Chunk c = getOrCreateChunk(chunkCoords, false);
            c[blockCoords] = block;
            if (!chunksToUpdate.Contains(c)) chunksToUpdate.Add(c);
        });
        foreach (Chunk c in chunksToUpdate) {
            OnChunkUpdate?.Invoke(c);
            c.AddEvent("back set block");
            c.Unstick();
        }
    }
    public void SetBlock(BlockCoord coords, Block block, bool updateChunk=true) {
        ChunkCoord chunkCoords = (ChunkCoord)coords;
        BlockCoord blockCoords = Chunk.WorldToLocal(coords);
        Chunk c = getOrCreateChunk(chunkCoords, false);
        c[blockCoords] = block;
        c.AddEvent("set block");
        if (updateChunk && c.State >= ChunkState.Loaded) {
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
            b = GetBlock((BlockCoord)testPoint);
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
            b = GetBlock((BlockCoord)testPoint);
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
