using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class SimpleProjectile2D : MonoBehaviour
{
    [Header("Impact")]
    [SerializeField] private bool destroyOnAnyCollision = true;
    [SerializeField] private GameObject vfxOnImpactHole;
    [SerializeField] private GameObject vfxOnImpact;

    [Header("Direction")]
    [SerializeField] private bool rotateToVelocity = false;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.None;
    }

    // NUEVO: dirección vectorial (4-dir o diagonal si algún día quieres)
    public void Launch(Vector2 dir, float speed, float lifetime)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();
        rb.linearVelocity = dir * speed;

        if (lifetime > 0f) Destroy(gameObject, lifetime);

        if (rotateToVelocity)
        {
            var v = rb.linearVelocity;
            if (v.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(ang, Vector3.forward);
            }
        }
    }

    // Compatibilidad con tu versión anterior (solo horizontal)
    public void Launch(int direction, float speed, float lifetime)
    {
        Launch(new Vector2(direction >= 0 ? 1f : -1f, 0f), speed, lifetime);
    }

    void Update()
    {
        if (rotateToVelocity)
        {
            var v = rb.linearVelocity;
            if (v.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(ang, Vector3.forward);
            }
        }
    }

    private void SpawnImpactVFX(bool hole = true)
    {
        if (!vfxOnImpact) return;

        Vector2 v = rb.linearVelocity;
        Quaternion rot = Quaternion.identity;

        if (v.sqrMagnitude > 0.0001f)
        {
            Vector2 opposite = -v.normalized;
            float ang = Mathf.Atan2(opposite.y, opposite.x) * Mathf.Rad2Deg;
            rot = Quaternion.AngleAxis(ang, Vector3.forward);
        }

        if (hole)
            Instantiate(vfxOnImpactHole, transform.position, rot);
        else
            Instantiate(vfxOnImpact, transform.position, rot);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!destroyOnAnyCollision) return;
        if (other.CompareTag("Enemy"))
        {
            SpawnImpactVFX(false);
        }
        else
        {
            SpawnImpactVFX();
        }
        if (!other.CompareTag("Player")) Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!destroyOnAnyCollision) return;
        if (collision.collider.CompareTag("Enemy"))
        {
            SpawnImpactVFX(false);
        }
        else
        {
            SpawnImpactVFX();
        }
        if (!collision.collider.CompareTag("Player")) Destroy(gameObject);
    }
}
