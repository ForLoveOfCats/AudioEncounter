using Godot;



public class ThirdPersonPlayer : Spatial {
	public int Id = -1;

	public bool ValidTargetTransform = false;
	public Transform TargetTransform = new Transform();
	public float CrouchPercent = 0f;

	public Spatial Joint;
	public MeshInstance Mesh;
	public Hitbox Head;
	public Hitbox Feet;


	public override void _Ready() {
		Joint = GetNode<Spatial>("EverythingJoint");
		Mesh = Joint.GetNode<MeshInstance>("Mesh");
		Head = Joint.GetNode<Hitbox>("HeadHitbox");
		Feet = Joint.GetNode<Hitbox>("FeetHitbox");

		base._Ready();
	}


	[Remote]
	public void NetUpdateTransform(Transform NewTransform, float NewCrouchPercent) {
		TargetTransform = NewTransform;
		ValidTargetTransform = true;
		CrouchPercent = NewCrouchPercent;
	}


	[Remote]
	public void NetDie() {
		if(Multiplayer.IsNetworkServer()) {
			Game.Alive.Remove(Id);
		}

		QueueFree();
	}


	public override void _Process(float Delta) {
		if(ValidTargetTransform) {
			Transform = Transform.InterpolateWith(TargetTransform, Delta / 0.1f);
		}

		Joint.RotationDegrees = new Vector3(
			-45 * CrouchPercent,
			0,
			0
		);

		float Percent = 1 - (CrouchPercent / 2f);
		Head.Translation = new Vector3(0, 1.3f * Percent, 0);
		Feet.Translation = new Vector3(0, -1.3f * Percent, 0);
		Joint.Translation = new Vector3(0, CrouchPercent / 2f, 0);

		((CapsuleMesh)Mesh.Mesh).MidHeight = 2 - CrouchPercent;

		base._Process(Delta);
	}
}
