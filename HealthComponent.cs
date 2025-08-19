using Godot;
using System;

public partial class HealthComponent : Node
{
    [Export]
    public int MaxHealth = 100;

    public int CurrentHealth { get; private set; }

    [Signal]
    public delegate void DiedEventHandler();

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        CurrentHealth -= damageAmount;
        if (CurrentHealth < 0)
            CurrentHealth = 0;

        // Sprite'ı kırmızıya boyama
        var parent = GetParent();
        if (parent != null)
        {
            var sprite = parent.GetNodeOrNull<Sprite2D>("Sprite2D");
            if (sprite != null)
            {
                sprite.Modulate = new Color(1, 0, 0); // Kırmızı
                GetTree().CreateTimer(0.1f).Timeout += () => {
                    if (GodotObject.IsInstanceValid(sprite))
                        sprite.Modulate = new Color(1, 1, 1);
                }; // 0.1 saniye sonra eski haline dön
            }
        }

        GD.Print($"HealthComponent: Hasar alındı! Kalan Can: {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            EmitSignal("Died");
            var parentNode = GetParent();
            if (parentNode != null)
                parentNode.QueueFree();
            else
                QueueFree();
        }
    }
}