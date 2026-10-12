using UnityEngine;

public class Pacman : GridEntity
{
    [Header("Animation")]
    [SerializeField] private float animationFPS = 10f;

    private static readonly int[] MouthFrames = { 0, 30, 60, 30 };

    private Vector2Int desiredDirection = Vector2Int.right;
    private SpriteRenderer sr;
    private Sprite[] frameSprites;
    private float animationTimer;
    private int animationFrame;
    private bool chompPlaying;

    public Vector2Int FacingDirection =>
        currentDirection == Vector2Int.zero ? Vector2Int.right : currentDirection;

    public bool IsMoving => CurrentTile != targetTile;

    public override void Initialize(Vector2Int startTile)
    {
        base.Initialize(startTile);
        currentDirection = Vector2Int.right;
        desiredDirection = Vector2Int.right;
        targetTile = startTile + currentDirection;
        RotateSprite(currentDirection);

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (frameSprites == null)
        {
            frameSprites = new Sprite[MouthFrames.Length];
            for (int i = 0; i < MouthFrames.Length; i++)
                frameSprites[i] = SpriteFactory.GetPacmanSprite(MouthFrames[i]);
        }
        if (sr != null) sr.sprite = frameSprites[1];
        animationFrame = 1;
        animationTimer = 0f;

        // На всякий случай — чтобы после респавна не тянулся старый chomp.
        StopChomp();
    }

    protected override void Update()
    {
        HandleInput();
        UpdateAnimation();
        UpdateChompSound();
        base.Update();
    }

    private void UpdateChompSound()
    {
        // Chomp играет ТОЛЬКО когда партия реально идёт
        // (не в меню, не на экранах победы/поражения/смерти)
        // и когда Пакман физически перемещается между тайлами.
        var gm = GameManager.Instance;
        bool playing = gm != null
                    && gm.State == GameState.Playing
                    && Time.timeScale > 0f; bool shouldChomp = playing && IsMoving;

        if (shouldChomp && !chompPlaying)
        {
            chompPlaying = true;
            if (AudioManager.Instance != null) AudioManager.Instance.StartChomp();
        }
        else if (!shouldChomp && chompPlaying)
        {
            StopChomp();
        }
    }

    public void StopChomp()
    {
        chompPlaying = false;
        if (AudioManager.Instance != null) AudioManager.Instance.StopChomp();
    }

    private void UpdateAnimation()
    {
        if (sr == null || frameSprites == null) return;

        if (!IsMoving)
        {
            if (sr.sprite != frameSprites[1]) sr.sprite = frameSprites[1];
            return;
        }

        animationTimer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(1f, animationFPS);
        while (animationTimer >= frameTime)
        {
            animationTimer -= frameTime;
            animationFrame = (animationFrame + 1) % frameSprites.Length;
        }
        sr.sprite = frameSprites[animationFrame];
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) desiredDirection = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) desiredDirection = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) desiredDirection = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) desiredDirection = Vector2Int.right;
    }

    protected override void OnTileReached()
    {
        bool dirChanged = false;
        if (CanMove(desiredDirection))
        {
            if (currentDirection != desiredDirection) dirChanged = true;
            currentDirection = desiredDirection;
        }
        targetTile = CanMove(currentDirection) ? CurrentTile + currentDirection : CurrentTile;
        if (dirChanged) RotateSprite(currentDirection);
    }

    private void RotateSprite(Vector2Int dir)
    {
        float angle = 0f;
        if (dir == Vector2Int.up) angle = 90f;
        else if (dir == Vector2Int.left) angle = 180f;
        else if (dir == Vector2Int.down) angle = 270f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}