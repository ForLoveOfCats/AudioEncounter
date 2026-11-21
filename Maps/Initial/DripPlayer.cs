using Godot;



public partial class DripPlayer : Node3D {
	public const double MaxDripTime = 3d;
	public const double MinDripTime = 1.25d;

	public ClipChooser DripChooser = new ClipChooser(4);
	public float CurrentDripTime = 0f;


	public override void _Process(double Delta) {
		if(Multiplayer.IsServer()) {
			CurrentDripTime -= (float)Delta;
			if(CurrentDripTime <= 0) {
				CurrentDripTime = (float)(Game.Rng.NextDouble() * (MaxDripTime - MinDripTime) + MinDripTime);
				Sfx.PlaySfxSpatially(SfxCatagory.DRIP, DripChooser.Choose(), GlobalTransform.Origin, 0);
			}
		}

		base._Process(Delta);
	}
}
