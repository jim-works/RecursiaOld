using Godot;

public partial class Marp : BipedalCombatant
{
    [Export] public float WalkSpeed = 10;
    [Export] public float CarryTime = 2;

    [Export] public float StateSwitchInterval = 1;
    [Export] public string SmackState = "smack";
    [Export] public float Smackitude = 100;
    [Export] public float SmackHeight = 10;
    [Export] public float SmackDamage = 1;

    [Export] public Node3D CarryTarget;

    private Combatant carrying = null;
    private AnimationNodeStateMachinePlayback stateMachine;
    private double stateSwitchTimer = 0;

    public override void _Ready()
    {
        base._Ready();
        stateMachine = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
    }

    public override void _PhysicsProcess(double dt)
    {
        if (StateSwitchInterval <= stateSwitchTimer)
        {
            if (stateMachine.GetCurrentNode() == SmackState)
            {
                stateMachine.Travel(WalkBlendNode);
                stateSwitchTimer = 0;
            }
        }
        stateSwitchTimer += dt;
        if (stateMachine.GetCurrentNode() == WalkBlendNode)
        {
            doWalk((float)dt);
        }
        base._PhysicsProcess(dt);
    }
    private void doWalk(float dt)
    {
        if (IsInstanceValid(carrying))
        {
            Vector3 carryDest = new Vector3(0,0,WalkSpeed);
            if (IsInstanceValid(CarryTarget)) carryDest = (CarryTarget.GlobalPosition-GlobalPosition).Normalized()*WalkSpeed;
            Velocity = new Vector3(carryDest.X, Velocity.Y, carryDest.Z);
            carrying.GlobalPosition = GlobalPosition+new Vector3(0,2,0);
            carrying.Velocity = Vector3.Zero;
            if (CarryTime <= stateSwitchTimer) carrying = null;
            return;
        }
        
        if (!World.Singleton.ClosestEnemy(GlobalPosition, Team, out Combatant closest)) return;
        if (closest == null) return;
        Vector3 dv = (closest.GlobalPosition-GlobalPosition).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.X, Velocity.Y, dv.Z);
        Vector3 dp = (closest.GlobalPosition-GlobalPosition);
        if (new Vector3(dp.X,0,dp.Z).LengthSquared() < 2 && dp.Y < 4)
        {
            if (dp.Y > 3)
            {
                carrying = closest;
                stateSwitchTimer = 0;
                return;
            }
            smack(closest);
        }
    }

    private void smack(Combatant c)
    {
        stateMachine.Travel(SmackState);
        stateSwitchTimer = 0;
        c.Velocity = (c.GlobalPosition-GlobalPosition).Normalized()*Smackitude+new Vector3(0,SmackHeight,0);
        c.TakeDamage(new Damage{Amount=SmackDamage,Team=Team});
    }
}