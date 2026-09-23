using System.Collections;
using UnityEngine;

public class WormshootablleCollectible : MonoBehaviour
{
    [Header("Health Settings")]
    public bool randomHitsToDestroy = false;

    [Tooltip("Wird nur benutzt, wenn 'Random Hits To Destroy' AUS ist")]
    public int hitsToDestroy = 3;

    [Tooltip("Wird nur benutzt, wenn 'Random Hits To Destroy' AN ist")]
    public int minRandomHits = 1;
    public int maxRandomHits = 5;

    private int currentHits;

    [Header("Collectable Settings")]
    [Tooltip("Das Canvas (oder GameObject), das beim Einsammeln kurz aktiviert wird")]
    public GameObject canvasToShow;

    [Tooltip("Wie lange (in Sekunden) das Canvas sichtbar bleibt")]
    public float displayDuration = 3f;

    [Tooltip("Tag des Spielers, der den Wurm einsammeln kann")]
    public string playerTag = "Player";

    private bool isCollected = false;

    void Start()
    {
        if (randomHitsToDestroy)
        {
            currentHits = Random.Range(minRandomHits, maxRandomHits + 1);
        }
        else
        {
            currentHits = hitsToDestroy;
        }

        if (canvasToShow != null)
        {
            canvasToShow.SetActive(false);
        }
    }


    public void TakeHit()
    {
        if (isCollected) return;

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

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Bullet"))
        {
            TakeHit();
            Destroy(collision.gameObject);
            return;
        }

        if (collision.CompareTag(playerTag))
        {
            Collect();
        }
    }

    void Collect()
    {
        isCollected = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        StartCoroutine(ShowCanvasThenDestroy());
    }

    IEnumerator ShowCanvasThenDestroy()
    {
        if (canvasToShow != null)
        {
            canvasToShow.SetActive(true);
            yield return new WaitForSeconds(displayDuration);
            canvasToShow.SetActive(false);
        }

        Destroy(gameObject);
    }
}