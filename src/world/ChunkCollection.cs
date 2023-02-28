using System.Collections.Generic;

public class ChunkCollection
{
    private Dictionary<ChunkCoord, Chunk> chunks = new Dictionary<ChunkCoord, Chunk>();

    //returns true if successful, false if destination chunk isn't present in the collection
    //doesn't trigger any events like meshing
    public bool SetBlock(BlockCoord coord, Block to)
    {
        if (TryGetValue((ChunkCoord)coord, out Chunk c))
        {
            c[Chunk.WorldToLocal(coord)] = to;
            return true;
        }
        return false;
    }

    //returns true if successful (block placed), false if destination chunk isn't present in the collection or the dest block isn't null
    //doesn't trigger any events like meshing
    public bool SetIfNull(BlockCoord coord, Block to)
    {
        if (TryGetValue((ChunkCoord)coord, out Chunk c))
        {
            BlockCoord pos = Chunk.WorldToLocal(coord);
            if (c[pos] == null) c[pos] = to; else return false;
            return true;
        }
        return false;
    }

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
        set {chunks[index] = value;}
    }

    public bool Contains(ChunkCoord c) => chunks.ContainsKey(c);
    public bool TryGetValue(ChunkCoord c, out Chunk chunk) => chunks.TryGetValue(c, out chunk);
    public void Remove(ChunkCoord c) => chunks.Remove(c);
    public void Add(Chunk c) => chunks[c.Position] = c;

    public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ChunkCoord, Chunk>> GetEnumerator() => chunks.GetEnumerator();
}