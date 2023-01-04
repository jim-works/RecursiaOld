using Godot;

public class Settings : Node
{
    [Signal] public delegate void on_pause();
    [Signal] public delegate void on_unpause();

    public static bool Paused = false;

    public override void _Process(float delta)
    {
        if (Input.IsActionJustPressed("toggle_fullscreen")) OS.WindowFullscreen = !OS.WindowFullscreen;

        if (Input.IsActionJustPressed("pause"))
        {
            if (Input.GetMouseMode() == Input.MouseMode.Captured)
            {
                Input.SetMouseMode(Input.MouseMode.Visible);
                Paused = true;
                EmitSignal("on_pause");
            }
            else if (Input.GetMouseMode() == Input.MouseMode.Visible)
            {
                Input.SetMouseMode(Input.MouseMode.Captured);
                Paused = false;
                EmitSignal("on_unpause");
            }
        }
        base._Process(delta);
    }
}