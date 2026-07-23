#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class DayNightFlashlightValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			WorldTime worldTime = world.GetNode<WorldTime>("WorldTime");
			DirectionalLight3D sun = world.GetNode<DirectionalLight3D>("DirectionalLight3D");
			Godot.Environment environment = world.GetNode<WorldEnvironment>("WorldEnvironment").Environment;
			PlayerFlashlight flashlight = world.GetNode<PlayerFlashlight>(
				"Player/CameraRig/SpringArm3D/Camera3D/Flashlight");
			Node3D player = world.GetNode<Node3D>("Player");

			worldTime.SetTimeOfDay(17.0f);
			float afternoonLight = sun.LightEnergy;
			float afternoonAmbient = environment.AmbientLightEnergy;
			float afternoonSky = environment.BackgroundEnergyMultiplier;
			worldTime.FullDayDurationSeconds = 2.0f;
			await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);

			Require(worldTime.CurrentHour >= 20.5f || worldTime.CurrentHour < 17.0f,
				"accelerated clock crosses from late afternoon into night");
			Require(sun.LightEnergy < afternoonLight, "directional light dims into night");
			Require(environment.AmbientLightEnergy < afternoonAmbient, "ambient light dims gradually");
			Require(environment.BackgroundEnergyMultiplier < afternoonSky, "sky brightness dims gradually");
			Require(environment.AmbientLightEnergy >= worldTime.NightAmbientEnergy,
				"night retains the configured minimum ambient visibility");

			Require(!flashlight.IsEnabled && !flashlight.ShadowEnabled, "flashlight starts off without shadows");
			flashlight._UnhandledInput(new InputEventAction
			{
				Action = "toggle_flashlight",
				Pressed = true,
			});
			Require(flashlight.IsEnabled, "F-toggle behavior enables the flashlight outdoors");
			Require(flashlight.GetParent() is Camera3D, "flashlight direction is inherited from the camera");
			player.GlobalPosition = new Vector3(-17.5f, 1.0f, 12.2f);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Require(flashlight.IsEnabled && flashlight.IsInsideTree(),
				"camera flashlight remains active inside the pharmacy");

			GD.Print("DAY_NIGHT_FLASHLIGHT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"DAY_NIGHT_FLASHLIGHT_VALIDATION: FAIL - {exception.Message}");
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
