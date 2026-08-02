#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class FootstepAudioValidation : Node
{
	public override async void _Ready()
	{
		Exception? failure = null;
		Node3D world = null!;
		StaticBody3D ground = null!;
		ThirdPersonPlayer player = null!;
		PlayerFootstepFeedback footsteps = null!;
		try
		{
			world = new Node3D { Name = "FootstepTestWorld" };
			ground = new StaticBody3D
			{
				Name = "Ground",
				Position = new Vector3(0.0f, -0.25f, 0.0f),
			};
			ground.AddChild(new CollisionShape3D
			{
				Shape = new BoxShape3D { Size = new Vector3(30.0f, 0.5f, 30.0f) },
			});
			player = GD.Load<PackedScene>(
				"res://scenes/player/third_person_player.tscn").Instantiate<ThirdPersonPlayer>();
			player.Position = new Vector3(0.0f, 1.05f, 0.0f);
			world.AddChild(ground);
			world.AddChild(player);
			AddChild(world);

			for (int frame = 0; frame < 8; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			Require(player.IsOnFloor(), "player settles on the test surface");

			footsteps = player.GetNode<PlayerFootstepFeedback>("FootstepFeedback");
			Require(footsteps.FootstepSounds.Count == 10,
				"the player carries ten non-repeating CC0 footstep variations");
			Require(footsteps.WalkStepDistance >= 1.2f && footsteps.WalkStepDistance <= 1.6f,
				"walking cadence uses believable distance-based spacing");

			Vector3 start = player.GlobalPosition;
			Input.ActionPress("move_forward");
			for (int frame = 0; frame < 90; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			Input.ActionRelease("move_forward");
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Vector3 horizontalTravel = player.GlobalPosition - start;
			horizontalTravel.Y = 0.0f;
			Require(horizontalTravel.Length() >= 4.0f,
				"real player locomotion drives the validation sample");
			Require(footsteps.PlayedStepCount >= 3,
				"walking distance produces a stable sequence of footstep events");
			Require(player.GetNode<AudioStreamPlayer3D>("FootstepFeedback/StepA").Stream is not null &&
				player.GetNode<AudioStreamPlayer3D>("FootstepFeedback/StepB").Stream is not null,
				"dual emitters alternate so adjacent steps can overlap naturally");
			Require(player.GetNode<AudioStreamPlayer3D>("FootstepFeedback/StepA").Bus == "Effects" &&
				player.GetNode<AudioStreamPlayer3D>("FootstepFeedback/StepB").Bus == "Effects" &&
				player.GetNode<PlayerMeleeAudioFeedback>("MeleeCombat/AudioFeedback").Bus == "Effects",
				"footstep and melee transients follow the Effects volume setting");

		}
		catch (Exception exception)
		{
			failure = exception;
		}

		Input.ActionRelease("move_forward");
		try
		{
			if (world is not null && IsInstanceValid(world))
			{
				world.QueueFree();
			}
			footsteps = null!;
			player = null!;
			ground = null!;
			world = null!;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}
		catch (Exception cleanupException)
		{
			failure ??= cleanupException;
		}

		if (failure is null)
		{
			GD.Print("FOOTSTEP_AUDIO_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		else
		{
			GD.PushError($"FOOTSTEP_AUDIO_VALIDATION: FAIL - {failure.Message}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
