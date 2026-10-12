using UnityEngine;

public class SpecialBonus : Collectible
{
    [SerializeField] private float collectRadius = 0.55f;

    private int scoreValue = 500;
    private GameManager gm;
    private Vector3 basePosition;
    private float bobTimer;

    public override int ScoreValue => scoreValue;

    public void Setup(GameManager gm, int points)
    {
        this.gm = gm;
        scoreValue = points;
        basePosition = transform.position;
    }

    private void Update()
    {
        bobTimer += Time.deltaTime;
        transform.position = basePosition + new Vector3(0f, Mathf.Sin(bobTimer * 3f) * 0.12f, 0f);

        if (collected) return;
        if (gm == null || gm.Pacman == null) return;
        if (gm.State != GameState.Playing) return;

        Vector3 pp = gm.Pacman.transform.position;
        if ((basePosition - pp).sqrMagnitude <= collectRadius * collectRadius)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBonusCollect();
            Collect();
        }
    }

    protected override void OnCollected()
    {
        if (gm != null) gm.OnSpecialBonusCollected(this);
    }
}