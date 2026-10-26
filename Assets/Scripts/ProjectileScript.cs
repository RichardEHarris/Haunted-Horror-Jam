using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    private GridEnemy target;
    private float speed;
    private float damage;
    private float remainingLifetime;
    private bool initialized;

    public void Initialize(GridEnemy targetEnemy, float movementSpeed, float hitDamage, float lifetime)
    {
        if (targetEnemy == null)
        {
            Debug.LogError("A projectile requires a valid enemy target.", this);
            Destroy(gameObject);
            return;
        }

        if (movementSpeed < 0f || hitDamage < 0f || lifetime <= 0f)
        {
            Debug.LogError(
                "Projectile speed and damage must be non-negative, and lifetime must be positive.",
                this);
            Destroy(gameObject);
            return;
        }

        target = targetEnemy;
        speed = movementSpeed;
        damage = hitDamage;
        remainingLifetime = lifetime;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f || target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime);
        if ((transform.position - targetPosition).sqrMagnitude <= 0.0001f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
