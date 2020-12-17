using Godot;
using static Godot.Mathf;

using static SteelMath;



public class WeaponStats {
	public int MaxAmmo = 0;
	public float MaxFireTime = 0f;
	public float MaxReloadTime = 0f;

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

	public Vector3 OgTranslation = new Vector3();

	public Vector2 Momentum = new Vector2();
	public bool Reloading = false;
	public float ReloadHidePercent = 0f;
	public float SprintTime = 0f;
	public float AdsTime = 0f;

	FirstPersonPlayer ParentPlayer = null;

	public WeaponStats Pistol = new WeaponStats() {
		MaxAmmo = 8,
		CurrentAmmo = 8,
		MaxFireTime = 0.1f,
		MaxReloadTime = 4f,
		HeadDamage = 35,
		BodyDamage = 15,
	};

	public WeaponStats CurrentWeapon = null;


	public override void _Ready() {
		MeshJoint = GetNode<Spatial>("MeshJoint");

		CurrentWeapon = Pistol;

		OgTranslation = Translation;
		ParentPlayer = (FirstPersonPlayer)GetParent().GetParent().GetParent();

		base._Ready();
	}


	public void TickFireTime(WeaponStats Weapon, float Delta) {
		Weapon.FireTimer = Clamp(Weapon.FireTimer - Delta, 0, Weapon.MaxFireTime);
	}


	public void PerformHitscan() {
		float VerticalDeviation = 0;
		float HorizontalDeviation = 0;

		Vector3 Origin = ParentPlayer.Cam.GlobalTransform.origin;
		Vector3 Endpoint = Origin + new Vector3(0, 0, -Range)
			.Rotated(new Vector3(1, 0, 0), Deg2Rad(ParentPlayer.Cam.RotationDegrees.x + VerticalDeviation))
			.Rotated(new Vector3(0, 1, 0), Deg2Rad(ParentPlayer.RotationDegrees.y + HorizontalDeviation));
		GD.Print(Origin, " ", Endpoint);

		var Exclude = new Godot.Collections.Array() { ParentPlayer };
		PhysicsDirectSpaceState State = GetWorld().DirectSpaceState;
		Godot.Collections.Dictionary Results = State.IntersectRay(Origin, Endpoint, Exclude, 1 | 2);

		if(Results.Count > 0 && Results["collider"] is Hitbox Box) {
			int Damage = CurrentWeapon.BodyDamage;
			if(Box.Kind == HitboxKind.HEAD) {
				Damage = CurrentWeapon.HeadDamage;
			}

			Box.Damage(Damage);
		}
	}


	public void RunFireEffects() {
		if(CurrentWeapon == Pistol) {
			Sfx.PlaySfx(SfxCatagory.PISTOL_FIRE, 0, GlobalTransform.origin);
			ParentPlayer.CamAnimations.Add(new PistolRecoil());
		}
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
		TickFireTime(Pistol, Delta);

		float PlayerSpeed = Round(ParentPlayer.Momentum.Flattened().Length());
		if(Input.IsActionJustPressed("Fire")
			&& CurrentWeapon.FireTimer <= 0
			&& PlayerSpeed <= FirstPersonPlayer.BaseSpeed
			&& SprintTime <= 0
			&& Reloading == false) {
			CurrentWeapon.FireTimer = CurrentWeapon.MaxFireTime;

			if(CurrentWeapon.CurrentAmmo > 0) {
				CurrentWeapon.CurrentAmmo -= 1;
				PerformHitscan();
				RunFireEffects();
			} else {
				Sfx.PlaySfx(SfxCatagory.EMPTY_CHAMBER_FIRE_CLICK, 0, GlobalTransform.origin);
			}
		}

		if(Input.IsActionJustPressed("Reload")
			&& CurrentWeapon.FireTimer <= 0
			&& CurrentWeapon.ReloadTimer <= 0
			&& CurrentWeapon.CurrentAmmo < CurrentWeapon.MaxAmmo) {
			CurrentWeapon.ReloadTimer = CurrentWeapon.MaxReloadTime;
			Reloading = true;
			Sfx.PlaySfx(SfxCatagory.RELOAD, 0, GlobalTransform.origin);
		} else if(Reloading && CurrentWeapon.ReloadTimer > 0) {
			CurrentWeapon.ReloadTimer = Clamp(CurrentWeapon.ReloadTimer - Delta, 0, CurrentWeapon.MaxReloadTime);
			if(CurrentWeapon.ReloadTimer <= 0) {
				CurrentWeapon.CurrentAmmo = CurrentWeapon.MaxAmmo;
				Sfx.PlaySfx(SfxCatagory.RELOAD, 1, GlobalTransform.origin);
			}
		} else {
			Reloading = false;
		}

		float OneTenth = CurrentWeapon.MaxReloadTime / 10;
		if(CurrentWeapon.ReloadTimer >= OneTenth * 9f) {
			ReloadHidePercent = 1 - (CurrentWeapon.ReloadTimer - OneTenth * 9f) / OneTenth;
		} else if(CurrentWeapon.ReloadTimer <= OneTenth) {
			ReloadHidePercent = CurrentWeapon.ReloadTimer / OneTenth;
		}
		float ReloadDisplay = Sin((ReloadHidePercent / 2f) * Pi);

		if(ParentPlayer.Mode == MovementMode.SPRINTING) {
			SprintTime = Clamp(SprintTime + Delta, 0, SprintChangeStateTime);
		} else {
			SprintTime = Clamp(SprintTime - Delta, 0, SprintChangeStateTime);
		}

		float SprintDisplay = CalcSprintDisplay();
		RotationDegrees = new Vector3(-140 * ReloadDisplay, 75f * SprintDisplay, 0);

		if(ParentPlayer.Mode != MovementMode.SPRINTING && Input.IsActionPressed("ADS")) {
			AdsTime = Clamp(AdsTime + Delta, 0, AdsChangeStateTime);
		} else {
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
