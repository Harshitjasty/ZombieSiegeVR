using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PulseProjectile : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float speed = 35f;
    public float lifetime = 2f;
    public float damage = 35f;

    private Vector3 direction;
    private Rigidbody rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = false;

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Launch(Vector3 travelDirection, float projectileDamage)
    {
        direction = travelDirection.normalized;
        damage = projectileDamage;

        rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
            return;

        ZombieController zombie =
            collision.collider.GetComponentInParent<ZombieController>();

        if (zombie != null)
        {
            hasHit = true;
            zombie.TakeDamage(damage);
            Debug.Log("Pulse Rifle projectile hit zombie! Damage: " + damage);
            Destroy(gameObject);
        }
        else
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}