using Godot;
using static Godot.Mathf;

using static Assert;



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



public class SprintCamBob : CamAnimation {
	public const float MaxTime = 0.26f;
	public const float MaxVert = 0.3f;
	public const float MaxHorz = 0.6f;

	public int Direction = 1;
	public float CurrentTime = 0;


	public SprintCamBob(int DirectionArg) {
		ActualAssert(DirectionArg == -1 || DirectionArg == 1);
		Direction = DirectionArg;
	}

	public void Tick(Spatial CamJoint, float Delta) {
		CurrentTime = Clamp(CurrentTime + Delta, 0, MaxTime);

		float Upper = Sin((CurrentTime / MaxTime) * Pi);

		float Horz = Upper * MaxHorz;
		float Vert = Upper * MaxVert;

		CamJoint.Translation = new Vector3(
			CamJoint.Translation.x + (Horz * Direction),
			CamJoint.Translation.y + Vert,
			CamJoint.Translation.z
		);
	}

	public bool ReachedEnd() {
		return CurrentTime >= MaxTime;
	}
}
