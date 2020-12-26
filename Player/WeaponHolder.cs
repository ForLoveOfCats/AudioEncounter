using Godot;
using static Godot.Mathf;



public enum WeaponKind {
	AK,
	PISTOL,
}


public class WeaponStats {
	public WeaponKind Kind = WeaponKind.AK;

	public int MaxAmmo = 0;
	public float MaxFireTime = 0f;
	public float MaxReloadTime = 0f;
	public bool FullAuto = false;

	public float RecoilAmount = 0f;
	public float RecoilTime = 0f;

	public int HeadDamage = 0;
	public int BodyDamage = 0;

	public int CurrentAmmo = 0;
	public float FireTimer = 0f;
	public float ReloadTimer = 0f;
}


public class WeaponHolder : Spatial {
	public const float Range = 500f;
	public const float MomentumFriction = 3f;
	public const float MaxRotation = 8f;
	public const float SprintChangeStateTime = 0.2f;
	public const float AdsChangeStateTime = FirstPersonPlayer.SprintSpeed / FirstPersonPlayer.BaseSpeed * FirstPersonPlayer.AccelerationTime;

	public Spatial MeshJoint;
	public MeshInstance AkMesh;
	public MeshInstance PistolMesh;

	public Vector3 OgTranslation = new Vector3();

	public Vector2 Momentum = new Vector2();
	public bool Reloading = false;
	public float ReloadHidePercent = 0f;
	public float SprintTime = 0f;
	public float AdsTime = 0f;

	FirstPersonPlayer ParentPlayer = null;
	ClipChooser TinkChooser = new ClipChooser(4);

	public WeaponStats CurrentWeapon = null;


	public override void _Ready() {
		MeshJoint = GetNode<Spatial>("MeshJoint");
		AkMesh = MeshJoint.GetNode<MeshInstance>("AkMesh");
		PistolMesh = MeshJoint.GetNode<MeshInstance>("PistolMesh");

		OgTranslation = Translation;
		ParentPlayer = (FirstPersonPlayer)GetParent().GetParent().GetParent();

		int Choice = Game.Rng.Next(2);
		if(Choice == 0) {
			EquipAk();
		}
		else if(Choice == 1) {
			EquipPistol();
		}

		base._Ready();
	}


	public void EquipAk() {
		AkMesh.Visible = true;
		PistolMesh.Visible = false;

		CurrentWeapon = new WeaponStats() {
			Kind = WeaponKind.AK,
			MaxAmmo = 30,
			CurrentAmmo = 30,
			MaxFireTime = 0.09f,
			MaxReloadTime = 4f,
			FullAuto = true,
			HeadDamage = 30,
			BodyDamage = 25,
			RecoilTime = 0.4f,
			RecoilAmount = 10f,
		};
	}


	public void EquipPistol() {
		AkMesh.Visible = false;
		PistolMesh.Visible = true;

		CurrentWeapon = new WeaponStats() {
			Kind = WeaponKind.PISTOL,
			MaxAmmo = 8,
			CurrentAmmo = 8,
			MaxFireTime = 0.1f,
			MaxReloadTime = 2f,
			FullAuto = false,
			HeadDamage = 40,
			BodyDamage = 20,
			RecoilTime = 0.35f,
			RecoilAmount = 6f,
		};
	}


	public void TickFireTime(WeaponStats Weapon, float Delta) {
		Weapon.FireTimer = Clamp(Weapon.FireTimer - Delta, 0, Weapon.MaxFireTime);
	}


	public void PerformHitscan() {
		float VerticalDeviation = 0;
		float HorizontalDeviation = 0;

		Vector3 Origin = ParentPlayer.Cam.GlobalTransform.origin;
		Vector3 Endpoint = Origin + new Vector3(0, 0, -Range)
			.Rotated(new Vector3(1, 0, 0), Deg2Rad(ParentPlayer.CamJoint.RotationDegrees.x + ParentPlayer.Cam.RotationDegrees.x + VerticalDeviation))
			.Rotated(new Vector3(0, 1, 0), Deg2Rad(ParentPlayer.RotationDegrees.y + HorizontalDeviation));

		var Exclude = new Godot.Collections.Array() { ParentPlayer };
		PhysicsDirectSpaceState State = GetWorld().DirectSpaceState;
		Godot.Collections.Dictionary Results = State.IntersectRay(Origin, Endpoint, Exclude, 1 | 2);

		if(Results.Count > 0) {
			var Position = (Vector3)Results["position"];
			var Normal = (Vector3)Results["normal"];

			if(Results["collider"] is Hitbox Box) {
				int Damage = CurrentWeapon.BodyDamage;
				if(Box.Kind == HitboxKind.HEAD) {
					Damage = CurrentWeapon.HeadDamage;
				}

				Box.Damage(Damage);

				Sfx.PlaySfxSpatially(SfxCatagory.FLESH_HIT, 0, Position, 0);
			}
			else {
				Sfx.PlaySfxSpatially(SfxCatagory.BULLET_HIT, 0, Position, 0);
			}

			Particles.Spawn(Particle.PISTOL_IMPACT, Position, Normal);
		}
	}


	public void RunFireEffects() {
		if(CurrentWeapon.Kind == WeaponKind.PISTOL) {
			Sfx.PlaySfx(SfxCatagory.PISTOL_FIRE, 0, GlobalTransform.origin, 0);
		}
		else if(CurrentWeapon.Kind == WeaponKind.AK) {
			Sfx.PlaySfx(SfxCatagory.AK_FIRE, 0, GlobalTransform.origin, 0);
		}

		float RecoilDampen = ((1 - CalcAdsDisplay()) + ParentPlayer.CrouchPercent) / 2f;
		ParentPlayer.CamAnimations.Add(new WeaponRecoil(CurrentWeapon.RecoilTime, CurrentWeapon.RecoilAmount, RecoilDampen));

		int Index = TinkChooser.Choose();
		Sfx.PlaySfxSpatially(SfxCatagory.CASING_TINK, Index, GlobalTransform.origin + new Vector3(0, -2, 0), 0);
	}


	public float CalcAdsDisplay() {
		float AdsPercent = 1 - AdsTime / AdsChangeStateTime;
		float AdsDisplay = Sin((AdsPercent / 2f) * Pi);
		return AdsDisplay;
	}


	public float CalcSprintDisplay() {
		float SprintPercent = SprintTime / SprintChangeStateTime;
		float SprintDisplay = Sin((SprintPercent / 2f) * Pi);
		return SprintDisplay;
	}


	public void TickMomentum(float Delta) {
		float Length = Clamp(Momentum.Length() - Delta * MomentumFriction, 0, 1);
		Momentum = Momentum.Normalized() * Length;

		float AdsPercent = CalcAdsDisplay();
		float SprintPercent = 1 - CalcSprintDisplay();

		MeshJoint.RotationDegrees = new Vector3(
			Momentum.y * MaxRotation * AdsPercent * SprintPercent,
			Momentum.x * MaxRotation * AdsPercent * SprintPercent,
			0
		);
	}


	public override void _Process(float Delta) {
		TickFireTime(CurrentWeapon, Delta);

		float PlayerSpeed = Round(ParentPlayer.Momentum.Flattened().Length());
		if(((!CurrentWeapon.FullAuto && Input.IsActionJustPressed("Fire"))
				|| (CurrentWeapon.FullAuto && Input.IsActionPressed("Fire")))
			&& CurrentWeapon.FireTimer <= 0
			&& PlayerSpeed <= FirstPersonPlayer.BaseSpeed
			&& SprintTime <= 0
			&& Reloading == false) {
			CurrentWeapon.FireTimer = CurrentWeapon.MaxFireTime;

			if(CurrentWeapon.CurrentAmmo > 0) {
				CurrentWeapon.CurrentAmmo -= 1;
				PerformHitscan();
				RunFireEffects();
			}
			else {
				Sfx.PlaySfx(SfxCatagory.EMPTY_CHAMBER_FIRE_CLICK, 0, GlobalTransform.origin, 0);
			}
		}

		if(Input.IsActionJustPressed("Reload")
			&& CurrentWeapon.ReloadTimer <= 0
			&& CurrentWeapon.CurrentAmmo < CurrentWeapon.MaxAmmo) {
			CurrentWeapon.ReloadTimer = CurrentWeapon.MaxReloadTime;
			Reloading = true;
			Sfx.PlaySfx(SfxCatagory.RELOAD, 0, GlobalTransform.origin, 0);
		}
		else if(Reloading && CurrentWeapon.ReloadTimer > 0) {
			CurrentWeapon.ReloadTimer = Clamp(CurrentWeapon.ReloadTimer - Delta, 0, CurrentWeapon.MaxReloadTime);
			if(CurrentWeapon.ReloadTimer <= 0) {
				CurrentWeapon.CurrentAmmo = CurrentWeapon.MaxAmmo;
				Sfx.PlaySfx(SfxCatagory.RELOAD, 1, GlobalTransform.origin, 0);
			}
		}
		else {
			Reloading = false;
		}

		float OneTenth = CurrentWeapon.MaxReloadTime / 10;
		if(CurrentWeapon.ReloadTimer >= OneTenth * 9f) {
			ReloadHidePercent = 1 - (CurrentWeapon.ReloadTimer - OneTenth * 9f) / OneTenth;
		}
		else if(CurrentWeapon.ReloadTimer <= OneTenth) {
			ReloadHidePercent = CurrentWeapon.ReloadTimer / OneTenth;
		}
		float ReloadDisplay = Sin((ReloadHidePercent / 2f) * Pi);

		if(ParentPlayer.Mode == MovementMode.SPRINTING) {
			SprintTime = Clamp(SprintTime + Delta, 0, SprintChangeStateTime);
		}
		else {
			SprintTime = Clamp(SprintTime - Delta, 0, SprintChangeStateTime);
		}

		float SprintDisplay = CalcSprintDisplay();
		RotationDegrees = new Vector3(-140 * ReloadDisplay, 75f * SprintDisplay, 0);

		if(ParentPlayer.Mode != MovementMode.SPRINTING && Input.IsActionPressed("ADS") && !Reloading) {
			AdsTime = Clamp(AdsTime + Delta, 0, AdsChangeStateTime);
		}
		else {
			AdsTime = Clamp(AdsTime - Delta, 0, AdsChangeStateTime);
		}

		float AdsDisplay = CalcAdsDisplay();

		Translation = new Vector3(
			OgTranslation.x * AdsDisplay,
			OgTranslation.y * AdsDisplay,
			OgTranslation.z
		);

		TickMomentum(Delta);

		base._Process(Delta);
	}
}
