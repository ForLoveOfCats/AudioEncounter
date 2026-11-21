using System;

using Godot;



public partial class SaneParticle : MeshInstance3D {
	public Random Rng;
	public float Life;


	public virtual void InitParticle() { }


	public virtual void TickParticle(float Delta) { }


	public sealed override void _Ready() {
		InitParticle();
	}


	public sealed override void _Process(double Delta) {
		Life -= (float)Delta;
		if(Life <= 0) {
			QueueFree();
		}

		TickParticle((float)Delta);
	}
}
