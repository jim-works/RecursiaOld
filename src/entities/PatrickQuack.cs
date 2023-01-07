using Godot;
//skeleton man
public class PatrickQuack : Combatant
{

    [Export] public float AttackInterval = 3;

    [Export] public PackedScene MinionSpawn;

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

    private float stepHeight = 25;
    private float attackTimer = 0;
    private float strikeImpulse = 25;
    private float maxFootHeightDiff = 30;
    private float friction = 10f;
    private int numMinions = 5;
    private float minionSpawnDelay = 0.5f;

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
        PhysicsActive = true;
        base._Ready();
    }

    public override void _PhysicsProcess(float delta)
    {
        attackTimer += delta;
        if (attackTimer >= AttackInterval)
        {
            attack();
        }
        base._PhysicsProcess(delta);
    }

    private void attack()
    {
        attackTimer = 0;
        Player closest = World.Singleton.ClosestPlayer(Position);
        Velocity += new Vector3(0,25,0); //little hop
        Velocity += (closest.Position-Position).Normalized()*strikeImpulse;
        //LookAt(closest.Position, Vector3.Up);
        for (int i = 0; i < numMinions; i++)
        {
            var minion = MinionSpawn.Instance<Combatant>();
            GetParent().AddChild(minion);
            if (i%2==0) minion.Translation = rHandDest.GlobalTransform.origin;
            else minion.Translation = lHandDest.GlobalTransform.origin;
            minion.Team = Team;
        }
        GD.Print("ATTACK");
    }

    //return that we are on ground if both feet are, or if one foot is much lower than the other one
    private bool updateTargets()
    {
        float amp = 50;
        float freq = 1f;
        float sample = Mathf.Sin(OS.GetTicksMsec()/1000.0f*freq)*amp;
        (Vector3 lFootPos, bool lFootOnGround) = getFootOffset(lFootDest.GlobalTransform.origin, lFootBaseOffset);
        (Vector3 rFootPos, bool rFootOnGround) = getFootOffset(rFootDest.GlobalTransform.origin, rFootBaseOffset);
        lFootDest.GlobalTransform = new Transform(lFootDest.GlobalTransform.basis, lFootPos);
        rFootDest.GlobalTransform = new Transform(rFootDest.GlobalTransform.basis, rFootPos);

        lHandDest.Translation = new Vector3(-sample, 0, 0);
        rHandDest.Translation = new Vector3(sample, 0, 0);

        //return that we are on ground if both feet are, or if one foot is much lower than the other one
        return (lFootOnGround && rFootOnGround) || ((lFootOnGround||rFootOnGround)&&Mathf.Abs(lFootPos.y-rFootPos.y)>=maxFootHeightDiff);
    }

    private (Vector3, bool) getFootOffset(Vector3 footTargetGlobal, Vector3 localBaseOffset)
    {
        Vector3 start = new Vector3(footTargetGlobal.x,footTargetGlobal.y+stepHeight,footTargetGlobal.z);
        BlockcastHit hit = World.Singleton.Blockcast(start,new Vector3(0,-stepHeight, 0));
        if (hit == null) return (GlobalTransform.Xform(localBaseOffset), false);
        return (hit.HitPos, true);
    }

    protected override void doCollision(World world, float dt)
    {
        bool onGround = updateTargets();
        if (onGround) {
            Velocity.y = Mathf.Max(0,Velocity.y);
            doFriction(friction*dt);
        }
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