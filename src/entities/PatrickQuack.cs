using Godot;
//skeleton man
public class PatrickQuack : Combatant
{
    [Export] public NodePath LHandIKPath = "metarig/skeleton/LHandIK";
    [Export] public NodePath RHandIKPath = "metarig/skeleton/RHandIK";
    [Export] public NodePath LFootIKPath = "metarig/skeleton/LFootIK";
    [Export] public NodePath RFootIKPath = "metarig/skeleton/RFootIK";
    [Export] public NodePath SkeletonPath = "metarig/Skeleton";
    [Export] public NodePath LHandDestPath;
    [Export] public NodePath RHandDestPath;
    [Export] public NodePath LFootDestPath;
    [Export] public NodePath RFootDestPath;

    private SkeletonIK lHandIK;
    private SkeletonIK rHandIK;
    private SkeletonIK lFootIK;
    private SkeletonIK rFootIK;

    private Spatial lHandDest;
    private Spatial rHandDest;
    private Spatial lFootDest;
    private Spatial rFootDest;

    private Vector3 lHandBaseOffset;
    private Vector3 rHandBaseOffset;
    private Vector3 lFootBaseOffset;
    private Vector3 rFootBaseOffset;

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
        setupIK();

        base._Ready();
    }

    public override void _PhysicsProcess(float dt)
    {
        updateTargets();
        base._PhysicsProcess(dt);
    }

    private void updateTargets()
    {
        float amp = 50;
        float freq = 1f;
        float sample = Mathf.Sin(OS.GetTicksMsec()/1000.0f*freq)*amp;

        lFootDest.GlobalTransform = new Transform(lFootDest.GlobalTransform.basis, getFootOffset(lFootDest.GlobalTransform, lFootBaseOffset));
        GD.Print("lfootdest: " + lFootDest.Translation);
        rFootDest.GlobalTransform = new Transform(rFootDest.GlobalTransform.basis, getFootOffset(rFootDest.GlobalTransform, rFootBaseOffset));

        lHandDest.Translation = new Vector3(-sample, 0, 0);
        rHandDest.Translation = new Vector3(sample, 0, 0);
    }

    private Vector3 getFootOffset(Transform footTarget, Vector3 baseOffset)
    {
        BlockcastHit hit = World.Singleton.Blockcast(GlobalTransform.Xform(new Vector3(footTarget.origin.x,0,footTarget.origin.z)),new Vector3(0,GlobalTransform.Xform(baseOffset).y,0));
        if (hit == null) return baseOffset;
        return hit.HitPos;
    }

    private void setupIK()
    {
        lHandIK = GetNode<SkeletonIK>(LHandIKPath);
        rHandIK = GetNode<SkeletonIK>(RHandIKPath);
        lFootIK = GetNode<SkeletonIK>(LFootIKPath);
        rFootIK = GetNode<SkeletonIK>(RFootIKPath);

        lHandDest = GetNode<Spatial>(LHandDestPath);
        rHandDest = GetNode<Spatial>(RHandDestPath);
        lFootDest = GetNode<Spatial>(LFootDestPath);
        rFootDest = GetNode<Spatial>(RFootDestPath);

        lHandIK.TargetNode = lHandDest.GetPath();
        rHandIK.TargetNode = rHandDest.GetPath();
        lFootIK.TargetNode = lFootDest.GetPath();
        rFootIK.TargetNode = rFootDest.GetPath();

        lHandBaseOffset = lHandDest.Translation;
        rHandBaseOffset = rHandDest.Translation;
        lFootBaseOffset = lFootDest.Translation;
        rFootBaseOffset = rFootDest.Translation;

        lHandIK.Start();
        rHandIK.Start();
        lFootIK.Start();
        rFootIK.Start();
    }
}