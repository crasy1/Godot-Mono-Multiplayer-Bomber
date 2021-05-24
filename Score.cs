using Godot;
using System;
using Godot.Collections;

public class Score : HBoxContainer
{
    private Dictionary<int,PlayerLabel> player_labels = new Dictionary<int, PlayerLabel>();
    private Label Winner => GetNode<Label>("../Winner");

    public override void _Ready()
    {
        Winner.Hide();
        SetProcess(true);
    }


    public override void _Process(float delta)
    {
        var rocks_left = GetNode<Node2D>("../Rocks").GetChildCount();
        if (rocks_left == 0)
        {
            var winner_name = "";
            var winner_score = 0;
            foreach (var p in player_labels)
            {
                
                if (player_labels[p.Key].score > winner_score)
                {
                    winner_score = player_labels[p.Key].score;
                    winner_name = player_labels[p.Key].name;
                }
            }


            Winner.Text="THE WINNER IS:\n" + winner_name;
            Winner.Show();
        }
    }


    [Sync]
    public void increase_score(int for_who)
    {
        // assert(for_who in player_labels)
        var pl = player_labels[for_who];
        pl.score += 1;
        pl.Label.Text = pl.name + "\n" + pl.score;
    }

    public void add_player(int id, string new_player_name)
    {
        var l = new Label();
        l.SetAlign(Label.AlignEnum.Center);
        l.SetText(new_player_name + "\n" + "0");
        l.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
        var font = new DynamicFont();
        font.SetSize(18);
        font.FontData = GD.Load<DynamicFontData>("res://montserrat.otf");
        l.AddFontOverride("font", font);
        AddChild(l);
        PlayerLabel playerLabel = new PlayerLabel();
        playerLabel.name = new_player_name;
        playerLabel.Label = l;
        playerLabel.score = 0;
        player_labels[id] = playerLabel;
    }


    private void _on_exit_game_pressed()
    {
        GetNode<GameState>("/root/GameState").end_game();
    }
}
