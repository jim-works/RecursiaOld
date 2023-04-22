//#define NO_UNLOADING

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Recursia;
public class WorldLoader
{
    private readonly World world;
    private readonly List<Node3D> chunkLoaders = new();
    private readonly int loadDistance = 16;
    private readonly HashSet<ChunkCoord> loadedChunks = new();
    private readonly List<ChunkCoord> toUnload = new();

    public WorldLoader(World world)
    {
        this.world = world;
    }

    public void UpdateChunkLoading()
    {
        loadedChunks.Clear();
        toUnload.Clear();
        foreach (Node3D loader in chunkLoaders)
        {
            ChunkCoord center = (ChunkCoord)loader.GlobalPosition;
            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int y = -loadDistance; y <= loadDistance; y++)
                {
                    for (int z = -loadDistance; z <= loadDistance; z++)
                    {
                        if (x * x + y * y + z + z > loadDistance * loadDistance) continue; //load in a sphere instead of cube
                        loadedChunks.Add(center + new ChunkCoord(x,y,z));
                    }
                }
            }
        }
#if NO_UNLOADING
#else
        foreach (var kvp in world.Chunks.GetChunkEnumerator()) {
            if (!loadedChunks.Contains(kvp.Key)) {
                toUnload.Add(kvp.Key);
            }
        }
        foreach (var c in toUnload) {
            world.UnloadChunk(c);
        }
#endif
        Task.Run(() => Parallel.ForEach(loadedChunks, async c => await world.LoadChunk(c)));
    }
    public void AddChunkLoader(Node3D loader)
    {
        chunkLoaders.Add(loader);
    }

    public void RemoveChunkLoader(Node3D loader)
    {
        chunkLoaders.Remove(loader);
    }
}