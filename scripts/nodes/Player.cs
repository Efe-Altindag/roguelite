using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed = 200f;
    [Export]
    public float Gravity = 900f;
    [Export]
    public float JumpVelocity = -400f;
    
    private Hitbox _attackHitbox;
    private Vector2 _velocity = Vector2.Zero;

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

        var healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            healthComponent.Died += OnPlayerDied;
        }
        else
        {
            GD.PrintErr("Player: HealthComponent node named 'HealthComponent' not found as a child.");
        }

    }

    private void Attack()
    {
        GD.Print("=== ATTACK FUNCTION STARTING ===");
        GD.Print("Player attacks!");
        if (_attackHitbox != null)
        {
            GD.Print($"Player: Enabling sword hitbox for 0.2s.");
            _attackHitbox.Enable();
        }
        else
        {
            GD.PrintErr("Player: No sword hitbox found when attacking.");
        }
    }

    private void OnPlayerDied()
    {
        GD.Print("Player öldü!");
        QueueFree(); // veya respawn işlemi
    }


    /*private HealthComponent FindHealthComponent(Node node)
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
    }*/
    public override void _PhysicsProcess(double delta)
    {
        // Sağa-sola hareket
        float direction = 0f;
        if (Input.IsActionPressed("move_right"))
            direction += 1f;
        if (Input.IsActionPressed("move_left"))
            direction -= 1f;

        _velocity.X = direction * Speed;

        // Yerçekimi uygula
        if (!IsOnFloor())
            _velocity.Y += Gravity * (float)delta;
        else
            _velocity.Y = 0f;

        // Zıplama
        if (IsOnFloor() && Input.IsActionJustPressed("jump"))
        {
            _velocity.Y = JumpVelocity;
        }

        // Attack input kontrolü
        if (Input.IsActionJustPressed("attack"))
        {
            Attack();
        }

        Velocity = _velocity;
        MoveAndSlide();
    }
}