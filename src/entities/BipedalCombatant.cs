using Godot;

public class BipedalCombatant : Combatant
{
    [Export] public NodePath AnimationTreePath;
    [Export] public string WalkBlendNode = "walk-blend";
    [Export] public bool SetSpeedToWalk=true;
    [Export] public float StrideLength = 40;

    [Export] public bool HandleCollision = true; //needed for large, segmented combatants. handles ik, standing on one foot, etc.
    [Export] public float MaxSlope = 5;
    [Export] public NodePath LFootBottom;
    [Export] public NodePath RFootBottom;

    protected AnimationTree animationTree;
    private bool lFootOnGround = false;
    private bool rFootOnGround = false;
    private Spatial lFootTarget;
    private Spatial rFootTarget;

    public override void _Ready()
    {
        animationTree = GetNode<AnimationTree>(AnimationTreePath);
        lFootTarget = GetNode<Spatial>(LFootBottom);
        rFootTarget = GetNode<Spatial>(RFootBottom);
        base._Ready();
    }

    public override void _PhysicsProcess(float dt)
    {
        if (SetSpeedToWalk) {
            animationTree.Set($"parameters/{WalkBlendNode}/TimeScale/scale", new Vector3(Velocity.x,0,Velocity.z).Length()/StrideLength);
        }
        float currSlope = Mathf.Max(getFootHeight(lFootTarget.GlobalTransform.origin), getFootHeight(rFootTarget.GlobalTransform.origin));
        if (currSlope >= MaxSlope) {
            Velocity = Vector3.Zero;
            collisionDirections = 0xff; //inside wall, so colliding in all directions
        } else {
            Position = new Vector3(Position.x, Position.y + currSlope, Position.z);
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
        Vector3 start = new Vector3(footTargetGlobal.x,footTargetGlobal.y+MaxSlope,footTargetGlobal.z);
        BlockcastHit hit = World.Singleton.Blockcast(start,new Vector3(0,-MaxSlope, 0));
        if (hit == null) return 0;
        return hit.HitPos.y-footTargetGlobal.y;
    }

    protected override void doCollision(World world, float dt)
    {
        updateGrounded();
        if (onGround()) {
            Velocity.y = Mathf.Max(0,Velocity.y);
        }
    }

    private void updateGrounded()
    {
        lFootOnGround = World.Singleton.Blockcast(lFootTarget.GlobalTransform.origin, new Vector3(0,-0.05f,0)) != null;
        rFootOnGround = World.Singleton.Blockcast(rFootTarget.GlobalTransform.origin, new Vector3(0,-0.05f,0)) != null;
    }

    private bool onGround() => lFootOnGround || rFootOnGround;
}