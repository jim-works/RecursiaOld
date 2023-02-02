using Godot;

public class Marp : BipedalCombatant
{
    [Export] public float WalkSpeed = 10;
    [Export] public float CarryTime = 2;

    [Export] public float StateSwitchInterval = 1;
    [Export] public string SmackState = "smack";
    [Export] public float Smackitude = 100;
    [Export] public float SmackHeight = 10;
    [Export] public float SmackDamage = 1;

    [Export] public Spatial CarryTarget;

    private Combatant carrying = null;
    private AnimationNodeStateMachinePlayback stateMachine;
    private float stateSwitchTimer = 0;

    public override void _Ready()
    {
        base._Ready();
        stateMachine = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
    }

    public override void _PhysicsProcess(float dt)
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
            doWalk(dt);
        }
        base._PhysicsProcess(dt);
    }
    private void doWalk(float dt)
    {
        if (carrying != null && !carrying.IsQueuedForDeletion())
        {
            Vector3 carryDest = new Vector3(0,0,WalkSpeed);
            if (CarryTarget != null) carryDest = (CarryTarget.GlobalTransform.origin-Position).Normalized()*WalkSpeed;
            Velocity = new Vector3(carryDest.x, Velocity.y, carryDest.z);
            carrying.Position = Position+new Vector3(0,2,0);
            carrying.Velocity = Vector3.Zero;
            if (CarryTime <= stateSwitchTimer) carrying = null;
            return;
        }
        
        if (!World.Singleton.ClosestEnemy(Position, Team, out Combatant closest)) return;
        if (closest == null) return;
        Vector3 dv = (closest.Position-Position).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.x, Velocity.y, dv.z);
        Vector3 dp = (closest.Position-Position);
        if (new Vector3(dp.x,0,dp.z).LengthSquared() < 2 && dp.y < 4)
        {
            if (dp.y > 3)
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
        c.Velocity = (c.Position-Position).Normalized()*Smackitude+new Vector3(0,SmackHeight,0);
        c.TakeDamage(new Damage{Amount=SmackDamage,Team=Team});
    }
}