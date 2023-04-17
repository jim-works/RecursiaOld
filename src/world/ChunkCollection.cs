using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public class ChunkCollection
{
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> chunks = new ();
    private readonly object _loadUnloadLock = new();
    private readonly Dictionary<BlockCoord, Block?> changes = new();
    private readonly World world;
    public ChunkCollection(World world)
    {
        this.world = world;
    }

    public Block? GetBlock(BlockCoord coord)
    {
        if (TryGetChunk((ChunkCoord)coord, out Chunk? c))
        {
            return c?[Chunk.WorldToLocal(coord)];
        }
        return null;
    }

    public Chunk this[ChunkCoord index] {
        get { return chunks[index];}
    }

    public bool Contains(ChunkCoord c) => chunks.ContainsKey(c);
    public bool TryGetChunk(ChunkCoord c, [MaybeNullWhen(false)] out Chunk chunk) => chunks.TryGetValue(c, out chunk);
    public Chunk? GetChunkOrNull(ChunkCoord c) {
        TryGetChunk(c, out Chunk? chunk);
        return chunk;
    }
    public bool TryGetAndStick(ChunkCoord coord, [MaybeNullWhen(false)] out Chunk.StickyReference c)
    {
        lock (_loadUnloadLock)
        {
            if (chunks.TryGetValue(coord, out var chunk))
            {
                // if (c.State == ChunkState.Unloaded)
                // {
                //     //c is going to be unloaded, return null.
                //     return false;
                // }
                c = Chunk.StickyReference.Stick(chunk);
                return true;
            }
            c = null;
            return false;
        }
    }

    //returns true if chunk was created
    //c is either the newly created chunk or chunk found.
    public bool GetOrCreateStickyChunk(ChunkCoord coord, out Chunk.StickyReference c)
    {
        lock (_loadUnloadLock)
        {
            //tmp to satisfy null checker, should get optimized out anyway.
            if (chunks.TryGetValue(coord, out Chunk? tmp))
            {
                c = Chunk.StickyReference.Stick(tmp);
                return false;
            }
            //need to create the chunk
            tmp = new(coord);
            c = Chunk.StickyReference.Stick(tmp);
            TryAdd(tmp);
            return true;
        }
    }

    //returns true if chunk was created
    //c is either the newly created chunk or chunk found.
    public bool GetOrCreateChunk(ChunkCoord coord, out Chunk c)
    {
        lock (_loadUnloadLock)
        {
            //tmp to satisfy null checker, should get optimized out anyway.
            if (chunks.TryGetValue(coord, out var tmp))
            {
                c = tmp;
                return false;
            }
            //need to create the chunk
            c = new(coord);
            TryAdd(c);
            return true;
        }
    }

    //returns true if successful, false if destination chunk isn't present in the collection
    public bool QueueBlock(BlockCoord coord, Block? to)
    {
        if (TryGetChunk((ChunkCoord)coord, out Chunk _))
        {
            changes.Add(coord, to);
            return true;
        }
        return false;
    }

    //returns true if successful (block placed), false if destination chunk isn't present in the collection or the dest block isn't null
    public bool QueueIfNull(BlockCoord coord, Block? to)
    {
        if (TryGetChunk((ChunkCoord)coord, out Chunk? c))
        {
            BlockCoord pos = Chunk.WorldToLocal(coord);
            if (c?[pos] == null) changes.Add(coord, to); else return false;
            return true;
        }
        return false;
    }

    //need to commit on main thread
    public void Commit()
    {
        world.BatchSetBlock(setBlock => {
            foreach (var kvp in changes)
            {
                setBlock(kvp.Key, kvp.Value);
            }
        });
        changes.Clear();
    }
    //returns true if added, false if already present in dictionary
    public bool TryAdd(Chunk c) {
        bool res = chunks.TryAdd(c.Position, c);
        if (!res) Godot.GD.PushWarning($"Adding duplicate chunk {c.Position}");
        return res;
    }

    public bool TryUnload(ChunkCoord coord, Action<Chunk> onUnload)
    {
        lock (_loadUnloadLock)
        {
            if (chunks.TryGetValue(coord, out Chunk? chunk))
            {
                if (!chunk.TryUnload()) return false; //don't unload sticky
                chunks.TryRemove(coord, out Chunk? _); //_ should be the same as chunk, since we are in the _loadUnloadLock
                onUnload(chunk);
            }
        }
        return false;
    }

    public IEnumerator<KeyValuePair<ChunkCoord, Chunk>> GetEnumerator() => chunks.GetEnumerator();
}