using Godot;
using System;

public class Player : KinematicBody2D
{
    private float MOTION_SPEED = 90.0f;

    [Puppet] private Vector2 puppet_pos = Vector2.Zero;
    [Puppet] private Vector2 puppet_motion = Vector2.Zero;

    [Export] private bool stunned = false;

    private string current_anim = "";
    private bool prev_bombing = false;
    private int bomb_index = 0;

    public override void _Ready()
    {
        stunned = false;
        puppet_pos = Position;
    }

    [Sync]
// # Use sync because it will be called everywhere
    private void setup_bomb(string bomb_name, Vector2 pos, int by_who)
    {
        var bomb = GD.Load<PackedScene>("res://bomb.tscn").Instance() as Bomb;
// # Ensure unique name for the bomb
        bomb.Name = bomb_name;
        bomb.Position = pos;
        bomb.from_player = by_who;
// # No need to set network master to bomb, will be owned by server by default
        GetNode("../..").AddChild(bomb);
    }

    [Puppet]
    public void stun()
    {
        stunned = true;
    }

    [Master]
    public void exploded(int _by_who)
    {
        if (stunned)
        {
            return;
        }

// # Stun puppets
        Rpc(nameof(stun));
// # Stun master - could use sync to do both at once
        stun();
    }

    public void set_player_name(string new_name)
    {
        GetNode<Label>("label").Text = new_name;
    }

    public override void _PhysicsProcess(float delta)
    {
        var motion = Vector2.Zero;

        if (IsNetworkMaster())
        {
            if (Input.IsActionPressed("move_left"))
            {
                motion += new Vector2(-1, 0);
            }

            if (Input.IsActionPressed("move_right"))
            {
                motion += new Vector2(1, 0);
            }

            if (Input.IsActionPressed("move_up"))
            {
                motion += new Vector2(0, -1);
            }

            if (Input.IsActionPressed("move_down"))
            {
                motion += new Vector2(0, 1);
            }

            var bombing = Input.IsActionPressed("set_bomb");

            if (stunned)
            {
                bombing = false;
                motion = Vector2.Zero;
            }

            if (bombing && !prev_bombing)
            {
                var bomb_name = Name + bomb_index;
                var bomb_pos = Position;
                Rpc(nameof(setup_bomb), bomb_name, bomb_pos, GetTree().GetNetworkUniqueId());
            }

            prev_bombing = bombing;

            Rset(nameof(puppet_motion), motion);
            Rset(nameof(puppet_pos), Position);
        }
        else
        {
            Position = puppet_pos;
            motion = puppet_motion;
        }

        var new_anim = "standing";
        if (motion.y < 0)
        {
            new_anim = "walk_up";
        }
        else if (motion.y > 0)
        {
            new_anim = "walk_down";
        }
        else if (motion.x < 0)
        {
            new_anim = "walk_left";
        }
        else if (motion.x > 0)
        {
            new_anim = "walk_right";
        }

        if (stunned)
        {
            new_anim = "stunned";
        }

        if (new_anim != current_anim)
        {
            current_anim = new_anim;
            GetNode<AnimationPlayer>("anim").Play(current_anim);
        }


// # FIXME: Use move_and_slide
        MoveAndSlide(motion * MOTION_SPEED);
        if (!IsNetworkMaster())
        {
// # To avoid jitter
            puppet_pos = Position;
        }
    }
}
