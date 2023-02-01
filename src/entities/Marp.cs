using Godot;

public class Marp : BipedalCombatant
{
    [Export] public float WalkSpeed = 10;
    [Export] public PackedScene Baby;

    [Export] public float StateSwitchInterval = 1;
    [Export] public string SmackState = "smack";
    [Export] public float Smackitude = 100;
    [Export] public float SmackHeight = 10;

    public Combatant carrying = null;

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
            }
            stateSwitchTimer = 0;
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
        if (carrying != null)
        {
            Velocity.z = WalkSpeed;
            Velocity.x = 0;
            carrying.Position = Position+new Vector3(0,2,0);
            carrying.Velocity = Vector3.Zero;
            return;
        }
        Combatant closest = World.Singleton.ClosestEnemy(Position, Team);
        Vector3 dv = (closest.Position-Position).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.x, Velocity.y, dv.z);
        Vector3 dp = (closest.Position-Position);
        if (new Vector3(dp.x,0,dp.z).LengthSquared() < 2)
        {
            // if (closest.Position.y-Position.y > 1)
            // {
            //     carrying = closest;
            //     return;
            // }
            smack(closest);
        }
    }

    private void smack(Combatant c)
    {
        // if (c == null) return;
        // if (c is Marp)
        // {
        //     if (Baby == null) return;
        //     Marp child = Baby.Instance<Marp>();
        //     child.Position = Position + new Vector3(10*GD.Randf(),10,10*GD.Randf());
        //     child.Scale = Scale/2;
        //     World.Singleton.AddChild(child);
        //     //return;
        // }
        stateMachine.Travel(SmackState);
        stateSwitchTimer = 0;
        c.Velocity += (c.Position-Position).Normalized()*Smackitude+new Vector3(0,SmackHeight,0);
    }
}