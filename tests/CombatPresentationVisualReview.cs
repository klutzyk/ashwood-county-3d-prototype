#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class CombatPresentationVisualReview : Node
{
	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(
				"res://scenes/world/ashwood/main_street.tscn").Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Gameplay/Player");
			PlayerMeleeCombat combat = player.GetNode<PlayerMeleeCombat>("MeleeCombat");
			combat.HitMoment = 0.23f;
			combat.TargetAssistArcDegrees = 180.0f;
			combat.TargetTrackingArcDegrees = 180.0f;
			PrototypeZombie target =
				world.GetNode<PrototypeZombie>("Gameplay/Zombies/MainStreetZombieCentral");
			foreach (Node child in world.GetNode("Gameplay/Zombies").GetChildren())
			{
				if (child is PrototypeZombie zombie && zombie != target)
				{
					zombie.SetAlive(false);
				}
			}

			player.SetPhysicsProcess(false);
			Vector3 fightDirection = new Vector3(-0.58f, 0.0f, -0.815f).Normalized();
			player.GlobalPosition = new Vector3(-34.5f, 1.0f, 1.8f);
			target.GlobalPosition = player.GlobalPosition +
				(fightDirection * 0.7f) + new Vector3(0.0f, -0.1f, 0.0f);
			player.LookAt(target.GlobalPosition, Vector3.Up, true);
			target.LookAt(player.GlobalPosition, Vector3.Up, true);
			Node3D cameraRig = player.GetNode<Node3D>("CameraRig");
			cameraRig.GlobalPosition = player.GlobalPosition + Vector3.Up * player.CameraHeight;
			Vector3 cameraLookDirection = fightDirection.Rotated(
				Vector3.Up,
				Mathf.DegToRad(25.0f));
			cameraRig.LookAt(
				cameraRig.GlobalPosition + cameraLookDirection,
				Vector3.Up);
			SpringArm3D springArm = player.GetNode<SpringArm3D>("CameraRig/SpringArm3D");
			springArm.SpringLength = 2.45f;
			Camera3D camera = springArm.GetNode<Camera3D>("Camera3D");
			camera.Fov = 61.0f;
			target.DetectionRadius = 0.0f;
			target.MoveSpeed = 0.0f;
			target.WanderSpeed = 0.0f;
			target.InvestigationSpeed = 0.0f;
			target.SeparationStrength = 0.0f;
			target.SetGameplayNoiseResponseEnabled(false);
			CanvasLayer? gameplayHud = world.GetNodeOrNull<CanvasLayer>(
				"Gameplay/GameplayHUD");
			if (gameplayHud is not null)
			{
				gameplayHud.Visible = false;
			}

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/combat_presentation_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
			await WaitFrames(8);
			int confirmedTargets = 0;
			combat.HitConfirmed += targetCount => confirmedTargets = targetCount;
			if (!combat.TryAttack())
			{
				throw new InvalidOperationException("review swing could not start");
			}
			if (!combat.HasAssistedTarget)
			{
				throw new InvalidOperationException(
					"review camera did not acquire the staged combat target");
			}

			await WaitForAttackProgress(combat, 0.08f, 90, "windup");
			float windupProgress = combat.AttackProgress;
			await Capture(Path.Combine(outputDirectory, "01_windup.png"), 1);
			for (int frame = 0; frame < 90 && confirmedTargets == 0; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			if (confirmedTargets == 0)
			{
				throw new InvalidOperationException(
					"review swing did not confirm a target within 90 frames " +
					$"(progress={combat.AttackProgress:0.00}, " +
					$"contact_gap={combat.LastHitAttemptContactGap:0.000}, " +
					$"segment={combat.WeaponStrikeSegmentLength:0.000})");
			}
			if (!combat.IsConfirmedHitPaused ||
				target.CurrentStateName != "Staggered" ||
				target.ImpactPresentationCount != 1 ||
				combat.WeaponStrikeDistanceTo(
					target.LastMeleeContactWorldPosition) > 0.22f)
			{
				throw new InvalidOperationException(
					"contact frame is missing hit-stop, stagger, or localized impact presentation " +
					$"(paused={combat.IsConfirmedHitPaused}, state={target.CurrentStateName}, " +
					$"impacts={target.ImpactPresentationCount}, " +
					$"weapon_gap={combat.WeaponStrikeDistanceTo(target.LastMeleeContactWorldPosition):0.000})");
			}
			float contactProgress = combat.AttackProgress;
			await Capture(Path.Combine(outputDirectory, "02_contact.png"), 1);
			await WaitForAttackProgress(combat, 0.78f, 120, "recovery");
			float recoveryProgress = combat.AttackProgress;
			await Capture(Path.Combine(outputDirectory, "03_recovery.png"), 1);

			if (!(windupProgress < contactProgress &&
				contactProgress < recoveryProgress &&
				combat.SwingTrailPointCount == 0))
			{
				throw new InvalidOperationException(
					"capture phases are not ordered or the swing ribbon survived into recovery");
			}

			GD.Print(
				$"COMBAT_PRESENTATION_VISUAL_REVIEW: {outputDirectory} " +
				$"windup={windupProgress:0.00} contact={contactProgress:0.00} " +
				$"recovery={recoveryProgress:0.00}");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"COMBAT_PRESENTATION_VISUAL_REVIEW: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private async Task WaitForAttackProgress(
		PlayerMeleeCombat combat,
		float expectedProgress,
		int maximumFrames,
		string phaseName)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (combat.IsAttacking && combat.AttackProgress >= expectedProgress)
			{
				return;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		throw new InvalidOperationException(
			$"review swing never reached the {phaseName} phase");
	}

	private async Task Capture(string path, int settleFrames)
	{
		await WaitFrames(Mathf.Max(settleFrames, 0));
		Image image = GetViewport().GetTexture().GetImage();
		if (image.IsEmpty())
		{
			throw new InvalidOperationException("rendered combat capture is empty");
		}
		Error error = image.SavePng(path);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"could not save {path}: {error}");
		}
	}

	private async Task WaitFrames(int frameCount)
	{
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}
}
