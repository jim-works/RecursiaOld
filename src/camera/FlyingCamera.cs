using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class FlyingCamera : Spatial
{
    [Export]
    public float FlySpeed;

    [Export]
    public float LookSensitivity = 0.3f;
    [Export]
    public float PunchDistance = 10;

    private float yaw;
    private float pitch;

    public override void _Ready()
    {
        Input.SetMouseMode(Input.MouseMode.Captured);
        base._Ready();
    }
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion m) {
            Vector2 d = m.Relative;
            yaw = (yaw - LookSensitivity*d.x) % 360;
            pitch = System.Math.Min(System.Math.Max(pitch-LookSensitivity*d.y,-90),90);
            RotationDegrees = new Vector3(pitch,yaw,0);
        }
        base._Input(e);
    }

    public override void _Process(float delta)
    {
        move(delta);        

        if (Input.IsActionJustPressed("pause")) {
            if (Input.GetMouseMode() == Input.MouseMode.Captured) Input.SetMouseMode(Input.MouseMode.Visible);
            else if (Input.GetMouseMode() == Input.MouseMode.Visible) Input.SetMouseMode(Input.MouseMode.Captured);
        }

        if (Input.IsActionJustPressed("punch")) {
            GD.Print(Transform.basis.z);
            Vector3 dir = -Transform.basis.z*PunchDistance;
            RaycastHit hit = World.Singleton.Raycast(Transform.origin, dir);
            if (hit != null) {
                World.Singleton.SetBlock(hit.BlockPos, null);
                GD.Print($"Block hit at {hit.BlockPos}");
            }
        }
        //Godot.GD.Print($"Verts: {Performance.GetMonitor(Performance.Monitor.RenderVerticesInFrame)}");

        
        base._Process(delta);
    }
    private void move(float delta)
    {
        float x = Input.GetActionStrength("move_right")-Input.GetActionStrength("move_left");
        float y = Input.GetActionStrength("fly_up")-Input.GetActionStrength("fly_down");
        float z = Input.GetActionStrength("move_backward")-Input.GetActionStrength("move_forward");
        Vector3 dir = FlySpeed*delta*new Vector3(x,0,z);
        Vector3 globalDir = FlySpeed*delta*new Vector3(0,y,0);
        TranslateObjectLocal(dir);
        GlobalTranslate(globalDir);
    }
}
