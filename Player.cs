using Godot;
using System;

public partial class Player : CharacterBody2D
{
	
	[Export]
	public float Speed = 200f;

	public override void _Ready()
	{
		var cam = GetNodeOrNull<Camera2D>("Camera2D");
		if (cam == null)
		{
			GD.PrintErr("Player: Camera2D node named 'Camera2D' not found as a child.");
			return;
		}
		cam.MakeCurrent();
	}
	

	
		private void Attack()
		{
			GD.Print("Player attacks!");
			// Buraya saldırı animasyonu, efekt veya mantığı eklenebilir.
		}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Vector2.Zero;

		if (Input.IsActionJustPressed("attack"))
		{
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

		Velocity = velocity;
		MoveAndSlide();
	}
}
