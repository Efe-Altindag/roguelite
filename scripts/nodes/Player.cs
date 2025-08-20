using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed = 200f;
    [Export]
    public float PushForce = 800f; // Force applied to push enemies
    [Export]
    public float PushDamping = 5f; // How fast push effects decay
    
    private Vector2 _pushVelocity = Vector2.Zero; // Accumulated external forces
    private Hitbox _attackHitbox;

    public override void _Ready()
    {
        GD.Print("=== PLAYER _Ready() STARTING ===");
        // Find and activate camera
        var cam = GetNodeOrNull<Camera2D>("Camera2D");
        if (cam == null)
        {
            GD.PrintErr("Player: Camera2D node named 'Camera2D' not found as a child.");
        }
        else
        {
            cam.MakeCurrent();
        }

        // Find Sword node and access its Hitbox child
        var sword = GetNodeOrNull<Node>("Sword");
        if (sword != null)
        {
            var hitboxNode = sword.GetNodeOrNull<Hitbox>("Hitbox");
            if (hitboxNode != null)
            {
                _attackHitbox = hitboxNode;
                GD.Print($"Player: Sword hitbox found at {_attackHitbox.GetPath()}");
            }
            else
            {
                GD.Print("Player: Sword node found but Hitbox not found under Sword. (Check Sword scene!)");
            }
        }
        else
        {
            GD.Print("Player: Sword node not found as a child.");
        }

    }

    private void Attack()
    {
        GD.Print("=== ATTACK FUNCTION STARTING ===");
        GD.Print("Player attacks!");
        if (_attackHitbox != null)
        {
            GD.Print($"Player: Enabling sword hitbox for 0.2s.");
            _attackHitbox.Enable(0.2f);
        }
        else
        {
            GD.PrintErr("Player: No sword hitbox found when attacking.");
        }
    }


    private HealthComponent FindHealthComponent(Node node)
    {
        if (node is HealthComponent direct)
            return direct;

        var byName = node.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (byName != null)
            return byName;

        foreach (var childObj in node.GetChildren())
        {
            if (childObj is Node child)
            {
                var found = FindHealthComponent(child);
                if (found != null)
                    return found;
            }
        }

        return null;
    }
    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Vector2.Zero;

        if (Input.IsActionJustPressed("attack"))
        {
            GD.Print("Attack input detected!");
            Attack();
        }

        if (Input.IsActionPressed("move_right"))
            velocity.X += 1;
        if (Input.IsActionPressed("move_left"))
            velocity.X -= 1;
        if (Input.IsActionPressed("move_down"))
            velocity.Y += 1;
        if (Input.IsActionPressed("move_up"))
            velocity.Y -= 1;

        if (velocity.Length() > 0)
        {
            velocity = velocity.Normalized() * Speed;
        }

        // Total velocity = player movement + push effects
        Velocity = velocity + _pushVelocity;
        MoveAndSlide();

        // Check collisions and apply push
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is Enemy enemy)
            {
                // Calculate push direction (from player to enemy)
                Vector2 pushDirection = (enemy.GlobalPosition - GlobalPosition);
                if (pushDirection.Length() < 0.1f) 
                    pushDirection = Vector2.Right; // Fallback
                pushDirection = pushDirection.Normalized();
                
                // Apply controlled push to enemy
                enemy.ApplyPush(pushDirection * PushForce * (float)delta);
            }
        }

        // Reduce own push effects over time
        _pushVelocity = _pushVelocity.MoveToward(Vector2.Zero, PushDamping * Speed * (float)delta);
    }

    // Apply external push forces
    public void ApplyPush(Vector2 pushVector)
    {
        _pushVelocity += pushVector;
    }
}