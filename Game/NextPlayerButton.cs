using Godot;



public partial class NextPlayerButton : Button {
	public void _on_NextPlayerButton_pressed() {
		Game.SpectateNextPlayer();
	}
}
