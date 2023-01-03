using Godot;
//skeleton man
public class PatrickQuack : Combatant
{
    [Export] public string LHandIKPath = "metarig/skeleton/LHandIK";
    [Export] public string RHandIKPath = "metarig/skeleton/RHandIK";
    [Export] public string LFootIKPath = "metarig/skeleton/LFootIK";
    [Export] public string RFootIKPath = "metarig/skeleton/RFootIK";
    [Export] public string SkeletonPath = "metarig/Skeleton";

    private SkeletonIK lHandIK;
    private SkeletonIK rHandIK;
    private SkeletonIK lFootIK;
    private SkeletonIK rFootIK;

    public override void _EnterTree()
    {
        _physicsActive = false;
        var children = GetNode(SkeletonPath).GetChildren();
        foreach (var child in children)
        {
            if (child is SegmentedCombatantChild segment)
            {
                segment.Parent = this;
            }
        }
        base._EnterTree();
    }

    public override void _Ready()
    {
        lHandIK = GetNode<SkeletonIK>(LHandIKPath);
        rHandIK = GetNode<SkeletonIK>(RHandIKPath);
        lFootIK = GetNode<SkeletonIK>(LFootIKPath);
        rFootIK = GetNode<SkeletonIK>(RFootIKPath);

        base._Ready();
    }
}