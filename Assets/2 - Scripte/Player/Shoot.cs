using UnityEngine;

public class Shoot : MonoBehaviour
{
    [Header("Shoot Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 10f;
    public float projectileGravityScale = 0f;
    public KeyCode shootKey = KeyCode.Mouse0;

    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (Input.GetKeyDown(shootKey))
        {
            if (playerMovement != null && !playerMovement.HasStarted)
            {
                return;
            }

            Fire();
        }
    }

    void Fire()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Prefab oder FirePoint nicht zugewiesen!");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = projectileGravityScale;
            rb.linearVelocity = Vector2.right * projectileSpeed;
        }
        else
        {
            Debug.LogWarning("Kein Rigidbody2D am Projectile gefunden!");
        }
    }
}