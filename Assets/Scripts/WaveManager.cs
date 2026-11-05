using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private GridMap gridMap;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

    private readonly Dictionary<GridEnemySettings, System.Random> randomByEnemySettings =
        new Dictionary<GridEnemySettings, System.Random>();
    private bool skipWaveTimerRequested;

    public int CurrentWaveNumber { get; private set; }
    public string CurrentWaveTitle { get; private set; }
    public float TimeUntilNextWave { get; private set; }
    public bool CanSkipWaveTimer { get; private set; }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("Cannot start enemy waves because no GameManager is available.", this);
            return;
        }

        gridMap = gameManager.GridMap;
        if (gridMap == null)
        {
            Debug.LogError("Cannot start enemy waves because no GridMap is available.", this);
            return;
        }

        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning("No waves are configured on the WaveManager.", this);
            return;
        }

        StartCoroutine(SpawnWaves());
    }

    public void SkipWaveTimer()
    {
        if (CanSkipWaveTimer)
        {
            skipWaveTimerRequested = true;
        }
    }

    private IEnumerator SpawnWaves()
    {
        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            WaveDefinition wave = waves[waveIndex];
            if (wave == null)
            {
                Debug.LogError($"Wave {waveIndex + 1} is not assigned.", this);
                continue;
            }

            if (wave.Duration <= 0f)
            {
                Debug.LogError($"Wave '{wave.WaveTitle}' must have a positive duration.", wave);
                continue;
            }

            CurrentWaveNumber = waveIndex + 1;
            CurrentWaveTitle = wave.WaveTitle;
            CanSkipWaveTimer = waveIndex < waves.Count - 1;
            TimeUntilNextWave = CanSkipWaveTimer ? wave.Duration : 0f;
            skipWaveTimerRequested = false;

            Debug.Log($"Starting wave {waveIndex + 1}: {wave.WaveTitle}", this);
            SpawnWaveEnemies(wave);

            while (CanSkipWaveTimer && TimeUntilNextWave > 0f && !skipWaveTimerRequested)
            {
                yield return null;
                TimeUntilNextWave = Mathf.Max(0f, TimeUntilNextWave - Time.deltaTime);
            }
        }

        CanSkipWaveTimer = false;
        TimeUntilNextWave = 0f;
    }

    private void SpawnWaveEnemies(WaveDefinition wave)
    {
        IReadOnlyList<WaveDefinition.EnemyEntry> enemies = wave.Enemies;
        if (enemies == null)
        {
            Debug.LogError($"Wave '{wave.WaveTitle}' has no enemy list.", wave);
            return;
        }

        for (int entryIndex = 0; entryIndex < enemies.Count; entryIndex++)
        {
            WaveDefinition.EnemyEntry entry = enemies[entryIndex];
            if (entry == null || entry.EnemySettings == null)
            {
                Debug.LogError(
                    $"Wave '{wave.WaveTitle}' has an enemy entry without settings.",
                    wave);
                continue;
            }

            GridEnemySettings settings = entry.EnemySettings;
            if (settings.EnemyPrefab == null)
            {
                Debug.LogError(
                    $"Enemy settings '{settings.name}' do not have a prefab assigned.",
                    settings);
                continue;
            }

            int spawnedCount = 0;
            System.Random random = GetRandom(settings);
            for (int enemyIndex = 0; enemyIndex < entry.Amount; enemyIndex++)
            {
                if (!gridMap.TryGetRandomEdgeSpawnCell(random, out Vector2Int spawnCell))
                {
                    Debug.LogWarning(
                        $"Could only spawn {spawnedCount} of {entry.Amount} " +
                        $"'{settings.name}' enemies for wave '{wave.WaveTitle}': " +
                        "no unoccupied edge cells are available.",
                        wave);
                    break;
                }

                if (!gridMap.TrySpawnEnemy(
                        settings.EnemyPrefab,
                        gameManager,
                        spawnCell,
                        settings.MovementSpeed,
                        settings.RetryInterval,
                        settings.BaseMachineDamage,
                        settings.MaxHealth,
                        settings.CurrencyReward,
                        out _))
                {
                    Debug.LogWarning(
                        $"Failed to spawn '{settings.name}' enemy at {spawnCell} " +
                        $"for wave '{wave.WaveTitle}'.",
                        wave);
                    break;
                }

                spawnedCount++;
            }
        }
    }

    private System.Random GetRandom(GridEnemySettings settings)
    {
        if (!randomByEnemySettings.TryGetValue(settings, out System.Random random))
        {
            random = new System.Random(settings.Seed);
            randomByEnemySettings.Add(settings, random);
        }

        return random;
    }
}
