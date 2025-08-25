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
    
    [Export]
    public float DashSpeed = 500f;
    [Export]
    public float DashDuration = 0.25f;

    private Hitbox _attackHitbox;
    private Vector2 _velocity = Vector2.Zero;
    private Node2D _visuals;
    private AnimatedSprite2D _animatedSprite;
    private bool _isAttacking = false;
    private bool _isJumping = false;
    private bool _isAttackingRun = false;
    private bool _isDashing = false;
    private bool _canAirDash = true;

    private uint _originalCollisionLayer; 
    private uint _originalCollisionMask;
    private CollisionShape2D _hurtboxCollisionShape;

    public override void _Ready()
    {
        _originalCollisionLayer = this.CollisionLayer;
        _originalCollisionMask = this.CollisionMask;

        var hurtbox = GetNodeOrNull<Area2D>("Hurtbox");
        if (hurtbox != null)
        {
            _hurtboxCollisionShape = hurtbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (_hurtboxCollisionShape == null) GD.PrintErr("Player: 'Hurtbox' node has no 'CollisionShape2D'.");
        }
        else
        {
            GD.Print("Player: 'Hurtbox' node not found.");
        }
        
        var cam = GetNodeOrNull<Camera2D>("Camera2D");
        if (cam != null) cam.MakeCurrent();
        var sword = GetNodeOrNull<Node>("Visuals")?.GetNodeOrNull<Node>("Sword");
        if (sword != null) _attackHitbox = sword.GetNodeOrNull<Hitbox>("Hitbox");
        var healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent != null) healthComponent.Died += OnPlayerDied;
        _visuals = GetNode<Node2D>("Visuals");
        _animatedSprite = _visuals.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (_animatedSprite != null) _animatedSprite.AnimationFinished += OnAnimationFinished;
    }

    private async void Dash(float direction)
    {
        _isDashing = true;
        _velocity.Y = 0;

        this.CollisionLayer = (1U << 9); 
        this.CollisionMask = (1U << 1); 

        if (_hurtboxCollisionShape != null)
        {
            _hurtboxCollisionShape.SetDeferred("disabled", true);
        }

        // DEĞİŞTİRİLDİ: Artık yönü kendisi belirlemiyor, parametreden gelen yönü kullanıyor.
        // float direction = _visuals.Scale.X; // BU SATIR SİLİNDİ VEYA YORUM YAPILDI
        _velocity.X = direction * DashSpeed;

        // YENİ: Dash yönüne göre karakterin görselini anında çevir.
        _visuals.Scale = new Vector2(direction, _visuals.Scale.Y);

        _animatedSprite.Play("dash");

        await ToSignal(GetTree().CreateTimer(DashDuration), "timeout");

        _velocity.X = 0;
        _isDashing = false;

        if (GodotObject.IsInstanceValid(this))
        {
            this.CollisionLayer = _originalCollisionLayer;
            this.CollisionMask = _originalCollisionMask;
            if (_hurtboxCollisionShape != null)
            {
                _hurtboxCollisionShape.SetDeferred("disabled", false);
            }
        }
    }
    
    
    #region Mevcut Fonksiyonlar
    // Player.cs içindeki Attack() fonksiyonunun son hali
private async void Attack()
{
    // Koşarak Saldırı Kısmı
    if (Mathf.Abs(Velocity.X) > 0.1f)
    {
        _isAttackingRun = true;
        _velocity.X = 0;
        _animatedSprite.Play("attack_run");
        if (_attackHitbox != null)
        {
            await ToSignal(GetTree().CreateTimer(0.333f), "timeout");

            // YENİ KONTROL: Bekleme bittiğinde, saldırının hala aktif olup olmadığını kontrol et.
            if (!_isAttackingRun) return; // Eğer saldırı iptal edildiyse, devam etme.

            GD.Print($"Player: Enabling sword hitbox for 0.2s. (attack_run)");
            _attackHitbox.Enable();
        }
    }
    // Normal Saldırı Kısmı
    else
    {
        _isAttacking = true;
        _animatedSprite.Play("attack");
        if (_attackHitbox != null)
        {
            await ToSignal(GetTree().CreateTimer(0.222f), "timeout");

            // YENİ KONTROL: Bekleme bittiğinde, saldırının hala aktif olup olmadığını kontrol et.
            if (!_isAttacking) return; // Eğer saldırı iptal edildiyse, devam etme.

            GD.Print($"Player: Enabling sword hitbox for 0.2s. (1)");
            _attackHitbox.Enable();
            
            await ToSignal(GetTree().CreateTimer(0.333f), "timeout");

            // YENİ KONTROL: İkinci bekleme bittiğinde de kontrol et.
            if (!_isAttacking) return; // Eğer saldırı iptal edildiyse, devam etme.

            GD.Print($"Player: Enabling sword hitbox for 0.2s. (2)");
            _attackHitbox.Enable();
        }
    }
}
    private void OnAnimationFinished()
    {
        if (_animatedSprite == null) return;
        string animName = _animatedSprite.Animation;
        if (animName == "attack") _isAttacking = false;
        else if (animName == "attack_run") _isAttackingRun = false;
        else if (animName == "jump") _isJumping = false;
    }
    private void OnPlayerDied() { QueueFree(); }
    public override void _PhysicsProcess(double delta)
    {
        float direction = 0f;
        if (!_isAttackingRun && !_isAttacking && !_isDashing)
        {
            if (Input.IsActionPressed("move_right")) direction += 1f;
            if (Input.IsActionPressed("move_left")) direction -= 1f;
        }
        if (!_isDashing) { _velocity.X = direction * Speed; }
        if (!IsOnFloor())
        {
            if (_isDashing)
            {
                // Eğer dash yapıyorsa, dikey hızı sıfırda tut (yerçekimi yok).
                _velocity.Y = 0;
            }
            else
            {
                // Eğer dash yapmıyorsa, normal yerçekimini uygula.
                _velocity.Y += Gravity * (float)delta;
            }
        }
        else
        {
            _velocity.Y = 0f;
            // Sadece dash yapmıyorsa dash hakkını yenile.
            if (!_isDashing)
            {
                _canAirDash = true;
            }
        }
        if (IsOnFloor() && Input.IsActionJustPressed("jump") && !_isDashing)
        {
            _velocity.Y = JumpVelocity;
            if (_isAttacking) _isAttacking = false;
            if (_isAttackingRun) _isAttackingRun = false;
            if (_attackHitbox != null) {
                _attackHitbox.Disable();
            }
            _isJumping = true;
            _animatedSprite.Play("jump");
        }
        if (Input.IsActionJustPressed("attack") && IsOnFloor() && !_isJumping && !_isAttacking && !_isAttackingRun && !_isDashing) { Attack(); }
        if (Input.IsActionJustPressed("dash") && !_isDashing)
        {
            if (IsOnFloor() || _canAirDash)
            {
                // 1. Varsayılan dash yönünü karakterin o an baktığı yön olarak ayarla.
                float dashDirection = _visuals.Scale.X;
    
                // 2. Dash anında hangi tuşa basıldığını anlık olarak kontrol et.
                if (Input.IsActionPressed("move_right"))
                {
                    dashDirection = 1f;
                }
                else if (Input.IsActionPressed("move_left"))
                {
                    dashDirection = -1f;
                }
                // Eğer hiçbir tuşa basılmıyorsa, varsayılan yönde (baktığı yönde) dash atar.
    
                if (!IsOnFloor()) { _canAirDash = false; }
    
                // Saldırıları iptal et
                if (_isAttacking) _isAttacking = false;
                if (_isAttackingRun) _isAttackingRun = false;
                if (_attackHitbox != null) _attackHitbox.Disable();
                _isJumping = false;
    
                // 3. Tespit ettiğimiz yönü Dash() fonksiyonuna parametre olarak gönder.
                Dash(dashDirection);
            }
        }
        Velocity = _velocity;
        if (!_isDashing)
        {
            if (Velocity.X > 0) _visuals.Scale = new Vector2(1, _visuals.Scale.Y);
            else if (Velocity.X < 0) _visuals.Scale = new Vector2(-1, _visuals.Scale.Y);
        }
    if (_animatedSprite != null && !_isAttacking && !_isAttackingRun && !_isDashing) // DEĞİŞTİRİLDİ: !_isJumping kaldırıldı
    {
        if (!IsOnFloor())
        {
            // EĞER HAVADAYSAK:
            // Her zaman "jump" animasyonunu oynattığımızdan emin olalım.
            if (_animatedSprite.Animation != "jump")
            {
                _animatedSprite.Play("jump");
            }

            if (_velocity.Y < 0) // Yükseliyorsak (zıplamanın başı)
            {
                // Yükselirken animasyonun 0-3 arası karelerde kalmasını sağlayalım.
                // Eğer düşüş karesine geçtiyse başa sar.
                if (_animatedSprite.Frame >= 4)
                {
                    _animatedSprite.Frame = 0;
                }
            }
            else // Düşüyorsak (_velocity.Y >= 0)
            {
                // Düşerken animasyonun 4. kareden başlamasını ve orada kalmasını sağlayalım.
                // Eğer hala yükselme karesindeyse, düşme karesine zorla.
                if (_animatedSprite.Frame < 4)
                {
                    _animatedSprite.Frame = 4;
                }
            }
        }
        else
        {
            // EĞER YERDEYSEK: Eski mantık devam etsin.
            if (Mathf.Abs(_velocity.X) > 0.1f)
            {
                _animatedSprite.Play("run");
            }
            else
            {
                _animatedSprite.Play("idle");
            }
        }
    }        
    MoveAndSlide();
    }
    #endregion
}