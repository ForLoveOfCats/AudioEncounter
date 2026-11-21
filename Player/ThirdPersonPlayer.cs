using Godot;

using static Assert;



public partial class ThirdPersonPlayer : Node3D {
	public long Id = -1;
	public string Nickname = "";

	public bool ValidTargetTransform = false;
	public Transform3D TargetTransform = new Transform3D();
	public float CrouchPercent = 0f;

	public Node3D Joint3D;
	public MeshInstance3D Mesh;
	public Hitbox Head;
	public Hitbox Feet;

	public Node3D CamJoint;
	public Camera3D Cam;
	public WeaponHolder Holder;


	public override void _Ready() {
		Joint3D = GetNode<Node3D>("EverythingJoint");
		Mesh = Joint3D.GetNode<MeshInstance3D>("Mesh");
		Head = Joint3D.GetNode<Hitbox>("HeadHitbox");
		Feet = Joint3D.GetNode<Hitbox>("FeetHitbox");

		CamJoint = GetNode<Node3D>("CamJoint");
		Cam = CamJoint.GetNode<Camera3D>("Cam");
		Holder = Cam.GetNode<WeaponHolder>("WeaponHolder");

		Cam.Current = false;

		base._Ready();
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetUpdateTransform(Transform3D NewTransform, float NewCrouchPercent) {
		TargetTransform = NewTransform;
		ValidTargetTransform = true;
		CrouchPercent = NewCrouchPercent;
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetUpdateSpecateCam(float CamJointRotation, float CamRotation, float CamFov) {
		CamJoint.Rotation = new Vector3(CamJointRotation, 0, 0);
		Cam.Rotation = new Vector3(CamRotation, 0, 0);
		Cam.Fov = CamFov;
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetSetSpectateWeapon(WeaponKind Kind) {
		GD.Print($"Set spectate weapon {Id}, {Kind}");
		if(Kind == WeaponKind.PISTOL) {
			Holder.EquipPistol();
		}
		else if(Kind == WeaponKind.AK) {
			Holder.EquipAk();
		}
		else if(Kind == WeaponKind.SHOTGUN) {
			Holder.EquipShotgun();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetUpdateWeaponHolder(Vector2 Momentum, float ReloadHidePercent, float SprintTime, float AdsTime) {
		Holder.Momentum = Momentum;
		Holder.ReloadHidePercent = ReloadHidePercent;
		Holder.SprintTime = SprintTime;
		Holder.AdsTime = AdsTime;
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetDie(long Killer) {
		Game.Alive.Remove(Id);

		if(Game.Self.Spectating == this) {
			if(Game.Alive.Count > 0) {
				Game.SpectateNextPlayer();
			}
			else {
				Game.Self.Spectating = null;
			}
		}

		if(Multiplayer.IsServer()) {
			string Message = "";
			if(Killer == -1) {
				Message = $"{Game.Nicknames[Id]} fell to their death";
			}
			else if(Killer == Game.ServerId) {
				Message = $"{Game.Nicknames[Id]} was killed by the server";
			}
			else {
				Message = $"{Game.Nicknames[Killer]} ended the life of {Game.Nicknames[Id]}";
			}

			Game.Self.Rpc(nameof(Game.NetNotifyKillfeed), Message);
		}

		QueueFree();
	}


	public void Spectate() {
		Game.Self.Spectating = this;
		Cam.MakeCurrent();
	}


	public override void _Process(double Delta) {
		Joint3D.Visible = Game.Self.Spectating != this;

		if(ValidTargetTransform) {
			Transform = Transform.InterpolateWith(TargetTransform, (float)Delta / 0.02f);
		}

		Joint3D.RotationDegrees = new Vector3(
			-45 * CrouchPercent,
			0,
			0
		);

		float Percent = 1 - (CrouchPercent / 2f);
		Head.Position = new Vector3(0, 1.3f * Percent, 0);
		Feet.Position = new Vector3(0, -1.3f * Percent, 0);
		Joint3D.Position = new Vector3(0, CrouchPercent / 2f, 0);

		((CapsuleMesh)Mesh.Mesh).Height = 2 - CrouchPercent;
		CamJoint.Position = new Vector3(0, 1.75f - CrouchPercent, 0);

		base._Process(Delta);
	}
}
