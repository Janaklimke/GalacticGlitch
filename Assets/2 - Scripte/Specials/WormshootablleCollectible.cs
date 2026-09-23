using System.Collections;
using UnityEngine;

public class WormshootablleCollectible : MonoBehaviour
{
    [Header("Health Settings")]
    public bool randomHitsToDestroy = false;
    public int hitsToDestroy = 3;
    public int minRandomHits = 1;
    public int maxRandomHits = 5;

    private int currentHits;

    [Header("Collectable Settings")]
    public GameObject canvasToShow;
    public float displayDuration = 3f;
    public string playerTag = "Player";

    private bool isCollected = false;

    void Start()
    {
        currentHits = randomHitsToDestroy
            ? Random.Range(minRandomHits, maxRandomHits + 1)
            : hitsToDestroy;

        if (canvasToShow != null)
            canvasToShow.SetActive(false);
    }

    public void TakeHit()
    {
        if (isCollected) return;

        currentHits--;
        if (currentHits <= 0)
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
                ShowCanvasThenHide(canvasToShow, displayDuration, gameObject)
            );
        }
        else
        {
            Destroy(gameObject);
        }
    }

    static IEnumerator ShowCanvasThenHide(GameObject canvas, float duration, GameObject wormToDestroy)
    {
        canvas.SetActive(true);
        yield return new WaitForSeconds(duration);
        canvas.SetActive(false);

        Destroy(wormToDestroy);
    }
}