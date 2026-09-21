using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float gravityScale = 2.5f;

    [Header("Start Settings")]
    public float startingGravityScale = 0f;

    private Rigidbody2D rb;
    public bool HasStarted { get; private set; } = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = startingGravityScale;
        rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!HasStarted)
            {
                StartGame();
            }

            Jump();
        }
    }

    void StartGame()
    {
        HasStarted = true;
        rb.gravityScale = gravityScale;
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
}