using System;
using System.Linq;
using System.Collections.Generic;

using Godot;
using static Godot.Mathf;

using static Assert;



public partial class Game : Node {
	public const int ServerId = 1;
	public const int Port = 27015;
	public const int MaxPlayers = 32;


	public static Random Rng = new Random();
	public static bool DeathmatchMode = true;

	public static PackedScene WorldScene = GD.Load<PackedScene>("res://Maps/Initial/Initial.tscn");
	public static PackedScene FirstPersonPlayerScene = GD.Load<PackedScene>("res://Player/FirstPersonPlayer.tscn");
	public static PackedScene ThirdPersonPlayerScene = GD.Load<PackedScene>("res://Player/ThirdPersonPlayer.tscn");
	public static PackedScene AdminControlsScene = GD.Load<PackedScene>("res://Game/AdminControls.tscn");
	public static PackedScene DeathScreenScene = GD.Load<PackedScene>("res://Game/DeathScreen.tscn");

	public static bool IsFullscreen = false;

	public static Game Self = null;
	public Node RuntimeRoot = null;
	public static AdminControls AdminPanel = null;
	public static Node DeathScreen = null;

	public static ScrollContainer KillFeedScroll = null;
	public static VBoxContainer KillFeedVBox = null;

	public ThirdPersonPlayer Spectating = null;
	public static int SpectatingIndex = 0;

	public static string Nickname = "BrianD";
	public static Dictionary<long, string> Nicknames = new Dictionary<long, string>();
	public static List<long> Alive = new List<long>();


	public override void _Ready() {
		if(Engine.IsEditorHint()) {
			return;
		}

		Self = this;
		RuntimeRoot = GetTree().Root.GetNode("RuntimeRoot");

		KillFeedScroll = RuntimeRoot.GetNode<ScrollContainer>("HBoxContainer/VBoxContainer/ScrollContainer");
		KillFeedVBox = KillFeedScroll.GetNode<VBoxContainer>("VBoxContainer");

		Multiplayer.PeerConnected += PlayerConnected;
		Multiplayer.PeerDisconnected += PlayerDisconnected;
		Multiplayer.ServerDisconnected += ServerDisconnected;
		// GetTree().Connect("network_peer_connected", new Callable(this, nameof(PlayerConnected)));
		// GetTree().Connect("network_peer_disconnected", new Callable(this, nameof(PlayerDisconnected)));
		// GetTree().Connect("server_disconnected", new Callable(this, nameof(ServerDisconnected)), flags: (uint)ConnectFlags.Deferred);

		base._Ready();
	}


	public void Host() {
		GD.Print("Hello from C# host");
		var Peer = new ENetMultiplayerPeer();
		Peer.CreateServer(Port, MaxPlayers);
		Multiplayer.MultiplayerPeer = Peer;

		RuntimeRoot.AddChild(WorldScene.Instantiate());

		AdminPanel = AdminControlsScene.Instantiate<AdminControls>();
		RuntimeRoot.AddChild(AdminPanel);
	}


	public void Connect(string IpString, string NickString) {
		if(IpString.Length == 0) {
			IpString = "127.0.0.1";
		}

		char[] NickArray = NickString.ToCharArray();
		NickArray = Array.FindAll(NickArray, car => char.IsLetter(car) || car == '_');
		NickString = new string(NickArray);
		if(NickString.Length > 0) {
			Nickname = NickString;
		}

		var Peer = new ENetMultiplayerPeer();
		Peer.CreateClient(IpString, Port);
		Multiplayer.MultiplayerPeer = Peer;

		RuntimeRoot.AddChild(WorldScene.Instantiate());

		DeathScreen = DeathScreenScene.Instantiate();
		RuntimeRoot.AddChild(DeathScreen);
	}


	public void PlayerConnected(long Id) {
		Log($"New player {Id} connected");

		if(Id == ServerId) {
			RpcId(ServerId, nameof(RegisterNickname), Nickname);
		}

		if(Multiplayer.IsServer()) {
			foreach(long AlivePlayer in Alive) {
				if(AlivePlayer == Id) {
					continue;
				}

				var Plr = (ThirdPersonPlayer)RuntimeRoot.GetNode(AlivePlayer.ToString());
				string Nickname = Nicknames[AlivePlayer];
				RpcId(Id, nameof(NetSpawn), AlivePlayer, Nickname, Plr.GlobalTransform.Origin, Plr.Rotation);
				Plr.RpcId(Id, nameof(ThirdPersonPlayer.NetSetSpectateWeapon), (int)Plr.Holder.CurrentWeapon.Kind);
			}
		}
	}


	public void PlayerDisconnected(long Id) {
		if(Multiplayer.IsServer() && Nicknames.GetOrNull(Id) is String Identifier) {
			Log($"Registered player {Identifier} disconnected");
		}
		else {
			Log($"Unregistered player {Id} disconnected");
		}

		Alive.Remove(Id);
		Nicknames.Remove(Id);

		Node ThirdPerson = RuntimeRoot.GetNodeOrNull(Id.ToString());
		if(ThirdPerson != null) {
			ThirdPerson.QueueFree();
		}
	}


	public void ServerDisconnected() {
		Log("Server disconnected");
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RegisterNickname(string ClientNick) {
		char[] NickArray = ClientNick.ToCharArray();
		NickArray = Array.FindAll(NickArray, car => char.IsLetter(car) || car == '_');
		ClientNick = new string(NickArray);

		if(ClientNick.Length == 0) {
			((ENetMultiplayerPeer)Multiplayer.MultiplayerPeer).DisconnectPeer(Multiplayer.GetRemoteSenderId());
		}

		string Identifier = $"{ClientNick}#{Multiplayer.GetRemoteSenderId()}";
		Nicknames.Add(Multiplayer.GetRemoteSenderId(), Identifier);
		Log("Registered player ", Identifier);
	}


	public void RequestRespawn() {
		RpcId(ServerId, nameof(NetRequestRespawn));
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetRequestRespawn() {
		if(!Self.Multiplayer.IsServer()) {
			return;
		}

		if(DeathmatchMode) {
			ChooseSpawnPointAndDoSpawn(Multiplayer.GetRemoteSenderId());
		}
	}


	public static void ChooseSpawnPointAndDoSpawn(long Id) {
		if(!Self.Multiplayer.IsServer()) {
			return;
		}

		var SpawnPoints = new List<Node3D>();
		foreach(Node GottenNode in Self.GetTree().GetNodesInGroup("spawn_point")) {
			SpawnPoints.Add((Node3D)GottenNode);
		}

		Node3D SpawnPoint = null;
		if(Alive.Count <= 0) {
			int Index = Rng.Next(SpawnPoints.Count);
			SpawnPoint = SpawnPoints[Index];
		}
		else {
			var Distances = new List<float>();

			foreach(Node3D Point in SpawnPoints) {
				float Distance = 0;
				foreach(long ExistingId in Alive) {
					var Plr = (ThirdPersonPlayer)Self.RuntimeRoot.GetNode(ExistingId.ToString());
					Distance += Point.GlobalTransform.Origin.DistanceTo(Plr.GlobalTransform.Origin);
				}
				Distances.Add(Distance / (float)Alive.Count);
			}

			int FarthestIndex = 0;
			float FarthestDistance = Distances[0];
			foreach((float Distance, int Index) in Distances.Select((item, index) => (item, index))) {
				if(Distance > FarthestDistance) {
					FarthestIndex = Index;
				}
			}
			SpawnPoint = SpawnPoints[FarthestIndex];
		}
		ActualAssert(SpawnPoint != null);

		string Nickname = Nicknames[Id];
		Game.Self.Rpc(nameof(Game.NetSpawn), Id, Nickname, SpawnPoint.GlobalTransform.Origin, SpawnPoint.Rotation);
		Game.Self.NetSpawn(Id, Nickname, SpawnPoint.GlobalTransform.Origin, SpawnPoint.Rotation);
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetSpawn(long Id, string Nickname, Vector3 Position, Vector3 Rotation) {
		if(Id == Multiplayer.GetUniqueId()) {
			Self.Spectating = null;

			Node3D Player = FirstPersonPlayerScene.Instantiate<Node3D>();
			Player.Name = Multiplayer.GetUniqueId().ToString();
			RuntimeRoot.AddChild(Player);

			Player.Position = Position;
			Player.Rotation = Rotation;

			if(DeathScreen != null) {
				DeathScreen.QueueFree();
			}
		}
		else {
			ThirdPersonPlayer Player = ThirdPersonPlayerScene.Instantiate<ThirdPersonPlayer>();
			Player.Id = Id;
			Player.Name = Id.ToString();
			Player.Nickname = Nickname;
			RuntimeRoot.AddChild(Player);

			Player.Position = Position;
			Player.Rotation = Rotation;

			Game.Alive.Add(Id);
		}
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetNotifyKillfeed(string Message) {
		var Entry = new Label();
		Entry.Text = Message;
		Entry.AutowrapMode = TextServer.AutowrapMode.Word;

		KillFeedVBox.AddChild(Entry);
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void NetClearKillfeed() {
		var Children = KillFeedVBox.GetChildren();
		foreach(Node Child in Children) {
			KillFeedVBox.RemoveChild(Child);
		}
	}


	public static void SpectateNextPlayer() {
		SpectatingIndex += 1;
		if(SpectatingIndex >= Game.Alive.Count) {
			SpectatingIndex = 0;
		}

		if(Game.Alive.Count > 0) {
			Game.Self.RuntimeRoot.GetNode<ThirdPersonPlayer>(Game.Alive[SpectatingIndex].ToString()).Spectate();
		}
	}


	public static void Print(params string[] Messages) {
		if(AdminPanel is AdminControls Panel) {
			string Message = String.Join(" ", Messages);
			Panel.CliOutput.Text += Message + "\n";
			Panel.CliOutput.SetCaretLine(Panel.CliOutput.GetLineCount());
		}
	}


	public static void Log(params string[] Messages) {
		string Message = String.Join(" ", Messages);

		if(AdminPanel is AdminControls Panel) {
			Panel.LogOutput.Text += Message + "\n\n";
			Panel.LogOutput.SetCaretLine(Panel.LogOutput.GetLineCount());
		}

		GD.Print(Message);
	}

	public override void _Process(double Delta) {
		if(Input.IsActionJustPressed("ToggleFullscreen")) {
			IsFullscreen = !IsFullscreen;
			GetWindow().Mode = IsFullscreen ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
		}

		KillFeedScroll.ScrollVertical = (int)Ceil((float)KillFeedScroll.GetVScrollBar().MaxValue);

		base._Process(Delta);
	}
}

public static class DictionaryExtension {
	public static V GetOrNull<K, V>(this Dictionary<K, V> Self, K Key)
	where V : class {
		V Value = null;
		Self.TryGetValue(Key, out Value);
		return Value;
	}
}
