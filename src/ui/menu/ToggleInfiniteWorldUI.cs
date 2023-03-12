using Godot;
using System;

public partial class ToggleInfiniteWorldUI : CheckButton
{
    public void OnToggled(bool toggled)
    {
        GlobalConfig.UseInfiniteWorlds = toggled;
    }
}
