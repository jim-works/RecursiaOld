using Godot;

public class PatrickQuack : BipedalCombatant
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
    [Export] public PackedScene[] EnemiesToSummon;
    [Export] public PackedScene Projectile;
    [Export] public NodePath SummonPoint;

    [Export] public AudioStream SummonSound;
    [Export] public AudioStream ShootSound;

    private AnimationNodeStateMachinePlayback stateMachine;
    private float stateSwitchTimer = 0;
    private float summonTimer = 0;
    private float shootTimer = 0;
    private Spatial summonPoint;
    private int spawnIdx = 0;

    public override void _Ready()
    {
        base._Ready();
        stateMachine = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
        summonPoint = GetNode<Spatial>(SummonPoint);
        BossUI.Singleton.Track(this, "Patrick Quack");
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

    public override void Die()
    {
        LootBlock b = (LootBlock)BlockTypes.Get("loot");
        b.Drops = new ItemStack[] {new ItemStack{Item=ItemTypes.Get("marp_rod"),Size=MinDrops+(Mathf.RoundToInt(GD.Randf()*RandomDrops))}};
        World.Singleton.SetBlock((BlockCoord)Position, b);
        base.Die();
    }
    private void doWalk(float dt)
    {
        if (!World.Singleton.ClosestEnemy(Position, Team, out Combatant closest)) return;
        if ((closest.Position-Position).LengthSquared() < MaxDistFromTarget*MaxDistFromTarget) return; // close enough to target, skip walking
        Vector3 dv = (closest.Position-Position).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.x, Velocity.y, dv.z);
        // if (shootTimer >= ShootInterval)
        // {
        //     PlaySound(ShootSound);
        //     Projectile proj = Projectile.Instance<Projectile>();
        //     World.Singleton.AddChild(proj);
        //     Vector3 origin = summonPoint.GlobalTransform.origin;
        //     proj.Position = origin;
        //     proj.Launch((closest.Position-origin).Normalized()*ProjectileVelocity, Team);
        //     shootTimer = 0;
        // }
        shootTimer += dt;
    }
    private void doSummon(float dt)
    {
        Velocity = new Vector3(0,Velocity.y,0);
        if (summonTimer >= SummonInterval)
        {
            PlaySound(SummonSound);
            Combatant c = EnemiesToSummon[spawnIdx].Instance<Combatant>();
            if (c is Marp m) m.CarryTarget = this;
            spawnIdx = (spawnIdx+1)%EnemiesToSummon.Length;
            c.Team = Team;
            c.InitialPosition = summonPoint.GlobalTransform.origin;
            World.Singleton.AddChild(c);
            summonTimer = 0;
        }
        summonTimer += dt;
    }
}