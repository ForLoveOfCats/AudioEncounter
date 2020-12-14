using Godot;
using static Godot.Mathf;
using static SteelMath;



public class FirstPersonPlayer : Character {
	public const float MouseSens = 0.2f;
	public const float BaseSpeed = 10f;
	public const float SprintSpeed = 22f;
	public const float Acceleration = BaseSpeed / 0.04f;
	public const float Friction = Acceleration / 2f;


	public Camera Cam;


	public int BackwardForwardDirection = 0;
	public int RightLeftDirection = 0;
	public Vector3 Momentum = new Vector3();


	public override void _Ready() {
		base._Ready();

		Cam = GetNode<Camera>("Cam");

		Input.SetMouseMode(Input.MouseMode.Captured);
	}


	public override void _Input(InputEvent Event) {
		if(Event is InputEventMouseMotion Motion) {
			RotateY(Deg2Rad(-Motion.Relative.x * MouseSens));
			Cam.RotationDegrees = new Vector3(
				Clamp(Cam.RotationDegrees.x - (Motion.Relative.y * MouseSens), -90, 90),
				Cam.RotationDegrees.y,
				Cam.RotationDegrees.z
			);
		}

		base._Input(Event);
	}


	public void HandleMovementInput(float Delta) {
		if(Input.IsActionJustPressed("Forward")) {
			BackwardForwardDirection = -1;
		}
		else if(BackwardForwardDirection == -1 && Input.IsActionJustReleased("Forward")) {
			BackwardForwardDirection = 0;
		}
		if(Input.IsActionJustPressed("Backward")) {
			BackwardForwardDirection = 1;
		}
		else if(BackwardForwardDirection == 1 && Input.IsActionJustReleased("Backward")) {
			BackwardForwardDirection = 0;
		}

		if(Input.IsActionJustPressed("Left")) {
			RightLeftDirection = -1;
		}
		else if(RightLeftDirection == -1 && Input.IsActionJustReleased("Left")) {
			RightLeftDirection = 0;
		}
		if(Input.IsActionJustPressed("Right")) {
			RightLeftDirection = 1;
		}
		else if(RightLeftDirection == 1 && Input.IsActionJustReleased("Right")) {
			RightLeftDirection = 0;
		}

		float MaxSpeed = BaseSpeed;
		if(Input.IsActionPressed("Sprint")) {
			MaxSpeed = SprintSpeed;
		}

		if(BackwardForwardDirection == 0 && RightLeftDirection == 0) {
			float FrictionModifiedSpeed = Clamp(Momentum.Flattened().Length() - (Friction * Delta), 0, MaxSpeed);
			Momentum = Momentum.Inflated(Momentum.Flattened().Normalized() * FrictionModifiedSpeed);
		}

		var Push = new Vector3(
			RightLeftDirection * Acceleration * Delta,
			0,
			BackwardForwardDirection * Acceleration * Delta
		).Rotated(new Vector3(0, 1, 0), Rotation.y);
		Momentum = ClampVec3(Momentum + Push, 0, MaxSpeed);
		GD.Print(Momentum.Length());
	}


	public override void _PhysicsProcess(float Delta) {
		HandleMovementInput(Delta);
		Momentum = Move(Momentum, Delta, 5, 40, 0.42f);

		base._PhysicsProcess(Delta);
	}
}
