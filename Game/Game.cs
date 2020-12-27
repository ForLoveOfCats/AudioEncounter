using System;
using System.Linq;
using System.Collections.Generic;

using Godot;

using static Assert;



public class Game : Node {
	public const int ServerId = 1;
	public const int Port = 27015;
	public const int MaxPlayers = 32;


	public static Random Rng = new Random();

	public static PackedScene WorldScene = GD.Load<PackedScene>("res://Maps/Initial/Initial.tscn");
	public static PackedScene FirstPersonPlayerScene = GD.Load<PackedScene>("res://Player/FirstPersonPlayer.tscn");
	public static PackedScene ThirdPersonPlayerScene = GD.Load<PackedScene>("res://Player/ThirdPersonPlayer.tscn");
	public static PackedScene AdminControlsScene = GD.Load<PackedScene>("res://Game/AdminControls.tscn");

	public static Game Self = null;
	public static Node RuntimeRoot = null;
	public static AdminControls AdminPanel = null;

	public static string Nickname = "BrianD";
	public static Dictionary<int, string> Nicknames = new Dictionary<int, string>();
	public static HashSet<int> Alive = new HashSet<int>();


	public override void _Ready() {
		if(Engine.EditorHint) {
			return;
		}

		Self = this;
		RuntimeRoot = GetTree().Root.GetNode("RuntimeRoot");

		GetTree().Connect("network_peer_connected", this, nameof(PlayerConnected));
		GetTree().Connect("network_peer_disconnected", this, nameof(PlayerDisconnected));
		GetTree().Connect("server_disconnected", this, nameof(ServerDisconnected), flags: (uint)ConnectFlags.Deferred);

		base._Ready();
	}


	public void Host() {
		var Peer = new NetworkedMultiplayerENet();
		Peer.CreateServer(Port, MaxPlayers);
		GetTree().NetworkPeer = Peer;

		RuntimeRoot.AddChild(WorldScene.Instance());

		AdminPanel = (AdminControls)AdminControlsScene.Instance();
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

		var Peer = new NetworkedMultiplayerENet();
		Peer.CreateClient(IpString, Port);
		GetTree().NetworkPeer = Peer;

		RuntimeRoot.AddChild(WorldScene.Instance());
	}


	public void PlayerConnected(int Id) {
		Log($"New player {Id} connected");

		if(Id == ServerId) {
			RpcId(ServerId, nameof(RegisterNickname), Nickname);
		}
	}


	public void PlayerDisconnected(int Id) {
		if(Multiplayer.IsNetworkServer() && Nicknames.GetOrNull(Id) is String Identifier) {
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


	[Remote]
	public void RegisterNickname(string ClientNick) {
		char[] NickArray = ClientNick.ToCharArray();
		NickArray = Array.FindAll(NickArray, car => char.IsLetter(car) || car == '_');
		ClientNick = new string(NickArray);

		if(ClientNick.Length == 0) {
			((NetworkedMultiplayerENet)Multiplayer.NetworkPeer).DisconnectPeer(Multiplayer.GetRpcSenderId());
		}

		string Identifier = $"{ClientNick}#{Multiplayer.GetRpcSenderId()}";
		Nicknames.Add(Multiplayer.GetRpcSenderId(), Identifier);
		Log("Registered player ", Identifier);
	}


	public static void ChooseSpawnPointAndDoSpawn(int Id) {
		ActualAssert(Self.Multiplayer.IsNetworkServer());

		var SpawnPoints = new List<Spatial>();
		foreach(Node GottenNode in Self.GetTree().GetNodesInGroup("spawn_point")) {
			SpawnPoints.Add((Spatial)GottenNode);
		}

		Spatial SpawnPoint = null;
		if(Alive.Count <= 0) {
			int Index = Rng.Next(SpawnPoints.Count);
			SpawnPoint = SpawnPoints[Index];
		}
		else {
			var Distances = new List<float>();

			foreach(Spatial Point in SpawnPoints) {
				float Distance = 0;
				foreach(int ExistingId in Alive) {
					var Plr = (ThirdPersonPlayer)RuntimeRoot.GetNode(ExistingId.ToString());
					Distance += Point.GlobalTransform.origin.DistanceTo(Plr.GlobalTransform.origin);
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

		Game.Alive.Add(Id);
		Game.Self.Rpc(nameof(Game.NetSpawn), Id, SpawnPoint.GlobalTransform.origin, SpawnPoint.Rotation);
		Game.Self.NetSpawn(Id, SpawnPoint.GlobalTransform.origin, SpawnPoint.Rotation);
	}


	[Remote]
	public void NetSpawn(int Id, Vector3 Position, Vector3 Rotation) {
		if(Id == Multiplayer.GetNetworkUniqueId()) {
			Spatial Player = (Spatial)FirstPersonPlayerScene.Instance();
			Player.Name = Multiplayer.GetNetworkUniqueId().ToString();
			RuntimeRoot.AddChild(Player);

			Player.Translation = Position;
			Player.Rotation = Rotation;
		}
		else {
			ThirdPersonPlayer Player = (ThirdPersonPlayer)ThirdPersonPlayerScene.Instance();
			Player.Id = Id;
			Player.Name = Id.ToString();
			RuntimeRoot.AddChild(Player);

			Player.Translation = Position;
			Player.Rotation = Rotation;
		}
	}


	public static void Print(params string[] Messages) {
		if(AdminPanel is AdminControls Panel) {
			string Message = String.Join(" ", Messages);
			Panel.CliOutput.Text += Message + "\n";
			Panel.CliOutput.CursorSetLine(Panel.CliOutput.GetLineCount());
		}
	}


	public static void Log(params string[] Messages) {
		string Message = String.Join(" ", Messages);

		if(AdminPanel is AdminControls Panel) {
			Panel.LogOutput.Text += Message + "\n\n";
			Panel.LogOutput.CursorSetLine(Panel.LogOutput.GetLineCount());
		}

		GD.Print(Message);
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
