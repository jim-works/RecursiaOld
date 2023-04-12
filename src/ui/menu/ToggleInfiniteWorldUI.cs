using Godot;
using System;

namespace Recursia;
public partial class ToggleInfiniteWorldUI : CheckButton
{
    public static void OnToggled(bool toggled)
    {
        GlobalConfig.UseInfiniteWorlds = toggled;
    }
}
