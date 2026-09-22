using UnityEngine;

public class Glitchdash : MonoBehaviour
{
    [Header("Glitch pickup")]
    public string glitchTag = "Glitch";    // tag on the collectible

    [Header("World scroll")]
    public float baseSpeed = 4f;           // normal scroll speed (units/sec)
    public float dashSpeed = 10f;          // scroll speed while dashing
    public float speedSmoothing = 8f;      // how quickly speed eases between the two

    [Header("Dash / invincibility")]
    public float dashDuration = 1.2f;
    public float invincibleExtraTime = 0.3f; // invincibility lasts a bit longer than the dash

    // Everything that scrolls reads this
    public static float WorldSpeed { get; private set; }

    public bool IsDashing => dashTimer > 0f;
    public bool IsInvincible => invincibleTimer > 0f;

    float dashTimer;
    float invincibleTimer;
    bool dead;
    SpriteRenderer sr;

    void Awake()
    {
        WorldSpeed = baseSpeed;
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (dead) return;

        dashTimer -= Time.deltaTime;
        invincibleTimer -= Time.deltaTime;

        // Ease toward the target speed so the dash feels like a burst, not a snap
        float target = IsDashing ? dashSpeed : baseSpeed;
        WorldSpeed = Mathf.Lerp(WorldSpeed, target, speedSmoothing * Time.deltaTime);

        // Flicker while invincible so the player can see it
        if (sr != null)
        {
            Color c = sr.color;
            c.a = (IsInvincible && Mathf.Sin(Time.time * 30f) > 0f) ? 0.4f : 1f;
            sr.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;

        if (other.CompareTag(glitchTag))
        {
            CollectGlitch(other.gameObject);
        }
    }

    void CollectGlitch(GameObject glitch)
    {
        // Refreshes the timers if you grab another one mid-dash
        dashTimer = dashDuration;
        invincibleTimer = dashDuration + invincibleExtraTime;

        Destroy(glitch);
    }
}