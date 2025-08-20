using Godot;
using System;

#nullable enable

public partial class Enemy : CharacterBody2D
{
	public enum State { Patrolling, Following }

	[Export]
	public float PatrolSpeed = 40f;
	[Export]
	public float FollowSpeed = 100f;
	[Export]
	public float FollowDistance = 200f;
	[Export]
	public float StopDistance = 12f;
	[Export]
	public float ChangeDirMin = 1.0f;
	[Export]
	public float ChangeDirMax = 3.0f;
	[Export]
	public float PushResistance = 0.3f; // Enemy resistance to being pushed (0-1)
	[Export]
	public float PushDamping = 4f; // How fast push effects decay
	[Export]
	public float TurnSpeed = 3f; // Smooth turning speed

	private Vector2 _pushVelocity = Vector2.Zero; // Push effects
	private Vector2 _smoothDirection = Vector2.Right; // For smooth turning
	private State _state = State.Patrolling;
	private Vector2 _direction = Vector2.Right;
	private double _changeTimer = 0.0;
	private double _changeInterval = 2.0;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private Player? _player;
	private AnimatedSprite2D? _animatedSprite2D;
	private HealthComponent? _healthComponent;
	private bool _isHurt = false; // New flag for hurt animation
	private bool _isDead = false; // New flag for death animation

	public override void _Ready()
	{
		_rng.Randomize();
		PickNewDirection();
		_changeInterval = _rng.RandfRange(ChangeDirMin, ChangeDirMax);
		_player = FindPlayer();
		_smoothDirection = _direction; // Initial direction
		_animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_healthComponent = GetNode<HealthComponent>("HealthComponent");
		if (_healthComponent != null)
		{
			_healthComponent.Died += OnDied;
		}
		if (_animatedSprite2D != null)
		{
			_animatedSprite2D.AnimationFinished += OnAnimationFinished;
		}
	}

	private void PickNewDirection()
	{
		var x = _rng.RandfRange(-1.0f, 1.0f);
		var y = _rng.RandfRange(-1.0f, 1.0f);
		var v = new Vector2(x, y);
		if (v.Length() == 0)
			v = Vector2.Right;
		_direction = v.Normalized();
	}

	private Player? FindPlayer()
	{
		var scene = GetTree().GetCurrentScene();
		if (scene == null)
			return null;
		return FindChildNodeRecursive<Player>(scene, "Player");
	}

	private T? FindChildNodeRecursive<T>(Node root, string name) where T : Node
	{
		var direct = root.GetNodeOrNull<T>(name);
		if (direct != null)
			return direct;

		foreach (var childObj in root.GetChildren())
		{
			if (childObj is Node child)
			{
				var found = FindChildNodeRecursive<T>(child, name);
				if (found != null)
					return found;
			}
		}

		return null;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			// If dead, stop all movement and animation updates
			Velocity = Vector2.Zero;
			_pushVelocity = Vector2.Zero;
			return;
		}

		// Try to find player if we don't have it yet
		if (_player == null)
			_player = FindPlayer();

		// Decide state based on distance to player
		if (_player != null)
		{
			var dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
			_state = dist <= FollowDistance ? State.Following : State.Patrolling;
		}

		Vector2 velocity = Vector2.Zero;

		if (_state == State.Patrolling)
		{
			_changeTimer += delta;
			if (_changeTimer >= _changeInterval)
			{
				PickNewDirection();
				_changeTimer = 0.0;
				_changeInterval = _rng.RandfRange(ChangeDirMin, ChangeDirMax);
			}
			// Smoothly turn toward patrol direction so animations reflect facing
			var targetDir = _direction;
			_smoothDirection = _smoothDirection.MoveToward(targetDir, TurnSpeed * (float)delta);
			velocity = _smoothDirection * PatrolSpeed;
		}
		else // Following
		{
			if (_player != null)
			{
				var toPlayer = _player.GlobalPosition - GlobalPosition;
				var distToPlayer = toPlayer.Length();
				if (distToPlayer <= StopDistance)
				{
					// Too close: slow down
					var targetDir = toPlayer.Normalized();
					_smoothDirection = _smoothDirection.MoveToward(targetDir, TurnSpeed * (float)delta);
					velocity = _smoothDirection * (FollowSpeed * 0.3f); // Slow approach
				}
				else
				{
					// Normal following: smooth turning
					var targetDir = toPlayer.Normalized();
					_smoothDirection = _smoothDirection.MoveToward(targetDir, TurnSpeed * (float)delta);
					velocity = _smoothDirection * FollowSpeed;
				}
			}
		}

		// Total velocity = AI movement + push effects
		Velocity = velocity + _pushVelocity;
		MoveAndSlide();

		// Collision control and counter-push
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			if (collision.GetCollider() is Player player)
			{
				// Apply light counter-push to player
				Vector2 pushDirection = (player.GlobalPosition - GlobalPosition);
				if (pushDirection.Length() < 0.1f) 
					pushDirection = Vector2.Left; // Fallback
				pushDirection = pushDirection.Normalized();
				
				player.ApplyPush(pushDirection * 60f * (float)delta); // Lighter force
			}
		}

		// Reduce push effects over time
		_pushVelocity = _pushVelocity.MoveToward(Vector2.Zero, PushDamping * FollowSpeed * (float)delta);
		UpdateAnimation();
	}

	private void UpdateAnimation()
	{
		if (_animatedSprite2D == null || _isHurt || _isDead) return;

		string animPrefix = "";
		float currentSpeed = Velocity.Length();
		
		if (_state == State.Patrolling)
		{
			animPrefix = currentSpeed > 5f ? "walk" : "idle";
		}
		else // Following
		{
			if (currentSpeed > 5f)
			{
				// Use run animation when following and moving fast
				animPrefix = "run";
			}
			else
			{
				animPrefix = "idle";
			}
		}

		string directionSuffix = GetDirectionSuffix(_smoothDirection);
		string animationName = $"{animPrefix}_{directionSuffix}";

		if (_animatedSprite2D.SpriteFrames.HasAnimation(animationName))
		{
			if (_animatedSprite2D.Animation != animationName)
			{
				_animatedSprite2D.Play(animationName);
			}
		}
		else
		{
			// Fallback to idle if animation not found
			string fallbackAnim = $"idle_{directionSuffix}";
			if (_animatedSprite2D.SpriteFrames.HasAnimation(fallbackAnim) && _animatedSprite2D.Animation != fallbackAnim)
			{
				_animatedSprite2D.Play(fallbackAnim);
			}
		}
	}

	public string GetDirectionSuffix(Vector2 direction) // Changed to public
	{
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			return direction.X > 0 ? "right" : "left";
		}
		else
		{
			return direction.Y > 0 ? "down" : "up";
		}
	}

	public void PlayHurtAnimation()
	{
		if (_animatedSprite2D == null || _isDead || _isHurt)
		{
			return; // Don't play hurt if dead or already hurting
		}
		
		_isHurt = true;
		string directionSuffix = GetDirectionSuffix(_smoothDirection);
		string hurtAnimationName = $"hurt_{directionSuffix}";
		if (_animatedSprite2D.SpriteFrames.HasAnimation(hurtAnimationName))
		{
			_animatedSprite2D.Stop(); // Explicitly stop current animation
			_animatedSprite2D.Play(hurtAnimationName);
		}
		else
		{
			GD.PrintErr($"Hurt animation '{hurtAnimationName}' not found. Resetting _isHurt.");
			_isHurt = false; // Reset flag if animation not found
			UpdateAnimation(); // Revert to normal animation if hurt animation not found
		}
	}

	private void OnHurtAnimationFinished()
	{
		if (_animatedSprite2D == null) return;
		// Ensure this only triggers for the hurt animation
		if (_animatedSprite2D.Animation.ToString().StartsWith("hurt"))
		{
			_isHurt = false;
			UpdateAnimation(); // Revert to normal animation
		}
	}

	// Apply external push forces
	public void ApplyPush(Vector2 pushVector)
	{
		// Reduce push force with resistance factor
		Vector2 finalPush = pushVector * (1f - PushResistance);
		_pushVelocity += finalPush;
	}

	private void OnDied()
	{
		if (_isDead) return; // Prevent multiple death calls
		_isDead = true;
		
		if (_animatedSprite2D == null)
		{
			QueueFree();
			return;
		}

		string directionSuffix = GetDirectionSuffix(_smoothDirection);
		string deathAnimationName = $"death_{directionSuffix}";
		
		if (_animatedSprite2D.SpriteFrames.HasAnimation(deathAnimationName))
		{
			_animatedSprite2D.Stop();
			_animatedSprite2D.Play(deathAnimationName);
		}
		else
		{
			GD.PrintErr($"Death animation '{deathAnimationName}' not found. Destroying immediately.");
			QueueFree();
		}
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite2D == null) return;
		
		string currentAnim = _animatedSprite2D.Animation.ToString();
		
		if (currentAnim.StartsWith("death"))
		{
			QueueFree();
		}
		else if (currentAnim.StartsWith("hurt"))
		{
			_isHurt = false;
			UpdateAnimation(); // Revert to normal animation
		}
	}
}