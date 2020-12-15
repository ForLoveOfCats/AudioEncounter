using Godot;



public class ThirdPersonPlayer : StaticBody {
	public bool ValidTargetTransform = false;
	public Transform TargetTransform = new Transform();


	[Remote]
	public void NetUpdateTransform(Transform NewTransform) {
		TargetTransform = NewTransform;
		ValidTargetTransform = true;
	}


	public override void _Process(float Delta) {
		if(ValidTargetTransform) {
			Transform = Transform.InterpolateWith(TargetTransform, Delta / 0.1f);
		}

		base._Process(Delta);
	}
}
