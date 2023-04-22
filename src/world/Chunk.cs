using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
namespace Recursia;
public enum ChunkState
{
    Unloaded = 0,
    Loaded = 1
}

public class Chunk : ISerializable
{
    public const int CHUNK_SIZE = 16;
    public ChunkCoord Position;
    private Block?[,,]? Blocks;
    public ChunkMesh? Mesh;
#if DEBUG
    public bool Meshed {get {
        return meshedHistory.TryPeek(out bool b) && b;
    } set {
        meshedHistory.Enqueue(value);
    }}
    private readonly ConcurrentQueue<bool> meshedHistory = new();
    private readonly ConcurrentQueue<string> eventHistory = new();
#else
    public bool Meshed {get; set;}
#endif
    public ChunkGenerationState GenerationState {get; set;}
    public ChunkState State { get; private set; }
    public List<WorldStructure> Structures = new();
    public List<PhysicsObject> PhysicsObjects = new();
    public bool SaveDirtyFlag = true;

    public Chunk(ChunkCoord chunkCoords)
    {
        Position = chunkCoords;
        Blocks = new Block[CHUNK_SIZE,CHUNK_SIZE,CHUNK_SIZE];
    }
        public Chunk(ChunkCoord chunkCoords, Block?[,,]? blocks)
    {
        Position = chunkCoords;
        Blocks = blocks;
    }

    public string GetMeshedHistory()
    {
#if DEBUG
        string res = "false ";
        foreach (var b in meshedHistory)
        {
            res += b;
        }
        return res;
#else
        return "mesh history not availble in release";
#endif
    }

    public void AddEvent(string e)
    {
#if DEBUG
        eventHistory.Enqueue(e);
#endif
    }

    public string GetEventHistory()
    {
#if DEBUG
        string res = "created\n";
        foreach (var b in eventHistory)
        {
            res += $"'{b}'\n";
        }
        return res;
#else
        return "event history not available in release";
#endif
    }

    public Block? this[BlockCoord index]
    {
        get { return this[index.X,index.Y,index.Z]; }
        set { this[index.X,index.Y,index.Z]=value; SaveDirtyFlag = true;}
    }
    public Block? this[int x, int y, int z]
    {
        get { return Blocks?[x, y, z]; }
        set
        {
            if (Blocks != null || value != null)
            {
                if (Blocks == null) Blocks = new Block[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
                Blocks[x, y, z] = value;
            }
        }
    }

    //sets chunk.state to max(Chunk.Loaded, chunk.state)
    public void Load()
    {
        State = ChunkState.Loaded;
    }
    //tries to unload, if sticky, fails
    public void Unload()
    {
        State = ChunkState.Unloaded;
    }

    public BlockCoord LocalToWorld(BlockCoord local)
    {
        return (BlockCoord)Position + local;
    }

    public static BlockCoord WorldToLocal(BlockCoord coord)
    {
        return coord % CHUNK_SIZE;
    }

    public void Serialize(BinaryWriter bw)
    {
        Position.Serialize(bw);
        //serialize blocks
        SerializationExtensions.Serialize(Blocks, bw);
        //serialize physics objects
        // bw.Write(PhysicsObjects.Count);
        // foreach (var p in PhysicsObjects)
        // {
        //     p.Serialize(bw);
        // }
    }

    public static Chunk Deserialize(BinaryReader br)
    {
        var pos = ChunkCoord.Deserialize(br);
        return new(pos, SerializationExtensions.DeserializeBlockArray(br))
        {
            //deserialize physics objects
            // int count = br.ReadInt32();
            // for (int i = 0; i < count; i++)
            // {
            //     var name = br.ReadString();
            //     var p = ObjectTypes.GetInstance<PhysicsObject>(name);
            //     c.PhysicsObjects.Add(p);
            // }
            GenerationState = ChunkGenerationState.GENERATED
        };
    }

    public override string ToString()
    {
        return $"Chunk at {Position}";
    }
}
