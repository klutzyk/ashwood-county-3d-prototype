#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.Gameplay;

public enum GameplayNoiseCategory
{
	Unspecified,
	Sprint,
	Melee,
	Door,
}

public readonly record struct GameplayNoiseEvent(
	Vector3 WorldPosition,
	float AudibleRadius,
	GameplayNoiseCategory Category = GameplayNoiseCategory.Unspecified);

public static class GameplayNoise
{
	public static event Action<GameplayNoiseEvent>? Emitted;

	public static void Emit(
		Vector3 worldPosition,
		float audibleRadius,
		GameplayNoiseCategory category = GameplayNoiseCategory.Unspecified)
	{
		if (audibleRadius <= 0.0f)
		{
			return;
		}

		Emitted?.Invoke(new GameplayNoiseEvent(worldPosition, audibleRadius, category));
	}
}
