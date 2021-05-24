using Godot;
using System;

public class Lobby : Control
{
    private GameState GameState => GetNode<GameState>("/root/GameState");
    private LineEdit ConnectName => GetNode<LineEdit>("Connect/Name");
    private Label ErrorLabel => GetNode<Label>("Connect/ErrorLabel");
    private Button Host => GetNode<Button>("Connect/Host");
    private Button Join => GetNode<Button>("Connect/Join");
    private Panel ConnectPanel => GetNode<Panel>("Connect");
    private Panel PlayersPanel => GetNode<Panel>("Players");
    private AcceptDialog ErrorDialog => GetNode<AcceptDialog>("ErrorDialog");
    private ItemList PlayersList => GetNode<ItemList>("Players/List");


    public override void _Ready()
    {
// # Called every time the node is added to the scene.
        GameState.Connect(nameof(GameState.connection_failed), this, nameof(_on_connection_failed));
        GameState.Connect(nameof(GameState.connection_succeeded), this, nameof(_on_connection_success));
        GameState.Connect(nameof(GameState.player_list_changed), this, nameof(refresh_lobby));
        GameState.Connect(nameof(GameState.game_ended), this, nameof(_on_game_ended));
        GameState.Connect(nameof(GameState.game_error), this, nameof(_on_game_error));
// # Set the player name according to the system username. Fallback to the path.
        if (OS.HasEnvironment("USERNAME"))
        {
            ConnectName.Text = OS.GetEnvironment("USERNAME");
        }
        else
        {
            var desktop_path = OS.GetSystemDir(0).Replace("\\", "/").Split("/");
            ConnectName.Text = desktop_path[desktop_path.Length - 2];
        }
    }

    public void _on_host_pressed()
    {
        if (ConnectName.Text == "")
        {
            ErrorLabel.Text = "Invalid name!";
            return;
        }

        ConnectPanel.Hide();
        PlayersPanel.Show();
        ErrorLabel.Text = "";

        var player_name = ConnectName.Text;
        GameState.host_game(player_name);
        refresh_lobby();
    }

    public void _on_join_pressed()
    {
        if (ConnectName.Text == "")
        {
            ErrorLabel.Text = "Invalid name!";
            return;
        }

        var ip = GetNode<LineEdit>("Connect/IPAddress").Text;
        if (!ip.IsValidIPAddress())
        {
            ErrorLabel.Text = "Invalid IP address!";
            return;
        }

        ErrorLabel.Text = "";
        Host.Disabled = true;
        Join.Disabled = true;

        var player_name = ConnectName.Text;
        GameState.join_game(ip, player_name);
    }


    public void _on_connection_success()
    {
        ConnectPanel.Hide();
        PlayersPanel.Show();
    }


    public void _on_connection_failed()
    {
        Host.Disabled = false;
        Join.Disabled = false;
        ErrorLabel.Text = "Connection failed.";
    }


    public void _on_game_ended()
    {
        Show();
        ConnectPanel.Show();
        PlayersPanel.Hide();
        Host.Disabled = false;
        Join.Disabled = false;
    }


    public void _on_game_error(string errtxt)
    {
        ErrorDialog.DialogText = errtxt;
        ErrorDialog.PopupCenteredMinsize();
        Host.Disabled = false;
        Join.Disabled = false;
    }


    public void refresh_lobby()
    {
        var players = GameState.get_player_list();
        players.Sort();


        PlayersList.Clear();
        PlayersList.AddItem(GameState.get_player_name() + " (You)");

        foreach (var p in players)
        {
            PlayersList.AddItem(p);
        }

        GetNode<Button>("Players/Start").Disabled = !GetTree().IsNetworkServer();
    }

    public void _on_start_pressed()
    {
        GameState.begin_game();
    }


    public void _on_find_public_ip_pressed()
    {
        OS.ShellOpen("https://icanhazip.com/");
    }
}
