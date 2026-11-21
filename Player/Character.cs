using System;
using Godot;
using static Godot.Mathf;



public partial class Character : CharacterBody3D {
	public bool OnFloor { get; private set; } = false;


	public Vector3 Move(Vector3 Momentum, float Delta, int MaxSlideCount, float MaxAngle, float Snap) {
		Vector3 Movement = Momentum * Delta;

		if(Momentum.Y <= 0) {
			Vector3 OriginalTranslation = Position;
			var SnapVec = new Vector3(0, -Snap, 0);
			KinematicCollision3D SnapCollision = MoveAndCollide(SnapVec);

			if(SnapCollision != null && Acos(SnapCollision.GetNormal().Dot(new Vector3(0, 1, 0))) <= DegToRad(MaxAngle)) {
				OnFloor = true;

				float TargetHLength = Movement.Flattened().Length();
				Movement = Movement.Slide(SnapCollision.GetNormal());
				float NewHLength = Movement.Flattened().Length();
				if(TargetHLength != 0 && NewHLength != 0) //Protect against division by zero
					Movement *= TargetHLength / Movement.Flattened().Length();

				Position = OriginalTranslation;
				Momentum.Y = 0; //On floor so "zero" out vertical momentum
			}
			else {
				Position = OriginalTranslation;
				OnFloor = false;
			}
		}
		else
			OnFloor = false;

		int SlideCount = 0;
		float Traveled = 0f;
		while(SlideCount <= MaxSlideCount) {
			Movement = Movement.Normalized() * (Movement.Length() - Traveled);
			KinematicCollision3D Collision = MoveAndCollide(Movement);
			if(Collision == null) {
				break; //No collision, reached destination
			}

			Traveled += Collision.GetTravel().Length();
			if(Traveled >= Movement.Length())
				break; //Reached destination
			SlideCount += 1;

			//Dirty hack to prevent horizontal nudging when landing
			bool SlideByNormal = true;
			float NormalAverageHorizontal = (Abs(Collision.GetNormal().X) + Abs(Collision.GetNormal().Z)) / 2f;
			if(Collision.GetNormal().Y >= NormalAverageHorizontal)
				SlideByNormal = false;

			if(SlideByNormal && Acos(Collision.GetNormal().Dot(new Vector3(0, 1, 0))) <= DegToRad(MaxAngle))
				Movement = Movement.Slide(Collision.GetNormal());

			if(Acos(Collision.GetNormal().Dot(new Vector3(0, 1, 0))) <= DegToRad(MaxAngle))
				OnFloor = true;
			else if(SlideByNormal)
				Momentum = Momentum.Slide(Collision.GetNormal());
		}

		if(OnFloor)
			Momentum.Y = 0f; //On floor so "zero" out vertical momentum
		Velocity = new Vector3();
		MoveAndSlide();
		return Momentum;
	}


	[Obsolete]
	public new bool IsOnFloor() {
		return OnFloor;
	}


	[Obsolete]
	public new bool IsOnWall() {
		return base.IsOnWall();
	}


	[Obsolete]
	public new bool IsOnCeiling() {
		return base.IsOnCeiling();
	}
}
