using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public class ChunkCollection
{
    public event Action<Chunk>? OnChunkLoad;
    public event Action<Chunk>? OnChunkUpdate;
    public event Action<Chunk, ChunkBuffer?>? OnChunkUnload;
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> chunks = new();
    //blocks to be placed when the chunk at chunkcoord generates
    private readonly ConcurrentDictionary<ChunkCoord, ChunkBuffer> buffers = new();

    public Block? GetBlock(BlockCoord coord)
    {
        ChunkCoord cc = (ChunkCoord)coord;
        if (TryGetChunk(cc, out Chunk? c))
        {
            return c?[Chunk.WorldToLocal(coord)];
        }
        else if (buffers.TryGetValue(cc, out ChunkBuffer? buf))
        {
            return buf?[Chunk.WorldToLocal(coord)];
        }
        return null;
    }
    //returns the chunk if chunk is in collection, null otherwise
    //if chunk is not in the collection, sets the block in the chunk's chunkbuffer. Change will persist when chunk is generated/loaded again.
    //does not trigger OnChunkUpdate
    public Chunk? SetBlockNoUpdate(BlockCoord coord, Block? to)
    {
        ChunkCoord cc = (ChunkCoord)coord;
        if (TryGetChunk(cc, out Chunk? chunk))
        {
            chunk[Chunk.WorldToLocal(coord)] = to;
        }
        else
        {
            buffers.AddOrUpdate(cc, c => {
                ChunkBuffer buf = new(c);
                buf[Chunk.WorldToLocal(coord)] = to;
                return buf;
            },
            (_, buf) => {
                buf[Chunk.WorldToLocal(coord)] = to;
                return buf;
            });
        }
        return chunk;
    }
    //returns the chunk if chunk is in collection, null otherwise
    //if chunk is not in the collection, sets the block in the chunk's chunkbuffer. Change will persist when chunk is generated/loaded again.
    //triggers OnChunkUpdate
    public Chunk? SetBlock(BlockCoord coord, Block? to)
    {
        if (SetBlockNoUpdate(coord, to) is Chunk c)
        {
            OnChunkUpdate?.Invoke(c);
            return c;
        }
        return null;
    }
    //only updates chunks at the end of the batch. much more efficient for large batches than many SetBlock() calls.
    //call batch(coord, block) to set the blocks
    //will not create new chunks
    public void BatchSetBlock(Action<Action<BlockCoord, Block?>> batch) {
        List<Chunk> chunksToUpdate = new();
        batch((coords, block) => {
            if (SetBlockNoUpdate(coords, block) is Chunk c && !chunksToUpdate.Contains(c))
            {
                chunksToUpdate.Add(c);
            }
        });
        foreach (Chunk c in chunksToUpdate) {
            if (c.State == ChunkState.Loaded) OnChunkUpdate?.Invoke(c);
            c.AddEvent("batch set block");
        }
    }

    public Chunk this[ChunkCoord index]
    {
        get { return chunks[index]; }
    }

    public bool Contains(ChunkCoord c) => chunks.ContainsKey(c);
    public bool TryGetChunk(ChunkCoord c, [MaybeNullWhen(false)] out Chunk chunk) => chunks.TryGetValue(c, out chunk);
    public Chunk? GetChunkOrNull(ChunkCoord c)
    {
        TryGetChunk(c, out Chunk? chunk);
        return chunk;
    }

    //returns true if chunk was created
    //c is either the newly created chunk or chunk found.
    //must add c to collection when ready, beware that it may be unloaded whenever after its added
    public Chunk GetOrCreateChunk(ChunkCoord coord)
    {
        if (chunks.TryGetValue(coord, out Chunk? c)) return c;
        return new(coord);
    }
    //tries to add the chunk to the collection and load it
    //returns true if added, false if already present in dictionary. Will not load if already present.
    public bool TryLoad(Chunk c)
    {
        if(!chunks.TryAdd(c.Position,c))
        {
            return false;
        }
        if (buffers.TryRemove(c.Position, out ChunkBuffer? b))
        {
            b.AddToChunk(c);
        }
        c.Load();
        OnChunkLoad?.Invoke(c);
        return true;
    }

    public bool TryUnload(ChunkCoord coord)
    {
        if (chunks.TryRemove(coord, out Chunk? chunk))
        {
            chunk.Unload();
            buffers.TryRemove(coord, out ChunkBuffer? buf);
            OnChunkUnload?.Invoke(chunk, buf);
        }
        return false;
    }
    public IEnumerable<KeyValuePair<ChunkCoord, ChunkBuffer>> GetBufferEnumerator() => buffers;
    public IEnumerable<KeyValuePair<ChunkCoord, Chunk>> GetChunkEnumerator() => chunks;
}