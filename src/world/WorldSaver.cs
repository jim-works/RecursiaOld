using Godot;
using System.IO;
using System.Collections.Generic;

//We use the encode the path to the root in the filename, down to a minimum SERIALIZATION_LEVEL
//Suppose the root has children at index 0, 2, 4. Child 0 has a child at index 1
//directory structure will be:
//  -   worldinfo.dat
//  -   -1.reg
//  -   0.reg
//  -   01.reg
//  -   2.reg
//  -   4.reg
//xxx.dat contains info like region origin, level, structures, etc
//if the region has level < SERIALIZATION_LEVEL, we serialize the entire region and all of its children into the .dat file
public partial class WorldSaver : Node
{
    [Export] public double SaveIntervalSeconds = 5;
    [Export] public string WorldsFolder = "worlds/";
    private double saveTimer;
    private const int SERIALIZATION_LEVEL = 5; //what level we should stop recursing the octree when saving
    private const string FILE_EXT = ".reg";

    private Stack<int> pathCache = new Stack<int>();

    public override void _Process(double delta)
    {
        saveTimer += delta;
        if (saveTimer > SaveIntervalSeconds) {
            saveTimer = 0;
            Save(World.Singleton);
        }
    }

    public void Save(World world)
    {
        string folder = Path.Join(WorldsFolder,world.Name);
        Directory.CreateDirectory(folder);
        Save(world.Octree.Root);
        GD.Print("Saved the world!");
    }
    //gets path to data file for region
    public string GetPath(Region region) {
        pathCache.Clear();
        region.AddPathToRoot(pathCache);
        if (pathCache.Count == 0) return Path.Join(WorldsFolder,World.Singleton.Name,"-1" + FILE_EXT); //root node
        string res = FILE_EXT;
        while (pathCache.Count != 0) {
            res = pathCache.Pop() + res;
        }
        return Path.Join(WorldsFolder,World.Singleton.Name,res);
    }
    //gets path to data file for region's child
    public string GetChildPath(Region region, int childIdx) {
        pathCache.Clear();
        pathCache.Push(childIdx);
        region.AddPathToRoot(pathCache);
        if (pathCache.Count == 0) return Path.Join(WorldsFolder,World.Singleton.Name,"-1" + FILE_EXT); //root node
        string res = FILE_EXT;
        while (pathCache.Count != 0) {
            res = pathCache.Pop() + res;
        }
        return Path.Join(WorldsFolder,World.Singleton.Name,res);
    }
    //recursively save region and all children according to filestructure above
    private void Save(Region region)
    {
        if (region.Level <= SERIALIZATION_LEVEL) {
            writeEntireRegion(region, GetPath(region));
            return;
        }
        writeRegionInfo(region, GetPath(region));
        if (region.Children != null) {
            for (int i = 0; i < region.Children.Length; i++)
            {
                Region c = region.Children[i];
                if (c != null) Save(c);
            }
        }
    }
    //writes the entire region recursively to one file
    private void writeEntireRegion(Region r, string fileName)
    {
        using (BinaryWriter bw = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))) {
            r.SerializeRecursive(bw);
        }
    }
    //writes the surface level region data, then creates directories for children if needed
    private void writeRegionInfo(Region r, string filepath)
    {
        //write data file
        using (BinaryWriter bw = new BinaryWriter(File.Open(filepath, FileMode.OpenOrCreate))) {
            r.SerializeNonRecursive(bw);
        }
    }

    //recursively loads the region stored at startpath and its children
    public Region LoadRec(string startpath) {
        (Region root, int[] children) = LoadRegion(startpath);
        if (children == null) {
            return root;
        }
        GD.Print($"Loaded {startpath}.dat: {root}");
        //load children
        foreach (int i in children) {
            Region child = LoadRec(GetChildPath(root, i));
            child.Parent = root;
            root.Children[i] = child;
        }
        return root;
    }
    //returns (region, index of children to load (null if none))
    //differentiates between region summaries and full recursive files
    //doesn't recurse
    public (Region, int[] childIdxs) LoadRegion(string filename) {
        using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open))) {
            bool fullySerialized = br.ReadByte() == 1;
            if (fullySerialized) {
                return (Region.DeserializeRecursive(br), null);
            } else {
                return Region.DeserializeNonRecursive(br);
            }
        }
    }
}