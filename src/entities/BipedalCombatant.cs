using Godot;

public partial class BipedalCombatant : Combatant
{
    [Export] public NodePath AnimationTreePath;
    [Export] public string WalkBlendNode = "walk";
    [Export] public bool SetSpeedToWalk=true;
    [Export] public float StrideLength = 40;

    [Export] public bool HandleCollision = true; //needed for large, segmented combatants. handles ik, standing on one foot, etc.
    [Export] public float MaxSlope = 5;
    [Export] public NodePath LFootBottom;
    [Export] public NodePath RFootBottom;

    protected AnimationTree animationTree;
    private bool lFootOnGround = false;
    private bool rFootOnGround = false;
    private Node3D lFootTarget;
    private Node3D rFootTarget;

    public override void _Ready()
    {
        animationTree = GetNode<AnimationTree>(AnimationTreePath);
        lFootTarget = GetNode<Node3D>(LFootBottom);
        rFootTarget = GetNode<Node3D>(RFootBottom);
        base._Ready();
    }

    public override void _PhysicsProcess(double dt)
    {
        if (SetSpeedToWalk) {
            animationTree.Set($"parameters/{WalkBlendNode}/TimeScale/scale", new Vector3(Velocity.X,0,Velocity.Z).Length()/StrideLength);
        }
        float currSlope = Mathf.Max(getFootHeight(lFootTarget.GlobalPosition), getFootHeight(rFootTarget.GlobalPosition));
        if (currSlope >= MaxSlope) {
            Velocity = Vector3.Zero;
            collisionDirections = 0xff; //inside wall, so colliding in all directions
        } else {
            GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y + currSlope, GlobalPosition.Z);
            collisionDirections = 0 | (onGround() ? 1 : 0 << (int)Direction.NegY); //update if we are on ground or not
        }
        base._PhysicsProcess(dt);
    }

    public void DoIK()
    {
        //TODO LOL
    }

    //returns the distance the foot needs to raise to be above the ground
    private float getFootHeight(Vector3 footTargetGlobal)
    {
        Vector3 start = new Vector3(footTargetGlobal.X,footTargetGlobal.Y+MaxSlope,footTargetGlobal.Z);
        BlockcastHit hit = World.Blockcast(start,new Vector3(0,-MaxSlope, 0));
        if (hit == null) return 0;
        return hit.HitPos.Y-footTargetGlobal.Y;
    }

    protected override void doCollision(World world, float dt)
    {
        updateGrounded();
        if (onGround()) {
            Velocity.Y = Mathf.Max(0,Velocity.Y);
        }
    }

    private void updateGrounded()
    {
        lFootOnGround = World.Blockcast(lFootTarget.GlobalPosition, new Vector3(0,-0.05f,0)) != null;
        rFootOnGround = World.Blockcast(rFootTarget.GlobalPosition, new Vector3(0,-0.05f,0)) != null;
    }

    private bool onGround() => lFootOnGround || rFootOnGround;
}