using UnityEngine;

public class DestroyZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Asteroid"))
        {
            Destroy(other.gameObject);
        }
    }
}