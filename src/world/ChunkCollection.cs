using System.Collections.Concurrent;
using System.Collections.Generic;

public class ChunkCollection
{
    public event System.Action<Chunk> OnChunkOverwritten;
    private ConcurrentDictionary<ChunkCoord, Chunk> chunks = new ();

    public Block GetBlock(BlockCoord coord)
    {
        if (TryGetValue((ChunkCoord)coord, out Chunk c))
        {
            return c[Chunk.WorldToLocal(coord)];
        }
        return null;
    }


    public Chunk this[ChunkCoord index] {
        get { return chunks.TryGetValue(index, out Chunk c) ? c : null;}
        set {
            chunks.AddOrUpdate(index, (coord) => value, (coord, old) => {
                OnChunkOverwritten?.Invoke(old);
                return value;
            });
        }
    }

    public bool Contains(ChunkCoord c) => chunks.ContainsKey(c);
    public bool TryGetValue(ChunkCoord c, out Chunk chunk) => chunks.TryGetValue(c, out chunk);
    public void TryRemove(ChunkCoord c, out Chunk chunk) => chunks.TryRemove(c,out chunk);
    public void Add(Chunk c) => chunks[c.Position] = c;

    public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ChunkCoord, Chunk>> GetEnumerator() => chunks.GetEnumerator();
}