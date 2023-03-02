public partial class RegionOctree
{
    public Region Root {get; private set;}

    public RegionOctree(int startingLevel, BlockCoord origin)
    {
        Root = Region.MakeRegion(startingLevel, origin);
        Root.Tree = this;
    }

    //returns false if region is invalid
    public bool AddRegion(Region r)
    {
        // Godot.GD.Print($"Trying to add region {r}");
        if (Root.InRegion(r.Origin)) return Root.AddChild(r);
        //region is outside octree, so we must expand with a new root    
        BlockCoord delta = r.Origin-Root.Origin;
        BlockCoord movement = new BlockCoord(delta.X < 0 ? (int)Root.Size : 0,delta.Y < 0 ? (int)Root.Size : 0,delta.Z < 0 ? (int)Root.Size : 0);
        // Godot.GD.Print($"Movement {movement}");
        //new root should be in the same direction as delta.
        //So if delta points in the -x direction, the current root should become the +x child of the new root
        //int oldRootIdx = (delta.X < 0 ? 1 : 0) << 2 | (delta.Y < 0 ? 1 : 0) << 1 | (delta.Z < 0 ? 1 : 0);
        Region newRoot = Region.MakeRegion(Root.Level+1, Root.Origin - movement);
        newRoot.Tree = this;
        newRoot.AddChild(Root);
        Root = newRoot;
        Godot.GD.Print($"New root {Root}");
        return AddRegion(r);
    }

    public override string ToString()
    {
        return $"RegionOctree with root {Root}";
    }
}