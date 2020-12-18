using System;

using Godot;



public class SaneParticle : MeshInstance {
	public Random Rng;
	public float Life;


	public virtual void InitParticle() { }


	public virtual void TickParticle(float Delta) { }


	public sealed override void _Ready() {
		InitParticle();
	}


	public sealed override void _Process(float Delta) {
		Life -= Delta;
		if(Life <= 0) {
			QueueFree();
		}

		TickParticle(Delta);
	}
}
