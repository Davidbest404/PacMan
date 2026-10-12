using UnityEngine;

public class PowerPellet : Collectible
{
    public override int ScoreValue => 50;

    protected override void OnCollected()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPowerPelletCollected(this);
    }
}