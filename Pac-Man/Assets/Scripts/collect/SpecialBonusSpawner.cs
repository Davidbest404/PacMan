using UnityEngine;

public class SpecialBonusSpawner : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int bonusPoints = 500;

    [Header("Timing (секунды)")]
    [SerializeField] private float minSpawnDelay = 8f;
    [SerializeField] private float maxSpawnDelay = 15f;

    [Header("Limit")]
    [SerializeField] private int maxBonusesPerSession = 4;

    [Header("Spawn points (в тайлах сетки)")]
    [SerializeField]
    private Vector2Int[] spawnPoints = new Vector2Int[]
    {
        new Vector2Int( 1, 27),
        new Vector2Int(26, 27),
        new Vector2Int( 3,  5),
        new Vector2Int(24,  5),
        new Vector2Int(13,  1),
    };

    [Header("Visual")]
    [SerializeField] private float bonusScale = 1.1f;

    private GameManager gm;
    private int spawnedCount;
    private float nextSpawnTimer;
    private int lastSpawnIndex = -1;
    private SpecialBonus activeBonus;

    public int SpawnedCount => spawnedCount;
    public bool CanSpawnMore => spawnedCount < maxBonusesPerSession;

    public void Setup(GameManager gm) { this.gm = gm; }

    public void ResetForNewGame()
    {
        if (activeBonus != null) Destroy(activeBonus.gameObject);
        activeBonus = null;
        spawnedCount = 0;
        lastSpawnIndex = -1;
        ScheduleNext();
    }

    public void OnBonusPicked()
    {
        activeBonus = null;
        if (CanSpawnMore) ScheduleNext();
    }

    private void ScheduleNext()
    {
        nextSpawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    private void Update()
    {
        if (gm == null || gm.State != GameState.Playing) return;
        if (activeBonus != null) return;
        if (!CanSpawnMore) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer > 0f) return;

        SpawnBonus();
    }

    private void SpawnBonus()
    {
        int index = PickSpawnIndex();
        Vector2Int tile = spawnPoints[index];
        lastSpawnIndex = index;

        var go = SpriteFactory.CreateSprite(
            "SpecialBonus",
            SpriteFactory.CherrySprite,
            Color.white,
            3);
        go.transform.position = new Vector3(tile.x, tile.y, 0f);
        go.transform.localScale = Vector3.one * bonusScale;

        var bonus = go.AddComponent<SpecialBonus>();
        bonus.Setup(gm, bonusPoints);

        activeBonus = bonus;
        spawnedCount++;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBonusSpawn();
    }

    private int PickSpawnIndex()
    {
        if (spawnPoints.Length == 1) return 0;
        int index;
        int safety = 0;
        do { index = Random.Range(0, spawnPoints.Length); safety++; }
        while (index == lastSpawnIndex && safety < 20);
        return index;
    }
}