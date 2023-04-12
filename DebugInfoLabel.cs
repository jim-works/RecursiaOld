using Godot;
using System.Collections.Generic;
using System;
using System.Text;

namespace Recursia;
public partial class DebugInfoLabel : Label
{
	public static readonly List<Func<Vector3,string>> Inputs = new();
	private readonly StringBuilder sb = new();

	public override void _Process(double delta)
	{
		sb.Clear();
		foreach (var func in Inputs)
		{
			sb.Append(func(Player.LocalPlayer.GlobalPosition));
			sb.Append('\n');
		}
		Text = sb.ToString();
	}
}