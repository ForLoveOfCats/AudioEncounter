using Godot;
using static Godot.Mathf;



public partial class BoltImpactParticle : SaneParticle {
	private Vector3 Momentum = new Vector3();


	public override void InitParticle() {
		Momentum = new Vector3(0, (float)(Rng.NextDouble() * 4d) + 7f, -10f)
			.Rotated(new Vector3(0, 0, 1), DegToRad((float)(Rng.NextDouble() * 360d)))
			.Rotated(new Vector3(1, 0, 0), Rotation.X)
			.Rotated(new Vector3(0, 1, 0), Rotation.Y)
			.Rotated(new Vector3(0, 0, 1), Rotation.Z);
	}


	public override void TickParticle(float Delta) {
		Vector3 NewTranslation = Position + (Momentum * Delta);
		Transform = Transform.LookingAt(NewTranslation, new Vector3(0, 1, 0));
		Position = NewTranslation;
	}
}
