using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

public partial class WorldGenerator
{
    private const int POLL_INTERVAL = 10;
    public int WorldgenThreads {get; private set;} = Godot.Mathf.Max(1,System.Environment.ProcessorCount-4); //seems reasonable
    public int StructuresPerChunk = 10; //seems reasonable
        
    //contains chunks requested from outside sources (world loading)
    //next step: either placingStructures or done
    private volatile HashSet<Chunk>[] shaping;
    //contains chunks that need to be shaped to place structures (key), and  a pointer to the chunk if it's ready (value)
    private volatile Dictionary<ChunkCoord, Chunk>[] expanding;
    private volatile ConcurrentDictionary<ChunkCoord, Chunk> expanded = new ConcurrentDictionary<ChunkCoord, Chunk>();
    //list of chunks that will contain the origin of a structure
    //next step: done
    private volatile Queue<Chunk> placingStructures = new Queue<Chunk>();
    //finished chunks that are ready to be send to the mesher
    private volatile List<Chunk> done = new List<Chunk>();

    //we have a big pool of threads for shaping
    private Thread[] generationThreads;
    //and for simplicity, only one thread for placing structures
    private Thread structureThread;

    private List<IChunkGenLayer> chunkGenLayers = new List<IChunkGenLayer>();
    private List<StructureProvider> structureProviders = new List<StructureProvider>();

    private float currSeed=1000.875f;

    public WorldGenerator()
    {
        //initialize worldgen threads
        Godot.GD.Print($"Starting world generator with {WorldgenThreads} threads!");
        chunkGenLayers.Add(initLayer(new HeightmapLayer()));
        chunkGenLayers.Add(initLayer(new OreLayer() {Ore=BlockTypes.Get("copper_ore"),RollsPerChunk=2,VeinProb=0.5f,StartDepth=0,MaxProbDepth=-10,VeinSize=10}));
        structureProviders.Add(new TreeStructureProvider());
        shaping = new HashSet<Chunk>[WorldgenThreads];
        expanding = new Dictionary<ChunkCoord, Chunk>[WorldgenThreads];
        generationThreads = new Thread[WorldgenThreads];
        for (int i = 0; i < WorldgenThreads; i++)
        {
            int id = i; //copy to avoid race condition
            shaping[i] = new HashSet<Chunk>();
            expanding[i] = new Dictionary<ChunkCoord, Chunk>();
            generationThreads[i] = new Thread(() => generationLoop(id));
            generationThreads[i].Start();
        }
        structureThread = new Thread(async () => await structureLoop());
        structureThread.Start();
    }


    private IChunkGenLayer initLayer(IChunkGenLayer layer)
    {
        layer.InitRandom(currSeed);
        currSeed *= 1.5987f;
        currSeed += 7.983f;
        return layer;
    }

    private int getThreadIndex(ChunkCoord c) {
        //positive mod of chunk's position to find which thread to assign it to
        //this gives a disjoint set of chunks to each thread, so we don't have to worry about them modifying the same chunk.
        int tid = (c.X+c.Y+c.Z) % WorldgenThreads;
        return tid < 0 ? tid + WorldgenThreads : tid;
    }

    //multithreaded world generation
    //I was chasing a race condition using tasks, so I switched to my own thread implementation.
    //Turns out I'm dumb and missed the race condition. But I'm too lazy to switch back to tasks
    //So we have this implementation instead :)
    public void GenerateDeferred(Chunk c)
    {
        int tid = getThreadIndex(c.Position);
        c.GenerationState = ChunkGenerationState.SHAPING;
        lock (shaping)
        {
            shaping[tid].Add(c);
        }
    }

    public void GenerateArea(ChunkCoord origin, ChunkCoord radius)
    {
        
    }
    //runs on multiple generationThreads
    private void generationLoop(int id)
    {
        List<Chunk> myQueue = new List<Chunk>();
        List<Chunk> myExpandingQueue = new List<Chunk>();
        while (true)
        {
            Thread.Sleep(POLL_INTERVAL);
            lock (shaping)
            {
                foreach(Chunk c in shaping[id]) myQueue.Add(c);
                shaping[id].Clear();
            }
            lock (expanding)
            {
                foreach(var kvp in expanding[id]) myExpandingQueue.Add(kvp.Value);
                expanding[id].Clear();
            }
            foreach (var c in myQueue)
            {
                ShapeChunk(World.Singleton, c);
                c.GenerationState = ChunkGenerationState.PLACING_STRUCTURES;
            }
            foreach (var c in myExpandingQueue)
            {
                ShapeChunk(World.Singleton, c);
                c.GenerationState = ChunkGenerationState.SHAPED;
                if (!expanded.TryAdd(c.Position, c)) Godot.GD.PrintErr($"cannot add chunk {c} to expanded");
            }
            lock (placingStructures) foreach (var c in myQueue) placingStructures.Enqueue(c);
            myQueue.Clear();
            myExpandingQueue.Clear();
        }
    }
    //run on single background thread
    private async Task structureLoop()
    {
        List<Chunk> toPlace = new List<Chunk>();
        while (true) {
            toPlace.Clear();
            lock (placingStructures) {
                toPlace.AddRange(placingStructures);
                placingStructures.Clear();
            }
            foreach (Chunk c in toPlace)
            {
                await placeStructures(World.Singleton, c);
            }
            await Task.Delay(POLL_INTERVAL);
        }
    }

    //populates the expanding dictionary
    private async Task<ChunkCollection> requestArea(World world, ChunkCoord corner, ChunkCoord size)
    {
        ChunkCollection result = new ChunkCollection();
        List<ChunkCoord> waitingForExpansion = new List<ChunkCoord>();
        for (int x = corner.X; x < corner.X + size.X; x++)
        {
            for (int y = corner.Y; y < corner.Y + size.Y; y++)
            {
                for (int z = corner.Z; z < corner.Z + size.Z; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x,y,z);
                    if (world.Chunks.Contains(coord)) continue; //won't shape if already exists
                    int tid = getThreadIndex(coord);

                    Chunk chunk = world.CreateChunk(coord);
                    lock (expanding)
                    {
                        expanding[tid][coord] = chunk;
                        waitingForExpansion.Add(coord);
                    }
                }
            }
        }
        //wait until all chunks are done
        while (waitingForExpansion.Count > 0)
        {
            await Task.Delay(POLL_INTERVAL);
            waitingForExpansion.RemoveAll(coord => {
                if (expanded.TryGetValue(coord, out Chunk ex) && ex.GenerationState == ChunkGenerationState.SHAPED && expanded.TryRemove(coord, out Chunk c)) {
                    result.Add(c);
                    return true;
                }
                return false;
            });
        }
        return result;
    }
    //empties finishedGenerations and sends all those chunks that are still valid to the mesher
    //a valid chunk is loaded in the world
    public void GetFinishedChunks(List<Chunk> dest)
    {
        lock (done)
        {
            dest.AddRange(done);
            done.Clear();
        }
    }
    public void ShapeChunk(World world, Chunk chunk) {
        foreach (var genLayer in chunkGenLayers)
        {
            genLayer.GenerateChunk(world,chunk);
        }
        chunk.GenerationState = ChunkGenerationState.SHAPED;
    }
    private async Task placeStructures(World world, Chunk chunk)
    {
        foreach (var provider in structureProviders)
        {
            //TODO: use regions for restrictions
            for (int i = 0; i < 5; i++)
            {
                int dx = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dy = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dz = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                BlockCoord origin = chunk.LocalToWorld(new BlockCoord(dx, dy, dz));
                if (!provider.SuitableLocation(world, origin)) continue;
                ChunkCollection area = await requestArea(world, chunk.Position, provider.MaxArea);
                Structure result = await provider.PlaceStructure(world.Chunks, origin);
                if (result != null && provider.Record)
                {
                    chunk.AddStructure(result);
                }
                lock (done)
                {
                    foreach (var c in area)
                    {
                        c.Value.GenerationState = ChunkGenerationState.GENERATED;
                        done.Add(c.Value);
                    }
                }
            }
        }
        lock (done)
        {
            chunk.GenerationState = ChunkGenerationState.GENERATED;
            done.Add(chunk);
        }
    }
}