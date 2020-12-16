using System.Collections.Generic;

using Godot;
using static Godot.Mathf;

using static Assert;
using static SteelMath;



public enum MovementMode { WALKING, SNEAKING, SPRINTING };


public class FirstPersonPlayer : Character {
	public const float MouseSens = 0.2f;
	public const float MaxFallSpeed = 75f;
	public const float Gravity = MaxFallSpeed / 0.6f;
	public const float BaseSpeed = 8f;
	public const float SneakSpeed = 4f;
	public const float SprintSpeed = 24f;
	public const float AccelerationTime = 0.07f;
	public const float Acceleration = BaseSpeed / AccelerationTime;
	public const float Friction = Acceleration / 2f;

	public const float FootstepBaseTime = 0.6f;
	public const float SprintingFootstepAcceleration = 2.2f;
	public const float CrunchSpeed = -35f;
	public const float BaseCrunchDmg = 20f;

	public Spatial CamJoint;
	public Camera Cam;
	public Vector3 InitialCamJointPos;

	public RayCast FloorCast;

	FootstepChooser ConcreteChooser = new FootstepChooser(6);
	FootstepChooser LeavesChooser = new FootstepChooser(6);

	public int BackwardForwardDirection = 0;
	public int RightLeftDirection = 0;
	public Vector3 Momentum = new Vector3();
	public MovementMode Mode = MovementMode.WALKING;

	public int Health = 100;

	public List<CamAnimation> CamAnimations = new List<CamAnimation>();
	public float FootstepCountdown = 0;
	public int NextBobDirection = 1;


	public override void _Ready() {
		base._Ready();

		CamJoint = GetNode<Spatial>("CamJoint");
		Cam = CamJoint.GetNode<Camera>("Cam");
		InitialCamJointPos = CamJoint.Translation;

		FloorCast = GetNode<RayCast>("FloorCast");

		Input.SetMouseMode(Input.MouseMode.Captured);
	}


	[Remote]
	public void NetDamage(int Damage) {
		Health -= Damage;
		CheckDie();
	}


	public void CheckDie() {
		if(Health <= 0) {
			Rpc(nameof(ThirdPersonPlayer.NetDie));
			QueueFree();
		}
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


	public void HandleFootsteps(float Delta) {
		float Decrement = Delta;
		if(Input.IsActionPressed("Sprint")) {
			Decrement *= SprintingFootstepAcceleration;
		}
		FootstepCountdown -= Decrement;

		if(OnFloor && Mode != MovementMode.SNEAKING && (BackwardForwardDirection != 0 || RightLeftDirection != 0)) {
			if(FootstepCountdown <= 0) {
				FootstepCountdown = FootstepBaseTime;

				int Index = -1;
				var Catagory = (SfxCatagory)(-1);
				if(FloorCast.GetCollider() is Node Floor) {
					if(Floor.IsInGroup("concrete")) {
						Index = ConcreteChooser.Choose();
						Catagory = SfxCatagory.CONCRETE;
					}
					else if(Floor.IsInGroup("leaves")) {
						Index = LeavesChooser.Choose();
						Catagory = SfxCatagory.LEAVES;
					}
					else {
						Index = ConcreteChooser.Choose();
						Catagory = SfxCatagory.CONCRETE;
					}
				}

				if(Index != -1) {
					ActualAssert(Catagory != (SfxCatagory)(-1));
					Sfx.PlaySfx(Catagory, Index, GlobalTransform.origin);
				}
			}
		}
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
		if(Input.IsActionPressed("Sneak")) {
			MaxSpeed = SneakSpeed;
			Mode = MovementMode.SNEAKING;
		} else if(Input.IsActionPressed("Sprint") && !Input.IsActionPressed("ADS")) {
			MaxSpeed = SprintSpeed;
			if(OnFloor && (BackwardForwardDirection != 0 || RightLeftDirection != 0)) {
				Mode = MovementMode.SPRINTING;
			} else {
				Mode = MovementMode.WALKING;
			}
		} else {
			Mode = MovementMode.WALKING;
		}

		if(BackwardForwardDirection == 0 && RightLeftDirection == 0) {
			float FrictionModifiedSpeed = Clamp(Momentum.Flattened().Length() - (Friction * Delta), 0, MaxSpeed);
			Momentum = Momentum.Inflated(Momentum.Flattened().Normalized() * FrictionModifiedSpeed);
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
			Sfx.PlaySfx(SfxCatagory.FALL_CRUNCH, 0, GlobalTransform.origin);
			CamAnimations.Add(new CrunchCamDip());

			float Overkill = -(OldMomentumY - CrunchSpeed);
			float Percent = Overkill / (MaxFallSpeed + CrunchSpeed);
			float Damage = BaseCrunchDmg + (100f * Percent);
			Health -= (int)Damage;
			GD.Print("Overkill: ", Overkill, " Percent: ", Percent, " Damage: ", Damage);
			CheckDie();
		}

		HandleFootsteps(Delta);

		CamJoint.Translation = InitialCamJointPos;
		CamJoint.RotationDegrees = new Vector3();

		int Index = 0;
		while(Index < CamAnimations.Count) {
			CamAnimations[Index].Tick(CamJoint, Delta);

			if(CamAnimations[Index].ReachedEnd()) {
				CamAnimations.RemoveAt(Index);
			}
			else {
				Index += 1;
			}
		}

		Rpc(nameof(ThirdPersonPlayer.NetUpdateTransform), Transform);

		base._PhysicsProcess(Delta);
	}
}
