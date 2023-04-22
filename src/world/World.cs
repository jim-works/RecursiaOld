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
        //Loader.UpdateChunkLoading();
        base._Ready();
    }

    public override void _Process(double delta)
    {
        WorldGen.GetFinishedChunks(fromWorldGen => {
            foreach (Chunk c in fromWorldGen)
            {
                if (OnChunkReady == null) GD.PrintErr("no subscriber!");
                c.AddEvent("on chunk ready");
                if (!Chunks.TryLoad(c)) {
                    GD.PushWarning($"Couldn't add chunk from world gen at {c.Position}");
                    continue;
                }
                OnChunkReady?.Invoke(c);
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
            (Chunk? c, _) = await saver!.LoadChunk(coord);
            if (c == null)
            {
                GenerateChunkDeferred(coord);
            }
            else
            {
                Chunks.TryLoad(c);
            }
        }
        catch (System.Exception e)
        {
            GD.PushError(e);
        }
    }
    public void UnloadChunk(ChunkCoord coord) {
        Chunks.TryUnload(coord, (c, buf) => {
            saver!.Save(c, buf);
            OnChunkUnload?.Invoke(c);
        });
    }
    //generates a chunk if it doesn't exist, thread-safe
    //ONLY GENERATES IF THE CHUNK IF NEWLY CREATED
    public void GenerateChunkDeferred(ChunkCoord coord)
    {
        if (!Chunks.Contains(coord))
        {
            //we will unstick when this chunk in generated when we call WorldGen.GetFinishedChunks
            WorldGen.GenerateDeferred(Chunks.GetOrCreateChunk(coord));
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
            if (Chunks.SetBlock(coords, block) is Chunk c && !chunksToUpdate.Contains(c))
            {
                chunksToUpdate.Add(c);
            }
        });
        foreach (Chunk c in chunksToUpdate) {
            OnChunkUpdate?.Invoke(c);
            c.AddEvent("batch set block");
        }
    }
    public void SetBlock(BlockCoord coords, Block? block) {
        if (Chunks.SetBlock(coords, block) is Chunk c && c.State >= ChunkState.Loaded) {
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
