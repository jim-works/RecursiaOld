using Godot;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;

//encode path using coordinate of chunk group:
//-1,0,0.group -> group containing chunks from -GROUP_SIZE,0,0 through -1,0,0
//xxx.dat contains info like region origin, level, structures, etc
//if the region has level < Region.ATOMIC_LOAD_LEVEL, we serialize the entire region and all of its children into the .dat file
public partial class WorldSaver : Node
{
    [Export] public double SaveIntervalSeconds = 5;
    [Export] public string WorldsFolder = Path.Join(Godot.OS.GetUserDataDir(),"worlds");
    private double saveTimer;
    private const string FILE_EXT = "cgroup";
    private const int GROUP_MAGIC_NUMBER = 0x5348001;

    private Stack<int> pathCache = new Stack<int>();
    private List<int> targetCache = new List<int>();

    public override void _Ready()
    {
        string folder = Path.Join(WorldsFolder, World.Singleton.Name);
        Directory.CreateDirectory(folder);
        GD.Print("World save folder is " + folder);
    }

    public override void _Process(double delta)
    {
        saveTimer += delta;
        if (saveTimer > SaveIntervalSeconds)
        {
            saveTimer = 0;
            Task.Run( () => Save(World.Singleton) );
        }
    }
    //gets path to data file for region
    public string GetPath(ChunkGroupCoord group)
    {
        return Path.Join(WorldsFolder, World.Singleton.Name, $"{group.X}.{group.Y}.{group.Z}.{FILE_EXT}");
    }

    public bool PathToChunkGroupExists(ChunkGroupCoord group)
    {
        return File.Exists(GetPath(group));
    }

    public void Save(World world)
    {
        Dictionary<ChunkGroupCoord, ChunkGroup> toSave = new Dictionary<ChunkGroupCoord, ChunkGroup>();
        foreach (var kvp in world.Chunks)
        {
            if (kvp.Value.SaveDirtyFlag) toSave[kvp.Value.Group.Position] = kvp.Value.Group;
        }
        Godot.GD.Print($"Saving {toSave.Count} groups...");
        foreach(var kvp in toSave) Save(kvp.Value);
        //Parallel.ForEach(toSave, kvp => Save(kvp.Value));
    }
    public void Save(ChunkGroup g)
    {
        try {
        Godot.GD.Print("saving " + g.Position.ToString());
        using (FileStream fs = File.Open(GetPath(g.Position), FileMode.Create))
        //using (GZipStream gz = new GZipStream(fs, CompressionLevel.Fastest))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            bw.Write(GROUP_MAGIC_NUMBER);
            int writingCount = 0;
            for (byte y = 0; y < ChunkGroup.GROUP_SIZE; y++)
            {
                for (byte x = 0; x < ChunkGroup.GROUP_SIZE; x++)
                {
                    for (byte z = 0; z < ChunkGroup.GROUP_SIZE; z++)
                    {
                        if (g.Chunks[x, y, z] != null) writingCount++;
                    }
                }
            }
            bw.Write(writingCount);
            for (byte y = 0; y < ChunkGroup.GROUP_SIZE; y++)
            {
                for (byte x = 0; x < ChunkGroup.GROUP_SIZE; x++)
                {
                    for (byte z = 0; z < ChunkGroup.GROUP_SIZE; z++)
                    {
                        if (g.Chunks[x,y,z] == null) continue;
                        g.Chunks[x,y,z].SaveDirtyFlag = false;
                        g.Chunks[x,y,z].Serialize(bw);
                    }
                }
            }
        }
        Godot.GD.Print("saved " + g.Position.ToString());
        } catch (System.Exception e) {
            Godot.GD.Print("error saving: " + e);
        }
    }
    public ChunkGroup Load(ChunkGroupCoord gc)
    {
        Godot.GD.Print("loading " + gc.ToString());
        try
        {
            using (FileStream fs = File.Open(GetPath(gc), FileMode.Open))
            //using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int magnum = br.ReadInt32();
                if (magnum != GROUP_MAGIC_NUMBER) throw new System.IO.InvalidDataException("Invalid group magic number: {magnum}. Should be {GROUP_MAGIC_NUMBER}");
                ChunkGroup g = new ChunkGroup(gc);
                int readingCount = br.ReadInt32();
                Godot.GD.Print("reading " + readingCount);
                for (int i = 0; i < readingCount; i++)
                {
                    Chunk.Deserialize(br, g);
                }
                Godot.GD.Print("loaded " + gc.ToString());
                return g;
            }
        }
        catch (FileNotFoundException)
        {
            Godot.GD.Print("file not found " + gc.ToString());
            return null;
        }
        catch (System.Exception e)
        {
            Godot.GD.PrintErr("Error loading chunkgroup " + e);
            return null;
        }
    }
}