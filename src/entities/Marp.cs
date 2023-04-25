using Godot;

namespace Recursia;
public partial class Marp : BipedalCombatant
{
    [Export] public float WalkSpeed = 10;
    [Export] public float CarryTime = 2;

    [Export] public float StateSwitchInterval = 1;
    [Export] public string SmackState = "smack";
    [Export] public float Smackitude = 100;
    [Export] public float SmackHeight = 10;
    [Export] public float SmackDamage = 1;
    [Export] public float AggroRange = 250;

    [Export] public Node3D? CarryTarget;

    private Combatant? carrying;
    private AnimationNodeStateMachinePlayback? stateMachine;
    private double stateSwitchTimer;

    public override void _Ready()
    {
        base._Ready();
        stateMachine = (AnimationNodeStateMachinePlayback)animationTree!.Get("parameters/playback");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (stateMachine == null)
        {
            GD.PushError("Marp statemachine is null!");
            doWalk();
            base._PhysicsProcess(delta);
            return;
        }
        if (StateSwitchInterval <= stateSwitchTimer)
        {
            if (stateMachine.GetCurrentNode() == SmackState)
            {
                stateMachine.Travel(WalkBlendNode);
                stateSwitchTimer = 0;
            }
        }
        stateSwitchTimer += delta;
        if (stateMachine.GetCurrentNode() == WalkBlendNode)
        {
            doWalk();
        }
        base._PhysicsProcess(delta);
    }
    private void doWalk()
    {
        //isinstancevalid checks null
        if (IsInstanceValid(carrying) && carrying!.IsInsideTree())
        {
            Vector3 carryDest = new(0,0,WalkSpeed);
            //isinstancevalid checks null
            if (IsInstanceValid(CarryTarget) && CarryTarget!.IsInsideTree()) carryDest = (CarryTarget!.GlobalPosition-GlobalPosition).Normalized()*WalkSpeed;
            else CarryTarget = null;
            Velocity = new Vector3(carryDest.X, Velocity.Y, carryDest.Z);
            carrying!.GlobalPosition = GlobalPosition+new Vector3(0,2,0);
            carrying.Velocity = Vector3.Zero;
            if (CarryTime <= stateSwitchTimer) carrying = null;
            return;
        }
        else
        {
            carrying = null;
        }

        if (!World!.Entities.ClosestEnemy(GlobalPosition, Team, AggroRange, out Combatant? closest)) return;
        Vector3 dv = (closest.GlobalPosition-GlobalPosition).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.X, Velocity.Y, dv.Z);
        Vector3 dp = closest.GlobalPosition-GlobalPosition;
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
        //never called if statemachine is null
        stateMachine!.Travel(SmackState);
        stateSwitchTimer = 0;
        c.Velocity = (c.GlobalPosition-GlobalPosition).Normalized()*Smackitude+new Vector3(0,SmackHeight,0);
        c.TakeDamage(new Damage{Amount=SmackDamage,Team=Team});
    }
}