using System.Collections.Generic;

using Godot;

using static Assert;



public enum FootstepKind { CONCRETE, LEAVES };



public class Footstep3DCleanup : AudioStreamPlayer3D {
	public override void _Ready() {
		Connect("finished", this, nameof(OnPlaybackFinished));
		base._Ready();
	}


	public void OnPlaybackFinished() {
		QueueFree();
	}
}



public class Footstep3D : Node {
	public static Footstep3D Self;

	public static Dictionary<FootstepKind, List<AudioStream>> Clips = new Dictionary<FootstepKind, List<AudioStream>>();


	static Footstep3D() {
		List<AudioStream> Concrete = LoadAllStreamsInFolder("res://TrimmedAudio/ConcreteFootsteps");
		Clips.Add(FootstepKind.CONCRETE, Concrete);

		List<AudioStream> Leaves = LoadAllStreamsInFolder("res://TrimmedAudio/LeavesFootsteps");
		Clips.Add(FootstepKind.LEAVES, Leaves);
	}


	public static List<AudioStream> LoadAllStreamsInFolder(string Path) {
		var Output = new List<AudioStream>();

		var Dir = new Directory();
		ActualAssert(Dir.Open(Path) == Error.Ok);

		Dir.ListDirBegin();

		string FileName = Dir.GetNext();
		while(FileName.Length > 0) {
			if(!Dir.CurrentIsDir() && !FileName.EndsWith(".import")) {
				var Stream = GD.Load<AudioStream>($"{Path}/{FileName}");
				ActualAssert(Stream != null);
				Output.Add(Stream);
			}

			FileName = Dir.GetNext();
		}

		Dir.ListDirEnd();

		return Output;
	}


	public override void _Ready() {
		Self = this;
		base._Ready();
	}


	public static void SendFootstep(FootstepKind Kind, int Index, Vector3 Pos) {
		Self.Rpc(nameof(PlayFootstepAt), Kind, Index, Pos);
	}


	[Remote]
	public void PlayFootstepAt(FootstepKind Kind, int Index, Vector3 Pos) {
		Footstep3DCleanup StreamPlayer = new Footstep3DCleanup();
		StreamPlayer.Stream = Clips[Kind][Index];

		switch(Kind) {
			case FootstepKind.CONCRETE: {
				StreamPlayer.UnitDb = 10;
				StreamPlayer.UnitSize = 10;
				break;
			}

			case FootstepKind.LEAVES: {
				StreamPlayer.UnitDb = 5;
				StreamPlayer.UnitSize = 5;
				break;
			}
		}

		Game.RuntimeRoot.AddChild(StreamPlayer);
		StreamPlayer.Translation = Pos;
		StreamPlayer.Play();
	}
}
