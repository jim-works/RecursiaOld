using System.Collections.Generic;

//Octtree above chunks
public class Region
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
        return delta.x>=0&&delta.y>=0&&delta.z>=0&&delta.x<(int)Size && delta.y < (int)Size && delta.z < (int)Size;
    }
    //assumes in region
    public int GetOctantId(BlockCoord coord)
    {
        BlockCoord delta = coord-Origin;
        //construct mask according to octant enum
        return (delta.x<OctantSize?0:1) << 2 | (delta.y < OctantSize?0:1) << 1 | (delta.z < OctantSize?0:1);
    }
    public Octant GetOctant(BlockCoord coord)
    {
        return (Octant)GetOctantId(coord);
    }
    public BlockCoord GetOctantOrigin(int id)
    {
        //check each bit in id mask
        BlockCoord delta = new BlockCoord(0,0,0);
        if ((id & 0b100) == 0b100) delta.x += (int)OctantSize;
        if ((id & 0b010) == 0b010) delta.y += (int)OctantSize;
        if ((id & 0b001) == 0b001) delta.z += (int)OctantSize;
        return Origin+delta;
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

        return AddChildUnchecked(child);
    }

    //skip initial checks on level,type, and in region.
    //returns false if tree already contains a region where we would put child
    public bool AddChildUnchecked(Region child)
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
            Structures.AddRange(child.Structures);
        }
        
        return true;
    }

    public void RemoveChild(int id)
    {
        if (Children[id] == null) return;
        Children[id].Parent = null;
        Children[id] = null;
    }

    public override string ToString()
    {
        return $"Region {Origin} level {Level} and size {Size}";
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
}