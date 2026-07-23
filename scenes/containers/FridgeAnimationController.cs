#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class FridgeAnimationController : Node
{
	[Export] public AnimationPlayer? AnimationPlayer { get; set; }

	[Export] public string OpenAnimationName { get; set; } = "Take 001";

	private SearchableContainer _container = null!;
	private bool _isOpen;

	public override void _Ready()
	{
		_container = GetParent<SearchableContainer>();
		_container.SearchCompleted += Open;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_container))
		{
			_container.SearchCompleted -= Open;
		}
	}

	private void Open()
	{
		if (_isOpen || AnimationPlayer == null)
		{
			return;
		}

		if (!AnimationPlayer.HasAnimation(OpenAnimationName))
		{
			GD.PushWarning(
				$"Fridge animation '{OpenAnimationName}' was not found."
			);
			return;
		}

		AnimationPlayer.Play(OpenAnimationName);
		_isOpen = true;
	}
}
