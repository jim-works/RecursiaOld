//saves annoyance
//#define NO_SAVING

using Godot;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using System.Linq;

//encode path using coordinate of chunk group:
//-1,0,0.group -> group containing chunks from -GROUP_SIZE,0,0 through -1,0,0
//xxx.dat contains info like region origin, level, structures, etc
//if the region has level < Region.ATOMIC_LOAD_LEVEL, we serialize the entire region and all of its children into the .dat file
namespace Recursia;
public partial class WorldSaver : Node
{
    [Export] public double SaveIntervalSeconds = 5;
    [Export] public double LoadIntervalSeconds = 0.100f;
    [Export] public string WorldsFolder = Path.Join(OS.GetUserDataDir(), "worlds");
    private double saveTimer;
    private double loadTimer;
    private World? world;

    private SQLInterface? sql;
    private readonly ConcurrentBag<(Chunk.StickyReference?, Action<Chunk.StickyReference?>)> callbackQueue = new();

    public override void _Ready()
    {
        world = GetParent<World>();
        string folder = Path.Join(WorldsFolder, world.Name);
        Directory.CreateDirectory(folder);
        sql = new SQLInterface(Path.Join(folder, "world.db"));
        GD.Print("World save folder is " + folder);
    }

    public override void _Process(double delta)
    {
        saveTimer += delta;
        loadTimer += delta;
        if (saveTimer > SaveIntervalSeconds)
        {
            saveTimer = 0;
            Save(world!);
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

    public async Task<Chunk.StickyReference?> LoadAndStick(ChunkCoord coord)
    {
#if NO_SAVING
        return null;
#else
        try
        {
            Chunk? c = await sql!.LoadChunk(coord);
            if (c == null) return null;
            return Chunk.StickyReference.Stick(c);
        }
        catch (Exception e)
        {
            GD.PushError(e);
            return null;
        }
#endif
    }

    public void Save(World world)
    {
#if NO_SAVING
        return;
#else
        foreach (var kvp in world.Chunks)
        {
            Save(kvp.Value);
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
        sql!.Save(c);
        c.SaveDirtyFlag = false;
#endif
    }
}