//saves annoyance
//#define NO_SAVING

using Godot;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using System.Linq;
namespace Recursia;
public partial class WorldSaver : Node
{
    public enum DataTableIDs
    {
        Terrain=0,
        TerrainBuffers=1,
        PhysicsObjects=2,
    }
    [Export] public double SaveIntervalSeconds = 5;
    [Export] public double LoadIntervalSeconds = 0.100f;
    [Export] public string WorldsFolder = Path.Join(OS.GetUserDataDir(), "worlds");
    private double saveTimer;
    private double loadTimer;
    private World? world;

    private SQLInterface? sql;
    private readonly ConcurrentBag<(Chunk, Action<Chunk>)> callbackQueue = new();

    public override void _Ready()
    {
        world = GetParent<World>();
        string folder = Path.Join(WorldsFolder, world.Name);
        Directory.CreateDirectory(folder);
        sql = new SQLInterface(Path.Join(folder, "world.db"), (br, name) => {
            Player p = PhysicsObject.Deserialize<Player>(world, br)!;
            p.Name = name;
            return p;
        });
        sql.RegisterDataTable("terrain", (int)DataTableIDs.Terrain, br => new Chunk(br));
        sql.RegisterDataTable("terrainBuffers", (int)DataTableIDs.TerrainBuffers, br => new ChunkBuffer(br));
        sql.RegisterDataTable("physicsObjects", (int)DataTableIDs.PhysicsObjects, br => PhysicsObject.DeserializeArray(world, br));
        GD.Print("World save folder is " + folder);

        world.Chunks.OnChunkUnload += (c,b) => {
            Save(c);
            if (b != null) Save(b);
        };
        world.Entities.OnChunkUnload += Save;
        sql.BeginPolling();
    }

    public override void _Process(double delta)
    {
        saveTimer += delta;
        loadTimer += delta;
        if (saveTimer > SaveIntervalSeconds)
        {
            saveTimer = 0;
            GD.Print("Saving...");
            Task.Run(() => {
                Save(world!);
                GD.Print("Saved the world.");
            });
        }
        if (loadTimer > LoadIntervalSeconds)
        {
            loadTimer = 0;
            while (callbackQueue.TryTake(out var pair))
            {
                pair.Item2?.Invoke(pair.Item1);
            }
        }
    }

    public override void _ExitTree()
    {
        Save(world!);
        sql!.Close();
    }

    public async Task<T?> Load<T>(ChunkCoord coord, int tableId)
    {
#if NO_SAVING
        return null;
#else
        try
        {
            return (T?)await sql!.LoadData(tableId, coord);
        }
        catch (Exception e)
        {
            GD.PushError(e);
            return default;
        }
#endif
    }
    public async Task<Player?> LoadPlayer(string name)
    {
#if NO_SAVING
        return null;
#else
        try
        {
            return await sql!.LoadPlayer(name);
        }
        catch (Exception e)
        {
            GD.PushError(e);
            return default;
        }
#endif
    }

    public void Save(World world)
    {
#if NO_SAVING
        return;
#else
        foreach (var kvp in world.Chunks.GetChunkEnumerator())
        {
            Save(kvp.Value);
        }
        foreach (var kvp in world.Chunks.GetBufferEnumerator())
        {
            Save(kvp.Value);
        }
        foreach (var p in world.Entities.Players)
        {
            Save(p.Value);
        }
        //sql!.SavePlayers(world.Entities.Players);
#endif
    }
    public void Save(Chunk c)
    {
#if NO_SAVING
        return;
#else
        if (!c.SaveDirtyFlag) return;
        sql!.Save((int)DataTableIDs.Terrain, c.Position, c);
        c.SaveDirtyFlag = false;
#endif
    }
//TODO: need to combine buffer if one already exists on disk (or design so that this can't happen)
        public void Save(ChunkBuffer c)
    {
#if NO_SAVING
        return;
#else
        if (!c.SaveDirtyFlag) return;
        sql!.Save((int)DataTableIDs.TerrainBuffers, c.Position, c);
        c.SaveDirtyFlag = false;
#endif
    }
    public void Save(Player p)
    {
#if NO_SAVING
        return;
#else
        sql!.Save(p);
#endif
    }
    public void Save(ChunkCoord coord, List<PhysicsObject> objs)
    {
        sql!.Save((int)DataTableIDs.PhysicsObjects, coord, new SerializableList<PhysicsObject>{List=objs.Where(obj => obj is not Player && !obj.NoSerialize()).ToList()});
    }
}