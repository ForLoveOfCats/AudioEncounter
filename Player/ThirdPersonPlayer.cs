using Godot;



public class ThirdPersonPlayer : Spatial {
	public int Id = -1;
	public string Nickname = "";

	public bool ValidTargetTransform = false;
	public Transform TargetTransform = new Transform();
	public float CrouchPercent = 0f;

	public Spatial Joint;
	public MeshInstance Mesh;
	public Hitbox Head;
	public Hitbox Feet;

	public Spatial CamJoint;
	public Camera Cam;


	public override void _Ready() {
		Joint = GetNode<Spatial>("EverythingJoint");
		Mesh = Joint.GetNode<MeshInstance>("Mesh");
		Head = Joint.GetNode<Hitbox>("HeadHitbox");
		Feet = Joint.GetNode<Hitbox>("FeetHitbox");

		CamJoint = GetNode<Spatial>("CamJoint");
		Cam = CamJoint.GetNode<Camera>("Cam");

		Cam.Current = false;

		base._Ready();
	}


	[Remote]
	public void NetUpdateTransform(Transform NewTransform, float NewCrouchPercent) {
		TargetTransform = NewTransform;
		ValidTargetTransform = true;
		CrouchPercent = NewCrouchPercent;
	}


	[Remote]
	public void NetUpdateSpecateCam(float CamJointRotation, float CamRotation, float CamFov) {
		CamJoint.Rotation = new Vector3(CamJointRotation, 0, 0);
		Cam.Rotation = new Vector3(CamRotation, 0, 0);
		Cam.Fov = CamFov;
	}


	[Remote]
	public void NetDie() {
		Game.Alive.Remove(Id);
		QueueFree();
	}


	public void Spectate() {
		Game.Self.Spectating = this;
		Cam.MakeCurrent();
	}


	public override void _Process(float Delta) {
		if(Game.Self.Spectating == this) {
			Joint.Visible = false;
		}

		if(ValidTargetTransform) {
			Transform = Transform.InterpolateWith(TargetTransform, Delta / 0.02f);
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
