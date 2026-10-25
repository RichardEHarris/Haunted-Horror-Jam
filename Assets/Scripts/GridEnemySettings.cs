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
    [SerializeField, Min(0.01f)] private float maxHealth = 10f;
    [SerializeField, Min(0)] private int currencyReward = 5;

    public GameObject EnemyPrefab => enemyPrefab;
    public int InitialEnemyCount => initialEnemyCount;
    public int Seed => seed;
    public float MovementSpeed => movementSpeed;
    public float RetryInterval => retryInterval;
    public float BaseMachineDamage => baseMachineDamage;
    public float MaxHealth => maxHealth;
    public int CurrencyReward => currencyReward;

    private void OnValidate()
    {
        initialEnemyCount = Mathf.Max(0, initialEnemyCount);
        movementSpeed = Mathf.Max(0.01f, movementSpeed);
        retryInterval = Mathf.Max(0.05f, retryInterval);
        baseMachineDamage = Mathf.Max(0f, baseMachineDamage);
        maxHealth = Mathf.Max(0.01f, maxHealth);
        currencyReward = Mathf.Max(0, currencyReward);
    }
}
