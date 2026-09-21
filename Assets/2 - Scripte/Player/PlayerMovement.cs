using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float gravityScale = 2.5f;
    public float startingGravityScale = 0f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = startingGravityScale;
        rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            return; // nach Game Over keine Eingaben mehr

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
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
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid") || collision.gameObject.CompareTag("Ground"))
        {
            GameManager.Instance.GameOver();
        }
    }
}