//#define NO_UNLOADING

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Recursia;
public class WorldLoader
{
    private readonly World world;
    private readonly List<Node3D> chunkLoaders = new();
    private readonly int loadDistance = 10;
    private readonly HashSet<ChunkCoord> loadedChunks = new();
    private readonly List<ChunkCoord> toUnload = new();
    private ConcurrentBag<(TaskCompletionSource, List<ChunkCoord>)> mainThreadQueue = new();

    private bool running;

    public WorldLoader(World world)
    {
        this.world = world;
    }

    public void UpdateChunkLoading()
    {
        lock(loadedChunks)
        {
            if (running) return;
        }
        Task.Run(async ()=> await doLoading());
    }
    public void Process()
    {
        doMainThreadUnloading();
    }
    public void AddChunkLoader(Node3D loader)
    {
        chunkLoaders.Add(loader);
    }

    public void RemoveChunkLoader(Node3D loader)
    {
        chunkLoaders.Remove(loader);
    }
    private void doMainThreadUnloading()
    {
        while (mainThreadQueue.TryTake(out var r))
        {
            (TaskCompletionSource tcs, List<ChunkCoord> toUnload) = r;
            foreach (ChunkCoord c in toUnload)
            {
                world.Entities.Unload(c);
            }
            tcs.SetResult();
        }
    }
    private async Task unloadOnMainThread(List<ChunkCoord> toUnload)
    {
        TaskCompletionSource tcs = new();
        mainThreadQueue.Add((tcs,toUnload));
        await tcs.Task;
    }

    private async Task doLoading()
    {
        lock(loadedChunks)
        {
            running = true;
        }
        loadedChunks.Clear();
        toUnload.Clear();
        List<Node3D> loadersToRemove = new();
        foreach (Node3D loader in chunkLoaders)
        {
            if (!GodotObject.IsInstanceValid(loader) || !loader.IsInsideTree())
            {
                loadersToRemove.Add(loader);
                continue;
            }
            ChunkCoord center = (ChunkCoord)loader.GlobalPosition;
            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int y = -loadDistance; y <= loadDistance; y++)
                {
                    for (int z = -loadDistance; z <= loadDistance; z++)
                    {
                        if (x * x + y * y + z + z > loadDistance * loadDistance) continue; //load in a sphere instead of cube
                        loadedChunks.Add(center + new ChunkCoord(x, y, z));
                    }
                }
            }
        }
        foreach (var loader in loadersToRemove)
        {
            chunkLoaders.Remove(loader);
        }
#if NO_UNLOADING
#else
        foreach (var kvp in world.Chunks.GetChunkEnumerator())
        {
            if (!loadedChunks.Contains(kvp.Key))
            {
                toUnload.Add(kvp.Key);
            }
        }
        await unloadOnMainThread(toUnload);
        foreach (var c in toUnload)
        {
            world.Chunks.TryUnload(c);
        }
#endif
        Parallel.ForEach(loadedChunks, async c => await world.LoadChunk(c));
        lock(loadedChunks)
        {
            running = false;
        }
    }
}