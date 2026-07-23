#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Gameplay;
using AshwoodCounty3DPrototype.Interactions;
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
	[Export] public float AwarenessUpdateInterval { get; set; } = 0.1f;
	[Export] public float DistantAwarenessUpdateInterval { get; set; } = 0.35f;
	[Export] public float DistantAwarenessThreshold { get; set; } = 18.0f;
	[Export] public float SeparationUpdateInterval { get; set; } = 0.15f;
	[Export] public float NavigationPathHeightOffset { get; set; } = -0.7f;
	[Export] public float WanderRadius { get; set; } = 6.0f;
	[Export] public float WanderSpeed { get; set; } = 0.28f;
	[Export] public float MinimumIdleDuration { get; set; } = 1.5f;
	[Export] public float MaximumIdleDuration { get; set; } = 3.5f;
	[Export] public float WanderTargetTolerance { get; set; } = 0.65f;
	[Export] public int WanderTargetAttempts { get; set; } = 6;
	[Export] public float SeparationRadius { get; set; } = 1.35f;
	[Export] public float SeparationStrength { get; set; } = 0.9f;
	[Export] public float InvestigationSpeed { get; set; } = 0.32f;
	[Export] public float InvestigationDuration { get; set; } = 2.5f;
	[Export] public float InvestigationTargetTolerance { get; set; } = 0.75f;
	[Export] public float PlayerSearchDuration { get; set; } = 3.0f;
	[Export] public float PlayerSearchRadius { get; set; } = 2.5f;
	[Export] public float PlayerSearchTargetInterval { get; set; } = 0.9f;
	[Export] public float LastKnownPositionTolerance { get; set; } = 0.75f;
	[Export] public float HitReactionDuration { get; set; } = 0.24f;
	[Export] public float KnockbackDamping { get; set; } = 12.0f;

	private const string SourceAnimationName = "mixamo_com";
	private const string IdleAnimationName = "Idle";
	private const string WalkAnimationName = "Walk";
	private const string AttackAnimationName = "Attack";
	private const string DeathAnimationName = "Death";
	private const string IdleAnimationPath = "res://assets/characters/zombies/zombie idle.fbx";
	private const string WalkAnimationPath = "res://assets/characters/zombies/zombie walk.fbx";
	private const string AttackAnimationPath = "res://assets/characters/zombies/zombie attack.fbx";
	private const string DeathAnimationPath = "res://assets/characters/zombies/zombie death.fbx";
	private const float AnimationBlendTime = 0.2f;

	private enum BehaviourState
	{
		Idle,
		Wandering,
		Investigating,
		SearchingNoise,
		SearchingPlayer,
		Chasing,
		Attacking,
	}

	private NavigationAgent3D _navigationAgent = null!;
	private AnimationPlayer _animationPlayer = null!;
	private Node3D _player = null!;
	private PlayerHealth _playerHealth = null!;
	private ZombieHealth _health = null!;
	private Node3D _visual = null!;
	private SearchableContainer _corpseLoot = null!;
	private BehaviourState _state = BehaviourState.Idle;
	private float _pathUpdateElapsed;
	private float _awarenessUpdateRemaining;
	private float _awarenessElapsed;
	private float _cachedDistanceToPlayer;
	private bool _cachedCanSeePlayer;
	private float _timeSincePlayerVisible = float.PositiveInfinity;
	private Vector3 _lastKnownPlayerPosition;
	private bool _hasLastKnownPlayerPosition;
	private bool _reachedLastKnownPlayerPosition;
	private float _playerSearchRemaining;
	private float _playerSearchTargetRemaining;
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
	private float _hitReactionRemaining;
	private Vector3 _knockbackVelocity;
	private float _separationUpdateRemaining;
	private Vector3 _cachedSeparation;
	private PhysicsRayQueryParameters3D _visionQuery = null!;

	public static readonly StringName ZombieGroupName = new("prototype_zombies");

	public string CurrentStateName => _state.ToString();
	public Vector3 LastHeardPosition => _lastHeardPosition;
	public GameplayNoiseCategory LastHeardCategory => _lastHeardCategory;
	public bool IsAlive { get; private set; } = true;

	public override void _Ready()
	{
		_player = GetNode<Node3D>(PlayerPath);
		_playerHealth = _player.GetNode<PlayerHealth>("Health");
		_health = GetNode<ZombieHealth>("Health");
		_visual = GetNode<Node3D>("Visual");
		_corpseLoot = GetNode<SearchableContainer>("CorpseLoot");
		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationPlayer = FindDescendant<AnimationPlayer>(this)
			?? throw new InvalidOperationException("Zombie model is missing an AnimationPlayer.");

		ConfigureAnimations();
		AddToGroup(ZombieGroupName);
		_random.Seed = (ulong)Time.GetTicksUsec() ^ GetInstanceId();
		_corpseLoot.SetLootSeed(CreateStableLootSeed(GetPath().ToString()));
		_corpseLoot.SetInteractionEnabled(false);
		_wanderOrigin = GlobalPosition;
		ScheduleIdleDelay();
		_navigationAgent.PathHeightOffset = NavigationPathHeightOffset;
		_navigationAgent.TargetDesiredDistance = Mathf.Max(AttackDistance - 0.3f, 0.5f);
		_pathUpdateElapsed = PathUpdateInterval;
		_cachedDistanceToPlayer = HorizontalDistanceTo(_player.GlobalPosition);
		_visionQuery = PhysicsRayQueryParameters3D.Create(Vector3.Zero, Vector3.Zero);
		_visionQuery.CollisionMask = VisionCollisionMask;
		_visionQuery.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		GameplayNoise.Emitted += OnGameplayNoiseEmitted;
		_health.Died += OnDied;
		PlayStateAnimation();
	}

	public override void _ExitTree()
	{
		GameplayNoise.Emitted -= OnGameplayNoiseEmitted;
		_health.Died -= OnDied;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsAlive)
		{
			return;
		}

		float deltaTime = (float)delta;
		if (_hitReactionRemaining > 0.0f)
		{
			UpdateHitReaction(deltaTime);
			return;
		}
		UpdateAwareness(deltaTime);
		UpdateInvestigation(deltaTime);
		UpdatePlayerSearch(deltaTime);
		UpdateWandering(deltaTime);
		UpdateAttack(deltaTime, _cachedDistanceToPlayer, _cachedCanSeePlayer);

		Vector3 movementDirection = _state switch
		{
			BehaviourState.Chasing => GetChaseNavigationDirection(deltaTime),
			BehaviourState.Wandering => GetPathDirection(),
			BehaviourState.Investigating => GetPathDirection(),
			BehaviourState.SearchingPlayer => GetPathDirection(),
			_ => Vector3.Zero,
		};
		if (!movementDirection.IsZeroApprox())
		{
			movementDirection = ApplySeparation(movementDirection, deltaTime);
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

	private void UpdateAwareness(float delta)
	{
		_awarenessElapsed += delta;
		_awarenessUpdateRemaining -= delta;
		if (_awarenessUpdateRemaining > 0.0f)
		{
			return;
		}

		_cachedDistanceToPlayer = HorizontalDistanceTo(_player.GlobalPosition);
		_cachedCanSeePlayer = CanSeePlayer(_cachedDistanceToPlayer);
		UpdatePlayerAwareness(_cachedCanSeePlayer, _awarenessElapsed);
		_awarenessElapsed = 0.0f;

		float threshold = Mathf.Max(DistantAwarenessThreshold, DetectionRadius);
		_awarenessUpdateRemaining = _cachedDistanceToPlayer > threshold
			? Mathf.Max(DistantAwarenessUpdateInterval, 0.01f)
			: Mathf.Max(AwarenessUpdateInterval, 0.01f);

		BehaviourState nextState = DetermineState(_cachedDistanceToPlayer, _cachedCanSeePlayer);
		SetState(nextState);
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

		if ((_state == BehaviourState.Chasing || _state == BehaviourState.Attacking) &&
			_timeSincePlayerVisible <= LostSightGracePeriod)
		{
			return BehaviourState.Chasing;
		}

		if (_hasLastKnownPlayerPosition)
		{
			return BehaviourState.SearchingPlayer;
		}

		if (_hasHeardNoise)
		{
			return _state == BehaviourState.SearchingNoise
				? BehaviourState.SearchingNoise
				: BehaviourState.Investigating;
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
		_health.RestoreAliveState(isAlive);
		IsAlive = isAlive;
		Visible = true;
		SetGameplayNoiseResponseEnabled(isAlive);
		SetPhysicsProcess(isAlive);
		_navigationAgent.AvoidanceEnabled = isAlive;
		if (!isAlive)
		{
			Velocity = Vector3.Zero;
		}

		SetCollisionDisabled(this, !isAlive);
		_corpseLoot.SetInteractionEnabled(!isAlive);
		if (!isAlive)
		{
			PlayDeadPose();
		}
		else
		{
			_state = BehaviourState.Idle;
			ScheduleIdleDelay();
			PlayStateAnimation();
		}
	}

	public bool ReceiveMeleeHit(float damage, Vector3 knockbackVelocity)
	{
		if (!IsAlive || !_health.ApplyDamage(damage))
		{
			return false;
		}

		if (!IsAlive)
		{
			return true;
		}

		_knockbackVelocity = knockbackVelocity;
		_hitReactionRemaining = Mathf.Max(HitReactionDuration, 0.0f);
		_visual.Scale = new Vector3(1.05f, 0.94f, 1.05f);
		return true;
	}

	private void UpdateHitReaction(float delta)
	{
		_hitReactionRemaining = Mathf.Max(_hitReactionRemaining - delta, 0.0f);
		Vector3 velocity = Velocity;
		velocity.X = _knockbackVelocity.X;
		velocity.Z = _knockbackVelocity.Z;
		Velocity = velocity;
		ApplyGravity(delta);
		MoveAndSlide();
		_knockbackVelocity = _knockbackVelocity.MoveToward(
			Vector3.Zero,
			Mathf.Max(KnockbackDamping, 0.0f) * delta);
		_visual.Scale = _visual.Scale.Lerp(Vector3.One, Mathf.Clamp(delta * 12.0f, 0.0f, 1.0f));
		if (_hitReactionRemaining <= 0.0f)
		{
			_visual.Scale = Vector3.One;
			PlayStateAnimation();
		}
	}

	private void OnDied()
	{
		IsAlive = false;
		SetGameplayNoiseResponseEnabled(false);
		_navigationAgent.AvoidanceEnabled = false;
		Velocity = Vector3.Zero;
		_hitReactionRemaining = 0.0f;
		_visual.Scale = Vector3.One;
		SetCollisionDisabled(this, true);
		_corpseLoot.SetInteractionEnabled(true);
		SetPhysicsProcess(false);
		_animationPlayer.Play(DeathAnimationName, AnimationBlendTime);
	}

	private static ulong CreateStableLootSeed(string value)
	{
		const ulong offsetBasis = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		ulong hash = offsetBasis;
		foreach (char character in value)
		{
			hash ^= character;
			hash *= prime;
		}
		return hash;
	}

	private void PlayDeadPose()
	{
		_animationPlayer.Play(DeathAnimationName);
		_animationPlayer.Seek(_animationPlayer.CurrentAnimationLength, true);
		_animationPlayer.Pause();
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
		if (_state is BehaviourState.Investigating or BehaviourState.SearchingNoise)
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
				SetState(BehaviourState.SearchingNoise);
			}
			return;
		}

		if (_state != BehaviourState.SearchingNoise)
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

	private void UpdatePlayerSearch(float delta)
	{
		if (_state != BehaviourState.SearchingPlayer)
		{
			return;
		}

		float tolerance = Mathf.Max(LastKnownPositionTolerance, 0.1f);
		if (!_reachedLastKnownPlayerPosition)
		{
			if (!_navigationAgent.IsNavigationFinished() &&
				HorizontalDistanceTo(_lastKnownPlayerPosition) > tolerance)
			{
				return;
			}

			_reachedLastKnownPlayerPosition = true;
			_playerSearchRemaining = Mathf.Max(PlayerSearchDuration, 0.0f);
			_playerSearchTargetRemaining = 0.0f;
		}

		_playerSearchRemaining = Mathf.Max(_playerSearchRemaining - delta, 0.0f);
		if (_playerSearchRemaining <= 0.0f)
		{
			_hasLastKnownPlayerPosition = false;
			_reachedLastKnownPlayerPosition = false;
			SetState(_hasHeardNoise ? BehaviourState.Investigating : BehaviourState.Idle);
			return;
		}

		_playerSearchTargetRemaining -= delta;
		if (_playerSearchTargetRemaining <= 0.0f || _navigationAgent.IsNavigationFinished())
		{
			TrySetNearbySearchTarget();
			_playerSearchTargetRemaining = Mathf.Max(PlayerSearchTargetInterval, 0.1f);
		}
	}

	private void TrySetNearbySearchTarget()
	{
		if (!NavigationMapIsReady() || PlayerSearchRadius <= 0.0f)
		{
			return;
		}

		float angle = _random.RandfRange(0.0f, Mathf.Tau);
		float distance = Mathf.Sqrt(_random.Randf()) * PlayerSearchRadius;
		Vector3 candidate = _lastKnownPlayerPosition + new Vector3(
			Mathf.Cos(angle) * distance,
			0.0f,
			Mathf.Sin(angle) * distance);
		_navigationAgent.TargetPosition = NavigationServer3D.MapGetClosestPoint(
			_navigationAgent.GetNavigationMap(),
			candidate);
	}

	private void UpdatePlayerAwareness(bool canSeePlayer, float delta)
	{
		if (canSeePlayer)
		{
			_timeSincePlayerVisible = 0.0f;
			_lastKnownPlayerPosition = _player.GlobalPosition;
			_hasLastKnownPlayerPosition = true;
			_reachedLastKnownPlayerPosition = false;
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
		_visionQuery.From = rayStart;
		_visionQuery.To = rayEnd;
		_visionQuery.CollisionMask = VisionCollisionMask;

		Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(_visionQuery);
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

	private Vector3 ApplySeparation(Vector3 movementDirection, float delta)
	{
		float radius = Mathf.Max(SeparationRadius, 0.0f);
		if (radius <= 0.0f || SeparationStrength <= 0.0f)
		{
			return movementDirection;
		}

		_separationUpdateRemaining -= delta;
		if (_separationUpdateRemaining <= 0.0f)
		{
			_cachedSeparation = CalculateSeparation(radius);
			_separationUpdateRemaining = Mathf.Max(SeparationUpdateInterval, 0.01f);
		}

		Vector3 combined = movementDirection + (_cachedSeparation * SeparationStrength);
		return combined.IsZeroApprox() ? movementDirection : combined.Normalized();
	}

	private Vector3 CalculateSeparation(float radius)
	{
		Vector3 separation = Vector3.Zero;
		foreach (Node node in GetTree().GetNodesInGroup(ZombieGroupName))
		{
			if (node is not PrototypeZombie other || other == this || !other.IsAlive)
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

		return separation;
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
		else if (_state == BehaviourState.SearchingPlayer)
		{
			_navigationAgent.TargetDesiredDistance = Mathf.Max(LastKnownPositionTolerance, 0.1f);
			_navigationAgent.TargetPosition = _lastKnownPlayerPosition;
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
			BehaviourState.SearchingPlayer => WalkAnimationName,
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
		AddAnimation(library, DeathAnimationName, DeathAnimationPath, shouldLoop: false);
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
