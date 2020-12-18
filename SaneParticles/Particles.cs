using Godot;



public enum Particle { PISTOL_IMPACT }


public class Particles : Node {
	private static PackedScene PistolImpactScene = GD.Load<PackedScene>("res://SaneParticles/BoltImpactParticles.tscn");

	public static Particles Self;


	public override void _Ready() {
		if(Engine.EditorHint) {
			return;
		}

		Self = this;

		base._Ready();
	}


	public static void Spawn(Particle Kind, Vector3 Position, Vector3 Normal) {
		Self.SpawnAt(Kind, Position, Normal);
		Self.Rpc(nameof(SpawnAt), Kind, Position, Normal);
	}


	[Remote]
	private void SpawnAt(Particle Kind, Vector3 Position, Vector3 Normal) {
		SaneParticles Impact = null;
		switch(Kind) {
			case Particle.PISTOL_IMPACT: {
				Impact = (SaneParticles)PistolImpactScene.Instance();
				break;
			}
		}

		if(Impact == null) {
			return;
		}

		Game.RuntimeRoot.AddChild(Impact);
		Impact.Translation = Position;
		Impact.GlobalTransform = Impact.GlobalTransform.LookingAt(Position + Normal, new Vector3(0, 1, 0));
		Impact.Scale = new Vector3(1, 1, 1);
	}
}
