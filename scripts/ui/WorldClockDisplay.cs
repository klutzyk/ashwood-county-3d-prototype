#nullable enable

using Godot;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.UI;

public partial class WorldClockDisplay : Label
{
	[Export] public NodePath WorldTimePath { get; set; } = new("../../WorldTime");

	private WorldTime _worldTime = null!;

	public override void _Ready()
	{
		_worldTime = GetNode<WorldTime>(WorldTimePath);
		_worldTime.TimeChanged += UpdateClock;
		int totalMinutes = Mathf.FloorToInt(_worldTime.CurrentHour * 60.0f);
		UpdateClock((totalMinutes / 60) % 24, totalMinutes % 60);
	}

	private void UpdateClock(int hour, int minute)
	{
		Text = $"{hour:00}:{minute:00}";
	}
}
