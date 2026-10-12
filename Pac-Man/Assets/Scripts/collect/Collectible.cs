using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    public abstract int ScoreValue { get; }
    protected bool collected;

    public bool IsCollected => collected;

    public virtual void Collect()
    {
        if (collected) return;
        collected = true;
        OnCollected();
        gameObject.SetActive(false);
    }

    protected abstract void OnCollected();

    public virtual void ResetCollectible()
    {
        collected = false;
        gameObject.SetActive(true);
    }
}