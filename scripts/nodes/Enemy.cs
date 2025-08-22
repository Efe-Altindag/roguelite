using Godot;
using System;

#nullable enable

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float TargetSpeed = 120f; // Speed when moving towards the target

	private Vector2 _smoothDirection = Vector2.Right; // For smooth turning
	private AnimatedSprite2D? _animatedSprite2D;
	private HealthComponent? _healthComponent;
	private CharacterBody2D? _target = null;
	private Timer? _attackCooldownTimer;
	private Area2D? _attackHitbox;

	private bool _isHurt = false; // New flag for hurt animation
	private bool _isDead = false; // New flag for death animation
	private bool _canAttack = true;

	private System.Collections.Generic.HashSet<Area2D> _playersInHitbox = new();


	public override void _Ready()
	{
		_smoothDirection = Vector2.Right; // Default facing
		_animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_healthComponent = GetNode<HealthComponent>("HealthComponent");
		_attackHitbox = GetNodeOrNull<Area2D>("Hitbox");
		_attackCooldownTimer = GetNodeOrNull<Timer>("AttackCooldownTimer");


		if (_healthComponent != null)
		{
			_healthComponent.Died += OnDied;
		}
		if (_animatedSprite2D != null)
		{
			_animatedSprite2D.AnimationFinished += OnAnimationFinished;
		}
		if (_attackHitbox != null)
		{
			_attackHitbox.AreaEntered += _on_hitbox_area_entered;
		}
		if (_attackCooldownTimer != null)
    	{
        _attackCooldownTimer.Timeout += OnAttackCooldownTimeout;
    	}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 moveVelocity = Velocity;

		// Hedefe yatay eksende yaklaş
		if (_target != null && GodotObject.IsInstanceValid(_target))
		{
			float dx = _target.GlobalPosition.X - GlobalPosition.X;
			if (Mathf.Abs(dx) > 5f) // Minimum mesafe
			{
				moveVelocity.X = Mathf.Sign(dx) * TargetSpeed;
			}
			else
			{
				moveVelocity.X = 0f;
			}
		}
		else
		{
			moveVelocity.X = 0f;
		}

		// Yerçekimi uygula
		if (!IsOnFloor())
			moveVelocity.Y += 900f * (float)delta; // Gravity sabit
		else
			moveVelocity.Y = 0f;

		Velocity = moveVelocity;
		MoveAndSlide();

		UpdateAnimation();
	}

	private void UpdateAnimation()
	{
		if (_animatedSprite2D == null || _isHurt || _isDead) return;
		
		// Determine animation based on actual movement
		Vector2 currentVelocity = Velocity;
		string animPrefix = currentVelocity.Length() > 10f ? "walk" : "idle";
		
		// Use movement direction for animation facing
		Vector2 facingDirection = currentVelocity.Length() > 5f ? currentVelocity : _smoothDirection;
		string directionSuffix = GetDirectionSuffix(facingDirection);
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
			// Fallback to idle animation
			string fallbackAnim = $"idle_{directionSuffix}";
			if (_animatedSprite2D.SpriteFrames.HasAnimation(fallbackAnim))
			{
				if (_animatedSprite2D.Animation != fallbackAnim)
				{
					_animatedSprite2D.Play(fallbackAnim);
				}
			}
		}
		
		// Update smooth direction for consistent facing
		if (currentVelocity.Length() > 5f)
		{
			_smoothDirection = currentVelocity.Normalized();
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

	private void OnAttackCooldownTimeout()
	{
    	_canAttack = true;
		int damage = (_attackHitbox as EnemyAttackHitbox)?.Damage ?? 10;
		foreach (var playerArea in _playersInHitbox)
		{
			if (GodotObject.IsInstanceValid(playerArea))
			{
				playerArea.Call("TakeDamage", damage);
				GD.Print($"Düşman, timer sonrası oyuncuya tekrar {damage} hasar verdi!");
			}
		}
		if (_playersInHitbox.Count > 0)
		{
			_canAttack = false;
			_attackCooldownTimer?.Start();
		}
	}
	private void _on_hitbox_area_entered(Area2D area)
	{
		if (area.GetParent().IsInGroup("player"))
		{
			_playersInHitbox.Add(area);

			// Timer bittiyse hemen hasar ver
			if (_canAttack)
			{
				int damage = (_attackHitbox as EnemyAttackHitbox)?.Damage ?? 10;
				area.Call("TakeDamage", damage);
				_canAttack = false;
				_attackCooldownTimer?.Start();
				GD.Print($"Düşman, oyuncuya {damage} hasar verdi!");
			}
		}
	}

	// Hitbox'tan çıkan player'ı takip et
	private void _on_hitbox_area_exited(Area2D area)
	{
		if (area.GetParent().IsInGroup("player"))
		{
			_playersInHitbox.Remove(area);
		}
	}


	private void _on_aggro_radius_body_entered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_target = body as CharacterBody2D;
		}
	}
}