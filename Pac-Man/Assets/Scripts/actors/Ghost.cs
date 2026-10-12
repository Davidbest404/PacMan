using System.Collections.Generic;
using UnityEngine;

public enum GhostType { Blinky, Pinky, Inky, Clyde }

public enum GhostState
{
    Wander,
    Chase,
    Flee,
    Watchman,
    Frightened,
    Damaged
}

public class Ghost : GridEntity
{
    public GhostType Type { get; private set; }
    public GhostState State { get; private set; }

    public float NormalSpeed = 5f;
    public float FrightenedSpeed = 3f;
    public float ReturnSpeed = 12f;

    [Header("Frightened visual")]
    [SerializeField] private float frightenedBlinkStart = 2f;
    [SerializeField] private float frightenedBlinkSpeed = 6f;

    [Header("Detection (Blinky / Inky)")]
    [SerializeField] private float detectRadius = 6f;
    [SerializeField] private float breakAwayRadius = 8f;

    [Header("Paranoid (Inky)")]
    [Range(0f, 1f)]
    [SerializeField] private float paranoidReversalChance = 0.15f;

    [Header("Damaged")]
    [SerializeField] private float damagedBaseDuration = 5f;

    private static readonly Color FrightenedColor = new Color(0.15f, 0.15f, 0.95f);
    private static readonly Color FrightenedBlinkColor = new Color(0.95f, 0.95f, 0.95f);
    private static readonly Color DamagedColor = new Color(0.5f, 0.5f, 0.5f);

    private Vector2Int homeTile;
    private Vector2Int exitTile;
    private float frightenedTimer;
    private float damagedTimer;
    private int watchmanWaypointIndex;

    private bool damagedOnBase;
    private bool leavingBase;

    private Pacman pacman;
    private GameManager gm;
    private SpriteRenderer sr;

    private static readonly Vector2Int[] WatchmanRoute = new Vector2Int[]
    {
        new Vector2Int( 1, 25),
        new Vector2Int(26, 25),
        new Vector2Int(26, 23),
        new Vector2Int( 3,  5),
        new Vector2Int(24,  5),
    };

    private static readonly Vector2Int[] AllDirs =
        { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };

    // ───────── Setup ─────────

    public void Setup(GhostType type, Pacman pacman, GameManager gm,
                      Vector2Int spawn, Vector2Int homeBase, Vector2Int exit)
    {
        Type = type;
        this.pacman = pacman;
        this.gm = gm;
        homeTile = homeBase;
        exitTile = exit;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = SpriteFactory.GhostSprite;

        Initialize(spawn);
        State = (type == GhostType.Clyde) ? GhostState.Watchman : GhostState.Wander;
        Speed = NormalSpeed;
        leavingBase = true;
        RestoreColor();

        if (Type == GhostType.Clyde)
            watchmanWaypointIndex = FindNearestWaypointIndex();
    }

    public override void Initialize(Vector2Int startTile)
    {
        base.Initialize(startTile);
        currentDirection = Vector2Int.up;
    }

    public void ResetGhost(Vector2Int spawn)
    {
        State = (Type == GhostType.Clyde) ? GhostState.Watchman : GhostState.Wander;
        Speed = NormalSpeed;
        frightenedTimer = 0f;
        damagedTimer = 0f;
        damagedOnBase = false;
        leavingBase = true;
        transform.rotation = Quaternion.identity;
        Initialize(spawn);
        RestoreColor();

        if (Type == GhostType.Clyde)
            watchmanWaypointIndex = FindNearestWaypointIndex();
    }

    // ───────── Update ─────────

    protected override void Update()
    {
        if (State == GhostState.Frightened)
        {
            frightenedTimer -= Time.deltaTime;
            UpdateFrightenedVisual();
            if (frightenedTimer <= 0f)
            {
                State = GhostState.Wander;
                RestoreColor();
            }
        }
        else if (State == GhostState.Damaged && damagedOnBase)
        {
            damagedTimer -= Time.deltaTime;
            if (damagedTimer <= 0f)
            {
                damagedOnBase = false;
                leavingBase = true;
                State = GhostState.Wander;
                RestoreColor();
            }
        }

        base.Update();
    }

    private void UpdateFrightenedVisual()
    {
        if (sr == null) return;
        if (frightenedTimer > frightenedBlinkStart) { sr.color = FrightenedColor; return; }

        float progress = 1f - (frightenedTimer / frightenedBlinkStart);
        float speed = Mathf.Lerp(frightenedBlinkSpeed, frightenedBlinkSpeed * 2.5f, progress);
        float t = Mathf.PingPong(Time.time * speed, 1f);
        sr.color = Color.Lerp(FrightenedColor, FrightenedBlinkColor, t);
    }

    // ───────── Публичные события ─────────

    public void Frighten(float duration)
    {
        if (State == GhostState.Damaged) return;

        State = GhostState.Frightened;
        frightenedTimer = duration;
        Speed = FrightenedSpeed;
        if (sr != null) sr.color = FrightenedColor;
        ReverseDirection();
    }

    public void Damage()
    {
        State = GhostState.Damaged;
        Speed = ReturnSpeed;
        damagedTimer = 0f;
        damagedOnBase = false;
        leavingBase = false;
        if (sr != null) sr.color = DamagedColor;
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

    // ───────── Ограничение движения ─────────

    protected override bool CanMove(Vector2Int dir, bool ghost = false)
    {
        Vector2Int next = CurrentTile + dir;

        if (Maze.GetTile(next.x, next.y) == Maze.Door)
        {
            // Дверь проходима только для повреждённых и для тех, кто выходит с базы.
            if (State != GhostState.Damaged && !leavingBase) return false;
        }

        return base.CanMove(dir, true);
    }

    // ───────── Основной коллбэк ─────────

    protected override void OnTileReached()
    {
        // Обычный призрак на базе — выводим его.
        if (State != GhostState.Damaged && IsOnBase() && !leavingBase)
            leavingBase = true;

        // ── Выход с базы ──
        if (leavingBase)
        {
            if (CurrentTile == exitTile || !IsOnBase())
            {
                leavingBase = false;
                if (State == GhostState.Damaged)
                {
                    State = GhostState.Wander;
                    RestoreColor();
                }
            }
            else
            {
                Vector2Int dir = DirectionTowardTarget(exitTile);
                if (dir == Vector2Int.zero)
                {
                    var valid = GetValidDirections(allowReverse: true);
                    dir = valid.Count > 0 ? ChooseToward(exitTile, valid) : -currentDirection;
                }
                MoveInDirection(dir);
                return;
            }
        }

        // ── Возврат на базу после урона (BFS) ──
        if (State == GhostState.Damaged)
        {
            HandleDamaged();
            return;
        }

        // ── Испуг: случайное направление ──
        if (State == GhostState.Frightened)
        {
            MoveInDirection(PickRandomDirection());
            return;
        }

        // ── Обычное поведение по роли ──
        UpdateAggressiveState();
        MoveInDirection(ChooseBehaviorDirection());
    }

    private bool IsOnBase()
    {
        return CurrentTile.x >= 11 && CurrentTile.x <= 16
            && CurrentTile.y >= 15 && CurrentTile.y <= 17;
    }

    private void HandleDamaged()
    {
        if (IsOnBase())
        {
            if (!damagedOnBase)
            {
                damagedOnBase = true;
                damagedTimer = damagedBaseDuration;
            }
            targetTile = CurrentTile;
            return;
        }

        // ★ BFS — единственный надёжный способ не осциллировать
        Vector2Int dir = DirectionTowardTarget(homeTile);

        if (dir == Vector2Int.zero)
        {
            // Страховка: BFS не нашёл путь. Откатываемся на жадный.
            var valid = GetValidDirections(allowReverse: true);
            dir = valid.Count > 0 ? ChooseToward(homeTile, valid) : -currentDirection;
        }
        MoveInDirection(dir);
    }

    // ───────── BFS-поиск пути ─────────

    /// <summary>
    /// BFS от CurrentTile до target с учётом стен и правил двери.
    /// Возвращает направление первого шага или Vector2Int.zero, если пути нет.
    /// </summary>
    private Vector2Int DirectionTowardTarget(Vector2Int target)
    {
        if (CurrentTile == target) return Vector2Int.zero;

        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        queue.Enqueue(CurrentTile);
        visited.Add(CurrentTile);

        bool found = false;
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == target) { found = true; break; }

            foreach (var dir in AllDirs)
            {
                var next = cur + dir;

                // Туннель
                if (next.x < 0) next.x = Maze.Width - 1;
                else if (next.x >= Maze.Width) next.x = 0;

                if (visited.Contains(next)) continue;

                char tile = Maze.GetTile(next.x, next.y);
                if (tile == Maze.Wall) continue;
                if (tile == Maze.Door && !CanPassDoorForPath()) continue;

                visited.Add(next);
                cameFrom[next] = cur;
                queue.Enqueue(next);
            }
        }

        if (!found) return Vector2Int.zero;

        // Восстанавливаем путь: идём от target назад, пока не упрёмся в CurrentTile.
        var node = target;
        while (cameFrom.ContainsKey(node) && cameFrom[node] != CurrentTile)
            node = cameFrom[node];

        Vector2Int step = node - CurrentTile;

        // Туннель: компенсируем "прыжок" через край
        if (step.x > 1) step.x = -1;
        else if (step.x < -1) step.x = 1;

        return step;
    }

    private bool CanPassDoorForPath()
    {
        // Повреждённые и выходящие с базы — дверь открыта.
        return State == GhostState.Damaged || leavingBase;
    }

    // ───────── Поведение по роли ─────────

    private void UpdateAggressiveState()
    {
        if (pacman == null) return;

        float distSq = (CurrentTile - pacman.CurrentTile).sqrMagnitude;
        float detectSq = detectRadius * detectRadius;
        float breakSq = breakAwayRadius * breakAwayRadius;

        switch (Type)
        {
            case GhostType.Blinky:
                if (State == GhostState.Chase)
                {
                    if (distSq > breakSq) State = GhostState.Wander;
                }
                else
                {
                    State = distSq <= detectSq ? GhostState.Chase : GhostState.Wander;
                }
                break;

            case GhostType.Inky:
                if (State == GhostState.Flee)
                {
                    if (Random.value < paranoidReversalChance) State = GhostState.Chase;
                    else if (distSq > breakSq) State = GhostState.Wander;
                }
                else if (State == GhostState.Chase)
                {
                    if (distSq > breakSq) State = GhostState.Wander;
                }
                else
                {
                    State = distSq <= detectSq ? GhostState.Flee : GhostState.Wander;
                }
                break;
        }
    }

    private Vector2Int ChooseBehaviorDirection()
    {
        var valid = GetValidDirections(allowReverse: false);
        if (valid.Count == 0) return -currentDirection;

        switch (Type)
        {
            case GhostType.Clyde:
                return ChooseWatchmanDirection(valid);
            case GhostType.Pinky:
                return valid[Random.Range(0, valid.Count)];
            case GhostType.Blinky:
                if (State == GhostState.Chase) return ChooseToward(pacman.CurrentTile, valid);
                return valid[Random.Range(0, valid.Count)];
            case GhostType.Inky:
                if (State == GhostState.Chase) return ChooseToward(pacman.CurrentTile, valid);
                if (State == GhostState.Flee) return ChooseAwayFrom(pacman.CurrentTile, valid);
                return valid[Random.Range(0, valid.Count)];
        }
        return valid[Random.Range(0, valid.Count)];
    }

    private Vector2Int ChooseWatchmanDirection(List<Vector2Int> valid)
    {
        if (WatchmanRoute.Length == 0) return valid[Random.Range(0, valid.Count)];

        Vector2Int target = WatchmanRoute[watchmanWaypointIndex];
        if (CurrentTile == target)
        {
            watchmanWaypointIndex = (watchmanWaypointIndex + 1) % WatchmanRoute.Length;
            target = WatchmanRoute[watchmanWaypointIndex];
        }

        Vector2Int diff = target - CurrentTile;
        if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1 && valid.Contains(diff))
            return diff;

        return ChooseToward(target, valid);
    }

    private int FindNearestWaypointIndex()
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < WatchmanRoute.Length; i++)
        {
            float d = (WatchmanRoute[i] - CurrentTile).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    // ───────── Хелперы ─────────

    private List<Vector2Int> GetValidDirections(bool allowReverse)
    {
        var list = new List<Vector2Int>(4);
        foreach (var dir in AllDirs)
        {
            if (!allowReverse && currentDirection != Vector2Int.zero && dir == -currentDirection)
                continue;
            if (CanMove(dir, true)) list.Add(dir);
        }
        return list;
    }

    private Vector2Int PickRandomDirection()
    {
        var valid = GetValidDirections(allowReverse: false);
        if (valid.Count == 0) return -currentDirection;
        return valid[Random.Range(0, valid.Count)];
    }

    private Vector2Int ChooseToward(Vector2Int target, List<Vector2Int> valid)
    {
        Vector2Int best = valid[0];
        float bestDist = float.MaxValue;
        foreach (var dir in valid)
        {
            Vector2Int next = CurrentTile + dir;
            float d = (next - target).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = dir; }
        }
        return best;
    }

    private Vector2Int ChooseAwayFrom(Vector2Int target, List<Vector2Int> valid)
    {
        Vector2Int best = valid[0];
        float bestDist = -1f;
        foreach (var dir in valid)
        {
            Vector2Int next = CurrentTile + dir;
            float d = (next - target).sqrMagnitude;
            if (d > bestDist) { bestDist = d; best = dir; }
        }
        return best;
    }

    private void MoveInDirection(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) { targetTile = CurrentTile; return; }

        if (!CanMove(dir, true))
        {
            dir = -currentDirection;
            if (!CanMove(dir, true)) { targetTile = CurrentTile; return; }
        }

        currentDirection = dir;
        targetTile = CurrentTile + dir;
    }

    private void ReverseDirection()
    {
        if (currentDirection == Vector2Int.zero) return;
        Vector2Int rev = -currentDirection;
        if (CanMove(rev, true))
        {
            currentDirection = rev;
            targetTile = CurrentTile + rev;
        }
    }
}