using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class GameState : Node

{
    private int DEFAULT_PORT = 10567;

    // Max number of players.
    private int MAX_PEERS = 12;

    private NetworkedMultiplayerENet peer = null;

// Name for my player.
    private string player_name = "The Warrior";

    // Names for remote players in id:name format.
    private Godot.Collections.Dictionary<int, string> players = new Godot.Collections.Dictionary<int, string>();
    private Array players_ready = new Array();

    // Signals to let lobby GUI know what's going on.
    [Signal]
    public delegate void player_list_changed();

    [Signal]
    public delegate void connection_failed();

    [Signal]
    public delegate void connection_succeeded();

    [Signal]
    public delegate void game_ended();

    [Signal]
    public delegate void game_error(string what);


//  Callback from SceneTree.
    public void _player_connected(int id)
    {
//  Registration of a client beings here, tell the connected player that we are here.
        RpcId(id, "register_player", player_name);
    }

//  Callback from SceneTree.
    private void _player_disconnected(int id)
    {
// Game is in progress.
        if (HasNode("/root/World"))
        {
            if (GetTree().IsNetworkServer())
            {
                EmitSignal("game_error", "Player " + players[id] + " disconnected");
                end_game();
            }
        }

        else
        {
            // Game is not in progress.
            // Unregister this player.
            unregister_player(id);
        }
    }

//  Callback from SceneTree, only for clients (not server).
    public void _connected_ok()
    {
        // We just connected to a server
        EmitSignal("connection_succeeded");
    }

    // Callback from SceneTree, only for clients (not server).
    public void _server_disconnected()
    {
        EmitSignal("game_error", "Server disconnected");
        end_game();
    }


    // Callback from SceneTree, only for clients (not server).
    public void _connected_fail()
    {
        // Remove peer
        GetTree().NetworkPeer = null;
        EmitSignal("connection_failed");
    }

    // Lobby management functions.
    [Remote]
    public void register_player(string new_player_name)
    {
        var id = GetTree().GetRpcSenderId();
        GD.Print(id);
        players[id] = new_player_name;
        EmitSignal("player_list_changed");
    }


    public void unregister_player(int id)
    {
        players.Remove(id);
        EmitSignal("player_list_changed");
    }

    [Remote]
    public void pre_start_game(Godot.Collections.Dictionary<int, int> spawn_points)
    {
// Change scene.
        var world = GD.Load<PackedScene>("res://world.tscn").Instance() as Node2D;
        GetTree().Root.AddChild(world);

        GetTree().Root.GetNode<Lobby>("Lobby").Hide();

        var player_scene = GD.Load<PackedScene>("res://player.tscn");
        foreach (var p_id in spawn_points)
        {
            var spawn_pos = world.GetNode<Node2D>("SpawnPoints/" + p_id.Value).Position;
            var player = player_scene.Instance() as Player;
// # Use unique ID as node name.
            player.Name = p_id.Value.ToString();
            player.Position = spawn_pos;
// #set unique id as master.
            player.SetNetworkMaster(p_id.Key);

            if (p_id.Key == GetTree().GetNetworkUniqueId())
            {
// # If node for this peer id, set name.
                player.set_player_name(player_name);
            }
            else
            {
// # Otherwise set name from peer.
                player.set_player_name(players[p_id.Key]);
            }

            world.GetNode<Node2D>("Players").AddChild(player);
        }

// # Set up score.
        world.GetNode<Score>("Score").add_player(GetTree().GetNetworkUniqueId(), player_name);
        foreach (var pn in players)
        {
            world.GetNode<Score>("Score").add_player(pn.Key, pn.Value);
        }


        if (!GetTree().IsNetworkServer())
        {
// # Tell server we are ready to start.
            RpcId(1, "ready_to_start", GetTree().GetNetworkUniqueId());
        }
        else if (players.Count == 0)

        {
            post_start_game();
        }
    }


    [Remote]
    public void post_start_game()
    {
// # Unpause and unleash the game!
        GetTree().Paused = false;
    }

    [Remote]
    public void ready_to_start(int id)
    {
        // assert(get_tree().is_network_server())

        if (!players_ready.Contains(id))
        {
            players_ready.Add(id);
        }

        if (players_ready.Count == players.Count)
        {
            foreach (var p in players)
            {
                RpcId(p.Key, "post_start_game");
            }

            post_start_game();
        }
    }

    public void host_game(string new_player_name)
    {
        player_name = new_player_name;
        peer = new NetworkedMultiplayerENet();
        peer.CreateServer(DEFAULT_PORT, MAX_PEERS);
        GetTree().NetworkPeer = peer;
    }

    public void join_game(string ip, string new_player_name)
    {
        player_name = new_player_name;
        peer = new NetworkedMultiplayerENet();
        peer.CreateClient(ip, DEFAULT_PORT);
        GetTree().NetworkPeer = peer;
    }


    public List<string> get_player_list()
    {
        var playersValues = players.Values;
        return playersValues.ToList();
    }


    public string get_player_name()
    {
        return player_name;
    }

    public void begin_game()
    {
        // assert(get_tree().is_network_server())

// # Create a dictionary with peer id and respective spawn points, could be improved by randomizing.
        var spawn_points = new Godot.Collections.Dictionary<int, int>();
// # Server in spawn point 0.
        spawn_points[1] = 0;
        var spawn_point_idx = 1;
        foreach (var p in players)
        {
            spawn_points[p.Key] = spawn_point_idx;
            spawn_point_idx += 1;
        }

// # Call to pre-start game with the spawn points.
        foreach (var p in players)
        {
            RpcId(p.Key, "pre_start_game", spawn_points);
        }

        pre_start_game(spawn_points);
    }

    public void end_game()
    {
// # Game is in progress.
        if (HasNode("/root/World"))
        {
// # End it
            GetNode("/root/World").QueueFree();
        }

        EmitSignal("game_ended");
        players.Clear();
    }


    public override void _Ready()
    {
        GetTree().Connect("network_peer_connected", this, nameof(_player_connected));
        GetTree().Connect("network_peer_disconnected", this, nameof(_player_disconnected));
        GetTree().Connect("connected_to_server", this, nameof(_connected_ok));
        GetTree().Connect("connection_failed", this, nameof(_connected_fail));
        GetTree().Connect("server_disconnected", this, nameof(_server_disconnected));
    }
}
