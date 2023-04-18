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
    private readonly ConcurrentBag<(Chunk.StickyReference, StickyChunkCollection)> done = new();

    private readonly List<IChunkGenLayer> shapingLayers = new();
    private readonly List<WorldStructureProvider> structureProviders = new();
    private readonly List<Chunk.StickyReference> toSend = new();

    public int Seed {get; private set;} = 1127;
    private int currSeed;
    private readonly World world;

    public WorldGenerator(World world)
    {
        currSeed = Seed;
        this.world = world;
    }

    public void LoadLayers()
    {
        //initialize worldgen threads
        Godot.GD.Print($"Starting world generator with {ShapingThreads} threads!");
        shapingLayers.Add(new ShapingLayer(getNextSeed));
        shapingLayers.Add(new DetailLayer());
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

    //multithreaded world generation, queues c to be generated
    //returns true if c was queued, false if it (or another chunk at the same coordinates) was already queued
    public bool GenerateDeferred(Chunk.StickyReference cref)
    {
        Chunk c = cref.Chunk;
        if (c.GenerationState == ChunkGenerationState.UNGENERATED && generating.TryAdd(c.Position, c))
        {
            var gen = cref;
            Task.Run(async () => await doGeneration(gen));
            return true;
        }
        return false;
    }

    //empties finishedGenerations and sends all those chunks that are still valid to the mesher
    //a valid chunk is loaded in the world
    public void GetFinishedChunks(Action<List<Chunk.StickyReference>> dest)
    {
        while (!done.IsEmpty)
        {
            if (done.TryTake(out var item))
            {
                Chunk.StickyReference c = item.Item1;
                StickyChunkCollection changes = item.Item2;
                toSend.Add(c);
                c.Chunk.AddEvent("sent to dest");
                //have to commit on main thread
                changes.Commit();
                changes.Dispose();
            }
        }
        dest(toSend);
        foreach (Chunk.StickyReference c in toSend)
        {
            if (generating.TryRemove(c.Chunk.Position, out _))
            {
                c.Chunk.AddEvent("removed from generating");
            }
        }
        toSend.Clear();
    }

    private async Task doGeneration(Chunk.StickyReference toGenerate)
    {
        try
        {
            toGenerate.Chunk.AddEvent("do generation");
            ShapeChunk(toGenerate.Chunk);
            toGenerate.Chunk.AddEvent("shaped");
            StickyChunkCollection collection = await GenerateStructures(toGenerate.Chunk);
            toGenerate.Chunk.AddEvent("structured");
            done.Add((toGenerate, collection));
            toGenerate.Chunk.AddEvent("sent to done");
        }
        catch (Exception e)
        {
            Godot.GD.PushError(e);
        }
    }

    public void ShapeChunk(Chunk chunk) {
        chunk.GenerationState = ChunkGenerationState.SHAPING;
        foreach (var genLayer in shapingLayers)
        {
            genLayer.GenerateChunk(world,chunk);
        }
        chunk.GenerationState = ChunkGenerationState.SHAPED;
    }

    public async Task<StickyChunkCollection> GenerateStructures(Chunk chunk)
    {
        chunk.GenerationState = ChunkGenerationState.PLACING_STRUCTURES;
        StickyChunkCollection area = new(world);
        foreach (var provider in structureProviders)
        {
                int dx = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dy = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dz = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                BlockCoord origin = chunk.LocalToWorld(new BlockCoord(dx, dy, dz));
                if (!provider.SuitableLocation(world, origin)) continue;
                await requestArea(chunk.Position, provider.MaxArea, area);
                WorldStructure? result = provider.PlaceStructure(area, origin);
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
    private async Task requestArea(ChunkCoord center, ChunkCoord size, StickyChunkCollection collection)
    {
        //coord, need to stick
        List<(ChunkCoord, Task<Chunk.StickyReference?>)> toProcess = new();
        HashSet<Chunk> needed = new ();
        for (int x = center.X-size.X; x <= center.X + size.X; x++)
        {
            for (int y = center.Y-size.Y; y <= center.Y + size.Y; y++)
            {
                for (int z = center.Z-size.Z; z <= center.Z + size.Z; z++)
                {
                    ChunkCoord coord = new(x, y, z);
                    //no need to load/sticky for each structure if we already have the chunk in the collection
                    if (!collection.ContainsKey(coord)) toProcess.Add((coord, Task.Run(() => world.GetStickyChunkOrLoadFromDisk(coord))));
                }
            }
        }
        Chunk.StickyReference?[] results = await Task.WhenAll(toProcess.Select(x => x.Item2));
        for (int i = 0; i < results.Length; i++)
        {
            var res = results[i];
            ChunkCoord coord = toProcess[i].Item1;
            if (res == null)
            {
                //we need to generate the chunk
                if (world.GenerateStickyChunkDeferred(coord) is Chunk.StickyReference chunkRef)
                {
                    //we will unsticky this after changes are commited
                    if (collection.TryAdd(chunkRef))
                    {
                        chunkRef.Chunk.AddEvent("genadd stickychunkcollection");
                        needed.Add(chunkRef.Chunk);
                    }
                    else
                    {
                        //shoudn't happen
                        Godot.GD.PushError("Couldn't add sticky chunk ref");
                        chunkRef.Chunk.AddEvent("GENFAIL stickychunkcollection");
                        chunkRef.Dispose();
                    }
                }
            }
            else if (!collection.TryAdd(res))
            {
                //chunk already exists, but another structure has added it to our collection.
                //we may need to wait on it, but shouldn't hold on to the sticky ref, since the collection won't be disposed until this structure is done with it.
                if (res.Chunk.GenerationState < ChunkGenerationState.SHAPED) needed.Add(res.Chunk);
                res.Chunk.AddEvent("FAIL stickychunkcollection");
                res.Dispose();
            }
            else
            {
                //chunk already exists, and we need to add it to the collection
                res.Chunk.AddEvent("add stickychunkcollection");
                //wait for this chunk to generate
                if (res.Chunk.GenerationState < ChunkGenerationState.SHAPED)
                {
                    if (needed.Add(res.Chunk))
                    {
                        res.Chunk.AddEvent("add needed");
                    }
                    else
                    {
                        res.Chunk.AddEvent("FAIL needed");
                        res.Dispose();
                    }
                }
            }
        }

        //wait until all chunks are done
        while (needed.Count > 0)
        {
            await Task.Delay(POLL_INTERVAL_MS);
            needed.RemoveWhere(chunk => chunk.GenerationState >= ChunkGenerationState.SHAPED);
        }
        return;
    }
}