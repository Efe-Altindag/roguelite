using Godot;
using System;

public partial class Hitbox : Area2D
{
    [Export]
    public int Damage = 10;

    public override void _Ready()
    {
        Monitoring = false;
        Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
    }

    private void OnBodyEntered(Node body)
    {
        GD.Print($"Hitbox: body entered: {body.Name} ({body.GetType().Name})");
        Hurtbox hurtbox = null;
        // Çarpışan body'sinin çocuklarında Area2D tipinde ve Hurtbox scriptli node ara
        foreach (var childObj in body.GetChildren())
        {
            if (childObj is Hurtbox h)
            {
                hurtbox = h;
                break;
            }
        }
        if (hurtbox != null)
        {
            hurtbox.TakeDamage(Damage);
        }
        // ...existing code...
    }

    public void Enable(float duration = 0.2f)
    {
        Monitoring = true;
        GetTree().CreateTimer(duration).Timeout += () => Monitoring = false;
    }
}
