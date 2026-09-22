using UnityEngine;

public class ScrollingObject : MonoBehaviour //anything that moves
{
    public float speedMultiplier = 1f;  //set speed to something like 0.3 on backrgound
    public bool destroyWhenOffscreen = true;
    public float destroyX = -15f;        // x position past which the object is destroyed

    void Update()
    {
        transform.Translate(Vector3.left * Glitchdash.WorldSpeed * speedMultiplier * Time.deltaTime, Space.World);

        if (destroyWhenOffscreen && transform.position.x < destroyX)
            Destroy(gameObject);
    }
}