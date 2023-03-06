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
    private List<int> targetCache = new List<int>();

    public override void _Process(double delta)
    {
        saveTimer += delta;
        if (saveTimer > SaveIntervalSeconds)
        {
            saveTimer = 0;
            Save(World.Singleton);
        }
    }

    public void Save(World world)
    {
        string folder = Path.Join(WorldsFolder, world.Name);
        Directory.CreateDirectory(folder);
        Save(world.Octree.Root);
        GD.Print("Saved the world!");
    }
    //gets path to data file for region
    //if rootLevelCutoff >= 0, cut off the returned path so it corresponds to a file (respecting SERIALIZQTION_LEVEL)
    public string GetPath(Region region, int rootLevelCutoff=-1)
    {
        pathCache.Clear();
        region?.AddPathToRoot(pathCache);
        if (pathCache.Count == 0) return Path.Join(WorldsFolder, World.Singleton.Name, "-1" + FILE_EXT); //root node
        int maxPathLength = World.Singleton.Octree.Root.Level - SERIALIZATION_LEVEL; //all chunks below level SERIALIZATION_LEVEL are placed in the same file as their parent, so we need to chop the path
        if (rootLevelCutoff >= 0 && maxPathLength == 0) return Path.Join(WorldsFolder, World.Singleton.Name, "-1" + FILE_EXT); //child of root node
        string res = FILE_EXT;
        
        int length = 0;
        while (pathCache.Count != 0)
        {
            length++;
            if (rootLevelCutoff >= 0 && length > maxPathLength) break;
            res = pathCache.Pop() + res;
        }
        return Path.Join(WorldsFolder, World.Singleton.Name, res);
    }
    //gets path to data file for region's child
    //if rootLevelCutoff >= 0, cut off the returned path so it corresponds to a file (respecting SERIALIZQTION_LEVEL)
    public string GetChildPath(Region region, int childIdx, int rootLevelCutoff=-1)
    {
        pathCache.Clear();
        pathCache.Push(childIdx);
        region?.AddPathToRoot(pathCache);
        if (pathCache.Count == 0) return Path.Join(WorldsFolder, World.Singleton.Name, "-1" + FILE_EXT); //root node
        int maxPathLength = rootLevelCutoff - SERIALIZATION_LEVEL; //all chunks below level SERIALIZATION_LEVEL are placed in the same file as their parent, so we need to chop the path
        if (rootLevelCutoff >= 0 && maxPathLength == 0) return Path.Join(WorldsFolder, World.Singleton.Name, "-1" + FILE_EXT); //child of root node
        string res = FILE_EXT;
        int length = 0;
        while (pathCache.Count != 0)
        {
            length++;
            if (rootLevelCutoff >= 0 && length > maxPathLength) break;
            res = pathCache.Pop() + res;
        }
        return Path.Join(WorldsFolder, World.Singleton.Name, res);
    }
    //recursively save region and all children according to filestructure above
    private void Save(Region region)
    {
        if (!region.BlockDirtyFlag) return; //only need to save changed regions
        Godot.GD.Print("Saved " + region);
        if (region.Level <= SERIALIZATION_LEVEL)
        {
            writeEntireRegion(region, GetPath(region));
            return;
        }
        writeRegionInfo(region, GetPath(region));
        if (region.Children != null)
        {
            for (int i = 0; i < region.Children.Length; i++)
            {
                Region c = region.Children[i];
                if (c != null) Save(c);
            }
        }
        region.UnsetBlockDirty();
    }
    //writes the entire region recursively to one file
    private void writeEntireRegion(Region r, string fileName)
    {
        using (BinaryWriter bw = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate)))
        {
            r.SerializeRecursive(bw);
        }
    }
    //writes the surface level region data, then creates directories for children if needed
    private void writeRegionInfo(Region r, string filepath)
    {
        //write data file
        using (BinaryWriter bw = new BinaryWriter(File.Open(filepath, FileMode.OpenOrCreate)))
        {
            r.SerializeNonRecursive(bw);
        }
    }

    //recursively loads the region stored at startpath and its children
    public Region LoadRec(string startpath)
    {
        Region root = LoadRegion(startpath);
        GD.Print($"Loaded {startpath}.dat: {root}");
        if (root.Children == null)
        {
            return root;
        }
        //load children
        foreach (int i in root.SavedChildIndicies)
        {
            Region child = LoadRec(GetChildPath(root, i));
            child.Parent = root;
            root.Children[i] = child;
        }
        return root;
    }
    //differentiates between region summaries and full recursive files
    //doesn't recurse
    public Region LoadRegion(string filename)
    {
        using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
        {
            bool fullySerialized = br.ReadByte() == 1;
            if (fullySerialized)
            {
                return Region.DeserializeRecursive(br);
            }
            else
            {
                return Region.DeserializeNonRecursive(br).Item1;
            }
        }
    }

    //force loads only the summary of the region, even if it's a recursive file
    private Region forceLoadNonRecursive(string filename)
    {
        using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
        {
            br.ReadByte();
            return Region.DeserializeNonRecursive(br).Item1;
        }
    }

    //extralevels: returns a region containing the block at coords at a specified level.
    //              levels=1 will return a  (2x2x2 cube of chunks)
    //  Usually, we load many chunks at once, so this should reduce the amount of disk reads for nearby chunks
    //if the chunk is not saved to disk, we return null
    //only reads from disk if region and all children are not already loaded
    public Region TryLoadRegion(World world, BlockCoord coords, int level = 1)
    {
        targetCache.Clear();
        //find path from root to chunk
        Region r = world.Octree.Root ?? forceLoadNonRecursive(GetPath(null)); //load root if needed
        int rootLevel = r.Level;
        bool mustRead = false;
        //descend down the tree to the specified coords
        while (r != null)
        {
            int next = r.GetOctantId(coords);
            if (r.Level == level) break; //found region at right level, no need to keep looping
            if (r.Children == null || r.Children[next] == null)
            {
                //our target is not in memory so...
                //find out if target region or parent is saved to disk
                bool found = false;
                Godot.GD.Print("blah blah" + r);
                if (r.SavedChildIndicies == null) return null;
                for (int i = 0; i < r.SavedChildIndicies.Length; i++)
                {
                    if (r.SavedChildIndicies[i] == next) { found = true; break; } //found our match
                }
                if (!found) return null; //dead end
                //read files to find the region
                mustRead = true;
                //maintain tree
                Godot.GD.Print($"eeeeek {r} {next} {rootLevel}");
                Region child = findRegionInFile(GetChildPath(r, next, rootLevelCutoff: rootLevel), rootLevel, coords, level);
                r.AddChild(child);
                return child;
            }
            //region may still be in memory
            r = r.Children[next];
        }
        if (!mustRead) return r.Contains(coords) ? r : null; //region in memory! still check bounds because world root could not contain the target
                                                             //read our region from file
        return null;
    }

    //only checks file at filepath
    private Region findRegionInFile(string filepath, int rootLevel, BlockCoord coords, int level)
    {
        using (BinaryReader br = new BinaryReader(File.Open(filepath, FileMode.Open)))
        {
            bool recursive = br.ReadByte() == 1;
            Region r = null;
            do
            {
                (r, long headerSize) = Region.DeserializeNonRecursive(br);
                //on the right level, so we either match now or never
                if (r.Level == level) {
                    if (!recursive) return r.Contains(coords) ? r : null;
                    //if it is recursive, we need to back up and parse the region recursively
                    br.BaseStream.Position -= headerSize;
                    return Region.DeserializeRecursive(br);
                } 
                int target = r.GetOctantId(coords);
                //verify we have the target
                int targetInList = -1;
                foreach (var i in r.SavedChildIndicies)
                {
                    if (i == target) { targetInList = i; break; }
                }
                if (targetInList == -1) return null; //dead end
                if (!recursive)
                {
                    //move to next file to find our actual region
                    string childpath = GetChildPath(r, target, rootLevelCutoff: rootLevel);
                    if (childpath == filepath) throw new System.FormatException($"file corrupted (nonrecursive leaf file): {filepath}"); //file corrupted, avoid infinite loop
                    Region actual = findRegionInFile(childpath, rootLevel, coords, level);
                    return actual;
                }
                //recursive file, so our this is a leaf node
                //skip targetInList children
                for (int i = 0; i < targetInList; i++)
                {
                    long childSize = br.ReadInt32() - 4; //subtract 4 bytes for the size field
                    br.BaseStream.Seek(childSize, SeekOrigin.Current);
                }
            } while (r.Level > level && r.Level > 0);
        }
        return null;
    }
}