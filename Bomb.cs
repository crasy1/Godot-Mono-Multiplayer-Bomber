using Godot;
using Godot.Collections;

public class Bomb : Area2D
{
    private Array<Node> in_area = new Array<Node>();
    public int from_player;

// # Called from the animation.
    public void explode()
    {
        if (!IsNetworkMaster())
        {
// # Explode only on master.
            return;
        }

        foreach (var p in in_area)
        {
            if (p.HasMethod(nameof(Rock.exploded)))
            {
// # Exploded has a master keyword, so it will only be received by the master.
                p.Rpc(nameof(Rock.exploded), from_player);
            }
        }
    }

    public void done()
    {
        QueueFree();
    }


    public void _on_Bomb_body_entered(Node body)
    {
        if (!in_area.Contains(body))
        {
            in_area.Add(body);
        }
    }


    public void _on_Bomb_body_exited(Node body)
    {
        in_area.Remove(body);
    }
}
