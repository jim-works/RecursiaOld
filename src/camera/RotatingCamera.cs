using Godot;

namespace Recursia
{
    public partial class RotatingCamera : Node3D
    {
        [Export]
        public float LookSensitivity = 0.3f;
        [Export]
        public float ControllerLookSensitivity = 5f;
        [Export]
        public bool RotateParentYaw = true;
        [Export]
        public bool RotateParentPitch;

        public static RotatingCamera? Singleton { get; private set; }

        private float yaw;
        private float pitch;

        public override void _Ready()
        {
            Singleton = this;
            base._Ready();
        }
        public override void _Input(InputEvent @event)
        {
            if (!Settings.Paused && @event is InputEventMouseMotion m)
            {
                Vector2 d = m.Relative;
                yaw = (yaw - (LookSensitivity * d.X)) % 360;
                pitch = System.Math.Min(System.Math.Max(pitch - (LookSensitivity * d.Y), -90), 90);
                RotationDegrees = new Vector3(pitch, yaw, 0);
            }
            base._Input(@event);
        }

        public override void _Process(double delta)
        {
            if (Settings.Paused)
            {
                base._Process(delta);
                return;
            }
            Player parent = GetParent<Player>();
            float look_x = Input.GetActionStrength("look_left") - Input.GetActionStrength("look_right");
            float look_y = Input.GetActionStrength("look_down") - Input.GetActionStrength("look_up");
            yaw = (yaw + (ControllerLookSensitivity * look_x)) % 360;
            pitch = System.Math.Min(System.Math.Max(pitch - (ControllerLookSensitivity * look_y), -90), 90);
            if (RotateParentPitch && RotateParentYaw)
            {
                parent.RotationDegrees = new Vector3(pitch, yaw, parent.RotationDegrees.Z);
                RotationDegrees = new Vector3(0, 0, RotationDegrees.Z);
            }
            else if (RotateParentPitch)
            {
                parent.RotationDegrees = new Vector3(pitch, parent.RotationDegrees.Y, parent.RotationDegrees.Z);
                RotationDegrees = new Vector3(0, RotationDegrees.Y, RotationDegrees.Z);
            }
            else if (RotateParentYaw)
            {
                parent.RotationDegrees = new Vector3(parent.RotationDegrees.X, yaw, parent.RotationDegrees.Z);
                RotationDegrees = new Vector3(RotationDegrees.X, 0, RotationDegrees.Z);
            }
            else
            {
                RotationDegrees = new Vector3(pitch, yaw, RotationDegrees.Z);
            }
            if (Input.IsActionJustPressed("punch"))
            {
                parent.Punch(-GlobalTransform.Basis.Z);
            }
            if (Input.IsActionJustPressed("use"))
            {
                parent.Use(-GlobalTransform.Basis.Z);
            }

            base._Process(delta);
        }
    }
}