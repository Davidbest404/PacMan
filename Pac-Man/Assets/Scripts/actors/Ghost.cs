using System.Collections.Generic;
using UnityEngine;

public enum GhostType { Blinky, Pinky, Inky, Clyde }
public enum GhostState { Scatter, Chase, Frightened, Eaten }

public class Ghost : GridEntity
{
    public GhostType Type { get; private set; }
    public GhostState State { get; private set; }

    public float NormalSpeed = 5f;
    public float FrightenedSpeed = 3f;
    public float EatenSpeed = 14f;

    [Header("Frightened visual")]
    [Tooltip("За сколько секунд до конца эффекта призрак начинает мигать")]
    [SerializeField] private float frightenedBlinkStart = 2f;
    [Tooltip("Скорость мигания (циклов в секунду)")]
    [SerializeField] private float frightenedBlinkSpeed = 6f;

    private static readonly Color FrightenedColor = new Color(0.15f, 0.15f, 0.95f);
    private static readonly Color FrightenedBlinkColor = new Color(0.95f, 0.95f, 0.95f);

    private Vector2Int scatterTarget;
    private Vector2Int homeTile;
    private float frightenedTimer;
    private Pacman pacman;
    private GameManager gm;
    private SpriteRenderer sr;

    private static readonly Vector2Int[] AllDirs =
        { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };

    public void Setup(GhostType type, Pacman pacman, GameManager gm, Vector2Int spawn, Vector2Int scatter)
    {
        Type = type;
        this.pacman = pacman;
        this.gm = gm;
        homeTile = spawn;
        scatterTarget = scatter;
        sr = GetComponent<SpriteRenderer>();
        Initialize(spawn);
        State = GhostState.Scatter;
        Speed = NormalSpeed;
    }

    public override void Initialize(Vector2Int startTile)
    {
        base.Initialize(startTile);
        currentDirection = Vector2Int.up;
        targetTile = startTile + currentDirection;
    }

    protected override void Update()
    {
        if (State == GhostState.Frightened)
        {
            frightenedTimer -= Time.deltaTime;
            UpdateFrightenedVisual();

            if (frightenedTimer <= 0f)
            {
                State = GhostState.Chase;
                RestoreColor();
            }
        }
        base.Update();
    }

    private void UpdateFrightenedVisual()
    {
        if (sr == null) return;

        if (frightenedTimer > frightenedBlinkStart)
        {
            sr.color = FrightenedColor;
            return;
        }

        // Мигание: ping-pong между синим и белым
        float t = Mathf.PingPong(Time.time * frightenedBlinkSpeed, 1f);
        sr.color = Color.Lerp(FrightenedColor, FrightenedBlinkColor, t);
    }

    public void SetMode(GhostState mode)
    {
        if (State == GhostState.Frightened || State == GhostState.Eaten) return;
        if (mode == GhostState.Scatter || mode == GhostState.Chase)
            State = mode;
    }

    public void Frighten(float duration)
    {
        if (State == GhostState.Eaten) return;

        State = GhostState.Frightened;
        frightenedTimer = duration;
        Speed = FrightenedSpeed;

        if (sr != null) sr.color = FrightenedColor;

        // Смена направления (разворот)
        if (currentDirection != Vector2Int.zero)
        {
            Vector2Int rev = -currentDirection;
            if (CanMove(rev, true))
            {
                currentDirection = rev;
                targetTile = CurrentTile + rev;
            }
        }
    }

    public void Eaten()
    {
        State = GhostState.Eaten;
        Speed = EatenSpeed;
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.5f);
    }

    public void ResetGhost(Vector2Int spawn)
    {
        State = GhostState.Scatter;
        Speed = NormalSpeed;
        frightenedTimer = 0f;
        transform.rotation = Quaternion.identity;
        Initialize(spawn);
        RestoreColor();
    }

    private void RestoreColor()
    {
        Speed = NormalSpeed;
        if (sr == null) return;
        sr.color = Type switch
        {
            GhostType.Blinky => new Color(1f, 0.1f, 0.1f),
            GhostType.Pinky => new Color(1f, 0.6f, 0.8f),
            GhostType.Inky => new Color(0.2f, 0.9f, 1f),
            GhostType.Clyde => new Color(1f, 0.6f, 0.1f),
            _ => Color.white
        };
    }

    protected override void OnTileReached()
    {
        if (State == GhostState.Eaten && CurrentTile == homeTile)
        {
            State = GhostState.Chase;
            Speed = NormalSpeed;
            RestoreColor();
        }

        Vector2Int dir = ChooseDirection();

        if (CanMove(dir, true))
        {
            currentDirection = dir;
            targetTile = CurrentTile + dir;
        }
        else
        {
            targetTile = CurrentTile;
        }
    }

    private Vector2Int ChooseDirection()
    {
        var valid = new List<Vector2Int>();

        foreach (var dir in AllDirs)
        {
            if (dir == -currentDirection) continue;
            if (!CanMove(dir, true)) continue;
            valid.Add(dir);
        }

        if (valid.Count == 0)
        {
            if (CanMove(-currentDirection, true)) return -currentDirection;
            return currentDirection;
        }

        if (State == GhostState.Frightened)
            return valid[Random.Range(0, valid.Count)];

        Vector2Int target = GetTargetTile();
        Vector2Int best = valid[0];
        float bestDist = float.MaxValue;
        foreach (var dir in valid)
        {
            Vector2Int next = CurrentTile + dir;
            float d = (next - target).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = dir;
            }
        }
        return best;
    }

    private Vector2Int GetTargetTile()
    {
        if (State == GhostState.Eaten) return homeTile;
        if (State == GhostState.Scatter) return scatterTarget;
        if (pacman == null || gm == null) return homeTile;

        Vector2Int pt = pacman.CurrentTile;
        Vector2Int pd = pacman.FacingDirection;

        switch (Type)
        {
            case GhostType.Blinky:
                return pt;
            case GhostType.Pinky:
                return pt + pd * 4;
            case GhostType.Inky:
                {
                    Vector2Int blinky = gm.GetGhostTile(GhostType.Blinky);
                    Vector2Int pivot = pt + pd * 2;
                    return pivot * 2 - blinky;
                }
            case GhostType.Clyde:
                {
                    float d = (CurrentTile - pt).sqrMagnitude;
                    return d > 64f ? pt : scatterTarget;
                }
        }
        return pt;
    }
}