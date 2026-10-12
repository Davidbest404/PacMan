using UnityEngine;

public class Pacman : GridEntity
{
    private Vector2Int desiredDirection = Vector2Int.right;

    public Vector2Int FacingDirection =>
        currentDirection == Vector2Int.zero ? Vector2Int.right : currentDirection;

    public override void Initialize(Vector2Int startTile)
    {
        base.Initialize(startTile);
        currentDirection = Vector2Int.right;
        desiredDirection = Vector2Int.right;
        targetTile = startTile + currentDirection;
        RotateSprite(currentDirection);
    }

    protected override void Update()
    {
        HandleInput();
        base.Update();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            desiredDirection = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            desiredDirection = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            desiredDirection = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            desiredDirection = Vector2Int.right;
    }

    protected override void OnTileReached()
    {
        bool dirChanged = false;

        if (CanMove(desiredDirection))
        {
            if (currentDirection != desiredDirection) dirChanged = true;
            currentDirection = desiredDirection;
        }

        if (CanMove(currentDirection))
            targetTile = CurrentTile + currentDirection;
        else
            targetTile = CurrentTile;

        if (dirChanged) RotateSprite(currentDirection);
    }

    private void RotateSprite(Vector2Int dir)
    {
        float angle = 0f;
        if (dir == Vector2Int.right) angle = 0f;
        else if (dir == Vector2Int.up) angle = 90f;
        else if (dir == Vector2Int.left) angle = 180f;
        else if (dir == Vector2Int.down) angle = 270f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}