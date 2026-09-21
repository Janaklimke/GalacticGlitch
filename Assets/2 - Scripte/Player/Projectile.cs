using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Lifetime Settings")]
    public float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}