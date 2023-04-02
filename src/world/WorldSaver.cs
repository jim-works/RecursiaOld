//saves annoyance
#define NO_SAVING

using Godot;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using System.Linq;



//encode path using coordinate of chunk group:
//-1,0,0.group -> group containing chunks from -GROUP_SIZE,0,0 through -1,0,0
//xxx.dat contains info like region origin, level, structures, etc
//if the region has level < Region.ATOMIC_LOAD_LEVEL, we serialize the entire region and all of its children into the .dat file
public partial class WorldSaver : Node
{
    [Export] public double SaveIntervalSeconds = 5;
    [Export] public double LoadIntervalSeconds = 0.100f;
    [Export] public string WorldsFolder = Path.Join(Godot.OS.GetUserDataDir(), "worlds");
    private double saveTimer;
    private double loadTimer;
    private const string DB_FILE_EXT = "db";

    private SQLInterface sql;
    private ConcurrentDictionary<ChunkCoord, Chunk> saveQueue = new();
    private ConcurrentDictionary<ChunkCoord, Action<Chunk>> loadQueue = new();
    private volatile List<(Chunk, Action<Chunk>)> callbackQueue = new();

    public override void _Ready()
    {
        string folder = Path.Join(WorldsFolder, World.Singleton.Name);
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
            Save(World.Singleton);

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
        Save(World.Singleton);
        emptySaveQueue();
        sql.Close();
    }

    public void Load(ChunkCoord coord, Action<Chunk> callback)
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
        Godot.GD.Print($"Saving {saveQueue.Count} groups...");
        //TODO THIS IS NOT SAFE
        sql.SaveChunks(saveQueue.Values);
        saveQueue.Clear();
        Godot.GD.Print("Saved");
#endif
    }
    private void emptyLoadQueue()
    {
        //copy to avoid race
        try {
        KeyValuePair<ChunkCoord, Action<Chunk>>[] coords = loadQueue.ToArray();
        List<Chunk> results = new List<Chunk>();
        sql.LoadChunks(coords.Select(kvp => kvp.Key), results);
        for (int i = 0; i < coords.Length; i++)
        {
            Chunk c = results[i];
            if (c != null)
            {
                c.SaveDirtyFlag = false;
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
            Godot.GD.Print(e);
        }
    }
} 