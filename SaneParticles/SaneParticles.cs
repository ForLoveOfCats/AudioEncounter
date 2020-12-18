using System;

using Godot;
using static Godot.Mathf;



public class SaneParticles : Spatial {
	[Export]
	private Mesh MeshField;
	[Export]
	private Material MaterialField;

	[Export]
	private Script ScriptField;

	[Export(PropertyHint.Range, "0,10,0.001,or_greater,")]
	public float BaseLife = 5;
	[Export(PropertyHint.Range, "0,50,1,or_greater,")]
	public int MaxCount = 10;
	[Export]
	public bool OneShot = false;
	[Export]
	public bool OneShotAutoplay = false;
	[Export]
	public bool Looping = true;

	private Random Rng = new Random();

	private float SpawnCooldown = 0;




	private void SpawnOneParticle() {
		var Particle = new SaneParticle();
		ulong Id = Particle.GetInstanceId();
		Particle.SetScript(ScriptField);
		Particle = GD.InstanceFromId(Id) as SaneParticle;

		Particle.Rng = Rng;
		Particle.Life = BaseLife;
		Particle.Mesh = MeshField;
		Particle.MaterialOverride = MaterialField;

		AddChild(Particle);
		Particle.GlobalTransform = GlobalTransform;
	}


	public void FireOneShot() {
		for(int Index = 0; Index < MaxCount; Index += 1) {
			SpawnOneParticle();
		}

		SpawnCooldown = BaseLife + 0.01f;
	}


	public override void _Ready() {
		if(OneShot && OneShotAutoplay) {
			FireOneShot();
		}
	}


	public override void _Process(float Delta) {
		SpawnCooldown = Clamp(SpawnCooldown - Delta, 0, float.MaxValue);

		if(OneShot && OneShotAutoplay && !Looping) {
			if(SpawnCooldown <= 0) {
				QueueFree();
				return;
			}
		}
		else {
			if(!Looping) {
				return;
			}

			if(OneShot) {
				if(SpawnCooldown <= 0) {
					for(int Index = 0; Index < MaxCount; Index += 1) {
						SpawnOneParticle();
					}

					SpawnCooldown = BaseLife + 0.01f;
				}
			}
			else if(SpawnCooldown <= 0) {
				SpawnOneParticle();
				SpawnCooldown = BaseLife / (float)MaxCount;
			}
		}
	}
}
