using System.Collections.Generic;

using Godot;

using static Assert;



public enum SfxCatagory {
	FALL_CRUNCH,
	EMPTY_CHAMBER_FIRE_CLICK,
	RELOAD,
	BULLET_HIT,
	FLESH_HIT,
	PISTOL_FIRE,
	AK_FIRE,
	CASING_TINK,
	CONCRETE,
	LEAVES
};



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
	public const bool PlayLocally = true;
	public const bool PlayRemote = true;

	public static Sfx Self;

	public static Dictionary<SfxCatagory, List<AudioStream>> Clips = new Dictionary<SfxCatagory, List<AudioStream>>();


	static Sfx() {
		Clips.Add(SfxCatagory.FALL_CRUNCH, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/FallCrunch.wav")
		});
		Clips.Add(SfxCatagory.EMPTY_CHAMBER_FIRE_CLICK, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/EmptyChamberFireClick.wav")
		});
		Clips.Add(SfxCatagory.RELOAD, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/ReloadStart.wav"),
			GD.Load<AudioStream>("res://TrimmedAudio/ReloadEnd.wav")
		});
		Clips.Add(SfxCatagory.BULLET_HIT, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/BulletHit.wav")
		});
		Clips.Add(SfxCatagory.FLESH_HIT, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/FleshHit.wav")
		});

		Clips.Add(SfxCatagory.PISTOL_FIRE, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/PistolFire.wav")
		});
		Clips.Add(SfxCatagory.AK_FIRE, new List<AudioStream> {
			GD.Load<AudioStream>("res://TrimmedAudio/AkFire.wav")
		});

		List<AudioStream> CasingTink = LoadAllStreamsInFolder("res://TrimmedAudio/CasingTinks");
		Clips.Add(SfxCatagory.CASING_TINK, CasingTink);

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
		if(Engine.EditorHint) {
			return;
		}

		Self = this;
		base._Ready();
	}


	public static void PlaySfx(SfxCatagory Catagory, int Index, Vector3 Pos) {
		if(PlayLocally) {
			PlaySfxLocally(Catagory, Index);
		}

		if(PlayRemote) {
			Self.Rpc(nameof(PlaySfxAt), Catagory, Index, Pos);
		}
	}


	public static void PlaySfxSpatially(SfxCatagory Catagory, int Index, Vector3 Pos) {
		if(PlayLocally) {
			Self.PlaySfxAt(Catagory, Index, Pos);
		}

		if(PlayRemote) {
			Self.Rpc(nameof(PlaySfxAt), Catagory, Index, Pos);
		}
	}


	private static void PlaySfxLocally(SfxCatagory Catagory, int Index) {
		Sfx2DCleanup StreamPlayer = new Sfx2DCleanup();
		StreamPlayer.Stream = Clips[Catagory][Index];
		StreamPlayer.Bus = "Reverb";

		switch(Catagory) {
			case SfxCatagory.EMPTY_CHAMBER_FIRE_CLICK: {
				StreamPlayer.VolumeDb = -8;
				break;
			}

			case SfxCatagory.CONCRETE: {
				StreamPlayer.VolumeDb = 4;
				break;
			}

			case SfxCatagory.LEAVES: {
				StreamPlayer.VolumeDb = -5;
				break;
			}

			case SfxCatagory.PISTOL_FIRE: {
				StreamPlayer.VolumeDb = -6;
				break;
			}

			case SfxCatagory.AK_FIRE: {
				StreamPlayer.VolumeDb = -6;
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
		StreamPlayer.Bus = "Reverb";

		switch(Catagory) {
			case SfxCatagory.FALL_CRUNCH: {
				StreamPlayer.UnitDb = 1;
				StreamPlayer.UnitSize = 18;
				break;
			}

			case SfxCatagory.EMPTY_CHAMBER_FIRE_CLICK: {
				StreamPlayer.UnitDb = -10;
				StreamPlayer.UnitSize = 28;
				StreamPlayer.MaxDb = -10;
				break;
			}

			case SfxCatagory.RELOAD: {
				StreamPlayer.UnitDb = -2;
				StreamPlayer.UnitSize = 40;
				StreamPlayer.MaxDb = -2;
				break;
			}

			case SfxCatagory.BULLET_HIT: {
				StreamPlayer.UnitDb = -1;
				StreamPlayer.UnitSize = 50;
				StreamPlayer.MaxDb = -1;
				break;
			}

			case SfxCatagory.FLESH_HIT: {
				StreamPlayer.UnitDb = 6;
				StreamPlayer.UnitSize = 30;
				StreamPlayer.MaxDb = 6;
				StreamPlayer.Bus = "Master";
				break;
			}

			case SfxCatagory.PISTOL_FIRE: {
				StreamPlayer.UnitDb = 1;
				StreamPlayer.UnitSize = 60;
				StreamPlayer.MaxDb = 1;
				break;
			}

			case SfxCatagory.AK_FIRE: {
				StreamPlayer.UnitDb = 1;
				StreamPlayer.UnitSize = 60;
				StreamPlayer.MaxDb = 1;
				break;
			}

			case SfxCatagory.CASING_TINK: {
				StreamPlayer.UnitDb = -4;
				StreamPlayer.UnitSize = 20;
				StreamPlayer.MaxDb = -4;
				break;
			}

			case SfxCatagory.CONCRETE: {
				StreamPlayer.UnitDb = 10;
				StreamPlayer.MaxDb = 10;
				StreamPlayer.UnitSize = 25;
				break;
			}

			case SfxCatagory.LEAVES: {
				StreamPlayer.UnitDb = 4;
				StreamPlayer.MaxDb = 4;
				StreamPlayer.UnitSize = 25;
				break;
			}
		}

		Game.RuntimeRoot.AddChild(StreamPlayer);
		StreamPlayer.Translation = Pos;
		StreamPlayer.Play();
	}
}
