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
    [Tooltip("Das Canvas (oder GameObject), das beim Einsammeln kurz aktiviert wird. Wird normalerweise vom Spawner per Init() gesetzt.")]
    public GameObject canvasToShow;

    [Tooltip("Wie lange (in Sekunden) das Canvas sichtbar bleibt")]
    public float displayDuration = 3f;

    [Tooltip("Tag des Spielers, der den Wurm einsammeln kann")]
    public string playerTag = "Player";

    private bool isCollected = false;

    public void Init(GameObject canvas)
    {
        if (canvas != null)
            canvasToShow = canvas;
    }

    void Start()
    {
        currentHits = randomHitsToDestroy
            ? Random.Range(minRandomHits, maxRandomHits + 1)
            : hitsToDestroy;

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
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCollected) return;

        if (collision.collider.CompareTag(playerTag))
        {
            Collect();
        }
    }

    void Collect()
    {
        isCollected = true;

        gameObject.SetActive(false);

        if (canvasToShow != null)
        {
            CoroutineRunner.Instance.StartCoroutine(
                ShowCanvasThenDestroy(canvasToShow, displayDuration, gameObject)
            );
        }
        else
        {
            Destroy(gameObject);
        }
    }

    static IEnumerator ShowCanvasThenDestroy(GameObject canvas, float duration, GameObject wormToDestroy)
    {
        canvas.SetActive(true);
        yield return new WaitForSeconds(duration);
        canvas.SetActive(false);

        Destroy(wormToDestroy);
    }
}