using Godot;

namespace Recursia;
public partial class PatrickQuack : BipedalCombatant
{
    [Export] public float MaxDistFromTarget = 50;
    [Export] public float WalkSpeed = 5;
    [Export] public int MinDrops = 5;
    [Export] public int RandomDrops = 3;

    [Export] public float StateSwitchInterval = 5;
    [Export] public float SummonInterval = 2;
    [Export] public float ShootInterval = 1;
    [Export] public float ProjectileVelocity = 50;
    [Export] public string SummonState = "summon";
    [Export] public PackedScene[] EnemiesToSummon = null!;
    [Export] public PackedScene? Projectile;
    [Export] public NodePath? SummonPoint;

    [Export] public AudioStream? SummonSound;
    [Export] public AudioStream? ShootSound;
    [Export] public float AggroRange = 512;

    private AnimationNodeStateMachinePlayback? stateMachine;
    private double stateSwitchTimer;
    private double summonTimer;
    private double shootTimer;
    private Node3D summonPoint = null!;
    private int spawnIdx;

    public override void _Ready()
    {
        base._Ready();
        stateMachine = (AnimationNodeStateMachinePlayback)animationTree!.Get("parameters/playback");
        summonPoint = GetNode<Node3D>(SummonPoint);
        if (BossUI.Singleton == null)
        {
            GD.PushError("BossUI Singleton is null!");
        }
        else
        {
            BossUI.Singleton.Track(this, "Patrick Quack");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (stateMachine == null)
        {
            doWalk((float)delta);
            GD.PushError("Null statemachine on patrick quack!");
            base._PhysicsProcess(delta);
            return;
        }
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
        stateSwitchTimer += delta;

        if (stateMachine.GetCurrentNode() == SummonState)
        {
            doSummon((float)delta);
        }
        if (stateMachine.GetCurrentNode() == WalkBlendNode)
        {
            doWalk((float)delta);
        }
        base._PhysicsProcess(delta);
    }

    public override void Die()
    {
        if (!BlockTypes.TryGet("loot", out Block? b) || !ItemTypes.TryGet("marp_rod", out Item? item))
        {
            GD.PushError("Couldn't find loot block or marp_rod for patrick quack drop!");
            return;
        }
        LootBlock l = (LootBlock)b;
        l.Drops = new ItemStack[] {new ItemStack{Item=item,Size=MinDrops+Mathf.RoundToInt(GD.Randf()*RandomDrops)}};
        World!.SetBlock((BlockCoord)GlobalPosition, b);
        base.Die();
    }
    private void doWalk(float dt)
    {
        if (!World!.Entities.ClosestEnemy(GlobalPosition, Team, AggroRange, out Combatant? closest)) return;
        if ((closest.GlobalPosition-GlobalPosition).LengthSquared() < MaxDistFromTarget*MaxDistFromTarget) return; // close enough to target, skip walking
        Vector3 dv = (closest.GlobalPosition-GlobalPosition).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.X, Velocity.Y, dv.Z);
        if (shootTimer >= ShootInterval)
        {
            PlaySound(ShootSound);
            if (Projectile == null)
            {
                GD.PushError("Patrick quack's projectile is null!");
                return;
            }
            Projectile proj = World.Entities.SpawnObject<Projectile>(Projectile, summonPoint.GlobalPosition);
            proj.Launch((closest.GlobalPosition-summonPoint.GlobalPosition).Normalized()*ProjectileVelocity, Team);
            shootTimer = 0;
        }
        shootTimer += dt;
    }
    private void doSummon(float dt)
    {
        Velocity = new Vector3(0,Velocity.Y,0);
        if (summonTimer >= SummonInterval)
        {
            PlaySound(SummonSound);
            Combatant c = World!.Entities.SpawnObject<Combatant>(EnemiesToSummon[spawnIdx], summonPoint.GlobalPosition);
            if (c is Marp m) m.CarryTarget = this;
            spawnIdx = (spawnIdx+1)%EnemiesToSummon.Length;
            c.Team = Team;
            summonTimer = 0;
        }
        summonTimer += dt;
    }
}