using UnityEngine;

public class Glitchdash : MonoBehaviour
{
    [Header("Glitch pickup")]
    public string glitchTag = "Glitch";    // tag on the collectible

    [Header("World scroll")]
    public float baseSpeed = 4f;           // fallback, falls kein Spawner in der Szene ist
    [Tooltip("Dash-Speed ist relativ zur aktuellen (wachsenden) Spawner-Speed, kein fester Wert")]
    public float dashSpeedMultiplier = 2.5f; // z.B. 2.5 = Dash ist 2.5x so schnell wie die aktuelle normale Speed
    public float speedSmoothing = 8f;      // how quickly speed eases between the two

    [Header("Dash / invincibility")]
    public float dashDuration = 1.2f;
    public float invincibleExtraTime = 0.3f; // invincibility lasts a bit longer than the dash

    [Header("Collision")]
    [Tooltip("Der Haupt-Collider für Kollisionen mit Asteroiden (NICHT der kleine Trigger-Collider fürs Glitch-Pickup). Wird während Invincibility zum Trigger, damit man ungehindert durchfliegt.")]
    public Collider2D playerCollider;

    [Header("Animation")]
    public Animator animator; // NEU: steuert den "glitch"-Trigger im Animator Controller

    // Everything that scrolls reads this
    public static float WorldSpeed { get; private set; }
    public static float BaseWorldSpeed { get; private set; } // Referenzwert für Multiplikator-Berechnung im Spawner

    public bool IsDashing => dashTimer > 0f;
    public bool IsInvincible => invincibleTimer > 0f;

    float dashTimer;
    float invincibleTimer;
    bool dead;
    SpriteRenderer sr;

    void Awake()
    {
        WorldSpeed = baseSpeed;
        BaseWorldSpeed = baseSpeed;
        sr = GetComponentInChildren<SpriteRenderer>();

        // NEU: Falls im Inspector nicht gesetzt, eigenen Collider verwenden
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();

        // NEU: Falls im Inspector nicht gesetzt, eigenen Animator verwenden
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (dead) return;

        dashTimer -= Time.deltaTime;
        invincibleTimer -= Time.deltaTime;

        // Normale Speed kommt vom Spawner (wächst mit der Zeit), Fallback auf baseSpeed falls kein Spawner existiert
        float normalSpeed = spawner.SpawnerActive ? spawner.CurrentDifficultySpeed : baseSpeed;
        BaseWorldSpeed = normalSpeed; // hält die Referenz für den Spawner-Multiplikator konsistent

        // Ease toward the target speed so the dash feels like a burst, not a snap
        // Dash-Ziel ist proportional zur aktuellen normalen Speed statt fix
        float target = IsDashing ? normalSpeed * dashSpeedMultiplier : normalSpeed;
        WorldSpeed = Mathf.Lerp(WorldSpeed, target, speedSmoothing * Time.deltaTime);

        // NEU: Während der Invincibility wird der Collider zum Trigger, dadurch gibt es
        // keine OnCollisionEnter2D-Events mehr mit Asteroiden -> keine Kollision/Game Over.
        // Danach wieder normaler (solider) Collider.
        if (playerCollider != null)
        {
            playerCollider.isTrigger = IsInvincible;
        }

        // Flicker while invincible so the player can see it
        if (sr != null)
        {
            Color c = sr.color;
            c.a = (IsInvincible && Mathf.Sin(Time.time * 30f) > 0f) ? 0.4f : 1f;
            sr.color = c;
        }

        // NEU: "glitch"-Bool im Animator hält den State so lange aktiv wie IsInvincible true ist
        // -> Animation dauert exakt so lange wie der komplette Dash (inkl. Invincible-Nachlaufzeit)
        if (animator != null) animator.SetBool("glitch", IsInvincible);
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

        // Animator-Bool wird in Update() über IsInvincible gesteuert, hier kein SetTrigger mehr nötig

        Destroy(glitch);
    }
}