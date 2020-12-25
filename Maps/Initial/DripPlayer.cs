using Godot;



public class DripPlayer : Spatial {
	public const double MaxDripTime = 3d;
	public const double MinDripTime = 1.25d;

	public ClipChooser DripChooser = new ClipChooser(4);
	public float CurrentDripTime = 0f;


	public override void _Process(float Delta) {
		if(Multiplayer.IsNetworkServer()) {
			CurrentDripTime -= Delta;
			if(CurrentDripTime <= 0) {
				CurrentDripTime = (float)(Game.Rng.NextDouble() * (MaxDripTime - MinDripTime) + MinDripTime);
				Sfx.PlaySfxSpatially(SfxCatagory.DRIP, DripChooser.Choose(), GlobalTransform.origin, 0);
			}
		}

		base._Process(Delta);
	}
}
