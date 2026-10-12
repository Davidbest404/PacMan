using System.Collections.Generic;
using UnityEngine;

public class MazeBuilder
{
    private readonly Transform parent;
    private readonly List<Collectible> pellets = new List<Collectible>();

    public MazeBuilder(Transform parent)
    {
        this.parent = parent;
    }

    public List<Collectible> Build()
    {
        pellets.Clear();

        for (int y = 0; y < Maze.Height; y++)
        {
            for (int x = 0; x < Maze.Width; x++)
            {
                char c = Maze.GetTile(x, y);
                Vector3 pos = new Vector3(x, y, 0);

                switch (c)
                {
                    case Maze.Wall:
                        CreateWall(pos);
                        break;
                    case Maze.Door:
                        CreateDoor(pos);
                        break;
                    case Maze.Pellet:
                        CreatePellet(pos, false);
                        break;
                    case Maze.PowerPellet:
                        CreatePellet(pos, true);
                        break;
                }
            }
        }

        return pellets;
    }

    private void CreateWall(Vector3 pos)
    {
        var go = SpriteFactory.CreateSprite("Wall", SpriteFactory.Square, new Color(0.15f, 0.15f, 0.9f), 0);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one;
    }

    private void CreateDoor(Vector3 pos)
    {
        var go = SpriteFactory.CreateSprite("Door", SpriteFactory.Square, new Color(1f, 0.7f, 0.8f), 1);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1f, 0.25f, 1f);
    }

    private void CreatePellet(Vector3 pos, bool power)
    {
        var go = SpriteFactory.CreateSprite(
            power ? "PowerPellet" : "Pellet",
            SpriteFactory.Circle,
            Color.white,
            2);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * (power ? 0.55f : 0.18f);

        Collectible collectible = power
            ? go.AddComponent<PowerPellet>()
            : (Collectible)go.AddComponent<Pellet>();

        pellets.Add(collectible);
    }
}