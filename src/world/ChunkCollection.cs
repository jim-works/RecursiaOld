using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public class ChunkCollection
{
    public event System.Action<Chunk>? OnChunkOverwritten;
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> chunks = new ();

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
        set {
            chunks.AddOrUpdate(index, (_) => value, (_, old) => {
                OnChunkOverwritten?.Invoke(old);
                return value;
            });
        }
    }

    public bool Contains(ChunkCoord c) => chunks.ContainsKey(c);
    public bool TryGetChunk(ChunkCoord c, [MaybeNullWhen(false)] out Chunk chunk) => chunks.TryGetValue(c, out chunk);
    public Chunk? GetChunkOrNull(ChunkCoord c) {
        TryGetChunk(c, out Chunk? chunk);
        return chunk;
    }
    public bool TryGetAndStick(ChunkCoord coord, [MaybeNullWhen(false)] out Chunk c)
    {
        //TODO: there is still a race condition between here and the lock. switch to ReaderWriterLockSlim?
        if (chunks.TryGetValue(coord, out c))
        {
            lock (c)
            {
                if (c.State == ChunkState.Unloaded)
                {
                    //c is going to be unloaded, return null.
                    return false;
                }
                c.Stick();
            }
            return true;
        }
        return false;
    }
    public void TryRemove(ChunkCoord c, [MaybeNullWhen(false)]out Chunk chunk) => chunks.TryRemove(c,out chunk);
    public void Add(Chunk c) => chunks[c.Position] = c;

    public IEnumerator<KeyValuePair<ChunkCoord, Chunk>> GetEnumerator() => chunks.GetEnumerator();
}