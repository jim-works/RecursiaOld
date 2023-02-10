using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class RotatingCamera : Spatial
{

    [Export]
    public float LookSensitivity = 0.3f;
    [Export]
    public float ControllerLookSensitivity = 5f;
    [Export]
    public bool RotateParentYaw = true;
    [Export]
    public bool RotateParentPitch = false;

    public static RotatingCamera Singleton;

    private float yaw;
    private float pitch;

    public override void _Ready()
    {
        Singleton = this;
        Input.SetMouseMode(Input.MouseMode.Captured);
        base._Ready();
    }
    public override void _Input(InputEvent e)
    {
        if (!Settings.Paused && e is InputEventMouseMotion m) {
            Vector2 d = m.Relative;
            yaw = (yaw - LookSensitivity*d.x) % 360;
            pitch = System.Math.Min(System.Math.Max(pitch-LookSensitivity*d.y,-90),90);
            RotationDegrees = new Vector3(pitch,yaw,0);
        }
        base._Input(e);
    }

    public override void _Process(float delta)
    {
        if (Settings.Paused) {
            base._Process(delta);
            return;
        }
        Player parent = GetParent<Player>();
        float look_x = Input.GetActionStrength("look_left") - Input.GetActionStrength("look_right");
        float look_y = Input.GetActionStrength("look_down") - Input.GetActionStrength("look_up");
        yaw = (yaw + ControllerLookSensitivity*look_x) % 360;
        pitch = System.Math.Min(System.Math.Max(pitch-ControllerLookSensitivity*look_y,-90),90);
        if (RotateParentPitch && RotateParentYaw)
        {
            parent.RotationDegrees = new Vector3(pitch,yaw,parent.RotationDegrees.z);
            RotationDegrees = new Vector3(0,0,RotationDegrees.z);
        }
        else if (RotateParentPitch)
        {
            parent.RotationDegrees = new Vector3(pitch, parent.RotationDegrees.y, parent.RotationDegrees.z);
            RotationDegrees = new Vector3(0, RotationDegrees.y, RotationDegrees.z);
        }
        else if (RotateParentYaw)
        {
            parent.RotationDegrees = new Vector3(parent.RotationDegrees.x, yaw, parent.RotationDegrees.z);
            RotationDegrees = new Vector3(RotationDegrees.x, 0, RotationDegrees.z); 
        }
        else
        {
            RotationDegrees = new Vector3(pitch,yaw,RotationDegrees.z);
        }
        if (Input.IsActionJustPressed("punch")) {
            parent.Punch(-GlobalTransform.basis.z);
        }
        if (Input.IsActionJustPressed("use")) {
            parent.Use(-GlobalTransform.basis.z);
        }  
        
        base._Process(delta);
    }

}
