using Godot;
//skeleton man
public class PatrickQuack : Combatant
{

    [Export] public float AttackInterval = 3;

    [Export] public PackedScene MinionSpawn;

    [Export] public float MoveSpeed = 5;
    [Export] public float StepInterval = 2;
    [Export] public float ControlPointHeight = 10;
    [Export] public float MinDistanceForStep = 5;

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

    private Bezier lFootPath;
    private float tlFoot = 1;
    private bool lFootOnGround;
    private Bezier rFootPath;
    private float trFoot = 1;
    private bool rFootOnGround;

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
        tlFoot = 1;
        trFoot = 1;
        base._Ready();
    }

    public override void _PhysicsProcess(float delta)
    {
        Player closest = World.Singleton.ClosestPlayer(Position);
        Vector3 targetV = (closest.Position-Position).Normalized()*MoveSpeed;
        Velocity.x = targetV.x;
        Velocity.z = targetV.z;

        updateFootBeziers();
        updateFootPosition(delta);
        
        base._PhysicsProcess(delta);
    }

    private void attack()
    {
        attackTimer = 0;
        // Player closest = World.Singleton.ClosestPlayer(Position);
        // Velocity += new Vector3(0,25,0); //little hop
        // Velocity += (closest.Position-Position).Normalized()*strikeImpulse;
        // //LookAt(closest.Position, Vector3.Up);
        // // for (int i = 0; i < numMinions; i++)
        // // {
        // //     var minion = MinionSpawn.Instance<Combatant>();
        // //     GetParent().AddChild(minion);
        // //     if (i%2==0) minion.Translation = rHandDest.GlobalTransform.origin;
        // //     else minion.Translation = lHandDest.GlobalTransform.origin;
        // //     minion.Team = Team;
        // // }
        // GD.Print("ATTACK");
    }
    //we are on ground if both feet are, or if one foot is much lower than the other foot, which is on the ground.
    private bool onGround() => (lFootOnGround && rFootOnGround) || ((lFootOnGround||rFootOnGround)&&Mathf.Abs(lFootDest.GlobalTransform.origin.y-rFootDest.GlobalTransform.origin.y)>=maxFootHeightDiff);
    private void updateGrounded()
    {
        lFootOnGround = World.Singleton.Blockcast(lFootDest.GlobalTransform.origin, new Vector3(0,-0.5f,0)) == null;
        rFootOnGround = World.Singleton.Blockcast(rFootDest.GlobalTransform.origin, new Vector3(0,-0.5f,0)) == null;
    }
    private void updateFootPosition(float dt)
    {
        tlFoot += dt/StepInterval;
        Vector3 lFootPos = lFootPath.Sample(tlFoot);
        trFoot += dt/StepInterval;
        Vector3 rFootPos = rFootPath.Sample(trFoot);
        lFootDest.GlobalTransform = new Transform(lFootDest.GlobalTransform.basis, lFootPos);
        rFootDest.GlobalTransform = new Transform(rFootDest.GlobalTransform.basis, rFootPos);
    }
    //return that we are on ground if both feet are, or if one foot is much lower than the other one
    private void updateFootBeziers()
    {
        if (needToStep(tlFoot, lFootDest.GlobalTransform.origin)) {
            tlFoot = 0;
            lFootPath = calcTrajectory(lFootDest.GlobalTransform.origin, lFootBaseOffset);
        }
        if (needToStep(trFoot, rFootDest.GlobalTransform.origin)) {
            trFoot = 0;
            rFootPath = calcTrajectory(rFootDest.GlobalTransform.origin, rFootBaseOffset);
        }
    }

    private bool needToStep(float t, Vector3 footGlobal)
    {
        return t>=1 && (footGlobal-Position).Dot(Velocity) < 0; //foot is behind combatant and is finished with its previous bezier
    }

    //returns true if foot is on ground
    private Bezier calcTrajectory(Vector3 footGlobal, Vector3 localFootOffset)
    {
        //use the current position as start of the bezier, so we only need to calculate the destination.
        Vector3 planarV = new Vector3(Velocity.x,0,Velocity.z);
        float planarVMag = planarV.Length();
        //distance in front of current pos to place foot
        // (m/step) = (sec/step)*(m/s)
        float dist = StepInterval * planarVMag;
        //account for terrain
        (Vector3 dest, _) = getFootOffset(Position+planarV/planarVMag*dist, localFootOffset);
        //the control point is the mean of the start/end, moved upward some amount
        Vector3 control = (footGlobal+dest)/2+new Vector3(0,ControlPointHeight,0);
        GD.Print($"pos: {Position}, start {footGlobal}, control {control}, end {dest}");
        return new Bezier(footGlobal,control,dest);
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
        updateGrounded();
        if (onGround()) {
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