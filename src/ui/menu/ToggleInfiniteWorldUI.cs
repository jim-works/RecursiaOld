using Godot;
using System;

public class ToggleInfiniteWorldUI : CheckButton
{
    public void OnToggled(bool toggled)
    {
        GlobalConfig.UseInfiniteWorlds = toggled;
    }
}
