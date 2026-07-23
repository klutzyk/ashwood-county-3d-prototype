#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AtmosphereAudioValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			WorldTime worldTime = world.GetNode<WorldTime>("WorldTime");
			AtmosphereAudio atmosphere = world.GetNode<AtmosphereAudio>("AtmosphereAudio");
			AudioStreamPlayer wind = atmosphere.GetNode<AudioStreamPlayer>("Wind");
			AudioStreamPlayer groans = atmosphere.GetNode<AudioStreamPlayer>("ZombieGroans");
			AudioStreamPlayer insects = atmosphere.GetNode<AudioStreamPlayer>("DayInsects");
			AudioStreamPlayer crickets = atmosphere.GetNode<AudioStreamPlayer>("NightCrickets");
			worldTime.SetProcess(false);
			atmosphere.SetProcess(false);

			Require(atmosphere.MinimumGroanInterval >= 10.0f &&
				atmosphere.MaximumGroanInterval > atmosphere.MinimumGroanInterval,
				"intermittent groans use sensible randomized delays");
			Require(atmosphere.AmbienceHeadroomDb <= -1.0f,
				"ambient layers retain headroom when they overlap");
			Require(wind.Bus == "Ambient" && groans.Bus == "Ambient" &&
				insects.Bus == "Ambient" && crickets.Bus == "Ambient" &&
				AudioServer.GetBusSend(AudioServer.GetBusIndex("Ambient")) == "Master",
				"ambient control remains routed through the authoritative Master bus");

			worldTime.SetTimeOfDay(12.0f);
			for (int step = 0; step < 10; step++)
			{
				atmosphere._Process(0.2);
			}
			Require(atmosphere.DaylightBlend > 0.95f &&
				insects.VolumeDb > crickets.VolumeDb,
				"day mix favours insects and suppresses crickets");

			worldTime.SetTimeOfDay(0.0f);
			atmosphere._Process(0.1);
			Require(atmosphere.DaylightBlend > 0.0f &&
				atmosphere.TargetDaylightBlend < 0.01f,
				"large time changes begin a smooth ambience transition");
			for (int step = 0; step < 15; step++)
			{
				atmosphere._Process(0.1);
			}
			Require(atmosphere.DaylightBlend < 0.01f &&
				crickets.VolumeDb > insects.VolumeDb,
				"night mix favours crickets after the crossfade");
			Require(wind.VolumeDb < -25.0f && groans.VolumeDb < -30.0f,
				"wind and groans remain below the continuous wildlife bed");

			GD.Print("ATMOSPHERE_AUDIO_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ATMOSPHERE_AUDIO_VALIDATION: FAIL - {exception.Message}");
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
