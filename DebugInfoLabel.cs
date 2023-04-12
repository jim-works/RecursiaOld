using Godot;
using System.Collections.Generic;
using System;
using System.Text;

public partial class DebugInfoLabel : Label
{
	public static List<Func<Vector3,string>> Inputs = new();	
	private StringBuilder sb = new();

	public override void _Process(double delta)
	{
		sb.Clear();
		foreach (var func in Inputs)
		{
			sb.Append(func(Player.LocalPlayer.GlobalPosition));
			sb.Append("\n");
		}
		Text = sb.ToString();
	}
}