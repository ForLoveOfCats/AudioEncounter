using System.Collections.Generic;

using Godot;

using static Assert;



public enum SfxCatagory { FALL_CRUNCH, CONCRETE, LEAVES };



public class Sfx2DCleanup : AudioStreamPlayer {
	public override void _Ready() {
		Connect("finished", this, nameof(OnPlaybackFinished));
		base._Ready();
	}


	public void OnPlaybackFinished() {
		QueueFree();
	}
}



public class Sfx3DCleanup : AudioStreamPlayer3D {
	public override void _Ready() {
		Connect("finished", this, nameof(OnPlaybackFinished));
		base._Ready();
	}


	public void OnPlaybackFinished() {
		QueueFree();
	}
}



public class Sfx : Node {
	public static Sfx Self;

	public static Dictionary<SfxCatagory, List<AudioStream>> Clips = new Dictionary<SfxCatagory, List<AudioStream>>();


	static Sfx() {
		Clips.Add(SfxCatagory.FALL_CRUNCH, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/FallCrunch.wav")
		});

		List<AudioStream> Concrete = LoadAllStreamsInFolder("res://TrimmedAudio/ConcreteFootsteps");
		Clips.Add(SfxCatagory.CONCRETE, Concrete);

		List<AudioStream> Leaves = LoadAllStreamsInFolder("res://TrimmedAudio/LeavesFootsteps");
		Clips.Add(SfxCatagory.LEAVES, Leaves);
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


	public static void PlaySfx(SfxCatagory Catagory, int Index, Vector3 Pos) {
		PlaySfxLocally(Catagory, Index);
		Self.Rpc(nameof(PlaySfxAt), Catagory, Index, Pos);
	}


	private static void PlaySfxLocally(SfxCatagory Catagory, int Index) {
		Sfx2DCleanup StreamPlayer = new Sfx2DCleanup();
		StreamPlayer.Stream = Clips[Catagory][Index];

		switch(Catagory) {
			case SfxCatagory.CONCRETE: {
				StreamPlayer.VolumeDb = 4;
				break;
			}

			case SfxCatagory.LEAVES: {
				StreamPlayer.VolumeDb = -5;
				break;
			}
		}

		Game.RuntimeRoot.AddChild(StreamPlayer);
		StreamPlayer.Play();
	}


	[Remote]
	private void PlaySfxAt(SfxCatagory Catagory, int Index, Vector3 Pos) {
		Sfx3DCleanup StreamPlayer = new Sfx3DCleanup();
		StreamPlayer.Stream = Clips[Catagory][Index];

		switch(Catagory) {
			case SfxCatagory.FALL_CRUNCH: {
				StreamPlayer.UnitDb = 1;
				StreamPlayer.UnitSize = 18;
				break;
			}

			case SfxCatagory.CONCRETE: {
				StreamPlayer.UnitDb = 10;
				StreamPlayer.UnitSize = 10;
				break;
			}

			case SfxCatagory.LEAVES: {
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
