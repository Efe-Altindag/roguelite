using Godot;
using System;

public partial class Hitbox : Area2D
{
    [Export]
    public int Damage = 10;
    [Export]
    public float Duration = 0.2f;

    // DEĞİŞTİ: Artık CollisionShape'i doğrudan kontrol etmek daha temiz bir yöntem.
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {
        // CollisionShape'i başlangıçta kapalı tutalım.
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _collisionShape.Disabled = true;

        // DOĞRU SİNYAL: `area_entered` sinyaline bağlanıyoruz.
        Connect("area_entered", new Callable(this, nameof(OnAreaEntered)));
    }

    // YENİ METOD: `area_entered` sinyali bu metodu çağıracak.
    private void OnAreaEntered(Area2D area)
    {
        // Çarptığımız alan bir Hurtbox mı diye kontrol ediyoruz.
        if (area is Hurtbox hurtbox)
        {
            GD.Print($"Hitbox, bir Hurtbox algıladı: {hurtbox.Name}");
            // Doğrudan Hurtbox'ın TakeDamage metodunu çağırıyoruz.
            hurtbox.TakeDamage(Damage);
        }
    }

    // ESKİ METOD SİLİNDİ: OnBodyEntered artık kullanılmıyor.

    // DEĞİŞTİ: Enable metodu artık Monitoring yerine CollisionShape'i açıp kapatacak.
    public void Enable()
    {
        _collisionShape.Disabled = false;
        // Belirlenen süre sonunda CollisionShape'i tekrar kapat.
        GetTree().CreateTimer(Duration).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(this))
            {
                _collisionShape.Disabled = true;
            }
        };
    }
    public void Disable()
    {
        if (_collisionShape != null)
        {
            _collisionShape.Disabled = true;
        }
    }
}

