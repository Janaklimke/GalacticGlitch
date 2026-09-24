using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float gravityScale = 2.5f;
    public float startingGravityScale = 0f;

    [Header("Start Delay")]
    public float startDelay = 2f;

    [Header("Animation")]
    public Animator animator;

    private Rigidbody2D rb;
    private Glitchdash glitchdash;
    private bool isStarting = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = startingGravityScale;
        rb.linearVelocity = Vector2.zero;

        if (animator == null) animator = GetComponent<Animator>();

        glitchdash = GetComponent<Glitchdash>();
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            return;

        if (isStarting)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Menu)
            {
                isStarting = true;

                GameManager.Instance.StartGame();

                StartCoroutine(StartMovementAfterDelay());
                return;
            }

            Jump();
        }
    }

    IEnumerator StartMovementAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);

        rb.gravityScale = gravityScale;
        Jump();

        isStarting = false;
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (animator != null && (glitchdash == null || !glitchdash.IsInvincible))
        {
            animator.SetTrigger("space pressed");
        }
    }

    public void Die()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            return;

        if (animator != null) animator.SetBool("death", true);

        rb.gravityScale = startingGravityScale;
        rb.linearVelocity = Vector2.zero;

        GameManager.Instance.GameOver();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            Die();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Die();
        }
    }
}