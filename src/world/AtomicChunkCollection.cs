using System.Collections.Generic;

//Chunk collection where we either write all changes at once or none at all
//Must call Commit() to apply changes. Will use World.BatchSetBlock()
public class AtomicChunkCollection
{
    private Dictionary<ChunkCoord, Chunk> chunks = new ();
    private Dictionary<BlockCoord, Block> changes = new();
    private World world;
    public AtomicChunkCollection(World world)
    {
        this.world = world;
    }

    //returns true if successful, false if destination chunk isn't present in the collection
    public bool SetBlock(BlockCoord coord, Block to)
    {
        if (TryGetValue((ChunkCoord)coord, out Chunk c))
        {
            changes.Add(coord, to);
            return true;
        }
        return false;
    }

    //returns true if successful (block placed), false if destination chunk isn't present in the collection or the dest block isn't null
    public bool SetIfNull(BlockCoord coord, Block to)
    {
        if (TryGetValue((ChunkCoord)coord, out Chunk c))
        {
            BlockCoord pos = Chunk.WorldToLocal(coord);
            if (c[pos] == null) changes.Add(coord, to); else return false;
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

    public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ChunkCoord, Chunk>> GetEnumerator() => chunks.GetEnumerator();
}