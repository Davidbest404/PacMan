using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float frightenedDuration = 7f;
    [SerializeField] private float scatterDuration = 7f;
    [SerializeField] private float chaseDuration = 20f;
    [SerializeField] private float restartDelay = 1.2f;

    public Pacman Pacman { get; private set; }
    public List<Ghost> Ghosts { get; private set; } = new List<Ghost>();
    public int Score { get; private set; }
    public int PelletsRemaining { get; private set; }
    public int TotalPellets { get; private set; }

    private readonly Vector2Int pacmanSpawn = new Vector2Int(13, 7);
    private readonly Vector2Int[] ghostSpawns = new Vector2Int[]
    {
        new Vector2Int(13, 19), // Blinky
        new Vector2Int(12, 19), // Pinky
        new Vector2Int(14, 19), // Inky
        new Vector2Int(15, 19), // Clyde
    };
    private readonly Vector2Int[] scatterTargets = new Vector2Int[]
    {
        new Vector2Int(26, 30), // top-right
        new Vector2Int(1, 30),  // top-left
        new Vector2Int(26, 0),  // bottom-right
        new Vector2Int(1, 0),   // bottom-left
    };

    private readonly List<Collectible> allPellets = new List<Collectible>();

    private bool isScatter = true;
    private float modeTimer;
    private bool isResetting = false;
    private bool gameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SetupCamera();
        BuildMaze();
        SpawnEntities();
        ResetLevel();
    }

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = Maze.Height / 2f + 1f;
        cam.transform.position = new Vector3(Maze.Width / 2f - 0.5f, Maze.Height / 2f - 0.5f, -10f);
        cam.backgroundColor = Color.black;
    }

    private void BuildMaze()
    {
        var mazeRoot = new GameObject("Maze").transform;
        mazeRoot.SetParent(transform);

        var builder = new MazeBuilder(mazeRoot);
        allPellets.AddRange(builder.Build());
        TotalPellets = allPellets.Count;
    }

    private void SpawnEntities()
    {
        // Pacman
        var pacObj = SpriteFactory.CreateSprite("Pacman", SpriteFactory.PacmanSprite, Color.yellow, 5);
        Pacman = pacObj.AddComponent<Pacman>();
        Pacman.Speed = 6f;

        // Ghosts
        Ghosts.Clear();
        SpawnGhost(GhostType.Blinky, 0, new Color(1f, 0.1f, 0.1f));
        SpawnGhost(GhostType.Pinky, 1, new Color(1f, 0.6f, 0.8f));
        SpawnGhost(GhostType.Inky, 2, new Color(0.2f, 0.9f, 1f));
        SpawnGhost(GhostType.Clyde, 3, new Color(1f, 0.6f, 0.1f));
    }

    private void SpawnGhost(GhostType type, int index, Color color)
    {
        var go = SpriteFactory.CreateSprite(type.ToString(), SpriteFactory.GhostSprite, color, 4);
        var ghost = go.AddComponent<Ghost>();
        ghost.Setup(type, Pacman, this, ghostSpawns[index], scatterTargets[index]);
        Ghosts.Add(ghost);
    }

    private void Update()
    {
        if (gameOver || isResetting) return;

        UpdateModeTimer();
        CheckPelletCollisions();
        CheckGhostCollisions();
    }

    private void UpdateModeTimer()
    {
        modeTimer -= Time.deltaTime;
        if (modeTimer > 0f) return;

        isScatter = !isScatter;
        modeTimer = isScatter ? scatterDuration : chaseDuration;

        var targetState = isScatter ? GhostState.Scatter : GhostState.Chase;
        foreach (var g in Ghosts)
            g.SetMode(targetState);
    }

    private void CheckPelletCollisions()
    {
        Vector3 pp = Pacman.transform.position;
        foreach (var p in allPellets)
        {
            if (p == null || p.IsCollected) continue;
            if ((p.transform.position - pp).sqrMagnitude < 0.09f)
                p.Collect();
        }
    }

    private void CheckGhostCollisions()
    {
        Vector3 pp = Pacman.transform.position;
        foreach (var g in Ghosts)
        {
            if (g.State == GhostState.Eaten) continue;
            if ((g.transform.position - pp).sqrMagnitude < 0.36f)
            {
                if (g.State == GhostState.Frightened)
                {
                    Score += 200;
                    g.Eaten();
                }
                else
                {
                    StartCoroutine(DeathRoutine());
                }
                break;
            }
        }
    }

    public void OnPelletCollected(Collectible c)
    {
        Score += c.ScoreValue;
        PelletsRemaining--;
        if (PelletsRemaining <= 0)
            TriggerRestart();
    }

    public void OnPowerPelletCollected(Collectible c)
    {
        Score += c.ScoreValue;
        PelletsRemaining--;

        foreach (var g in Ghosts)
            g.Frighten(frightenedDuration);

        // После окончания frightened — снова начать с Scatter
        isScatter = true;
        modeTimer = frightenedDuration;

        if (PelletsRemaining <= 0)
            TriggerRestart();
    }

    private void TriggerRestart()
    {
        if (isResetting) return;
        isResetting = true;
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (isResetting) yield break;
        isResetting = true;
        if (Pacman != null) Pacman.gameObject.SetActive(false);
        yield return new WaitForSeconds(restartDelay);
        ResetLevel();
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);
        ResetLevel();
    }

    public void ResetLevel()
    {
        Score = 0;
        PelletsRemaining = TotalPellets;
        gameOver = false;
        isResetting = false;

        foreach (var p in allPellets)
            p.ResetCollectible();

        if (!Pacman.gameObject.activeSelf)
            Pacman.gameObject.SetActive(true);

        Pacman.Initialize(pacmanSpawn);

        for (int i = 0; i < Ghosts.Count; i++)
            Ghosts[i].ResetGhost(ghostSpawns[i]);

        isScatter = true;
        modeTimer = scatterDuration;
        foreach (var g in Ghosts)
            g.SetMode(GhostState.Scatter);
    }

    public Vector2Int GetGhostTile(GhostType type)
    {
        foreach (var g in Ghosts)
            if (g.Type == type) return g.CurrentTile;
        return Vector2Int.zero;
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(15, 10, 400, 40), $"Score: {Score}", style);
        GUI.Label(new Rect(15, 40, 400, 40), $"Pellets left: {PelletsRemaining}", style);
    }
}