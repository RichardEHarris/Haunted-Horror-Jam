using UnityEngine;

public class BehaviourShooter : BehaviourScript
{
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0f)] private float targetRange = 5f;
    [SerializeField, Min(0f)] private float projectileSpeed = 8f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 5f;

    protected override void OnValidate()
    {
        base.OnValidate();
        targetRange = Mathf.Max(0f, targetRange);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        damage = Mathf.Max(0f, damage);
        projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
    }

    protected override void OnTick()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("The placeable object shooter has no projectile prefab assigned.", this);
            return;
        }

        Vector3 spawnPosition = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position;
        if (!GridMap.TryGetClosestEnemy(spawnPosition, targetRange, out GridEnemy target))
        {
            return;
        }

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            spawnPosition,
            projectilePrefab.transform.rotation);
        ProjectileScript projectile =
            projectileObject.GetComponent<ProjectileScript>();
        if (projectile == null)
        {
            Debug.LogError(
                "The projectile prefab must have a PlaceableObjectProjectile component.",
                projectileObject);
            Destroy(projectileObject);
            return;
        }

        projectile.Initialize(target, projectileSpeed, damage, projectileLifetime);
    }
}
