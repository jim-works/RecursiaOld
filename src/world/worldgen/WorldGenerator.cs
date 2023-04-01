using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;

public partial class WorldGenerator
{
    private const int POLL_INTERVAL_MS = 100;
    public int ShapingThreads {get; private set;} = Godot.Mathf.Max(1,System.Environment.ProcessorCount-4); //seems reasonable
    public int StructuresPerChunk = 10; //seems reasonable
        
    //contains chunks requested from outside sources (world loading)
    private ConcurrentDictionary<ChunkCoord, Chunk> generating = new ();
    //finished chunks that are ready to be send to the mesher
    private volatile ConcurrentBag<(Chunk, AtomicChunkCollection)> done = new ConcurrentBag<(Chunk, AtomicChunkCollection)>();

    private List<IChunkGenLayer> shapingLayers = new List<IChunkGenLayer>();
    private List<StructureProvider> structureProviders = new List<StructureProvider>();

    public int Seed {get; private set;} = 1127;
    private int currSeed;

    public WorldGenerator()
    {
        currSeed = Seed;
        //initialize worldgen threads
        Godot.GD.Print($"Starting world generator with {ShapingThreads} threads!");
        shapingLayers.Add(initLayer(new ShapingLayer()));
        shapingLayers.Add(initLayer(new DetailLayer()));
        //chunkGenLayers.Add(initLayer(new OreLayer() {Ore=BlockTypes.Get("copper_ore"),RollsPerChunk=2,VeinProb=0.5f,StartDepth=0,MaxProbDepth=-10,VeinSize=10}));
        //structureProviders.Add(new TreeStructureProvider());
        structureProviders.Add(new BoxStructureProvider());
    }

    private int getNextSeed()
    {
        unchecked
        {
            //"randomize" seeds for next layer
            //numbers chosen by fair dice roll
            currSeed ^= currSeed << 13;
            currSeed ^= currSeed >> 17;
            currSeed ^= currSeed << 5;
            currSeed *= 0x8001AD;    
        }
        return currSeed;
    }

    private IChunkGenLayer initLayer(IChunkGenLayer layer)
    {
        layer.InitRandom(getNextSeed);
        return layer;
    }

    //multithreaded world generation, queues c to be generated
    //returns true if c was queued, false if it (or another chunk at the same coordinates) was already queued
    public bool GenerateDeferred(Chunk c)
    {
        if (!generating.ContainsKey(c.Position))
        {
            generating[c.Position] = c;
            Chunk gen = c;
            Task.Run(async () => await doGeneration(gen));
            return true;
        }
        return false;
    }
    
        //empties finishedGenerations and sends all those chunks that are still valid to the mesher
    //a valid chunk is loaded in the world
    public void GetFinishedChunks(List<Chunk> dest)
    {
        foreach (var c in done) {
            dest.Add(c.Item1);
            c.Item2.Commit();
            generating.TryRemove(c.Item1.Position, out _);
        }
        done.Clear();
    }

    private async Task doGeneration(Chunk toGenerate)
    {
        ShapeChunk(World.Singleton, toGenerate);
        AtomicChunkCollection collection = await GenerateStructures(World.Singleton, toGenerate);
        done.Add((toGenerate, collection));
    }

    public void ShapeChunk(World world, Chunk chunk) {
        chunk.GenerationState = ChunkGenerationState.SHAPING;
        foreach (var genLayer in shapingLayers)
        {
            genLayer.GenerateChunk(world,chunk);
        }
        chunk.GenerationState = ChunkGenerationState.SHAPED;
    }

    public async Task<AtomicChunkCollection> GenerateStructures(World world, Chunk chunk)
    {
        chunk.GenerationState = ChunkGenerationState.PLACING_STRUCTURES;
        AtomicChunkCollection area = new AtomicChunkCollection(world);
        foreach (var provider in structureProviders)
        {
                int dx = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dy = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dz = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                BlockCoord origin = chunk.LocalToWorld(new BlockCoord(dx, dy, dz));
                if (!provider.SuitableLocation(world, origin)) continue;
                await requestArea(world, chunk.Position, provider.MaxArea, area);
                Structure result = await provider.PlaceStructure(area, origin);
                if (result != null && provider.Record)
                {
                    chunk.Structures.Add(result);
                }
        }
        chunk.GenerationState = ChunkGenerationState.GENERATED;
        return area;
    }

    //populates the expanding dictionary
    //returns a task that completes when the area is ready (all chunks are shaped)
    private async Task<AtomicChunkCollection> requestArea(World world, ChunkCoord center, ChunkCoord size, AtomicChunkCollection collection)
    {
        //coord, subscriber
        HashSet<ChunkCoord> needed = new ();
        int neededCount = 0;
        for (int x = center.X-size.X; x < center.X + size.X; x++)
        {
            for (int y = center.Y-size.Y; y < center.Y + size.Y; y++)
            {
                for (int z = center.Z-size.Z; z < center.Z + size.Z; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x,y,z);
                    neededCount ++;
                    world.GetOrLoadChunkCheckDisk(coord, res => {
                        if (res != null && res.GenerationState >= ChunkGenerationState.SHAPED) {
                            collection.Add(res);
                            neededCount--;
                        }
                        else {
                            needed.Add(coord);
                            if (res == null) world.GenerateChunkDeferred(coord);
                        }
                    });
                }
            }
        }
        //wait until all chunks are done
        while (neededCount > 0)
        {
            await Task.Delay(POLL_INTERVAL_MS);
            Godot.GD.Print($"Waiting for {neededCount} chunks to be shaped at {center}...");
            try {
                List<ChunkCoord> toRemove = new List<ChunkCoord>();
                foreach (var coord in needed)
                if (world.GetChunk(coord) is Chunk c && c.GenerationState >= ChunkGenerationState.SHAPED) {
                    collection.Add(c);
                    toRemove.Add(coord);
                }
                foreach (var coord in toRemove) {
                    needed.Remove(coord);
                    neededCount--;
                }
            } catch (Exception e) {
                Godot.GD.PrintErr(e);
            }
        }
        Godot.GD.Print($"All chunks shaped at {center}!");
        return collection;
    }
}