using System.Threading;
using System.Collections.Generic;

public class WorldGenerator
{
    private const int POLL_INTERVAL = 10;
    public int WorldgenThreads {get; private set;} = Godot.Mathf.Max(1,System.Environment.ProcessorCount-1); //seems reasonable
        
    private volatile HashSet<Chunk>[] toGenerate;
    private volatile List<Chunk> generated = new List<Chunk>();
    private Thread[] generationThreads;

    private List<IChunkGenLayer> chunkGenLayers = new List<IChunkGenLayer>();
    private List<StructureProvider> structureProviders = new List<StructureProvider>();

    public WorldGenerator()
    {
        //initialize worldgen threads
        Godot.GD.Print($"Starting world generator with {WorldgenThreads} threads!");
        chunkGenLayers.Add(new HeightmapLayer());
        toGenerate = new HashSet<Chunk>[WorldgenThreads];
        generationThreads = new Thread[WorldgenThreads];
        for (int i = 0; i < WorldgenThreads; i++)
        {
            int id = i; //copy to avoid race condition
            toGenerate[i] = new HashSet<Chunk>();
            generationThreads[i] = new Thread(() => generationLoop(id));
            generationThreads[i].Start();
        }
    }
    //multithreaded world generation
    //I was chasing a race condition using tasks, so I switched to my own thread implementation.
    //Turns out I'm dumb and missed the race condition. But I'm too lazy to switch back to tasks
    //So we have this implementation instead :)
    public void GenerateDeferred(Chunk c)
    {
        //positive mod of chunk's position to find which thread to assign it to
        //this gives a disjoint set of chunks to each thread, so we don't have to worry about them modifying the same chunk.
        int tid = (c.Position.x+c.Position.y+c.Position.z) % WorldgenThreads;
        tid = tid < 0 ? tid + WorldgenThreads : tid;
        lock (toGenerate)
        {
            toGenerate[tid].Add(c);
        }

    }
    //run on generationThread
    private void generationLoop(int id)
    {
        List<Chunk> myQueue = new List<Chunk>();
        
        while (true)
        {
            Thread.Sleep(POLL_INTERVAL);
            lock (toGenerate)
            {
                foreach(Chunk c in toGenerate[id]) myQueue.Add(c);
                toGenerate[id].Clear();
            }
            foreach (var c in myQueue)
            {
                GenerateChunk(World.Singleton, c);
            }
            lock(generated)
            {
                foreach (var c in myQueue) generated.Add(c);
            }
            myQueue.Clear();
        }
    }
    //empties finishedGenerations and sends all those chunks that are still valid to the mesher
    //a valid chunk is loaded in the world
    public void SendToMesher()
    {
        lock (generated)
        {
            foreach(Chunk c in generated)
            {
                Mesher.Singleton.MeshDeferred(c);
            }
            generated.Clear();
        }
        
    }
    public void GenerateChunk(World world, Chunk chunk) {
        foreach (var genLayer in chunkGenLayers)
        {
            genLayer.GenerateChunk(world,chunk);
        }
    }
}