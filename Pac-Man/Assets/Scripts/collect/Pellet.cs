using UnityEngine;

public class Pellet : Collectible
{
    public override int ScoreValue => 10;

    protected override void OnCollected()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPelletCollected(this);
    }
}