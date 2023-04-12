using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class FlyingCamera : Node3D
{
    [Export]
    public float FlySpeed;

    [Export]
    public float LookSensitivity = 0.3f;
    [Export]
    public float ControllerLookSensitivity = 5f;
    [Export]
    public float PunchDistance = 100;

    private float yaw;
    private float pitch;
    private World world;

    public override void _Ready()
    {
        world = GetParent<World>();
        base._Ready();
    }
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion m) {
            Vector2 d = m.Relative;
            yaw = (yaw - LookSensitivity*d.X) % 360;
            pitch = System.Math.Min(System.Math.Max(pitch-LookSensitivity*d.Y,-90),90);
            RotationDegrees = new Vector3(pitch,yaw,0);
        }
        base._Input(e);
    }

    public override void _Process(double delta)
    {
        move((float)delta);

        if (Input.IsActionJustPressed("punch")) {
            Vector3 dir = -Basis.Z*PunchDistance;
            BlockcastHit hit = world.Blockcast(GlobalPosition, dir);
            if (hit != null) {
                world.SetBlock(hit.BlockPos, null);
            }
        }
        if (Input.IsActionJustPressed("use")) {
            Vector3 dir = -Basis.Z*PunchDistance;
            BlockcastHit hit = world.Blockcast(GlobalPosition, dir);
            if (hit != null) {
                SphereShaper.Shape3D(world, hit.HitPos, 25);
            }
        }
        
        base._Process(delta);
    }
    private void move(float delta)
    {
        float look_x = Input.GetActionStrength("look_left") - Input.GetActionStrength("look_right");
        float look_y = Input.GetActionStrength("look_down") - Input.GetActionStrength("look_up");
        yaw = (yaw + ControllerLookSensitivity*look_x) % 360;
        pitch = System.Math.Min(System.Math.Max(pitch-ControllerLookSensitivity*look_y,-90),90);
        RotationDegrees = new Vector3(pitch,yaw,0);

        float x = Input.GetActionStrength("move_right")-Input.GetActionStrength("move_left");
        float y = Input.GetActionStrength("fly_up")-Input.GetActionStrength("fly_down");
        float z = Input.GetActionStrength("move_backward")-Input.GetActionStrength("move_forward");
        Vector3 dir = FlySpeed*delta*new Vector3(x,0,z);
        Vector3 globalDir = FlySpeed*delta*new Vector3(0,y,0);
        TranslateObjectLocal(dir);
        GlobalTranslate(globalDir);
    }
}
