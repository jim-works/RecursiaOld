using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
namespace Recursia;
public partial class World : Node
{
    [Export] public Texture2D? BlockTextureAtlas;
    [Export] public Vector3 SpawnPoint = new(0,10,0);
    public ChunkCollection Chunks;
    public EntityCollection Entities;

    public WorldGenerator WorldGen;
    public WorldLoader Loader;
    public event System.Action<Chunk>? OnChunkUpdate;
    public event System.Action<Chunk>? OnChunkReady;
    public event System.Action<Chunk>? OnChunkUnload;

    private double chunkLoadingInterval = 0.5f; //seconds per chunk loading update
    private double _chunkLoadingTimer;
    private WorldSaver? saver;

    public World()
    {
        Chunks = new();
        Loader = new(this);
        Entities = new(this);
        WorldGen = new(this);
    }
    public override void _EnterTree()
    {
        BlockLoader.Load(BlockTextureAtlas!);
        WorldGen.LoadLayers();
        base._EnterTree();
    }

    public override void _Ready()
    {
        saver = GetNode<WorldSaver>("WorldSaver");
        ObjectTypes.TryGetInstance(this, "player", SpawnPoint, out Player _);
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
                Chunk c = item.Chunk;
                c.AddEvent("on chunk ready");
                OnChunkReady?.Invoke(c);
                item.Dispose(); //we keep chunk sticky until it's done generating
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

    public async Task LoadChunk(ChunkCoord coord)
    {
        if (Chunks.Contains(coord)) return;
        try
        {
            Chunk.StickyReference? c = await GetStickyChunkOrLoadFromDisk(coord);
            if (c == null)
            {
                GenerateChunkDeferred(coord);
            }
            else
            {
                //dont' want this to be sticky
                c.Dispose();
            }
        }
        catch (System.Exception e)
        {
            GD.PushError(e);
        }
    }
    public void UnloadChunk(ChunkCoord coord) {
        Chunks.TryUnload(coord, c => {
            saver!.Save(c);
            OnChunkUnload?.Invoke(c);
        });
    }
    //gets a chunk from memory, or loads it from disk if it's not in memory
    //calls callback when load is completed. returned chunk is sticky, call c.Unstick() when done.
    //doesn't generate a new chunk, callback is invoked with null if it doesn't exist
    //never returns a chunk in UNLOADED state
    public async Task<Chunk.StickyReference?> GetStickyChunkOrLoadFromDisk(ChunkCoord coord)
    {
        if (Chunks.TryGetAndStick(coord, out Chunk.StickyReference? c))
        {
            return c;
        }
        else
        {
            return await loadFromDiskAndStick(coord);
        }
    }
    private async Task<Chunk.StickyReference?> loadFromDiskAndStick(ChunkCoord coord)
    {
        Chunk.StickyReference? r = await saver!.LoadAndStick(coord);
        //make sure is hasn't already been loaded since we went to the db
        if (Chunks.TryGetAndStick(coord, out Chunk.StickyReference? loaded))
        {
            //we have already loaded this since the request was put out
            r?.Dispose();
            return loaded;
        }
        else if (r == null)
        {
            //chunk does not exist on disk
            return null;
        }
        else
        {
            //exists on disk, but not in memory
            //let's load it
            Chunks.TryAdd(r.Chunk);
            r.Chunk.AddEvent("loaded from disk");
            OnChunkReady?.Invoke(r.Chunk);
            return r;
        }
    }
    //generates a chunk if it doesn't exist, thread-safe
    //if stick, we sticky the chunk twice. it will get unstickied once when it spawns
    //IF STICK, YOU HAVE TO UNSTICKY
    public Chunk.StickyReference? GenerateStickyChunkDeferred(ChunkCoord coord)
    {
        //don't need to check if worldgen is in the process of generating the chunk since it will already be in World.Chunks before it gets sent to generator
        if (Chunks.GetOrCreateStickyChunk(coord, out Chunk.StickyReference c))
        {
            //we will unstick when this chunk in generated when we call WorldGen.GetFinishedChunks
            //this second stick is unstickied by the caller
            if (WorldGen.GenerateDeferred(c))
            {
                return Chunk.StickyReference.Stick(c.Chunk);
            }
        }
        c.Dispose(); //not doing anything, don't hold sticky
        return null;
    }
    //generates a chunk if it doesn't exist, thread-safe
    //we sticky the chunk once. it will get unstickied once when it spawns
    //ONLY GENERATES IF THE CHUNK IF NEWLY CREATED
    public void GenerateChunkDeferred(ChunkCoord coord)
    {
        //don't need to check if worldgen is in the process of generating the chunk since it will already be in World.Chunks before it gets sent to generator
        if (Chunks.GetOrCreateStickyChunk(coord, out Chunk.StickyReference c))
        {
            //we will unstick when this chunk in generated when we call WorldGen.GetFinishedChunks
            WorldGen.GenerateDeferred(c);
        }
        else
        {
            c.Dispose();
        }
    }
    public Block? GetBlock(BlockCoord coords) => Chunks.GetBlock(coords);
    public ItemStack BreakBlock(BlockCoord coords) {
        DropTable? dt = GetBlock(coords)?.DropTable;
        if (dt == null) {
            GD.PushWarning("Null droptable!");
        }
        SetBlock(coords, null);
        return dt?.GetDrop() ?? new ItemStack();
    }
    //only updates chunks at the end of the batch
    //call batch(coord, block) to set the blocks
    //will not create new chunks
    public void BatchSetBlock(System.Action<System.Action<BlockCoord, Block?>> batch) {
        List<Chunk> chunksToUpdate = new();
        batch((coords, block) => {
            ChunkCoord chunkCoords = (ChunkCoord)coords;
            BlockCoord blockCoords = Chunk.WorldToLocal(coords);
            if (Chunks.TryGetChunk(chunkCoords, out Chunk? c))
            {
                c[blockCoords] = block;
                if (!chunksToUpdate.Contains(c)) chunksToUpdate.Add(c);
            }
        });
        foreach (Chunk c in chunksToUpdate) {
            OnChunkUpdate?.Invoke(c);
            c.AddEvent("batch set block");
            //c.Unstick();
        }
    }
    public void SetBlock(BlockCoord coords, Block? block, bool updateChunk=true) {
        ChunkCoord chunkCoords = (ChunkCoord)coords;
        BlockCoord blockCoords = Chunk.WorldToLocal(coords);
        Chunks.GetOrCreateChunk(chunkCoords, out Chunk c);
        c[blockCoords] = block;
        c.AddEvent("set block");
        if (updateChunk && c.State >= ChunkState.Loaded) {
            OnChunkUpdate?.Invoke(c);
        }
    }
    //returns the block closest to origin that intersects the line segment from origin to (origin + line)
    public BlockcastHit? Blockcast(Vector3 origin, Vector3 line) {
        //TODO: improve this
        const float stepSize = 0.05f;
        float lineLength = line.Length();
        Vector3 lineNorm = line/lineLength;
        BlockCoord oldCoords = (BlockCoord)origin;
        Block? b = GetBlock(oldCoords);
        if (b != null)
        {
            return new BlockcastHit
            {
                HitPos = origin,
                BlockPos = oldCoords,
                Block = b,
                Normal = Vector3.Zero,
            };
        }

        for (float t = 0; t < lineLength; t += stepSize) {
            //only query world when we are in a new block
            Vector3 testPoint = origin + t*lineNorm;
            BlockCoord coords = (BlockCoord)testPoint;
            if (coords == oldCoords) continue;
            oldCoords=coords;
            b = GetBlock((BlockCoord)testPoint);
            if (b != null)
            {
                return new BlockcastHit
                {
                    HitPos = testPoint,
                    BlockPos = oldCoords,
                    Block = b,
                    //normal direction will be the greatest difference from the center
                    Normal = Math.MaxComponent(testPoint - ((Vector3)oldCoords + new Vector3(0.5f, 0.5f, 0.5f))).Normalized()
                };
            }
        }
        return null;
    }
    //adds all non-null blocks intersecting the line between origin and dest to buffer
    public void BlockcastAll(Vector3 origin, Vector3 dest, List<BlockcastHit> buffer) {
        //TODO: improve this
        const float stepSize = 0.05f;
        Vector3 d = dest-origin;
        float lineLength = d.Length();
        Vector3 lineNorm = d/lineLength;
        BlockCoord oldCoords = (BlockCoord)origin;
        Block? b = GetBlock(oldCoords);
        if (b != null)
        {
            buffer.Add(new BlockcastHit
            {
                HitPos = origin,
                BlockPos = oldCoords,
                Block = b,
                Normal = Vector3.Zero
            });
        }

        for (float t = 0; t < lineLength; t += stepSize) {
            //only query world when we are in a new block
            Vector3 testPoint = origin + t*lineNorm;
            BlockCoord coords = (BlockCoord)testPoint;
            if (coords == oldCoords) continue;
            oldCoords=coords;
            b = GetBlock((BlockCoord)testPoint);
            if (b != null)
            {
                buffer.Add(new BlockcastHit
                {
                    HitPos = testPoint,
                    BlockPos = oldCoords,
                    Block = b,
                    //normal direction will be the greatest difference from the center
                    Normal = Math.MaxComponent((Vector3)oldCoords + new Vector3(0.5f, 0.5f, 0.5f) - testPoint).Normalized()
                });
            }
        }
    }
}
