using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;

namespace Recursia;
public partial class WorldGenerator
{
    private const int POLL_INTERVAL_MS = 100;
    public int ShapingThreads {get; private set;} = Godot.Mathf.Max(1,System.Environment.ProcessorCount-4); //seems reasonable
    public int StructuresPerChunk = 10; //seems reasonable

    //contains chunks requested from outside sources (world loading)
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> generating = new ();
    //finished chunks that are ready to be send to the mesher
    private readonly ConcurrentBag<(Chunk, AtomicChunkCollection)> done = new();

    private readonly List<IChunkGenLayer> shapingLayers = new();
    private readonly List<WorldStructureProvider> structureProviders = new();
    private readonly List<Chunk> toSend = new();

    public int Seed {get; private set;} = 1127;
    private int currSeed;
    private readonly World world;

    public WorldGenerator(World world)
    {
        currSeed = Seed;
        this.world = world;
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
    public void GetFinishedChunks(Action<List<Chunk>> dest)
    {
        while (!done.IsEmpty)
        {
            if (done.TryTake(out var item))
            {
                Chunk c = item.Item1;
                AtomicChunkCollection changes = item.Item2;
                toSend.Add(c);
                c.AddEvent("sent to dest");
                changes.Commit();
            }
        }
        dest(toSend);
        foreach (Chunk c in toSend)
        {
            generating.TryRemove(c.Position, out _);
            c.AddEvent("removed from generating");
        }
        toSend.Clear();
    }

    private async Task doGeneration(Chunk toGenerate)
    {
        toGenerate.AddEvent("do generation");
        ShapeChunk(toGenerate);
        toGenerate.AddEvent("shaped");
        AtomicChunkCollection collection = await GenerateStructures(toGenerate);
        toGenerate.AddEvent("structured");
        done.Add((toGenerate, collection));
        toGenerate.AddEvent("sent to done");
    }

    public void ShapeChunk(Chunk chunk) {
        chunk.GenerationState = ChunkGenerationState.SHAPING;
        foreach (var genLayer in shapingLayers)
        {
            genLayer.GenerateChunk(world,chunk);
        }
        chunk.GenerationState = ChunkGenerationState.SHAPED;
    }

    public async Task<AtomicChunkCollection> GenerateStructures(Chunk chunk)
    {
        chunk.GenerationState = ChunkGenerationState.PLACING_STRUCTURES;
        AtomicChunkCollection area = new(world);
        foreach (var provider in structureProviders)
        {
                int dx = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dy = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dz = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                BlockCoord origin = chunk.LocalToWorld(new BlockCoord(dx, dy, dz));
                if (!provider.SuitableLocation(world, origin)) continue;
                await requestArea(chunk.Position, provider.MaxArea, area);
                WorldStructure result = provider.PlaceStructure(area, origin);
                if (result != null && provider.Record)
                {
                    chunk.Structures.Add(result);
                }
                foreach (var kvp in area)
                {
                    kvp.Value.Unstick();
                }
        }
        chunk.GenerationState = ChunkGenerationState.GENERATED;
        return area;
    }

    //populates the expanding dictionary
    //returns a task that completes when the area is ready (all chunks are shaped)
    private async Task<AtomicChunkCollection> requestArea(ChunkCoord center, ChunkCoord size, AtomicChunkCollection collection)
    {
        //coord, need to stick
        HashSet<ChunkCoord> needed = new ();
        for (int x = center.X-size.X; x < center.X + size.X; x++)
        {
            for (int y = center.Y-size.Y; y < center.Y + size.Y; y++)
            {
                for (int z = center.Z-size.Z; z < center.Z + size.Z; z++)
                {
                    ChunkCoord coord = new(x,y,z);
                    world.GetStickyChunkOrLoadFromDisk(coord, res => {
                        if (res?.GenerationState >= ChunkGenerationState.SHAPED) {
                            collection.Add(res);
                        }
                        else {
                            needed.Add(coord);
                            if (res == null) world.GenerateChunkDeferred(coord, true);
                        }
                    });
                }
            }
        }
        //wait until all chunks are done
        while (needed.Count > 0)
        {
            await Task.Delay(POLL_INTERVAL_MS);
            needed.RemoveWhere(coord =>
            {
                if (world.GetChunk(coord) is Chunk c && c.GenerationState >= ChunkGenerationState.SHAPED)
                {
                    collection.Add(c);
                    return true;
                }
                return false;
            });
        }
        return collection;
    }
}