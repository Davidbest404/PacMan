using UnityEngine;

public abstract class GridEntity : MonoBehaviour
{
    public float Speed = 5f;

    public Vector2Int CurrentTile { get; protected set; }
    protected Vector2Int targetTile;
    protected Vector2Int currentDirection = Vector2Int.zero;

    public static Vector3 GridToWorld(Vector2Int tile) => new Vector3(tile.x, tile.y, 0);

    public virtual void Initialize(Vector2Int startTile)
    {
        CurrentTile = startTile;
        targetTile = startTile;
        transform.position = GridToWorld(startTile);
    }

    protected virtual void Update()
    {
        MoveStep();
    }

    protected void MoveStep()
    {
        if (CurrentTile == targetTile)
        {
            OnTileReached();
            return;
        }

        Vector3 targetPos = GridToWorld(targetTile);
        float step = Speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        if ((transform.position - targetPos).sqrMagnitude < 0.0001f)
        {
            transform.position = targetPos;
            CurrentTile = targetTile;
            ApplyTunnelWrap();
            OnTileReached();
        }
    }

    private void ApplyTunnelWrap()
    {
        if (CurrentTile.x < 0)
        {
            CurrentTile = new Vector2Int(Maze.Width - 1, CurrentTile.y);
            targetTile = CurrentTile;
            transform.position = GridToWorld(CurrentTile);
        }
        else if (CurrentTile.x >= Maze.Width)
        {
            CurrentTile = new Vector2Int(0, CurrentTile.y);
            targetTile = CurrentTile;
            transform.position = GridToWorld(CurrentTile);
        }
    }

    protected abstract void OnTileReached();

    protected virtual bool CanMove(Vector2Int dir, bool ghost = false)
    {
        if (dir == Vector2Int.zero) return false;
        Vector2Int next = CurrentTile + dir;
        return Maze.IsWalkable(next.x, next.y, ghost);
    }
}