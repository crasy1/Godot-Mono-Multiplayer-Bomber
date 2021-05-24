using Godot;
using System;

public class Rock : KinematicBody2D
{
    // Sent to everyone else
    [Puppet]
    public void do_explosion()
    {
        GetNode<AnimationPlayer>("AnimationPlayer").Play("explode");
    }

// Received by owner of the rock
    [Master]
    public void exploded(int by_who)
    {
        // Re-sent to puppet rocks
        Rpc(nameof(do_explosion));
        GetNode<HBoxContainer>("../../Score").Rpc(nameof(Score.increase_score), by_who);
        do_explosion();
    }
}
