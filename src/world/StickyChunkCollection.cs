using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Recursia;

public class StickyChunkCollection : System.IDisposable
{
    private readonly ConcurrentDictionary<ChunkCoord, Chunk.StickyReference> stickies = new();
    private readonly Dictionary<BlockCoord, Block?> changes = new();
    private bool open = true;
    private readonly World world;

    public StickyChunkCollection(World world)
    {
        this.world = world;
    }

    public bool TryAdd(Chunk.StickyReference chunkRef) => open && stickies.TryAdd(chunkRef.Chunk.Position, chunkRef);
    public bool ContainsKey(ChunkCoord coord) => open && stickies.ContainsKey(coord);
    public void Dispose()
    {
        open = false;
        foreach (var kvp in stickies)
        {
            kvp.Value.Dispose();
        }
        stickies.Clear();
        System.GC.SuppressFinalize(this);
    }
    //returns true if successful, false if destination chunk isn't present in the collection
    public bool QueueBlock(BlockCoord coord, Block? to)
    {
        if (!open) return false;
        if (stickies.ContainsKey((ChunkCoord)coord))
        {
            changes.Add(coord, to);
            return true;
        }
        return false;
    }

    //returns true if successful (block placed), false if destination chunk isn't present in the collection or the dest block isn't null
    public bool QueueIfNull(BlockCoord coord, Block? to)
    {
        if (!open) return false;
        if (stickies.TryGetValue((ChunkCoord)coord, out Chunk.StickyReference? c))
        {
            BlockCoord pos = Chunk.WorldToLocal(coord);
            if (c.Chunk?[pos] == null) changes.Add(coord, to); else return false;
            return true;
        }
        return false;
    }

    //need to commit on main thread
    public void Commit()
    {
        if (!open) return;
        world.BatchSetBlock(setBlock => {
            foreach (var kvp in changes)
            {
                setBlock(kvp.Key, kvp.Value);
            }
        });
        changes.Clear();
    }
    public IEnumerator<KeyValuePair<ChunkCoord, Chunk.StickyReference>> GetEnumerator() => stickies.GetEnumerator();
}