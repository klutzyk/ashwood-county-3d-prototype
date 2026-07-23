#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Gameplay;
using AshwoodCounty3DPrototype.Player;

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
	[Export] public float AttackDamage { get; set; } = 20.0f;
	[Export] public float AttackCooldown { get; set; } = 1.0f;
	[Export] public float AttackHitMoment { get; set; } = 0.98f;
	[Export] public float MoveSpeed { get; set; } = 0.36f;
	[Export] public float Acceleration { get; set; } = 8.0f;
	[Export] public float Gravity { get; set; } = 24.0f;
	[Export] public float TurnSpeed { get; set; } = 7.0f;
	[Export] public float PathUpdateInterval { get; set; } = 0.35f;
	[Export] public float NavigationPathHeightOffset { get; set; } = -0.7f;
	[Export] public float WanderRadius { get; set; } = 6.0f;
	[Export] public float WanderSpeed { get; set; } = 0.28f;
	[Export] public float MinimumIdleDuration { get; set; } = 1.5f;
	[Export] public float MaximumIdleDuration { get; set; } = 3.5f;
	[Export] public float WanderTargetTolerance { get; set; } = 0.65f;
	[Export] public int WanderTargetAttempts { get; set; } = 6;
	[Export] public float SeparationRadius { get; set; } = 1.2f;
	[Export] public float SeparationStrength { get; set; } = 0.75f;
	[Export] public float InvestigationSpeed { get; set; } = 0.32f;
	[Export] public float InvestigationDuration { get; set; } = 2.5f;
	[Export] public float InvestigationTargetTolerance { get; set; } = 0.75f;

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
		Wandering,
		Investigating,
		Searching,
		Chasing,
		Attacking,
	}

	private NavigationAgent3D _navigationAgent = null!;
	private AnimationPlayer _animationPlayer = null!;
	private Node3D _player = null!;
	private PlayerHealth _playerHealth = null!;
	private BehaviourState _state = BehaviourState.Idle;
	private float _pathUpdateElapsed;
	private float _timeSincePlayerVisible = float.PositiveInfinity;
	private Vector3 _lastKnownPlayerPosition;
	private float _previousAttackAnimationPosition;
	private float _timeSinceAttackHit;
	private bool _attackHitAttempted;
	private readonly RandomNumberGenerator _random = new();
	private Vector3 _wanderOrigin;
	private float _idleRemaining;
	private Vector3 _wanderTarget;
	private bool _hasHeardNoise;
	private bool _respondsToGameplayNoise = true;
	private Vector3 _lastHeardPosition;
	private GameplayNoiseCategory _lastHeardCategory;
	private float _investigationRemaining;

	public static readonly StringName ZombieGroupName = new("prototype_zombies");

	public string CurrentStateName => _state.ToString();
	public Vector3 LastHeardPosition => _lastHeardPosition;
	public GameplayNoiseCategory LastHeardCategory => _lastHeardCategory;
	public bool IsAlive { get; private set; } = true;

	public override void _Ready()
	{
		_player = GetNode<Node3D>(PlayerPath);
		_playerHealth = _player.GetNode<PlayerHealth>("Health");
		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationPlayer = FindDescendant<AnimationPlayer>(this)
			?? throw new InvalidOperationException("Zombie model is missing an AnimationPlayer.");

		ConfigureAnimations();
		AddToGroup(ZombieGroupName);
		_random.Seed = (ulong)Time.GetTicksUsec() ^ GetInstanceId();
		_wanderOrigin = GlobalPosition;
		ScheduleIdleDelay();
		_navigationAgent.PathHeightOffset = NavigationPathHeightOffset;
		_navigationAgent.TargetDesiredDistance = Mathf.Max(AttackDistance - 0.3f, 0.5f);
		_pathUpdateElapsed = PathUpdateInterval;
		GameplayNoise.Emitted += OnGameplayNoiseEmitted;
		PlayStateAnimation();
	}

	public override void _ExitTree()
	{
		GameplayNoise.Emitted -= OnGameplayNoiseEmitted;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsAlive)
		{
			return;
		}

		float deltaTime = (float)delta;
		float distanceToPlayer = HorizontalDistanceTo(_player.GlobalPosition);
		bool canSeePlayer = CanSeePlayer(distanceToPlayer);
		UpdatePlayerAwareness(canSeePlayer, deltaTime);
		BehaviourState nextState = DetermineState(distanceToPlayer, canSeePlayer);
		SetState(nextState);
		UpdateInvestigation(deltaTime);
		UpdateWandering(deltaTime);
		UpdateAttack(deltaTime, distanceToPlayer, canSeePlayer);

		Vector3 movementDirection = _state switch
		{
			BehaviourState.Chasing => GetChaseNavigationDirection(deltaTime),
			BehaviourState.Wandering => GetPathDirection(),
			BehaviourState.Investigating => GetPathDirection(),
			_ => Vector3.Zero,
		};
		if (!movementDirection.IsZeroApprox())
		{
			movementDirection = ApplySeparation(movementDirection);
		}

		float movementSpeed = _state switch
		{
			BehaviourState.Wandering => WanderSpeed,
			BehaviourState.Investigating => InvestigationSpeed,
			_ => MoveSpeed,
		};
		ApplyHorizontalMovement(movementDirection, movementSpeed, deltaTime);
		ApplyGravity(deltaTime);

		Vector3 facingDirection = _state == BehaviourState.Attacking
			? HorizontalDirectionTo(_player.GlobalPosition)
			: movementDirection;
		RotateToward(facingDirection, deltaTime);

		MoveAndSlide();
	}

	private void UpdateAttack(float delta, float distanceToPlayer, bool canSeePlayer)
	{
		if (_state != BehaviourState.Attacking)
		{
			return;
		}

		_timeSinceAttackHit += delta;
		float animationLength = (float)_animationPlayer.CurrentAnimationLength;
		float hitMoment = Mathf.Clamp(AttackHitMoment, 0.0f, animationLength);
		float currentPosition = (float)_animationPlayer.CurrentAnimationPosition;

		if (!_attackHitAttempted &&
			_previousAttackAnimationPosition <= hitMoment &&
			currentPosition >= hitMoment)
		{
			_attackHitAttempted = true;
			if (_timeSinceAttackHit >= Mathf.Max(AttackCooldown, 0.0f) &&
				canSeePlayer && distanceToPlayer <= AttackDistance)
			{
				_playerHealth.ApplyDamage(AttackDamage);
				_timeSinceAttackHit = 0.0f;
			}
		}

		_previousAttackAnimationPosition = currentPosition;
		if (_animationPlayer.IsPlaying() ||
			_timeSinceAttackHit < Mathf.Max(AttackCooldown, 0.0f))
		{
			return;
		}

		_previousAttackAnimationPosition = 0.0f;
		_attackHitAttempted = false;
		_animationPlayer.Play(AttackAnimationName, AnimationBlendTime);
	}

	private BehaviourState DetermineState(float distanceToPlayer, bool canSeePlayer)
	{
		if (canSeePlayer)
		{
			return distanceToPlayer <= AttackDistance
				? BehaviourState.Attacking
				: BehaviourState.Chasing;
		}

		if (_hasHeardNoise)
		{
			return _state == BehaviourState.Searching
				? BehaviourState.Searching
				: BehaviourState.Investigating;
		}

		if ((_state == BehaviourState.Chasing || _state == BehaviourState.Attacking) &&
			_timeSincePlayerVisible <= LostSightGracePeriod &&
			HorizontalDistanceTo(_lastKnownPlayerPosition) > _navigationAgent.TargetDesiredDistance)
		{
			return BehaviourState.Chasing;
		}

		return _state == BehaviourState.Wandering
			? BehaviourState.Wandering
			: BehaviourState.Idle;
	}

	public void SetGameplayNoiseResponseEnabled(bool enabled)
	{
		_respondsToGameplayNoise = enabled;
		if (!enabled)
		{
			_hasHeardNoise = false;
		}
	}

	public void SetAlive(bool isAlive)
	{
		IsAlive = isAlive;
		Visible = isAlive;
		SetGameplayNoiseResponseEnabled(isAlive);
		SetPhysicsProcess(isAlive);
		if (!isAlive)
		{
			Velocity = Vector3.Zero;
		}

		SetCollisionDisabled(this, !isAlive);
	}

	private static void SetCollisionDisabled(Node node, bool disabled)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is CollisionShape3D collisionShape)
			{
				collisionShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, disabled);
			}
			SetCollisionDisabled(child, disabled);
		}
	}

	private void OnGameplayNoiseEmitted(GameplayNoiseEvent noise)
	{
		if (!_respondsToGameplayNoise || _state == BehaviourState.Attacking ||
			HorizontalDistanceTo(noise.WorldPosition) > noise.AudibleRadius)
		{
			return;
		}

		_lastHeardPosition = noise.WorldPosition;
		_lastHeardCategory = noise.Category;
		_hasHeardNoise = true;
		if (_state is BehaviourState.Investigating or BehaviourState.Searching)
		{
			SetState(BehaviourState.Investigating, forceRefresh: true);
		}
	}

	private void UpdateInvestigation(float delta)
	{
		if (_state == BehaviourState.Investigating)
		{
			if (_navigationAgent.IsNavigationFinished() ||
				HorizontalDistanceTo(_lastHeardPosition) <= Mathf.Max(InvestigationTargetTolerance, 0.1f))
			{
				_investigationRemaining = Mathf.Max(InvestigationDuration, 0.0f);
				SetState(BehaviourState.Searching);
			}
			return;
		}

		if (_state != BehaviourState.Searching)
		{
			return;
		}

		_investigationRemaining = Mathf.Max(_investigationRemaining - delta, 0.0f);
		if (_investigationRemaining <= 0.0f)
		{
			_hasHeardNoise = false;
			SetState(BehaviourState.Idle);
		}
	}

	private void UpdatePlayerAwareness(bool canSeePlayer, float delta)
	{
		if (canSeePlayer)
		{
			_timeSincePlayerVisible = 0.0f;
			_lastKnownPlayerPosition = _player.GlobalPosition;
			_hasHeardNoise = false;
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

	private Vector3 GetChaseNavigationDirection(float delta)
	{
		_pathUpdateElapsed += delta;
		if (!NavigationMapIsReady())
		{
			return Vector3.Zero;
		}

		if (_pathUpdateElapsed >= PathUpdateInterval)
		{
			_navigationAgent.TargetPosition = _lastKnownPlayerPosition;
			_pathUpdateElapsed = 0.0f;
		}

		return GetPathDirection();
	}

	private Vector3 GetPathDirection()
	{
		if (!NavigationMapIsReady())
		{
			return Vector3.Zero;
		}

		if (_navigationAgent.IsNavigationFinished())
		{
			return Vector3.Zero;
		}

		return HorizontalDirectionTo(_navigationAgent.GetNextPathPosition());
	}

	private void UpdateWandering(float delta)
	{
		if (_state == BehaviourState.Wandering)
		{
			if (_navigationAgent.IsNavigationFinished() ||
				HorizontalDistanceTo(_wanderTarget) <= Mathf.Max(WanderTargetTolerance, 0.1f))
			{
				SetState(BehaviourState.Idle);
			}
			return;
		}

		if (_state != BehaviourState.Idle)
		{
			return;
		}

		_idleRemaining = Mathf.Max(_idleRemaining - delta, 0.0f);
		if (_idleRemaining <= 0.0f)
		{
			TryStartWandering();
		}
	}

	private void TryStartWandering()
	{
		if (!NavigationMapIsReady() || WanderRadius <= 0.0f)
		{
			ScheduleIdleDelay();
			return;
		}

		Rid navigationMap = _navigationAgent.GetNavigationMap();
		int attempts = Mathf.Max(WanderTargetAttempts, 1);
		float targetTolerance = Mathf.Max(WanderTargetTolerance, 0.1f);
		for (int attempt = 0; attempt < attempts; attempt++)
		{
			float angle = _random.RandfRange(0.0f, Mathf.Tau);
			float distance = Mathf.Sqrt(_random.Randf()) * WanderRadius;
			Vector3 candidate = _wanderOrigin + new Vector3(
				Mathf.Cos(angle) * distance,
				0.0f,
				Mathf.Sin(angle) * distance);
			Vector3 navigationPoint = NavigationServer3D.MapGetClosestPoint(navigationMap, candidate);
			if (HorizontalDistanceTo(navigationPoint) <= targetTolerance * 2.0f ||
				HorizontalDistanceBetween(_wanderOrigin, navigationPoint) > WanderRadius + targetTolerance)
			{
				continue;
			}

			_wanderTarget = navigationPoint;
			_navigationAgent.TargetDesiredDistance = targetTolerance;
			_navigationAgent.TargetPosition = _wanderTarget;
			SetState(BehaviourState.Wandering);
			return;
		}

		ScheduleIdleDelay();
	}

	private void ScheduleIdleDelay()
	{
		float minimum = Mathf.Max(MinimumIdleDuration, 0.0f);
		float maximum = Mathf.Max(MaximumIdleDuration, minimum);
		_idleRemaining = _random.RandfRange(minimum, maximum);
	}

	private Vector3 ApplySeparation(Vector3 movementDirection)
	{
		float radius = Mathf.Max(SeparationRadius, 0.0f);
		if (radius <= 0.0f || SeparationStrength <= 0.0f)
		{
			return movementDirection;
		}

		Vector3 separation = Vector3.Zero;
		foreach (Node node in GetTree().GetNodesInGroup(ZombieGroupName))
		{
			if (node is not PrototypeZombie other || other == this)
			{
				continue;
			}

			Vector3 offset = GlobalPosition - other.GlobalPosition;
			offset.Y = 0.0f;
			float distance = offset.Length();
			if (distance >= radius)
			{
				continue;
			}

			if (distance <= 0.001f)
			{
				offset = GetInstanceId() < other.GetInstanceId() ? Vector3.Left : Vector3.Right;
				distance = 0.001f;
			}
			separation += offset.Normalized() * (1.0f - (distance / radius));
		}

		Vector3 combined = movementDirection + (separation * SeparationStrength);
		return combined.IsZeroApprox() ? movementDirection : combined.Normalized();
	}

	private bool NavigationMapIsReady()
	{
		Rid navigationMap = _navigationAgent.GetNavigationMap();
		return navigationMap.IsValid && NavigationServer3D.MapGetIterationId(navigationMap) > 0;
	}

	private void ApplyHorizontalMovement(Vector3 direction, float speed, float delta)
	{
		Vector3 velocity = Velocity;
		Vector3 targetVelocity = direction * Mathf.Max(speed, 0.0f);
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

	private static float HorizontalDistanceBetween(Vector3 first, Vector3 second)
	{
		return new Vector2(first.X, first.Z).DistanceTo(new Vector2(second.X, second.Z));
	}

	private void SetState(BehaviourState nextState, bool forceRefresh = false)
	{
		if (_state == nextState && !forceRefresh)
		{
			return;
		}

		BehaviourState previousState = _state;
		_state = nextState;
		if (_state == BehaviourState.Attacking)
		{
			_previousAttackAnimationPosition = 0.0f;
			_timeSinceAttackHit = Mathf.Max(AttackCooldown, 0.0f);
			_attackHitAttempted = false;
		}
		else if (_state == BehaviourState.Chasing)
		{
			_navigationAgent.TargetDesiredDistance = Mathf.Max(AttackDistance - 0.3f, 0.5f);
			_pathUpdateElapsed = PathUpdateInterval;
		}
		else if (_state == BehaviourState.Investigating)
		{
			_navigationAgent.TargetDesiredDistance = Mathf.Max(InvestigationTargetTolerance, 0.1f);
			_navigationAgent.TargetPosition = _lastHeardPosition;
		}
		else if (_state == BehaviourState.Idle && previousState != BehaviourState.Idle)
		{
			ScheduleIdleDelay();
		}
		PlayStateAnimation();
	}

	private void PlayStateAnimation()
	{
		string animationName = _state switch
		{
			BehaviourState.Chasing => WalkAnimationName,
			BehaviourState.Wandering => WalkAnimationName,
			BehaviourState.Investigating => WalkAnimationName,
			BehaviourState.Attacking => AttackAnimationName,
			_ => IdleAnimationName,
		};
		_animationPlayer.Play(animationName, AnimationBlendTime);
	}

	private void ConfigureAnimations()
	{
		AnimationLibrary library = _animationPlayer.GetAnimationLibrary("");
		AddAnimation(library, IdleAnimationName, IdleAnimationPath, shouldLoop: true);
		AddAnimation(library, WalkAnimationName, WalkAnimationPath, shouldLoop: true);
		AddAnimation(library, AttackAnimationName, AttackAnimationPath, shouldLoop: false);
	}

	private static void AddAnimation(
		AnimationLibrary library,
		string name,
		string assetPath,
		bool shouldLoop)
	{
		PackedScene animationScene = ResourceLoader.Load<PackedScene>(assetPath);
		Node sourceRoot = animationScene.Instantiate();
		AnimationPlayer sourcePlayer = FindDescendant<AnimationPlayer>(sourceRoot)
			?? throw new InvalidOperationException($"{assetPath} is missing an AnimationPlayer.");
		Animation animation = (Animation)sourcePlayer.GetAnimation(SourceAnimationName).Duplicate(true);

		animation.LoopMode = shouldLoop
			? Animation.LoopModeEnum.Linear
			: Animation.LoopModeEnum.None;
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
