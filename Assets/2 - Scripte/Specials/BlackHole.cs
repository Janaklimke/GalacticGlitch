using UnityEngine;

public class BlackHole : MonoBehaviour
{
    [Header("Pull")]
    public float pullRadius = 2f;
    public float pullStrength = 30f;   // force at the centre, fades to 0 at the edge of the radius
    public bool verticalOnly = true;   // player has a fixed x, so pull up/down only

    [Header("Kill")]
    public bool killsInvincible = false; // true = even a dashing player dies

    void FixedUpdate()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pullRadius);
        Debug.Log("Hits in range: " + hits.Length);

        foreach (Collider2D hit in hits)
        {
            Debug.Log("Found player, applying force");
            Glitchdash player = hit.GetComponentInParent<Glitchdash>();
            if (player == null) continue;

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            Vector2 toHole = (Vector2)transform.position - rb.position;

            // Stronger the closer you are
            float falloff = 1f - Mathf.Clamp01(toHole.magnitude / pullRadius);

            Vector2 dir = verticalOnly ? new Vector2(0f, Mathf.Sign(toHole.y)) : toHole.normalized;
            rb.AddForce(dir * pullStrength * falloff);
        }
    }

    // Stay (not Enter) so you still die if invincibility ends while inside
    void OnTriggerStay2D(Collider2D other)
    {
        Glitchdash player = other.GetComponentInParent<Glitchdash>();
        if (player == null) return;

        if (player.IsInvincible && !killsInvincible) return;

        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.Die();
        }
    }

    // Shows the pull radius in the Scene view when the object is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
}