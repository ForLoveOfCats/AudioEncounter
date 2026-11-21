using Godot;
using static Godot.Mathf;



public interface CamAnimation {
	void Tick(Node3D CamJoint, float Delta);

	bool ReachedEnd();
}



public partial class CrunchCamDip : CamAnimation {
	public const float MaxTime = 0.45f;
	public const float MaxValue = 18f;

	public float CurrentTime = MaxTime;

	public void Tick(Node3D CamJoint, float Delta) {
		CurrentTime = Clamp(CurrentTime - Delta, 0, MaxTime);
		float Squared = CurrentTime * CurrentTime;
		float RotX = ((Sin(CurrentTime / MaxTime * Pi) * Squared) / (0.4f * MaxTime)) * (1 / MaxTime) * MaxValue;

		CamJoint.RotationDegrees = new Vector3(
			CamJoint.RotationDegrees.X - RotX,
			CamJoint.RotationDegrees.Y,
			CamJoint.RotationDegrees.Z
		);
	}

	public bool ReachedEnd() {
		return CurrentTime <= 0;
	}
}



public partial class WeaponRecoil : CamAnimation {
	public float MaxTime = 0.35f;
	public float MaxValue = 7f;
	public float CrouchPercent = 0f;

	public float CurrentTime = 0;

	public WeaponRecoil(float MaxTimeArg, float MaxValueArg, float CrouchPercentArg) {
		MaxTime = MaxTimeArg;
		MaxValue = MaxValueArg;
		CrouchPercent = CrouchPercentArg;
		CurrentTime = MaxTime;
	}

	public void Tick(Node3D CamJoint, float Delta) {
		CurrentTime = Clamp(CurrentTime - Delta, 0, MaxTime);
		float Squared = CurrentTime * CurrentTime;
		float RotX = ((Sin(CurrentTime / MaxTime * Pi) * Squared) / (0.4f * MaxTime)) * (1 / MaxTime) * MaxValue;

		RotX *= 1 - (CrouchPercent / 4f);

		CamJoint.RotationDegrees = new Vector3(
			CamJoint.RotationDegrees.X + RotX,
			CamJoint.RotationDegrees.Y,
			CamJoint.RotationDegrees.Z
		);
	}

	public bool ReachedEnd() {
		return CurrentTime <= 0;
	}
}
