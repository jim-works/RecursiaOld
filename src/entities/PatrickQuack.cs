using Godot;

public class PatrickQuack : BipedalCombatant
{
    [Export] public float MaxDistFromPlayer = 50;
    [Export] public float WalkSpeed = 5;

    [Export] public float StateSwitchInterval = 5;
    [Export] public float SummonInterval = 2;
    [Export] public string SummonState = "summon";
    [Export] public PackedScene EnemyToSummon;
    [Export] public NodePath SummonPoint;

    private AnimationNodeStateMachinePlayback stateMachine;
    private float stateSwitchTimer = 0;
    private float summonTimer = 0;
    private Spatial summonPoint;

    public override void _Ready()
    {
        base._Ready();
        stateMachine = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
        summonPoint = GetNode<Spatial>(SummonPoint);
    }

    public override void _PhysicsProcess(float dt)
    {
        if (StateSwitchInterval <= stateSwitchTimer)
        {
            if (stateMachine.GetCurrentNode() == SummonState)
            {
                stateMachine.Travel(WalkBlendNode);
            }
            else
            {
                stateMachine.Travel(SummonState);
            }
            stateSwitchTimer = 0;
        }
        stateSwitchTimer += dt;
        
        if (stateMachine.GetCurrentNode() == SummonState)
        {
            doSummon(dt);
        }
        if (stateMachine.GetCurrentNode() == WalkBlendNode)
        {
            doWalk(dt);
        }
        base._PhysicsProcess(dt);
    }
    private void doWalk(float dt)
    {
        Player closest = World.Singleton.ClosestPlayer(Position);
        if ((closest.Position-Position).LengthSquared() < MaxDistFromPlayer*MaxDistFromPlayer) return; // close enough, skip walking
        Vector3 dv = (closest.Position-Position).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.x, Velocity.y, dv.z);
    }
    private void doSummon(float dt)
    {
        Velocity = new Vector3(0,Velocity.y,0);
        if (summonTimer >= SummonInterval)
        {
            Combatant c = World.Singleton.SpawnChild<Combatant>(EnemyToSummon);
            c.Team = Team;
            c.Position = summonPoint.GlobalTransform.origin;
            summonTimer = 0;
        }
        summonTimer += dt;
    }
}