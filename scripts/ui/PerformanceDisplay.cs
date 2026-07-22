using Godot;

namespace AshwoodCounty3DPrototype.UI;

public partial class PerformanceDisplay : Label
{
	public override void _Process(double delta)
	{
		Text = $"FPS: {Engine.GetFramesPerSecond()}";
	}
}
