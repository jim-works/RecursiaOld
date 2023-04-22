using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;

namespace Recursia;
public class WorldGenerator
{
    public int ShapingThreads { get; } = Godot.Mathf.Max(1, Environment.ProcessorCount - 4); //seems reasonable
    public int StructuresPerChunk = 1; //seems reasonable

    //contains chunks requested from outside sources (world loading)
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> generating = new ();
    //finished chunks that are ready to be send to the mesher
    private readonly ConcurrentBag<Chunk> done = new();

    private readonly List<IChunkGenLayer> shapingLayers = new();
    private readonly List<WorldStructureProvider> structureProviders = new();
    private readonly List<Chunk> toSend = new();

    public int Seed { get; } = 1127;
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
        structureProviders.Add(new TreeStructureProvider());
        structureProviders.Add(new SkyTreeStructureProvider());
        //structureProviders.Add(new BoxStructureProvider());
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
    public bool GenerateDeferred(Chunk c)
    {
        if (c.GenerationState == ChunkGenerationState.UNGENERATED && generating.TryAdd(c.Position, c))
        {
            Task.Run(() => doGeneration(c));
            return true;
        }
        return false;
    }

    //empties finishedGenerations and sends all those chunks that are still valid to the mesher
    //a valid chunk is loaded in the world
    public void GetFinishedChunks(Action<List<Chunk>> callback)
    {
        while (!done.IsEmpty)
        {
            if (done.TryTake(out Chunk? c))
            {
                toSend.Add(c);
                c.AddEvent("sent to dest");
            }
        }
        callback(toSend);
        foreach (Chunk c in toSend)
        {
            if (generating.TryRemove(c.Position, out _))
            {
                c.AddEvent("removed from generating");
            }
        }
        toSend.Clear();
    }

    private void doGeneration(Chunk toGenerate)
    {
        try
        {
            toGenerate.AddEvent("do generation");
            ShapeChunk(toGenerate);
            toGenerate.AddEvent("shaped");
            GenerateStructures(toGenerate);
            toGenerate.AddEvent("structured");
            done.Add(toGenerate);
            toGenerate.AddEvent("sent to done");
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

    public void GenerateStructures(Chunk chunk)
    {
        chunk.GenerationState = ChunkGenerationState.PLACING_STRUCTURES;
        foreach (var provider in structureProviders)
        {
            for (int i = 0; i < provider.RollsPerChunk; i++)
            {
                int dx = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dy = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                int dz = (int)(Godot.GD.Randf() * Chunk.CHUNK_SIZE);
                BlockCoord origin = new(System.Math.Min(dx, Chunk.CHUNK_SIZE - 1), System.Math.Min(dy, Chunk.CHUNK_SIZE - 1), System.Math.Min(dz, Chunk.CHUNK_SIZE - 1));
                if (!provider.SuitableLocation(chunk, origin)) continue;
                WorldStructure? result = provider.PlaceStructure(world.Chunks, chunk.LocalToWorld(origin));
                if (result != null && provider.Record)
                {
                    chunk.Structures.Add(result);
                }
            }
        }
        chunk.GenerationState = ChunkGenerationState.GENERATED;
    }
}