using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class FlyingCamera : Spatial
{
    [Export]
    public float FlySpeed;

    [Export]
    public float LookSensitivity = 0.3f;

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
        if (e is InputEventKey k) {
            if (k.Scancode == (uint)KeyList.Escape) {
                if (Input.GetMouseMode() == Input.MouseMode.Captured) Input.SetMouseMode(Input.MouseMode.Visible);
                else if (Input.GetMouseMode() == Input.MouseMode.Visible) Input.SetMouseMode(Input.MouseMode.Captured);
            }
        }
        base._Input(e);
    }

    public override void _Process(float delta)
    {
        float x = (Input.IsPhysicalKeyPressed((int)KeyList.D) ? 1 : 0)-(Input.IsPhysicalKeyPressed((int)KeyList.A) ? 1 : 0);
        float y = (Input.IsPhysicalKeyPressed((int)KeyList.Space) ? 1 : 0)-(Input.IsPhysicalKeyPressed((int)KeyList.Shift) ? 1 : 0);
        float z = (Input.IsPhysicalKeyPressed((int)KeyList.S) ? 1 : 0)-(Input.IsPhysicalKeyPressed((int)KeyList.W) ? 1 : 0);
        Vector3 dir = FlySpeed*delta*new Vector3(x,0,z);
        Vector3 globalDir = FlySpeed*delta*new Vector3(0,y,0);
        
        //Godot.GD.Print($"Verts: {Performance.GetMonitor(Performance.Monitor.RenderVerticesInFrame)}");

        TranslateObjectLocal(dir);
        GlobalTranslate(globalDir);
        base._Process(delta);
    }
}
