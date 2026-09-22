using UnityEngine;

public class Shoot : MonoBehaviour
{
    [Header("Shoot Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 10f;
    public float projectileGravityScale = 0f;
    public KeyCode shootKey = KeyCode.Mouse0;

    [Header("Animation")]
    public Animator animator; // NEU: steuert den "pew"-Trigger

    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        // NEU: Falls im Inspector nicht gesetzt, eigenen Animator verwenden
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return; // nur schießen während des Spiels

        if (Input.GetKeyDown(shootKey))
        {
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

        if (animator != null) animator.SetTrigger("pew");
    }
}