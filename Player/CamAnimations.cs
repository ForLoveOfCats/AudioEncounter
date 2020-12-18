using Godot;
using static Godot.Mathf;



public interface CamAnimation {
	void Tick(Spatial CamJoint, float Delta);

	bool ReachedEnd();
}



public class CrunchCamDip : CamAnimation {
	public const float MaxTime = 0.45f;
	public const float MaxValue = 18f;

	public float CurrentTime = MaxTime;

	public void Tick(Spatial CamJoint, float Delta) {
		CurrentTime = Clamp(CurrentTime - Delta, 0, MaxTime);
		float Squared = CurrentTime * CurrentTime;
		float RotX = ((Sin(CurrentTime / MaxTime * Pi) * Squared) / (0.4f * MaxTime)) * (1 / MaxTime) * MaxValue;

		CamJoint.RotationDegrees = new Vector3(
			CamJoint.RotationDegrees.x - RotX,
			CamJoint.RotationDegrees.y,
			CamJoint.RotationDegrees.z
		);
	}

	public bool ReachedEnd() {
		return CurrentTime <= 0;
	}
}



public class WeaponRecoil : CamAnimation {
	public float MaxTime = 0.35f;
	public float MaxValue = 7f;

	public float CurrentTime = 0;

	public WeaponRecoil(float MaxTimeArg, float MaxValueArg) {
		MaxTime = MaxTimeArg;
		MaxValue = MaxValueArg;
		CurrentTime = MaxTime;
	}

	public void Tick(Spatial CamJoint, float Delta) {
		CurrentTime = Clamp(CurrentTime - Delta, 0, MaxTime);
		float Squared = CurrentTime * CurrentTime;
		float RotX = ((Sin(CurrentTime / MaxTime * Pi) * Squared) / (0.4f * MaxTime)) * (1 / MaxTime) * MaxValue;

		CamJoint.RotationDegrees = new Vector3(
			CamJoint.RotationDegrees.x + RotX,
			CamJoint.RotationDegrees.y,
			CamJoint.RotationDegrees.z
		);
	}

	public bool ReachedEnd() {
		return CurrentTime <= 0;
	}
}
