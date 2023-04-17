using System.IO;
using System.Collections.Generic;
using System;
namespace Recursia;
public enum ChunkState
{
    Unloaded = 0,
    Loaded = 1,
    //Sticky chunks will not be unloaded. They are used for structures in worldgen that require neighboring chunks to be loaded
    Sticky = 2,
}

public partial class Chunk : ISerializable
{
    public const int CHUNK_SIZE = 16;
    public ChunkCoord Position;
    private Block?[,,]? Blocks;
    public ChunkMesh? Mesh;
    public bool Meshed {get {
        return meshedHistory.Count != 0 && meshedHistory.Peek();
    } set {
        meshedHistory.Enqueue(value);
    }}
    public ChunkGenerationState GenerationState {get; set;}
    public ChunkState State { get; private set; }
    public List<WorldStructure> Structures = new();
    public List<PhysicsObject> PhysicsObjects = new();
    public bool SaveDirtyFlag = true;
    public int stickyCount;
    private readonly object _stickyLock = new();
    private readonly Queue<bool> meshedHistory = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> eventHistory = new();

    public Chunk(ChunkCoord chunkCoords)
    {
        Position = chunkCoords;
        Blocks = new Block[CHUNK_SIZE,CHUNK_SIZE,CHUNK_SIZE];
    }

    public string GetMeshedHistory()
    {
        string res = "false ";
        foreach (var b in meshedHistory)
        {
            res += b;
        }
        return res;
    }

    public void AddEvent(string e)
    {
        eventHistory.Enqueue(e);
    }

    public string GetEventHistory()
    {
        string res = "created\n";
        foreach (var b in eventHistory)
        {
            res += $"'{b}'\n";
        }
        return res;
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
        lock (_stickyLock)
        {
            State = (ChunkState)System.Math.Max((int)ChunkState.Loaded, (int)State);
        }
    }
    //tries to unload, if sticky, fails
    public bool TryUnload()
    {
        lock (_stickyLock)
        {
            if (stickyCount > 0) return false;
            State = ChunkState.Unloaded;
            return true;
        }
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
        if (Blocks == null)
        {
            //this case doesn't need to exist, but should be faster than the other
            bw.Write(CHUNK_SIZE*CHUNK_SIZE*CHUNK_SIZE);
            bw.Write(0);
        }
        else
        {
            Block? curr = Blocks[0, 0, 0];
            int run = 0;
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        Block? b = Blocks[x, y, z];
                        if (curr == b)
                        {
                            run++;
                            continue;
                        }
                        bw.Write(run);
                        if (curr == null)
                        {
                            bw.Write(0);
                        }
                        else
                        {
                            bw.Write(1);
                            bw.Write(curr.Name);
                            curr.Serialize(bw);
                        }
                        run = 1;
                        curr = b;
                    }
                }
            }

            bw.Write(run);
            if (curr == null)
            {
                bw.Write(0);
            }
            else
            {
                bw.Write(1);
                bw.Write(curr.Name);
                curr.Serialize(bw);
            }
        }
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
        Chunk c = new(pos);
        int run = 0;
        //deserialize blocks
        Block? read = null;
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    if (run == 0)
                    {
                        run = br.ReadInt32();
                        bool nullBlock = br.ReadInt32() == 0;
                        if (nullBlock)
                        {
                            read = null;
                        }
                        else
                        {
                            string blockName = br.ReadString();
                            try
                            {
                                BlockTypes.TryGet(blockName, out read);
                                read!.Deserialize(br);
                            }
                            catch (Exception e)
                            {
                                Godot.GD.PushError($"Error deserializing block {blockName} at {pos} {x} {y} {z}: {e}");
                                read = null;
                            }
                        }
                    }
                    c[x, y, z] = read;
                    run--;
                }
            }
        }
        //deserialize physics objects
        // int count = br.ReadInt32();
        // for (int i = 0; i < count; i++)
        // {
        //     var name = br.ReadString();
        //     var p = ObjectTypes.GetInstance<PhysicsObject>(name);
        //     c.PhysicsObjects.Add(p);
        // }
        c.GenerationState = ChunkGenerationState.GENERATED;
        return c;
    }

    public override string ToString()
    {
        return $"Chunk at {Position}";
    }
}
