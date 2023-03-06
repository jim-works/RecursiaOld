using System.Collections.Generic;
using System.IO;
using System.Linq;
//Octtree above chunks
public partial class Region : ISerializable
{
    public enum Octant
    {
        Origin=0b000,
        PosZ=0b001,
        PosY=0b010,
        PosZY=0b011,
        PosX=0b100,
        PosXZ=0b101,
        PosXY=0b110,
        PosXYZ=0b111
    }
    //Region extends in positive directions from origin
    public BlockCoord Origin;
    public int Level;
    public readonly int Size;
    public readonly int OctantSize;
    public Region Parent;
    public Region[] Children;
    public RegionOctree Tree;
    //True if any subregion is loaded
    public bool Loaded {get; private set;} = false;
    //structure is contained if region contains the position of the structure.
    // structure could overfill into this region from neighbor without being in this list
    public List<Structure> Structures = new List<Structure>();

    public static Region MakeRegion(int level, BlockCoord origin)
    {
        if (level == 0) return new Chunk((ChunkCoord)origin);
        else return new Region(level, origin);
    }

    protected Region(int level, BlockCoord origin)
    {
        Level = level;
        Size = Chunk.CHUNK_SIZE*Math.Pow(2,Level);
        if (Size == 0) Godot.GD.PrintErr($"Region Size = 0: with level {Level} and origin {origin}");
        OctantSize = Level==0?0 : Chunk.CHUNK_SIZE*Math.Pow(2,Level-1);
        Origin = origin;
    }

    public bool InRegion(BlockCoord coord)
    {
        BlockCoord delta = coord-Origin;
        return delta.X>=0&&delta.Y>=0&&delta.Z>=0&&delta.X<(int)Size && delta.Y < (int)Size && delta.Z < (int)Size;
    }
    //assumes in region
    public int GetOctantId(BlockCoord coord)
    {
        BlockCoord delta = coord-Origin;
        //construct mask according to octant enum
        return (delta.X<OctantSize?0:1) << 2 | (delta.Y < OctantSize?0:1) << 1 | (delta.Z < OctantSize?0:1);
    }
    public Octant GetOctant(BlockCoord coord)
    {
        return (Octant)GetOctantId(coord);
    }
    public int GetChildIdx(Region child) {
        if (Children == null) return -1;
        for (int i = 0; i < Children.Length; i++) {
            if (Children[i] == child) return i;
        }
        return -1;
    }
    public BlockCoord GetOctantOrigin(int id)
    {
        //check each bit in id mask
        BlockCoord delta = new BlockCoord(0,0,0);
        if ((id & 0b100) == 0b100) delta.X += (int)OctantSize;
        if ((id & 0b010) == 0b010) delta.Y += (int)OctantSize;
        if ((id & 0b001) == 0b001) delta.Z += (int)OctantSize;
        return Origin+delta;
    }

    public void AddStructure(Structure s)
    {
        Region curr = this;
        while (curr != null)
        {
            curr.Structures.Add(s);
            curr = curr.Parent;
        }
    }

    //returns true if added child, false if couldn't
    //child must have level <= this.level-1, and be within this region
    //child must be in the region, and we must not already have a child in the place it will go
    //child level=0 must be Chunk
    public bool AddChild(Region child)
    {
        if (!InRegion(child.Origin)) return false;
        if (child.Level >= Level) return false;
        if (child.Level == 0 && !(child is Chunk)) return false;
        if (Children == null) Children = new Region[8];

        return addChildUnchecked(child);
    }

    //skip initial checks on level,type, and in region.
    //returns false if tree already contains a region where we would put child
    private bool addChildUnchecked(Region child)
    {
        int idx = GetOctantId(child.Origin);
        if (child.Level < Level-1)
        {
            //will be at least a grandchild
            if (Children[idx] == null)
            {
                Region r = MakeRegion(Level-1,GetOctantOrigin(idx));
                r.Parent = this;
                r.Tree = Tree;
                Children[idx] = r;
            }
            //recursively add children
            //all conditions are satisfied, so don't worry about undoing
            return Children[idx].AddChild(child);
        }
        else
        {
            //will be a direct child
            if (Children[idx] != null) return false;
            Children[idx] = child;
            child.Parent = this;
            Children[idx].Tree = Tree;
            //set loaded if needed
            if (Children[idx].Loaded && !Loaded) updateLoadedStatus();
            Structures.AddRange(child.Structures);
        }
        
        return true;
    }

    public void RemoveChild(int id)
    {
        if (Children[id] == null) return;
        Children[id].Parent = null;
        Children[id] = null;
        updateLoadedStatus();
    }

    //loads all children and parent (if needed)
    public void Load()
    {
        bool oldLoaded = Loaded;
        setLoaded(true);
        if (!oldLoaded) Parent?.updateLoadedStatus(); //only need to update if not already loaded
    }

    //unload all children and parent (if needed)
    public void Unload()
    {
        bool oldLoaded = Loaded;
        setLoaded(false);
        if (oldLoaded) Parent?.updateLoadedStatus(); //only need to update if not already unloaded
    }

    private void setLoaded(bool val)
    {
        Loaded = val;
        if (Children == null) return;
        foreach (var child in Children)
        {
            child?.setLoaded(val);
        }
    }

    private void updateLoadedStatus()
    {
        if (Children == null) 
        {
            Parent?.updateLoadedStatus();
            return;
        }
        if (Loaded)
        {
            //unload if all children are unloaded
            foreach (var child in Children)
            {
                if (child != null && child.Loaded) return;
            }
            Loaded = false;
            Parent?.updateLoadedStatus();
        }
        else
        {
            //load if any children are loaded
            foreach (var child in Children)
            {
                if (child != null && child.Loaded) {
                    Loaded = true;
                    Parent?.updateLoadedStatus();
                }
            }
        }
    }

    public override string ToString()
    {
        return $"{(Loaded ? "Loaded" : "Unloaded")} region {Origin} level {Level} and size {Size}";
    }
    public string Print(int indention=0, System.Text.StringBuilder output = null)
    {
        if (output == null) output = new System.Text.StringBuilder();
        output.Append("\n");
        for (int i = 0; i < indention; i++) output.Append("\t");
        output.Append($"{this}:");
        if (Children == null) return output.ToString() + " leaf";
        for (int i = 0; i < Children.Length; i++) {
            Children[i]?.Print(indention+1, output);
        }
        return output.ToString();
    }
    public void AddChunks(ChunkCollection col)
    {
        if (this is Chunk c) {
            col.Add(c);
            return;
        }
        if (Children == null) return;
        foreach (var child in Children) {
            child?.AddChunks(col);
        }
    }
    //writes the 0 byte to indicate nonrecursive, then calls SerializeInfo()
    public void SerializeNonRecursive(BinaryWriter bw)
    {
        bw.Write((byte)0); //indicate this is a nonrecursive file
        SerializeInfo(bw);
    }
    //writes the 1 byte to indicate recursive, then calls Serialize()
    public void SerializeRecursive(BinaryWriter bw) {
        bw.Write((byte)1); //indicate that this is a recursive file
        Serialize(bw);
    }
    //doesn't serialize children, but includes the number of and indicies of children
    public void SerializeInfo(BinaryWriter bw) {
        bw.Write(Level);
        Origin.Serialize(bw);
        if (Children == null) {
            bw.Write((byte)0);
            return;
        }
        bw.Write((byte)Children.Count(x => x != null));
        for (int i = 0; i < Children.Length; i++) {
            if (Children[i] != null) bw.Write((byte)i);
        }
    }
    //Calls SerializeInfo(), recursively calls Serialize() for all non-null children
    public virtual void Serialize(BinaryWriter bw)
    {
        SerializeInfo(bw);
        for (int i = 0; i < Children.Length; i++) {
            if (Children[i] != null) {
                Children[i].Serialize(bw);
            }
        }
    }
    //depth-first traversal of octree. If callback returns false, we don't visit that region's children
    public void Traverse(System.Func<Region, bool> callback) {
        if (callback(this)) {
            if (Children == null) return;
            for (int i = 0; i < Children.Length; i++) {
                if (Children[i] != null) Children[i].Traverse(callback);
            }
        }
    }
    //adds the path of children you'd need to stack from the root to get to this node to the stack
    public void AddPathToRoot(Stack<int> dest)
    {
        Region r = this;
        while (r.Parent != null) {
            dest.Push(r.Parent.GetChildIdx(r));
            r = r.Parent;
        }
    }
    //returns (region, children indexes)
    public static (Region, int[]) DeserializeNonRecursive(BinaryReader br)
    {
        int level = br.ReadInt32();
        if (level == 0) {//this is a chunk 
            return (Chunk.Deserialize(br), null);
        }
        BlockCoord origin = BlockCoord.Deserialize(br);
        Region r = new Region(level, origin);
        //read # children
        int children = br.ReadByte();
        if (children == 0) return (r, null);
        r.Children = new Region[8];
        int[] idxs = new int[children];
        //read child indicies
        for (int i = 0; i < children; i++) {
            idxs[i] = br.ReadByte();
        }
        return (r,idxs);
    }
    public static Region DeserializeRecursive(BinaryReader br) {
        (Region r, int[] childrenIdxs) = DeserializeNonRecursive(br);
        if (childrenIdxs == null) return r;
        foreach (var i in childrenIdxs) {
            r.Children[i] = Region.DeserializeRecursive(br);
        }
        return r;
    }
}