using Godot;
using System;

public partial class Hurtbox : Area2D
{
    [Signal]
    public delegate void DamagedEventHandler(int amount);

    public override void _Ready()
    {
        // Sinyal bağlantısı kaldırıldı - manuel çağrı ile çalışacak
    }

    private void OnDamaged(int amount)
    {
        var health = GetParent()?.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health != null)
        {
            health.TakeDamage(amount);
        }
    }

    public void TakeDamage(int amount)
    {
        OnDamaged(amount);
    }
}
