using Godot;
using static Godot.Mathf;



public class WeaponStats {
	public int MaxAmmo = 0;
	public float MaxFireTime = 0f;
	public float MaxReloadTime = 0f;

	public float BaseDamage = 0f;

	public int CurrentAmmo = 0;
	public float FireTimer = 0f;
	public float ReloadTimer = 0f;
}


public class WeaponHolder : Spatial {
	public WeaponStats Pistol = new WeaponStats() {
		MaxAmmo = 8,
		CurrentAmmo = 8,
		MaxFireTime = 0.22f,
		MaxReloadTime = 5f,
		BaseDamage = 15f,
	};

	public WeaponStats CurrentWeapon = null;


	public override void _Ready() {
		CurrentWeapon = Pistol;
		base._Ready();
	}


	public void TickFireTime(WeaponStats Weapon, float Delta) {
		Weapon.FireTimer = Clamp(Weapon.FireTimer - Delta, 0, Weapon.MaxFireTime);
	}


	public override void _Process(float Delta) {
		TickFireTime(Pistol, Delta);

		if(Input.IsActionJustPressed("Fire") && CurrentWeapon.FireTimer <= 0) {
			CurrentWeapon.FireTimer = CurrentWeapon.MaxFireTime;

			if(CurrentWeapon.CurrentAmmo > 0) {
				CurrentWeapon.CurrentAmmo -= 1;

				if(CurrentWeapon.CurrentAmmo == 0) {
					CurrentWeapon.CurrentAmmo = -1;
					//Perform hitscan now
				}
			} else {
				Sfx.PlaySfx(SfxCatagory.EMPTY_CHAMBER_FIRE_CLICK, 0, GlobalTransform.origin);
			}
		}

		if(Input.IsActionJustPressed("Reload") && CurrentWeapon.FireTimer <= 0 && CurrentWeapon.ReloadTimer <= 0) {
			CurrentWeapon.ReloadTimer = CurrentWeapon.MaxReloadTime;
		} else if(CurrentWeapon.ReloadTimer > 0 && CurrentWeapon.CurrentAmmo == -1) {
			CurrentWeapon.ReloadTimer = Clamp(CurrentWeapon.ReloadTimer - Delta, 0, CurrentWeapon.MaxReloadTime);
			if(CurrentWeapon.ReloadTimer <= 0) {
				CurrentWeapon.CurrentAmmo = CurrentWeapon.MaxAmmo;
			}
		}

		base._Process(Delta);
	}
}
