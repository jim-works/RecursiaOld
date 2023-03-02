using Godot;

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
    [Export] public PackedScene[] EnemiesToSummon;
    [Export] public PackedScene Projectile;
    [Export] public NodePath SummonPoint;

    [Export] public AudioStream SummonSound;
    [Export] public AudioStream ShootSound;

    private AnimationNodeStateMachinePlayback stateMachine;
    private double stateSwitchTimer = 0;
    private double summonTimer = 0;
    private double shootTimer = 0;
    private Node3D summonPoint;
    private int spawnIdx = 0;

    public override void _Ready()
    {
        base._Ready();
        stateMachine = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
        summonPoint = GetNode<Node3D>(SummonPoint);
        BossUI.Singleton.Track(this, "Patrick Quack");
    }

    public override void _PhysicsProcess(double dt)
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
            doSummon((float)dt);
        }
        if (stateMachine.GetCurrentNode() == WalkBlendNode)
        {
            doWalk((float)dt);
        }
        base._PhysicsProcess(dt);
    }

    public override void Die()
    {
        LootBlock b = (LootBlock)BlockTypes.Get("loot");
        b.Drops = new ItemStack[] {new ItemStack{Item=ItemTypes.Get("marp_rod"),Size=MinDrops+(Mathf.RoundToInt(GD.Randf()*RandomDrops))}};
        World.Singleton.SetBlock((BlockCoord)GlobalPosition, b);
        base.Die();
    }
    private void doWalk(float dt)
    {
        if (!World.Singleton.ClosestEnemy(GlobalPosition, Team, out Combatant closest)) return;
        if ((closest.GlobalPosition-GlobalPosition).LengthSquared() < MaxDistFromTarget*MaxDistFromTarget) return; // close enough to target, skip walking
        Vector3 dv = (closest.GlobalPosition-GlobalPosition).Normalized()*WalkSpeed;
        Velocity = new Vector3(dv.X, Velocity.Y, dv.Z);
        // if (shootTimer >= ShootInterval)
        // {
        //     PlaySound(ShootSound);
        //     Projectile proj = Projectile.Instantiate<Projectile>();
        //     World.Singleton.AddChild(proj);
        //     Vector3 origin = summonPoint.GlobalPosition;
        //     proj.GlobalPosition = origin;
        //     proj.Launch((closest.GlobalPosition-origin).Normalized()*ProjectileVelocity, Team);
        //     shootTimer = 0;
        // }
        shootTimer += dt;
    }
    private void doSummon(float dt)
    {
        Velocity = new Vector3(0,Velocity.Y,0);
        if (summonTimer >= SummonInterval)
        {
            PlaySound(SummonSound);
            Combatant c = EnemiesToSummon[spawnIdx].Instantiate<Combatant>();
            if (c is Marp m) m.CarryTarget = this;
            spawnIdx = (spawnIdx+1)%EnemiesToSummon.Length;
            c.Team = Team;
            c.InitialPosition = summonPoint.GlobalPosition;
            World.Singleton.AddChild(c);
            summonTimer = 0;
        }
        summonTimer += dt;
    }
}