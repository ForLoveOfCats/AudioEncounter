using System.Collections.Generic;

using Godot;



public class CommandTokenizer {
	public string[] Parts;
	public int Index = 0;


	public CommandTokenizer(string Command) {
		Parts = Command.Split(' ');
	}


	public Result Next() {
		if(Index >= Parts.Length) {
			return new Result();
		}

		string Output = Parts[Index];
		Index += 1;
		return new Token { Inner = Output };
	}
}



public class Result { }

public class Token : Result {
	public string Inner;
}



public class AdminControls : Node {
	public TextEdit CliOutput;
	public TextEdit LogOutput;

	public LineEdit CommandInput;

	public HashSet<int> ToSpawn = new HashSet<int>();

	public override void _Ready() {
		CliOutput = GetNode<TextEdit>("VBoxContainer/HBoxContainer/CliOutput");
		LogOutput = GetNode<TextEdit>("VBoxContainer/HBoxContainer/LogOutput");

		CommandInput = GetNode<LineEdit>("VBoxContainer/CommandInput");

		base._Ready();
	}



	public void _on_CommandInput_text_entered(string Text) {
		CommandInput.Text = "";
		var Tokenizer = new CommandTokenizer(Text);

		if(Tokenizer.Next() is Token CommandName) {
			Game.Print(">>>", Text.Trim());

			switch(CommandName.Inner) {
				case "list_all": {
					foreach(string Identifier in Game.Nicknames.Values) {
						Game.Print(Identifier);
					}
					break;
				}

				case "list_to_spawn": {
					foreach(int Id in ToSpawn) {
						Game.Print(Game.Nicknames[Id]);
					}
					break;
				}

				case "add_to_spawn": {
					if(Tokenizer.Next() is Token IdString) {
						if(int.TryParse(IdString.Inner, out int Id)) {
							if(Game.Alive.Contains(Id)) {
								Game.Print($"Player with ID {Id} is already alive");
							}
							else if(Game.Nicknames.ContainsKey(Id)) {
								ToSpawn.Add(Id);
							}
							else {
								Game.Print($"Not a known player ID: {Id}");
							}
						}
						else {
							Game.Print($"Cannot parse '{IdString.Inner}' as int");
						}
					}
					break;
				}

				case "remove_to_spawn": {
					if(Tokenizer.Next() is Token IdString) {
						if(int.TryParse(IdString.Inner, out int Id)) {
							if(!ToSpawn.Remove(Id)) {
								Game.Print($"Player with ID {Id} was not in spawn list");
							}
						}
						else {
							Game.Print($"Cannot parse '{IdString.Inner}' as int");
						}
					}
					break;
				}

				case "add_all_to_spawn": {
					foreach(int Id in Game.Nicknames.Keys) {
						if(!Game.Alive.Contains(Id)) {
							ToSpawn.Add(Id);
							Game.Print($"Added player with ID {Id} to spawn list");
						}
					}
					break;
				}

				case "clear_to_spawn": {
					ToSpawn.Clear();
					break;
				}

				case "do_spawn": {
					foreach(int Id in ToSpawn) {
						Game.Alive.Add(Id);
						Game.Self.Rpc(nameof(Game.NetSpawn), Id, new Vector3(0, 2, 0), new Vector3());
						Game.Self.NetSpawn(Id, new Vector3(0, 2, 0), new Vector3());
					}
					ToSpawn.Clear();
					break;
				}
			}

			Game.Print();
		}
	}
}

