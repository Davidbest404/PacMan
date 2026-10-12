using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { MainMenu, Playing, Dying, GameOver, Win }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lives")]
    [SerializeField] private int initialLives = 4;
    [SerializeField] private float deathAnimationDuration = 1.5f;
    [SerializeField] private float respawnDelay = 0.3f;

    [Header("Power pellet")]
    [SerializeField] private float frightenedDuration = 7f;

    [Header("Ghost damage scoring")]
    [SerializeField] private int ghostDamageBaseScore = 200;

    public Pacman Pacman { get; private set; }
    public List<Ghost> Ghosts { get; private set; } = new List<Ghost>();
    public int Score { get; private set; }
    public int PelletsRemaining { get; private set; }
    public int TotalPellets { get; private set; }
    public int Lives { get; private set; }
    public int SpecialBonusesCollected { get; private set; }
    public GameState State { get; private set; } = GameState.MainMenu;

    private readonly Vector2Int pacmanSpawn = new Vector2Int(13, 7);
    private readonly Vector2Int[] ghostSpawns = new Vector2Int[]
    {
        new Vector2Int(13, 17),
        new Vector2Int(12, 17),
        new Vector2Int(14, 17),
        new Vector2Int(13, 16),
    };
    private readonly Vector2Int ghostHomeTile = new Vector2Int(13, 17);
    private readonly Vector2Int ghostExitTile = new Vector2Int(13, 19);

    private readonly List<Collectible> allPellets = new List<Collectible>();
    private SpecialBonusSpawner bonusSpawner;

    private int damagedGhostsThisFrightened;
    private bool anyGhostFrightenedLastFrame;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        AudioSettings.Load();
    }

    private void Start()
    {
        SetupAudio();
        SetupCamera();
        BuildMaze();
        SpawnEntities();
        SetupSpawner();

        if (UIManager.Instance != null)
            UIManager.Instance.Build();

        Time.timeScale = 0f;
        SetState(GameState.MainMenu);
    }

    // ───────── Публичные запросы от UI ─────────

    public void RequestStartGame()
    {
        Time.timeScale = 1f;
        StartNewGame();
    }

    public void RequestMainMenu()
    {
        Time.timeScale = 0f;
        SetState(GameState.MainMenu);
    }

    // ───────── Setup ─────────

    private void SetupAudio()
    {
        var go = new GameObject("AudioManager");
        go.transform.SetParent(transform);
        var am = go.AddComponent<AudioManager>();
        am.Init();
    }

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(
            Maze.Width / 2f - 0.5f,
            Maze.Height / 2f - 0.5f,
            -10f);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        ApplyPixelPerfectCamera(cam);
    }

    private void ApplyPixelPerfectCamera(Camera cam)
    {
        if (cam == null) return;

        // Желаемая высота вида: лабиринт + запас под HUD
        float desiredViewHeight = Maze.Height + 4f;

        // Целочисленный PPU — сколько экранных пикселей приходится на 1 юнит мира
        float rawPPU = Screen.height / desiredViewHeight;
        int ppu = Mathf.Max(1, Mathf.FloorToInt(rawPPU));

        // Подгоняем orthographicSize так, чтобы PPU был ровно целым
        cam.orthographicSize = Screen.height / (2f * ppu);

        // Округляем позицию камеры до сетки пикселей —
        // иначе при нецелом PPU спрайты «дрожат» на субпикселях
        Vector3 p = cam.transform.position;
        cam.transform.position = new Vector3(
            Mathf.Round(p.x * ppu) / ppu,
            Mathf.Round(p.y * ppu) / ppu,
            p.z);
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
        var pacObj = SpriteFactory.CreateSprite("Pacman", SpriteFactory.PacmanSprite, Color.yellow, 5);
        Pacman = pacObj.AddComponent<Pacman>();
        Pacman.Speed = 6f;
        Pacman.Initialize(pacmanSpawn);

        Ghosts.Clear();
        SpawnGhost(GhostType.Blinky, 0);
        SpawnGhost(GhostType.Pinky, 1);
        SpawnGhost(GhostType.Inky, 2);
        SpawnGhost(GhostType.Clyde, 3);
    }

    private void SpawnGhost(GhostType type, int index)
    {
        var go = SpriteFactory.CreateSprite(type.ToString(), SpriteFactory.GhostSprite, Color.white, 4);
        var ghost = go.AddComponent<Ghost>();
        ghost.Setup(type, Pacman, this, ghostSpawns[index], ghostHomeTile, ghostExitTile);
        Ghosts.Add(ghost);
    }

    private void SetupSpawner()
    {
        var go = new GameObject("SpecialBonusSpawner");
        go.transform.SetParent(transform);
        bonusSpawner = go.AddComponent<SpecialBonusSpawner>();
        bonusSpawner.Setup(this);
    }

    // ───────── Update ─────────

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            ApplyPixelPerfectCamera(Camera.main);
        }

        if (State != GameState.Playing) return;
        CheckPelletCollisions();
        CheckGhostCollisions();
        UpdateFrightenedMusic();
    }

    private void CheckPelletCollisions()
    {
        Vector3 pp = Pacman.transform.position;
        foreach (var p in allPellets)
        {
            if (p == null || p.IsCollected) continue;
            if ((p.transform.position - pp).sqrMagnitude < 0.09f)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPellet();
                p.Collect();
            }
        }
    }

    private void CheckGhostCollisions()
    {
        Vector3 pp = Pacman.transform.position;
        foreach (var g in Ghosts)
        {
            if (g.State == GhostState.Damaged) continue;
            if ((g.transform.position - pp).sqrMagnitude < 0.36f)
            {
                if (g.State == GhostState.Frightened)
                {
                    int multiplier = 1 << damagedGhostsThisFrightened;
                    Score += ghostDamageBaseScore * multiplier;
                    damagedGhostsThisFrightened++;
                    g.Damage();
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayEatGhost();
                }
                else
                {
                    StartCoroutine(DeathRoutine());
                }
                break;
            }
        }
    }

    private void UpdateFrightenedMusic()
    {
        bool anyFrightened = false;
        for (int i = 0; i < Ghosts.Count; i++)
        {
            if (Ghosts[i].State == GhostState.Frightened) { anyFrightened = true; break; }
        }

        if (anyFrightened == anyGhostFrightenedLastFrame) return;
        anyGhostFrightenedLastFrame = anyFrightened;

        if (AudioManager.Instance == null) return;
        if (anyFrightened) AudioManager.Instance.PlayFrightenedMusic();
        else AudioManager.Instance.PlayGameMusic();
    }

    // ───────── События ─────────

    public void OnPelletCollected(Collectible c)
    {
        Score += c.ScoreValue;
        PelletsRemaining--;
        if (PelletsRemaining <= 0) StartCoroutine(WinRoutine());
    }

    public void OnPowerPelletCollected(Collectible c)
    {
        Score += c.ScoreValue;
        PelletsRemaining--;
        damagedGhostsThisFrightened = 0;
        foreach (var g in Ghosts) g.Frighten(frightenedDuration);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPowerPellet();

        if (PelletsRemaining <= 0) StartCoroutine(WinRoutine());
    }

    public void OnSpecialBonusCollected(SpecialBonus b)
    {
        Score += b.ScoreValue;
        SpecialBonusesCollected++;
        if (bonusSpawner != null) bonusSpawner.OnBonusPicked();
    }

    // ───────── Партия ─────────

    private void StartNewGame()
    {
        Score = 0;
        Lives = initialLives;
        PelletsRemaining = TotalPellets;
        SpecialBonusesCollected = 0;
        damagedGhostsThisFrightened = 0;
        anyGhostFrightenedLastFrame = false;

        foreach (var p in allPellets) p.ResetCollectible();

        Pacman.gameObject.SetActive(true);
        Pacman.enabled = true;
        Pacman.Initialize(pacmanSpawn);

        for (int i = 0; i < Ghosts.Count; i++)
            Ghosts[i].ResetGhost(ghostSpawns[i]);

        if (bonusSpawner != null) bonusSpawner.ResetForNewGame();

        // Запуск партии идёт через корутину — с паузой на джингл.
        StartCoroutine(GameStartRoutine());
    }

    private IEnumerator GameStartRoutine()
    {
        // Мир заморожен, HUD уже виден, но Пакман и призраки не двигаются.
        Time.timeScale = 0f;
        SetState(GameState.Playing);

        float jingleDuration = 0f;

        if (AudioManager.Instance != null)
        {
            // Гасим то, что могло играть до этого (menuMusic, старая gameMusic и т.п.)
            AudioManager.Instance.StopMusic();

            // 1) Джингл старта — единственный звук на этот момент
            AudioManager.Instance.PlayGameStartJingle();
            jingleDuration = AudioManager.Instance.GetGameStartJingleDuration();
        }

        // 2) Ждём длину джингла (реального времени — timeScale = 0)
        if (jingleDuration > 0f)
            yield return new WaitForSecondsRealtime(jingleDuration);

        // 3) Джингл закончился — включаем основную музыку и размораживаем игру
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameMusic();

        Time.timeScale = 1f;
    }
    private IEnumerator DeathRoutine()
    {
        SetState(GameState.Dying);
        Time.timeScale = 0f;
        Pacman.enabled = false;
        Pacman.StopChomp();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlayDeath();
        }

        int losingIconIndex = Lives - 1;
        var pacSr = Pacman.GetComponent<SpriteRenderer>();
        Color pacOriginalColor = pacSr != null ? pacSr.color : Color.yellow;

        float t = 0f;
        while (t < deathAnimationDuration)
        {
            t += Time.unscaledDeltaTime;
            if (pacSr != null)
            {
                float progress = t / deathAnimationDuration;
                float alpha = progress < 0.6f
                    ? Mathf.PingPong(t * 15f, 1f)
                    : Mathf.Lerp(1f, 0f, (progress - 0.6f) / 0.4f);
                pacSr.color = new Color(1f, 1f, 0f, alpha);
            }
            yield return null;
        }

        Pacman.gameObject.SetActive(false);
        if (pacSr != null) pacSr.color = pacOriginalColor;

        Lives--;

        if (Lives <= 0)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLoseJingle();
            SetState(GameState.GameOver);
            yield break;
        }

        yield return new WaitForSecondsRealtime(respawnDelay);

        Pacman.gameObject.SetActive(true);
        Pacman.enabled = true;
        Pacman.Initialize(pacmanSpawn);

        for (int i = 0; i < Ghosts.Count; i++)
            Ghosts[i].ResetGhost(ghostSpawns[i]);

        damagedGhostsThisFrightened = 0;
        anyGhostFrightenedLastFrame = false;

        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGameMusic();
        SetState(GameState.Playing);
    }

    private IEnumerator WinRoutine()
    {
        SetState(GameState.Win);
        Time.timeScale = 0f;
        Pacman.StopChomp();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlayWinJingle();
        }
        yield return new WaitForSecondsRealtime(1.0f);
    }

    private void SetState(GameState s)
    {
        State = s;
        if (UIManager.Instance != null)
            UIManager.Instance.OnGameStateChanged(s);

        if (AudioManager.Instance == null) return;

        switch (s)
        {
            case GameState.MainMenu:
                AudioManager.Instance.PlayMenuMusic();
                break;
            case GameState.GameOver:
            case GameState.Win:
                AudioManager.Instance.StopMusic();
                break;
        }
    }

    // ───────── Утилиты ─────────

    public Vector2Int GetGhostTile(GhostType type)
    {
        foreach (var g in Ghosts)
            if (g.Type == type) return g.CurrentTile;
        return Vector2Int.zero;
    }
}