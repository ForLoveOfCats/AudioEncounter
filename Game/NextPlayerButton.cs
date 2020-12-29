using Godot;



public class NextPlayerButton : Button {
	public void _on_NextPlayerButton_pressed() {
		Game.SpectateNextPlayer();
	}
}
