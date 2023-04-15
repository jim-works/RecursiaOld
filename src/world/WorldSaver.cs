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
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> saveQueue = new();
    private readonly ConcurrentDictionary<ChunkCoord, Action<Chunk?>> loadQueue = new();
    private readonly List<(Chunk?, Action<Chunk?>)> callbackQueue = new();

    public override void _Ready()
    {
        world = GetParent<World>();
        string folder = Path.Join(WorldsFolder, world.Name);
        Directory.CreateDirectory(folder);
        sql = new SQLInterface(Path.Join(folder, "world.db"));
        GD.Print("World save folder is " + folder);
        Task.Run(() =>
        {
            while (true)
            {
                Task.Delay(TimeSpan.FromSeconds(LoadIntervalSeconds)).Wait();
                emptyLoadQueue();
            }
        });
        Task.Run(() =>
        {
            while (true)
            {
                Task.Delay(TimeSpan.FromSeconds(SaveIntervalSeconds)).Wait();
                emptySaveQueue();
                //GC.Collect();
            }
        });
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
            lock (callbackQueue)
            {
                foreach (var (chunk, callback) in callbackQueue)
                {
                    callback?.Invoke(chunk);
                }
                callbackQueue.Clear();
            }
        }
    }

    public override void _ExitTree()
    {
        Save(world!);
        emptySaveQueue();
        sql!.Close();
    }

    public void LoadAndStick(ChunkCoord coord, Action<Chunk?> callback)
    {
#if NO_SAVING
        callback(null);
#else
        loadQueue[coord] = callback;
#endif
    }

    public void Save(World world)
    {
#if NO_SAVING
        return;
#else
        foreach (var kvp in world.Chunks)
        {
            if (!kvp.Value.SaveDirtyFlag) continue;
            saveQueue[kvp.Key] = kvp.Value;
            kvp.Value.SaveDirtyFlag = false;
        }
        sql!.SavePlayers(world.Entities.Players);
#endif
    }
    public void Save(Chunk c)
    {
#if NO_SAVING
        return;
#else
        if (!c.SaveDirtyFlag) return;
        saveQueue[c.Position] = c;
#endif
    }
    private void emptySaveQueue()
    {
#if NO_SAVING
        return;
#else
        GD.Print($"Saving {saveQueue.Count} groups...");
        //TODO THIS ISN"T SAFE FOR SOME REASON
        sql!.SaveChunks(() => {
            foreach (var kvp in saveQueue.ToArray())
                if (saveQueue.TryRemove(kvp.Key, out Chunk? val)) return val;
            return null;
        });
        saveQueue.Clear();
        GD.Print("Saved");
#endif
    }
    private void emptyLoadQueue()
    {
        //copy to avoid race
        try {
        KeyValuePair<ChunkCoord, Action<Chunk?>>[] coords = loadQueue.ToArray();
        List<Chunk?> results = new();
        sql!.LoadChunks(coords.Select(kvp => kvp.Key), results);
        for (int i = 0; i < coords.Length; i++)
        {
            Chunk? c = results[i];
            if (c != null)
            {
                c.SaveDirtyFlag = false;
                c.Stick();
            }
            loadQueue.TryRemove(coords[i].Key, out _);
        }
        lock (callbackQueue)
        {
            for (int i = 0; i < coords.Length; i++)
            {
                callbackQueue.Add((results[i], coords[i].Value));
            }
        }
        } catch (Exception e) {
            GD.Print(e);
        }
    }
}