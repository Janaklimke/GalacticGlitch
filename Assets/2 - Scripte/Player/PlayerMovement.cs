using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float gravityScale = 2.5f;
    public float startingGravityScale = 0f;

    [Header("Animation")]
    public Animator animator; // NEU: steuert "space pre" (Jump-Trigger) und "death" (Bool)

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = startingGravityScale;
        rb.linearVelocity = Vector2.zero;

        // NEU: Falls im Inspector nicht gesetzt, eigenen Animator verwenden
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            return; // nach Game Over keine Eingaben mehr

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Menu)
            {
                GameManager.Instance.StartGame();
                rb.gravityScale = gravityScale;
            }

            Jump();
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // NEU: Trigger-Parameter "space pre" feuern -> Übergang zu "fly"
        if (animator != null) animator.SetTrigger("space pre");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid") || collision.gameObject.CompareTag("Ground"))
        {
            // NEU: Bool-Parameter "death" setzen -> Übergang zu "death"
            if (animator != null) animator.SetBool("death", true);

            GameManager.Instance.GameOver();
        }
    }
}