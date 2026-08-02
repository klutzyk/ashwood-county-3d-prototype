#nullable enable

using System;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Verifies the temporary Main Street play boundary and the four hero-tree
/// trunk colliders structurally and against the live physics space.
/// </summary>
public partial class MainStreetWorldSafetyValidation : Node
{
	private const string ScenePath = "res://scenes/world/ashwood/main_street.tscn";
	private const string BoundaryPath = "Environment/PrototypeSafetyBoundary";
	private const string PresentationPath = "Environment/Presentation";
	private const float PositionTolerance = 0.005f;

	private readonly record struct BoundaryExpectation(
		string Name,
		Vector3 Position,
		Vector3 Size,
		Vector3 RayStart,
		Vector3 RayEnd);

	private readonly record struct TreeExpectation(
		string VisualName,
		string ColliderName,
		float Height,
		float Radius);

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ValidatePerimeterStructure(world);
			ValidateHeroTreeStructure(world);
			ValidateDensityFacadeStructure(world);
			ValidatePerimeterPhysics(world);
			ValidateHeroTreePhysics(world);
			ValidateDensityFacadePhysics(world);
			await ValidatePlayerContainment(world);
			await ValidateHeroTreeCharacterContact(world);
			await ValidateDensityFacadeCharacterContact(world);

			GD.Print("MAIN_STREET_WORLD_SAFETY_VALIDATION: PASS");
			QuitAfterManagedCleanup(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_WORLD_SAFETY_VALIDATION: FAIL - {exception}");
			QuitAfterManagedCleanup(1);
		}
	}

	private static void ValidatePerimeterStructure(Node3D world)
	{
		StaticBody3D boundary = world.GetNode<StaticBody3D>(BoundaryPath);
		Require((boundary.CollisionLayer & 1u) != 0,
			"prototype perimeter is visible to player, zombie, and LOS collision mask 1");
		Require(boundary.FindChildren("*", "MeshInstance3D", true, false).Count == 0,
			"prototype perimeter remains nonvisual");

		BoundaryExpectation[] expectations = GetBoundaryExpectations();
		CollisionShape3D[] shapes = boundary.GetChildren()
			.OfType<CollisionShape3D>()
			.ToArray();
		Require(shapes.Length == expectations.Length,
			"prototype perimeter contains exactly four wall shapes");

		foreach (BoundaryExpectation expected in expectations)
		{
			CollisionShape3D collision = boundary.GetNode<CollisionShape3D>(expected.Name);
			BoxShape3D? box = collision.Shape as BoxShape3D;
			Require(box is not null,
				$"{expected.Name} perimeter wall uses an efficient box shape");
			Require(IsNear(collision.Position, expected.Position) &&
				IsNear(box!.Size, expected.Size),
				$"{expected.Name} wall sits immediately outside the authored footprint " +
				$"(position {collision.Position}, size {box!.Size})");
			Require(!collision.Disabled,
				$"{expected.Name} perimeter wall is enabled");
		}

		Node3D player = world.GetNode<Node3D>("Gameplay/Player");
		Node3D bakery = world.GetNode<Node3D>("BakeryRoot");
		Node3D school = world.GetNode<Node3D>("Environment/AshwoodSchool");
		Node3D safePoint = world.GetNode<Node3D>("Environment/ReliefSafePoint");
		foreach ((string label, Node3D node) in new[]
		{
			("player spawn", player),
			("bakery interior", bakery),
			("school", school),
			("relief safe point", safePoint),
		})
		{
			Require(IsInsideAuthoredFootprint(node.GlobalPosition, 1.0f),
				$"{label} remains safely inside the temporary perimeter");
		}
	}

	private static void ValidateHeroTreeStructure(Node3D world)
	{
		Node3D presentation = world.GetNode<Node3D>(PresentationPath);
		Node3D collisionRoot = presentation.GetNode<Node3D>(
			"HeroTreeTrunkCollisions");
		Require(collisionRoot.GetChildCount() == 4,
			"exactly four hero-tree trunk bodies are authored");
		Require(collisionRoot.FindChildren("*", "MeshInstance3D", true, false).Count == 0,
			"hero-tree collision helpers remain nonvisual");

		foreach (TreeExpectation expected in GetTreeExpectations())
		{
			Node3D visual = presentation.GetNode<Node3D>(
				$"Trees/{expected.VisualName}");
			Require(visual.SceneFilePath ==
				"res://assets/environment/nature/ashwood_hero_tree_small_02.glb",
				$"{expected.VisualName} remains a raw optimized hero-tree instance");

			StaticBody3D body = collisionRoot.GetNode<StaticBody3D>(
				expected.ColliderName);
			CollisionShape3D collision = body.GetNode<CollisionShape3D>("Collision");
			CylinderShape3D? cylinder = collision.Shape as CylinderShape3D;
			Require(cylinder is not null,
				$"{expected.VisualName} lower trunk uses a cylinder collider");
			Require((body.CollisionLayer & 1u) != 0,
				$"{expected.VisualName} trunk participates in movement and LOS physics");
			Require(IsNear(body.GlobalPosition.X, visual.GlobalPosition.X) &&
				IsNear(body.GlobalPosition.Z, visual.GlobalPosition.Z),
				$"{expected.VisualName} trunk collider aligns with its visible base");
			Require(IsNear(cylinder!.Height, expected.Height) &&
				IsNear(cylinder.Radius, expected.Radius),
				$"{expected.VisualName} trunk dimensions match its scaled lower trunk");
			Require(IsNear(
				collision.GlobalPosition.Y - cylinder.Height * 0.5f,
				visual.GlobalPosition.Y),
				$"{expected.VisualName} trunk collider is grounded at the visual base");
			Require(!collision.Disabled,
				$"{expected.VisualName} trunk collision is enabled");
		}
	}

	private static void ValidatePerimeterPhysics(Node3D world)
	{
		StaticBody3D boundary = world.GetNode<StaticBody3D>(BoundaryPath);
		PhysicsDirectSpaceState3D space = world.GetWorld3D().DirectSpaceState;
		foreach (BoundaryExpectation expected in GetBoundaryExpectations())
		{
			RequireRayHits(
				space,
				expected.RayStart,
				expected.RayEnd,
				boundary,
				1u,
				$"{expected.Name} wall blocks live physics at the play edge");
		}
	}

	private static void ValidateHeroTreePhysics(Node3D world)
	{
		Node3D presentation = world.GetNode<Node3D>(PresentationPath);
		Node3D collisionRoot = presentation.GetNode<Node3D>(
			"HeroTreeTrunkCollisions");
		PhysicsDirectSpaceState3D space = world.GetWorld3D().DirectSpaceState;
		PlayerMeleeCombat combat = world.GetNode<PlayerMeleeCombat>(
			"Gameplay/Player/MeleeCombat");
		PrototypeZombie zombie = world.GetNode<PrototypeZombie>(
			"Gameplay/Zombies/MainStreetZombieCentral");
		Require((combat.HitCollisionMask & 1u) != 0 &&
			(zombie.VisionCollisionMask & 1u) != 0,
			"melee and zombie sight queries share the hero-trunk collision layer");

		foreach (TreeExpectation expected in GetTreeExpectations())
		{
			StaticBody3D body = collisionRoot.GetNode<StaticBody3D>(
				expected.ColliderName);
			CollisionShape3D collision = body.GetNode<CollisionShape3D>("Collision");
			Vector3 centre = collision.GlobalPosition;
			RequireRayHits(
				space,
				centre - Vector3.Right,
				centre + Vector3.Right,
				body,
				combat.HitCollisionMask,
				$"{expected.VisualName} trunk blocks live melee/vision LOS physics");
		}
	}

	private static void ValidateDensityFacadeStructure(Node3D world)
	{
		Node3D collisionRoot = world.GetNode<Node3D>(
			"Environment/WorldPolish/DensityPresentation/HistoricFrontage/FacadeCollision");
		StaticBody3D[] bodies = collisionRoot.GetChildren()
			.OfType<StaticBody3D>()
			.ToArray();
		Require(bodies.Length == 7,
			"all seven accessible density facades have solid authored volumes");
		foreach (StaticBody3D body in bodies)
		{
			CollisionShape3D collision = body.GetNode<CollisionShape3D>("Collision");
			Require(collision.Shape is BoxShape3D box && box.Size.X >= 7.5f &&
				box.Size.Y >= 5.6f && box.Size.Z >= 5.0f,
				$"{body.Name} uses one building-scale box instead of detailed collision");
			Require((body.CollisionLayer & 1u) != 0 &&
				IsInsideAuthoredFootprint(body.GlobalPosition, 0.5f),
				$"{body.Name} participates in movement physics inside the play perimeter");
		}
	}

	private static void ValidateDensityFacadePhysics(Node3D world)
	{
		Node3D collisionRoot = world.GetNode<Node3D>(
			"Environment/WorldPolish/DensityPresentation/HistoricFrontage/FacadeCollision");
		PhysicsDirectSpaceState3D space = world.GetWorld3D().DirectSpaceState;
		foreach (StaticBody3D body in collisionRoot.GetChildren().OfType<StaticBody3D>())
		{
			float side = Mathf.Sign(body.GlobalPosition.Z);
			BoxShape3D box = (BoxShape3D)body
				.GetNode<CollisionShape3D>("Collision").Shape;
			float[] sampleOffsets = [0.0f, -box.Size.X * 0.27f, box.Size.X * 0.27f];
			bool hitFacade = false;
			string actual = "none";
			foreach (float offset in sampleOffsets)
			{
				Vector3 from = new(body.GlobalPosition.X + offset, 2.0f, side * 7.7f);
				Vector3 to = new(body.GlobalPosition.X + offset, 2.0f, side * 10.2f);
				GodotObject? collider = RayCollider(space, from, to, 1u);
				actual = collider is Node node
					? $"{node.GetPath()} ({node.GetClass()})"
					: "none";
				if (collider == body)
				{
					hitFacade = true;
					break;
				}
			}
			Require(hitFacade,
				$"{body.Name} facade blocks live physics from the sidewalk side " +
				$"across centre and quarter-width probes (last actual {actual})");
		}
	}

	private async System.Threading.Tasks.Task ValidatePlayerContainment(Node3D world)
	{
		CharacterBody3D player = world.GetNode<CharacterBody3D>("Gameplay/Player");
		StaticBody3D boundary = world.GetNode<StaticBody3D>(BoundaryPath);
		player.SetProcess(false);
		player.SetPhysicsProcess(false);
		player.Velocity = Vector3.Zero;
		player.GlobalPosition = new Vector3(108.0f, 1.11f, 0.0f);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		KinematicCollision3D? collision = player.MoveAndCollide(
			new Vector3(5.0f, 0.0f, 0.0f));
		Require(collision is not null && collision.GetCollider() == boundary,
			"the real player capsule physically contacts the east safety wall");
		Require(player.GlobalPosition.X < 110.0f,
			"the real player capsule cannot move beyond the authored ground edge");
	}

	private async System.Threading.Tasks.Task ValidateHeroTreeCharacterContact(
		Node3D world)
	{
		StaticBody3D treeBody = world.GetNode<StaticBody3D>(
			$"{PresentationPath}/HeroTreeTrunkCollisions/North06Trunk");
		CharacterBody3D player = world.GetNode<CharacterBody3D>("Gameplay/Player");
		player.GlobalPosition = new Vector3(58.5f, 1.11f, -8.38f);
		player.Velocity = Vector3.Zero;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		KinematicCollision3D? playerContact = player.MoveAndCollide(
			new Vector3(-3.0f, 0.0f, 0.0f));
		Require(playerContact is not null && playerContact.GetCollider() == treeBody,
			"the real player capsule physically contacts the North06 hero trunk " +
			$"(actual {DescribeCollider(playerContact)})");

		player.GlobalPosition = new Vector3(0.0f, 1.11f, 0.0f);
		PrototypeZombie zombie = world.GetNode<PrototypeZombie>(
			"Gameplay/Zombies/MainStreetZombieCentral");
		zombie.SetProcess(false);
		zombie.SetPhysicsProcess(false);
		zombie.Velocity = Vector3.Zero;
		zombie.GlobalPosition = new Vector3(58.5f, 1.1f, -8.38f);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		KinematicCollision3D? zombieContact = zombie.MoveAndCollide(
			new Vector3(-3.0f, 0.0f, 0.0f));
		Require(zombieContact is not null && zombieContact.GetCollider() == treeBody,
			"the real zombie capsule physically contacts the North06 hero trunk " +
			$"(actual {DescribeCollider(zombieContact)})");
	}

	private async System.Threading.Tasks.Task ValidateDensityFacadeCharacterContact(
		Node3D world)
	{
		StaticBody3D facade = world.GetNode<StaticBody3D>(
			"Environment/WorldPolish/DensityPresentation/HistoricFrontage/" +
			"FacadeCollision/SouthEastFeedStore");
		BoxShape3D box = (BoxShape3D)facade
			.GetNode<CollisionShape3D>("Collision").Shape;
		CharacterBody3D player = world.GetNode<CharacterBody3D>("Gameplay/Player");
		float side = Mathf.Sign(facade.GlobalPosition.Z);
		float[] sampleOffsets = [0.0f, -box.Size.X * 0.3f, box.Size.X * 0.3f];
		bool contactedFacade = false;
		string actual = "none";
		foreach (float offset in sampleOffsets)
		{
			player.GlobalPosition = new Vector3(
				facade.GlobalPosition.X + offset,
				1.11f,
				side * 7.15f);
			player.Velocity = Vector3.Zero;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			KinematicCollision3D? contact = player.MoveAndCollide(
				new Vector3(0.0f, 0.0f, side * 4.0f));
			actual = DescribeCollider(contact);
			if (contact is not null && contact.GetCollider() == facade)
			{
				contactedFacade = true;
				break;
			}
		}
		Require(contactedFacade,
			"the real player capsule contacts the South East Feed Store facade " +
			$"across centre and quarter-width approaches (last actual {actual})");
	}

	private static string DescribeCollider(KinematicCollision3D? collision) =>
		collision?.GetCollider() is Node node
			? $"{node.GetPath()} ({node.GetClass()})"
			: "none";

	private static void RequireRayHits(
		PhysicsDirectSpaceState3D space,
		Vector3 from,
		Vector3 to,
		GodotObject expectedCollider,
		uint collisionMask,
		string message)
	{
		GodotObject? collider = RayCollider(space, from, to, collisionMask);
		string actual = collider is Node node
			? $"{node.GetPath()} ({node.GetClass()})"
			: "none";
		Require(collider == expectedCollider,
			$"{message} (actual {actual})");
	}

	private static GodotObject? RayCollider(
		PhysicsDirectSpaceState3D space,
		Vector3 from,
		Vector3 to,
		uint collisionMask)
	{
		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(from, to, collisionMask);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		Godot.Collections.Dictionary hit = space.IntersectRay(query);
		return hit.Count > 0 ? hit["collider"].AsGodotObject() : null;
	}

	private static BoundaryExpectation[] GetBoundaryExpectations() =>
	[
		new(
			"West",
			new Vector3(-110.5f, 2.0f, 0.0f),
			new Vector3(1.0f, 12.0f, 82.0f),
			new Vector3(-108.0f, 4.0f, 0.0f),
			new Vector3(-113.0f, 4.0f, 0.0f)),
		new(
			"East",
			new Vector3(110.5f, 2.0f, 0.0f),
			new Vector3(1.0f, 12.0f, 82.0f),
			new Vector3(108.0f, 4.0f, 0.0f),
			new Vector3(113.0f, 4.0f, 0.0f)),
		new(
			"North",
			new Vector3(0.0f, 2.0f, -40.5f),
			new Vector3(222.0f, 12.0f, 1.0f),
			new Vector3(0.0f, 4.0f, -38.0f),
			new Vector3(0.0f, 4.0f, -43.0f)),
		new(
			"South",
			new Vector3(0.0f, 2.0f, 40.5f),
			new Vector3(222.0f, 12.0f, 1.0f),
			new Vector3(0.0f, 4.0f, 38.0f),
			new Vector3(0.0f, 4.0f, 43.0f)),
	];

	private static TreeExpectation[] GetTreeExpectations() =>
	[
		new("North03", "North03Trunk", 3.65f, 0.32f),
		new("North06", "North06Trunk", 3.35f, 0.29f),
		new("South03", "South03Trunk", 3.6f, 0.31f),
		new("South06", "South06Trunk", 3.45f, 0.3f),
	];

	private static bool IsInsideAuthoredFootprint(
		Vector3 position,
		float margin) =>
		position.X > -110.0f + margin &&
		position.X < 110.0f - margin &&
		position.Z > -40.0f + margin &&
		position.Z < 40.0f - margin;

	private static bool IsNear(float first, float second) =>
		Mathf.IsEqualApprox(first, second) ||
		Mathf.Abs(first - second) <= PositionTolerance;

	private static bool IsNear(Vector3 first, Vector3 second) =>
		IsNear(first.X, second.X) &&
		IsNear(first.Y, second.Y) &&
		IsNear(first.Z, second.Z);

	private void QuitAfterManagedCleanup(int exitCode)
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GetTree().Quit(exitCode);
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
