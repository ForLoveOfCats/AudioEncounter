using System.Collections.Generic;
using Godot;
using static Godot.Mathf;
using static SteelMath;



public class FirstPersonPlayer : Character {
	public const float MouseSens = 0.2f;
	public const float MaxFallSpeed = 120f;
	public const float Gravity = MaxFallSpeed / 0.65f;
	public const float BaseSpeed = 12f;
	public const float SprintSpeed = 22f;
	public const float Acceleration = BaseSpeed / 0.04f;
	public const float Friction = Acceleration / 2f;

	public const float FootstepBaseTime = 0.78f;
	public const float SprintingFootstepAcceleration = 2.15f;
	public const float CrunchSpeed = -40f;

	public Spatial CamJoint;
	public Camera Cam;

	public AudioStreamPlayer FallCrunch;
	public Footstep2D ConcreteFootsteps;


	public int BackwardForwardDirection = 0;
	public int RightLeftDirection = 0;
	public Vector3 Momentum = new Vector3();
	public List<CamAnimation> CamAnimations = new List<CamAnimation>();
	public float FootstepCountdown = FootstepBaseTime;


	public class CamAnimation {
		public const float MaxTime = 0.45f;
		public const float MaxValue = 18f;

		public float CurrentTime = MaxTime;

		public float Tick(float Delta) {
			CurrentTime = Clamp(CurrentTime - Delta, 0, MaxValue);
			float Squared = CurrentTime * CurrentTime;
			return ((Sin(CurrentTime / MaxTime * Pi) * Squared) / (0.4f * MaxTime)) * (1 / MaxTime) * MaxValue;
		}

		public bool ReachedEnd() {
			return CurrentTime <= 0;
		}
	}


	public override void _Ready() {
		base._Ready();

		CamJoint = GetNode<Spatial>("CamJoint");
		Cam = CamJoint.GetNode<Camera>("Cam");

		FallCrunch = GetNode<AudioStreamPlayer>("FallCrunch");
		ConcreteFootsteps = GetNode<Footstep2D>("ConcreteFootsteps");

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
		} else {
			float Decrement = Delta;
			if(Input.IsActionPressed("Sprint")) {
				Decrement *= SprintingFootstepAcceleration;
			}

			FootstepCountdown -= Decrement;
			if(FootstepCountdown <= 0) {
				ConcreteFootsteps.Play();
				FootstepCountdown = FootstepBaseTime;
			}
		}

		if(OnFloor) {
			Momentum.y = 0;
		}
		else {
			Momentum.y = Clamp(Momentum.y - Gravity * Delta, -MaxFallSpeed, MaxFallSpeed);
		}

		var Push = new Vector3(
			RightLeftDirection * Acceleration * Delta,
			0,
			BackwardForwardDirection * Acceleration * Delta
		).Rotated(new Vector3(0, 1, 0), Rotation.y);
		Momentum = Momentum.Inflated(ClampVec3(Momentum.Flattened() + Push, 0, MaxSpeed));
	}


	public override void _PhysicsProcess(float Delta) {
		HandleMovementInput(Delta);

		bool WasOnFloor = OnFloor;
		float OldMomentumY = Momentum.y;
		Momentum = Move(Momentum, Delta, 5, 40, 0.42f);
		if(!WasOnFloor && OnFloor && OldMomentumY < CrunchSpeed) {
			FallCrunch.Play();
			CamAnimations.Add(new CamAnimation());
		}

		int Index = 0;
		float Combined = 0;
		while(Index < CamAnimations.Count) {
			Combined -= CamAnimations[Index].Tick(Delta);
			if(CamAnimations[Index].ReachedEnd()) {
				CamAnimations.RemoveAt(Index);
			} else {
				Index += 1;
			}
		}

		CamJoint.RotationDegrees = new Vector3(Combined, 0, 0);

		base._PhysicsProcess(Delta);
	}
}
