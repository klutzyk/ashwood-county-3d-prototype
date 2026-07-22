#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.Zombies;

public partial class PrototypeZombie : CharacterBody3D
{
	[Export] public NodePath PlayerPath { get; set; } = new("../../Player");
	[Export] public float DetectionRadius { get; set; } = 12.0f;
	[Export] public float FieldOfViewDegrees { get; set; } = 100.0f;
	[Export] public float LostSightGracePeriod { get; set; } = 1.25f;
	[Export] public float EyeHeight { get; set; } = 1.45f;
	[Export] public float PlayerTargetHeight { get; set; } = 0.75f;
	[Export(PropertyHint.Layers3DPhysics)] public uint VisionCollisionMask { get; set; } = 1;
	[Export] public float AttackDistance { get; set; } = 1.6f;
	[Export] public float MoveSpeed { get; set; } = 0.36f;
	[Export] public float Acceleration { get; set; } = 8.0f;
	[Export] public float Gravity { get; set; } = 24.0f;
	[Export] public float TurnSpeed { get; set; } = 7.0f;
	[Export] public float PathUpdateInterval { get; set; } = 0.35f;

	private const string SourceAnimationName = "mixamo_com";
	private const string IdleAnimationName = "Idle";
	private const string WalkAnimationName = "Walk";
	private const string AttackAnimationName = "Attack";
	private const string IdleAnimationPath = "res://assets/characters/zombies/zombie idle.fbx";
	private const string WalkAnimationPath = "res://assets/characters/zombies/zombie walk.fbx";
	private const string AttackAnimationPath = "res://assets/characters/zombies/zombie attack.fbx";
	private const float AnimationBlendTime = 0.2f;

	private enum BehaviourState
	{
		Idle,
		Chasing,
		Attacking,
	}

	private NavigationAgent3D _navigationAgent = null!;
	private AnimationPlayer _animationPlayer = null!;
	private Node3D _player = null!;
	private BehaviourState _state = BehaviourState.Idle;
	private float _pathUpdateElapsed;
	private float _timeSincePlayerVisible = float.PositiveInfinity;
	private Vector3 _lastKnownPlayerPosition;

	public string CurrentStateName => _state.ToString();

	public override void _Ready()
	{
		_player = GetNode<Node3D>(PlayerPath);
		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationPlayer = FindDescendant<AnimationPlayer>(this)
			?? throw new InvalidOperationException("Zombie model is missing an AnimationPlayer.");

		ConfigureAnimations();
		_navigationAgent.TargetDesiredDistance = Mathf.Max(AttackDistance - 0.3f, 0.5f);
		_pathUpdateElapsed = PathUpdateInterval;
		PlayStateAnimation();
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaTime = (float)delta;
		float distanceToPlayer = HorizontalDistanceTo(_player.GlobalPosition);
		bool canSeePlayer = CanSeePlayer(distanceToPlayer);
		UpdatePlayerAwareness(canSeePlayer, deltaTime);
		BehaviourState nextState = DetermineState(distanceToPlayer, canSeePlayer);
		SetState(nextState);

		Vector3 movementDirection = _state == BehaviourState.Chasing
			? GetNavigationDirection(deltaTime)
			: Vector3.Zero;

		ApplyHorizontalMovement(movementDirection, deltaTime);
		ApplyGravity(deltaTime);

		Vector3 facingDirection = _state == BehaviourState.Attacking
			? HorizontalDirectionTo(_player.GlobalPosition)
			: movementDirection;
		RotateToward(facingDirection, deltaTime);

		MoveAndSlide();
	}

	private BehaviourState DetermineState(float distanceToPlayer, bool canSeePlayer)
	{
		if (!canSeePlayer &&
			(_state == BehaviourState.Idle ||
			_timeSincePlayerVisible > LostSightGracePeriod ||
			HorizontalDistanceTo(_lastKnownPlayerPosition) <= _navigationAgent.TargetDesiredDistance))
		{
			return BehaviourState.Idle;
		}

		return canSeePlayer && distanceToPlayer <= AttackDistance
			? BehaviourState.Attacking
			: BehaviourState.Chasing;
	}

	private void UpdatePlayerAwareness(bool canSeePlayer, float delta)
	{
		if (canSeePlayer)
		{
			_timeSincePlayerVisible = 0.0f;
			_lastKnownPlayerPosition = _player.GlobalPosition;
			return;
		}

		_timeSincePlayerVisible += delta;
	}

	private bool CanSeePlayer(float distanceToPlayer)
	{
		if (distanceToPlayer > DetectionRadius || distanceToPlayer <= 0.001f)
		{
			return false;
		}

		Vector3 directionToPlayer = HorizontalDirectionTo(_player.GlobalPosition);
		Vector3 forward = GlobalBasis.Z.Normalized();
		float minimumFacingDot = Mathf.Cos(Mathf.DegToRad(FieldOfViewDegrees * 0.5f));
		if (forward.Dot(directionToPlayer) < minimumFacingDot)
		{
			return false;
		}

		Vector3 rayStart = GlobalPosition + Vector3.Up * EyeHeight;
		Vector3 rayEnd = _player.GlobalPosition + Vector3.Up * PlayerTargetHeight;
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayStart, rayEnd);
		query.CollisionMask = VisionCollisionMask;
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

		Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
		return hit.Count > 0 && hit["collider"].AsGodotObject() == _player;
	}

	private Vector3 GetNavigationDirection(float delta)
	{
		_pathUpdateElapsed += delta;
		Rid navigationMap = _navigationAgent.GetNavigationMap();
		if (!navigationMap.IsValid || NavigationServer3D.MapGetIterationId(navigationMap) == 0)
		{
			return Vector3.Zero;
		}

		if (_pathUpdateElapsed >= PathUpdateInterval)
		{
			_navigationAgent.TargetPosition = _lastKnownPlayerPosition;
			_pathUpdateElapsed = 0.0f;
		}

		if (_navigationAgent.IsNavigationFinished())
		{
			return Vector3.Zero;
		}

		return HorizontalDirectionTo(_navigationAgent.GetNextPathPosition());
	}

	private void ApplyHorizontalMovement(Vector3 direction, float delta)
	{
		Vector3 velocity = Velocity;
		Vector3 targetVelocity = direction * MoveSpeed;
		velocity.X = Mathf.MoveToward(velocity.X, targetVelocity.X, Acceleration * delta);
		velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocity.Z, Acceleration * delta);
		Velocity = velocity;
	}

	private void ApplyGravity(float delta)
	{
		Vector3 velocity = Velocity;
		if (IsOnFloor())
		{
			velocity.Y = 0.0f;
		}
		else
		{
			velocity.Y -= Gravity * delta;
		}

		Velocity = velocity;
	}

	private void RotateToward(Vector3 direction, float delta)
	{
		if (direction.IsZeroApprox())
		{
			return;
		}

		float targetRotation = Mathf.Atan2(direction.X, direction.Z);
		Rotation = new Vector3(0.0f, Mathf.LerpAngle(Rotation.Y, targetRotation, TurnSpeed * delta), 0.0f);
	}

	private Vector3 HorizontalDirectionTo(Vector3 target)
	{
		Vector3 direction = target - GlobalPosition;
		direction.Y = 0.0f;
		return direction.IsZeroApprox() ? Vector3.Zero : direction.Normalized();
	}

	private float HorizontalDistanceTo(Vector3 target)
	{
		Vector2 position = new(GlobalPosition.X, GlobalPosition.Z);
		return position.DistanceTo(new Vector2(target.X, target.Z));
	}

	private void SetState(BehaviourState nextState)
	{
		if (_state == nextState)
		{
			return;
		}

		_state = nextState;
		PlayStateAnimation();
	}

	private void PlayStateAnimation()
	{
		string animationName = _state switch
		{
			BehaviourState.Chasing => WalkAnimationName,
			BehaviourState.Attacking => AttackAnimationName,
			_ => IdleAnimationName,
		};
		_animationPlayer.Play(animationName, AnimationBlendTime);
	}

	private void ConfigureAnimations()
	{
		AnimationLibrary library = _animationPlayer.GetAnimationLibrary("");
		AddAnimation(library, IdleAnimationName, IdleAnimationPath);
		AddAnimation(library, WalkAnimationName, WalkAnimationPath);
		AddAnimation(library, AttackAnimationName, AttackAnimationPath);
	}

	private static void AddAnimation(AnimationLibrary library, string name, string assetPath)
	{
		PackedScene animationScene = ResourceLoader.Load<PackedScene>(assetPath);
		Node sourceRoot = animationScene.Instantiate();
		AnimationPlayer sourcePlayer = FindDescendant<AnimationPlayer>(sourceRoot)
			?? throw new InvalidOperationException($"{assetPath} is missing an AnimationPlayer.");
		Animation animation = (Animation)sourcePlayer.GetAnimation(SourceAnimationName).Duplicate(true);

		animation.LoopMode = Animation.LoopModeEnum.Linear;
		KeepRootMotionInPlace(animation);
		if (library.HasAnimation(name))
		{
			library.RemoveAnimation(name);
		}
		library.AddAnimation(name, animation);
		sourceRoot.Free();
	}

	private static void KeepRootMotionInPlace(Animation animation)
	{
		for (int track = 0; track < animation.GetTrackCount(); track++)
		{
			if (animation.TrackGetType(track) != Animation.TrackType.Position3D ||
				!animation.TrackGetPath(track).ToString().EndsWith(":mixamorig_Hips"))
			{
				continue;
			}

			Vector3 startingPosition = animation.TrackGetKeyValue(track, 0).AsVector3();
			for (int key = 0; key < animation.TrackGetKeyCount(track); key++)
			{
				Vector3 position = animation.TrackGetKeyValue(track, key).AsVector3();
				position.X = startingPosition.X;
				position.Z = startingPosition.Z;
				animation.TrackSetKeyValue(track, key, position);
			}
		}
	}

	private static T? FindDescendant<T>(Node node) where T : Node
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is T match)
			{
				return match;
			}

			T? descendant = FindDescendant<T>(child);
			if (descendant is not null)
			{
				return descendant;
			}
		}

		return null;
	}
}
