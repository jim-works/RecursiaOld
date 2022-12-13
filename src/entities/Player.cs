using Godot;

public class Player : PhysicsObject
{
    [Export]
    public float Reach = 100;
    [Export]
    public float MoveSpeed = 10;
    [Export]
    public float JumpHeight = 10;
    [Export]
    public Vector3 CameraOffset = new Vector3(0,0.7f,0);

    public override void _Ready()
    {
        World.Singleton.ChunkLoaders.Add(this);
        Size = new Vector3(0.7f,1.8f,0.7f);
        base._Ready();
    }

    public override void _Process(float delta)
    {
        move(delta);
        if (Input.IsActionJustPressed("pause")) {
            if (Input.GetMouseMode() == Input.MouseMode.Captured) Input.SetMouseMode(Input.MouseMode.Visible);
            else if (Input.GetMouseMode() == Input.MouseMode.Visible) Input.SetMouseMode(Input.MouseMode.Captured);
        }

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
        BlockcastHit hit = World.Singleton.Blockcast(Position+CameraOffset, dir*Reach);
        if (hit != null) {
            SphereShaper.Shape(World.Singleton, hit.HitPos, 25);
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