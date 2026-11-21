using Godot;
using static Godot.Mathf;


public static class SteelMath {
	public static float LogBase(float x, float Base) {
		return Log(x) / Log(Base);
	}


	public static float SafeSign(float Input) {
		//Equality check matches +0 and -0
		if(Input == 0f || Input > 0)
			return 1f;
		else
			return -1f;
	}


	public static float SnapToGrid(float ToSnap, int GridSize, int DivisionCount) {
		return Mathf.Round(ToSnap / (GridSize / DivisionCount)) * (GridSize / DivisionCount);
	}


	public static float LoopRotation(float Rot) {
		Rot = Rot % 360;

		if(Rot < 0)
			Rot += 360;

		if(Rot == 360)
			Rot = 0;

		return Rot;
	}


	public static Vector3 ClampVec3(Vector3 Vec, float Min, float Max) {
		return Vec.Normalized() * Mathf.Clamp(Vec.Length(), Min, Max);
	}


	public static Vector3 RoundVec3(Vector3 Vec) {
		return new Vector3(Round(Vec.X), Round(Vec.Y), Round(Vec.Z));
	}


	public static Vector3 Flattened(this Vector3 Self) {
		return new Vector3(Self.X, 0, Self.Z);
	}


	public static Vector3 Inflated(this Vector3 Self, Vector3 Source) {
		return new Vector3(Source.X, Self.Y, Source.Z);
	}


	public static Vector2 ClampVec2(Vector2 Vec, float Min, float Max) {
		return Vec.Normalized() * Mathf.Clamp(Vec.Length(), Min, Max);
	}


	public static Color LerpColor(Color A, Color B, float T) {
		Color Out = new Color();
		Out.R = Lerp(A.R, B.R, T);
		Out.G = Lerp(A.G, B.G, T);
		Out.B = Lerp(A.B, B.B, T);
		Out.A = Lerp(A.A, B.A, T);
		return Out;
	}


	public static float RandomSign() {
		return SafeSign(GD.Randf() - 0.5f);
	}
}
