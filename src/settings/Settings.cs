using Godot;

namespace Recursia;
public partial class Settings : Node
{
    [Signal] public delegate void on_pauseEventHandler();
    [Signal] public delegate void on_unpauseEventHandler();

    public const int SAVE_FORMAT_VERSION = 1;

    public static bool Paused {get; set;}

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("toggle_fullscreen")) DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
        if (Input.IsActionJustPressed("toggle_debug") && DebugDraw.Singleton != null) DebugDraw.Singleton.Draw = !DebugDraw.Singleton.Draw;

        if (Input.IsActionJustPressed("pause"))
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                Paused = true;
                EmitSignal("on_pause");
            }
            else if (Input.MouseMode == Input.MouseModeEnum.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                Paused = false;
                EmitSignal("on_unpause");
            }
        }
        base._Process(delta);
    }
}