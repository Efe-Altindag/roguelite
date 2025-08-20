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

        // Sprite'ı kırmızıya boyama (red flash effect)
        var parent = GetParent();
        if (parent != null)
        {
            var sprite = parent.GetNodeOrNull<Sprite2D>("Sprite2D");
            var animatedSprite = parent.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            
            if (sprite != null)
            {
                sprite.Modulate = new Color(1, 0, 0); // Kırmızı
                GetTree().CreateTimer(0.1f).Timeout += () => {
                    if (GodotObject.IsInstanceValid(sprite))
                        sprite.Modulate = new Color(1, 1, 1);
                };
            }
            else if (animatedSprite != null)
            {
                animatedSprite.Modulate = new Color(1, 0, 0); // Kırmızı
                GetTree().CreateTimer(0.1f).Timeout += () => {
                    if (GodotObject.IsInstanceValid(animatedSprite))
                        animatedSprite.Modulate = new Color(1, 1, 1);
                };
            }
        }

        // Notify parent (Enemy) to play hurt animation
        if (GetParent() is Enemy enemy)
        {
            enemy.PlayHurtAnimation();
        }

        GD.Print($"HealthComponent: Hasar alındı! Kalan Can: {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            EmitSignal("Died");
        }
    }
}
