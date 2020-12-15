using System;

using Godot;



public class Game : Node {
	public const int ServerId = 1;
	public const int Port = 27015;
	public const int MaxPlayers = 32;


	public static Random Rng = new Random();

	public static PackedScene WorldScene = GD.Load<PackedScene>("res://Maps/Initial/Initial.tscn");
	public static PackedScene FirstPersonPlayerScene = GD.Load<PackedScene>("res://Player/FirstPersonPlayer.tscn");
	public static PackedScene ThirdPersonPlayerScene = GD.Load<PackedScene>("res://Player/ThirdPersonPlayer.tscn");

	public static Game Self = null;
	public static Node RuntimeRoot = null;

	public override void _Ready() {
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

		Node Player = FirstPersonPlayerScene.Instance();
		Player.Name = Multiplayer.GetNetworkUniqueId().ToString();
		RuntimeRoot.AddChild(Player);
	}


	public void Connect(string IpString) {
		if(IpString.Length == 0) {
			IpString = "127.0.0.1";
		}

		var Peer = new NetworkedMultiplayerENet();
		Peer.CreateClient(IpString, Port);
		GetTree().NetworkPeer = Peer;

		RuntimeRoot.AddChild(WorldScene.Instance());

		Node Player = FirstPersonPlayerScene.Instance();
		Player.Name = Multiplayer.GetNetworkUniqueId().ToString();
		RuntimeRoot.AddChild(Player);
	}


	public void PlayerConnected(int Id) {
		GD.Print($"Player {Id} connected");

		Node Player = ThirdPersonPlayerScene.Instance();
		Player.Name = Id.ToString();
		RuntimeRoot.AddChild(Player);
	}


	public void PlayerDisconnected(int Id) {
		GD.Print($"Player {Id} disconnected");

		RuntimeRoot.GetNode($"{Id}").QueueFree();
	}


	public void ServerDisconnected() {
		GD.Print("Server disconnected");
	}
}