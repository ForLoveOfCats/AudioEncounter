using Godot;



public class Footstep2D : Node {
	int LastIndex = -1;

	public int Play() {
		Godot.Collections.Array Children = GetChildren();

		int Index = LastIndex;
		while(Index == LastIndex) {
			Index = Game.Rng.Next(Children.Count);
		}
		LastIndex = Index;

#if false
		var Child = (AudioStreamPlayer)Children[Index];
		Child.Play();
#endif

		return Index;
	}
}
