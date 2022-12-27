using Godot;

public class Player : Combatant
{
    [Export]
    public float Reach = 100;
    [Export]
    public float MoveSpeed = 10;
    [Export]
    public float JumpHeight = 10;
    [Export]
    public Vector3 CameraOffset = new Vector3(0,0.7f,0);
    [Export]
    public float ShootSpeed = 25;

    [Export]
    public PackedScene Projectile;

    public override void _EnterTree()
    {
        World.Singleton.ChunkLoaders.Add(this);
        World.Singleton.Players.Add(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        World.Singleton.Players.Remove(this);
        World.Singleton.ChunkLoaders.Remove(this);
        base._ExitTree();
    }

    public override void _Process(float delta)
    {
        move(delta);

        base._Process(delta);
    }

    //called by rotating camera
    public void Punch(Vector3 dir)
    {
        BlockcastHit hit = World.Singleton.Blockcast(Position+CameraOffset, dir*Reach);
        if (hit != null) {
            World.Singleton.SetBlock(hit.BlockPos, null);
        }
    }

    //called by rotating camera
    public void Use(Vector3 dir)
    {
        // Projectile proj = Projectile.Instance<Projectile>();
        // World.Singleton.AddChild(proj);
        // proj.Position = Position+CameraOffset;
        // proj.Launch(dir*ShootSpeed, Team);
        BlockcastHit hit = World.Singleton.Blockcast(Position+CameraOffset, dir*Reach);
        if (hit != null) {
            World.Singleton.SetBlock(hit.BlockPos+(BlockCoord)hit.Normal, BlockTypes.Get("dirt"));
        }
    }

    private void move(float delta)
    {
        float x = Input.GetActionStrength("move_right")-Input.GetActionStrength("move_left");
        float z = Input.GetActionStrength("move_backward")-Input.GetActionStrength("move_forward");
        Vector3 movement =  LocalDirectionToWorld(new Vector3(MoveSpeed*x,0,MoveSpeed*z));
        Velocity.x = movement.x;
        Velocity.z = movement.z;
        if (Input.IsActionJustPressed("jump"))
        {
            Velocity.y = JumpHeight;
        }
    }
}