using UnityEngine;

[CreateAssetMenu(fileName = "GridEnemySettings", menuName = "Grid/Enemy Settings")]
public class GridEnemySettings : ScriptableObject
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField, Min(0)] private int initialEnemyCount = 4;
    [SerializeField] private int seed = 24680;
    [SerializeField, Min(0.01f)] private float movementSpeed = 2f;
    [SerializeField, Min(0.05f)] private float retryInterval = 1f;
    [SerializeField, Min(0f)] private float baseMachineDamage = 10f;

    public GameObject EnemyPrefab => enemyPrefab;
    public int InitialEnemyCount => initialEnemyCount;
    public int Seed => seed;
    public float MovementSpeed => movementSpeed;
    public float RetryInterval => retryInterval;
    public float BaseMachineDamage => baseMachineDamage;

    private void OnValidate()
    {
        initialEnemyCount = Mathf.Max(0, initialEnemyCount);
        movementSpeed = Mathf.Max(0.01f, movementSpeed);
        retryInterval = Mathf.Max(0.05f, retryInterval);
        baseMachineDamage = Mathf.Max(0f, baseMachineDamage);
    }
}
