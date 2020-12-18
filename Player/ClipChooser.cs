public class ClipChooser {
	int LastIndex = -1;
	int Count = -1;


	public ClipChooser(int CountArg) {
		Count = CountArg;
	}


	public int Choose() {
		int Index = LastIndex;

		while(Index == LastIndex) {
			Index = Game.Rng.Next(Count);
		}
		LastIndex = Index;


		return Index;
	}
}
