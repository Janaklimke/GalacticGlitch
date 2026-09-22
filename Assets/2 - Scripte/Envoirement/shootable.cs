using UnityEngine;

public class shootable : MonoBehaviour
{
    [Header("Health Settings")]
    public bool randomHitsToDestroy = false;

    [Tooltip("Wird nur benutzt, wenn 'Random Hits To Destroy' AUS ist")]
    public int hitsToDestroy = 3;

    [Tooltip("Wird nur benutzt, wenn 'Random Hits To Destroy' AN ist")]
    public int minRandomHits = 1;
    public int maxRandomHits = 5;

    private int currentHits;

    void Start()
    {
        if (randomHitsToDestroy)
        {
            // maxRandomHits + 1, da Random.Range bei int-Werten exklusiv ist
            currentHits = Random.Range(minRandomHits, maxRandomHits + 1);
        }
        else
        {
            currentHits = hitsToDestroy;
        }
    }

    public void TakeHit()
    {
        currentHits--;

        if (currentHits <= 0)
        {
            DestroyObject();
        }
    }

    void DestroyObject()
    {
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            TakeHit();
            Destroy(collision.gameObject);
        }
    }
}